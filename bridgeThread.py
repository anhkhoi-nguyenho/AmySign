import queue
import threading
import time
import subprocess

# Tampon partagé
transcript_queue = queue.Queue(maxsize=1000)  # Limite pour éviter explosion mémoire

# Exemple Producteur (Whisper)
def whisper_producer():
    command = "./build/bin/whisper-stream"
    process = subprocess.Popen(command, shell=True, stdout=subprocess.PIPE, text=True)

    try :
        for line in process.stdout:
            output = line.strip()
            transcript_queue.put(output)
    except KeyboardInterrupt:
        print("Stopping")
    finally:
        process.terminate()
        process.wait()

# Consommateur
def analyzer_consumer():
    while True:  # ou condition d'arrêt
        try:
            item = transcript_queue.get(timeout=1.0)  # Bloque un peu si vide
            print("[Consommateur]",item)
            transcript_queue.task_done()
        except queue.Empty:
            continue  # ou sleep léger

# Lancement
if __name__ == "__main__":
    prod_thread = threading.Thread(target=whisper_producer, daemon=True)
    cons_thread = threading.Thread(target=analyzer_consumer, daemon=True)

    prod_thread.start()
    cons_thread.start()

    # Main thread : garde le programme vivant ou gère UI/websocket etc.
    try:
        while True:
            time.sleep(1)
    except KeyboardInterrupt:
        print("\nArrêt...")
