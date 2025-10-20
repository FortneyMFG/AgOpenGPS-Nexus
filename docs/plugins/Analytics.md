# Analytics & Export Plugin Requirements (Draft)

Refer to [Core Lifecycle & Editing Interfaces](CoreLifecycle.md) for lifecycle hook details shared across plugin types.

## Overview

Analytics and export plugins transform job/session data into reports, dashboards, and external formats. Season organizers and
multi-field envelopes require new filtering and provenance expectations.

## Runtime Contracts

- Subscribe to `onFarmLoaded`, `onSeasonLoaded`, `onJobLoaded`, `onContextChanged`, and `onSessionStart` to cache active context and trigger incremental analytics updates. Events include authoring metadata (`createdBy`, `createdAt`, `lastModifiedAt`) and plugin `extensions` for crop/profit overlays.【F:docs/SRS/sections/6X_Core_Domain_Services/62_Job_Lifecycle.md†L18-L74】
- Listen for `onSessionPause`/`onSessionResume` to manage incremental analytics windows, and `onSessionWeatherUpdate` when weather-dependent compliance checks are required.【F:docs/SRS/sections/6X_Core_Domain_Services/62_Job_Lifecycle.md†L38-L64】
- Handle `onLayerStartEdit`/`onFeatureCommit` events to recalculate analytics when operators adjust crop, genetics, risk, or profit zones via the shared editing toolchain.【F:docs/ADR/ADR-044_ZoneDrawingFramework.md†L29-L74】
- Accept filters for `seasonId`, `jobId`, and `sessionId` to scope analytics outputs. Provide Season-first and Farm-first report
  presets to match UI navigation flows.【F:docs/SRS/sections/6X_Core_Domain_Services/62_Job_Lifecycle.md†L74-L92】
- Read `job.stats.fields[]` to present per-field summaries even when jobs span multiple fields. Aggregations should clearly
  delineate totals vs. per-field results.【F:docs/ADR/ADR-043_MultiFieldJobEnvelopes.md†L47-L75】
- Honor `Layer.v1` provenance when aggregating planned vs. actual data. Preserve `source`, `transform`, `hash`, `createdBy`, and `actor`
  metadata in exported datasets.【F:schemas/Layer.v1.json†L1-L117】
- Surface plugin-authored overlays from `job.extensions`, `season.extensions`, and `session.extensions` alongside core metrics so crop type, profitability, and genetics insights remain traceable without mutating core schemas.【F:schemas/Job.v1.json†L1-L146】【F:schemas/Season.v1.json†L1-L91】【F:schemas/Session.v1.json†L1-L99】

## Session Awareness

- Include session metadata (env snapshot, inputs, notes) in reports to support compliance and agronomic analysis.
- When sessions are absent (legacy jobs), treat the data as a single implicit session and encourage operators to segment future
  work.

## Layer Reuse Handling

- Detect reused layers via shared hashes; avoid double-counting reused prescriptions or coverage when summarizing across seasons.
- Provide operators with provenance breadcrumbs that link reused layers back to the originating job/session.

## Export Expectations

- Exports must maintain referential integrity between seasons, jobs, sessions, fields, and layers using the canonical IDs defined
  in the schemas.
- When generating regulatory logs, include both planned and actual layers plus session notes to capture operator intent vs. field
  reality.

## Compatibility Notes

- Legacy exporters that only support farm/job filters must be upgraded to recognize seasons and sessions; otherwise they will be
  flagged in compatibility tests.
- Replace any remaining “Run” terminology in report templates or filenames with “Session”.
