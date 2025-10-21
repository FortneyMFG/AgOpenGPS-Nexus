# 76 — Geospatial Extensibility (Status: collecting proposals)

## Problem statement
Outline how third parties introduce new geospatial layer types, projections, and analytics while staying compatible with Core registries, tiling systems, and UI kernels.【F:docs/SRS/sections/7X_Mapping_Geospatial/71_Mapping_Kernel_Contracts.md†L1-L132】【F:docs/SRS/sections/9X_Frontends_Ops/94_Extensibility_Packaging_Updates.md†L17-L120】

## Requirements (from contributors)
- R-GEO-000 (MUST, projection support): Provide CRS utilities (EPSG codes, unit conversions) and enforce precision policies defined in ADR-022 so new layers maintain spatial fidelity.【F:docs/ADR/ADR-022-crs-units-precision-policy.md†L1-L44】【F:docs/SRS/sections/7X_Mapping_Geospatial/71_Mapping_Kernel_Contracts.md†L92-L132】
- R-GEO-001 (SHOULD, custom layer registration): Allow plugins to register custom geometry/attribute schemas with validation hooks and metadata so Core can tile, persist, and render them alongside built-in layers.【F:docs/SRS/sections/7X_Mapping_Geospatial/72_Mapping_Layers_Plugin.md†L12-L156】
- R-GEO-002 (MUST, edit tooling): Extend the shared LayerEditService to support custom geometry editors, attribute panels, and undo/redo semantics for plugin-defined layers.【F:docs/ADR/ADR-044_ZoneDrawingFramework.md†L29-L74】【F:docs/SRS/sections/9X_Frontends_Ops/91_UI_Shell_Layout.md†L90-L176】
- R-GEO-003 (SHOULD, data import/export): Enable custom import/export adapters that map to open formats while preserving provenance and schema version tags.【F:docs/SRS/sections/3X_Data_Storage/32-O5%20-%20Metadata-driven%20variable-rate%20layers.md†L32-L64】【F:docs/SRS/sections/3X_Data_Storage/32_Persistence_Formats.md†L94-L156】
- R-GEO-004 (MUST, capability discovery): Advertise new geospatial capabilities via plugin manifests with compatibility metadata so UI shells and automation can respond gracefully when providers are missing.【F:docs/SRS/sections/9X_Frontends_Ops/94_Extensibility_Packaging_Updates.md†L17-L120】
- R-GEO-005 (SHOULD, multi-machine consistency): Ensure custom layers synchronize across collaborative sessions using the same journal and conflict resolution rules as built-in layers.【F:docs/SRS/sections/7X_Mapping_Geospatial/72_Mapping_Layers_Plugin.md†L120-L188】【F:docs/SRS/sections/3X_Data_Storage/33_Offline_First_Sync.md†L66-L124】

## Current sentiment
Geospatial extensibility hinges on solid registry governance and shared editing tooling; contributors want ADR-022 precision policies and LayerEditService upgrades finalized before accepting third-party layer types into production builds.【F:docs/ADR/ADR-022-crs-units-precision-policy.md†L1-L44】【F:docs/SRS/sections/7X_Mapping_Geospatial/71_Mapping_Kernel_Contracts.md†L92-L132】
