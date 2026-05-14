# Speech2FSL
## Whisper
Dans le cadre de ce projet, l'implémentation C++/C de Whisper par OpenAI, Whisper.cpp par ggml-org sera utilisé pour raison de performance.

Whisper.cpp est compilé pour OpenVINO (CPU), Vulkan (iGPU) et NVIDIA CUDA (dGPU)

Test :

Machine cible : Fedora 44 KDE, Intel Core i7 10th Gen, NVIDIA RTX 2060 Mobile, 32GB RAM

Model stt : large-v3-turbo

Résultat : latence trop important sur OpenVINO et Vulkan pour traitement en temps réel