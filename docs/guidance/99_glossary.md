# Guidance Orchestrator — Glossary

| Term | Definition |
| --- | --- |
| ENU | East-North-Up local tangent plane coordinates anchored by `origin_llh` + `enu_epoch`. |
| `field.rev` | Monotonic revision assigned to the accepted field boundary polygon. |
| `hole.rev` | Monotonic revision for a keep-out polygon used in plan key hashing. |
| `W_eff` | Effective implement width derived from active sections (vehicle frame). |
| `plan.key` | SHA256 hash of `field.rev`, `holes.rev`, width bucket, settings hash, orientation metadata. |
| `plan_id` | Stable identifier for committed catalog (hash of geometry + settings + source). |
| `epsilon_normalized` | Pass-selection hysteresis threshold (0.05 normalized ≈ 0.10 m lateral delta). |
| `speed_cap_mps` | Maximum allowed speed for current curvature; UI shows mph/kph equivalents. |
| `row_bias_m` | Optional lateral offset applied when row/implement sensors are valid. |
| Quick Refresh | Operator-triggered planner run using current boundary, keep-outs, and effective width. |
| AB fallback | Deterministic AB-offset planner used when F2C errors or times out. |
| Catalog retention | Policy of storing last three catalogs per `field.rev` under `~/.nexus/guidance/plans/`. |
| Golden scenarios | Regression fixtures T01–T07 verifying latency, coverage, fallback, overrides. |
