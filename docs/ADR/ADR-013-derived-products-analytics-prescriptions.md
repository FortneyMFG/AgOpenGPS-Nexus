# ADR-013: Derived products and prescription analytics

## Status
Drafting (target review window: 2025-12-02 week)

**Relevant Plugin(s):** Variable Mapping, Mapping, Rate Control, Telemetry Logging, Job Tasks



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

## Governance Updates
- **Human-in-the-loop QA.** Representative agronomic partners review quarterly derived-product updates using curated field datasets. Approvals and feedback are logged in the provenance system.
- **Reference datasets.** Public benchmark packs define acceptable variance thresholds per product type. CI enforces these bounds and blocks releases exceeding tolerance.
- **Change notification.** Recipe adjustments trigger alerts to dependent prescription services and include reproducibility bundles for verification before rollout.

## Amendment — 2025 architecture refresh (NX-190)

- Recipes must ship as manifest-signed YAML/JSON bundles with SHA-256 hashes recorded in the Layer Registry (ADR-010). Hashes accompany layer provenance so Report Builder exports and Profit analytics can verify inputs.
- The engine now accepts zone masks authored through the LayerEditService (ADR-044) as ROI filters. Recipes reference these masks by ID, and regression fixtures cover zone-enabled derivations to ensure deterministic clipping.
- QA datasets include season/job/session context pulled from ADR-041. Fixtures replay session metadata, proving that planned vs. actual comparisons and provenance survive reprocessing.
- ISOXML and external exchange flows (ADR-014, NX-165) embed recipe hashes and layer IDs to guarantee fidelity when exporting to task controllers or re-importing partner prescriptions.

## Validation
- Prescription derivations must complete within four minutes for a 160-acre reference field, including smoothing and ROI masking stages.
- QA report generator must compute coverage, variance, and RMSE metrics with ≤ 0.5% deviation from analytical goldens across the regression suite.
- Recipe hashing must remain stable across platforms with zero mismatches detected in cross-platform regression runs.

## References
- [Testing & CI requirements](../SRS/sections/9X_Frontends_Ops/96_Quality_Engineering_Release.md)
- [Data model & storage requirements](../SRS/sections/3X_Data_Storage/32_Persistence_Formats.md)
- [ADR-012: Multi-session and multi-PoseStream fusion](ADR-012-multi-session-posestream-fusion.md)
- [ADR-014: Interop for prescriptions and agronomic formats](ADR-014-interop-prescription-formats.md)
- [ADR-019: Provenance, audit, and QA](ADR-019-provenance-audit-qa.md)
