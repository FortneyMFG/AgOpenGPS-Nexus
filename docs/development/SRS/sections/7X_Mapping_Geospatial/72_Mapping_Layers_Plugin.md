# 72 — Mapping Layers Plugin
*(Status: Proposed)*

**Author:** Codex
**Created:** 2025-10-20
**Version:** 0.1.0
**Section ID:** 72
**Editors:** Mapping & Geospatial Working Group
**Last Updated:** 2025-10-20
**Related Sections:** 71 — Mapping Kernel Contracts, 62 — Job Lifecycle, 64 — Telemetry & Health, 81 — Guidance Orchestrator
**Upstream Dependencies:** ADR-030, ADR-041, ADR-044, ADR-047, ADR-045–ADR-053
**Downstream Impacts:** Mapping UI, Rate/Guidance plugins, Analytics & Reporting layers

---

## 72.1 Purpose & Scope

Specify expectations for Nexus mapping plugins that render coverage, agronomic overlays, and guidance artifacts while consuming lifecycle events and registry metadata from Core. The section governs how plugins hydrate multi-field envelopes, maintain provenance, and participate in collaborative zone editing.

---

## 72.2 Context

- Job and session lifecycle events deliver farm/field envelopes, session IDs, and plugin extension payloads used to hydrate caches.【F:docs/sections/6X_Core_Domain_Services/62_Job_Lifecycle.md†L18-L66】
- LayerEditService in ADR-044 centralizes geometry editing, journaling, and undo/redo semantics shared across plugins.【F:docs/sections/7X_Mapping_Geospatial/72-ADR-044 - Zone Drawing Framework.md†L29-L74】
- Telemetry mesh (ADR-047) replicates live layer edits and controller telemetry so collaborative users share a coherent view.【F:docs/sections/4X_Interprocess_Communications/42-ADR-047 - Live Telemetry Mesh.md†L33-L62】
- Plugin-specific ADRs define schemas for crop type, genetics, yield, profit, risk, and weather overlays that must align with Layer Registry entries.【F:docs/sections/7X_Mapping_Geospatial/72-ADR-045 - Crop Type Plugin & Layers.md†L29-L71】【F:docs/sections/7X_Mapping_Geospatial/72-ADR-053 - Weather & Environment Plugin.md†L21-L49】

---

## 72.3 Legacy Comparison

| Area / Theme | Legacy Behavior | Identified Limitation | Modernization Opportunity | Reference / Source |
|---------------|-----------------|------------------------|---------------------------|--------------------|
| Multi-Field Handling | Plugins mounted one field at a time with manual boundary switching. | Crossing field edges interrupted coverage and statistics. | Adopt union envelopes with shared R-tree queries and lifecycle updates. | ADR-043 review |
| Layer Provenance | Layers stored as ad-hoc files without provenance metadata. | Difficult to audit or reuse layers across jobs. | Require provenance blocks per `Layer.v1` and session linkage. | Layer schema audit |
| Zone Editing | Individual plugins shipped unique editing tools. | No shared undo/redo or collaborative editing path. | Centralize editing in LayerEditService (ADR-044). | Zone framework RFC |

---

## 72.4 Definitions

| Term | Definition |
|------|-------------|
| Union Envelope | Combined polygon covering all mounted fields for a job session, used for navigation and analytics. |
| LayerEditService | Core-hosted editing service providing journaled geometry edits and shared tooling per ADR-044. |
| Provenance DAG | Directed acyclic graph representing layer creation and transformations for audits and reuse. |
| Plugin Extension Bag | Structured JSON payload emitted in job/session context events so plugins surface additional overlays. |

---

> **Requirement Grammar (RFC-2119):**
> - **MUST / MUST NOT** = mandatory expectations with verification.
> - **SHOULD / SHOULD NOT** = strong preference subject to waiver.
> - **MAY** = optional capability recorded with enabling conditions.

## 72.5 Requirements

| ID | Priority | Category | Summary | Source / C-IDs | Key Metrics / Verification |
|----|-----------|-----------|---------|-----------------|-----------------------------|
| R-MAP-7200 | MUST | Lifecycle | Plugins MUST consume `onFarmLoaded`, `onSeasonLoaded`, `onJobLoaded`, `onContextChanged`, and `onSessionStart` events to hydrate caches before rendering. | ADR-030/041 | Automated integration harness verifies event handling order. |
| R-MAP-7201 | MUST | Geometry | Multi-field mounts MUST construct a union envelope and per-field R-tree enabling ≤ 25 ms p95 spatial lookups for up to 10 fields. | ADR-043 | Performance bench in `bench/mapping/envelope_mount.md`. |
| R-MAP-7202 | MUST | Provenance | Layers written by plugins MUST populate provenance blocks, `jobId`, optional `sessionId`, and register IDs in `session.layerRefs[]`. | Layer.v1 schema | Schema validation tests and replay audits. |
| R-MAP-7203 | SHOULD | UX | Layer drawers SHOULD group overlays by session and expose provenance badges, move/reuse flows, and toggle planned vs. actual states. | Mapping UI backlog | UI automation verifying drawer affordances. |
| R-MAP-7204 | MUST | Zone Editing | Editable layers MUST integrate with LayerEditService hooks (`onLayerStartEdit`, `onFeatureCommit`, undo/redo). | ADR-044 | Manual + automated editing tests. |
| R-MAP-7205 | SHOULD | Telemetry | Plugins SHOULD publish commanded vs. actual metrics and provenance updates into telemetry topics for audit. | ADR-047, Telemetry SRS | Telemetry integration tests confirm event emission. |

### 72.5.1 Plugin API Surface

| Plugin Type | Required Hooks |
| --- | --- |
| Mapping | `mountFields(fieldIds[])`, `setActiveSession(sessionId)`, `writeLayer(layerId, payload, provenance)`, LayerEditService callbacks; emit per-field stats. |
| Rate / Sections | Consume lifecycle context, respect keep-outs/headlands, and write provenance-aware actual layers. |
| Guidance | Use union envelopes for lookahead; pause automation when LayerEditService is active to avoid conflicting edits. |
| Analytics / Export | Filter by `seasonId`, `jobId`, `sessionId`; honor provenance metadata when producing planned vs. actual reports. |

### 72.5.2 Layer Catalog Participation

- Crop type, genetics, yield, profit, risk, weather, soil, terrain, drainage, and advisor layers MUST align with registry schema definitions published alongside plugin ADRs.【F:docs/sections/7X_Mapping_Geospatial/72-ADR-045 - Crop Type Plugin & Layers.md†L29-L71】【F:docs/Plugins/Terrain3D.md†L1-L140】
- Imports via NX-113 normalization flow MUST record source hashes, operator IDs, and transforms prior to persistence to maintain provenance DAG integrity.

---

## 72.6 Acceptance Criteria & Verification

- Scenario tests mount multi-field envelopes, edit layers collaboratively, and verify provenance replay without divergence.
- Import/export smoke tests validate GeoTIFF, GeoJSON, and ISOXML pathways populate registry-compliant metadata.
- UI regression suites ensure layer drawers, provenance badges, and session grouping remain functional across sessions.

### 72.6.1 Requirement-to-Verification Map

| Req ID | Verification Type | Artifact / Location | Pass/Fail Threshold |
|--------|--------------------|---------------------|---------------------|
| R-MAP-7200 | Lifecycle integration | `tests/integration/mapping_plugin_lifecycle.cs` | Event ordering matches spec; caches hydrated before render. |
| R-MAP-7201 | Performance bench | `bench/mapping/envelope_mount.md` | Union envelope queries ≤ 25 ms p95 for 10 fields. |
| R-MAP-7202 | Schema validation | `tests/schemas/layer_provenance.spec` | 100% provenance fields populated in fixtures. |
| R-MAP-7204 | Functional test | `tests/integration/layer_edit_service.cs` | Undo/redo + journal replay succeeds with no conflicts. |
| R-MAP-7205 | Telemetry test | `tests/telemetry/mapping_metrics.cs` | Commanded vs. actual metrics available in telemetry stream. |

---

## 72.7 Constraints

- Shared editing toolbar modes originate from Core; plugins may not override geometry primitives without ADR approval.
- Autosave cadence and TileStore flush behavior must match Section 71 persistence guarantees to avoid data loss.
- Mapping plugins must respect embedded hardware budgets (≤ 1.5 GB RAM, ≤ 256 MB tile cache) during intensive sessions.

### 72.7.1 Non-Functional Requirement Classes

- **Performance:** Envelope queries, tile streaming, UI rendering latency.
- **Reliability:** Autosave + provenance replay fidelity, collaborative edit resilience.
- **Security:** Authenticated plugin manifests, provenance chain integrity.
- **Usability:** Layer drawer clarity, session grouping, conflict resolution dialogs.
- **Operability:** Diagnostics for layer imports, telemetry for cache health.

---

## 72.8 Risks & Open Issues

| ID | Description | Impact | Mitigation / Status | Owner |
|----|-------------|--------|---------------------|-------|
| RISK-72-1 | Multi-field envelopes exceed performance targets on low-end hardware. | Medium | Profile union algorithms; cache spatial indices per session. | @mapping |
| RISK-72-2 | Provenance DAG cycles caused by manual layer moves. | Low | Enforce DAG validation before commit; expose UI diagnostics. | @core |
| ISSUE-72-1 | Define fallback when a mounted field polygon is missing/corrupt. | Medium | Pending decision in guidance + mapping WG. | @mapping |
| ISSUE-72-2 | Expressing partial field ownership in union envelopes. | Medium | Track in ADR follow-up; may require envelope metadata extensions. | @product |

---

## 72.9 Design Considerations

| ID | Consideration | Description |
|----|----------------|-------------|
| C1 | Multi-Field Navigation | Operators expect seamless traversal across adjacent fields without prompts. |
| C2 | Provenance Transparency | Layer drawers, telemetry, and reports must surface provenance for audits and reuse. |
| C3 | Collaborative Editing | Shared LayerEditService enables deterministic undo/redo and telemetry replication. |
| C4 | Import Normalization | External GIS data must normalize into registry schemas with reproducible transforms. |
| C5 | UI Ergonomics | Layer drawers, session grouping, and provenance badges guide operators through complex overlays. |
| C6 | Hardware Constraints | Tile caches and spatial indices must respect Raspberry Pi resource budgets. |

### 72.9.1 Assumptions & Preconditions

- [A1] Core lifecycle events remain authoritative and reliable for context hydration.
- [A2] Plugins integrate telemetry emission libraries for provenance and rate metrics.
- [A3] Operators possess necessary permissions to reuse or relocate layers across jobs.

---

## 72.10 Option Overview

| Option ID | Status | Type / Theme | Description | Reference Document |
|-----------|--------|--------------|-------------|--------------------|
| — | — | — | All architectural trade-offs reflected as design considerations in §72.9. | — |

---

## 72.11 Comparison Matrix

| Attribute / Criteria | Unified Mapping Plugin Framework | Legacy Plugin Integrations |
|----------------------|----------------------------------|----------------------------|
| Envelope Handling | Union envelopes with R-tree lookups | Per-field manual selection |
| Provenance | Mandatory provenance DAG + layer references | Ad-hoc filenames and notes |
| Editing | Shared LayerEditService with journaling | Plugin-specific tools, no shared undo |
| Telemetry | Commanded vs. actual metrics exported | Limited or no telemetry hooks |
| Import/Export | Normalized ingest with provenance capture | Manual GIS conversions |
