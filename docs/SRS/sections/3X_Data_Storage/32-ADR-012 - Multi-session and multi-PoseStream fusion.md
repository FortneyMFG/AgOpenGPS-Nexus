# ADR-012: Multi-session and multi-PoseStream fusion

## Status
Drafting (target review window: 2025-11-19 week)

**Relevant Plugin(s):** Mapping, Variable Mapping, Telemetry Logging, Job Tasks



## Context
Operations often blend PoseStreams from multiple machines and seasons (e.g., planter plus sprayer passes) to produce prescriptions or QA analytics. Without a defined fusion policy, CRS reprojection, deduplication, and provenance handling remain ad-hoc and error-prone. ADR-012 formalizes merge rules that rely on ADR-009 persistence, ADR-010 layer registry metadata, and the forthcoming ADR-022 CRS policy so composite datasets remain deterministic.

## Decision
- Establish alignment rules for time and space, including CRS reprojection steps driven by ADR-022 and priority hierarchies for overlapping PoseStreams.
- Define deduplication and authority resolution policies (e.g., prescription overrides historical averages when quality thresholds are met) with area-weighted merges.
- Provide provenance handling and auditing so merged layers retain source lineage required by ADR-019 governance.
- Implement fusion services and utilities that expose configurable recipes and validation hooks for downstream analytics.

## Consequences
- Analytics and prescription pipelines can rely on deterministic merge outputs, increasing reproducibility and QA confidence.
- Fusion introduces computational overhead and dependency on precise CRS utilities, requiring performance and accuracy monitoring.
- Provenance tracking becomes mandatory across merged datasets, necessitating updates to storage schemas and UI surfacing.

## Governance Updates
- **Fusion recipe registry.** Recipes are published as signed manifests referencing provenance records and CRS configurations. Registry entries include owner, validation status, and retirement plan.
- **Stress suite coverage.** Dense overlap, conflicting CRS, and noisy GNSS fixtures must pass determinism and accuracy thresholds before recipes become defaults. Failures block promotion until mitigations are merged.
- **Audit trail.** Recipe updates automatically notify dependent analytics teams and append change summaries to the provenance ledger for traceability.

## Validation
- Fusion outputs must maintain ≤ 3 cm positional drift after CRS reprojection when merging two RTK-quality PoseStreams.
- Priority arbitration must uphold configured authority hierarchies across 200 randomized scenarios, logging the selected source for audit.
- Provenance tracker must persist complete lineage for merged layers and export JSON summaries with < 200 ms serialization for 10k-sample jobs.

## References
- [Data model & storage requirements](../SRS/sections/3X_Data_Storage/32_Persistence_Formats.md)
- [Control & automation requirements](../SRS/sections/6X_Core_Domain_Services/61_Kinematics_Pose_Fusion.md)
- [ADR-009: PoseStream vector logs and layer TileStore persistence](ADR-009-posestream-vector-tilestore-persistence.md)
- [ADR-022: CRS, units, and precision policy](ADR-022-crs-units-precision-policy.md)
- [ADR-019: Provenance, audit, and QA](ADR-019-provenance-audit-qa.md)
