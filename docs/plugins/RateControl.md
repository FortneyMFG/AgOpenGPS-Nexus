# Rate & Section Control Plugin Requirements (Draft)

## Overview

Rate and section control plugins manage product application across implements. Multi-field jobs and session metadata require
additional context for deterministic control and audit trails.

## Runtime Contracts

- Subscribe to `onFarmLoaded`, `onJobLoaded`, and `onSessionStart` so controller state machines preload farm assets, job field geometry, and session metadata before enabling sections. Core context includes `seasonId?`, authoring metadata, and plugin `extensions` for agronomic overlays.【F:docs/SRS/sections/03_JobLifecycle.md†L18-L40】
- Receive `jobId`, `sessionId`, and `fieldIds[]` on initialization and when sessions change. Use this context to align coverage
  logging, implement leases, and automated shutoff policies.
- Respect multi-field envelopes provided by the mapping plugin to avoid unintended application when crossing field boundaries.
- Emit coverage/rate layers tagged with `jobId` and `sessionId`; append provenance records describing rate tables, variable maps,
  and section events, including `createdBy` metadata for audit trails.【F:docs/SRS/sections/04_MappingLayers.md†L10-L83】【F:schemas/Layer.v1.json†L1-L117】
- Read/write `session.extensions` for plugin-specific telemetry (e.g., product lot tracking, profitability) while preserving core session fields.【F:schemas/Session.v1.json†L1-L99】

## Session-Aware Outputs

- When journaling section states, include session metadata (start/end timestamps, operator notes) to support compliance reports.
- Autosave changes whenever rate presets, material inputs, or environment data shift beyond configured thresholds; align with
  session autosave cadence for consistent recovery.【F:docs/SRS/sections/03_JobLifecycle.md†L37-L74】

## UX Integration

- Display active session name, environment snapshot, and per-field coverage percentages in control panels to help operators
  validate product usage across fields.
- Provide controls to start a new session directly from the rate panel; Core handles lifecycle events but plugins should surface
  state transitions.

## Compatibility Notes

- Plugins relying on a single field must update; Core will pass all mounted fields and expect per-field rate totals.
- Replace any “Run” terminology with “Session” in logs, tooltips, and exported files for operator clarity.
