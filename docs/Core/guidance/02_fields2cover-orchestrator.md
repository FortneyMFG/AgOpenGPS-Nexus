# Guidance Orchestrator — Fields2Cover Planner Integration

This note captures the planner request pipeline, caching, fallback policy, and SLA tracking for the Fields2Cover (F2C) sidecar.

## Request assembly
- Inputs: `field.rev`, `holes.rev`, `EffectiveWidth`, implement profile, headland & swath settings, orientation mode, seeds.
- Compose `plan.key = SHA256(field.rev || holes.rev || width_bucket || settings_hash || orientation_source || round(bearing_rad,1e-4))`.
- Populate `F2CRequest` with `_per_m` curvature fields and ENU geometry anchored by `origin_llh` + `enu_epoch`.

## Caching & retention
- Check disk cache under `~/.nexus/guidance/plans/<fieldId>/<plan_id>.json` (max 200 MB configurable).
- Retain last three catalogs per `field.rev`; record retention telemetry (`kept`, `evicted`).

## Planner SLA
- Target: `MakePlan` p50 ≤ 120 ms, p95 ≤ 200 ms for ≤ 65 ha (≈160 ac), ≤2 holes.
- Timeout: 500 ms → AB fallback; log `source='fallback'`, emit toast.
- Retries: exponential backoff 1s→2s→4s→8s when sidecar unreachable.

## Partial replans
- Eligible when edits are >30 m from active path, family order unchanged, and unworked swaths remain.
- Equivalent plan detection uses `Hausdorff < 0.2 m` and heading RMS < 0.5° on remaining path.

## Telemetry & seeds
- Emit `plan_ms`, `num_paths`, `total_length_m`, `max_curv_per_m`, `est_coverage_m2`, `planner_seed`, `sequencer_seed`, `source`, `orientation_source`, `orientation_reason`.
- Store F2C version + license ID for audit.

## Acceptance checks
- [ ] Cache hits skip gRPC call and serve plan within 20 ms.
- [ ] Timeout triggers fallback AB planner in ≤20 ms with `source='fallback'` telemetry.
- [ ] Partial replan maintains steer-target continuity with no 25 Hz gaps.
- [ ] Catalog retention restores most recent plan after orchestrator restart.
