# 31 — Domain Data Model
*(Status: Proposed)*

**Author:** Codex
**Created:** 2025-10-20
**Section ID:** 31
**Version:** 0.1.0
**Editors:** @nexus-docs-team
**Last Updated:** 2025-10-20
**Related Sections:** [32 — Persistence & Formats](32_Persistence_Formats.md), [33 — Offline-first & Sync](33_Offline_First_Sync.md), [34 — Backup, Retention & Archival](34_Backup_Retention_Archival.md)
**Upstream Dependencies:** [ADR-040](31-ADR-040 - Season Organizers.md), [ADR-041](../6X_Core_Domain_Services/62-ADR-041 - Job Sessions Lifecycle.md), [ADR-043](31-ADR-043 - Multi-Field Job Envelopes.md), [ADR-044](../7X_Mapping_Geospatial/72-ADR-044 - Zone Drawing Framework.md)
**Downstream Impacts:** Schema contracts under `schemas/`, layer catalog governance, season navigation UX, plugin provenance pipelines

---

## 31.1 Purpose & Scope

This section defines the canonical agronomic data hierarchy that Nexus uses to organise spatial assets and operational work. It establishes the Farm → Field spatial lineage, the Season → Job → Session operational cadence, and the shared provenance metadata that binds layers, journals, and analytics across the platform. The scope covers Core-managed identities, plugin extension points, schema ownership, and provenance guarantees required for deterministic replay and compliance reporting.

---

## 31.2 Context

- Core persists immutable identifiers and authoring metadata for every entity, providing a stable anchor for synchronization, schema validation, and provenance reconciliation.【F:docs/sections/3X_Data_Storage/32_Persistence_Formats.md†L12-L156】
- Plugins enrich the hierarchy through `extensions` payloads while respecting Core-owned fields documented in JSON schemas under `schemas/` and ADR-040…ADR-045.【F:schemas/Farm.v1.json†L1-L92】【F:schemas/Field.v1.json†L1-L154】【F:schemas/Season.v1.json†L1-L91】【F:schemas/Job.v1.json†L1-L146】【F:schemas/Session.v1.json†L1-L115】
- Season organisers, job lifecycle services, and layer journaling depend on this hierarchy to coordinate multi-field work, cross-session analytics, and operator navigation flows.【F:docs/sections/3X_Data_Storage/31-ADR-040 - Season Organizers.md†L14-L104】【F:docs/sections/3X_Data_Storage/31-ADR-043 - Multi-Field Job Envelopes.md†L32-L121】【F:docs/sections/7X_Mapping_Geospatial/72-ADR-044 - Zone Drawing Framework.md†L29-L74】

Assumptions and exclusions:
- Legacy AgOpenGPS file layouts remain supported through import/export bridges but do not dictate identifier formats or metadata ownership going forward.
- Hardware inventories, regulatory exports, and analytics layers reference the entities defined here but are specified in their respective SRS sections.

---

## 31.3 Legacy Comparison

| Area / Theme | Legacy Behavior | Identified Limitation | Modernization Opportunity | Reference / Source |
|---------------|-----------------|------------------------|---------------------------|--------------------|
| Architecture | Directories per field/job with implicit relationships encoded by folder names. | Coupled storage prevents cross-field jobs and season views. | Normalize hierarchies with explicit IDs and schema-governed relationships. | [ADR-040](31-ADR-040 - Season Organizers.md) |
| Performance | Flat files per session/layer without provenance hashes. | Hard to audit or replay deterministically. | Embed provenance hashes and session references in schemas to enable deterministic replay. | [ADR-041](../6X_Core_Domain_Services/62-ADR-041 - Job Sessions Lifecycle.md) |
| UX / Config | Operators pivot between farms and seasons manually; plugins store bespoke metadata. | Inconsistent navigation, brittle plugin integrations. | Provide shared navigation flows (Season-first, Farm-first) and typed `extensions` namespaces for plugins. | [ADR-043](31-ADR-043 - Multi-Field Job Envelopes.md) |

---

## 31.4 Definitions

| Term | Definition |
|------|-------------|
| Farm.v1 | Core-owned entity describing operator farms, spatial assets, and shared metadata.【F:schemas/Farm.v1.json†L1-L92】 |
| Field.v1 | Spatial child of a farm containing polygons, headlands, and crop history extensions.【F:schemas/Field.v1.json†L1-L154】 |
| Season.v1 | Operational grouping aggregating jobs across farms and years.【F:schemas/Season.v1.json†L1-L91】 |
| Job.v1 | Work package referencing farms, fields, seasons, and sessions with plugin extension hooks.【F:schemas/Job.v1.json†L1-L146】 |
| Session.v1 | Execution slice within a job carrying environment snapshots, layer references, and provenance.【F:schemas/Session.v1.json†L1-L115】 |
| Layer.v1 | Persisted output or telemetry layer storing provenance, units, and attachments.【F:schemas/Layer.v1.json†L1-L117】 |
| LayerEditEvent.v1 | Journal entry capturing deterministic layer edit provenance and undo/redo linkage.【F:docs/sections/7X_Mapping_Geospatial/72-ADR-044 - Zone Drawing Framework.md†L29-L74】 |
| CropTypeHistoryRecord.v1 | Embedded history for crop rotations tied to fields and crop layers.【F:schemas/CropTypeHistoryRecord.v1.json†L1-L53】 |

---

> **Requirement Grammar (RFC-2119):**
> - **MUST / MUST NOT** = mandatory; test must exist.
> - **SHOULD / SHOULD NOT** = strong recommendation; justify exceptions.
> - **MAY** = optional; document enabling conditions.
>
> **Clarity Checklist:** Avoid weak words: *fast, robust, user-friendly, handle, support, adequate,* etc.
> Prefer measurable forms: *“≤ 250 ms p95,” “error rate < 0.1%,” “99.5% success over 10k trials.”*
> Each requirement: single behavior, single actor, single condition, single metric.

## 31.5 Requirements

| ID | Priority | Category | Summary | Source / C-IDs | Key Metrics / Verification |
|----|-----------|-----------|---------|-----------------|-----------------------------|
| R-31000 | MUST | Capability | Core MUST persist canonical Farm → Field and Season → Job → Session hierarchies with immutable IDs and authoring metadata. | C1, ADR-040 | Schema validation against `Farm.v1`, `Field.v1`, `Season.v1`, `Job.v1`, `Session.v1`; round-trip sync tests.【F:schemas/Farm.v1.json†L1-L92】【F:schemas/Job.v1.json†L1-L146】 |
| R-31001 | MUST | Provenance | Every entity MUST expose `createdBy`, `createdAt`, and `lastModifiedAt` with Core ownership; plugins MAY extend via `extensions`. | C2, ADR-041 | Contract tests ensuring read-only enforcement and plugin serialization.【F:schemas/Field.v1.json†L29-L151】 |
| R-31002 | SHOULD | Extensibility | Plugins SHOULD register schema references for extension payloads to enable validation without Core interpretation. | C3, ADR-045 | Plugin schema registry acceptance tests (lint + CI).【F:schemas/CropTypeHistoryRecord.v1.json†L1-L53】 |
| R-31003 | MUST | Provenance | Layers MUST record `jobId`, `sessionId?`, provenance hashes, and actor metadata for deterministic replay. | C4, ADR-044 | Layer fixture replay verifying provenance chain integrity.【F:schemas/Layer.v1.json†L21-L117】 |
| R-31004 | MUST | Observability | Sessions MUST capture weather snapshots with defined metrics and emit deltas on update. | C5, ADR-053 | Weather snapshot schema validation; replay harness diff tests.【F:schemas/Session.v1.json†L64-L113】 |
| R-31005 | SHOULD | Navigation | UI flows SHOULD offer Season-first and Farm-first traversals backed by canonical hierarchy queries. | C6, ADR-040 | UX acceptance checklist; navigation integration tests.【F:docs/sections/3X_Data_Storage/31-ADR-040 - Season Organizers.md†L58-L104】 |
| R-31006 | MAY | Analytics | Cost, profit, genetics, and risk analytics MAY attach domain-specific facts using `extensions` while preserving Core-owned identifiers. | C7, ADR-050 | Plugin integration smoke tests verifying extensions do not mutate Core fields.【F:docs/sections/7X_Mapping_Geospatial/72-ADR-050 - Cost & Profit Plugin.md†L21-L52】 |

### 31.5.1 Requirement Sources & Rationale

| Req ID | Source (issue/discussion/standard) | Rationale (one line) |
|--------|-------------------------------------|----------------------|
| R-31000 | ADR-040 review minutes, NX-223 season aggregator rollout | Cross-device season navigation requires normalized IDs. |
| R-31001 | ADR-041 session lifecycle QA notes | Provenance auditing demands immutable authoring metadata. |
| R-31002 | ADR-045 crop history schema briefing | Plugin-managed schemas avoid Core re-release for analytics updates. |
| R-31003 | ADR-044 layer journaling sign-off | Replay harnesses depend on consistent provenance chaining. |
| R-31004 | ADR-053 weather plugin kickoff | Compliance exports and agronomy analytics need structured weather snapshots. |
| R-31005 | Season navigator UX study | Operators require both season-centric and farm-centric workflows. |
| R-31006 | ADR-050 cost/profit governance | Financial analytics must extend Core entities without breaking provenance. |

---

## 31.6 Acceptance Criteria & Verification

> **Examples:**
> - Automated unit or integration test coverage thresholds.
> - Simulated scenario replay verification.
> - Manual review or field test sign-off checklist.

### 31.6.1 Requirement-to-Verification Map

| Req ID | Verification Type | Artifact / Location | Pass/Fail Threshold |
|--------|--------------------|---------------------|---------------------|
| R-31000 | Contract tests | `tests/Core/DomainHierarchyTests.cs` (planned) | CRUD + sync round-trips preserve IDs and metadata. |
| R-31001 | Schema lint + CI | `schemas/*.json` validation suite | JSON schema CI reports 0 violations per release. |
| R-31002 | Plugin integration | `plugins/*/tests/ExtensionSchemaTests.cs` | Extensions register schema refs and serialize without Core diffs. |
| R-31003 | Replay harness | `tests/Replays/LayerProvenanceReplay.md` | Deterministic hash match across 10k frames. |
| R-31004 | Simulation fixture | `tests/Replays/WeatherSnapshotReplay.md` | Weather snapshots diff-free against baseline. |
| R-31005 | UX acceptance | `docs/QA/navigation-checklist.md` | All navigation tasks completed ≤ 3 steps. |
| R-31006 | Plugin smoke | `plugins/*/tests/ExtensionInvarianceTests.cs` | Core-owned fields unchanged after plugin persistence cycles. |

---

## 31.7 Constraints

- Identifiers MUST use lowercase slugs with ASCII-safe separators (`-`, `_`, `.`, `:`) for filesystem and URI safety.【F:docs/sections/3X_Data_Storage/31-ADR-043 - Multi-Field Job Envelopes.md†L58-L104】
- `fieldIds` MUST reference farms explicitly authorized for a job; cross-farm jobs require explicit envelopes defined in ADR-043.【F:docs/sections/3X_Data_Storage/31-ADR-043 - Multi-Field Job Envelopes.md†L32-L121】
- Authoring metadata (`createdBy`, `createdAt`) is immutable after persistence; updates MUST write `lastModifiedAt` instead.【F:schemas/Job.v1.json†L63-L142】
- Sessions MUST maintain stable IDs to preserve layer provenance across edits and sync operations.【F:docs/sections/6X_Core_Domain_Services/62-ADR-041 - Job Sessions Lifecycle.md†L40-L92】

### 31.7.1 Non-Functional Requirement Classes

- **Performance:** Hierarchy queries return within 150 ms p95 for 10k entities (target for future performance ADRs).
- **Reliability & Availability:** Sync conflicts resolved deterministically with eventual convergence under offline merges.
- **Security:** Access control inherits from operator identities and season membership policies (see ADR-019).
- **Safety:** Provenance integrity underpins audit trails for agronomic compliance.
- **Usability/UX:** Navigation flows minimize context switching between season-first and farm-first journeys.
- **Operability:** Audit logs track all hierarchy mutations with actor attribution.
- **Portability:** JSON schema definitions remain serializable across Windows and Linux deployments.
- **Maintainability:** Entity schemas versioned via semantic suffixes (`*.v1`) with migration notes in ADRs.

---

## 31.8 Risks & Open Issues

| ID | Description | Impact | Mitigation / Status | Owner |
|----|-------------|--------|---------------------|-------|
| RISK-31-1 | Legacy file imports may omit provenance metadata required by new schemas. | Medium | Import bridge injects default provenance and flags gaps for operator review. | @nexus-docs-team |
| RISK-31-2 | Plugin extensions without schema refs reduce validation coverage. | Medium | Enforce schema registration via CI guardrails (planned). | @nexus-platform |
| ISSUE-31-1 | Need authoritative navigation API for season-first queries. | Low | Draft API in progress under ADR-040 follow-up. | @nexus-core |

---

## 31.9 Design Considerations

| ID | Consideration | Description |
|----|----------------|-------------|
| C1 | Canonical Hierarchies | Farm → Field and Season → Job → Session must remain the system of record across services and storage layers. |
| C2 | Provenance Integrity | Immutable authoring metadata and provenance hashes are required for replay, analytics, and compliance. |
| C3 | Plugin Extensions | `extensions` namespaces allow domain innovation without Core schema churn but must reference typed schemas. |
| C4 | Layer Provenance | Layers depend on stable job/session IDs and provenance journals for deterministic reuse. |
| C5 | Environment Context | Weather snapshots and operator notes enrich sessions for analytics and reporting. |
| C6 | Operator Navigation | UI clients need both season-centric and farm-centric traversal patterns. |
| C7 | Analytics Attachments | Financial, genetics, and risk analytics attach to jobs/sessions via extensions while preserving Core ownership. |

### 31.9.1 Assumptions & Preconditions

- [A1] PoseStream vector logs provide ≥ 25 Hz pose data referenced by sessions and layers.【F:docs/sections/3X_Data_Storage/32_Persistence_Formats.md†L30-L156】
- [A2] Network time synchronization keeps distributed rigs within ±50 ms, ensuring provenance timestamps remain ordered.【F:docs/sections/7X_Mapping_Geospatial/72-ADR-044 - Zone Drawing Framework.md†L29-L74】
- [A3] Operators retain write access to job data directories required for journaling and sync tools.【F:docs/sections/3X_Data_Storage/32_Persistence_Formats.md†L121-L156】

### 31.9.2 Entity Profiles

The following summaries capture Core vs. plugin responsibilities per entity. Detailed schema definitions live under `schemas/`.

#### Farm.v1
- **Identity:** `farm:<slug>` unique to the operator organisation.
- **Core Attributes:** `name`, authoring metadata, optional imagery/asset references, shared operator notes.
- **Relationships:** Owns one or more fields; referenced as the primary farm on a job.
- **Plugin Extensions:** Crop plans, profitability projections, geojson overlays stored verbatim in `extensions`.
- **Schema:** `schemas/Farm.v1.json` defines required fields and scope annotations.【F:schemas/Farm.v1.json†L1-L92】

#### Field.v1
- **Identity:** `field:<slug>` unique within a farm.
- **Core Attributes:** Polygons with optional holes, headlands, tags, notes, authoring metadata.
- **Relationships:** Belongs to a farm; referenced by jobs via `fieldIds`.
- **Geometry:** Stored as polygon exteriors and holes using `[lon, lat (, elevation)]` coordinates; headlands capture width, units, optional pass counts.
- **Plugin Extensions:** Zone drawings, soil sampling, crop rotation histories inside `extensions`.
- **Schema:** `schemas/Field.v1.json` outlines geometry metadata and plugin hooks.【F:schemas/Field.v1.json†L1-L154】

#### Season.v1
- **Identity:** `season:<year-or-label>` unique across deployments.
- **Core Attributes:** `name`, date range, ordered `jobIds`, optimizer state, authoring metadata.
- **Relationships:** Aggregates jobs from multiple farms; referenced by jobs via `seasonId`.
- **Plugin Extensions:** Budget snapshots, crop rotation targets, planned profitability.
- **Schema:** `schemas/Season.v1.json` documents payload and validator fields.【F:schemas/Season.v1.json†L1-L91】

#### Job.v1
- **Identity:** `job:<slug>` stable across devices.
- **Core Attributes:** Primary `farmId`, multi-field `fieldIds[]`, operation classification, planned/actual timing, authoring metadata, per-field statistics.
- **Relationships:** Optionally linked to a season; contains sessions; references layers created during execution.
- **Plugin Extensions:** Profitability models, analytics overlays, plugin-owned lifecycle data.
- **Schema:** `schemas/Job.v1.json` sets envelope semantics and extension hooks.【F:schemas/Job.v1.json†L1-L146】

#### Session.v1
- **Identity:** `session:<number-or-uuid>` unique within a job.
- **Core Attributes:** Name, start/end timestamps, environment snapshot, input summary, notes, `layerRefs[]`, authoring metadata.
- **Relationships:** Belongs to a job; referenced by layers for provenance.
- **Weather Snapshot:** Captures environmental metrics with optional updates emitted via lifecycle events.
- **Plugin Extensions:** Journals, rate summaries, operator notes, analytics metadata.
- **Schema:** `schemas/Session.v1.json` defines required metadata and extension hooks.【F:schemas/Session.v1.json†L1-L115】

#### Layer.v1 and Journals
- **Layer.v1:** Stores kind, units, job/session references, provenance object, attachments, and plugin extensions.【F:schemas/Layer.v1.json†L1-L117】
- **LayerEditEvent.v1:** Immutable journal entries linking edits to jobs, sessions, and collaborative mesh replication.【F:docs/sections/7X_Mapping_Geospatial/72-ADR-044 - Zone Drawing Framework.md†L29-L74】

#### Embedded Records
- **CropTypeHistoryRecord.v1:** Records crop status, year, source, and provenance references for analytics without mutating core geometry.【F:schemas/CropTypeHistoryRecord.v1.json†L1-L53】
- **GeneticsPlan.v1 / GeneticsVariety.v1:** Plugin-owned plan/actual layer schemas preserving provenance to jobs/sessions.【F:docs/sections/7X_Mapping_Geospatial/72-ADR-046 - Genetics Plugin & Layers.md†L21-L66】
- **CostRecord.v1 / ProfitLayer.v1:** Financial extensions capturing expenses, revenues, and attribution.【F:docs/sections/7X_Mapping_Geospatial/72-ADR-050 - Cost & Profit Plugin.md†L21-L52】
- **WeatherOverlay.v1:** Raster overlays referencing weather sources with plugin-owned value arrays.【F:docs/sections/7X_Mapping_Geospatial/72-ADR-053 - Weather & Environment Plugin.md†L21-L49】

---
