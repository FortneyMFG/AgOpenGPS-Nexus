# 83 — Autosteer Target Models (Status: drafting requirements)

## Problem statement
Guidance planners must hand deterministic steer targets to the Autosteer plugin across a range of controller models. Nexus needs to formalize which controllers ship, how they consume fused pose/implement data, and how fallback strategies behave when latency or noise threatens stability.

## Requirements (from contributors)
- R-AUTO-000 (MUST, controller menu): Ship Pure Pursuit as the default controller and expose Stanley and MPC when their dependencies (pose fusion accuracy, hardware loops) are satisfied.
- R-AUTO-001 (MUST, publication): Emit `SteerTargets` at 25 Hz (± 5 ms jitter) with optional `speed_cap_mps` and `row_bias_m` inputs honored by each controller.
- R-AUTO-002 (MUST, pose inputs): Support fused pose streams at ≥ 50 Hz and row/implement sensors when available, rejecting stale inputs beyond 100 ms.
- R-AUTO-003 (SHOULD, delay compensation): Provide controller-specific latency and steer-rate compensation (feed-forward or predictive) when operator hardware supplies limits.
- R-AUTO-004 (SHOULD, tuning): Persist controller gain profiles per equipment profile and surface tuning parameters through the planner/Autosteer UI per ADR-033.
- R-AUTO-005 (MUST, fallback parity): Ensure fallback controller retains path tracking within ± 0.2 m cross-track error and ± 1° heading error during planner switchover.
- R-AUTO-006 (COULD, diagnostics): Publish controller health metrics (cross-track error, heading error, applied steering) to telemetry for replay and QA dashboards.

## Options
- O-AUTO-PP: Pure Pursuit baseline — geometric controller with configurable look-ahead distance.
- O-AUTO-ST: Stanley feedback — heading plus cross-track feedback controller tuned for moderate latency.
- O-AUTO-MPC: Model predictive control — predictive solver modeling steer-rate and implement dynamics.

### Option families & decision ordering
| Family ID | Type | Options | Decides before | Notes |
|---|---|---|---|---|
| DS-AUTO-BASE | Exclusive | O-AUTO-PP, O-AUTO-ST, O-AUTO-MPC | DS-AUTO-ENHANCE | Selects the primary controller shipped by default and supported in baseline hardware bundles. |
| DS-AUTO-ENHANCE | Composable | O-AUTO-ST, O-AUTO-MPC | — | Determines which advanced controllers the Autosteer plugin exposes when dependencies are met. |

## Comparison (quick matrix)
| Option | Pros | Cons | Risks | Borrow from existing |
|---|---|---|---|---|
| O-AUTO-PP | Simple, deterministic, low compute; proven fallback path. | Limited delay compensation; struggles with aggressive headland turns. | Overly conservative tuning may drift at high speed. | AgOpenGPS V6 baseline controller. |
| O-AUTO-ST | Handles heading error better at moderate speeds; minimal compute overhead. | Requires precise pose fusion; gain tuning sensitive to noise. | Incorrect gain can induce oscillation on slippery soil. | Open-source Stanley implementations in robotics stacks. |
| O-AUTO-MPC | Predictive, handles actuator limits and multi-body geometry. | Highest compute and tuning complexity; depends on solver health. | Solver failure could stall targets without resilient fallback. | ADR-033 MPC prototypes and autonomy research pilots. |

## Decision matrices
- [ ] DS-AUTO-BASE decision matrix drafted
- [ ] DS-AUTO-ENHANCE decision matrix drafted

## Evaluation criteria
- Maintain steer target jitter within ± 5 ms at 25 Hz across controllers.
- Uphold cross-track error ≤ 0.2 m and heading error ≤ 1° in beta regression runs.
- Support offline tuning workflows and configuration persistence per equipment profile.
- Keep fallback transitions seamless during planner refreshes and latency spikes.
- Ensure telemetry coverage for controller comparison without saturating storage.

## Current sentiment
- Pure Pursuit remains the required baseline for deterministic recovery flows.
- Stanley is the next candidate for default enablement once pose fusion noise targets are met.
- MPC is promising for advanced implements but requires additional validation around solver performance on CM5-class hardware.

## Open questions
- Q-AUTO-001: What minimum hardware (CPU, IMU rate) is required to support MPC in-field without throttling other services?
- Q-AUTO-002: How should controller selection interact with job profiles and automated tuning presets?
- Q-AUTO-003: Which telemetry metrics best differentiate controller performance for field trials?
