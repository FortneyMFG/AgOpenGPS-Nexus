# ADR-045 — Crop Type Plugin & Layers

- **Status:** Drafting
- **Date:** -
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

## Governance Updates

- **Registry-first layers.** `cropType.planned`, `cropType.actual`, and `cropType.history` definitions are now frozen in the
  layer registry and must ship schema diffs alongside manifest changes so downstream analytics detect upgrades in advance.【F:schemas/Layer.v1.json†L1-L120】【F:schemas/CropTypePlanned.v1.json†L1-L118】
- **History promotion checklist.** Season close-outs must attach validator output proving that `cropType.history` entries match
  session journals before the archive publishes, providing an auditable trail for Report Builder templates and profit analytics.【F:schemas/CropTypeHistoryRecord.v1.json†L1-L130】【F:docs/sections/9X_Frontends_Ops/91-ADR-051 - Report Builder & Export System.md†L9-L70】
- **Fixture coverage.** Regression packs now include multi-field jobs with overlapping crop edits to guarantee deterministic
  journal playback through the Zone Drawing framework and Live Telemetry Mesh replication paths.【F:docs/sections/7X_Mapping_Geospatial/72-ADR-044 - Zone Drawing Framework.md†L9-L74】【F:docs/sections/4X_Interprocess_Communications/42-ADR-047 - Live Telemetry Mesh.md†L9-L70】

## Amendment — 2025 architecture refresh (NX-190)

- Job and session snapshots embed the mounted crop IDs alongside genetics references so Profit, Field Health, and Weather
  plugins can align analytics with the authoritative crop context without bespoke joins.【F:schemas/Session.v1.json†L1-L120】
- Multi-field job envelopes publish per-field crop summaries in `job.stats.fields[]`, allowing Report Builder templates to split
  acreage and analytics by field even when a single job spans multiple parcels.【F:docs/sections/3X_Data_Storage/31-ADR-043 - Multi-Field Job Envelopes.md†L9-L112】
- Crop zone edits propagated over the mesh now carry provenance hashes and replay seeds, enabling collaborative planners to
  reconcile planned vs. actual layers after offline work without manual merge steps.【F:docs/sections/4X_Interprocess_Communications/42-ADR-048 - RadioBridge for ELRS LoRa Telemetry.md†L9-L60】

## Alternatives Considered

1. **Keep crop records as freeform notes.** Rejected due to poor analytics integration and inability to tie to spatial geometry.
2. **Embed crop data directly in Field documents.** Would bloat the core schema with mutable agronomy details best handled by a
   plugin with dedicated tooling.

## Dependencies

- Requires ADR-044 Zone Drawing Framework and the Core context bus from ADR-040/041/043.
- Feeds genetics (ADR-046), yield analytics (ADR-049), profit calculations (ADR-050), and report builder (ADR-051).

## SRS Impact

- Satisfies crop history requirement R-DATA-041 and associated catalog expectations in §04 Mapping Layers and §08 Data Model & Storage.【F:docs/sections/3X_Data_Storage/32_Persistence_Formats.md†L28-L31】【F:docs/sections/7X_Mapping_Geospatial/72_Mapping_Layers_Plugin.md†L62-L67】
- Hooks crop context auto-fill into job lifecycle and work order flows described in §03 Job Lifecycle (R-JOB-040…R-JOB-042).【F:docs/sections/6X_Core_Domain_Services/62_Job_Lifecycle.md†L27-L75】
- Powers crop overlay toggles and analytics in §05 Frontends (R-FE-074).【F:docs/sections/9X_Frontends_Ops/91_UI_Shell_Layout.md†L29-L30】

---

## Change Log

| Date | Summary | Author | PR / Issue |
|------|---------|--------|------------|
| 2025-10-24 | Initial draft | Nexus Team (Codex) |  |

