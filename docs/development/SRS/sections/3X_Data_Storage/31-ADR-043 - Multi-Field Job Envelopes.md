# ADR-043 — Multi-Field Job Envelopes

- **Status:** Accepted — - lifecycle & mapping review
- **Date:** -
- **Author(s):** Nexus architecture guild
- **NX Task:** NX-131 Field job session lifecycle ADR

## Context

[SRS §31 Domain Data Model](31_Domain_Data_Model.md) defines a farm-first organizer: farms contain seasons, seasons contain
jobs, and jobs contain the sessions that persist spatial layers for coverage, guidance, and analytics. Fields remain important
partitions for agronomic statistics and operator guidance, but the storage hierarchy is anchored on farms and seasons rather
than individual field folders. The existing single-field restriction breaks that model. Operators must stop or duplicate jobs
whenever equipment crosses interior driveways or parcels even though the spatial data continues to journal into the same farm,
season, job, and session layer stacks. Mapping, coverage, and analytics plugins therefore cannot emit consistent multi-field
layers without bespoke stitching.

We need an envelope contract that lets a job mount several fields simultaneously, preserves per-field statistics, and still
aligns with the farm/season/session storage hierarchy described in SRS §31. Plugins should read and write spatial layers that
roll up by session, job, season, and farm, while treating fields as a deterministic split within those layers for analytics,
guidance, and regulatory reporting.

## Decision

Jobs MAY mount multiple fields drawn from the same farm organizer. Core persists the mounted `fieldIds`, immutable authoring
metadata, and derived statistics on the job record. When a job mounts fields, Core resolves a union envelope spanning all
members and journals it to the layer hierarchy alongside per-field splits. The mapping plugin (and any spatial consumer)
treats the union envelope as the active working geometry for coverage and guidance layers while still exposing deterministic
per-field indices for analytics. Plugins emit coverage, rate, and telemetry layers into the session/job/season/farm journals
as usual, but must tag per-field contributions inside those layers for rollups.

Plugin extensions may annotate per-field envelopes (e.g., crop type or profitability overlays) without mutating the stored
geometry. Core publishes the resolved union envelope via `onFarmLoaded(farmContext)` and `onJobLoaded(jobContext)` callbacks
that include session and layer provenance. Whenever the mounted field set changes, Core emits `activeEnvelopeChanged`
notifications so plugins can rebuild spatial indices, refresh UI overlays, and append a new layer segment without replaying
historical data.

### Job payload fragment

```json
{
  "id": "job:2025-plant-soy",
  "farmId": "farm:NorthHome",
  "fieldIds": ["field:North40", "field:DrivewayWest"],
  "sessions": ["session:1", "session:2"],
  "envelope": {
    "polygons": [
      {"exterior": [[-94.6123, 41.5621], [-94.6109, 41.5621], [-94.6109, 41.5648], [-94.6123, 41.5648], [-94.6123, 41.5621]]},
      {"exterior": [[-94.6094, 41.5616], [-94.6079, 41.5616], [-94.6079, 41.5634], [-94.6094, 41.5634], [-94.6094, 41.5616]]}
    ],
    "members": [
      {"fieldId": "field:North40", "areaHa": 16.2},
      {"fieldId": "field:DrivewayWest", "areaHa": 4.8}
    ],
    "boundingBox": {"minLon": -94.6123, "minLat": 41.5616, "maxLon": -94.6079, "maxLat": 41.5648},
    "centroid": [-94.6101, 41.563],
    "areaHa": 21.0,
    "crsEpsg": 4326
  },
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

## SRS Impact

- Reinforces §31 Domain Data Model expectations for farm → season → job → session organizers while allowing fields to remain the
  unit of analytics and guidance splits within those layers.【F:docs/development/SRS/sections/3X_Data_Storage/31_Domain_Data_Model.md†L12-L140】
- Extends the mount/unmount lifecycle in §62 Job Lifecycle so `mountFields` events capture multi-field context and layer
  provenance, keeping session journals deterministic when the envelope changes.【F:docs/development/SRS/sections/6X_Core_Domain_Services/62_Job_Lifecycle.md†L59-L112】
- Aligns with §72 Mapping Layers Plugin by enforcing union envelope construction, per-field R-tree maintenance, and layer
  tagging so spatial outputs can be replayed and aggregated by session, job, season, and farm without bespoke stitching.【F:docs/development/SRS/sections/7X_Mapping_Geospatial/72_Mapping_Layers_Plugin.md†L1-L74】

## Consequences

- The mapping plugin merges polygons from all mounted fields, computes a union envelope, and journals coverage/guidance layers
  against the active session without forcing operators to reopen jobs when crossing internal breaks.
- Coverage, rate, telemetry, and analytics plugins aggregate across the job while tagging per-field contributions in
  `job.stats.fields` and layer metadata so exports remain compliant with regulatory splits.
- Spatial indexing (R-tree) includes all mounted fields so guidance and constraint lookups remain within latency budgets even as
  envelopes change. Plugins append a new layer segment on each `activeEnvelopeChanged` notification to preserve replay parity.
- UI workflows provide multi-field selection controls, display the combined envelope, and surface which sessions contribute to
  the active layer stack.
- Core publishes `onFarmLoaded`/`onJobLoaded` events with `fieldIds[]`, per-field acreage, immutable IDs, session references,
  and job metadata; plugins listen for `mountFields(fieldIds[])` and may read job `extensions` to drive crop/genetics or
  profitability overlays. Core emits `onJobContextChanged` when the active envelope changes so spatial caches and analytics
  recompute safely without rewriting history.

`job.envelope` captures the deterministic geometry Core publishes: union polygons, optional per-field members with analytics
metadata, bounding box/centroid hints, and the CRS used for the envelope. Plugins rely on the shared structure defined in
`Job.v1` to avoid bespoke geometry parsing when replaying sessions or rendering overlays, and they associate emitted layers with
the same session/job identifiers recorded in the envelope.

## Alternatives considered

1. **Single-field jobs with quick swap UI.** Rejected because coverage/layer continuity breaks and plugins would need to stitch
   outputs manually across job boundaries.
2. **Treat additional fields as headlands.** Headlands cannot express non-contiguous parcels and would overload their semantics.

## Migration & compatibility

- Existing single-field jobs remain valid with a single entry in `fieldIds`.
- Importers should populate `fieldIds` from legacy job folders by inspecting the field boundary loaded during the job.
- Plugins must not assume a single field; compatibility shims should log warnings when older plugins ignore extra field IDs.
- Per-field statistics in existing logs should map into the new `job.stats.fields` array during migration.

## References

- [Section 31 — Domain Data Model](../sections/3X_Data_Storage/31_Domain_Data_Model.md)
- [Section 62 — Job Lifecycle](../sections/6X_Core_Domain_Services/62_Job_Lifecycle.md)
- [Section 72 — Mapping Layers Plugin](../sections/7X_Mapping_Geospatial/72_Mapping_Layers_Plugin.md)
- [ADR-040 — Season Organizers](ADR-040_SeasonOrganizers.md)

---

## Change Log

| Date | Summary | Author | PR / Issue |
|------|---------|--------|------------|
| 2025-10-24 | Initial draft | Nexus Team (Codex) |  |

