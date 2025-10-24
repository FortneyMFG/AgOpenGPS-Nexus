# 92 — Gauges & Machine Panels
*(Status: Proposed)*

**Author:** Codex  
**Created:** 2025-10-20  
**Version:** 0.1.0  
**Section ID:** 92  
**Editors:** Frontend & Operations Working Group  
**Last Updated:** 2025-10-20  
**Related Sections:** 4X — Interprocess Communications, 52 — AgIO Service, 94 — Extensibility & Packaging Updates  
**Upstream Dependencies:** 1X — Platform Foundations, 5X — Hardware IO Device Layer  
**Downstream Impacts:** 91 — UI Shell & Layout, 95 — Security & Permissions, 96 — Quality Engineering & Release

---

## 92.1 Purpose & Scope

Specify the read-only telemetry gauges and machine panels surfaced in Nexus frontends. Gauges visualize J1939/ISOBUS signals, plugin-provided metrics, and simulated feeds through consistent metadata, transport envelopes, and UI behaviors. This section governs canonical IDs, rendering rules, and interaction patterns so operators receive reliable diagnostics across desktop, companion, and remote layouts.【F:docs/sections/9X_Frontends_Ops/91_UI_Shell_Layout.md†L96-L151】

---

## 92.2 Context

- Gauges reuse AgOpenGPS UDP/CAN envelopes alongside existing layer and rate PGNs to avoid disrupting hardware transports.  
- Metadata-driven dashboards (Section 91) consume gauge definitions to render overlays, strips, and inspectors.  
- Plugin manifests declare additional gauges; governance and capability discovery align with ADR-031 and ADR-018.【F:docs/sections/9X_Frontends_Ops/94-ADR-031 - Official Plugin Bundle Dependency Governance.md†L19-L68】【F:docs/sections/9X_Frontends_Ops/94-ADR-018 - Plugin API Capability Discovery and Runtime Model.md†L19-L66】

---

## 92.3 Legacy Comparison

| Area / Theme | Legacy Behavior | Identified Limitation | Modernization Opportunity | Reference / Source |
|---------------|-----------------|------------------------|---------------------------|--------------------|
| Telemetry Visualization | Ad-hoc WinForms gauges tied to specific PGNs. | Hard-coded scaling and inconsistent alerts. | Metadata-defined gauges with unit scaling, alarm bands, and shared widgets. | Legacy AgOpenGPS utilities |
| Transport | Custom UDP messages per utility. | Duplicate logic and no capability discovery. | Shared Gauge Block transport with capability bits and manifest discovery. | AgIO gateway design notes |
| Operator Interaction | Fixed layout panels with limited detail modals. | No sparklines, provenance, or multi-source arbitration. | Configurable overlays, detail modals, and arbitration via quality scores. | Community feedback sessions |

> **Informative:** Establishes modernization drivers for metadata-driven gauge rendering.

---

## 92.4 Definitions

| Term | Definition |
|------|-------------|
| Gauge Block | Canonical telemetry envelope containing gauge samples, timestamps, and quality scores over UDP/CAN. |
| SPN (Suspect Parameter Number) | SAE J1939 identifier describing specific vehicle parameter semantics. |
| DDI (Device Descriptor Identifier) | ISOBUS identifier linking implement-specific channels to standardized semantics. |
| Target Band | Gauge metadata describing nominal operating range used to color indicators. |

---

> **Requirement Grammar (RFC-2119):**
> - **MUST / MUST NOT** = mandatory; verification required.  
> - **SHOULD / SHOULD NOT** = strong recommendation; justify exceptions.  
> - **MAY** = optional; document enabling conditions.

## 92.5 Requirements

| ID | Priority | Category | Summary | Source / C-IDs | Key Metrics / Verification |
|----|-----------|----------|---------|-----------------|-----------------------------|
| R-GA-000 | MUST | Metadata | Gauges MUST declare PGN/SPN/DDI, units, scaling, and alarm metadata to render correctly in dashboards. | Legacy gauge backlog; Section 91 metadata tooling | Metadata schema validation tests |
| R-GA-001 | MUST | Transport | Gauge Blocks MUST reuse AgOpenGPS UDP/CAN envelope with quality score and TTL to coexist with existing PGNs. | AgIO transport plan | Transport compatibility regression |
| R-GA-002 | SHOULD | Capability Discovery | Gauge publishers SHOULD advertise supported IDs via capability bit in PGN `0xE2` to allow pre-provisioning. | Capability registry plan | Capability handshake tests |
| R-GA-003 | MUST | Rendering Behavior | UI widgets MUST respect target bands, alarm bands, and smoothing metadata to color and animate indicators consistently. | Section 91 dashboards | UI visual regression harness |
| R-GA-004 | SHOULD | Interaction | Gauges SHOULD expose detail modal with sparkline, min/max/avg, raw bytes, and provenance. | Operator UX studies | Interaction acceptance checklist |
| R-GA-005 | MUST | Multi-source Arbitration | When multiple feeds emit same gaugeId, highest quality score wins; ties resolved by schema version then timestamp. | Gauge arbitration notes | Automated arbitration tests |
| R-GA-006 | SHOULD | Offline Behavior | Gauges SHOULD gray out when TTL exceeded and optionally raise stale warnings. | Simulation + offline cache | Offline resilience soak tests |
| R-GA-007 | MAY | Smoothing | EMA smoothing MAY be enabled via metadata, providing `emaAlpha` and optional `deadband`. | Gauge schema definition | Unit tests for smoothing application |

### 92.5.1 Requirement Sources & Rationale

| Req ID | Source | Rationale |
|--------|--------|-----------|
| R-GA-000 | Metadata-driven UI charter | Guarantees declarative dashboards render uniformly. |
| R-GA-001 | AgIO gateway design | Ensures compatibility with existing field hardware. |
| R-GA-003 | Operator usability testing | Prevents inconsistent color/alert behaviors. |
| R-GA-005 | Fleet telemetry pilots | Avoids duplicate or oscillating data when multiple controllers publish. |

---

## 92.6 Acceptance Criteria & Verification

Gauge implementations undergo schema validation, transport compatibility testing, and UI visual regression to confirm that metadata produces consistent output across layouts.

### 92.6.1 Requirement-to-Verification Map

| Req ID | Verification Type | Artifact / Location | Pass/Fail Threshold |
|--------|-------------------|---------------------|---------------------|
| R-GA-000 | Schema validation | `schemas/gauges/gauge.schema.json` | All definitions validate against schema |
| R-GA-001 | Integration | `tests/transports/GaugeBlockHarness.cs` | No packet loss; legacy clients parse payload |
| R-GA-003 | Visual regression | `tes../UI/GaugePalette.snap` | Widgets match reference colors and animations |
| R-GA-005 | Automated arbitration | `tests/simulators/GaugeMultiSource.feature` | Highest quality feed selected 100% of time |

---

## 92.7 Constraints

- Gauge metadata must remain immutable once published to avoid breaking manifest compatibility.
- Transport payloads must respect existing bandwidth allocations alongside rate-control and steering PGNs.
- Security policies in §95 enforce read-only scope for gauge subscribers; write access remains prohibited.【F:docs/sections/9X_Frontends_Ops/95_Security_Permissions.md†L31-L68】
- Gauge definitions MUST ship as schema-validated, versioned artifacts consumable by UI and headless deployments.
- Gauge transports MUST emit heartbeat or equivalent liveness signals at documented cadences to support stale detection.
- UI shells MUST invalidate cached gauge metadata when manifest versions change to honor offline policies from §91.

---

## 92.8 Interfaces & Dependencies

- Consumes AgIO capability registry and plugin manifest discovery defined in §94.  
- Publishes gauge telemetry consumed by UI Shell (§91) and CLI diagnostics (§93).  
- Simulation feeds leverage deterministic replay governance described in §96.

---

## 92.9 Design Considerations

| ID | Consideration | Description |
|----|----------------|-------------|
| C1 | J1939 canonical mapping | Maintain SAE J1939 mappings for engine RPM, coolant temperature, oil pressure, battery potential, and fuel level with documented scaling. |
| C2 | Hydraulic & implement gauges | Provide generic analog gauge IDs with vendor override hooks for implement-specific pressure sensors. |
| C3 | Unit registry & conversions | Extend unit registry (kPa, bar, psi) and support automatic conversions in UI/CLI to aid international operators. |
| C4 | Alarm ergonomics | Balance blink cadence, color ramps, and hysteresis so operators receive actionable alerts without distraction. |
| C5 | Capability advertisement | Broadcast supported gauges so remote clients pre-provision dashboards and avoid runtime surprises. |

### 92.9.1 Assumptions & Preconditions

- [A1] Hardware vendors supply accurate PGN/SPN documentation for new gauges.  
- [A2] Plugin manifests declare additional gauges through governance in ADR-031.  
- [A3] Operators calibrate thresholds during commissioning to align with machine-specific norms.

---

## 92.10 Option Overview

No competing options are under review; gauge handling follows design considerations (C1–C5) and shared metadata tooling.

---

## 92.11 Comparison Matrix

| Attribute / Criteria | Legacy Gauges | Metadata-Driven Gauges |
|----------------------|---------------|------------------------|
| Implementation Effort | Low — static panels. | Medium — schema + manifest integration. |
| Maintainability | Low — code changes per gauge. | High — declarative definitions reused across shells. |
| Operator Clarity | Medium — inconsistent alerts. | High — standardized colors, alarms, and detail modals. |
| Extensibility | Limited to known PGNs. | High — plugin manifests can add gauges safely. |
| Risk | High — inconsistent scaling. | Medium — requires schema governance. |

---

## 92.12 Decision Matrix

> **Informative:** Weighted scoring deferred until hydraulic gauge roadmap completes vendor outreach; interim focus remains on implementing considerations C1–C5.

---

## 92.13 Evaluation & Verification

- Run deterministic replay datasets to confirm gauge smoothing, alarms, and arbitration across remote clients.  
- Validate rendering performance on constrained hardware (Raspberry Pi/CM5) to maintain frame-rate budgets.  
- Confirm CLI telemetry commands output identical gauge metadata for automation pipelines.【F:docs/sections/9X_Frontends_Ops/93_Command_Line_Interface.md†L55-L86】

**Acceptance Criteria**

- All **MUST** requirements pass schema and transport verification.  
- Alarm thresholds and unit conversions documented per gauge profile.  
- Remote dashboards render gauges with identical styling to desktop layouts.

---

## 92.14 Implementation Policy

*(Reserved — storage layouts and transport encodings are captured in supporting ADRs.)*

---

## 92.15 Community Sentiment

- Operators value readable alarm cues more than bespoke skins; consistent palettes reduce fatigue.  
- Power users request JSON export/import for gauge configurations to simplify fleet rollouts.  
- Contributors emphasize deterministic replay to validate smoothing parameters before enabling new gauges by default.【F:docs/sections/9X_Frontends_Ops/96_Quality_Engineering_Release.md†L19-L74】

### 92.15.1 Section Change Log

| Date | Summary | PR / Issue |
|------|---------|------------|
| 2025-10-20 | Converted to SRS template; codified gauge metadata requirements. | #0000 |

---

## 92.16 Traceability

| Requirement ID | Considerations | ADR(s) | Verification Artifact | Implementation Reference |
|----------------|----------------|--------|-----------------------|--------------------------|
| R-GA-000 | C1, C3 | 94-ADR-031 | `schemas/gauges/gauge.schema.json` | Gauge metadata repository |
| R-GA-001 | C1, C5 | — | `tests/transports/GaugeBlockHarness.cs` | AgIO transport module |
| R-GA-003 | C4 | — | `tes../UI/GaugePalette.snap` | UI Shell gauge components |
| R-GA-005 | C5 | — | `tests/simulators/GaugeMultiSource.feature` | Gauge arbitration service |

---

## 92.17 Conformance

Gauge implementations conform when all **MUST** requirements pass verification, alarm metadata is documented, and dashboards render without divergence across operator shells.

---

## Standards Context

Aligns with ISO 11783 (ISOBUS) for implement telemetry and SAE J1939 for vehicle parameter definitions, ensuring compatibility across agricultural equipment fleets.

## Verification
- Gauges render correctly in both the overlay strip and standalone panels.
- Needle/tiles shift colors based on `alarmBands` warn/crit thresholds.
- Engine RPM and coolant temperature track known replay logs after applying the documented scaling and offsets.
- Simulated packet loss triggers the stale indicator when the TTL expires.
- Reloading JSON configuration adds/removes gauges without requiring code changes.

## Related ADRs

- [ADR-016 — Firmware Transport Variable Rate PGNs](../../ADR/ADR-016-firmware-transport-variable-rate-pgns.md)
- [ADR-034 — Metadata-Driven Dashboards](../../ADR/ADR-034-metadata-driven-dashboards.md)
- [ADR-052 — Field Health Plugin](../../ADR/ADR-052_FieldHealthPlugin.md)
