# Data Model & Storage (Status: collecting proposals)

## Problem statement
Clarify how fields, boundaries, tram lines, tiles, and telemetry are stored, synchronized, and exchanged between tools.

## Requirements (from contributors)
- R-DATA-000 (MUST, current-AgOpenGPS): Preserve the field streamer serialization that reads/writes boundaries, flags, tram lines, recorded paths, and worked area files.【F:SourceCode/AgOpenGPS.Core/Streamers/Field/FieldStreamer.cs†L7-L107】
- R-DATA-001 (MUST, current-AgOpenGPS): Continue shipping SQLite-based persistence used for mapping and section records via WinForms packages.【F:SourceCode/GPS/AgOpenGPS.csproj†L39-L48】
- R-DATA-002 (SHOULD, current-AgOpenGPS): Keep shared libraries (AgOpenGPS.Core + AgLibrary) responsible for geometry and streaming logic.【F:SourceCode/AgOpenGPS.Core/AgOpenGPS.Core.csproj†L7-L15】
- R-DATA-003 (SHOULD): Define export/import formats (ISOXML, shapefile, GeoJSON) with clear versioning.
- R-DATA-010 (MUST, proposed-variable-layer): Preserve binary section coverage while storing additional layer geometry, accumulators, and quality metadata per section/row.【F:docs/SRS/options/O-DATA-5_MetadataDrivenLayers.md†L1-L29】
- R-DATA-011 (MUST, proposed-variable-layer): Ship a layer catalogue, units registry, and configuration schema that can be extended without code changes while keeping exports (CSV/GeoTIFF) consistent.【F:docs/SRS/options/O-DATA-5_MetadataDrivenLayers.md†L30-L58】
- R-DATA-012 (SHOULD, proposed-variable-layer): Bundle chunked, compressed map tiles with quantization metadata and schema hashes so replay and analytics can reconstruct engineering values exactly.【F:docs/SRS/options/O-DATA-5_MetadataDrivenLayers.md†L59-L78】
- R-DATA-004 (COULD): Add compression and delta sync for large telemetry sets without breaking existing file readers.
- R-DATA-013 (SHOULD, retention): Define minimum retention/archival windows for agronomic history (e.g., three seasons accessible offline, long-term archives exportable to cold storage) so future requirements inherit a shared performance envelope.
- R-DATA-014 (SHOULD, schema integrity): Clarify how schema hashes flow through export/import tooling (including mismatch detection and operator prompts) to prevent silent drift between machines running different Core or firmware versions.

## Options
- O-DATA-0: Status quo — Local file storage with SQLite + custom binary/JSON field artifacts.
- O-DATA-1: Formalize a documented schema (e.g., protobuf) for all field assets.
- O-DATA-2: Move storage to a centralized database (PostgreSQL/PostGIS) with sync clients.
- O-DATA-3: Use cloud object storage for heavy assets with local caching.
- O-DATA-4: Introduce versioned packages (zip bundles) for field moves.
- O-DATA-5: [Metadata-driven variable-rate layers](../options/O-DATA-5_MetadataDrivenLayers.md) — Catalog + storage for multi-layer telemetry.

## Comparison (quick matrix)
| Option | Pros | Cons | Risks | Borrow from existing |
|---|---|---|---|---|
| O-DATA-0 | Works offline, proven by users | Fragmented formats | Hard to audit schema | Field streamer code |
| O-DATA-1 | Shared contracts | Migration effort | All consumers must upgrade | Streamer interfaces |
| O-DATA-2 | Central management | Requires server infra | Connectivity outages | SQLite schema seeds |
| O-DATA-3 | Offloads storage | Needs internet | Bandwidth costs | Field backups |
| O-DATA-4 | Easier distribution | Packaging workflow overhead | Version skew | Publish pipeline |
| O-DATA-5 | Rich telemetry without breaking legacy files | Larger schema + tooling investment | Migration errors during rollout | Field streamer interfaces + layer catalog plan |

## Evaluation criteria
Offline use, storage footprint, interoperability, migration effort, tooling availability.

## Current sentiment
- Keep local files operational while documenting how they evolve and what metadata is missing for machine-to-machine exchange.
- Layer catalog work should land with export tooling and schema hashes before any centralized storage move is reconsidered.【F:docs/SRS/options/O-DATA-5_MetadataDrivenLayers.md†L59-L78】【F:docs/SRS/options/O-TEST-4_LayerReplayCI.md†L7-L27】

## Open questions
- Which artifacts must be backward compatible for existing rigs?
- How do we tag data with firmware/app versions for troubleshooting?
