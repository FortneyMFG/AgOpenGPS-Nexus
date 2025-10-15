# Guidance Plugin Requirements (Draft)

## Overview

Guidance plugins compute steering targets, lookahead paths, and visual overlays. They must honor multi-field envelopes, session
context, and provenance expectations introduced by ADR-041 and ADR-043.

## Runtime Contracts

- Subscribe to `onFarmLoaded`, `onJobLoaded`, and `onSessionStart` to prime lookahead models with farm assets, mounted field geometry, and session metadata. Core broadcasts the active context (`farmId`, `fieldIds[]`, `seasonId?`, `jobId`, `sessionId`) plus plugin `extensions` for overlays.【F:docs/SRS/sections/03_JobLifecycle.md†L18-L40】
- Receive `jobId`, `sessionId`, and `fieldIds[]` in lifecycle events. Guidance calculations must adjust lookahead logic when the
  union envelope changes (e.g., crossing into an adjacent field).
- Consume mapping plugin envelope updates to maintain accurate boundary awareness and prevent autosteer beyond mounted fields.
- Emit telemetry tagged with `sessionId` so analytics can correlate steering performance with specific outings.【F:docs/SRS/sections/03_JobLifecycle.md†L59-L92】
- Leverage `session.extensions` for agronomic hints (crop type, growth stage) when adjusting lookahead speed or lane biasing, while leaving core session fields immutable.【F:schemas/Session.v1.json†L1-L99】

## Layer & Provenance Expectations

- Guidance outputs stored as layers (e.g., driven paths) must reference `jobId` and `sessionId` in `Layer.v1` documents, include `createdBy` metadata, and append provenance entries with the plugin ID and deterministic hash of the generated geometry.【F:schemas/Layer.v1.json†L1-L117】
- Reused guidance layers (e.g., previous AB lines) should remain in place; plugins link them via `session.layerRefs[]` without
  duplicating geometry.

## UX Integration

- Provide operators with field-aware lane previews that remain continuous across multi-field envelopes.
- Surfacing session metadata (notes, inputs) helps contextualize steering adjustments; guidance panels should display the active
  session name and environment snapshot when available.

## Compatibility Notes

- Plugins assuming a single field must be updated; Core will log warnings when `fieldIds.length > 1` and legacy hooks respond.
- Legacy "Run" terminology in UI/tooling should be updated to "Session" to stay consistent with ADR-041.
