# Guidance Orchestrator — Path Catalog & Sequencer

Describes catalog organization, sequencing rules, retention, and override handling.

## Catalog model
- `Catalog.meta`: `plan_id`, `field_rev`, `source`, orientation provenance (`orientation_source`, `orientation_reason`), creation UTC.
- `Path.meta`: includes `family`, `index`, `length_m`, `peak_curv_per_m`, `quality`, `source`, `f2c_version`, `f2c_license`.
- `PathEquivalence`: defaults `hausdorff_thresh_m = 0.2`, `heading_rms_deg = 0.5`.

## Sequencing policies
- Default serpentine order for swaths; connectors optional based on plan response.
- Sequencer respects pass-selection hysteresis with `epsilon_normalized = 0.05` (or 0.10 m lateral delta) before switching.
- `PathFrozen` state suppresses automatic resequencing until `Resume Plan` or auto-resume timer expiry.

## Retention
- Persist last three catalogs per `field.rev` in `~/.nexus/guidance/plans/<fieldId>/<plan_id>.json`.
- On restart, reload most recent plan; resume only if equivalence holds, otherwise prompt user.
- Retention metadata (`kept`, `evicted`) logged for diagnostics.

## Overrides & row bias
- Manual wheel input > threshold emits `PathFrozen`; freeze persists through replans.
- Optional row sensors may set `row_bias_m` bounded by `row_bias_max_m = ±0.15` with decay `row_bias_decay_s = 2.0`.
- Speed caps: publish `speed_cap_mps` when curvature infeasible; UI shows mph/kph plus raw m/s.

## Acceptance checks
- [ ] Sequencer never flips paths more than 0.05/min under nominal noise.
- [ ] Frozen paths maintain steer-target publication until resume.
- [ ] Row bias hook decays to zero within 2 s of losing validity.
- [ ] Plan retention reloads prior catalog and prompts when equivalence fails.
