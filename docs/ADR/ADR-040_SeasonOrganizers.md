# ADR-040 — Season Organizers

- **Status:** Drafting
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
through shared context events.

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

- **Core context broadcast:** When a season is loaded or its membership changes, Core updates the shared job context payload
  (see ADR-041) so plugins receive `onJobLoaded` notifications that include `seasonId`, `seasonName`, and derived analytics such
  as total acreage.
- **Plugin extensions:** Season documents expose a `extensions` object for plugin-owned metadata (e.g., seasonal crop
  rotations, budget snapshots). Core persists the blob but does not interpret plugin keys.
- **Cross-plugin coordination:** Analytics and reporting plugins may subscribe to `onSeasonLoaded` via the event bus, using the
  optimizer payload and job roster to seed forecasts or roll up completed work. Crop-type history or profitability projections
  live inside plugin extensions to avoid bloating the core schema.

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
