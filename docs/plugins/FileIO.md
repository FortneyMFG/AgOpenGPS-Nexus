# File I/O Plugin Requirements (Draft)

## Overview

File I/O plugins handle import and export workflows for ISOXML, Shapefile, GeoJSON, CSV, and job bundles. They must normalize data per the Layer Registry, maintain provenance, and respect offline-first storage policies.

## Runtime Responsibilities

- Implement import pipelines for ISOXML TaskData, Shapefile/GeoJSON vectors, GeoTIFF/COG rasters, and CSV tabular data. Normalize CRS, units, and attribute schemas before writing layers or ledger entries, following the NX-113 ingest flow.【F:docs/SRS/sections/7X_Mapping_Geospatial/72_Mapping_Layers_Plugin.md†L84-L106】【F:docs/SRS/sections/3X_Data_Storage/32-ADR-014 - Interop for prescription and agronomic formats.md†L12-L56】
- Validate incoming layers against Layer Registry definitions. Reject unregistered IDs with actionable diagnostics referencing expected schema hashes.【F:docs/SRS/sections/3X_Data_Storage/32-ADR-010 - Layer registry and variable-rate framework.md†L33-L58】
- Support export bundles for jobs (layers, ledger, notes, presets) with deterministic folder layouts per ADR-030. Provide integrity manifests (hashes, metadata) for auditing.【F:docs/SRS/sections/6X_Core_Domain_Services/62-ADR-030 - Field job sessions and lifecycle services.md†L33-L86】
- Coordinate with Telemetry Logging and Replay plugins to include log references in job bundles when requested.

## Implementation Status

- `Aog.Plugins.FileIO.FileIoSurfaceService` ingests delimited agronomic grids via `ExternalAgronomicMapIngestor` and exports normalized surfaces to GeoJSON feature collections with provenance metadata and bounding boxes. The wrapper providers referenced by the manifest delegate to this service so simulation flows can request import/export operations directly from plugin metadata.【F:Nexus SourceCode/src/Aog.Plugins/FileIO/FileIoSurfaceService.cs†L1-L198】【F:Nexus SourceCode/src/Aog.Plugins/FileIO/SurfaceExportProvider.cs†L1-L32】

### Example

```csharp
var service = new FileIoSurfaceService();
var layer = service.ImportSurfaceFromDelimitedFile(
    path: "~/imports/prescription.csv",
    layerId: "layer:vr.sample",
    kind: "rate",
    units: "kg/ha",
    cellSizeMeters: 12,
    source: "import:legacy",
    transform: "normalize:nearest",
    createdBy: "user:demo");

await service.ExportSurfaceToGeoJsonAsync(layer, "~/exports/prescription.geojson");
```

## UX Requirements

- Offer import wizards that preview metadata (layer type, units, coverage area) and highlight validation issues before commit.
- Provide export dialogs with scope selection (session, job, season) and options for format (ISOXML, GeoPackage, CSV, Nexus bundle).
- Surface success/failure summaries with links to provenance entries and log files.

## Compatibility Notes

- Offline-first operation: imports/exports run locally and sync when connectivity is available. Operators can copy bundles via removable media without breaking provenance.
- Sessions replace legacy Run terminology in filenames and manifest metadata to align with ADR-041.
