# Mapping Plugin Requirements (Draft)

## Overview

Mapping plugins render the working envelope, coverage, rate overlays, and historical layers for Nexus jobs. They must operate across multi-field envelopes, respect session metadata, and publish provenance suitable for analytics and reporting.

Refer to [Core Lifecycle & Editing Interfaces](CoreLifecycle.md) for the canonical event and editing hook definitions exposed by Core.

## Runtime Contracts

- **Lifecycle events:** Subscribe to `onFarmLoaded`, `onSeasonLoaded`, `onJobLoaded`, `onContextChanged`, `onSessionStart`, `onSessionPause`, `onSessionResume`, `onSessionMetadataChange`, and `onSessionEnd` to hydrate caches, mount field geometry, and flush journals. Core broadcasts farm/job/session context including authoring metadata and plugin `extensions` for overlays.【F:docs/SRS/sections/03_JobLifecycle.md†L18-L74】【F:schemas/Job.v1.json†L1-L146】
- **Multi-field envelopes:** Implement `mountFields(fieldIds[])` to receive one or more field IDs. Load all referenced polygons, compute a union envelope, and publish per-field indices for analytics consumers. Merges must be deterministic for replay parity.【F:docs/ADR/ADR-043_MultiFieldJobEnvelopes.md†L21-L75】
- **Session context:** Implement `setActiveSession(sessionId)` so coverage and rate outputs attach the correct provenance. Session-aware journaling unlocks analytics comparisons and report exports.【F:docs/ADR/ADR-041_JobSessions.md†L15-L73】
- **Layer authoring:** Provide `writeLayer(layerId, payload, provenance)` to store raster/vector outputs, ensuring `provenance` includes `jobId`, `sessionId`, `source`, `transform`, `hash`, and `createdAt` consistent with `Layer.v1`. Planned vs. actual flags follow Layer Registry metadata (`x-nexus-planned`, `x-nexus-actual`).【F:docs/ADR/ADR-010-layer-registry-variable-rate.md†L33-L58】【F:schemas/Layer.v1.json†L1-L117】
- **Per-field stats:** Emit coverage, rate, and time-in-field statistics grouped by field and aggregate them across the job. Stats feed `job.stats.fields[]` and analytics exports.【F:docs/SRS/sections/04_MappingLayers.md†L10-L83】
- **Extension overlays:** Consume `job.extensions` and `session.extensions` to render plugin-authored layers (crop type, profitability, genetics) without mutating core geometry. Publish derived overlays through `writeLayer` with provenance referencing the contributing plugin.【F:schemas/Job.v1.json†L1-L146】【F:schemas/Session.v1.json†L1-L115】
- **Zone Tool integration:** Register editable layer IDs with the LayerEditService, relay `onLayerStartEdit`/`onFeatureCommit` events to attribute panels, and persist `LayerEditEvent.v1` journals for undo/redo and collaborative mesh sync.【F:docs/ADR/ADR-044_ZoneDrawingFramework.md†L29-L74】【F:schemas/LayerEditEvent.v1.json†L1-L140】
- **Ingest hooks:** Implement normalization for GeoTIFF/COG and GeoJSON/GeoPackage inputs following the NX-113 flow before writing layers, rejecting payloads that fail registry validation.【F:docs/SRS/sections/04_MappingLayers.md†L84-L106】

## Data Expectations

- Geometry is sourced from `Field.v1` documents; mapping plugins must tolerate polygons with holes, headlands, and crop history overlays.
- Envelope changes require boundary broadcasts and cache invalidations prior to new coverage updates, even when fields are removed mid-session.
- Tile storage must follow deterministic hashing rules so layer reuse/move operations stay consistent across sessions and replays.【F:schemas/Layer.v1.json†L1-L117】

## UX Integration

- Provide operators with live coverage percentages per field and highlight transitions between fields within the combined envelope.
- Surface planned vs. actual toggles, provenance badges, and plugin attribution in layer drawers so operators understand data lineage.
- Show collaborative edit presence and undo/redo stacks during Zone Tool sessions, respecting mesh visibility presets from the multi-machine plugin.【F:docs/ADR/ADR-047_LiveTelemetryMesh.md†L33-L62】

## Compatibility Notes

- Plugins assuming a single field per job must emit warnings and fallback gracefully but will not meet Nexus requirements.
- Backwards compatibility shims may map legacy `Run` hooks to the new session APIs during transition; long term, plugins must adopt the updated contracts from ADR-041.
- External ingest must reject layers lacking registry entries, surfacing actionable diagnostics rather than silently storing untyped artifacts.
