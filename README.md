# 🌊 Hydrodynamics SPH Simulator

<img width="1916" height="887" alt="image" src="https://github.com/user-attachments/assets/cabff4a4-5e20-483f-8886-b570f5fc1743" />

A high-performance **Smoothed Particle Hydrodynamics (SPH)** fluid simulator implemented in **Unity**, capable of simulating 10,000 particles at **30 FPS** on modest hardware. Inspired by **Sebastian Lague**'s tutorials and the scientific work in [MDPI 2023: SPH Fluid Simulation](https://www.mdpi.com/2076-3417/15/17/9706).


https://github.com/user-attachments/assets/8f17600e-70de-4376-b236-670e71dcee76


---

## 🎯 Key Features

- **Real-time SPH simulation** of fluids with up to **10k particles**  
- **Density, pressure, viscosity** calculations per particle  
- **Spatial hashing** for efficient neighbor search  
- **GPU-optimized instanced rendering** with **velocity-based color mapping**  
- **Collision handling** with world bounds and arbitrary colliders  
- **Icosphere-based particles** for realistic visualization  

---

## 🧪 Scientific & Technical Overview

This project implements **SPH (Smoothed Particle Hydrodynamics)** with a focus on accuracy and performance:

1. **Particle System**
   - `FluidParticle` struct stores **position, velocity, acceleration, density, and pressure**.
   - Particles are integrated with **explicit Euler integration** each frame.

2. **Density & Pressure Calculation**
   - Uses **Poly6 kernel** for smooth density computation.
   - Pressure computed via **Equation of State**:  
     \[
     P_i = k \left( \left(\frac{\rho_i}{\rho_0}\right)^7 - 1 \right)
     \]
   - Ensures realistic fluid compressibility.

3. **Force Computation**
   - **Pressure forces** via **Spiky gradient kernel**.
   - **Viscosity forces** via **Viscosity Laplacian kernel**.
   - Includes **gravity** and optional velocity damping.

4. **Spatial Hashing**
   - Accelerates neighbor search using a **3D grid hash**:
     \[
     hash = (x \cdot 73856093) \oplus (y \cdot 19349663) \oplus (z \cdot 83492791)
     \]
   - Only checks nearby cells, drastically reducing complexity from **O(n²) → O(n)**.

5. **Boundary Handling**
   - World bounds handled via simple position reflection.
   - Collider interaction uses **closest-point approximation** and reflective velocity.
   - Box collisions approximate normal along the smallest penetration axis.

6. **Visualization**
   - GPU **instanced rendering** for 10k particles at 30 FPS.
   - **Velocity-based coloring**:
     - Dark blue → Mint → Yellow → Red/Orange for speed mapping.
   - Mesh: dynamically generated **Icosphere** for smooth spheres.

---

## ⚙️ Performance

| Metric | Value |
|--------|-------|
| Particle Count | 10,000 |
| Frame Rate | 30 FPS |
| GPU | GTX 960 |
| CPU | i5-12600KF |
| RAM | 16 GB |

> Achieves smooth simulation on mid-range hardware thanks to **instanced rendering** and **spatial hashing**.

---

## 🛠️ Implementation Highlights

- **Unity C#** for simulation & rendering
- **Custom Shader** for particle colors and lighting
- **MaterialPropertyBlock batching** for efficient GPU uploads
- **Kernel functions** implemented directly in C# for easy modification:
  - Poly6, Spiky gradient, Viscosity Laplacian

---

## 📽️ Media

- Simulation video placeholder:  
  `![Video Placeholder](path-to-your-video.gif)`  

- Screenshot placeholder:  
  `![Screenshot](path-to-your-screenshot.png)`

---

## 📚 References & Inspiration

- Inspired by [Sebastian Lague SPH tutorials](https://www.youtube.com/c/SebastianLague)  
- Research paper: [MDPI Applied Sciences, 2023](https://www.mdpi.com/2076-3417/15/17/9706)  

---

## 🚀 Getting Started

1. Clone the repository:  
```bash
git clone https://github.com/yourusername/hydrodynamics-sph.git
Open in Unity 2021.3+

Assign particleMaterial and collision layers in HydrodynamicsManager.

Hit Play to run simulation.
