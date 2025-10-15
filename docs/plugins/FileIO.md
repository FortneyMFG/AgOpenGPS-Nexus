# File I/O Plugin Requirements (Draft)

## Overview

File I/O plugins handle import and export workflows for ISOXML, Shapefile, GeoJSON, CSV, and job bundles. They must normalize data per the Layer Registry, maintain provenance, and respect offline-first storage policies.

## Runtime Responsibilities

- Implement import pipelines for ISOXML TaskData, Shapefile/GeoJSON vectors, GeoTIFF/COG rasters, and CSV tabular data. Normalize CRS, units, and attribute schemas before writing layers or ledger entries, following the NX-113 ingest flow.【F:docs/SRS/sections/04_MappingLayers.md†L84-L106】【F:docs/ADR/ADR-014-interop-prescription-formats.md†L12-L56】
- Validate incoming layers against Layer Registry definitions. Reject unregistered IDs with actionable diagnostics referencing expected schema hashes.【F:docs/ADR/ADR-010-layer-registry-variable-rate.md†L33-L58】
- Support export bundles for jobs (layers, ledger, notes, presets) with deterministic folder layouts per ADR-030. Provide integrity manifests (hashes, metadata) for auditing.【F:docs/ADR/ADR-030-field-job-sessions.md†L33-L86】
- Coordinate with Telemetry Logging and Replay plugins to include log references in job bundles when requested.

## UX Requirements

- Offer import wizards that preview metadata (layer type, units, coverage area) and highlight validation issues before commit.
- Provide export dialogs with scope selection (session, job, season) and options for format (ISOXML, GeoPackage, CSV, Nexus bundle).
- Surface success/failure summaries with links to provenance entries and log files.

## Compatibility Notes

- Offline-first operation: imports/exports run locally and sync when connectivity is available. Operators can copy bundles via removable media without breaking provenance.
- Sessions replace legacy Run terminology in filenames and manifest metadata to align with ADR-041.
