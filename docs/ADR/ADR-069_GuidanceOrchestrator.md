# ADR-069 — Guidance Orchestrator plugin

## Status
Proposed (seeking feedback from Plugins & Autonomy circles)

### Deciders
Plugins WG, Autonomy WG, UI WG

### Review Date
2025-11-15 (or after first pilot)

## Context
Nexus relies on ADR-033 for lane planning and autosteer arbitration, but today operators must pre-import boundaries or rely on legacy planners that ignore keep-outs and dynamic implement width. Fields2Cover (F2C) offers high-quality coverage plans if we can stream live field geometry, implement state, and refresh policies without stalling the cab experience. We need an orchestrator plugin that turns the first headland lap into a boundary, manages keep-outs, feeds F2C, and keeps Autosteer supplied with deterministic steering targets.

## Assumptions
- PoseStream provides fused ENU pose (position, heading, speed) at ≥ 50 Hz with latency budgets established in ADR-033.
- Sections plugin publishes section On/Auto states at ≥ 4 Hz for effective width computation.
- Autosteer consumes `SteerTargets` at 20–50 Hz (25 Hz default) and remains available even if planning is in flight.
- Fields2Cover runs as a local gRPC sidecar with CPU isolation; no ROS bridge is required for MVP.

## Non-Goals
- Implementing row-sensor or vision fusion control loops (hook only).
- Introducing ROS2 runtime integration (bridge may be added later).
- Managing implement-level hydraulic actuation policies (handled by supervisor layer).

## Frames, Units, and Rates
- **World frame:** ENU meters (`x` east, `y` north, `z` up); upstream converts WGS84 → ENU.
- **Vehicle frame:** origin at rear axle midpoint (articulation joint on articulated rigs); `+x` forward, `+y` left.
- **Units:** Angles in radians; curvature in 1/m; speeds in m/s; timestamps ISO-8601 UTC.
- **Loops:** Planner loop 4 Hz; Execution/`SteerTargets` 25 Hz; loop detection 2 Hz while recording.

## Decision
- Introduce a Guidance Orchestrator plugin responsible for live boundary building, planner coordination, and execution state machine transitions (Idle → Recording → Planning → Ready → Executing).
- Represent live geometry, implement spans, and path catalogs using typed data contracts shared with Autosteer, Sections, and Coverage Writer plugins, now annotated with IDs, frames, units, and revisions for traceability.
- Integrate with F2C via a sidecar client that caches responses per `(field.rev, width bucket, settings hash)` and falls back to AB offsets on error, recording the plan source (`f2c` | `fallback`).
- Maintain a Quick Refresh workflow that re-runs planning when significant geometry or implement changes occur, while throttling `W_eff` churn with hysteresis (banner at ±10%, auto replan at ±20%) and debouncing edits (boundary 500 ms, keep-out 250 ms).
- Guarantee deterministic execution: when a replan is pending the Execution Engine continues publishing from the last committed plan at 25 Hz with no gaps until the new plan is committed.
- Respect operator overrides: manual wheel input above the disengage threshold or Autosteer disengage SHALL freeze the active `path_id`, retain the current catalog, and suppress automatic replans until the operator re-engages or selects **Resume Plan**; an optional auto-resume timer MAY unfreeze after a configured idle period (default OFF).
- Persist orientation provenance: record whether the active bearing derives from operator AB input, profile defaults, optimal F2C solve, or inheritance from a prior job and include the confidence score for solver-derived headings.
- Provide operator UX hooks (status pills, prompts, side-panel controls) that surface planner state, plan source, and allow partial replans without freezing the UI.
- Enforce planner SLA: ≤ 200 ms per `MakePlan` for ≤ 65 ha (≈ 160 ac), ≤ 2 holes; timeout at 500 ms triggers AB fallback with telemetry.

## Consequences
- Operators gain a guided workflow for recording fields, marking obstacles, and locking onto planned swaths without leaving the cab.
- Sections and Coverage Writer can stay in sync because implement span updates and coverage polygons originate from the same orchestrator.
- Planner latency becomes more visible; we must guarantee sidecar availability and test fallback paths rigorously while surfacing plan source to operators and support.
- Additional regression coverage is required to validate loop detection, plan caching, steer-target publishing at the specified rates, and hysteresis tuned to prevent UI churn.
- Catalog retention introduces a visible `plan_id` history; support tooling must expose which plans were kept or evicted to aid replay and tablet-resume scenarios.

## Architecture sketch
1. **Live Field Builder** collects PoseStream samples, detects loops through spatial hashing, polygonizes candidate boundaries, and tracks revision IDs. Accepted polygons plus keep-outs feed the catalog and planning triggers.
2. **Implement Lens** listens to SectionState diffs, computes `EffectiveWidth`, and decides whether to trigger auto-refresh or banner prompts based on hysteresis thresholds.
3. **Planner Client** packages `F2CRequest` payloads and interprets `Headland`/`Swath`/`Connector` path responses, persisting them in the catalog alongside metadata required for UX sorting and automation sequencing.
4. **Execution Engine** exposes catalog browsing, path locking, serpentine sequencing, and publishes `SteerTargets` to Autosteer at 25 Hz with preview points, curvature, cross-track error, optional row bias hooks, and speed caps when curvature exceeds limits at the current speed.
5. **Refresh Manager** listens for boundary edits, keep-out additions, orientation changes, or operator-invoked Quick Refresh, coordinating partial replans when feasible to avoid discarding completed swaths.

## Data contracts
```ts
// Live geometry (ENU meters)
type Polygon = {
  id?: string;
  outer: [number, number][];               // ENU meters
  holes: [ [number, number][] ];
  crs?: 'ENU';
  origin_llh?: [number, number, number];   // lat, lon, ellipsoidal height (WGS84)
  enu_epoch?: string;                      // ISO-8601 timestamp the local ENU origin was frozen
  rev?: number;                            // monotonic revision when accepted
};

// Coverage-derived track (ENU)
type TrackPoint = { x: number; y: number; t: number };
type DrivenTrack = TrackPoint[];

// Effective width from sections (vehicle frame)
type EffectiveWidth = {
  total: number;                           // meters
  spans: Array<{ y0: number; y1: number }>; // lateral spans, meters
  centroid: number;                        // meters, +y left
  rev?: number;
};

type Implement = {
  width_nom: number;                       // meters
  min_turn_radius: number;                 // meters
};

type F2CRequest = {
  field: Polygon;                          // boundary + holes
  implement: Implement;
  headlands: { count: number; width: number; smoothing: number; max_curv_per_m: number };
  swaths: { spacing: number; smoothing: number; max_curv_per_m: number; straightness_bias: number };
  orientation: { mode: 'AB' | 'OPTIMAL'; bearing_rad?: number; nudge_m?: number };
};

type PathFamily = 'Headland' | 'Swath' | 'WorkEdge' | 'Contour';
type PathMeta = {
  id: string;                              // stable across refresh when geometry equal
  family: PathFamily;
  index: number;
  length_m: number;
  peak_curv_per_m: number;                 // 1/m
  est_coverage_m2: number;
  legal: boolean;
  quality: number;                         // 0..1 composite
  source?: 'f2c' | 'fallback';
  f2c_version?: string;
  f2c_license?: string;
};
type Path = {
  meta: PathMeta;
  spline: [number, number][];              // ENU samples, arc-length parameterized
  knots?: number[];
};

type PlanMeta = {
  plan_id: string;
  field_rev: number;
  source: 'f2c' | 'fallback';
  created_utc: string;
  orientation_source: 'ab_operator' | 'ab_profile' | 'optimal_f2c' | 'inherited';
  bearing_rad?: number;
  confidence?: number;                     // 0..1 for solver-derived headings
  orientation_reason?: string;             // narrative for audits (e.g., "Adopt heading @ 2025-10-16T14:21Z")
};

type Catalog = {
  meta: PlanMeta;
  paths: Record<string, Path>;
  order: string[];
  connectors?: Path[];
  retention: { kept: string[]; evicted: string[] };  // persisted history per field.rev
};

type PathEquivalence = {
  hausdorff_thresh_m: number;              // e.g., 0.2
  heading_rms_deg: number;                 // e.g., 0.5
};

type SteerTargets = {
  preview_point_xy: [number, number];      // vehicle frame meters
  ref_heading_rad: number;
  ref_curvature_per_m?: number;
  cross_track_m: number;
  heading_err_rad: number;
  desired_curvature_per_m: number;
  speed_mps: number;
  engaged: boolean;
  plan_id: string;
  path_id: string;
  ts_utc: string;
  speed_cap_mps?: number;
  row_bias_m?: number;
};
```

JSON Schemas SHALL target draft 2020-12 with `$id` values under `aog://schemas/guidance-orchestrator/...` and maintain aliases for deprecated `*_1pm` curvature fields during the migration window.

## Configuration defaults
- Pass-selection hysteresis uses `epsilon_normalized = 0.05` (or `0.10 m` lateral delta after normalization) to suppress churn when adjacent swaths score similarly.
- Path equivalence defaults to `hausdorff_thresh_m = 0.2` and `heading_rms_deg = 0.5`.
- Row bias hook defaults to `row_bias_max_m = ±0.15` with decay horizon `row_bias_decay_s = 2.0`.

## Geometry Conventions
- Outer polygon windings SHALL be counter-clockwise; holes SHALL be clockwise to align with F2C orientation expectations.
- A single field MAY contain multiple disjoint outer rings; disconnected rings are planned independently and sequenced per playbook policy, or the operator is prompted to split them into separate fields when sequencing would conflict.
- Local ENU coordinates are anchored per field using stored `origin_llh` and `enu_epoch` metadata to guarantee consistent replay after device restarts.
- Vertices are stored in ENU meters with minimum post-simplification spacing ≥ 0.05 m to avoid pathological offsets and duplicate points.
- Spline sampling defaults to 0.5–1.0 m arc-length spacing; the Execution Engine re-samples at control time using arc-length interpolation for smooth steering targets.

## Catalog Retention
- Retain up to the last three committed `plan_id`s per `field.rev`; new plans evict the oldest while persisting metadata for replay.
- On process restart, restore the most recent committed catalog from disk. If the previous active `path_id` remains equivalent (`Hausdorff < 0.2 m`, heading RMS < 0.5°) resume execution automatically; otherwise prompt the operator to re-lock.
- Persist catalogs on disk under `~/.nexus/guidance/plans/<fieldId>/<plan_id>.json` (configurable root) with checksums and enforce a configurable disk budget to avoid unbounded growth.

## Unworkable Pockets
- Regions with legal area below `2 × W_eff` or bottlenecks narrower than `W_eff` SHALL be labeled `unworkable`, excluded from sequencing, and never bridged automatically; impassable necks require explicit operator intervention.
- Operators may receive a "Finish with narrow tool" hint for excluded regions; plan metadata surfaces these pockets so external workflows can schedule follow-up passes.

## Row and implement sensor bias hook
- When a validated row/implement sensor publishes `RowLock` telemetry, the Execution Engine MAY apply a bounded lateral bias (`row_bias_m`) while preserving the active `path_id`.
- Bias is clamped to configured maxima and does not modify catalog geometry; it is cleared when the hook drops validity or the operator disengages Autosteer.
- Default limits: `row_bias_max_m = ±0.15` with exponential decay to zero over `row_bias_decay_s = 2.0` when validity degrades, ensuring smooth handoff back to catalog geometry.

## Event flow
- Pose samples extend `DrivenTrack`; loop detection fires at 2 Hz while Recording. Accepted boundary updates `field.rev` and publishes `FieldChanged(field.rev)` events after a 500 ms debounce; keep-out edits use 250 ms.
- Section mask changes feed EffectiveWidth; when hysteresis limits are exceeded, raise `WidthChanged(eff.rev, delta_pct)` events. Banner at ±10%, auto replan at ±20%.
- Planner client receives `PlanRequested(plan.key)` messages, where `plan.key := SHA256(field.rev || holes.rev || width_bucket || settings_hash || orientation_source || round(bearing_rad, 1e-4))`, calls F2C, and records per-plan metrics. Success emits `PlanCommitted(plan_id, source)` with catalog metadata; failures fall back to AB offsets and log `source='fallback'`.
- Operator locking a path emits `PathActivated(path_id)`, prompting Execution Engine to stream `SteerTargets` at 25 Hz and compute coverage polygons. Completion raises `PathCompleted(path_id)`.
- Quick Refresh events log `QuickRefresh(trigger)`; partial replans only occur when edits are > 30 m from the active path and family order is unchanged. Otherwise perform a full replan to keep sequencing consistent.
- When a new plan is committed mid-execution, retain the current path if the remaining geometry matches (Hausdorff distance < 0.2 m and heading RMS < 0.5°). Otherwise finish the current swath then switch at the next connector.
- Manual override or Autosteer disengage emits `PathFrozen(path_id)`; automatic replans are suppressed until `Resume Plan` or re-engagement logs `PathResumed(path_id)`.

## Failure Modes & Mitigations
- **Sidecar timeout/unavailable:** trigger AB fallback, surface toast, retry with exponential backoff (1 s, 2 s, 4 s, 8 s).
- **Invalid field/keep-out polygon:** attempt `buffer(0)` repair; if still invalid prompt operator to retrace or discard change.
- **Plan thrash (pass index flipping):** apply ±1 index stickiness with ≥ 3 s dwell and require cost delta > ε before switching.
- **Overspeed vs curvature:** Execution caps desired curvature and surfaces a “Reduce speed” banner when curvature exceeds implement limits.
- **Tiny islands / disconnected polygons:** flag unworkable pockets when the legal area or width falls below thresholds; prompt operators to split disconnected regions or accept exclusion before planning proceeds.
- **Pathological geometry load:** reject polygons with > 50k vertices or > 32 holes and request simplification to protect planner latency budgets.
- **Sidecar resume after tablet reboot:** leverage catalog retention to reload the last committed plan and avoid plan storms; if equivalence fails, block auto-resume until the operator confirms the new selection.

## Observability
- Emit per-plan telemetry: `plan_ms`, `num_paths`, `total_length_m`, `max_curv_per_m`, `est_coverage_m2`, `source`, `field.rev`, `settings.hash`, `f2c_version`, `f2c_license`, `orientation_source`, `orientation_reason`, and `bearing_rad`.
- Record event log entries (`FieldChanged`, `WidthChanged`, `PlanRequested`, `PlanCommitted`, `PathActivated`, `PathCompleted`, `QuickRefresh`, `PathFrozen`, `PathResumed`) with timestamps for replay/debug.
- Provide debug overlay: active `plan_id`, `path_id`, index, progress (0..1), preview lookahead distance, current `W_eff` and hysteresis state, retained plan history, and whether speed caps or row biases are active.
- When `speed_cap_mps` is present, surface localized mph/kph equivalents for operators while retaining the raw m/s value in engineering overlays.
- Record deterministic seeds (`planner_seed`, `sequencer_seed`) for each plan/execute cycle to guarantee replay fidelity and golden test reproducibility.

## Security & licensing
- Capture the Fields2Cover version and license identifier in plan metadata and telemetry to satisfy OSS reporting and enable reproducible planning results.
- Ensure sidecar communication stays on localhost unless explicitly configured for remote planners; reject unsigned binaries in production builds per existing plugin policy.

## Validation & rollout
- Extend the guidance regression harness with loop-detection traces, F2C sample fields (with/without keep-outs), and Quick Refresh latency tests to meet SRS §19 acceptance metrics (MakePlan p50 ≤ 120 ms, p95 ≤ 200 ms; AB fallback engage ≤ 20 ms after timeout).
- Stage cab UI updates to surface status pills, Quick Refresh prompts, and plan source indicators, coordinating with UI Owner for design review.
- Pilot the plugin on the composite simulator before scheduling hardware benches; require ≥ 10 consecutive plan/execute/refresh cycles without missed targets, UI stalls, or SteerTarget gaps.
- Maintain a "golden fields" bundle (rectangle, L-shape, donut with keep-out, narrow corridor) in the regression harness and publish measured metrics (latency, coverage, pass flips) for each run.
- Persist `planner_seed` and `sequencer_seed` values with each regression artifact to make reruns deterministic across environments.

### Recommended regression scenarios
- **T01 Rectangle (65 ha, no holes):** Verify two headlands, serpentine swaths, and plan latency p95 ≤ 200 ms.
- **T02 Donut field (single keep-out):** Confirm headlands respect the hole and connectors avoid the exclusion zone.
- **T03 Dynamic W_eff change:** Apply +15% (banner only) then +25% (auto replan) without SteerTarget gaps.
- **T04 Keep-out near active path:** Inject keep-out within 15 m of active path; ensure full replan while execution continues on the previous plan until connector.
- **T05 Sidecar failure:** Kill F2C mid-plan; fallback AB engages ≤ 20 ms post-timeout with `source='fallback'` telemetry.
- **T06 Overspeed vs curvature:** Command speed beyond curvature limit; `speed_cap_mps` surfaces and UI banners "Reduce speed".
- **T07 Manual override:** Apply manual steering blip; expect `PathFrozen` event, replans suppressed, and `Resume Plan` restores sequencing.

## Acceptance guardrails
- **Latency:** `MakePlan` p50 ≤ 120 ms, p95 ≤ 200 ms for ≤ 65 ha (≈ 160 ac) polygons with ≤ 2 holes; AB fallback engages within 20 ms of timeout.
- **Determinism:** No gaps in `SteerTargets` at 25 Hz during replans (verified in simulation harness).
- **Integrity:** Polygon auto-repair success rate ≥ 99.9%; invalid cases are surfaced to UI with actionable prompts.
- **Stability:** Pass flip rate < 0.05 flips/min under nominal field noise with hysteresis enabled.

## Alternatives considered
- **Legacy boundary import workflow** — rejected because it requires pre-planned fields and cannot adapt to live keep-outs or implement width changes.
- **Direct Autosteer planner extension** — rejected to keep planner orchestration modular and align with plugin governance (ADR-031). The orchestrator remains a plugin to avoid tight coupling with Autosteer internals.

## References
- [SRS §19 Guidance Orchestrator Plugin](../SRS/sections/8X/81_Guidance_Orchestrator.md)
- [ADR-033 Guidance planner & autosteer orchestration](ADR-033-guidance-planner-autosteer.md)
- [ADR-027 Spatial constraints & zone policies](ADR-027-spatial-constraints.md)
- [ADR-044 Zone & layer drawing framework](ADR-044_ZoneDrawingFramework.md)
