import sys
from AmySign_code.glosses import pipeline
import io
# spaCy finished loading
sys.stderr.write("READY\n")
sys.stderr.flush()
sys.stdin = io.TextIOWrapper(sys.stdin.detach(), encoding='utf-8-sig')

# 1. Force Python to output stdout and stderr in UTF-8
sys.stdout.reconfigure(encoding='utf-8')
sys.stderr.reconfigure(encoding='utf-8')

def main():

    # When the pipe breaks, this loop ends automatically
    for line in iter(sys.stdin.readline, ''):
        clean_line = line.strip()
        # if clean_line == "é":
            # sys.stderr.write("[Python] : I got é\n")
        sys.stderr.write("Python reads: " + clean_line + '\n')
        if clean_line == "fffa09deb551437381d7567274bae72e":
            break
        
        etat = pipeline(clean_line)

        for gloss in etat.glosses_finales :
            sys.stdout.write(gloss + '\n')
            sys.stdout.flush()
            
    # The loop broke (either by "quit" or Unity dying). 
    sys.stdout.write("Standard input closed. Exiting.\n")
    sys.stdout.flush()
    sys.exit(0) # Explicitly terminate the script

if __name__ == "__main__":
    main()