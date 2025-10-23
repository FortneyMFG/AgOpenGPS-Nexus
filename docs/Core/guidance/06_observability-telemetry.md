# Guidance Orchestrator — Observability & Telemetry

Outlines metrics, event logs, and artifacts required for diagnostics and compliance.

## Metrics
- Per-plan: `plan_ms`, `num_paths`, `total_length_m`, `max_curv_per_m`, `est_coverage_m2`, `planner_seed`, `sequencer_seed`, `source`, `orientation_source`, `orientation_reason`, `bearing_rad`.
- Execution: 25 Hz jitter, `speed_cap_mps` activations, `row_bias_m` magnitude, pass flip rate.
- Loop builder: repair success rate, simplification tolerance distribution, polygon density warnings.

## Events
- `FieldChanged(field.rev)`, `KeepOutChanged(hole.rev)`, `WidthChanged(eff.rev, delta_pct)`.
- `PlanRequested(plan.key)`, `PlanCommitted(plan_id, source)`, `PlanFailed(plan_id, reason)`.
- `PathActivated`, `PathCompleted`, `PathFrozen`, `PathResumed`, `QuickRefresh(trigger)`.

## Artifacts
- Persist catalogs in `~/.nexus/guidance/plans/...` with SHA256 checksum and schema version.
- Log planner/ sequencer seeds with each regression artifact.
- Attach golden scenario metrics (T01–T07) to CI artifacts for trend tracking.

## Tooling
- JSON Schema validation (draft 2020-12) for catalog and steer target payloads using `ajv-cli`.
- Sim harness reporter that exports CSV + JSON metrics for regression dashboards.

## Acceptance checks
- [ ] Metrics stream ingested by telemetry backend with <5 s lag.
- [ ] Event log replays replicate Quick Refresh + override sequences deterministically.
- [ ] Schema validation integrated into CI gating workflows.
- [ ] Golden scenario artifacts published with seeds and plan provenance for each build.
