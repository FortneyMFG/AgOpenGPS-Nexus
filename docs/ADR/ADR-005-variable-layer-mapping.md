# ADR-005: Metadata-driven variable-rate layer mapping and imports

## Status
Proposed

## Context
Legacy AgOpenGPS (V6) stores worked area, commanded rates, and agronomic overlays as binary section strips plus optional layer exports. Nexus currently renders coverage using a single binary mask and lacks a way to align section visualization, variable-rate controllers, and imported agronomic datasets under one schema. The SRS calls for metadata-driven layer catalogs, units registries, and schema hashes so multiple variable-rate layers can coexist safely across Core, UI, plugins, and firmware.【F:docs/SRS/sections/08_Data_Model_Storage.md†L11-L25】【F:docs/SRS/options/O-DATA-5_MetadataDrivenLayers.md†L6-L38】 It also requires the communications and plugin slices to support versioned layer transports, replay validation, and third-party extensibility.【F:docs/SRS/sections/03_Comm_Transports.md†L11-L45】【F:docs/SRS/sections/11_Testing_CI_CDPipelines.md†L11-L18】【F:docs/SRS/sections/12_Extensibility_Plugins.md†L11-L60】

Operators expect to import agronomic prescriptions (e.g., crop density grids, yield maps, soil zones) from GeoTIFF, shapefile, and ISOXML sources, have Nexus normalize them into layer definitions, and feed rate controllers that translate the data into commanded section outputs. Without a shared layer model, the importer cannot validate metadata or convert engineering units, and plugins cannot reliably consume or visualize these datasets.

## Decision
Adopt a metadata-driven layer architecture spanning Core, UI, and plugins:

- **Layer catalogue & registry** — Implement the SRS-mandated layer catalogue and units registry so every layer (coverage, commanded rate, actual rate, agronomic inputs) carries schema version, units, normalization hints, and quality metadata. Persist registry snapshots alongside field/machine profiles and exchange hashes via existing capability handshakes.【F:docs/SRS/options/O-DATA-5_MetadataDrivenLayers.md†L6-L38】【F:docs/SRS/sections/03_Comm_Transports.md†L11-L25】
- **Section visualization alignment** — Replace the single binary coverage rendering path with layer-aware map overlays that can stack on/off masks, numeric heatmaps, and derived layers while honouring quantization metadata and display ranges.【F:docs/SRS/options/O-DATA-5_MetadataDrivenLayers.md†L9-L29】【F:docs/SRS/sections/08_Data_Model_Storage.md†L11-L18】
- **External variable-map ingest** — Build import adapters that accept GeoTIFF, shapefile, ISOXML, and CSV agronomic layers, map them to registry entries, and perform unit normalization plus coordinate reprojection during ingest. Imported layers must include provenance metadata and schema hashes so replay/tests can flag mismatches.【F:docs/SRS/options/O-DATA-5_MetadataDrivenLayers.md†L30-L58】【F:docs/SRS/sections/11_Testing_CI_CDPipelines.md†L11-L18】
- **Plugin contracts** — Define plugin-facing APIs so rate controllers can request layer tiles along the vehicle path, convert agronomic prescriptions into commanded section rates, and stream actual/commanded feedback through the variable-rate PGN bridge when available.【F:docs/SRS/sections/12_Extensibility_Plugins.md†L14-L60】【F:docs/SRS/options/O-COMM-5_VariableRatePGNs.md†L4-L33】

## Consequences
- **Positive impacts**
  - Aligns coverage visualization, importer workflows, and rate-control plugins on a single schema, unlocking richer overlays and analytics.
  - Enables Nexus to reuse imported agronomic data for both simulation/replay and live application while meeting SRS traceability expectations.
  - Provides a foundation for future firmware/controllers to advertise supported layers without bespoke UI code.
- **Negative/mitigated impacts**
  - Introduces new registries and storage formats that existing tools must migrate toward; mitigated via compatibility readers for legacy section strips and staged feature flags per SRS guidance.【F:docs/SRS/sections/03_Comm_Transports.md†L36-L45】
  - Requires additional validation and replay fixtures; mitigated by expanding the deterministic layer replay tests mandated by the SRS.【F:docs/SRS/sections/11_Testing_CI_CDPipelines.md†L11-L18】
- **Follow-up actions**
  - Author protobuf/schema updates for the layer catalogue and plugin requests.
  - Extend the UI map and analytics panels to visualize multiple layer types with shared legends.
  - Implement import tooling for common agronomic formats and wire them into the Nexus migration wizard.
  - Deliver a variable-rate controller plugin that consumes the new layer APIs and emits commanded rates derived from imported maps.

## References
- [Section 03 — Communications & Transports](../SRS/sections/03_Comm_Transports.md)
- [Section 08 — Data Model & Storage](../SRS/sections/08_Data_Model_Storage.md)
- [Section 11 — Testing & CI/CD Pipelines](../SRS/sections/11_Testing_CI_CDPipelines.md)
- [Section 12 — Extensibility & Plugins](../SRS/sections/12_Extensibility_Plugins.md)
- [Option O-COMM-5 — Variable-rate PGN suite](../SRS/options/O-COMM-5_VariableRatePGNs.md)
- [Option O-DATA-5 — Metadata-driven variable-rate layers](../SRS/options/O-DATA-5_MetadataDrivenLayers.md)
