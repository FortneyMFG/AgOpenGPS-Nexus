# ADR-014: Interop for prescription and agronomic formats

## Status
Drafting (target review window: 2025-11-26 week)

**Relevant Plugin(s):** Variable Mapping, Rate Control, Mapping, Planter Monitor, File IO, Job Tasks


## Context
Nexus must import and export industry-standard agronomic formats—ISOXML TaskData, GeoTIFF/COG rasters, Shapefile/GeoPackage vectors, MBTiles tilesets—while honoring units, CRS, and attribute mapping. Current pipelines rely on bespoke scripts that lose metadata and introduce spatial error. ADR-014 defines canonical interop behavior aligned with ADR-010 layer registry metadata, ADR-022 CRS policy, and ADR-013 derivation outputs.

## Decision
- Adopt ISOXML TaskData as the primary vector prescription format, with GeoPackage as a fallback and GeoTIFF/COG for raster outputs, ensuring consistent naming and attribute conventions.
- Normalize units and CRS according to ADR-022, including audit logging for every transformation and conversion.
- Provide import/export tooling that maps attributes into Nexus layer metadata and captures provenance links for ADR-019.
- Supply sample fixtures and validation harnesses to maintain compatibility across releases and detect regressions.

## Consequences
- Operators gain predictable import/export flows across major agronomic systems, improving interoperability and data retention.
- Enforcing canonical formats and audit logging increases tooling complexity but mitigates field surprises and compliance risks.
- Existing scripts and UI flows must adapt to new schemas and validation checks, requiring documentation and training updates.

## Governance Updates
- **Compatibility matrix.** The data interoperability team maintains a vendor-format matrix capturing quirks, firmware levels, and regression status. Matrix updates accompany each release and inform operator documentation.
- **Schema diff automation.** Import/export tooling now emits structured diffs when incoming data deviates from expected schemas, surfacing actionable warnings for operators.
- **Regression rehearsals.** Firmware releases trigger replay of the compatibility suite before publication, with failures blocking distribution until addressed.

## Amendment — 2025 architecture refresh (NX-190)

- ISOXML import/export pipelines now align with the Job lifecycle defined in [ADR-030](ADR-030-field-job-sessions.md). TaskData exports bundle job metadata, session hashes, and layer IDs so remote controllers preserve provenance when re-imported.
- Planned rate layers referenced in ISOXML use `vr.planned.*` catalog IDs from ADR-010. The Rate Control plugin consumes the same IDs at runtime, ensuring Core and hardware stay in lockstep.
- External ingest (NX-113) validates GeoTIFF/COG rasters and GeoJSON/GeoPackage vectors against the Layer Registry before writing to storage. Failed validations surface actionable diagnostics referencing expected schema hashes.
- Report Builder templates (ADR-051) depend on interop metadata to assemble crop and profit summaries. Exports now include recipe hashes from ADR-013 to satisfy compliance audits.

## Validation
- ISOXML TaskData importer/exporter must retain 100% of task attributes with ≤ 2 cm spatial error against canonical fixtures.
- GeoTIFF/COG pipelines must preserve raster statistics (mean, stdev) within 0.2% after compression/decompression across sample datasets.
- Interop audit log must capture CRS transformations and unit conversions for every import/export, verified via automated scenarios.

## References
- [Communications & transports requirements](../4X_Interprocess_Communications/42_Transports.md)
- [Data model & storage requirements](../3X_Data_Storage/32_Persistence_Formats.md)
- [ADR-010: Layer registry and variable-rate framework](ADR-010-layer-registry-variable-rate.md)
- [ADR-013: Derived products and prescription analytics](ADR-013-derived-products-analytics-prescriptions.md)
- [ADR-022: CRS, units, and precision policy](ADR-022-crs-units-precision-policy.md)
