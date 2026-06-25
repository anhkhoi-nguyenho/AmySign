import sys
import subprocess
from AmySign_code.glosses import pipeline
import orjson
from pathlib import Path

# 1. Force Python to output stdout and stderr in UTF-8
sys.stdout.reconfigure(encoding='utf-8')
sys.stderr.reconfigure(encoding='utf-8')

def json_output_string(data: dict):
    # Serialize to bytes and Decode to string
    return orjson.dumps(data).decode('utf-8')

def main():

    root = Path(sys.executable).parent.parent
    
    whisper_dir = root / "whisper"

    command = [
        str(whisper_dir / "whisper-stream.exe"),
        "-t", "12",
        "-m", str(whisper_dir / "ggml-model-q5_0.bin"),
        "-sd", "1100",
        "-cd", "1500",
        "-vth", "0.4",
        "--length", "5000",
        "-bs", "1",
        "-ac", "1500",
    ]

    process = subprocess.Popen(command, stdout=subprocess.PIPE, encoding='utf-8')
    for line in iter(process.stdout.readline, ''):
        
        etat = pipeline(line.strip())
        
        printFrench = 1
        for gloss in etat.glosses_finales :
            data = {"gloss": gloss, "printFrench": printFrench , "french": line}
            sys.stdout.write(json_output_string(data) + '\n')
            sys.stdout.flush()
            printFrench = 0
        
    sys.exit(0)


if __name__ == "__main__" :
    sys.stdout.write('\n')
    sys.stdout.flush()
    main()