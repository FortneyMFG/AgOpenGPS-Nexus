# ADR-017: Equipment profiles and kinematics

## Status
Drafting (target review window: 2025-12-05 week)

**Relevant Plugin(s):** Autosteer, Guidance Planner, Mapping, Section Control


## Context
Accurate guidance and control require kinematic models that describe tractor, implement, and hitch behavior. Legacy profiles provide limited geometry, leading to inconsistent PoseStream projections and autosteer hand-offs. ADR-017 defines the profile schema, kinematic models, and sensor fusion expectations so ADR-033 guidance and ADR-008 hierarchy share a consistent foundation.

## Decision
- Establish profile schemas capturing hitch linkages, attachment points, toolbar placement, and sensor locations for multi-steer rigs.
- Provide kinematic models and simulation utilities that translate PoseStream inputs into steering commands and lookahead points.
- Define fusion strategies for multiple pose sources (IMU, GNSS, implement sensors) with convergence expectations and oscillation limits.
- Deliver an operator-facing profile editor with validation logic and deterministic JSON exports for configuration management.

## Consequences
- Guidance planner (ADR-033) and control systems gain reliable geometry data, improving accuracy and stability.
- Maintaining detailed profiles increases setup effort but enables richer simulation, diagnostics, and analytics.
- Sensor fusion introduces complexity that demands regression fixtures and cross-platform validation.

## Governance Updates
- **Calibration data exchange.** Field calibration sessions emit signed bundles (raw logs, solved parameters, environmental notes) stored alongside profile versions. Bundles must be replayable in simulation before publishing.
- **Correlation testing.** Simulation outputs are compared against hardware logs for each profile update. Drift beyond tolerance blocks release and spins follow-up tasks.
- **Lifecycle tracking.** Profiles carry effective dates and deprecation notices so operators can schedule recalibration windows proactively.

## Amendment — 2025 architecture refresh (NX-190)

- Profiles now capture per-session snapshots referenced by [ADR-041](ADR-041_JobSessions.md). When a session starts, Core records the active profile version and calibration bundle ID so replay, profit, and genetics analytics can correlate machine state with agronomic outcomes.
- Device Manager surfaces session-linked profile history, allowing operators to inspect configuration changes between sessions without trawling raw files.
- Multi-machine telemetry mesh (ADR-047) distributes profile hashes as part of presence broadcasts so collaborating rigs can confirm they share compatible geometry before exchanging coverage.

## Validation
- Kinematic simulations must track hitch articulation within ≤ 2 cm error over 100 m paths compared to motion capture baselines.
- Multi-steer fusion must converge within five cycles after switching pose sources while avoiding > 1° yaw oscillations.
- Profile editor must enforce attachment constraints and export deterministic JSON validated via schema conformance tests.

## References
<<<<<<< HEAD
- [Interprocess API requirements](../SRS/sections/4X_Interprocess_Communications/41_Inter_Application_API.md)
- [Control & automation requirements](../SRS/sections/6X_Core_Domain_Services/61_Kinematics_Pose_Fusion.md)
=======
- [Interprocess API requirements](../SRS/sections/4X/41_Inter_Application_API.md)
- [Control & automation requirements](../SRS/sections/6X/61_Kinematics_Pose_Fusion.md)
>>>>>>> origin/develop
- [ADR-008: Equipment hierarchy](ADR-008-equipment-hierarchy.md)
- [ADR-033: Guidance planner and autosteer orchestration](ADR-033-guidance-planner-autosteer.md)
