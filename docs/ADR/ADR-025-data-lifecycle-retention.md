# ADR-025: Data lifecycle and retention policy

## Status
Drafting (target review window: 2025-12-22 week)

**Relevant Plugin(s):** Mapping, Variable Mapping, Telemetry Logging, Job Tasks, UI Shell



## Context
Field deployments accumulate large PoseStream, tile, and derived datasets that must comply with retention, privacy, and storage constraints. Without coordinated lifecycle policies, devices risk running out of space or violating retention rules. ADR-025 defines storage management expectations aligned with ADR-009 persistence, ADR-019 provenance, and ADR-020 determinism workflows.

## Decision
- Establish retention windows for on-device and removable storage, including compaction triggers and archival/export workflows.
- Define background maintenance jobs (re-encode LZ4→Zstd, privacy flag propagation) that keep disk usage within budget while preserving determinism guarantees.
- Provide privacy tagging and redaction policies for exports, ensuring sensitive fields are handled consistently across pipelines.
- Expose lifecycle status through telemetry and operator tooling so crews can manage storage proactively.

## Consequences
- Devices maintain healthy storage utilization and compliance posture but require additional background services and monitoring.
- Privacy and retention enforcement add operational overhead yet reduce risk of data leakage.
- Export workflows must integrate with provenance and determinism checks, increasing complexity but aligning datasets across systems.

## Governance Updates
- **Cost modeling.** Storage planners publish per-deployment cost models covering hot/cold tiers and maintenance CPU. Operators receive calculators to forecast spend before enabling features.
- **Privacy propagation.** Privacy tags link to provenance so redaction requests automatically cascade through exports and derived datasets.
- **Compliance audits.** Quarterly audits reconcile policy with regulatory obligations, documenting variances and remediation plans.

## Validation
- Retention planner must enforce configurable windows (30/90/365 days) with automated tests proving compliance.
- Compaction scheduler must keep disk utilization ≤ 70% under heavy ingest while limiting maintenance CPU to ≤ 15% on average.
- Privacy flag propagation must redact sensitive fields in exports with zero leakage verified via automated diff comparisons.

## References
- [Data model & storage requirements](../SRS/sections/08_Data_Model_Storage.md)
- [Backend services requirements](../SRS/sections/04_Backend_Services.md)
- [Telemetry & health requirements](../SRS/sections/10_Telemetry_Health.md)
- [ADR-009: PoseStream vector logs and layer TileStore persistence](ADR-009-posestream-vector-tilestore-persistence.md)
- [ADR-019: Provenance, audit, and QA governance](ADR-019-provenance-audit-qa.md)
- [ADR-020: Determinism, replay, and CI guardrails](ADR-020-determinism-replay-ci.md)
