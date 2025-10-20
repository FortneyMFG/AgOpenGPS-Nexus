# 82 — Planning (Status: drafting requirements)

## Problem statement
Nexus needs a deterministic planning stack that transforms boundary, keep-out, implement, and orientation inputs into steerable catalogs without pausing Autosteer. Operators expect quick refreshes, predictable fallbacks, and consistent telemetry across restarts while the planner evaluates complex headlands and partial replans.

## Requirements (from contributors)
- R-PLAN-000 (MUST, planner API): Package planner requests with `{boundary_rev, keepouts_rev, effective_width, implement_profile, headland_spec, orientation}` and handle responses within 200 ms median for ≤ 65 ha fields; expose error codes and fallback reasons to UI.
- R-PLAN-001 (MUST, caching): Cache planner outputs keyed by field revision + settings hash; reuse cached catalogs until hysteresis thresholds trigger a refresh (boundary debounce 500 ms, keep-out 250 ms, width 10%).
- R-PLAN-002 (MUST, fallback mode): Provide deterministic AB-line fallback when planner timeouts (> 500 ms) or failures occur, tagging targets with `plan_source=fallback` for telemetry/audit.
- R-PLAN-003 (SHOULD, partial replans): Preserve unworked paths when new keep-outs appear ≥ 30 m away and maintain sequencing; otherwise trigger a full replan with operator confirmation.
- R-PLAN-004 (MUST, plan persistence): Persist the last three plan catalogs per field to disk and restore the latest catalog on restart to avoid unnecessary replanning during power cycles.
- R-PLAN-005 (MUST, execution stream): Publish `SteerTargets` at 25 Hz with ≤ 5 ms jitter, include optional `speed_cap_mps` and `row_bias_m`, and keep streaming along the previous path until a new catalog is committed.
- R-PLAN-006 (SHOULD, equivalency policy): Define Hausdorff and heading tolerances for hot swaps so UI/automation agree when a refreshed path is equivalent and can remain engaged.
- R-PLAN-007 (MUST, telemetry): Record planner latency, target jitter, and plan source metadata for each session to power QA dashboards and regression tests.

## Options
- O-PLAN-001: Embedded Fields2Cover driver — Planner logic hosts the F2C integration inside the Guidance plugin with in-process caching.
- O-PLAN-002: gRPC planner microservice — Planner requests marshal over gRPC to an external service that owns caches and provides fallbacks.
- O-PLAN-003: Hybrid planner (embedded + remote fallback) — Primary planner runs in-process while selected jobs can escalate to a remote solver when local latency budgets are exceeded.

### Option families & decision ordering
| Family ID | Type | Options | Decides before | Notes |
|---|---|---|---|---|
| DS-PLAN-RUNTIME | Exclusive | O-PLAN-001, O-PLAN-002, O-PLAN-003 | DS-PLAN-FALLBACK | Determines where planning workloads execute and how caches persist. |
| DS-PLAN-FALLBACK | Composable | O-PLAN-001, O-PLAN-003 | — | Governs whether fallbacks share code paths with primary planner or live in remote service. |

## Comparison (quick matrix)
| Option | Pros | Cons | Risks | Borrow from existing |
|---|---|---|---|---|
| O-PLAN-001 | Lowest latency; reuse plugin DI context; simpler deployment. | Tighter coupling to Guidance plugin; resource contention under heavy loads. | Planner updates require full plugin redeploy. | AgOpenGPS v6 in-process planners. |
| O-PLAN-002 | Clear isolation; scale planner separately; language-agnostic service. | Adds serialization overhead; requires service management. | Network hiccups can degrade determinism if not cached. | Existing ADR-033 remote planner experiments. |
| O-PLAN-003 | Keeps fast local path; optional burst capacity; flexible deployments. | Higher complexity syncing caches; requires arbitration logic. | Drift between local and remote solvers if versions diverge. | Hybrid planner prototypes from Fields2Cover beta. |

## Decision matrices
- [ ] DS-PLAN-RUNTIME decision matrix drafted
- [ ] DS-PLAN-FALLBACK decision matrix drafted

## Evaluation criteria
- Deterministic latency ≤ 200 ms for ≤ 65 ha polygons with ≤ 2 holes.
- Hot-swap equivalency preserves Autosteer engagement without jitter.
- Planner caches survive restarts and configuration reloads.
- Telemetry coverage enables regression dashboards (latency, fallback rate, jitter).
- Deployments remain supportable for offline operators (no cloud dependency by default).

## Current sentiment
- Embedded Fields2Cover (O-PLAN-001) remains the default due to minimal latency and reuse of ADR-033 contracts.
- Hybrid planner (O-PLAN-003) is attractive for larger fields if cache coherence costs stay manageable.

## Open questions
- Q-PLAN-001: Which planner artifacts (catalog JSON, solver logs) must sync to support remote debugging?
- Q-PLAN-002: How should planner hysteresis thresholds adapt for multi-implement rigs with variable width sensors?
- Q-PLAN-003: What telemetry sampling strategy balances 25 Hz steer target logs with storage constraints during long jobs?
