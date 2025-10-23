# 71 — Mapping Kernel & Registry Contracts
*(Status: Proposed)*

**Author:** Codex
**Created:** 2025-10-20
**Version:** 0.1.0
**Section ID:** 71
**Editors:** Mapping & Geospatial Working Group
**Last Updated:** 2025-10-24
**Related Sections:** 32 — Persistence & Formats, 62 — Job Lifecycle, 63 — Layers Registry & Journal Contracts, 72 — Mapping Layers Plugin, 73 — Variable Mapping, 77 — Variable Rate Control
**Upstream Dependencies:** ADR-009, ADR-010, ADR-030, ADR-040, ADR-041, ADR-044, ADR-047
**Downstream Impacts:** Layer Registry schemas, Mapping plugins, Analytics & Reporting services

---

## 71.1 Purpose & Scope

Define the Nexus Core mapping kernel contracts exposed over gRPC along with the authoritative layer catalog governed by the Layer Registry. The section aligns session lifecycle services, plugin overlays, and persistence semantics so plugins, analytics, and reporting stacks can interoperate deterministically.

---

## 71.2 Context

- Core emits farm, field, season, job, and session lifecycle events captured in ADR-030/040/041 and consumed by mapping plugins.【F:docs/development/SRS/sections/6X_Core_Domain_Services/62-ADR-030 - Field job sessions and lifecycle services.md†L1-L66】【F:docs/development/SRS/sections/6X_Core_Domain_Services/62-ADR-041 - Job Sessions Lifecycle.md†L1-L60】
- Layer Registry governance (ADR-010) standardizes identifiers, provenance rules, and schema hashes for all mapping overlays.【F:docs/development/SRS/sections/3X_Data_Storage/32-ADR-010 - Layer registry and variable-rate framework.md†L11-L40】
- TileStore persistence (ADR-009) and telemetry mesh coordination (ADR-047) guarantee reliable replication of coverage, telemetry, and edit journals.【F:docs/development/SRS/sections/3X_Data_Storage/32-ADR-009 - PoseStream vector logs and layer TileStore persistence.md†L11-L45】【F:docs/development/SRS/sections/4X_Interprocess_Communications/42-ADR-047 - Live Telemetry Mesh.md†L21-L71】
- Analytics and profitability overlays rely on plugin ADRs (045–053) to keep layer semantics compatible with reporting and advisor tooling.【F:docs/development/SRS/sections/7X_Mapping_Geospatial/72-ADR-045 - Crop Type Plugin & Layers.md†L1-L52】【F:docs/development/SRS/sections/7X_Mapping_Geospatial/72-ADR-050 - Cost & Profit Plugin.md†L1-L52】

---

## 71.3 Legacy Comparison

| Area / Theme | Legacy Behavior | Identified Limitation | Modernization Opportunity | Reference / Source |
|---------------|-----------------|------------------------|---------------------------|--------------------|
| Mapping Contracts | WinForms-era mapping used implicit file drops with ad-hoc identifiers. | Plugins duplicated schemas and could not rely on deterministic provenance. | Centralize layer definitions and provenance in Layer Registry with gRPC services. | ADR-009/010 archives |
| Session Lifecycle | Mapping listened to UI-specific events only. | Telemetry or analytics integrations missed lifecycle transitions. | Publish lifecycle bus via Core services to keep all consumers synchronized. | ADR-030/041 discussions |
| Tile Storage | Raster/vector persistence used proprietary formats. | Failed to deduplicate reused layers or support collaborative editing. | Adopt TileStore chunking and LayerEditEvent journals under ADR-009/044. | Legacy storage audit |

---

## 71.4 Definitions

| Term | Definition |
|------|-------------|
| Layer Registry | Canonical catalog of layer identifiers, schemas, and provenance metadata governed by ADR-010. |
| TileStore | Chunked binary persistence format for raster/vector tiles defined in ADR-009. |
| Lifecycle Bus | Event stream for farm, season, job, and session state transitions exposed by JobService/SessionService. |
| Provenance | Hash-linked audit metadata recording layer author, source, transform pipeline, and timestamps. |

---

> **Requirement Grammar (RFC-2119):**
> - **MUST / MUST NOT** = mandatory; verification required.
> - **SHOULD / SHOULD NOT** = strong recommendation; justify any exception.
> - **MAY** = optional capability; document enabling conditions.

## 71.5 Requirements

| ID | Priority | Category | Summary | Source / C-IDs | Key Metrics / Verification |
|----|-----------|-----------|---------|-----------------|-----------------------------|
| R-MAP-7100 | MUST | API | Core MUST expose the mapping kernel gRPC services listed in §71.5.1 with stable service/package names. | ADR-009/010/030/041 | Contract unit tests ensure descriptors match registry definitions. |
| R-MAP-7101 | MUST | Persistence | Every persisted layer MUST include `jobId`, optional `sessionId`, and provenance entries per `Layer.v1`. | ADR-010 | Schema validation in CI + integration tests on TileStore pipelines. |
| R-MAP-7102 | MUST | Registry | Layer identifiers listed in §71.5.2 MUST remain unique and traceable through registry hashes. | Layer Registry Charter | Registry lint job checks for duplicates and hash drift. |
| R-MAP-7103 | SHOULD | Performance | Lifecycle service calls SHOULD complete ≤ 150 ms p95 for union envelope mounts up to 10 fields. | Mapping WG perf notes | CI perf harness with synthetic envelopes. |
| R-MAP-7104 | MAY | Extensibility | New layer families MAY be added when accompanied by ADR + schema update and design consideration review. | ADR process | Release checklist ensures ADR recorded before enabling IDs. |

### 71.5.1 Core gRPC Service Catalog

| Service | Purpose | Status | Notes |
| --- | --- | --- | --- |
| `aog.core.FarmService` | Manage farm records backed by `Farm.v1`. | Reserved | Aligns identifiers and plugin extension bags.【F:schemas/Farm.v1.json†L1-L61】 |
| `aog.core.FieldService` | Govern field geometry, headlands, assets. | Reserved | Coordinates job envelopes and session context.【F:schemas/Field.v1.json†L1-L77】 |
| `aog.core.SeasonService` | Organize jobs into seasons and broadcast context. | Drafting | Based on ADR-040 lifecycle flows.【F:docs/development/SRS/sections/3X_Data_Storage/31-ADR-040 - Season Organizers.md†L1-L60】 |
| `aog.job.JobService` | Create, mount, resume, and close jobs. | Proposed | Defines lifecycle verbs and journaling semantics.【F:docs/development/SRS/sections/6X_Core_Domain_Services/62-ADR-030 - Field job sessions and lifecycle services.md†L1-L66】 |
| `aog.job.SessionService` | Manage session lifecycle, notes, and weather snapshots. | Drafting | Anchored by ADR-041 and `Session.v1`.【F:schemas/Session.v1.json†L1-L84】 |
| `aog.layer.LayerService` | Enumerate, read, and update layer metadata. | Drafting | Follows Layer Registry governance.【F:docs/development/SRS/sections/3X_Data_Storage/32-ADR-010 - Layer registry and variable-rate framework.md†L11-L40】 |
| `aog.layer.TileStoreService` | Upload/download tile payloads. | In Review | Implements TileStore chunking semantics.【F:docs/development/SRS/sections/3X_Data_Storage/32-ADR-009 - PoseStream vector logs and layer TileStore persistence.md†L11-L45】 |
| `aog.telemetry.TelemetryService` | Publish telemetry and diagnostics. | Drafting | Shares topics with telemetry mesh ADR-047.【F:docs/development/SRS/sections/4X_Interprocess_Communications/42-ADR-047 - Live Telemetry Mesh.md†L21-L71】 |
| `aog.machine.DeviceService` | Enumerate connected devices and health. | Drafting | Anchored by `Device.v1`.【F:schemas/Device.v1.json†L1-L80】 |
| `aog.machine.MultiMachineService` | Coordinate multi-rig presence. | Drafting | Uses telemetry mesh ACL/QoS policies.【F:docs/development/SRS/sections/4X_Interprocess_Communications/42-ADR-047 - Live Telemetry Mesh.md†L33-L62】 |
| `aog.analytics.AnalyticsService` | Run yield, profitability, and VR summaries. | Drafting | Relies on analytics plugin ADRs.【F:docs/development/SRS/sections/7X_Mapping_Geospatial/72-ADR-049 - Yield & Analytics Plugin.md†L1-L56】【F:docs/development/SRS/sections/7X_Mapping_Geospatial/72-ADR-050 - Cost & Profit Plugin.md†L1-L52】 |
| `aog.file.FileIOService` | Import/export mapping artifacts. | Proposed | Governed by ADR-030 import/export mandates.【F:docs/development/SRS/sections/6X_Core_Domain_Services/62-ADR-030 - Field job sessions and lifecycle services.md†L14-L38】 |
| `aog.map.ZoneService` | Support geometry editing via zone framework. | Drafting | Works with ADR-044 zone editing flows.【F:docs/development/SRS/sections/7X_Mapping_Geospatial/72-ADR-044 - Zone Drawing Framework.md†L21-L74】 |
| `aog.report.ReportService` | Generate reports from provenance data. | Drafting | Integrates with Report Builder ADR-051.【F:docs/development/SRS/sections/9X_Frontends_Ops/91-ADR-051 - Report Builder & Export System.md†L1-L68】 |

### 71.5.2 Layer Catalog Overview

Layer identifiers governed by the registry remain canonical for mapping, analytics, and reporting stacks.

- Variable Rate & Agronomic layers include planned/actual rate surfaces for seed, fertilizer, and crop protection outputs.【F:docs/development/SRS/sections/3X_Data_Storage/32-ADR-010 - Layer registry and variable-rate framework.md†L35-L53】
- Coverage, crop type, genetics, yield, profit, risk, weather, soil, imagery, geometry, telemetry, and administrative overlays follow the tabulated definitions in the legacy catalog; registry tooling verifies uniqueness and provenance metadata.
- Reserved namespaces (`soil.*`, `geometry.tile`, etc.) remain blocked until corresponding ADRs ratify schemas.

Detailed identifier tables stay published in the registry documentation to prevent duplication.

### 71.5.3 Context Bus Alignment

Farm → Field → Season → Job → Session contexts MUST be emitted over the lifecycle bus so plugins synchronize caches and provenance fields without polling.【F:docs/development/SRS/sections/3X_Data_Storage/31-ADR-040 - Season Organizers.md†L21-L44】【F:docs/development/SRS/sections/6X_Core_Domain_Services/62-ADR-041 - Job Sessions Lifecycle.md†L52-L72】

---

## 71.6 Acceptance Criteria & Verification

- Contract smoke tests validate every gRPC descriptor against canonical protobuf definitions.
- TileStore integration tests ingest sample layers and verify provenance hashes remain stable across replays.
- Lifecycle bus replay harness confirms farm/field/job/session events arrive in order for multi-field envelopes.

### 71.6.1 Requirement-to-Verification Map

| Req ID | Verification Type | Artifact / Location | Pass/Fail Threshold |
|--------|--------------------|---------------------|---------------------|
| R-MAP-7100 | Contract tests | `tests/contracts/mapping_kernel/` | All services register with expected names & fields. |
| R-MAP-7101 | Integration tests | `tests/integration/tilestore_provenance.cs` | Provenance hash matches baseline vector. |
| R-MAP-7102 | Registry lint | `tools/registry/layer_catalog_lint.py` | No duplicate IDs; schema hashes match registry. |
| R-MAP-7103 | Performance harness | `bench/mapping/envelope_mount.md` | p95 latency ≤ 150 ms for 10-field mounts. |

---

## 71.7 Constraints

- gRPC contract changes require ADR review and migration plan for plugins.
- Layer Registry schemas must remain backward compatible or ship conversion tooling.
- TileStore storage footprint per session MUST remain within Raspberry Pi budget (≤ 256 MB active tiles).

### 71.7.1 Non-Functional Requirement Classes

- **Performance:** Envelope mount latency, tile streaming throughput.
- **Reliability & Availability:** Crash-safe journaling, deterministic provenance hashes.
- **Security:** Authenticated service endpoints, provenance integrity verification.
- **Operability:** Layer catalog linting, telemetry for TileStore health.
- **Maintainability:** Generated gRPC stubs, schema versioning disciplines.
- **Portability:** Linux ARM64/AMD64 support for TileStore and gRPC services.

---

## 71.8 Risks & Open Issues

| ID | Description | Impact | Mitigation / Status | Owner |
|----|-------------|--------|---------------------|-------|
| RISK-71-1 | Registry drift between schemas and gRPC descriptors. | Medium | Automate contract lint + schema hash checks in CI. | @registry |
| RISK-71-2 | TileStore size exceeds embedded hardware limits. | Medium | Implement compression + eviction policies; monitor metrics. | @mapping |
| ISSUE-71-1 | Define multi-tenant access control for analytics services. | Medium | Pending security ADR; track in telemetry mesh backlog. | @security |
| ISSUE-71-2 | Finalize reserved soil namespace schemas. | Low | Coordinate with agronomy advisors; draft ADR. | @analytics |

---

## 71.9 Design Considerations

| ID | Consideration | Description |
|----|----------------|-------------|
| C1 | Registry Governance | Layer IDs and schemas require central stewardship to prevent collisions and regressions. |
| C2 | Provenance Integrity | Hash-linked provenance chains enable audits and collaborative editing recovery. |
| C3 | Multi-Field Envelopes | Union envelopes must stay deterministic for coverage, guidance, and analytics consistency. |
| C4 | Telemetry Mesh Integration | Live replication of edits and telemetry depends on ADR-047 QoS policies. |
| C5 | Plugin Extensibility | Contracts must expose extension bags for plugin overlays without core schema churn. |
| C6 | Resource Limits | Embedded targets constrain tile cache size and gRPC concurrency. |

### 71.9.1 Assumptions & Preconditions

- [A1] Layer Registry automation remains authoritative for ID/hash publication.
- [A2] Plugins adopt Job/Session lifecycle events without custom polling.
- [A3] TileStore deployment targets include SSD-class storage for buffering.

---

## 71.10 Option Overview

| Option ID | Status | Type / Theme | Description | Reference Document |
|-----------|--------|--------------|-------------|--------------------|
| — | — | — | No standalone option documents. Design considerations captured in §71.9. | — |

---

## 71.11 Comparison Matrix

| Attribute / Criteria | Unified Core Mapping Kernel | Legacy File-Based Mapping |
|----------------------|------------------------------|---------------------------|
| Determinism | High — governed by registry + provenance hashes. | Low — manual file handling. |
| Plugin Compatibility | High — shared gRPC contracts. | Low — bespoke integrations. |
| Persistence Safety | High — TileStore journaling and autosave. | Medium — ad-hoc copies prone to corruption. |
| Scalability | High — multi-field envelopes and analytics services. | Low — single-field assumption. |
| Operational Overhead | Medium — registry maintenance required. | Medium — manual coordination required. |
