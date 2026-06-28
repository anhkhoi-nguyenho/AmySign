#include "common-sdl.h"
#include "common.h"
#include "common-whisper.h"
#include "whisper.h"

#include <chrono>
#include <iostream>
#include <cstdio>
#include <fstream>
#include <string>
#include <thread>
#include <vector>
#include <process.h>

#define NOMINMAX // Prevent windows.h from defining min, conflicting with std::min
#include <windows.h>

// command-line parameters
struct whisper_params {
    int32_t n_threads  = std::min(4, (int32_t) std::thread::hardware_concurrency());
    int32_t step_ms    = 0;
    int32_t length_ms  = 5000;
    int32_t keep_ms    = 0;
    int32_t capture_id = -1;
    int32_t max_tokens = 0;
    int32_t audio_ctx  = 0;
    int32_t beam_size  = -1;

    float vad_thold    = 0.6f;
    float freq_thold   = 100.0f;

    bool translate     = false;
    bool no_fallback   = false;
    bool print_special = false;
    bool no_context    = true;
    bool no_timestamps = false;
    bool tinydiarize   = false;
    bool save_audio    = false;
    bool use_gpu       = true;
    bool flash_attn    = false;

    std::string language  = "fr";
    std::string model;
    std::string fname_out;
};

int32_t cooldown_duration 	= 2000;
int32_t sample_duration 	= 3000;

void display_configurable_options(const whisper_params & params){
	std::cerr << "\nwhisper-stream configurable options:\n" << std::flush;
	std::cerr << "  -t N,     --threads N          number of threads to use during computation                    -   current:   " << params.n_threads     					 << "\n" << std::flush;
	std::cerr << "            --length N           audio length in milliseconds                                   -   current:   " << params.length_ms     					 << "\n" << std::flush;
	std::cerr << "  -vth N,   --vad-thold N        voice activity detection threshold (higher = more sensitive)   -   current:   " << params.vad_thold     					 << "\n" << std::flush;
	std::cerr << "  -fth N,   --freq-thold N       high-pass frequency cutoff                                     -   current:   " << params.freq_thold    					 << "\n" << std::flush;
	std::cerr << "  -ac N,    --audio-ctx N        audio context size (0 - all)                                   -   current:   " << params.audio_ctx     					 << "\n" << std::flush;
	std::cerr << "  -bs N,    --beam-size N        beam size for beam search                                      -   current:   " << params.beam_size 	  					 << "\n" << std::flush;
	std::cerr << "  -m FNAME, --model FNAME        model path                                                     -   current:   " << params.model.c_str() 					 << "\n" << std::flush;
	std::cerr << "  -fa,      --flash-attn         enable flash attention during inference                        -   current:   " << (params.flash_attn ? "true" : "false") << "\n" << std::flush;
	std::cerr << "  -cd,      --cooldown           cooldown duration of vad                                       -   current:   " << cooldown_duration						 << "\n" << std::flush;
	std::cerr << "  -sd,      --sample-duration    sample duration of vad                                         -   current:   " << sample_duration						 << "\n" << std::flush;
}

static bool whisper_params_parse(int argc, char ** argv, whisper_params & params) {
    for (int i = 1; i < argc; i++) {
        std::string arg = argv[i];

        if 		(arg == "-t"    || arg == "--threads")          { params.n_threads     = std::stoi(argv[++i]); }
        else if (                  arg == "--length")           { params.length_ms     = std::stoi(argv[++i]); }
        else if (arg == "-vth"  || arg == "--vad-thold")        { params.vad_thold     = std::stof(argv[++i]); }
        else if (arg == "-fth"  || arg == "--freq-thold")       { params.freq_thold    = std::stof(argv[++i]); }
        else if (arg == "-ac"   || arg == "--audio-ctx")        { params.audio_ctx     = std::stoi(argv[++i]); }
		else if (arg == "-bs"   || arg == "--beam-size")        { params.beam_size     = std::stoi(argv[++i]); }
        else if (arg == "-m"    || arg == "--model")            { params.model         = argv[++i]; }
		else if (arg == "-fa"   || arg == "--flash-attn")       { params.flash_attn    = true; }
		else if (arg == "-cd"   || arg == "--cooldown")         { cooldown_duration    = std::stoi(argv[++i]); }
		else if (arg == "-sd"   || arg == "--sample-duration")  { sample_duration      = std::stoi(argv[++i]); }
        else {
			std::cerr << "Error: unknown argument: " << arg.c_str() << "\n" << std::flush;
            exit(0);
        }
    }

    return true;
}


int main(int argc, char ** argv) {

	SetConsoleOutputCP(CP_UTF8);
    SetConsoleCP(CP_UTF8);
	
	int pid = _getpid();
	
	std::cerr << "pid" << pid << std::endl;
	
    ggml_backend_load_all();

    whisper_params params;

	if (whisper_params_parse(argc, argv, params) == false) {
        return 1;
    }
	
	if (params.model.empty()) {
		std::cerr << "Error: please specify model location using option -m\n" << std::flush;
		exit(0);
	}

    const int n_samples_step = (1e-3*params.step_ms  )*WHISPER_SAMPLE_RATE;
    const int n_samples_len  = (1e-3*params.length_ms)*WHISPER_SAMPLE_RATE;
    const int n_samples_keep = (1e-3*params.keep_ms  )*WHISPER_SAMPLE_RATE;
    const int n_samples_30s  = (1e-3*30000.0         )*WHISPER_SAMPLE_RATE;

    const bool use_vad = true; // sliding window mode uses VAD

    // init audio

    audio_async audio(params.length_ms);
    if (!audio.init(params.capture_id, WHISPER_SAMPLE_RATE)) {
        std::cerr << "Error: audio init failed!\n" << std::flush;
        return 1;
    }

    audio.resume();

    // whisper init
    if (params.language != "auto" && whisper_lang_id(params.language.c_str()) == -1){
        std::cerr << "Error: unknown language\n" << std::flush;
        exit(0);
    }

    struct whisper_context_params cparams = whisper_context_default_params();

    cparams.use_gpu    = params.use_gpu;
    cparams.flash_attn = params.flash_attn;

    struct whisper_context * ctx = whisper_init_from_file_with_params(params.model.c_str(), cparams);
    if (ctx == nullptr) {
        std::cerr << "Error: failed to initialize whisper context!\n" << std::flush;
        return 2;
    }

    std::vector<float> pcmf32    (n_samples_30s, 0.0f);
    std::vector<float> pcmf32_old;
    std::vector<float> pcmf32_new(n_samples_30s, 0.0f);

    std::vector<whisper_token> prompt_tokens;
	
	display_configurable_options(params);

	if (!whisper_is_multilingual(ctx)) {
		std::cerr << "Error: model is not multilingual, French is not supported!\n" << std::flush;
		exit(0);
	}
	std::cerr << "READY\n" << std::flush;

    int n_iter = 0;

    bool is_running = true;

    auto t_last  = std::chrono::high_resolution_clock::now();
    const auto t_start = t_last;

    // main audio loop
    while (is_running) {

        // handle Ctrl + C
        is_running = sdl_poll_events();

        if (!is_running) {
            break;
        }

        // process new audio
        {
            const auto t_now  = std::chrono::high_resolution_clock::now();
            const auto t_diff = std::chrono::duration_cast<std::chrono::milliseconds>(t_now - t_last).count();

            if (t_diff < cooldown_duration) {
                std::this_thread::sleep_for(std::chrono::milliseconds(100));

                continue;
            }

            audio.get(sample_duration, pcmf32_new);

            if (::vad_simple(pcmf32_new, WHISPER_SAMPLE_RATE, 1000, params.vad_thold, params.freq_thold, false)) {
                audio.get(params.length_ms, pcmf32);
            } else {
                std::this_thread::sleep_for(std::chrono::milliseconds(100));

                continue;
            }

            t_last = t_now;
        }

        // run the inference
        {
            whisper_full_params wparams = whisper_full_default_params(params.beam_size > 1 ? WHISPER_SAMPLING_BEAM_SEARCH : WHISPER_SAMPLING_GREEDY);

            wparams.print_progress   = false;
            wparams.print_special    = params.print_special;
            wparams.print_realtime   = false;
            wparams.print_timestamps = !params.no_timestamps;
            wparams.translate        = params.translate;
            wparams.single_segment   = !use_vad;
            wparams.max_tokens       = params.max_tokens;
            wparams.language         = params.language.c_str();
            wparams.n_threads        = params.n_threads;
            wparams.beam_search.beam_size = params.beam_size;

            wparams.audio_ctx        = params.audio_ctx;

            wparams.tdrz_enable      = params.tinydiarize; // [TDRZ]

            // disable temperature fallback
            //wparams.temperature_inc  = -1.0f;
            wparams.temperature_inc  = params.no_fallback ? 0.0f : wparams.temperature_inc;

            wparams.prompt_tokens    = params.no_context ? nullptr : prompt_tokens.data();
            wparams.prompt_n_tokens  = params.no_context ? 0       : prompt_tokens.size();

            if (whisper_full(ctx, wparams, pcmf32.data(), pcmf32.size()) != 0) {
                std::cerr << "Error: failed to process audio\n" << std::flush;
                return 6;
            }

            // print result;
            {
                const int n_segments = whisper_full_n_segments(ctx);
                for (int i = 0; i < n_segments; ++i) {
                    const char * text = whisper_full_get_segment_text(ctx, i);

					std::string output = text;

					output += "\n";

					std::cout << output << std::flush;
                }
            }
        }
    }

    audio.pause();

    whisper_free(ctx);

    return 0;
}