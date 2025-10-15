# ADR-045 — Crop Type Plugin & Layers

- **Status:** Drafting
- **Date:** 2025-03-19
- **Author(s):** Nexus architecture guild
- **NX Task:** NX-190 Comprehensive ADR portfolio review

## Context

Operators plan crop rotations, execute planting, and audit historical crop usage by field. Today these records live in disparate
files or shapefiles without consistent provenance or ties to job/session data. The Crop Type plugin needs a formal place in the
Core hierarchy, leveraging the Zone Drawing Framework for spatial edits while syncing crop history back to fields and jobs.

## Decision

Deliver an official Crop Type plugin that owns three layer families (`cropType.planned`, `cropType.actual`, `cropType.history`),
connects crop selections to jobs and sessions, and surfaces historical context in analytics. Core remains responsible for ID
integrity, context publication, and storage; the plugin focuses on attribute semantics, UI workflows, and analytics hooks.

### Layer Definitions

- `cropType.planned` — Operator-authored plans drawn ahead of a season using the zone tool. Attributes include `crop`,
  `year`, `status=planned`, `source`, and `notes`.
- `cropType.actual` — Derived from planting coverage or manual edits during jobs. Attributes include `crop`, `variety?`,
  `year`, `status=actual`, `source` (sensor/manual/import), and `notes`.
- `cropType.history` — Read-only archive layers generated at season close to capture previous crops. Attributes include `crop`,
  `year`, `status=historical`, `source`, `notes`, and references to the originating job/session.

### Data Model Integration

- `Field.v1` gains `cropTypeHistory[]` (chronological records storing `year`, `crop`, `status`, `source`, `layerId`, and
  optional `notes`). Entries are append-only and flagged as plugin-owned via `x-nexus-scope: plugin`.
- Job and session context events include `activeCrop` metadata derived from the most recent planned/actual layers so rate and
  genetics plugins can auto-fill defaults.
- The plugin auto-updates `job.extensions["cropType.actual"]` with the crop assigned to the current envelope for quick lookup.

### UI & Workflow

- Field navigator surfaces quick crop selectors with common rotations and favorites.
- Historical timeline toggles planned vs. actual vs. historical overlays per season.
- Integrates with the zone drawing toolbar; attribute panel exposes crop dropdown, status toggle, source picker, and freeform
  notes.

### Analytics Hooks

- Provides an API `getPreviousCrop(geometry|fieldId)` returning the most recent historical record intersecting the geometry.
- Publishes rotation statistics (e.g., crop frequency, monocrop alerts) via `job.extensions["cropType.analytics"]`.
- Report Builder (ADR-051) consumes plugin-provided report sections summarizing crop acreage per farm/season/job.

## Consequences

- Crop context becomes deterministic for planners, rate controllers, and analytics without duplicating data entry.
- Field history survives across jobs and seasons with immutable IDs and provenance to layers/sessions.
- Requires close coordination with Genetics and Yield plugins to ensure crop context is populated before analytics fire.

## Alternatives Considered

1. **Keep crop records as freeform notes.** Rejected due to poor analytics integration and inability to tie to spatial geometry.
2. **Embed crop data directly in Field documents.** Would bloat the core schema with mutable agronomy details best handled by a
   plugin with dedicated tooling.

## Dependencies

- Requires ADR-044 Zone Drawing Framework and the Core context bus from ADR-040/041/043.
- Feeds genetics (ADR-046), yield analytics (ADR-049), profit calculations (ADR-050), and report builder (ADR-051).

## SRS Impact

- Updates §02 Data Model (Field crop history, job/session crop context).
- Extends §03 Job Lifecycle with crop auto-fill flows tied to session start.
- Adds crop layer catalog entries in §04 Mapping Layers and analytics requirements in §08 Data & Storage.
