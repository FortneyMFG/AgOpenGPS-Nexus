# 81 — Guidance Orchestrator
*(Status: Proposed)*

**Author:** Codex
**Created:** 2025-10-20
**Version:** 0.1.0
**Section ID:** 81
**Editors:** Guidance Working Group
**Last Updated:** 2025-10-20
**Related Sections:** 82 — Planning, 83 — Autosteer Target Models
**Upstream Dependencies:** 2X — System Architecture, 4X — Interprocess Communications, ADR-033, ADR-069
**Downstream Impacts:** 6X — Core Domain Services, 9X — Frontends & Ops, Guidance plugins

---

## 81.1 Purpose & Scope

Deliver a Nexus-native guidance workflow that converts live driving into dependable field boundaries, coordinates planner refreshes, and streams deterministic targets to Autosteer while honoring spatial constraints and implement state.
This section defines the orchestration behaviors, UX responsibilities, and contract expectations that frame Guidance plugin development and verification.

---

## 81.2 Context

- Legacy stacks require manual boundary imports and refresh flows that ignore keep-outs or dynamic implement width.
- PoseStream provides fused ENU pose at ≥ 50 Hz; Sections publishes section states at ≥ 4 Hz, enabling live implement width tracking.
- Fields2Cover (F2C) runs as a local gRPC sidecar with CPU isolation and ≤ 200 ms median response for ≤ 65 ha fields.
- Autosteer consumes `SteerTargets` at 20–50 Hz (25 Hz default) with engagement lifecycle hooks defined in ADR-033.
- Mapping UI supplies gestures and editing primitives aligned with ADR-044 zone tooling.

---

## 81.3 Legacy Comparison

| Area / Theme | Legacy Behavior | Identified Limitation | Modernization Opportunity | Reference / Source |
|---------------|-----------------|------------------------|---------------------------|--------------------|
| Boundary Capture | Manual polygon imports or offline edits. | No live validation; operator friction in-cab. | Automated live field builder with validation prompts. | Legacy AOG workflow |
| Keep-out Handling | Treated as optional layers without planner enforcement. | Planner ignores obstacles; risk of collisions. | Promote keep-outs to first-class plan inputs with catalog metadata. | ADR-033 backlog |
| Planner Refresh | Manual replan triggers with long pauses. | Autosteer stalls while planner recomputes. | Quick Refresh pipeline that continues publishing existing targets. | Guidance pilot notes |
| Implement Width | Static width configured per implement. | No hysteresis; width drift causes UI churn. | Dynamic effective width from Sections plugin with hysteresis thresholds. | Sections plugin design |

---

## 81.4 Definitions

| Term | Definition |
|------|-------------|
| Live Field Builder | Module that detects loops in driven tracks and proposes field boundaries. |
| Effective Width (`W_eff`) | Active implement width computed from section spans and hysteresis thresholds. |
| Catalog | Structured collection of headland loops, swaths, and connectors persisted for execution. |
| Quick Refresh | Operator or automatic trigger that re-runs planning with current geometry and implement state. |
| Equivalence Policy | Hausdorff and heading tolerances that determine whether a refreshed path matches the active path. |

---

> **Requirement Grammar (RFC-2119):**
> - **MUST / MUST NOT** = mandatory requirements with verification evidence.
> - **SHOULD / SHOULD NOT** = strong recommendations; document exceptions.
> - **MAY / COULD** = optional behaviors or roadmap items.

## 81.5 Requirements

### 81.5.1 Live Field Builder & Keep-Outs

| ID | Priority | Summary | Verification |
|----|----------|---------|--------------|
| R-GO-LFB-000 | MUST | Detect closed loops in driven tracks; prompt operator to accept polygons ≥ 0.25 ha with validity checks. | Loop-detection regression suite; operator prompt telemetry. |
| R-GO-LFB-001 | MUST | Support keep-out polygon creation via gestures and tracing; treat accepted polygons as catalog holes. | UI integration tests; coverage overlay validation. |
| R-GO-LFB-002 | SHOULD | Maintain boundary revision identifiers and emit change events for planner caches keyed by `(rev, width bucket, settings hash)`. | Planner cache hit-rate metrics. |
| R-GO-LFB-003 | SHOULD | Apply configurable simplification (default 0.1 m) to reduce noise while preserving headland fidelity. | Geometry unit tests. |
| R-GO-LFB-004 | SHOULD | Prefer the largest simple loop as primary boundary during first lap; offer remaining loops as keep-outs. | Pilot workflow observations. |
| R-GO-LFB-005 | MUST | Reject polygons with > 50k vertices or > 32 holes; prompt operator to simplify before planning. | Input validation tests. |
| R-GO-LFB-006 | SHOULD | Support multiple disjoint outer rings via independent planning or operator-assisted field splits. | Multi-component field scenarios. |

### 81.5.2 Effective Width & Implement State

| ID | Priority | Summary | Verification |
|----|----------|---------|--------------|
| R-GO-IMP-000 | MUST | Compute `W_eff` from section spans and publish totals, active spans, and lateral centroid ≥ 4 Hz. | Integration tests with Sections plugin. |
| R-GO-IMP-001 | MUST | Apply hysteresis so ≥ 10% change prompts Quick Refresh and ≥ 20% change forces replans. | HIL tests capturing width deltas. |
| R-GO-IMP-002 | SHOULD | Support dynamic spacing (swath spacing = `W_eff`) and fixed spacing with operator defaults. | Planner configuration coverage. |

### 81.5.3 Planner Integration (Fields2Cover)

| ID | Priority | Summary | Verification |
|----|----------|---------|--------------|
| R-GO-PLAN-000 | MUST | Convert `{boundary, holes, W_eff, implement profile, headland settings, orientation}` into F2C requests and surface errors within 1 s. | Planner integration tests. |
| R-GO-PLAN-001 | MUST | Cache F2C responses keyed by `(field.rev, width bucket, settings hash)`; reuse unless inputs change beyond hysteresis thresholds. | Cache telemetry; replay tests. |
| R-GO-PLAN-002 | MUST | Provide fallback AB-line offsets when F2C times out ≥ 500 ms or errors; label `plan_source=fallback`. | Failure-injection tests. |
| R-GO-PLAN-003 | SHOULD | Support partial replans when new keep-outs appear ≥ 30 m from active path; otherwise perform full replan. | Scenario regression suite. |
| R-GO-PLAN-004 | SHOULD | Persist last three committed catalogs per `field.rev` under configurable disk root and restore on restart. | Persistence tests. |
| R-GO-PLAN-005 | SHOULD | Exclude pockets with legal area < `2 × W_eff` or bottlenecks < `W_eff`, marking them `unworkable`. | Planner QA metrics. |
| R-GO-PLAN-006 | MUST | Compose deterministic cache key `SHA256(field.rev || holes.rev || width_bucket || settings_hash || orientation_source || round(bearing_rad, 1e-4))`. | Hash consistency checks. |

### 81.5.4 Path Catalog & Execution

| ID | Priority | Summary | Verification |
|----|----------|---------|--------------|
| R-GO-EXEC-000 | MUST | Store headlands, swaths, connectors with metadata accessible to UI and automation controllers. | Catalog schema tests. |
| R-GO-EXEC-001 | MUST | Allow operators to lock onto catalog paths and publish preview curves / steer targets at 25 Hz. | UI automation; Autosteer telemetry. |
| R-GO-EXEC-002 | MUST | Track per-path progress (0..1) via pose projection and emit completion events. | Sequencer regression. |
| R-GO-EXEC-003 | SHOULD | Support serpentine or operator-defined sequencing with next/previous commands. | UX acceptance tests. |
| R-GO-EXEC-004 | SHOULD | Publish coverage polygons for active implement footprint to Coverage Writer. | Coverage overlay integration. |
| R-GO-EXEC-005 | MUST | Continue publishing existing targets during replans until new plan committed; maintain engagement if geometry equivalent. | Latency monitoring; equivalence policy tests. |
| R-GO-EXEC-006 | MUST | Treat paths as equivalent during hot swaps only when Hausdorff < 0.2 m and heading RMS < 0.5°. | Equivalence validation suite. |
| R-GO-EXEC-007 | MUST | Freeze active `path_id` on Autosteer disengage or manual override; require explicit resume or optional auto-resume timer. | HIL disengage tests. |
| R-GO-EXEC-008 | SHOULD | Publish `speed_cap_mps` when curvature exceeds limits so UI can prompt speed reductions. | Telemetry inspection. |
| R-GO-EXEC-009 | COULD | Accept optional row/implement sensor bias offsets within configured bounds without changing `path_id`. | Sensor fusion experiments. |

### 81.5.5 Refresh Policies & Operator Experience

| ID | Priority | Summary | Verification |
|----|----------|---------|--------------|
| R-GO-REFRESH-000 | MUST | Provide “Quick Refresh” control acknowledging commands ≤ 250 ms while maintaining steer targets. | UI latency measurement. |
| R-GO-REFRESH-001 | MUST | Auto-trigger planning when geometry or orientation changes; require confirmation for minor width deltas below hysteresis. | Planner trigger logs. |
| R-GO-REFRESH-002 | SHOULD | Support optional auto-refresh after headland `N` completes per job/session. | Configuration tests. |
| R-GO-UX-000 | MUST | Surface status pills (Recording, Field Established, Planning, Ready, Executing) and Quick Refresh availability. | UX checklist. |
| R-GO-UX-001 | MUST | Display prompts for detected loops and major width changes. | UI acceptance tests. |
| R-GO-UX-002 | SHOULD | Provide controls for headland, swath spacing, orientation, and implement parameters. | UX review. |
| R-GO-UX-003 | COULD | Offer map overlays previewing planned swaths/headlands with color coding. | Visual QA. |
| R-GO-UX-004 | SHOULD | Surface orientation provenance and offer “Adopt current heading” shortcut. | UX tests. |
| R-GO-UX-005 | MUST | Display speed limits derived from `speed_cap_mps` in localized units. | Localization tests. |

---

## 81.6 Interfaces & Data Contracts

- `Polygon`, `TrackPoint`, `EffectiveWidth`, `Implement`, `F2CRequest`, `Path`, `Catalog`, and `SteerTargets` follow ADR-069 structures, including IDs, frames, units, revisions, timestamps, and metadata hooks (`origin_llh`, `enu_epoch`, `orientation_reason`).
- JSON Schemas use draft 2020-12 with `$id` prefix `aog://schemas/guidance-orchestrator/...`.
- Planner requests/responses include timestamps, revision identifiers, `plan_source`, and planner provenance (Fields2Cover version & license ID).
- Implement metadata references the session’s equipment profile identifier for traceability.
- Catalog metadata captures `plan_id`, `field_rev`, orientation source, bearing, `orientation_reason`, and retention markers (`kept`, `evicted`).
- `SteerTargets` include optional `speed_cap_mps` and `row_bias_m`; downstream consumers ignore omitted fields and accept `_per_m` curvature properties alongside temporary `_1pm` aliases.
- Equivalence policy is published with catalog metadata, exposing Hausdorff and heading tolerances for automation and UI alignment.

---

## 81.7 Performance & Timing

- Loop detection and polygonization run ≥ 2 Hz while recording headlands with latency ≤ 150 ms per iteration for 10 km traces.
- Orchestrator core loop evaluates refresh triggers and builds preview bundles at 4 Hz with ≤ 50 ms per cycle.
- `SteerTargets` publish at 25 Hz with ±5 ms jitter to protect Autosteer stability budgets from ADR-033.
- Fields2Cover plans meet p50 latency ≤ 120 ms and p95 latency ≤ 200 ms for ≤ 65 ha polygons with ≤ 2 holes; AB fallback engages within 20 ms of timeout.
- Operator overrides do not introduce gaps in `SteerTargets`; frozen paths continue publishing at 25 Hz until resume or relock.

---

## 81.8 Verification & Validation

- Simulated 65 ha fields with one keep-out yield valid plans (no self-intersections, coverage ≥ 98%) with plan latency p50 ≤ 120 ms and p95 ≤ 200 ms.
- Hardware-in-loop tests confirm Quick Refresh end-to-end latency ≤ 1.5 s with cached responses and zero gaps in `SteerTargets` at 25 Hz during replans.
- Regression suites cover loop-detection traces across noise levels, ensuring false-positive rate < 2% when noise σ ≤ 0.15 m and polygon auto-repair success ≥ 99.9%.
- Stability testing demonstrates pass flip rate < 0.05 flips/min under nominal noise with hysteresis features enabled.
- Golden scenarios (ADR-069 T01–T07) record latency, coverage, fallback usage, and override behavior with `planner_seed` and `sequencer_seed` for deterministic reruns.

---

## 81.9 Design Considerations

| ID | Consideration | Description |
|----|----------------|-------------|
| C1 | Live boundary UX | Prioritize in-cab prompts and preview overlays so operators trust automated loop detection. |
| C2 | Planner continuity | Maintain steer-target publication during replans to avoid Autosteer dropouts. |
| C3 | Keep-out governance | Enforce obstacle metadata as first-class plan inputs with revision tracking. |
| C4 | Operator overrides | Provide predictable manual override handling with explicit resume pathways. |
| C5 | Telemetry completeness | Capture latency, plan source, and seed metadata to support diagnostics and replay. |

### 81.9.1 Assumptions & Preconditions

- PoseStream, Sections plugin, and Autosteer publish at documented cadences.
- Fields2Cover sidecar remains reachable within latency budgets; fallback AB mode stays validated.
- Guidance UI surfaces required prompts and orientation controls defined in ADR-069.

---

## 81.10 Open Questions

- Should Fields2Cover connectors persist for re-entry after manual detours or regenerate on demand per transition?
- How should multi-implement rigs expose composite effective width without overcomplicating operator controls?
- What telemetry subset enables remote monitoring of planning events while respecting offline privacy constraints?

---

## 81.11 References

- [ADR-033 — Guidance planner and autosteer orchestration](81-ADR-033%20-%20Guidance%20planner%20and%20autosteer%20orchestration.md)
- [ADR-069 — Guidance Orchestrator plugin](81-ADR-069%20-%20Guidance%20Orchestrator%20plugin.md)
- [ADR-027 — Spatial constraints and zone policies](../ADR/ADR-027-spatial-constraints.md)
- [ADR-044 — Zone & layer drawing framework](../ADR/ADR-044_ZoneDrawingFramework.md)
- [How-to: guidance lane contracts](../../../../Core/howto/guidance-lane-contracts.md)
