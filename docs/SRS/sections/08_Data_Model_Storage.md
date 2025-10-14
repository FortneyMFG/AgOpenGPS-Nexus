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
- R-DATA-015 (MUST, PoseStream persistence): Record PoseStream and SectionState vector logs with monotonic ordering, compression hints, and replay indexes so deterministic replays can rebuild field state without floating drift.
- R-DATA-016 (MUST, tile store governance): Define chunked tile layouts, cell sizes, codecs, and compaction/append strategies for quantitative layers so storage remains bounded while preserving engineering fidelity.
- R-DATA-017 (SHOULD, multi-session fusion): Document spatial/temporal alignment rules, CRS policies, and provenance chaining when merging PoseStreams or layers across sessions or implements so analytics stay auditable.
- R-DATA-018 (SHOULD, provenance registry): Capture registry hashes, quality scores, and audit metadata alongside stored tiles and logs so derived products can expose traceable lineage through plugins and exports.
- R-DATA-026 (MUST, spatial constraint schema): Persist boundary, headland, keep-out, and work-disabled zones as vector polygons (with holes) plus per-zone metadata (label, priority, enabled flag, provenance) in the geometry layer store so Core and plugins share authoritative constraints.
- R-DATA-027 (MUST, buffered footprints): Support independent drive/work buffers per zone and store the effective buffered polygon so guidance, section control, and visualization can evaluate constraints without recomputing offsets at runtime.
- R-DATA-028 (SHOULD, indexed queries): Maintain an R-tree or equivalent spatial index over zones to keep PoseStream zone-mask lookups and map rendering within real-time CPU budgets even with dozens of polygons.
- R-DATA-029 (MUST, job metadata schema): Publish a versioned `aog.job.v1` metadata document that records job identity, timestamps, spatial hints, asset references, preset/layout links, and provenance so Core, UI, and plugins can coordinate lifecycle actions and resume behavior deterministically.【F:docs/ADR/ADR-030-field-job-sessions.md†L13-L49】
- R-DATA-030 (MUST, job store layout): Maintain a canonical `/Jobs/<Job>/` filesystem layout with `job.json`, `Resume.txt`, and a structured `data/` subtree for boundaries, coverage, prescriptions, and attachments so import/export tooling and Drive-In detection stay interoperable.【F:docs/ADR/ADR-030-field-job-sessions.md†L19-L63】
- R-DATA-031 (SHOULD, job journaling): Journal coverage tiles and asset edits with crash-safe checkpoints and autosave triggers so replays and recovery flows restore deterministic state after interruptions.【F:docs/ADR/ADR-030-field-job-sessions.md†L70-L86】
- R-DATA-032 (SHOULD, preset/layout provenance): Record preset selections, layout version links, and snapshot diffs alongside job metadata so historical runs capture the equipment context used to produce coverage and rate outputs.【F:docs/ADR/ADR-030-presets-and-layout-linking.md†L11-L36】

### R-CRS — Coordinate reference & precision
- R-DATA-019 (MUST, canonical CRS policy): Define the default CRS (WGS84) and rules for switching to projected systems (e.g., UTM per field) so PoseStreams, tile stores, and exports remain aligned without manual overrides.
- R-DATA-020 (MUST, numeric precision): Specify numeric storage types (f16/f32/u16) and rounding policies per layer type to balance fidelity with storage budgets across vector logs and tiles.
- R-DATA-021 (SHOULD, unit normalization): Publish a canonical units catalog (e.g., pop/ac, gal/ac, kg/ha) and conversion rules so ingest/export flows normalize values deterministically.
- R-DATA-022 (SHOULD, reprojection governance): Require import/export tooling to log CRS transforms, precision loss, and bounding boxes whenever data is reprojected or resampled.

### R-DATA — Lifecycle & performance guardrails
- R-DATA-023 (MUST, retention policy): Document minimum on-device retention windows, compaction triggers, and archival workflows so storage usage stays predictable across rigs.
- R-DATA-024 (SHOULD, background maintenance): Define background jobs (tile compaction, log roll-up, codec upgrades) with CPU/IO budgets to avoid starving live ingest.
- R-DATA-025 (SHOULD, deterministic transforms): Mandate deterministic transforms between PoseStream vector logs and TileStore outputs so replays reproduce identical hashes given fixture inputs.

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
- Spatial constraint zones should reuse the same geometry store and provenance logging so interop, replay, and automation consumers receive identical data regardless of import source or deployment topology.

## Upcoming ADR coverage
- **ADR-007 PoseStream & SectionState architecture** will codify the unified pose timeline, diffed SectionState records, and replay determinism needed to satisfy R-DATA-015 and align with transport and plugin requirements.【F:docs/ADR/ADR-roadmap.md†L67-L73】
- **ADR-009 Persistence & storage** will finalize the vector log + tile store architecture governed by R-DATA-016 and R-DATA-018, including codecs, compaction, and crash-safety policies.【F:docs/ADR/ADR-roadmap.md†L111-L117】
- **ADR-012 Multi-session & fusion** will settle the merge semantics and provenance chain expected by R-DATA-017 when combining historical PoseStreams or agronomic layers.【F:docs/ADR/ADR-roadmap.md†L123-L129】
- **ADR-014 Interop formats** will map the internal tile/log model to export/import standards while preserving schema hashes and units per R-DATA-003 and R-DATA-014.【F:docs/ADR/ADR-roadmap.md†L135-L141】
- **ADR-027 Spatial constraints & zone policies** delivers the zone storage, buffering, and provenance needed for R-DATA-026…R-DATA-028 while binding pose masks to storage.【F:docs/ADR/ADR-roadmap.md†L27-L41】
- **ADR-029 Mapping plugin architecture** splits the geospatial kernel from plugin engines so R-DATA-010…R-DATA-025 remain satisfied without bloating Core deployments.【F:docs/ADR/ADR-roadmap.md†L30-L36】
- **ADR-030 Field job sessions & lifecycle services** implements the job metadata, journaling, and filesystem layout defined in R-DATA-029…R-DATA-031.【F:docs/ADR/ADR-roadmap.md†L38-L44】
- **ADR-030 (Presets & layout linking)** captures preset/layout provenance expectations under R-DATA-029…R-DATA-032 and ties them to task orchestration.【F:docs/ADR/ADR-roadmap.md†L46-L52】
- **ADR-022 CRS/units & precision policy** will define canonical CRS defaults, numeric precision tiers, and unit normalization so PoseStreams, tiles, and exports stay aligned under R-DATA-019…R-DATA-022.【F:docs/ADR/ADR-roadmap.md†L145-L151】
- **ADR-023 Session/job model & provenance graph** will wire sessions and provenance metadata together, satisfying R-DATA-017…R-DATA-032 and linking analytics outputs to storage.【F:docs/ADR/ADR-roadmap.md†L153-L159】
- **ADR-025 Data lifecycle & retention** codifies retention windows, compaction triggers, and archival workflows required by R-DATA-013 and R-DATA-023…R-DATA-031.【F:docs/ADR/ADR-roadmap.md†L165-L171】
- **ADR-026 Performance budgets** sets the CPU/IO and storage targets that govern background maintenance jobs and replay determinism under R-DATA-024…R-DATA-031.【F:docs/ADR/ADR-roadmap.md†L173-L179】

## Open questions
- Which artifacts must be backward compatible for existing rigs?
- How do we tag data with firmware/app versions for troubleshooting?
