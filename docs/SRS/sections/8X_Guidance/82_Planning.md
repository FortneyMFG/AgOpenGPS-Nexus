# 82 — Planning (Status: drafting)

## Problem statement
Detail the interfaces between the guidance planner, Fields2Cover integration, and plan caching mechanisms so refreshes, replans, and safety fallbacks remain predictable across field operations.

## Requirements (from contributors)
- R-PLAN-000 (MUST, planner API): Package planner requests with `{boundary_rev, keepouts_rev, effective_width, implement_profile, headland_spec, orientation}` and handle responses within 200 ms median for ≤65 ha fields; expose error codes and fallback reasons to UI.
- R-PLAN-001 (MUST, caching): Cache planner outputs keyed by field revision + settings hash; reuse cached catalogs until hysteresis thresholds trigger a refresh (boundary debounce 500 ms, keep-out 250 ms, width 10 %).
- R-PLAN-002 (MUST, fallback mode): Provide deterministic AB-line fallback when planner timeouts (>500 ms) or failures occur, tagging targets with `plan_source=fallback` for telemetry/audit.
- R-PLAN-003 (SHOULD, partial replans): Preserve unworked paths when new keep-outs appear ≥30 m away and maintain sequencing; otherwise trigger a full replan with operator confirmation.
- R-PLAN-004 (MUST, plan persistence): Persist the last three plan catalogs per field to disk and restore the latest catalog on restart to avoid unnecessary replanning during power cycles.
- R-PLAN-005 (MUST, execution stream): Publish `SteerTargets` at 25 Hz with ≤5 ms jitter, include optional `speed_cap_mps` and `row_bias_m`, and keep streaming along the previous path until a new catalog is committed.
- R-PLAN-006 (SHOULD, equivalency policy): Define Hausdorff and heading tolerances for hot swaps so UI/automation agree when a refreshed path is equivalent and can remain engaged.
- R-PLAN-007 (MUST, telemetry): Record planner latency, target jitter, and plan source metadata for each session to power QA dashboards and regression tests.

## Current sentiment
Teams are focusing on deterministic planner caches and hot-swap rules so Autosteer never loses engagement during quick refresh or orientation changes; telemetry coverage is required before releasing to beta operators.

---
