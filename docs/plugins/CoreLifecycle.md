# Core Lifecycle & Editing Interfaces (Draft)

This reference summarizes the lifecycle events and editing hooks published by Core for plugins. Implementers should subscribe via the shared SDK to avoid bespoke event-bus wiring. All payloads include immutable IDs and authoring metadata as defined in the SRS data model.

## Context Events

| Event | Description | Payload Highlights |
| --- | --- | --- |
| `onFarmLoaded` | Fired when operators select a farm. | `farmId`, farm metadata snapshot, mounted field roster. |
| `onSeasonLoaded` | Fired when a season is selected or a job references a season. | `seasonId`, `name`, `dateRange`, `jobIds[]`, optimizer state, `extensions`. |
| `onJobLoaded` | Fired when a job mounts or resumes. | `farmId`, `seasonId?`, `jobId`, `fieldIds[]`, job metadata (core + `extensions`), immutable IDs. |
| `onContextChanged` | Fired when mounted fields, season, or session IDs change. | Diff summary plus current context snapshot. |
| `onSessionStart` / `onSessionPause` / `onSessionResume` / `onSessionEnd` | Fired on session lifecycle transitions. | `jobId`, `sessionId`, `fieldIds[]`, environment snapshot, crop/genetics context, pause/resume reasons. |
| `onSessionMetadataChange` | Fired when session metadata updates. | Updated session document with diff summary. |
| `onSessionWeatherUpdate` | Fired when weather snapshots change. | Temperature, humidity, wind, rainfall, pressure, source, captured timestamp. |

## Zone Drawing & Layer Editing

| Event | Trigger | Notes |
| --- | --- | --- |
| `onLayerStartEdit` | Layer enters edit mode via LayerEditService. | Payload includes `layerId`, `jobId`, `sessionId?`, editable attribute schema reference. Plugins should prepare attribute editors and pause conflicting automation. |
| `onFeatureCommit` | Geometry/attribute change committed. | Carries `LayerEditEvent` journal entry. Plugins recompute analytics, refresh overlays, and forward deltas to collaborative meshes. |
| `onLayerUndo` / `onLayerRedo` | Undo stack mutates. | Provides previous/next `LayerEditEvent` IDs so plugins update derived caches. |

## Analytics & Reporting Hooks

- `getPreviousCrop(geometry|fieldId)` — Provided by the Crop Type plugin to return the most recent crop history entry intersecting the geometry.
- `getYieldByCrop(scope)` / `getYieldByVariety(scope)` — Yield plugin aggregations returning totals, averages, and statistics scoped to farm/season/job/session.
- `registerReportSection(sectionId, capabilities, renderFn)` — Report Builder hook for contributing report content. Declare dependencies (`crop`, `genetics`, `yield`, `profit`, `risk`, `weather`) so the generator orders sections correctly.
- `onReportGenerate(context)` — Pre-notification allowing plugins to preload caches before rendering report sections.

## Implementation Notes

- Subscribe to events through the SDK `LifecycleClient` to ensure ordering guarantees (`onFarmLoaded` → `onSeasonLoaded` → `onJobLoaded` → `onSessionStart`).
- Treat all IDs as immutable; use human-readable `name`/`label` fields for UI display.
- Layer edits must emit `LayerEditEvent.v1` documents. Use the provided helper to append journal entries so undo/redo remains deterministic.
- Collaborative scenarios replicate `LayerEditEvent` journals via the Live Telemetry Mesh. Plugins should handle duplicate events idempotently.
- Weather snapshots populate `Session.weatherSnapshot` and `weather.overlay` layers. Plugins that depend on weather should subscribe to `onSessionWeatherUpdate` and fall back to the latest snapshot when updates pause.

For detailed schemas refer to `/schemas` and the ADRs listed in [docs/ADR/ADR-roadmap.md](../ADR/ADR-roadmap.md).
