# ADR-022: CRS, units, and precision policy

## Status
Drafting (target review window: 2025-12-20 week)

**Relevant Plugin(s):** Mapping, Variable Mapping, Rate Control, Telemetry Logging, File IO



## Context
Consistent coordinate reference systems (CRS), units, and numeric precision are foundational for fusion, interop, and analytics workflows. Without a project-wide policy, layers and PoseStreams risk accumulating distortion and rounding errors. ADR-022 codifies CRS defaults, precision tiers, and reprojection rules supporting ADR-010 layer registry, ADR-014 interop, and ADR-029 mapping kernel decisions.

## Decision
- Establish default CRS selection rules (e.g., WGS84 vs. per-field projections) with documented fallbacks when distortion thresholds are exceeded.
- Define numeric precision tiers (f16/f32/u16) and rounding policies for on-disk storage and in-memory processing across services.
- Provide unit normalization guidance and conversion utilities that integrate with registries and telemetry pipelines.
- Require reprojection audit logging for all imports/exports, ensuring transformations are traceable and debuggable.

## Consequences
- Spatial operations gain predictable distortion bounds and consistent units, improving analytics and replay accuracy.
- Enforcing conversions and audit logging adds processing overhead but prevents silent drift across deployments.
- Legacy data sets must pass through normalization pipelines, increasing migration workload but yielding higher data quality.

## Governance Updates
- **CRS decision tooling.** A guided decision tree with distortion heatmaps now ships alongside CLI tools that flag when distortion exceeds thresholds, recommending alternative CRS selections.
- **Normalization matrix.** [docs/reference/crs-normalization-matrix.md](../reference/crs-normalization-matrix.md)
  codifies storage, processing, and audit expectations for every spatial pipeline so
  pods implement ADR-022 consistently.
- **Operator education.** Training modules and visual overlays teach operators how CRS selection impacts analytics, driving better adoption of recommended projections.
- **Alerting.** Pipelines emit warnings when incoming data deviates from site-approved CRS, ensuring redaction workflows catch misconfigured sources.

## Validation
- CRS policy must select projections with ≤ 1 cm distortion across 640-acre test extents and document fallback strategies.
- Conversion utilities must pass regression suites covering 50 assets with ≤ 0.1% conversion error across imperial/metric cases.
- Reprojection audit hooks must emit structured telemetry for every transform, validated via automated import/export scenarios.

## References
- [Data model & storage requirements](../SRS/sections/08_Data_Model_Storage.md)
- [Communications & transports requirements](../SRS/sections/03_Comm_Transports.md)
- [ADR-010: Layer registry and variable-rate framework](ADR-010-layer-registry-variable-rate.md)
- [ADR-014: Interop for prescription and agronomic formats](ADR-014-interop-prescription-formats.md)
- [ADR-029: Mapping plugin architecture](ADR-029-mapping-plugin-architecture.md)
