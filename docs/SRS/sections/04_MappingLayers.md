# 04 — Mapping, Layer Governance & Multi-Field Envelopes (Status: Drafting)

## Overview

Mapping plugins render coverage, rate, and guidance layers while respecting farm/field geometry and job/session provenance.
Multi-field job envelopes must provide continuous navigation across adjacent fields without breaking statistics or journaling. Core publishes lifecycle events (`onFarmLoaded`, `onSeasonLoaded`, `onJobLoaded`, `onContextChanged`, `onSessionStart`) so mapping engines can hydrate caches, consume job or session `extensions`, and expose plugin-authored overlays (crop type, profitability, genetics, field health, weather) alongside core coverage.【F:docs/SRS/sections/03_JobLifecycle.md†L18-L64】 Zone editing flows reuse the shared LayerEditService contracts defined in ADR-044.【F:docs/ADR/ADR-044_ZoneDrawingFramework.md†L29-L74】

## Multi-Field Envelope Handling

- Mapping plugins receive `mountFields(fieldIds[])` and must load all referenced field polygons, build a union envelope, and maintain an R-tree for per-field spatial queries. Job `extensions` provide optional crop-type, genetics, profitability, and risk overlays aligned with mounted fields.【F:docs/ADR/ADR-043_MultiFieldJobEnvelopes.md†L12-L68】【F:schemas/Job.v1.json†L1-L146】
- When rendering coverage, plugins accumulate totals both for the job aggregate and per-field rollups stored in
  `job.stats.fields[]` and exposed via analytics exports.【F:docs/ADR/ADR-043_MultiFieldJobEnvelopes.md†L47-L75】
- Guidance and section control consumers rely on the union envelope to avoid operator prompts when crossing internal lanes; the
  plugin must emit boundary updates whenever the envelope changes.
- Performance target: union envelope queries must resolve within ≤ 25 ms p95 for up to 10 mounted fields.

## Session Context & Layer Provenance

- Mapping, rate, guidance, and analytics plugins receive `jobId`, `sessionId`, and `fieldIds[]` in context events. Outputs must attach provenance referencing the active session, inherit authoring metadata, and include `layerId` entries in `session.layerRefs[]` when persisted.【F:docs/SRS/sections/03_JobLifecycle.md†L18-L66】【F:schemas/Session.v1.json†L1-L115】
- Layer documents record `jobId`, optional `sessionId`, `units`, and a `provenance` block containing `source`, `transform`,
  `hash`, `createdAt`, and optional `actor`. Reused layers update `jobId`/`sessionId` while appending provenance history instead
  of duplicating payloads.【F:schemas/Layer.v1.json†L1-L117】
- Planned vs. actual layers are differentiated by metadata: planners mark layers as `planned`, runtime outputs mark `actual`; UI
  surfaces both in the layer drawer with toggles and provenance summaries.

## Layer Reuse & Move Semantics

- Moving a layer between jobs updates references without copying tiles: tooling rewrites `jobId`/`sessionId`, appends a
  provenance record, and updates the destination session’s `layerRefs[]`.
- When reusing a layer (e.g., applying last year’s prescription), the layer remains stored once; jobs include a `layerRefs[]`
  pointer for traceability.
- Tile stores enforce deterministic hashing; merging operations produce consistent `hash` values given identical inputs to ensure
  analytics deduplicate reused layers.

## Storage & Journaling Expectations

- Session autosave triggers flush coverage tiles, LayerEditEvent journals, and provenance updates before acknowledging `onSessionEnd` events.【F:docs/SRS/sections/03_JobLifecycle.md†L66-L92】
- Layers inherit the job’s folder layout (`/Jobs/<Job>/layers/<layerId>/`) with metadata stored in `Layer.v1` documents and tiles
  stored under `tiles/` with recommended cell sizes documented in layer-specific ADRs.
- Journaling retains both planned and actual layers with timestamped provenance entries, enabling later audits to reconstruct the
  exact inputs and transformations applied during the session.

## UX Requirements

- Layer drawer displays active job layers grouped by session; operators can toggle historical layers, move a layer to another job
  (relocate provenance), or reuse from a prior job via a selection modal.
- Multi-field selection UI presents combined envelope outlines and per-field coverage completion percentages updated in real time.
- Session metadata panel surfaces linked layers with provenance badges showing source plugin, createdAt, and reuse indicators.

## Plugin API Summary

| Plugin Type | Required Updates |
| --- | --- |
| Mapping | Implement `mountFields(fieldIds[])`, `setActiveSession(sessionId)`, `writeLayer(layerId, payload, provenance)`, and LayerEditService hooks (`onLayerStartEdit`, `onFeatureCommit`, `onLayerUndo/Redo`); publish per-field stats. |
| Rate/Sections | Consume `jobId`, `sessionId`, and `fieldIds[]` in lifecycle events; write session-aware layers with provenance. |
| Guidance | Respect multi-field envelopes for lookahead and coverage overlays; include session metadata in telemetry outputs; pause automation while LayerEditService is active to avoid conflicting edits. |
| Analytics/Export | Filter by `seasonId`, `jobId`, and `sessionId`; honor provenance when generating planned vs. actual reports; consume plugin overlays (crop type, genetics, yield, profit, risk, weather) via context events. |

## Shared Zone Drawing Framework

- Layer editing is centralized in Core’s LayerEditService (ADR-044). Plugins declare editable layers and attribute schemas via manifests and respond to `onLayerStartEdit`, `onFeatureCommit`, and `onLayerUndo/Redo` events.【F:docs/ADR/ADR-044_ZoneDrawingFramework.md†L29-L74】
- Toolbar modes include polygon, rectangle, brush, and eraser tools supplied by Core; plugins contribute attribute panels (crop, genetics, risk, profit tags) declaratively.
- LayerEditEvent journals persist geometry/attribute operations with deterministic hashes. Collaborative scenarios replicate journals via the Live Telemetry Mesh (ADR-047).【F:docs/ADR/ADR-047_LiveTelemetryMesh.md†L33-L62】

## Layer Catalog Additions

- **Crop Type:** `cropType.planned`, `cropType.actual`, `cropType.history` store crop, year, status, source, and notes aligned with Field crop history and job/session context.【F:docs/ADR/ADR-045_CropTypePlugin.md†L29-L71】
- **Genetics:** `genetics.plan`, `genetics.variety` capture seed brand/product/lot/treatment with provenance to coverage events and barcode change logs.【F:docs/ADR/ADR-046_GeneticsPlugin.md†L21-L66】
- **Yield:** `yield.actual`, `yield.moisture`, `yield.testWeight` store normalized harvest metrics with smoothing metadata and aggregation bins.【F:docs/ADR/ADR-049_YieldPlugin.md†L21-L52】
- **Profit:** `profit.net` overlays combine yield-derived revenue and cost inputs, referencing `CostRecord` transactions and source layers.【F:docs/ADR/ADR-050_CostProfitPlugin.md†L21-L52】
- **Risk:** `risk.flood`, `risk.compaction`, `risk.weeds`, `risk.other` annotate severity and observations, leveraging LayerEditService for edits and validating metadata with `FieldHealthRiskLayer.v1` (severity scale, observer provenance, attachments).【F:docs/ADR/ADR-052_FieldHealthPlugin.md†L21-L44】【F:schemas/FieldHealthRiskLayer.v1.json†L1-L140】
- **Weather:** `weather.overlay` visualizes rainfall, temperature, and wind vectors sourced from sensors/APIs and linked to session weather snapshots.【F:docs/ADR/ADR-053_WeatherPlugin.md†L21-L49】
- **Soil:** `soil.ph`, `soil.om`, `soil.p`, `soil.k`, `soil.n`, `soil.zn`, `soil.ec`, `soil.cec` grids capture lab-imported attributes with sample depth, lot, and lab provenance for agronomic analytics and prescription derivations.【F:docs/plugins/SoilLab.md†L1-L120】
- **Terrain:** `terrain.elevation`, `terrain.slope`, `terrain.aspect`, and `terrain.flowAccumulation` surfaces derive from LiDAR/RTK/DEM ingest to support drainage planning, erosion mitigation, and contour guidance workflows.【F:docs/plugins/Terrain3D.md†L1-L140】
- **Drainage design:** `drain.tilePlan` and `drain.outlet` vector layers store planned tile paths, outlet locations, pipe sizes, and installation notes for export to contractors and integration with guidance paths.【F:docs/plugins/Terrain3D.md†L85-L140】
- **Advisor recommendations:** `advisor.vrRecommendation.*` layers (e.g., population, nitrogen) store AI-derived prescription candidates with model metadata, confidence scores, and source history to keep decisions auditable and reproducible in replay fixtures.【F:docs/plugins/AgronomicAdvisor.md†L1-L170】

Each layer definition includes unit metadata, provenance expectations, and accessibility requirements (color ramps, legends) maintained in plugin manifests and schema files under `/schemas`.

## External ingest flow (NX-113)

1. **Normalization:** Incoming GeoTIFF/COG rasters and GeoJSON/GeoPackage vectors are reprojected to the field’s working CRS (ADR-022) and resampled to registry-defined resolution.
2. **Validation:** Normalized payloads validate against Layer Registry definitions (ADR-010). Units, planned/actual flags, and provenance fields must match the catalog entry before persistence.
3. **Provenance capture:** Successful imports append provenance entries to `Layer.v1` noting source file hashes, operator, transform pipeline, and ingestion timestamps.
4. **Session linking:** When imports occur during an active session, JobsService appends the layer ID to `session.layerRefs[]` and emits `onLayerImported` events so plugins refresh overlays.
5. **Error handling:** Validation failures emit structured diagnostics referencing the expected schema hash and missing attributes. Operators receive actionable guidance to adjust source data.

## Open Questions

- What fallback envelope should mapping use if a field polygon is missing or corrupt during mount?
- How should plugins express partial field mounts (e.g., split ownership) without breaking union envelopes?
