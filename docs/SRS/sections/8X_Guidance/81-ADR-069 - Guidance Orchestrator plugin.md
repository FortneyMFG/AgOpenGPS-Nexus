# 81-ADR-069 — Guidance Orchestrator Plugin

*(Status: Proposed)*

**Authors:** Codex
**Reviewers:** Plugins WG, Autonomy WG, UI WG
**Created:** 2025-10-20
**Last Updated:** 2025-10-20
**Supersedes:** —
**Superseded by:** —
**Related SRS:** `81_Guidance_Orchestrator.md`, `82_Planning.md`
**Related Options:** —

---

## 1) Context

ADR-033 establishes the orchestration contract, but operators still rely on manual boundary imports or legacy planners that ignore keep-outs and dynamic implement width.
Fields2Cover (F2C) can provide high-quality coverage plans when supplied with live field geometry, implement state, and refresh policies without interrupting cab workflows.
This ADR captures the plugin responsibilities for turning the first headland lap into a boundary, managing keep-outs, coordinating planner refreshes, and streaming deterministic steering targets.

---

## 2) Decision

Adopt a Guidance Orchestrator plugin that:

- Builds live boundaries from PoseStream data, detects loops, and tracks revisions for downstream caching.
- Manages keep-outs, implement spans, and orientation provenance while coordinating F2C planner requests and AB fallbacks.
- Maintains a Quick Refresh workflow that continues publishing existing steer targets until new plans commit, enforcing hysteresis to avoid churn.
- Respects operator overrides by freezing active paths on disengage and providing explicit resume / auto-resume controls.
- Persists catalog history, plan metadata, and telemetry for diagnostics and replay scenarios.

### Decision Summary

- **Scope:** Guidance plugin runtime responsible for geometry capture, planner coordination, and execution streaming.
- **Boundary:** Planner solver implementations (Fields2Cover, AB fallback) are governed by SRS §82; Autosteer controller details live in SRS §83.
- **Implementation Level:** Product architecture + plugin design; directs UI, telemetry, and persistence workstreams.

---

## 3) Consequences

**Positive Impacts**

- Operators gain an end-to-end workflow for recording fields, marking obstacles, and engaging planned swaths without leaving the cab.
- Sections, Coverage Writer, and Autosteer remain synchronized because implement spans, catalog metadata, and steer targets originate from the same orchestrator.
- Planner latency and fallback behavior become observable through telemetry, aiding support and regression.

**Negative / Mitigated Impacts**

- Planner and catalog persistence introduce additional storage and validation requirements; mitigated via retention policies and disk budget enforcement.
- Quick Refresh makes planner SLA compliance highly visible; mitigated by caching and deterministic fallback.
- Catalog retention exposes plan history requiring support tooling updates for replay and resume flows.

**Follow-up Actions**

- Finalize schema definitions for geometry, catalog, and telemetry payloads with draft 2020-12 JSON Schema identifiers.
- Implement hysteresis tuning (width change thresholds, boundary debounce) and document operator messaging.
- Build regression coverage for loop detection, plan caching, and steer-target streaming at 25 Hz.

---

## 4) Rationale

The plugin-centric approach unifies geometry, planner coordination, and execution while honoring ADR-033 contracts.
It reduces manual boundary management, enforces keep-outs as first-class inputs, and maintains deterministic steering outputs even during replans.

---

## 5) Alternatives Considered

| Option | Summary | Reason Not Selected |
|--------|---------|---------------------|
| Maintain manual boundary imports | Continue relying on external tools for boundary creation. | Fails to deliver live workflow; ignores keep-outs and hysteresis policies. |
| Planner-only microservice | Offload orchestration to remote service. | Loses tight coupling with UI/Autosteer events; increases latency risk. |

---

## 6) Implementation Notes

- **Frames, Units, Rates:** World frame ENU meters; vehicle frame origin at rear axle midpoint; angles in radians; curvature in 1/m; planner loop 4 Hz; `SteerTargets` 25 Hz ±5 ms jitter; loop detection 2 Hz while recording.
- **Assumptions:** PoseStream delivers fused pose ≥ 50 Hz; Sections plugin publishes section states ≥ 4 Hz; F2C runs as local gRPC sidecar with CPU isolation; Autosteer consumes targets at 20–50 Hz.
- **Non-Goals:** Row-sensor or vision fusion control loops, ROS2 integration, implement-level hydraulic policy management.
- **Data Contracts:** `Polygon`, `TrackPoint`, `EffectiveWidth`, `F2CRequest`, `Path`, and `Catalog` include IDs, frames, units, revisions, timestamps, `origin_llh`, `enu_epoch`, and `orientation_reason` metadata.
- **Refresh Behavior:** Quick Refresh acknowledges operator input ≤ 250 ms and maintains existing steer targets until new plan commit; auto-refresh triggers on geometry/orientation changes with hysteresis-managed prompts.

---

## 7) References

- [ADR-033 — Guidance Planner & Autosteer Orchestration](81-ADR-033%20-%20Guidance%20planner%20and%20autosteer%20orchestration.md)
- [ADR-027 — Spatial Constraints & Zone Policies](../ADR/ADR-027-spatial-constraints.md)
- [ADR-018 — Capability Discovery](../ADR/ADR-018-capability-discovery.md)
- [Fields2Cover documentation](https://fields2cover.github.io/)
