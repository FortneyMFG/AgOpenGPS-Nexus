# ADR-033: Guidance planner and autosteer orchestration

## Status
Drafting (target review window: 2025-11-21 week)

## Context
The Nexus guidance stack must reconcile legacy planners (AB, curve, turn) and controllers (Stanley, pure pursuit) with PoseStream, zone gating, and plugin extensibility. Existing implementations rely on legacy assumptions that do not honor ADR-027 zone policies or ADR-032 layer metadata. ADR-033 defines the canonical lane model, lookahead scheduling, and constraint handling to align autosteer firmware, UI, and plugins.

## Decision
- Standardize lane and turn templates (straight, curve, adaptive) with preview publishing and lookahead scheduling tied to PoseStream cadence.
- Integrate constraint and zone masks into guidance planning so outputs respect ADR-027 gating before reaching control arbitration.
- Port legacy controllers into Nexus with deterministic fixtures, ensuring closed-loop behavior meets stability targets.
- Define plugin hooks for lane publishing, telemetry, and degraded mode messaging, coordinating with ADR-018 capability discovery.

## Consequences
- Guidance outputs become deterministic and compatible with new zone policies, improving operator trust and safety.
- Porting and refactoring legacy planners introduces significant testing burden but unlocks integration with plugins and telemetry.
- Autosteer firmware must adapt to new orchestration signals, requiring validation on hardware benches.

## Governance Updates
- **Validation ladder.** Guidance programs progress through simulation-only, hardware-in-the-loop, and field pilot stages with documented exit criteria tied to spatial constraint metrics.
- **Fallback behaviors.** Automation loss triggers documented manual handover cues (audio, HUD banners) consistent across products. Playbooks include operator drills prior to release.
- **Telemetry capture.** Each validation stage records telemetry and incident reports which feed back into guidance tuning cycles.

## Validation
- Guidance regression suite must achieve ≤ 4 cm lateral RMS error versus legacy traces across AB, curve, and adaptive headland fixtures.
- Constraint fault-injection tests must force autosteer disengagement within 150 ms of zone mask conflicts while logging controlling constraints.
- Firmware loop-in-the-loop benches must demonstrate steady-state steering error ≤ 2° at 15 km/h using recorded PoseStream inputs.

## References
- [Control & automation requirements](../SRS/sections/09_Control_Automation.md)
- [Interprocess API requirements](../SRS/sections/07_Interprocess_API.md)
- [Extensibility & plugin requirements](../SRS/sections/12_Extensibility_Plugins.md)
- [ADR-027: Spatial constraints and zone policies](ADR-027-spatial-constraints.md)
- [ADR-032: Layer controllers and aggregation runtime](ADR-032-presets-and-layout-linking.md)
- [ADR-017: Equipment profiles and kinematics](ADR-017-profiles-kinematics.md)
