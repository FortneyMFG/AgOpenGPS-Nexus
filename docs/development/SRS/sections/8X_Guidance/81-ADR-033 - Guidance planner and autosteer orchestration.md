# 81-ADR-033 — Guidance Planner & Autosteer Orchestration

*(Status: Proposed)*

**Authors:** Codex
**Reviewers:** Guidance WG, Autonomy WG, UI WG
**Created:** 2025-10-20
**Last Updated:** 2025-10-20
**Supersedes:** —
**Superseded by:** —
**Related SRS:** `81_Guidance_Orchestrator.md`, `82_Planning.md`, `83_Autosteer_Target_Models.md`
**Related Options:** —

---

## 1) Context

Legacy AgOpenGPS guidance flows rely on disparate planners (AB, curve, turn) and controllers (Stanley, Pure Pursuit) that predate ADR-027 zone policies and ADR-068 layer metadata.
PoseStream integration, zone gating, and plugin extensibility require a unified orchestration model covering lane templates, lookahead scheduling, and constraint handling across Core, Autosteer, and UI surfaces.
This ADR consolidates expectations for deterministic planning, constraint enforcement, and plugin hand-offs.

---

## 2) Decision

Standardize the Nexus guidance stack around a shared orchestration contract that:

- Defines canonical lane and turn templates (straight, curve, adaptive) with preview publishing synchronized to PoseStream cadence.
- Integrates constraint and zone masks upstream so guidance outputs respect ADR-027 gating before reaching control arbitration.
- Ports legacy controllers under deterministic fixtures to meet stability targets while exposing telemetry for regression.
- Establishes plugin hooks for lane publishing, telemetry capture, degraded mode messaging, and capability discovery (ADR-018).

### Decision Summary

- **Scope:** Guidance planner coordination, Autosteer interface, and plugin messaging for Nexus Core deployments.
- **Boundary:** Hardware-specific steering actuations remain governed by subsystem ADRs.
- **Implementation Level:** Architecture + orchestration contract; drives planner, Autosteer, and UI workstreams.

---

## 3) Consequences

**Positive Impacts**

- Guidance outputs become deterministic and compatible with zone policies, increasing operator trust and safety.
- Shared telemetry enables reproducible regression suites and hardware-in-the-loop validation.
- Unified contracts reduce integration friction across plugins and UI.

**Negative / Mitigated Impacts**

- Porting and refactoring legacy planners requires substantial testing; mitigated by staged validation ladder.
- Autosteer firmware must adapt to new orchestration signals; mitigated via hardware bench schedules.
- Increased visibility of planner latency necessitates service-level objectives and monitoring.

**Follow-up Actions**

- Implement validation ladder spanning simulation, hardware-in-the-loop, and field pilots with exit criteria tied to spatial constraints.
- Document fallback behaviors and operator handover cues (audio, HUD banners) for degraded modes.
- Expand telemetry capture for planner and controller events feeding guidance tuning cycles.

---

## 4) Rationale

Decision matrices across SRS §§81–83 favor unified orchestration to maintain deterministic guidance while enabling extensibility.
Alternatives relying on legacy ad-hoc planners fail to satisfy zone policies, telemetry completeness, or plugin integration requirements.

---

## 5) Alternatives Considered

| Option | Summary | Reason Not Selected |
|--------|---------|---------------------|
| Legacy ad-hoc orchestration | Maintain existing planner/controller pairings without shared contracts. | Ignores ADR-027 constraints, lacks telemetry, and hinders plugin extensibility. |
| Standalone firmware coordination | Push orchestration entirely into hardware controllers. | Sacrifices UI insight, telemetry, and cross-plugin integration. |

---

## 6) Implementation Notes

- **Validation Ladder:** Progress from simulation-only to hardware-in-the-loop and field pilots with documented exit criteria tied to spatial constraint metrics.
- **Fallback Behaviors:** Automation loss triggers manual handover cues consistent across products; operator drills precede release.
- **Telemetry Capture:** Regression, planner, and controller telemetry feed tuning cycles and support tooling.

---

## 7) References

- [Control & automation requirements](../6X_Core_Domain_Services/61_Kinematics_Pose_Fusion.md)
- [Interprocess API requirements](../4X_Interprocess_Communications/41_Service_APIs_Contracts.md)
- [Extensibility & plugin requirements](../9X_Frontends_Ops/94_Extensibility_Packaging_Updates.md)
- [ADR-027 — Spatial constraints and zone policies](../ADR/ADR-027-spatial-constraints.md)
- [ADR-068 — Layer controllers and aggregation runtime](../ADR/ADR-068-layer-controllers-runtime.md)
- [ADR-017 — Equipment profiles and kinematics](../ADR/ADR-017-profiles-kinematics.md)
