# ADR-046 — Genetics Plugin & Layers

- **Status:** Drafting
- **Date:** 2025-03-19
- **Author(s):** Nexus architecture guild
- **NX Task:** NX-190 Comprehensive ADR portfolio review

## Context

Seed variety, lot, and treatment information is critical for agronomy, compliance, and yield attribution. Legacy workflows store
this data in spreadsheets disconnected from spatial coverage and crop records. Nexus requires a plugin that captures genetics
metadata as spatial layers linked to jobs and sessions, integrates with live planting operations, and exports standardized
reports.

## Decision

Ship a Genetics plugin that maintains plan and actual layers (`genetics.plan`, `genetics.variety`), captures barcode/lot details,
and binds them to jobs, sessions, and crop context. The plugin leverages the Zone Drawing Framework for spatial edits and Core's
coverage events to auto-write variety polygons during planting.

### Layers & Schemas

- `genetics.plan` — Pre-job plans referencing seed varieties. Attributes: `brand`, `product`, `traitStack`, `lot`, `treatment`,
  `source`, `notes`.
- `genetics.variety` — Actual planted varieties captured from live coverage. Attributes mirror the plan plus `appliedAt` and
  `sessionId`.
- Schemas `GeneticsPlan.v1` and `GeneticsVariety.v1` define the attribute structure, provenance references, and mandatory
  metadata (immutable IDs, `createdBy`, `createdAt`, `lastModifiedAt`).

### Runtime Behavior

- When a job starts, the plugin reads crop context from the Crop Type plugin and preselects matching varieties.
- During active coverage, the plugin listens to `onSessionStart`, `onCoverageTick`, and `onFeatureCommit` to maintain sync between
  live coverage and genetics layers.
- Barcode scans or manual selections update the active variety assignment; the plugin records the change with provenance entries
  and optional change-log events surfaced in UI.

### UI & Exports

- Provides a picker UI with search, favorites, and recent lots. Supports barcode input via keyboard wedge or serial scanner.
- Displays a mini legend overlay showing current variety assignments within the active envelope.
- Exports CSV, GeoJSON, and ISOXML records including lot/treatment metadata and provenance references to jobs and sessions.

## Consequences

- Genetics information becomes spatially anchored to coverage, enabling downstream analytics (yield, profit) to compute variety
  performance.
- Requires tight coupling with crop context and session lifecycle events to avoid stale assignments.
- Adds new schemas and storage expectations for change logs and exports.

## Alternatives Considered

1. **Attach genetics data to crop layers only.** Rejected because planting operations often mix varieties within a field and need
   separate provenance and audit trails.
2. **Maintain genetics outside Nexus.** Incompatible with analytics and reporting objectives.

## Dependencies

- Depends on ADR-045 Crop Type plugin for crop context.
- Uses ADR-044 Zone Drawing Framework for manual edits.
- Integrates with ADR-047 Multi-Machine mesh for live share of active variety when collaborating.

## SRS Impact

- Delivers genetics record schemas required by R-DATA-044 and catalog coverage in §04 Mapping Layers.【F:docs/SRS/sections/08_Data_Model_Storage.md†L32-L34】【F:docs/SRS/sections/04_MappingLayers.md†L62-L67】
- Threads barcode and variety change events through job lifecycle workflows (R-JOB-040…R-JOB-042).【F:docs/SRS/sections/03_JobLifecycle.md†L27-L75】
- Documents plugin registration and governance obligations for genetics extensions in §12 Extensibility.【F:docs/SRS/sections/12_Extensibility_Plugins.md†L18-L36】
