# 64 — Telemetry & Health Services
*(Status: Proposed)*

**Author:** Codex
**Created:** 2025-10-20
**Status:** Proposed
**Version:** 0.1.0
**Section ID:** 64
**Editors:** @Codex
**Last Updated:** 2025-10-20
**Related Sections:** 61 Kinematics & Pose Fusion, 62 Job Lifecycle, 42 Interprocess Communications
**Upstream Dependencies:** 42-ADR-047 Live Telemetry Mesh, 21-O6 Linux Core Service
**Downstream Impacts:** Automation Engine, Analytics Dashboards, Equipment Health Records

---

## 64.1 Purpose & Scope

This section defines telemetry, diagnostics, and health reporting services that observe
system behavior, log events, surface alerts, and expose structured feeds for operators,
maintainers, and remote dashboards across desktop, kiosk, and headless deployments.【F:docs/development/SRS/sections/6X_Core_Domain_Services/64_Telemetry_Health.md†L1-L116】

---

## 64.2 Context

- Depends on AgIO diagnostics, Linux Core services, Live Telemetry Mesh, and Layer telemetry.
- Interacts with automation engines, weather logging, equipment maintenance workflows,
  and remote dashboards.
- Out of scope: hardware sensor design or third-party cloud analytics services.

---

## 64.3 Legacy Comparison

| Area / Theme | Legacy Behavior | Identified Limitation | Modernization Opportunity | Reference / Source |
|--------------|-----------------|-----------------------|---------------------------|--------------------|
| Diagnostics UI | WinForms monitors for UDP/serial traffic and PGN inspection. | Manual workflows and limited automation. | Retain monitors while adding machine-readable telemetry feeds. | 【F:docs/development/SRS/sections/6X_Core_Domain_Services/64_Telemetry_Health.md†L7-L23】 |
| Health Endpoints | Ad hoc scripts for watchdogs and service restarts. | No standardized `/healthz` or structured logs. | Expose Linux Core health endpoints and structured logging. | 【F:docs/development/SRS/sections/6X_Core_Domain_Services/64_Telemetry_Health.md†L15-L21】 |
| Telemetry Coverage | Manual inspection of layer quality and packet rates. | Difficult to detect degradation early. | Provide layer-aware diagnostics and alerting. | 【F:docs/development/SRS/sections/6X_Core_Domain_Services/64_Telemetry_Health.md†L21-L32】 |

---

## 64.4 Definitions

| Term | Definition |
|------|------------|
| Telemetry Feed | Structured metrics, counters, and events published for monitoring and analytics. |
| Health Endpoint | Network endpoint reporting service readiness, dependency checks, and error states. |
| Advisory Mask | Non-blocking spatial overlay providing operator warnings without enforcing automation gates. |
| EquipmentHealthRecord | Document aggregating maintenance-relevant telemetry, cycles, and alerts. |

---

## 64.5 Requirements

| ID | Priority | Category | Summary | Source / C-IDs | Key Metrics / Verification |
|----|----------|----------|---------|-----------------|----------------------------|
| R-TH-000 | MUST | Diagnostics | Retain UDP and serial monitor dialogs that log and export raw traffic. | C1 | UI regression verifying monitor availability and export. |
| R-TH-001 | MUST | Diagnostics | Maintain event log viewers consolidating historical and in-session diagnostics. | C1 | Integration test confirming event viewer persists entries. |
| R-TH-002 | SHOULD | Diagnostics | Continue exposing PGN inspector tools and message guides. | C1 | UI test verifying PGN inspector functionality. |
| R-TH-003 | SHOULD | Telemetry | Provide machine-readable telemetry feeds for remote monitoring. | C2 | Metrics exporter smoke ensuring schema contract. |
| R-TH-004 | SHOULD | Service Health | Expose `/healthz`, structured logs, and watchdog hooks in Linux Core service. | C2 | System test verifying watchdog restarts and health endpoint responses. |
| R-TH-005 | COULD | Alerting | Add health scoring/alerting correlating GNSS, network, and firmware states. | C3 | Alert simulator verifying scoring thresholds. |
| R-TH-010 | SHOULD | Layer Diagnostics | Surface packet-rate monitors, legend parity tests, and bad-sample counters tied to layers. | C4 | Telemetry test ensuring counters emitted with layer metadata. |
| R-TH-011 | SHOULD | UI Guidance | Publish operator guidance overlays borrowing AgDiag tooling with opt-in controls. | C4 | UX test verifying overlays toggle and log consent. |
| R-TH-012 | SHOULD | Governance | Define log retention, alert routing, and privacy constraints for structured telemetry. | C5 | Policy review ensuring retention + consent documented. |
| R-TH-020 | MUST | Mapping Integration | Document PoseStream sample usage for ribbons, heatmaps, and contour layers with deterministic interpolation. | C6 | Replay test comparing UI/export parity. |
| R-TH-021 | SHOULD | Visualization | Specify level-of-detail and legend behaviors near the vehicle for consistent rendering. | C6 | Visual regression verifying LOD policies. |
| R-TH-022 | MUST | Constraint Alerts | Emit real-time alerts when keep-out zones inhibit guidance or product application. | C7 | Simulation verifying alert latency ≤ 250 ms. |
| R-TH-023 | SHOULD | Constraint Audit | Log zone-driven gates and overrides with actor/timestamp for audits. | C7 | Audit log inspection ensuring fields captured. |
| R-TH-030 | MUST | Telemetry Mesh | Stream presence, trail, coverage, and layer-delta topics with QoS tiers and ACL enforcement. | C8 | Mesh test ensuring QoS + ACL compliance. |
| R-TH-031 | SHOULD | Radio Diagnostics | Expose RadioBridge counters (RSSI, retries, FEC, encryption) in dashboards/logs. | C8 | Telemetry verification with radio simulator. |
| R-TH-032 | SHOULD | Edit History | Forward `LayerEditEvent` summaries to telemetry feeds for collaborative visibility. | C4 | Subscription test confirming edit events broadcast. |
| R-TH-033 | SHOULD | Weather Logging | Track weather auto-logging cadence and sensor availability for alerting. | C9 | Weather ingest test verifying cadence monitoring. |
| R-TH-034 | MUST | Equipment Maintenance | Aggregate telemetry into `EquipmentHealthRecord` documents for maintenance scheduling. | C10 | Data pipeline test ensuring record completeness. |
| R-TH-035 | SHOULD | Maintenance Workflow | Emit maintenance due/overdue events with recommended tasks and parts. | C10 | TaskService sync verifying events generated. |
| R-TH-036 | SHOULD | Automation Audit | Provide rule evaluation telemetry for the Automation Engine. | C11 | Automation test verifying telemetry entries per rule execution. |

### 64.5.1 Requirement Sources & Rationale

| Req ID | Source | Rationale |
|--------|--------|-----------|
| R-TH-000 | AgIO diagnostics backlog | Preserve operator troubleshooting workflows. |
| R-TH-004 | Linux Core service pilots | Enable watchdog integration and remote dashboards. |
| R-TH-010 | Layer diagnostics proposal | Make telemetry quality visible to operators. |
| R-TH-022 | Spatial safety ADR | Ensure guidance keeps operators aware of zone constraints. |
| R-TH-034 | Equipment health program | Drive proactive maintenance scheduling. |

---

## 64.6 Acceptance Criteria & Verification

Validation combines UI regression, telemetry exporter smoke tests, replay simulations,
mesh QoS verification, and policy reviews. Watchdog integration tests must demonstrate
service restart and alert propagation while telemetry pipelines prove structured
logging, privacy enforcement, and maintenance record generation.

### 64.6.1 Requirement-to-Verification Map

| Req ID | Verification Type | Artifact / Location | Pass/Fail Threshold |
|--------|-------------------|---------------------|---------------------|
| R-TH-000 | UI Regression | `tests/ui/AgIOUdpMonitor.cs` | Monitor logs packets and exports sample capture. |
| R-TH-004 | System Test | `tests/system/LinuxCoreHealthz.yml` | `/healthz` returns 200; watchdog restarts service < 30 s. |
| R-TH-022 | Simulation | `tests/sim/ConstraintAlertScenario.json` | Alerts emitted ≤ 250 ms from zone intersection. |
| R-TH-030 | Mesh Test | `tests/integration/TelemetryMeshQoS.cs` | QoS tiers enforced; ACL rejects unauthorized client. |
| R-TH-034 | Data Pipeline Test | `tests/integration/EquipmentHealthRecord.cs` | Records include hours, cycles, fault codes. |

---

## 64.7 Constraints

- Must support offline-first rigs with local log storage and deferred export.
- Must guard personally identifiable information when streaming telemetry off-rig.
- Must align with regulatory retention policies for agronomic and safety data.

---

## 64.8 Stakeholder Expectations

Operators expect immediate feedback and actionable diagnostics, maintainers require
maintenance schedules with telemetry evidence, and remote supervisors need health
feeds for fleet oversight without compromising operator privacy.

---

## 64.9 Design Considerations

| ID | Consideration | Description |
|----|---------------|-------------|
| C1 | Legacy diagnostics continuity | Preserve AgIO monitors and PGN tools while modernizing telemetry outputs. |
| C2 | Headless observability | Linux Core deployments need `/healthz`, structured logs, and watchdog integration. |
| C3 | Privacy & retention | Telemetry exports must respect operator consent, retention windows, and redaction policies. |
| C4 | Layer-aware telemetry | Diagnostics should tie counters and overlays to specific layers for actionable insights. |
| C5 | Automation explainability | Alerting and telemetry must document why automation engaged or halted. |
| C6 | Mesh scalability | Telemetry mesh QoS, ACLs, and stale detection must scale to multi-machine crews. |
| C7 | Maintenance integration | Equipment health records and alerts must link to TaskService workflows. |

### 64.9.1 Assumptions & Preconditions

- [A1] Operators can grant opt-in consent for advanced diagnostics overlays.
- [A2] Telemetry mesh connectivity maintains latency ≤ 250 ms within a field crew.
- [A3] Equipment sensors provide sufficient fidelity for maintenance scoring.

#### 64.9.2 Layer diagnostics detail (from former Option 64-O5)

Layer diagnostics extend variable-rate telemetry by adding packet-rate monitors,
legend parity tests, and bad-sample counters. Overlays visualize health metrics with
AgDiag tooling, providing opt-in toggles, consent logging, and privacy-aware exports.
Dashboards combine telemetry with provenance tags so operators correlate coverage
quality, constraint alerts, and automation decisions.

---

## 64.10 Option Overview

| Option ID | Status | Type / Theme | Description | Reference Document |
|-----------|--------|--------------|-------------|--------------------|
| 64-O1 | Proposed | Metrics Pipeline | Structured metrics sourced from AgIO exporters (Prometheus/Influx). | — |
| 64-O2 | Exploring | Cloud Aggregation | Remote aggregation with dashboards for fleet oversight. | — |
| 64-O3 | Exploring | Embedded Analytics | Local scoring engine analyzing telemetry without cloud dependencies. | — |

---

## 64.11 Comparison Matrix

| Attribute / Criteria | 64-O1 | 64-O2 | 64-O3 |
|----------------------|-------|-------|-------|
| Offline Support | Medium | Low | High |
| Operational Overhead | Medium | High | Medium |
| Fleet Visibility | Medium | High | Medium |
| Privacy Risk | Medium | High | Low |

---

## 64.12 Option Evaluation

Current sentiment favors combining structured metrics (64-O1) with embedded analytics
(64-O3) to serve offline rigs while optional cloud aggregation (64-O2) remains gated by
privacy policies and operator consent.

---

## 64.13 Evaluation & Verification

Benchmark metrics track telemetry feed latency, alert response time, mesh backlog, and
maintenance record freshness. Verification suites replay constraint scenarios and
radio diagnostics while privacy audits review retention and consent logs.

---

## 64.14 Implementation Policy

Implementations must instrument telemetry exporters, document `/healthz` and watchdog
contracts, provide configurable retention policies, and integrate equipment health
records with TaskService and automation telemetry feeds.

---

## 64.15 Community Sentiment

Contributors request retaining familiar monitors while bundling Linux Core health,
layer-aware diagnostics, and constraint alerting to keep automation explainable.

### 64.15.1 Section Change Log

| Date | Summary | PR / Issue |
|------|---------|------------|
| 2025-10-20 | Initial rewrite using v0.1 template. | #0000 |

---

## 64.16 Traceability

| Requirement ID | Related Option(s) | ADR(s) | Verification Artifact | Implementation Reference |
|----------------|-------------------|--------|-----------------------|--------------------------|
| R-TH-000 | 64-O1 | 64-ADR-019 | `tests/ui/AgIOUdpMonitor.cs` | `SourceCode/AgIO/Source/Forms/FormUDPMonitor.cs` |
| R-TH-004 | 64-O1, 64-O3 | 21-O6 | `tests/system/LinuxCoreHealthz.yml` | `Core/LinuxServiceHost` |
| R-TH-022 | 64-O3 | 42-ADR-047 | `tests/sim/ConstraintAlertScenario.json` | `Core/ConstraintAlerts` |
| R-TH-034 | 64-O3 | EquipmentHealth Program | `tests/integration/EquipmentHealthRecord.cs` | `docs/Plugins/EquipmentHealth.md` |

---

## 64.17 Conformance

Implementations conform when diagnostics continuity, telemetry feeds, health endpoints,
constraint alerting, mesh streaming, maintenance records, and privacy policies meet the
stated requirements and are verified through mapped artifacts.

---

## Standards Context

This section aligns with ISO/IEC/IEEE 29148:2018 and IEEE 1016:2017, emphasizing
observable automation, privacy-aware telemetry, and safety alerts for agronomic systems.
