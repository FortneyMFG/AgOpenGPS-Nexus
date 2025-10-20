# ADR-040 — Season Organizers

- **Status:** Accepted — 2025-05-17 architecture guild review
- **Date:** 2025-03-18
- **Author(s):** Nexus architecture guild
- **NX Task:** NX-131 Field job session lifecycle ADR

## Context

Operators plan operations around crop years or agronomic campaigns that span multiple farms, yet the current job catalog only
allows grouping by the owning farm folder. Teams have asked for a season construct to bundle related jobs, build reports, and
stage planning artifacts (prescriptions, scouting notes, equipment presets) before execution. Analytics plugins also need a
stable organizer to filter metrics across job sets without assuming all fields live under one farm.

## Decision

Introduce a first-class `Season` entity that optionally sits above jobs. A season records identity, human-readable labels,
creation metadata, a date range, the jobs it covers, freeform notes, and an opaque optimizer payload for planners to checkpoint
their state. Seasons may span multiple farms and can be created ahead of any job execution to support planning and reporting
flows. Core owns the identifier, lifecycle, and synchronization of season documents while plugins consume read-only snapshots
through shared context events. Seasons participate in the global context publish/subscribe system: Core emits
`onFarmLoaded(farmContext)` followed by `onSeasonLoaded(seasonContext)` and `onJobLoaded(jobContext)` so plugins can hydrate
cached analytics and register interest in seasonal overlays before sessions begin.

### Season payload

```json
{
  "id": "season:2025",
  "name": "2025 Crop Year",
  "dateRange": {"start": "2025-01-01", "end": "2025-12-31"},
  "jobIds": ["job:2025-plant-soy", "job:2025-spray-1"],
  "notes": "",
  "optimizerState": null,
  "createdBy": "user:planner.annika",
  "createdAt": "2025-01-02T14:00:00Z",
  "lastModifiedAt": "2025-02-10T22:15:00Z"
}
```

### Extension hooks

- **Context fan-out:** When a season is loaded or its membership changes, Core republishes the active context over the lifecycle
  bus. Plugins receive `onSeasonLoaded` followed by `onJobLoaded` events that include `seasonId`, `seasonName`, immutable farm
  and field IDs, authoring metadata, and derived acreage totals.
- **Core-owned vs. plugin-extendable:** Season documents mark immutable IDs, labels, and audit metadata as Core-owned fields
  while the `extensions` object and nested plugin namespaces remain plugin-owned. Plugin writers may store crop rotation
  projections, seasonal profitability snapshots, or agronomic advisories inside `extensions` without mutating core columns.
- **Lifecycle guarantees:** Prior to any session starting, Core publishes the resolved farm/season/job context so plugins such as
  Crop Type, Genetics, Yield, or Profit can attach state and register overlays. The event contract requires plugins to tolerate
  replay of `onSeasonLoaded` when membership changes while honoring idempotent updates.
- **Cross-plugin coordination:** Analytics and reporting plugins subscribe to `onSeasonLoaded` and `onContextChanged` events to
  recalculate aggregates. Season optimizer payloads may be interpreted by specialized plugins (e.g., Profit, Report Builder) but
  Core treats the payload as opaque binary or JSON blobs.

## SRS Impact

- Satisfies the season catalog hierarchy, payload, and synchronization notes captured in §02 Data Model for Season → Job → Session orchestration.【F:docs/SRS/sections/3X/31_Domain_Data_Model.md†L1-L140】
- Enables season-scoped navigation, analytics, and work planning flows described in §03 Job Lifecycle lifecycle state and event tables.【F:docs/SRS/sections/6X/62_Job_Lifecycle.md†L1-L64】
- Provides the context handle relied on by backend services to hydrate caches before sessions, addressing §04 Backend Services orchestration requirements.【F:docs/SRS/sections/2X/21_System_Decomposition_Boundaries.md†L6-L27】

## Consequences

- Navigation flows may start with season selection, letting operators drill into participating farms and jobs without scanning
  entire farm directories.
- Exports and analytics can scope to a season to produce consolidated reports, seasonal work summaries, or compliance packets.
- Planning tools gain a persistent document to store optimizer checkpoints, field readiness status, or material budgets.
- Season metadata must be synchronized across devices alongside farms and jobs to keep references consistent.
- Core maintains authoring metadata (`createdBy`, `createdAt`, `lastModifiedAt`); plugins append read-only analytics within the
  season `extensions` bag without mutating core fields.

## Alternatives considered

1. **Continue using farm folders only.** Rejected because multi-farm operations cannot aggregate jobs cleanly and planning assets
   would be duplicated under each farm.
2. **Use tags on jobs.** Tags do not capture date ranges, are difficult to audit, and cannot express optimizer payloads without
   embedding additional schemas.

## Migration & compatibility

- `seasonId` on jobs is nullable; existing jobs remain valid with no season reference.
- Importers should create a default season only when operators opt in; Core must not auto-create seasons during migration.
- UI and API flows may hide the Season step when no seasons are defined to preserve today’s farm-first workflow.
- Synchronization tooling must merge seasons by `id` and keep `jobIds` deduplicated.

## Validation

- Regression fixtures must demonstrate season selection driving `onSeasonLoaded` within 200 ms of `onFarmLoaded` on the headless host while keeping cache hydration idempotent across repeats.
- Job/session analytics exports scoped to a season must include all participating jobs with consistent acreage totals and provenance hashes.
- Offline merge tests confirm concurrent season edits reconcile deterministically using last-write-wins on Core-owned fields and plugin-defined merge rules inside `extensions`.

## References

- [Section 31 — Domain Data Model](../SRS/sections/3X/31_Domain_Data_Model.md)
- [Section 62 — Job Lifecycle](../SRS/sections/6X/62_Job_Lifecycle.md)
- [Section 21 — System Decomposition & Boundaries](../SRS/sections/2X/21_System_Decomposition_Boundaries.md)
- [ADR-023 — Session & Job Model](ADR-023-session-job-model.md)
