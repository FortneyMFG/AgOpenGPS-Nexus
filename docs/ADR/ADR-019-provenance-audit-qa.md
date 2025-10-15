# ADR-019: Provenance, audit, and QA governance

## Status
Drafting (target review window: 2025-12-12 week)

## Context
As Nexus orchestrates multi-layer analytics and prescriptions, the platform must track provenance, quality status, and audit trails across storage, UI, and export pipelines. Current workflows lack unified identifiers and hash checks, making compliance reporting and troubleshooting difficult. ADR-019 defines provenance registry expectations aligned with ADR-023 session models, ADR-013 derived products, and ADR-009 persistence.

## Decision
- Introduce a provenance registry capturing dataset hashes, job/session IDs, quality flags, and chain-of-custody rules across ingestion, transformation, and export stages.
- Enforce QA badge surfacing in UI components with drill-down links to dataset histories and audit events.
- Integrate hash validation and provenance checks into CI pipelines to detect regressions and unauthorized modifications.
- Coordinate with ADR-023 session orchestration to ensure provenance relationships span jobs, PoseStreams, and derived artifacts.

## Consequences
- Operators and auditors gain visibility into data lineage and QA status, improving trust and compliance readiness.
- Additional storage and UI surfaces are required to store and present provenance metadata, increasing implementation scope.
- CI pipelines must compute and compare hashes, adding runtime overhead but preventing silent drift.

## Validation
- Provenance records must apply SHA-256 hash stamps to PoseStream and tile artifacts with 100% match against regression goldens.
- UI badges must reflect QA state transitions within two seconds of provenance updates with verified telemetry events.
- Audit pipeline must export signed JSON logs for 30-day retention with ≤ 5% size overhead relative to raw event streams.

## References
- [Telemetry & health requirements](../SRS/sections/10_Telemetry_Health.md)
- [Testing & CI requirements](../SRS/sections/11_Testing_CI_CDPipelines.md)
- [Data model & storage requirements](../SRS/sections/08_Data_Model_Storage.md)
- [ADR-009: PoseStream vector logs and layer TileStore persistence](ADR-009-posestream-vector-tilestore-persistence.md)
- [ADR-023: Session and job model](ADR-023-session-job-model.md)
