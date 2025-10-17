# Guidance Orchestrator Plugin (Status: drafting requirements)

## Problem statement
Operators need a Nexus-native guidance workflow that converts live driving into dependable field boundaries, plans efficient swaths with Fields2Cover, and streams deterministic targets to Autosteer while respecting spatial constraints and implement state. The current stack requires manual boundary imports and brittle refresh flows that do not honor keep-outs or dynamic implement width.

## Scope
This section captures requirements for the Guidance Orchestrator plugin responsible for live boundary construction, keep-out handling, planner coordination, and path execution. It complements ADR-033 (guidance planner & autosteer orchestration) and ADR-027 (spatial constraints) by defining the higher-level behaviors, operator UX, and data contracts needed to ship the Fields2Cover-backed planner.

## Assumptions & dependencies
- PoseStream delivers fused ENU pose (position, heading, speed) at ≥ 50 Hz with deterministic latency budgets established in ADR-033.
- Section state (On/Auto) is available from the Sections plugin at ≥ 4 Hz per ADR-027, enabling effective width calculations with hysteresis.
- Fields2Cover (F2C) runs as a local gRPC sidecar with CPU isolation and meets the SLA of ≤ 200 ms median response for ≤ 65 ha fields.
- Autosteer plugin consumes `SteerTargets` at 20–50 Hz (25 Hz default) and exposes engagement lifecycle events defined in ADR-033.
- Mapping UI supplies gestures (tap, long-press) and boundary editing primitives consistent with ADR-044.

## Requirements

### R-GO-LFB — Live field builder & keep-outs
- R-GO-LFB-000 (MUST): Detect closed loops in the driven track and prompt the operator to accept or reject candidate field boundaries when the polygon area ≥ 0.25 ha and passes validity checks (no self-intersections, correct winding).
- R-GO-LFB-001 (MUST): Allow operators to create keep-out polygons through map long-press/right-click gestures and by tracing around obstacles, producing polygons that become holes in the active field boundary.
- R-GO-LFB-002 (SHOULD): Maintain boundary revision identifiers and emit change events so downstream planners can cache by `(rev, effective width bucket, settings hash)`.
- R-GO-LFB-003 (SHOULD): Apply Douglas-Peucker or equivalent simplification with configurable tolerance (default 0.1 m) to reduce noise while preserving key vertices for headland loops.
- R-GO-LFB-004 (SHOULD): When multiple loops are detected during the first lap, prefer the largest simple loop as the boundary and present remaining loops as candidate keep-outs.
- R-GO-LFB-005 (MUST): Reject polygon submissions with > 50k vertices or > 32 holes and prompt the operator to simplify before planning to maintain planner SLA compliance.
- R-GO-LFB-006 (SHOULD): Support fields with multiple disjoint outer rings by planning each component independently or prompting the operator to split the field when sequencing would conflict.

### R-GO-IMP — Effective width & implement state
- R-GO-IMP-000 (MUST): Compute effective implement width (`W_eff`) from active section spans (On/Auto) and expose total width, active spans, and lateral centroid (vehicle frame) to planners at ≥ 4 Hz.
- R-GO-IMP-001 (MUST): Apply hysteresis so only ≥ 10% change in `W_eff` (or ≥ 0.3 m absolute) triggers Quick Refresh banners and ≥ 20% change forces automatic replans; smaller deltas raise advisory banners without recomputing plans.
- R-GO-IMP-002 (SHOULD): Support both dynamic spacing (swath spacing = `W_eff`) and fixed spacing (swath spacing = nominal width) with operator-selected default per job profile.

### R-GO-PLAN — Planner integration (Fields2Cover)
- R-GO-PLAN-000 (MUST): Convert `{boundary, holes, W_eff, implement profile, headland settings, orientation}` into F2C plan requests and surface planner errors/fallbacks to the operator within 1 s.
- R-GO-PLAN-001 (MUST): Cache successful F2C responses keyed by `(field.rev, width bucket, settings hash)` and reuse them unless inputs change beyond hysteresis thresholds (boundary debounce 500 ms, keep-out 250 ms).
- R-GO-PLAN-002 (MUST): Provide fallback AB-line offset generation when F2C times out (≥ 500 ms) or returns an error, preserving operator control continuity and flagging plan source as `fallback`.
- R-GO-PLAN-003 (SHOULD): Support partial replans that preserve unworked swaths/headlands when new keep-outs are added ≥ 30 m away from the current active path and catalog family order remains unchanged; otherwise perform a full replan to maintain sequencing integrity.
- R-GO-PLAN-004 (SHOULD): Persist the last three committed catalogs per `field.rev` under `~/.nexus/guidance/plans/<fieldId>/<plan_id>.json` (configurable root with disk budget enforcement) and restore the most recent on plugin restart to avoid unnecessary replanning.
- R-GO-PLAN-005 (SHOULD): Exclude pockets with legal area < `2 × W_eff` or width bottlenecks < `W_eff`, marking them `unworkable`, avoiding automatic bridging attempts, and surfacing the exclusion to the operator.
- R-GO-PLAN-006 (MUST): Compose `plan.key` for caching as `SHA256(field.rev || holes.rev || width_bucket || settings_hash || orientation_source || round(bearing_rad, 1e-4))` so deterministic retries reuse identical plans.

### R-GO-EXEC — Path catalog & execution
- R-GO-EXEC-000 (MUST): Store headland loops, interior swaths, and optional connectors in a path catalog with metadata (`id`, `family`, length, curvature, coverage) accessible to UI and automation controllers.
- R-GO-EXEC-001 (MUST): Allow operators to lock onto any catalog path via map tap or list selection and stream preview curves / steer targets at 25 Hz to Autosteer.
- R-GO-EXEC-002 (MUST): Track per-path progress (0..1) using pose projection onto the active spline and emit completion events for sequencing.
- R-GO-EXEC-003 (SHOULD): Support serpentine or operator-defined sequencing, exposing next/previous path commands in the UI and via plugin API.
- R-GO-EXEC-004 (SHOULD): Publish coverage polygons for the active implement footprint to the Coverage Writer so worked area overlays stay synchronized.
- R-GO-EXEC-005 (MUST): Maintain steer-target publication during replans by continuing the previously committed path until the new plan is committed; if the active path remains geometrically equivalent (Hausdorff distance < 0.2 m on remaining segment) keep it engaged, otherwise finish the current swath before switching.
- R-GO-EXEC-006 (MUST): Treat paths as equivalent during hot swaps only when the remaining segment meets both Hausdorff < 0.2 m and heading RMS < 0.5°; otherwise defer switching until the next connector or operator confirmation.
- R-GO-EXEC-007 (MUST): When Autosteer disengages or manual steering input exceeds the override threshold, freeze the active `path_id`, suppress automatic replans, and require explicit operator action to resume sequencing; a configurable auto-resume timer (default OFF) MAY unfreeze the plan after sustained inactivity.
- R-GO-EXEC-008 (SHOULD): Publish `speed_cap_mps` in `SteerTargets` when curvature exceeds implement limits at the current speed so UI can prompt speed reductions.
- R-GO-EXEC-009 (COULD): Accept optional row/implement sensor bias (`row_bias_m`) that laterally offsets the preview point within configured bounds (`row_bias_max_m = ±0.15`, `row_bias_decay_s = 2.0` default) without changing `path_id`.

### R-GO-REFRESH — Dynamic refresh policies
- R-GO-REFRESH-000 (MUST): Provide a “Quick Refresh” control that re-runs F2C with latest boundary/keep-outs and `W_eff`, guaranteeing UI responsiveness (command acknowledged within 250 ms) while continuing to publish steer targets from the last committed plan.
- R-GO-REFRESH-001 (MUST): Auto-trigger planning when boundaries/holes change, headland settings change, or orientation mode toggles; defer to operator confirmation for minor width changes below hysteresis.
- R-GO-REFRESH-002 (SHOULD): Support optional auto-refresh after headland `N` completes, as configured per job or session.

### R-GO-UX — Operator experience & messaging
- R-GO-UX-000 (MUST): Surface status pills (Recording, Field Established, Planning, Ready, Executing) and Quick Refresh availability in the plugin UI state machine.
- R-GO-UX-001 (MUST): Display prompts for detected loops (“Add as Field / Keep-Out?”) with area readout, and for major width changes (“W_eff changed to X m; Quick Refresh recommended”).
- R-GO-UX-002 (SHOULD): Provide side panel controls for headland count/width/smoothing, swath spacing/bias, orientation mode, and implement parameters sourced from equipment profiles.
- R-GO-UX-003 (COULD): Offer map overlays previewing planned swaths/headlands with color-coding for family (`Headland`, `Swath`, `WorkEdge`, `Contour`).
- R-GO-UX-004 (SHOULD): Surface plan orientation provenance (`ab_operator`, `ab_profile`, `optimal_f2c`, `inherited`) with bearing readout, capture the narrative reason string (e.g., "Adopt current heading @ timestamp"), and provide an "Adopt current heading" shortcut for AB mode.
- R-GO-UX-005 (MUST): When `speed_cap_mps` is emitted, display the limit in localized mph/kph alongside the raw m/s value within the guidance UI.

## Data contracts
- `Polygon`, `TrackPoint`, `EffectiveWidth`, `Implement`, `F2CRequest`, `Path`, `Catalog`, and `SteerTargets` structures SHALL follow the shapes documented in ADR-069, including IDs, frames, units, revision fields, timestamps, and the new metadata hooks (`origin_llh`, `enu_epoch`, `orientation_reason`). JSON Schema supplements SHALL use draft 2020-12 with `$id` prefix `aog://schemas/guidance-orchestrator/...`.
- Planner requests/responses MUST encode timestamps, revision identifiers, plan source (`f2c` | `fallback`), and planner provenance (Fields2Cover version & license ID) so telemetry logs can correlate operator actions with generated paths.
- Implement metadata MUST reference the equipment profile identifier used for the session to maintain traceability.
- Catalog metadata SHALL capture `plan_id`, `field_rev`, orientation source, bearing, `orientation_reason`, and retention bookkeeping (`kept`, `evicted`) to support replay and diagnostics.
- `SteerTargets` SHALL include optional `speed_cap_mps` and `row_bias_m` fields; downstream consumers MUST ignore the fields when omitted and accept both the canonical `_per_m` curvature properties and legacy `_1pm` aliases during the migration window.
- Curvature-bearing properties in planner/execution contracts SHALL use the `_per_m` suffix (`max_curv_per_m`, `ref_curvature_per_m`, `desired_curvature_per_m`, `peak_curv_per_m`).
- Equivalence policy SHALL be published alongside catalog metadata (Hausdorff and heading tolerances) to keep automation and UI consistent.

## Frames, units, and rates
- World frame SHALL be East-North-Up meters; vehicle frame origin SHALL be the rear axle midpoint with `+x` forward and `+y` left.
- Angles SHALL be expressed in radians and curvature in 1/m for all APIs.
- Planner evaluation loop SHALL run at 4 Hz, loop detection at 2 Hz while recording, and `SteerTargets` SHALL publish at 25 Hz with ±5 ms jitter.

## Performance & timing
- Loop detection and polygonization SHALL run at ≥ 2 Hz while recording headlands, with latency ≤ 150 ms per iteration for 10 km traces.
- Orchestrator core loop SHALL evaluate refresh triggers and build preview bundles at 4 Hz without exceeding 50 ms per cycle.
- Steer target publishing MUST remain within ±5 ms jitter at 25 Hz to avoid degrading Autosteer stability budgets defined in ADR-033.
- Fields2Cover plans SHALL meet p50 latency ≤ 120 ms and p95 latency ≤ 200 ms for ≤ 65 ha polygons with ≤ 2 holes when running on the reference CPU budget; AB fallback SHALL engage within 20 ms of a timeout.
- Operator overrides SHALL not introduce gaps in `SteerTargets`; frozen paths MUST continue publishing at 25 Hz until resume or re-lock.

## Validation
- Simulated 65 ha fields with one keep-out SHALL produce valid plans (no self-intersections, coverage ≥ 98% of legal area) with plan latency p50 ≤ 120 ms and p95 ≤ 200 ms.
- Hardware-in-loop tests SHALL confirm Quick Refresh end-to-end latency (operator click to first new steer target) ≤ 1.5 s with cached F2C responses and zero gaps in `SteerTargets` at 25 Hz during replans.
- Regression suites SHALL include loop-detection traces with varying noise levels to ensure false-positive rate < 2% when noise σ ≤ 0.15 m and polygon auto-repair success rate ≥ 99.9%.
- Stability testing SHALL show pass flip rate < 0.05 flips/min under nominal field noise when hysteresis features are enabled.
- Regression harness SHALL include the golden scenarios enumerated in ADR-069 (T01–T07) and record latency, coverage, fallback usage, and override behavior metrics for each run.
- All regression artifacts SHALL record `planner_seed` and `sequencer_seed` values to guarantee deterministic reruns across environments.

## Open questions
- Should Fields2Cover connectors be persisted for re-entry after manual detours, or generated on demand per transition?
- How should multi-implement rigs (e.g., hitch-drawn plus trailing) expose composite effective width to planners without over-complicating operator controls?
- What telemetry subset is required for remote monitoring of planning events while respecting offline privacy constraints?

## References
- [ADR-033 — Guidance planner and autosteer orchestration](../ADR/ADR-033-guidance-planner-autosteer.md)
- [ADR-027 — Spatial constraints and zone policies](../ADR/ADR-027-spatial-constraints.md)
- [ADR-044 — Zone & layer drawing framework](../ADR/ADR-044_ZoneDrawingFramework.md)
- [How-to: guidance lane contracts](../howto/guidance-lane-contracts.md)
