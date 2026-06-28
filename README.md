# AmySign
## Whisper
Dans le cadre de ce projet, l'implémentation C++/C de Whisper par OpenAI, Whisper.cpp par ggml-org sera utilisé pour raison de performance.

Whisper.cpp est compilé pour OpenVINO (CPU), Vulkan (iGPU) et NVIDIA CUDA (dGPU)

Machine de test initial : Fedora 44 KDE, Intel Core i7 10th Gen, NVIDIA RTX 2060 Mobile, 32GB RAM

Machine de developpement : Windows 11 Pro, Intel Core i7 10th Gen, NVIDIA RTX 2060 Mobile, 32GB RAM

Model stt : large-v3-turbo

Résultat : latence trop important sur OpenVINO et Vulkan pour traitement en temps réel

### DEV history
20/05/2026 
- First attempt -- Adding shared ThreadSafe buffer to whisper.cpp stream example

23/05/2026 
- Change of software architecture implementing producer consumer algorithm from C++ to Python
- Whisper model is changed to large-v3-q5_0 for higher accuracy while keeping speed by reducing ressource usage
- Whisper-stream is now compiled to an executable and interact with Gloss converter using subprocess.Pop() stdout capture and threadsafe buffer

27/02/2026
- Build for Windows

23/06/2026
- Additional modifications : Clean up unused code sections, Add options
- Fix encoding mismatch for proper inter-process communication
