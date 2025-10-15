# 04 — Mapping, Layer Governance & Multi-Field Envelopes (Status: Drafting)

## Overview

Mapping plugins render coverage, rate, and guidance layers while respecting farm/field geometry and job/session provenance.
Multi-field job envelopes must provide continuous navigation across adjacent fields without breaking statistics or journaling. Core publishes lifecycle events (`onFarmLoaded`, `onJobLoaded`, `onSessionStart`) so mapping engines can hydrate caches, consume job or session `extensions`, and expose plugin-authored overlays (crop type, profitability, genetics) alongside core coverage.【F:docs/SRS/sections/03_JobLifecycle.md†L18-L40】

## Multi-Field Envelope Handling

- Mapping plugins receive `mountFields(fieldIds[])` and must load all referenced field polygons, build a union envelope, and maintain an R-tree for per-field spatial queries. Job `extensions` provide optional crop-type or profitability overlays aligned with mounted fields.【F:docs/ADR/ADR-043_MultiFieldJobEnvelopes.md†L12-L68】【F:schemas/Job.v1.json†L1-L146】
- When rendering coverage, plugins accumulate totals both for the job aggregate and per-field rollups stored in
  `job.stats.fields[]` and exposed via analytics exports.【F:docs/ADR/ADR-043_MultiFieldJobEnvelopes.md†L47-L75】
- Guidance and section control consumers rely on the union envelope to avoid operator prompts when crossing internal lanes; the
  plugin must emit boundary updates whenever the envelope changes.
- Performance target: union envelope queries must resolve within ≤ 25 ms p95 for up to 10 mounted fields.

## Session Context & Layer Provenance

- Mapping, rate, guidance, and analytics plugins receive `jobId`, `sessionId`, and `fieldIds[]` in context events. Outputs must attach provenance referencing the active session, inherit authoring metadata, and include `layerId` entries in `session.layerRefs[]` when persisted.【F:docs/SRS/sections/03_JobLifecycle.md†L18-L40】【F:schemas/Session.v1.json†L1-L99】
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

- Session autosave triggers flush coverage tiles and provenance updates before acknowledging `onSessionEnd` events.【F:docs/SRS/sections/03_JobLifecycle.md†L45-L66】
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
| Mapping | Implement `mountFields(fieldIds[])`, `setActiveSession(sessionId)`, and `writeLayer(layerId, payload, provenance)` APIs; publish per-field stats. |
| Rate/Sections | Consume `jobId`, `sessionId`, and `fieldIds[]` in lifecycle events; write session-aware layers with provenance. |
| Guidance | Respect multi-field envelopes for lookahead and coverage overlays; include session metadata in telemetry outputs. |
| Analytics/Export | Filter by `seasonId`, `jobId`, and `sessionId`; honor provenance when generating planned vs. actual reports. |

## Open Questions

- What fallback envelope should mapping use if a field polygon is missing or corrupt during mount?
- How should plugins express partial field mounts (e.g., split ownership) without breaking union envelopes?
