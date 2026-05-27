// First working modified example code for new architecture
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

// command-line parameters
struct whisper_params {
    //int32_t n_threads  = std::min(4, (int32_t) std::thread::hardware_concurrency());
    int32_t n_threads  = 10;
    int32_t step_ms    = 0;
    int32_t length_ms  = 3000;
    int32_t keep_ms    = 0;
    int32_t capture_id = -1;
    int32_t max_tokens = 32;
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
    bool save_audio    = false; // save audio to wav file
    bool use_gpu       = true;
    bool flash_attn    = true;

    std::string language  = "fr";
    std::string model     = "C:/Users/redacted/projectE3/whisper.cpp/models/ggml-large-v3-q5_0.bin";
    std::string fname_out;
};

int main(int argc, char ** argv) {
    ggml_backend_load_all();

    whisper_params params;

    //params.keep_ms   = std::min(params.keep_ms,   params.step_ms);
    //params.length_ms = std::max(params.length_ms, params.step_ms);

    const int n_samples_step = (1e-3*params.step_ms  )*WHISPER_SAMPLE_RATE;
    const int n_samples_len  = (1e-3*params.length_ms)*WHISPER_SAMPLE_RATE;
    const int n_samples_keep = (1e-3*params.keep_ms  )*WHISPER_SAMPLE_RATE;
    const int n_samples_30s  = (1e-3*30000.0         )*WHISPER_SAMPLE_RATE;

    //const bool use_vad = n_samples_step <= 0; // sliding window mode uses VAD
    const bool use_vad = true; // sliding window mode uses VAD

    params.no_timestamps  = !use_vad;
    params.no_context    |= use_vad;
    params.max_tokens     = 0;

    // init audio

    audio_async audio(params.length_ms);
    if (!audio.init(params.capture_id, WHISPER_SAMPLE_RATE)) {
        //fprintf(stderr, "%s: audio.init() failed!\n", __func__);
        return 1;
    }

    audio.resume();

    // whisper init
    if (params.language != "auto" && whisper_lang_id(params.language.c_str()) == -1){
        //fprintf(stderr, "error: unknown language '%s'\n", params.language.c_str());
        exit(0);
    }

    struct whisper_context_params cparams = whisper_context_default_params();

    cparams.use_gpu    = params.use_gpu;
    cparams.flash_attn = params.flash_attn;

    struct whisper_context * ctx = whisper_init_from_file_with_params(params.model.c_str(), cparams);
    if (ctx == nullptr) {
        //fprintf(stderr, "error: failed to initialize whisper context\n");
        return 2;
    }

    std::vector<float> pcmf32    (n_samples_30s, 0.0f);
    std::vector<float> pcmf32_old;
    std::vector<float> pcmf32_new(n_samples_30s, 0.0f);

    std::vector<whisper_token> prompt_tokens;

    // print some info about the processing
    {
        //fprintf(stderr, "\n");
        if (!whisper_is_multilingual(ctx)) {
            //fprintf(stderr, "%s: WARNING: model is not multilingual, French is not supported - exiting...\n", __func__);
            exit(0);
        }
        /*
        fprintf(stderr, "%s: processing %d samples (step = %.1f sec / len = %.1f sec / keep = %.1f sec), %d threads, lang = %s, task = %s, timestamps = %d ...\n",
                __func__,
                n_samples_step,
                float(n_samples_step)/WHISPER_SAMPLE_RATE,
                float(n_samples_len )/WHISPER_SAMPLE_RATE,
                float(n_samples_keep)/WHISPER_SAMPLE_RATE,
                params.n_threads,
                params.language.c_str(),
                params.translate ? "translate" : "transcribe",
                params.no_timestamps ? 0 : 1);

        fprintf(stderr, "%s: using VAD, will transcribe on speech activity\n", __func__);

        fprintf(stderr, "\n");
        */
        fprintf(stderr, "Ready !\n");
    }

    int n_iter = 0;

    bool is_running = true;

    //printf("[Start speaking]\n");
    //fflush(stdout);
    //std::cout << "\n[Start speaking]\n" << std::flush;

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

            if (t_diff < 2000) {
                std::this_thread::sleep_for(std::chrono::milliseconds(100));

                continue;
            }

            audio.get(2000, pcmf32_new);

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
                //fprintf(stderr, "failed to process audio\n");
                return 6;
            }

            // print result;
            {
                //const int64_t t1 = (t_last - t_start).count()/1000000;
                //const int64_t t0 = std::max(0.0, t1 - pcmf32.size()*1000.0/WHISPER_SAMPLE_RATE);

                //printf("\n");
                //printf("### Transcription %d START | t0 = %d ms | t1 = %d ms\n", n_iter, (int) t0, (int) t1);
                //printf("\n");
                //std::cout << "\n### Transcription " << n_iter << " START | t0 = " << (int) t0 << " ms | t1 = " << (int) t1 << " ms\n\n" << std::flush;

                const int n_segments = whisper_full_n_segments(ctx);
                for (int i = 0; i < n_segments; ++i) {
                    const char * text = whisper_full_get_segment_text(ctx, i);

                    if (!params.no_timestamps) {

                        //const int64_t t0 = whisper_full_get_segment_t0(ctx, i);
                        //const int64_t t1 = whisper_full_get_segment_t1(ctx, i);

                        //std::string output = "[" + to_timestamp(t0, false) + " --> " + to_timestamp(t1, false) + "]  " + text;
                        std::string output = text;

                        /*
                        if (whisper_full_get_segment_speaker_turn_next(ctx, i)) {
                            output += " [SPEAKER_TURN]";
                        }
                        */
                        output += "\n";

                        //printf("%s", output.c_str());
                        //fflush(stdout);
                        std::cout << output << std::flush;


                    }
                }
                /*
                if (use_vad) {
                    //printf("\n");
                    //printf("### Transcription %d END\n", n_iter);
                    std::cout << "\n### Transcription " << n_iter << " END\n" << std::flush;
                }
                */
            }

            //++n_iter;
            //fflush(stdout);
        }
    }

    audio.pause();

    //whisper_print_timings(ctx);
    whisper_free(ctx);

    return 0;
}
