# 🧵 Unity Cloth Simulation – In Progress

This is a custom cloth simulation built in Unity as part of my technical portfolio. The goal is to develop a system that reacts to forces (like wind and gravity), anchors, and collisions, using code-driven physics rather than relying solely on Unity’s built-in cloth component.

---

 ## 📌 Project Overview

-This cloth simulation project was developed in Unity as a way to explore real-time physics-based animation through direct implementation. Rather than relying on Unity’s built-in cloth components, I built the system from the ground up to better understand the underlying math and structure used in real-time simulations.

-The solver currently uses a simple **Verlet integration** model with a **spring-mass system**. The constraint formulation and iterative correction approach were inspired by **“Large Steps in Cloth Simulation”** by **David Baraff and Andrew Witkin** (SIGGRAPH 1998). My focus has been on achieving stable, believable motion at interactive framerates — particularly relevant in games.

---

## ✅ Current Progress

- [x] Basic particle system for cloth points (mass + velocity)
- [x] Structural spring constraints between particles
- [x] Gravity and wind force integration
- [x] Simple anchor support (e.g. pinning corners)
- [ ] Self-collision / object collision
- [ ] GPU optimization / compute shader pass (planned)

> 👀 Visuals coming soon: working on GIFs and screenshots as I refine the look and feel

---

## 🧠 Challenges & Learnings

- Translating high-level research concepts (e.g., global vs. local constraints) into real-time Unity systems.
- Managing stability without excessive constraint iterations — exploring stiffness vs. speed.
- Debugging forces and constraints with minimal visual feedback.
- Exploring ways to expand the system toward **semi-implicit** solvers or **GPU-based** evaluation based on further research.

This project has deepened my appreciation for physically-based modeling and the trade-offs between performance, stability, and realism — particularly in interactive contexts like games.


---

## 📚 Research Grounding

- This project references multiple papers and techniques from computer graphics research. Key concepts and inspirations include:
  - **Baraff & Witkin (1998)** – Position-based constraint solving, step-size stability trade-offs, and efficient solvers for interactive cloth.
  - **Provot (1995)** – Early position-based dynamics and distance constraint satisfaction.
  - **Jakobsen (2001)** – Use of Verlet integration and constraint enforcement in simple game physics, popularized by *Hitman: Codename 47*.

While this implementation does not yet fully replicate the more advanced implicit solvers or collision models described in those works, the goal has been to ground all development in real literature and build intuition from foundational principles.

---

## 🔜 Next Steps

- Add support for mesh skinning (connect simulation to visual mesh)
- Integrate with Unity colliders for environment interaction
- Polish visual output for portfolio presentation
- Optional: Explore GPU-based simulation with compute shaders for large-scale cloth

---

## 📎 Related Skills Demonstrated

- C# physics implementation
- Unity custom systems (non-MonoBehaviour)
- Real-time simulation debugging and performance tuning

---

## 👋 About Me

I'm a game developer with a CS background and a focus on systems design, shaders, and worldbuilding. This cloth sim is part of a broader technical portfolio showcasing my work in Unity and real-time interactivity.
