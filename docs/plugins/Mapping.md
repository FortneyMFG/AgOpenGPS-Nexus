# Mapping Plugin Requirements (Draft)

## Overview

Mapping plugins render the working envelope, coverage, rate overlays, and historical layers for Nexus jobs. They must operate
across multi-field envelopes, respect session metadata, and publish provenance suitable for analytics and reporting.

## Runtime Contracts

- **Lifecycle events:** Subscribe to `onFarmLoaded`, `onJobLoaded`, `onSessionStart`, `onSessionMetadataChange`, and `onSessionEnd` to hydrate caches, mount field geometry, and flush journals. Core broadcasts farm/job/session context including authoring metadata and plugin `extensions` for overlays.【F:docs/SRS/sections/03_JobLifecycle.md†L12-L72】【F:schemas/Job.v1.json†L1-L146】

- **Multi-field envelopes:** Implement `mountFields(fieldIds[])` to receive one or more field IDs. Load all referenced polygons,
  compute a union envelope, and publish per-field indices for analytics consumers.【F:docs/ADR/ADR-043_MultiFieldJobEnvelopes.md†L21-L63】
- **Session context:** Implement `setActiveSession(sessionId)` so coverage and rate outputs attach the correct provenance.
- **Layer authoring:** Provide `writeLayer(layerId, payload, provenance)` to store raster/vector outputs, ensuring `provenance`
  includes `jobId`, `sessionId`, `source`, `transform`, `hash`, and `createdAt` consistent with `Layer.v1`.
- **Per-field stats:** Emit coverage, rate, and time-in-field statistics grouped by field and aggregate them across the job. Stats
  feed `job.stats.fields[]` and analytics exports.【F:docs/SRS/sections/04_MappingLayers.md†L10-L83】
- **Extension overlays:** Consume `job.extensions` and `session.extensions` to render plugin-authored layers (crop type, profitability, genetics) without mutating core geometry. Publish derived overlays through `writeLayer` with provenance referencing the contributing plugin.【F:schemas/Job.v1.json†L1-L146】【F:schemas/Session.v1.json†L1-L99】

## Data Expectations

- Geometry is sourced from `Field.v1` documents; mapping plugins must tolerate polygons with holes and optional headland
  definitions.
- When envelopes change (fields added/removed), plugins broadcast boundary updates and invalidate caches accordingly.
- Tile storage must follow deterministic hashing rules so layer reuse/move operations stay consistent across sessions.

## UX Integration

- Provide operators with live coverage percentages per field and highlight transitions between fields within the combined
  envelope.
- Support toggling planned vs. actual layers with clear provenance indicators (source plugin, session, timestamp).

## Compatibility Notes

- Plugins assuming a single field per job must emit warnings and fallback gracefully but will not meet Nexus requirements.
- Backwards compatibility shims may map legacy `Run` hooks to the new session APIs during transition; long term, plugins must
  adopt the updated contracts from ADR-041.
