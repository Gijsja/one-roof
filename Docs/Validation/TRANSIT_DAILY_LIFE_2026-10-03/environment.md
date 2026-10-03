# Validation environment

2026-10-03: Intel Core i5-4570@3.20GHz,4 cores/4 threads;15GiB RAM;Linux Mint22.3;Unity6000.3.24f1; NVIDIA GeForce GT755M (GK107M),1GiB VRAM,driver470.256.02;OpenGL4.6,NVIDIA direct rendering on display:0 confirmed by `glxinfo -B` outside sandbox. Sandbox cannot access display/license IPC, so isolated Unity/native validations use reviewed escalation. Native resolution/quality/framecap/VSync and actual viewport must be captured per run; this environment inventory alone does not prove performance.
