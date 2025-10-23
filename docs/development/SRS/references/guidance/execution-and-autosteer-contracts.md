# Guidance Orchestrator — Execution & Autosteer Contracts

Details the 25 Hz execution loop, `SteerTargets` payload, and safety hooks.

## Execution loop
- Runs at 25 Hz; each tick fetches current pose, evaluates plan progress, emits `SteerTargets`.
- If a replan is pending, continue publishing from last committed catalog until swap allowed.
- Preview lookahead 80–120 m configurable; sample spline via arc-length interpolation.

## SteerTargets schema
- `preview_point_xy`: vehicle frame meters.
- `ref_heading_rad`, `ref_curvature_per_m`, `desired_curvature_per_m` (alias `_1pm` supported during migration).
- `cross_track_m`, `heading_err_rad`, `speed_mps`, `speed_cap_mps` (optional), `row_bias_m` (optional).
- `plan_id`, `path_id`, `ts_utc` (ISO-8601), `engaged` boolean.

## Safety hooks
- Speed capping: when `|desired_curvature_per_m| > curvature_limit`, compute `speed_cap_mps` and surface UI prompt.
- Row sensors: apply `row_bias_m` within ±0.15 m, decaying over 2 s when validity drops.
- Overrides: manual steering freeze maintains `SteerTargets` continuity but marks `engaged=false` if Autosteer disengages.

## Telemetry & observability
- Log 25 Hz `SteerTargets` for replay; ensure no gaps when replanning.
- Record `PathFrozen`/`PathResumed` events with timestamps.
- Monitor jitter budget ±5 ms; flag warnings when exceeded.

## Acceptance checks
- [ ] Autosteer integration consumes `SteerTargets` 25 Hz with no gaps during replans.
- [ ] Speed cap surfaces for curvature-limited corners and clears when safe.
- [ ] Row bias hook shifts preview point smoothly and decays after validity loss.
- [ ] Override freeze toggles `engaged` flag and suppresses auto-replans until resume.
