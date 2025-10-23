# Nexus Glossary

## Platform-wide Terms

| Term | Definition |
| --- | --- |
| **Pose** | A timestamped position/orientation sample for a physical or virtual node (tractor, implement, toolbar, sensor) within the canonical coordinate reference system. |
| **PoseStream** | The ordered, canonical time series of poses and associated state deltas that all Nexus services, plugins, and replays consume. |
| **Equipment** | A configured machine profile representing a tractor, combine, sprayer, or other power unit that may host one or more implements. |
| **Implement** | A functional attachment (planter, toolbar, sprayer) mounted to equipment and containing toolbars, sections, and sensors. |
| **Toolbar** | A physical or logical boom/bar spanning multiple sections with shared lookahead, overlap, and control metadata. |
| **SectionNode** | The smallest controllable output element (row unit, nozzle, valve) with an addressable on/auto/off state. |
| **SectionGroup** | A named grouping of SectionNodes (or nested groups) that can receive aggregate commands, overlaps, or rates. |
| **Master Group** | The highest-priority SectionGroup controlling a toolbar or implement; manual overrides or safety interlocks apply here first. |
| **Layer** | A time- or session-bounded spatial dataset (coverage, rate, yield, diagnostics) registered with units, precision, and visualization metadata. |
| **Prescription** | A target layer that prescribes rates or setpoints to controllers (e.g., VR fertilizer, seeding) and feeds automation decisions. |
| **Event** | A discrete control or telemetry record representing an observed action (e.g., section on/off, obstruction detected). |
| **Opportunity** | The expected number of controllable actions in an interval (e.g., rows that should fire) used to evaluate misses/doubles. |
| **Tile** | A chunk of gridded spatial data persisted in the TileStore with shared codec/precision metadata. |
| **Cell** | The individual sample within a tile storing a value, weight, min/max, and quality metadata. |
| **Vector Log** | The append-only PoseStream/SectionState record used for deterministic replay and audit trails. |
| **Fusion** | The process of combining multiple PoseStreams or layers (sessions, implements, seasons) into a unified dataset with provenance. |
| **Provenance** | Recorded lineage describing how data was produced, transformed, and validated across plugins, sessions, and exports. |
| **Registry Hash** | A stable hash computed over layer definitions and schemas to detect mismatches between plugins, firmware, and stored data. |

## Guidance Orchestrator Terms

| Term | Definition |
| --- | --- |
| **ENU** | East-North-Up local tangent plane coordinates anchored by `origin_llh` + `enu_epoch`. |
| **`field.rev`** | Monotonic revision assigned to the accepted field boundary polygon. |
| **`hole.rev`** | Monotonic revision for a keep-out polygon used in plan key hashing. |
| **`W_eff`** | Effective implement width derived from active sections (vehicle frame). |
| **`plan.key`** | SHA256 hash of `field.rev`, `holes.rev`, width bucket, settings hash, orientation metadata. |
| **`plan_id`** | Stable identifier for committed catalog (hash of geometry + settings + source). |
| **`epsilon_normalized`** | Pass-selection hysteresis threshold (0.05 normalized ≈ 0.10 m lateral delta). |
| **`speed_cap_mps`** | Maximum allowed speed for current curvature; UI shows mph/kph equivalents. |
| **`row_bias_m`** | Optional lateral offset applied when row/implement sensors are valid. |
| **Quick Refresh** | Operator-triggered planner run using current boundary, keep-outs, and effective width. |
| **AB fallback** | Deterministic AB-offset planner used when F2C errors or times out. |
| **Catalog retention** | Policy of storing last three catalogs per `field.rev` under `~/.nexus/guidance/plans/`. |
| **Golden scenarios** | Regression fixtures T01–T07 verifying latency, coverage, fallback, overrides. |

> Have another term to add? Update this master glossary and cross-link any domain-specific guides back here.
