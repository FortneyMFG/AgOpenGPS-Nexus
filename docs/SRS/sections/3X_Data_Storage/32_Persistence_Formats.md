# 32 — Persistence & Formats
*(Status: review)*

**Section ID:** 32
**Version:** 0.2.0
**Editors:** @nexus-docs-team
**Last Updated:** 2025-02-14
**Related Sections:** [31 — Domain Data Model](31_Domain_Data_Model.md), [33 — Offline-first & Sync](33_Offline_First_Sync.md), [34 — Backup, Retention & Archival](34_Backup_Retention_Archival.md)
**Upstream Dependencies:** [ADR-009](32-ADR-009 - PoseStream vector logs and layer TileStore persistence.md), [ADR-012](32-ADR-012 - Multi-session and multi-PoseStream fusion.md), [ADR-014](32-ADR-014 - Interop for prescription and agronomic formats.md), [ADR-022](32-ADR-022 - CRS units and precision policy.md)
**Downstream Impacts:** TileStore implementation, export/import tooling, analytics pipelines, retention services, plugin schema registries

---

## 32.1 Purpose & Scope

This section defines how Nexus stores, validates, synchronises, and exchanges agronomic data artifacts, including PoseStream vector logs, layer tiles, job metadata, and export packages. It captures requirements for schema governance, codecs, provenance tracking, and interoperability with industry standards so that offline rigs and cloud services share deterministic, verifiable data sets.

---

## 32.2 Context

- Core currently ships SQLite databases, binary section coverage, and JSON metadata used by the legacy AgOpenGPS applications.【F:docs/SRS/sections/3X_Data_Storage/32_Persistence_Formats.md†L7-L64】
- ADR-009…ADR-014 introduce TileStore, PoseStream replay, and interop bridges that expand beyond legacy formats while ensuring deterministic migrations.【F:docs/SRS/sections/3X_Data_Storage/32-ADR-009 - PoseStream vector logs and layer TileStore persistence.md†L9-L73】【F:docs/SRS/sections/3X_Data_Storage/32-ADR-014 - Interop for prescription and agronomic formats.md†L10-L45】
- Plugins rely on registry hashes, schema references, and provenance metadata to produce analytics layers that align with Core-managed storage constraints.【F:docs/SRS/sections/3X_Data_Storage/32-O5 - Metadata-driven variable-rate layers.md†L1-L78】

Assumptions:
- Offline rigs must operate with local files first, then sync via USB, mesh, or intermittent internet connectivity.
- Legacy field streamer files remain readable during migration phases and serve as import/export compatibility baselines.

---

## 32.3 Legacy Comparison

| Area / Theme | Legacy Behavior | Identified Limitation | Modernization Opportunity | Reference / Source |
|---------------|-----------------|------------------------|---------------------------|--------------------|
| Storage Layout | Field streamer binaries + ad-hoc JSON folders. | Implicit schema, poor provenance. | Versioned JSON schemas + TileStore packages with provenance hashes. | [FieldStreamer.cs](../../SourceCode/AgOpenGPS.Core/Streamers/Field/FieldStreamer.cs) |
| Synchronisation | Manual file copy between rigs. | No delta sync, risk of data drift. | Introduce manifest-driven sync with compression and hash validation. | [Section 33](33_Offline_First_Sync.md) |
| Interoperability | Bespoke scripts for ISOXML/Shape/GeoJSON. | Metadata loss, CRS mismatch. | Formal export/import pipelines with schema hashes and CRS policy. | [ADR-014](32-ADR-014 - Interop for prescription and agronomic formats.md) |

---

## 32.4 Definitions

| Term | Definition |
|------|-------------|
| TileStore | Chunked, compressed storage for quantitative layer tiles with provenance metadata.【F:docs/SRS/sections/3X_Data_Storage/32-ADR-009 - PoseStream vector logs and layer TileStore persistence.md†L35-L63】 |
| PoseStream | Ordered vector log capturing pose and SectionState records for deterministic replay.【F:docs/SRS/sections/3X_Data_Storage/32-ADR-009 - PoseStream vector logs and layer TileStore persistence.md†L9-L45】 |
| Schema Hash | Deterministic fingerprint attached to stored/exported artifacts to detect version skew.【F:docs/SRS/sections/3X_Data_Storage/32_Persistence_Formats.md†L25-L156】 |
| Layer Catalogue | Registry describing available layer kinds, units, codecs, and schema references.【F:docs/SRS/sections/3X_Data_Storage/32-O5 - Metadata-driven variable-rate layers.md†L1-L78】 |
| Spatial Constraint Store | Vector repository for boundaries, headlands, and keep-out zones shared by Core and plugins.【F:docs/SRS/sections/7X_Mapping_Geospatial/72-ADR-027 - Spatial Constraints & Zone Policies.md†L13-L53】 |
| Job Package | Structured filesystem layout (`/Jobs/<Job>/`) with metadata, resume info, and data subtrees.【F:docs/SRS/sections/6X_Core_Domain_Services/62-ADR-030 - Field job sessions and lifecycle services.md†L19-L86】 |

---

> **Requirement Grammar (RFC-2119):**
> - **MUST / MUST NOT** = mandatory; test must exist.
> - **SHOULD / SHOULD NOT** = strong recommendation; justify exceptions.
> - **MAY** = optional; document enabling conditions.
>
> **Clarity Checklist:** Avoid weak words: *fast, robust, user-friendly, handle, support, adequate,* etc.
> Prefer measurable forms: *“≤ 250 ms p95,” “error rate < 0.1%,” “99.5% success over 10k trials.”*
> Each requirement: single behavior, single actor, single condition, single metric.

## 32.5 Requirements

### 32.5.1 Summary Requirements

| ID | Priority | Category | Summary | Source / C-IDs | Key Metrics / Verification |
|----|-----------|-----------|---------|-----------------|-----------------------------|
| R-32000 | MUST | Compatibility | Preserve legacy field streamer serialization for backwards compatibility while documenting migrations. | R-DATA-000, R-DATA-001 | Regression suite covering import/export parity for legacy rigs.【F:docs/SRS/sections/3X_Data_Storage/32_Persistence_Formats.md†L7-L25】 |
| R-32001 | MUST | Schema Governance | Publish and enforce JSON schema + registry hashes for all persisted entities and layers. | R-DATA-010…R-DATA-018 | Schema CI verifying hashes + registry catalogue integrity.【F:docs/SRS/sections/3X_Data_Storage/32-O5 - Metadata-driven variable-rate layers.md†L1-L78】 |
| R-32002 | MUST | Provenance | PoseStream logs and TileStore tiles must embed monotonic ordering, provenance hashes, and compression metadata for deterministic replay. | R-DATA-015, R-DATA-016, R-DATA-025 | Replay harness ensures byte-for-byte deterministic outputs across 10k frames.【F:docs/SRS/sections/3X_Data_Storage/32_Persistence_Formats.md†L26-L78】 |
| R-32003 | SHOULD | Interoperability | Import/export pipelines for ISOXML, shapefile/GeoPackage, GeoTIFF/COG must retain metadata with ≤2 cm spatial error. | R-DATA-003, R-DATA-014 | Fixture-driven geospatial diff tests; CRS precision audit. 【F:docs/SRS/sections/3X_Data_Storage/32-ADR-014 - Interop for prescription and agronomic formats.md†L10-L45】 |
| R-32004 | MUST | Lifecycle & Retention | Document and enforce retention windows, compaction triggers, and archival workflows for jobs, telemetry, and layers. | R-DATA-013, R-DATA-023 | Retention planner integration tests; audit log review. 【F:docs/SRS/sections/3X_Data_Storage/34-ADR-025 - Data lifecycle and retention policy.md†L10-L58】 |
| R-32005 | SHOULD | Performance | Provide indexed spatial queries and background maintenance jobs that meet CPU/IO budgets. | R-DATA-024, R-DATA-028 | Benchmark harness with ≤ 250 ms p95 zone lookups; maintenance job telemetry. 【F:docs/SRS/sections/7X_Mapping_Geospatial/72-ADR-027 - Spatial Constraints & Zone Policies.md†L13-L53】 |
| R-32006 | SHOULD | Analytics & Extensions | Support plugin-owned schemas (yield, cost, genetics, risk, weather) with provenance linking back to jobs/sessions. | R-DATA-041…R-DATA-048 | Schema registry tests verifying plugin payload serialization and provenance references.【F:docs/SRS/sections/7X_Mapping_Geospatial/72-ADR-049 - Yield & Analytics Plugin.md†L21-L52】 |
| R-32007 | MUST | Inventory & Reporting | Persist inventory ledger, regulatory exports, and report templates with deterministic manifests. | R-DATA-049…R-DATA-053 | CLI regression suite verifying manifest hashes and export fidelity.【F:docs/plugins/Regulatory.md†L1-L140】 |
| R-32008 | SHOULD | Presets & Session Provenance | Record preset/layout selections, crop history, and session weather snapshots tied to job metadata. | R-DATA-032, R-DATA-041, R-DATA-042 | Session lifecycle tests validating provenance fields and snapshot serialization.【F:docs/SRS/sections/9X_Frontends_Ops/91-ADR-032 - Presets and Layout Linking for Equipment Workflows.md†L7-L33】 |

### 32.5.2 Detailed Requirement Catalogue

| ID | Priority | Category | Summary | Source / Reference |
|----|-----------|-----------|---------|--------------------|
| R-DATA-000 | MUST | Compatibility | Preserve field streamer serialization for boundaries, flags, tram lines, recorded paths, worked area files.【F:SourceCode/AgOpenGPS.Core/Streamers/Field/FieldStreamer.cs†L7-L107】 |
| R-DATA-001 | MUST | Compatibility | Continue shipping SQLite persistence for mapping and section records via WinForms packages.【F:SourceCode/GPS/AgOpenGPS.csproj†L39-L48】 |
| R-DATA-002 | SHOULD | Architecture | Keep AgOpenGPS.Core + AgLibrary responsible for geometry and streaming logic.【F:SourceCode/AgOpenGPS.Core/AgOpenGPS.Core.csproj†L7-L15】 |
| R-DATA-003 | SHOULD | Interop | Define export/import formats (ISOXML, shapefile, GeoJSON) with clear versioning.【F:docs/SRS/sections/3X_Data_Storage/32_Persistence_Formats.md†L21-L78】 |
| R-DATA-004 | COULD | Performance | Add compression and delta sync for large telemetry sets without breaking existing readers.【F:docs/SRS/sections/3X_Data_Storage/32_Persistence_Formats.md†L79-L120】 |
| R-DATA-010 | MUST | Layers | Preserve binary section coverage while storing additional layer geometry, accumulators, quality metadata per section/row.【F:docs/SRS/sections/3X_Data_Storage/32-O5 - Metadata-driven variable-rate layers.md†L1-L29】 |
| R-DATA-011 | MUST | Layers | Ship layer catalogue, units registry, configuration schema extendable without code changes while keeping exports consistent.【F:docs/SRS/sections/3X_Data_Storage/32-O5 - Metadata-driven variable-rate layers.md†L30-L58】 |
| R-DATA-012 | SHOULD | Layers | Bundle chunked, compressed map tiles with quantization metadata and schema hashes for deterministic analytics.【F:docs/SRS/sections/3X_Data_Storage/32-O5 - Metadata-driven variable-rate layers.md†L59-L78】 |
| R-DATA-013 | SHOULD | Retention | Define minimum retention/archival windows for agronomic history with offline accessibility.【F:docs/SRS/sections/3X_Data_Storage/32_Persistence_Formats.md†L79-L156】 |
| R-DATA-014 | SHOULD | Interop | Clarify schema hash flow through export/import tooling to detect mismatches.【F:docs/SRS/sections/3X_Data_Storage/32_Persistence_Formats.md†L80-L156】 |
| R-DATA-015 | MUST | Replay | Record PoseStream and SectionState vector logs with monotonic ordering, compression hints, replay indexes.【F:docs/SRS/sections/3X_Data_Storage/32_Persistence_Formats.md†L26-L78】 |
| R-DATA-016 | MUST | TileStore | Define chunked tile layouts, cell sizes, codecs, compaction strategies for quantitative layers.【F:docs/SRS/sections/3X_Data_Storage/32_Persistence_Formats.md†L26-L78】 |
| R-DATA-017 | SHOULD | Fusion | Document spatial/temporal alignment rules, CRS policies, provenance chaining for multi-session fusion.【F:docs/SRS/sections/3X_Data_Storage/32_Persistence_Formats.md†L78-L156】 |
| R-DATA-018 | SHOULD | Registry | Capture registry hashes, quality scores, audit metadata alongside tiles/logs.【F:docs/SRS/sections/3X_Data_Storage/32_Persistence_Formats.md†L26-L156】 |
| R-DATA-019 | MUST | CRS | Define default CRS (WGS84) and rules for projected systems (UTM per field).【F:docs/SRS/sections/3X_Data_Storage/32_Persistence_Formats.md†L121-L156】 |
| R-DATA-020 | MUST | Precision | Specify numeric storage types and rounding policies per layer. 【F:docs/SRS/sections/3X_Data_Storage/32_Persistence_Formats.md†L121-L156】 |
| R-DATA-021 | SHOULD | Units | Publish canonical units catalog and conversion rules. 【F:docs/SRS/sections/3X_Data_Storage/32_Persistence_Formats.md†L121-L156】 |
| R-DATA-022 | SHOULD | Reprojection | Require tooling to log CRS transforms, precision loss, bounding boxes when reprojecting data.【F:docs/SRS/sections/3X_Data_Storage/32_Persistence_Formats.md†L121-L156】 |
| R-DATA-023 | MUST | Retention | Document on-device retention windows, compaction triggers, archival workflows.【F:docs/SRS/sections/3X_Data_Storage/32_Persistence_Formats.md†L121-L156】 |
| R-DATA-024 | SHOULD | Maintenance | Define background jobs (tile compaction, log roll-up, codec upgrades) with CPU/IO budgets.【F:docs/SRS/sections/3X_Data_Storage/32_Persistence_Formats.md†L121-L156】 |
| R-DATA-025 | SHOULD | Determinism | Mandate deterministic transforms between PoseStream logs and TileStore outputs.【F:docs/SRS/sections/3X_Data_Storage/32_Persistence_Formats.md†L26-L78】 |
| R-DATA-026 | MUST | Spatial Constraints | Persist zones as vector polygons with metadata and provenance.【F:docs/SRS/sections/7X_Mapping_Geospatial/72-ADR-027 - Spatial Constraints & Zone Policies.md†L13-L53】 |
| R-DATA-027 | MUST | Buffered Footprints | Support independent drive/work buffers per zone and persist buffered polygons.【F:docs/SRS/sections/7X_Mapping_Geospatial/72-ADR-027 - Spatial Constraints & Zone Policies.md†L13-L20】 |
| R-DATA-028 | SHOULD | Spatial Indexing | Maintain R-tree or equivalent spatial index for zones to meet real-time budgets.【F:docs/SRS/sections/7X_Mapping_Geospatial/72-ADR-027 - Spatial Constraints & Zone Policies.md†L13-L21】 |
| R-DATA-029 | MUST | Job Metadata | Publish versioned `aog.job.v1` recording identity, timestamps, spatial hints, provenance.【F:docs/SRS/sections/6X_Core_Domain_Services/62-ADR-030 - Field job sessions and lifecycle services.md†L13-L63】 |
| R-DATA-030 | MUST | Job Store Layout | Maintain canonical `/Jobs/<Job>/` layout with `job.json`, `Resume.txt`, structured `data/` subtree.【F:docs/SRS/sections/6X_Core_Domain_Services/62-ADR-030 - Field job sessions and lifecycle services.md†L19-L63】 |
| R-DATA-031 | SHOULD | Journaling | Journal coverage tiles and asset edits with crash-safe checkpoints and autosave triggers.【F:docs/SRS/sections/6X_Core_Domain_Services/62-ADR-030 - Field job sessions and lifecycle services.md†L70-L86】 |
| R-DATA-032 | SHOULD | Presets/Layouts | Record preset selections, layout version links, snapshot diffs alongside job metadata.【F:docs/SRS/sections/9X_Frontends_Ops/91-ADR-032 - Presets and Layout Linking for Equipment Workflows.md†L7-L33】 |
| R-DATA-040 | MUST | Zone Editing | Persist `LayerEditEvent.v1` journals with deterministic hashes, actor metadata, undo/redo pointers.【F:docs/SRS/sections/7X_Mapping_Geospatial/72-ADR-044 - Zone Drawing Framework.md†L29-L74】 |
| R-DATA-041 | MUST | Crop History | Extend `Field.v1` with `cropTypeHistory[]` entries referencing crop layers, status, year, source.【F:docs/SRS/sections/7X_Mapping_Geospatial/72-ADR-045 - Crop Type Plugin & Layers.md†L41-L55】 |
| R-DATA-042 | MUST | Session Weather | Store weather samples within `Session.v1` and emit diffs on updates.【F:docs/SRS/sections/7X_Mapping_Geospatial/72-ADR-053 - Weather & Environment Plugin.md†L33-L44】 |
| R-DATA-043 | SHOULD | Plugin Attribute Schemas | Allow layers to reference plugin-owned attribute schemas for validation. 【F:docs/SRS/sections/7X_Mapping_Geospatial/72-ADR-044 - Zone Drawing Framework.md†L29-L74】 |
| R-DATA-044 | MUST | Genetics Records | Publish `GeneticsPlan.v1` and `GeneticsVariety.v1` schemas capturing trait metadata and provenance. 【F:docs/SRS/sections/7X_Mapping_Geospatial/72-ADR-046 - Genetics Plugin & Layers.md†L21-L66】 |
| R-DATA-045 | MUST | Yield Layers | Define metadata for yield layers including smoothing, calibration, aggregation blocks. 【F:docs/SRS/sections/7X_Mapping_Geospatial/72-ADR-049 - Yield & Analytics Plugin.md†L21-L52】【F:schemas/YieldActual.v1.json†L38-L241】【F:schemas/YieldMoisture.v1.json†L38-L204】【F:schemas/YieldTestWeight.v1.json†L38-L204】 |
| R-DATA-046 | MUST | Cost & Profit | Introduce `CostRecord.v1` and `ProfitLayer.v1` validated by Core but owned by plugins. 【F:docs/SRS/sections/7X_Mapping_Geospatial/72-ADR-050 - Cost & Profit Plugin.md†L21-L52】 |
| R-DATA-047 | SHOULD | Risk Overlays | Store risk layers with severity scales, timestamps, observer IDs linked to sessions. 【F:docs/SRS/sections/7X_Mapping_Geospatial/72-ADR-052 - Field Health & Risk Plugin.md†L21-L44】【F:schemas/FieldHealthRiskLayer.v1.json†L1-L140】 |
| R-DATA-048 | SHOULD | Weather Overlays | Capture `weather.overlay` grids with units, resolution, source metadata aligned with snapshots. 【F:docs/SRS/sections/7X_Mapping_Geospatial/72-ADR-053 - Weather & Environment Plugin.md†L21-L49】 |
| R-DATA-049 | SHOULD | Report Templates | Version report template manifests describing sections, data sources, output formats. 【F:docs/SRS/sections/9X_Frontends_Ops/91-ADR-051 - Report Builder & Export System.md†L21-L52】 |
| R-DATA-050 | MUST | Inventory Ledger | Define `InventoryLot.v1`, `InventoryTransaction.v1`, reconciliation policies capturing lot attributes. 【F:docs/SRS/sections/7X_Mapping_Geospatial/72-ADR-050 - Cost & Profit Plugin.md†L15-L40】 |
| R-DATA-051 | SHOULD | Inventory Provenance | Link inventory transactions to sessions, layers, work orders for traceability. 【F:docs/SRS/sections/6X_Core_Domain_Services/62_Job_Lifecycle.md†L86-L109】【F:docs/SRS/sections/7X_Mapping_Geospatial/72-ADR-050 - Cost & Profit Plugin.md†L15-L40】 |
| R-DATA-052 | MUST | Soil & Lab Layers | Introduce canonical soil layer definitions with ingestion metadata and provenance. 【F:docs/plugins/SoilLab.md†L1-L120】 |
| R-DATA-053 | SHOULD | Regulatory Snapshots | Store regulatory exports as signed JSON referencing sessions, weather, applied products, operators. 【F:docs/plugins/Regulatory.md†L1-L140】 |

### 32.5.3 Requirement Sources & Rationale

| Req ID | Source (issue/discussion/standard) | Rationale (one line) |
|--------|-------------------------------------|----------------------|
| R-32000 | Legacy field streamer migration review | Avoid disrupting existing fleets during Nexus rollout. |
| R-32001 | ADR-009/ADR-010 schema governance briefing | Deterministic hashes needed for cross-device validation. |
| R-32002 | Replay determinism task force minutes | Rebuild exact state for QA, analytics, and compliance. |
| R-32003 | ADR-014 interop workshop | Maintain fidelity when exchanging data with third-party tools. |
| R-32004 | ADR-025 retention council | Guarantee storage budgets and regulatory readiness. |
| R-32005 | ADR-026 performance budgets | Prevent background maintenance from starving live ingest. |
| R-32006 | Plugin council sync | Enable analytics plugins without Core redeployments. |
| R-32007 | Regulatory and finance working group | Ensure audit-ready exports and ledgers. |
| R-32008 | Preset/layout UX study | Preserve context for analytics and operator troubleshooting. |

---

## 32.6 Acceptance Criteria & Verification

> **Examples:**
> - Automated unit or integration test coverage thresholds.
> - Simulated scenario replay verification.
> - Manual review or field test sign-off checklist.

### 32.6.1 Requirement-to-Verification Map

| Req ID | Verification Type | Artifact / Location | Pass/Fail Threshold |
|--------|--------------------|---------------------|---------------------|
| R-32000 | Regression suite | `tests/Interop/LegacyFieldStreamerTests.cs` | Round-trip diff ≤ 0 across legacy fixtures. |
| R-32001 | Schema CI | `schemas/` validation pipeline | 100% schema hash coverage per build. |
| R-32002 | Replay harness | `tests/Replays/PoseStreamReplay.md` | Deterministic hash match across 10k frames. |
| R-32003 | Geospatial fixtures | `tests/Interop/GeomExportTests.cs` | Spatial error ≤ 2 cm, metadata parity 100%. |
| R-32004 | Retention simulator | `tests/Retention/PlannerScenarioTests.cs` | Expired assets purged, audit log entries recorded. |
| R-32005 | Performance benchmarks | `benchmarks/TileStoreIndexBench.cs` | Zone lookups ≤ 250 ms p95 under load profile. |
| R-32006 | Plugin integration | `plugins/*/tests/PersistenceContractsTests.cs` | Extensions serialise/deserialise with provenance intact. |
| R-32007 | CLI export regression | `tests/Exports/ManifestDeterminismTests.cs` | Checksums stable across builds. |
| R-32008 | Session lifecycle tests | `tests/Core/SessionMetadataTests.cs` | Provenance + presets stored for all fixtures. |

---

## 32.7 Constraints

- TileStore packages MUST cap storage footprints per job according to ADR-026 performance budgets.【F:docs/SRS/sections/2X_System_Architecture/21-ADR-900 - PoseStream, Layer, and Control Program Roadmap.md†L173-L179】
- All exports MUST include schema hashes and CRS metadata to prevent silent drift during imports.【F:docs/SRS/sections/3X_Data_Storage/32_Persistence_Formats.md†L80-L156】
- Differential sync MUST operate without requiring continuous connectivity; resumes MUST verify checksums before commit.【F:docs/SRS/sections/3X_Data_Storage/33_Offline_First_Sync.md†L16-L124】
- Background maintenance MUST obey CPU/IO budgets defined in ADR-026 to avoid impacting live ingest.【F:docs/SRS/sections/2X_System_Architecture/21-ADR-900 - PoseStream, Layer, and Control Program Roadmap.md†L173-L179】

### 32.7.1 Non-Functional Requirement Classes

- **Performance:** Compression codecs deliver ≤ 40% storage overhead compared to source telemetry while retaining determinism.
- **Reliability & Availability:** Sync resumes gracefully after power loss, replaying manifests without corruption.
- **Security:** Export packages include signatures and provenance metadata for tamper detection (see ADR-019).
- **Safety:** Deterministic replay ensures safety investigations reproduce operator actions exactly.
- **Usability/UX:** Operators receive actionable prompts when schema or CRS mismatches occur during import/export.
- **Operability:** Health dashboards expose tile compaction status, retention progress, and sync backlog depth.
- **Portability:** Storage layouts remain compatible with Windows and Linux filesystems.
- **Maintainability:** Schema version bumps documented via semantic changelog and ADR updates.

---

## 32.8 Risks & Open Issues

| ID | Description | Impact | Mitigation / Status | Owner |
|----|-------------|--------|---------------------|-------|
| RISK-32-1 | Legacy rigs running mixed schema versions may reject new TileStore packages. | Medium | Provide compatibility shim and validation CLI for legacy deployments. | @nexus-core |
| RISK-32-2 | Compression upgrades may break determinism or blow CPU budgets. | High | Run replay + benchmark suites before codec rollout; stage behind feature flags. | @nexus-platform |
| ISSUE-32-1 | Need signed export manifest format for regulatory submissions. | Medium | ADR follow-up drafting JSON signature policy. | @nexus-docs-team |

---

## 32.9 Design Considerations

| ID | Consideration | Description |
|----|----------------|-------------|
| C1 | Deterministic Storage | Replay and analytics rely on byte-for-byte deterministic transforms between logs and tiles. |
| C2 | Offline-first Sync | Storage solutions must tolerate intermittent connectivity and manual media transfers. |
| C3 | Schema Federation | Plugins contribute schemas that Core validates via registry hashes without direct coupling. |
| C4 | Interop Fidelity | Industry format round-trips must retain metadata, CRS, and units with measurable tolerances. |
| C5 | Retention Planning | Storage and archival strategies must balance on-device limits with regulatory retention windows. |
| C6 | Observability | Operators and support teams need visibility into sync, compaction, and export health. |
| C7 | Migration Safety | Legacy compatibility is maintained through documented packages and regression suites. |

### 32.9.1 Assumptions & Preconditions

- [A1] Operators can allocate ≥ 64 GB of local storage per rig for active season data, with archival plans for older seasons.【F:docs/SRS/sections/3X_Data_Storage/34-ADR-025 - Data lifecycle and retention policy.md†L10-L58】
- [A2] Sync endpoints (USB, SMB/NFS, S3, mesh) provide reliable checksum verification mechanisms.【F:docs/SRS/sections/3X_Data_Storage/33_Offline_First_Sync.md†L16-L124】
- [A3] Schema registry infrastructure is available during build/release to publish hashes and validation artifacts.【F:docs/SRS/sections/3X_Data_Storage/32_Persistence_Formats.md†L21-L156】

### 32.9.2 Upcoming ADR Coverage

- ADR-007 PoseStream & SectionState architecture codifies replay determinism and section diffs.【F:docs/SRS/sections/2X_System_Architecture/21-ADR-900 - PoseStream, Layer, and Control Program Roadmap.md†L67-L117】
- ADR-012 Multi-session fusion finalises merge semantics and provenance chaining.【F:docs/SRS/sections/2X_System_Architecture/21-ADR-900 - PoseStream, Layer, and Control Program Roadmap.md†L123-L149】
- ADR-014 Interop formats map internal models to ISOXML/GeoPackage exports while preserving schema hashes.【F:docs/SRS/sections/2X_System_Architecture/21-ADR-900 - PoseStream, Layer, and Control Program Roadmap.md†L135-L141】
- ADR-025 Data lifecycle and retention sets archival workflows and compaction triggers.【F:docs/SRS/sections/2X_System_Architecture/21-ADR-900 - PoseStream, Layer, and Control Program Roadmap.md†L165-L171】

---
