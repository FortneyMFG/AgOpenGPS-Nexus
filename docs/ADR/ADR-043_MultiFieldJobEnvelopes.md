# ADR-043 — Multi-Field Job Envelopes

- **Status:** Drafting
- **Date:** 2025-03-18
- **Author(s):** Nexus architecture guild
- **NX Task:** NX-131 Field job session lifecycle ADR

## Context

Current jobs are scoped to a single field boundary, forcing operators to end a job when crossing a lane or driveway that splits
contiguous fields. Mapping, coverage, and analytics plugins treat each field separately, complicating operations where equipment
works multiple fields in one outing without unloading inputs. A union envelope is needed so the job can mount several fields and
plugins can publish aggregated and per-field outputs.

## Decision

Allow jobs to mount multiple fields simultaneously. The mapping plugin (and other spatial consumers) treat the union of the
fields as the active working envelope while still tracking per-field statistics. Core records the mounted `fieldIds`, immutable
authoring metadata, and derived stats on the job and requires plugins to respect multi-field contexts when emitting coverage,
guidance, or rate outputs. Plugin extensions may annotate per-field envelopes (e.g., crop type or profitability overlays)
without mutating core geometry. Core publishes the resolved union envelope via `onJobLoaded(jobContext)` and surfaces
`activeEnvelopeChanged` notifications whenever the mounted field set is updated so plugins can rebuild spatial indices and UI
overlays deterministically.

### Job payload fragment

```json
{
  "id": "job:2025-plant-soy",
  "farmId": "farm:NorthHome",
  "fieldIds": ["field:North40", "field:DrivewayWest"],
  "sessions": ["session:1", "session:2"],
  "stats": {
    "fields": [
      {"fieldId": "field:North40", "areaHa": 16.2},
      {"fieldId": "field:DrivewayWest", "areaHa": 4.8}
    ]
  },
  "createdBy": "user:planner.annika",
  "createdAt": "2025-03-01T18:05:00Z",
  "lastModifiedAt": "2025-05-05T07:10:00Z",
  "extensions": {
    "cropType.history": {
      "field:North40": "corn-2024",
      "field:DrivewayWest": "cover-rye-2024"
    }
  }
}
```

## Consequences

- The mapping plugin must merge polygons from all mounted fields, compute a union envelope, and render coverage without
  requiring the operator to reopen jobs when crossing internal breaks.
- Coverage, rate, and analytics plugins aggregate across the job while preserving per-field rollups in `job.stats.fields` and in
  exported reports.
- Spatial indexing (R-tree) must include all mounted fields so guidance and constraint lookups remain within latency budgets.
- UI workflows need multi-field selection controls and a live indicator of the combined envelope.
- Core publishes `onFarmLoaded` and `onJobLoaded` events with `fieldIds[]`, per-field acreage, immutable IDs, and job metadata;
  plugins listen for the subsequent `mountFields(fieldIds[])` call (Core-provided helper that resolves geometry) and may read
  job `extensions` to drive crop/genetics or profitability overlays. Core also emits `onJobContextChanged` when the active
  envelope changes so spatial caches and analytics recompute safely.

## Alternatives considered

1. **Single-field jobs with quick swap UI.** Rejected because coverage/layer continuity breaks and plugins would need to stitch
   outputs manually across job boundaries.
2. **Treat additional fields as headlands.** Headlands cannot express non-contiguous parcels and would overload their semantics.

## Migration & compatibility

- Existing single-field jobs remain valid with a single entry in `fieldIds`.
- Importers should populate `fieldIds` from legacy job folders by inspecting the field boundary loaded during the job.
- Plugins must not assume a single field; compatibility shims should log warnings when older plugins ignore extra field IDs.
- Per-field statistics in existing logs should map into the new `job.stats.fields` array during migration.
