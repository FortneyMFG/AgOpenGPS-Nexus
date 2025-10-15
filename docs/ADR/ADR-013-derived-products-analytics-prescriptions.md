# ADR-013: Derived products and prescription analytics

## Status
Drafting (target review window: 2025-12-02 week)

## Context
Turning PoseStream-derived datasets (yield, soil, NDVI) into actionable prescriptions requires reproducible recipes, QA metrics, and deterministic exports. Current tooling mixes manual steps and custom scripts, making repeatability and auditability difficult. ADR-013 defines a governed derivation pipeline that builds on ADR-012 fusion outputs, ADR-010 layer metadata, and ADR-014 interop expectations.

## Decision
- Provide a configurable derivation pipeline capable of gridding, smoothing, ROI masking, banding, and clamping with parameterized recipes and stable hashes.
- Compute QA metrics (coverage, variance, RMSE vs. targets) as part of every derivation run and persist results for downstream provenance (ADR-019).
- Support reproducible recipes defined in machine-readable formats (YAML/JSON) with versioned schemas and validation tooling.
- Deliver regression fixtures and CI integration that validate deterministic outputs across supported operating systems.

## Consequences
- Agronomic teams gain repeatable prescription generation, enabling collaboration and compliance workflows.
- Formalizing recipes and QA metrics increases initial tooling complexity but improves observability and auditing.
- Exporters and interop adapters must consume derived outputs using consistent metadata and provenance links.

## Validation
- Prescription derivations must complete within four minutes for a 160-acre reference field, including smoothing and ROI masking stages.
- QA report generator must compute coverage, variance, and RMSE metrics with ≤ 0.5% deviation from analytical goldens across the regression suite.
- Recipe hashing must remain stable across platforms with zero mismatches detected in cross-platform regression runs.

## References
- [Testing & CI requirements](../SRS/sections/11_Testing_CI_CDPipelines.md)
- [Data model & storage requirements](../SRS/sections/08_Data_Model_Storage.md)
- [ADR-012: Multi-session and multi-PoseStream fusion](ADR-012-multi-session-posestream-fusion.md)
- [ADR-014: Interop for prescriptions and agronomic formats](ADR-014-interop-prescription-formats.md)
- [ADR-019: Provenance, audit, and QA](ADR-019-provenance-audit-qa.md)
