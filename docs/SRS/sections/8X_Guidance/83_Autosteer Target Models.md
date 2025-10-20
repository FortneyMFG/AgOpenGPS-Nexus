# 83 — Autosteer Target Models (Status: drafting)

## Overview
Autosteer target generation converts planner output (desired path and orientation) into real-time steering setpoints.  
Three primary controller families are supported or under evaluation in Nexus: **Pure Pursuit**, **Stanley**, and **Model Predictive Control (MPC)**.  
Each represents a progressively more dynamic approach to minimizing cross-track and heading error between the vehicle (or implement) and the desired path.

### 83.1 Pure Pursuit (Geometric)
**Principle:** Select a look-ahead point (`Ld`) ahead on the path and compute the curvature required to intersect it.  
**Formula:** `δ = atan((2 * L * sin(α)) / Ld)`  
Where `L` = wheelbase and `α` = heading to the look-ahead point.

**Characteristics:**
- Single geometric parameter `Ld` acts as the gain.
- Stable and smooth at low to moderate speeds.
- Ignores actuator delay and dynamic constraints.

**Integration:**
- Baseline controller for AgOpenGPS and other open systems.
- Runs at 10–50 Hz within the `SteerTarget` generator.
- Suitable for fallback and testing modes.

### 83.2 Stanley (Feedback)
**Principle:** Combine heading and lateral error feedback to drive steering correction.  
**Formula:** `δ = ψe + atan((k * ey) / v)`  
Where `ey` = cross-track error, `v` = vehicle speed, `k` = gain, and `ψe` = heading error.

**Characteristics:**
- Merges geometric and proportional control behavior.
- Reacts more effectively to curvature and slip than Pure Pursuit.
- Requires accurate heading data and noise-filtered pose input.

**Integration:**
- Drop-in feedback layer using pose fusion from GPS + IMU.
- Nominal update rate: 25–50 Hz.
- Computational cost: negligible.

### 83.3 Model Predictive Control (MPC)
**Principle:** Predict future system behavior over a short horizon using a simplified kinematic or dynamic model, then solve an optimization problem to minimize future cross-track, heading, and steering effort.  
**Optimization:** `min Σ[(x - xref)ᵀ Q (x - xref) + uᵀ R u] subject to xₖ₊₁ = A xₖ + B uₖ`

**Characteristics:**
- Predictive and constraint-aware; models latency, steer rate limits, and multi-body geometry (tractor + implement).
- Naturally fuses fast-rate IMU (≥100 Hz) and slower GPS/feeler updates.
- Smooth, anticipatory steering through curves and headlands.

**Integration:**
- Implemented as an advanced planner inside `Aog.Plugins.Autonomy`.
- Consumes fused pose and implement states from `PoseFusion`.
- Outputs steer-rate or steer-angle targets to AgIO.
- Typical internal loop rate: 50–100 Hz; publishes 10–25 Hz target stream.

### 83.4 Comparative Summary
| Controller | Type | Delay Compensation | Rate/Angle Limits | Implement Geometry | Row Feeler Support | Typical Rate | Compute Load |
|-------------|------|--------------------|------------------|--------------------|--------------------|---------------|---------------|
| Pure Pursuit | Geometric | No | No | No | External add-on | 10–50 Hz | Very Low |
| Stanley | Feedback | Partial | Partial | No | Partial (pose fusion only) | 25–50 Hz | Low |
| MPC | Predictive / Optimization | Yes | Yes | Yes | Native integration (direct fusion) | 50–100 Hz | Moderate |

### 83.5 Guidance Selection Policy
System SHALL allow controller selection via configuration profile.  
Default SHALL remain **Pure Pursuit** for deterministic fallback.  
**Stanley** and **MPC** MAY be selected when their corresponding plugins are present and pose fusion accuracy meets defined thresholds (see `81_Guidance_Orchestrator.md`).
