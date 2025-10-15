# 02 — Data Model: Farm → Field and Season → Job → Session Hierarchies (Status: Drafting)

## Overview

Nexus manages agronomic work using two complementary hierarchies:

1. **Spatial hierarchy — Farm → Field:** Organizes reusable geometry, imagery, and operational metadata.
2. **Operational hierarchy — Season → Job → Session:** Structures planned and actual work across time, equipment, and
   multi-field envelopes.

These hierarchies share identifiers, authoring metadata, and provenance so Core, plugins, and analytics can traverse
relationships without relying on filesystem structure alone. Every primary entity carries immutable IDs, human-readable labels,
and audit fields (`createdBy`, `createdAt`, `lastModifiedAt`). Core owns the lifecycle of these fields while plugins attach
domain-specific facts through `extensions` bags that Core stores verbatim.

### Core vs. plugin responsibilities

| Area | Core-owned | Plugin-extendable |
| --- | --- | --- |
| Identity & metadata | `id`, `name`, authoring metadata, relationship pointers | Derived analytics (`cropType.*`, profitability summaries), historical annotations |
| Geometry | Farm/field polygons, headlands, shared assets | Zone drawings, prescription overlays stored as layers |
| Operational hierarchy | Season/job/session creation, lifecycle events, journaling checkpoints | Session extensions, job-level agronomic insights |
| Provenance | `jobId`, `sessionId`, `createdAt`, `hash`, actor | Additional provenance attributes per plugin (e.g., calibration IDs) |

## Entities

### Farm.v1
- **Identity:** `farm:<slug>` unique to the operator organization.
- **Core attributes:** `name`, authoring metadata, optional imagery/asset references, shared notes for operators.
- **Relationships:** Owns one or more fields; referenced as the primary farm on a job.
- **Plugin extensions:** `extensions` may include crop-type plans, profitability projections, or geojson overlays published by
  analytics plugins.
- **Schema:** `schemas/Farm.v1.json` (draft) defines required fields, authoring metadata, and asset metadata, with `x-nexus-scope`
  flags denoting core vs plugin control.【F:schemas/Farm.v1.json†L1-L92】

### Field.v1
- **Identity:** `field:<slug>` unique within the owning farm.
- **Core attributes:** Polygons (with holes), tags/headlands, notes, authoring metadata.
- **Relationships:** Belongs to a farm; referenced by jobs via `fieldIds`.
- **Geometry:** Stored as polygon exteriors and optional holes using `[lon, lat (, elevation)]` coordinates; headlands capture
  width, units, and optional pass counts.
- **Plugin extensions:** Zone drawings, soil sampling layers, crop rotation histories recorded inside `extensions`.
- **Schema:** `schemas/Field.v1.json` documents the geometry, metadata layout, authoring fields, and extension hooks.【F:schemas/Field.v1.json†L1-L142】

### Season.v1
- **Identity:** `season:<year-or-label>` unique across the operator’s deployment.
- **Core attributes:** `name`, date range, ordered `jobIds`, optimizer state, authoring metadata.
- **Relationships:** May contain jobs from multiple farms; referenced by jobs through `seasonId`.
- **Plugin extensions:** Budget snapshots, crop rotation targets, planned profitability stored within `extensions`.
- **Schema:** `schemas/Season.v1.json` establishes the payload, authoring metadata, and validator fields.【F:schemas/Season.v1.json†L1-L91】

### Job.v1
- **Identity:** `job:<slug>` stable across devices.
- **Core attributes:** Primary `farmId`, multi-field `fieldIds[]`, operation classification, planned/actual timing, authoring
  metadata, per-field statistics.
- **Relationships:** Optionally linked to a Season; contains Sessions; references Layers created during execution.
- **Plugin extensions:** Profitability models, zone analytics, crop genetics overlays, and other plugin-owned data live in the
  `extensions` object.
- **Schema:** `schemas/Job.v1.json` adds `seasonId?`, `fieldIds[]`, `sessions[]`, authoring metadata, and extension hooks for
  multi-field envelopes and session tracking.【F:schemas/Job.v1.json†L1-L146】

### Session.v1
- **Identity:** `session:<number-or-uuid>` unique within a job.
- **Core attributes:** Name, start/end timestamps, environment snapshot, input summary, notes, `layerRefs[]`, authoring
  metadata.
- **Relationships:** Belongs to a job; referenced by layers for provenance; surfaced to plugins via lifecycle events.
- **Plugin extensions:** Session `extensions` collect crop-type actuals, rate summaries, operator journals, and other plugin
  insights.
- **Schema:** `schemas/Session.v1.json` captures required metadata, authoring fields, and flexible extension hooks.【F:schemas/Session.v1.json†L1-L99】

### Layer.v1 (update)
- **Identity:** `layer:<slug>` stable across storage round-trips.
- **Core attributes:** Kind, units, job/session references, provenance object (source, transform, hash, createdAt, actor,
  authoring metadata), attachments.
- **Relationships:** Linked to the producing job/session; reused layers retain provenance but may be mounted by later jobs via
  metadata updates.
- **Plugin extensions:** Plugins may append statistics or derived summaries in `extensions` while Core guards canonical
  provenance.
- **Schema:** `schemas/Layer.v1.json` codifies provenance, authoring metadata, and extension requirements for reuse/move
  operations.【F:schemas/Layer.v1.json†L1-L117】

## Relationships & Constraints

| Parent | Child | Cardinality | Notes |
| --- | --- | --- | --- |
| Farm | Field | 1 → N | Fields inherit shared assets from the farm. |
| Season | Job | 0 → N | Jobs may omit `seasonId`; seasons can aggregate jobs across farms. |
| Farm | Job | 1 → N | Every job declares a primary farm; multi-farm jobs use multiple entries in `fieldIds`. |
| Job | Session | 1 → N | Session 1 auto-creates on job start; additional sessions created by operator. |
| Job/Session | Layer | 0 → N | Layers record job and optional session provenance; reuse retains source IDs. |

Additional constraints:
- `fieldIds` must reference fields that belong to either the job’s primary farm or explicitly shared farms when cross-farm work
  is planned.
- Session IDs must remain stable during lifecycle operations so layer provenance stays intact.
- Seasons deduplicate `jobIds`; synchronization merges the set without re-ordering active job lists unless explicitly managed by
  the operator.
- Authoring metadata is immutable once persisted; updates create new `lastModifiedAt` timestamps but do not change `createdBy`
  or `createdAt`.

## Identifier & Provenance Policies

- Identifiers use lowercase slugs with ASCII-safe separators (`-`, `_`, `.`, `:`) to support filesystem storage and URIs.
- Provenance entries record `source`, `transform`, `hash`, `createdAt`, and optional `actor` to trace layer reuse and analytics
  derivations.
- When moving a layer between jobs, tooling updates `jobId`/`sessionId` while appending provenance records; data duplication is
  discouraged in favor of lightweight relinking.

## UX & Navigation Implications

- Navigators support **Season-first** (Season → Farm(s) → Job → Session) and **Farm-first** (Farm → Field → Job → Session) flows.
- Multi-field jobs present aggregated statistics with drill-down to per-field metrics sourced from `job.stats.fields` plus
  plugin-augmented overlays (crop type, profitability) surfaced from `job.extensions`.
- Operators can start a new session mid-job, capturing the environment snapshot and notes without closing the job.

## Open Questions

- How should optimizer state be versioned to ensure backwards compatibility between season planners?
- What access controls are required when seasons span multiple organizations or contractors?
