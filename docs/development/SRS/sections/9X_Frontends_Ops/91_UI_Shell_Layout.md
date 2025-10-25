# 91 — UI Shell & Layout
*(Status: Proposed)*

**Authors:** Nexus Team (Codex)
**Created:** 2025-10-20  
**Version:** 0.1.0  
**Section ID:** 91  
**Editors:** Frontend & Operations Working Group  
**Last Updated:** 2025-10-20  
**Related Sections:** 11 — OS Support, 13 — UI Framework & UX, 72 — Zone Drawing Framework, 94 — Extensibility & Packaging Updates  
**Upstream Dependencies:** 1X — Platform Foundations, 2X — System Architecture, 4X — Interprocess Communications  
**Downstream Impacts:** 92 — Gauges & Machine Panels, 93 — Command Line Interface, 96 — Quality Engineering & Release

---

## 91.1 Purpose & Scope

Define the Nexus UI shell, layout system, and companion experiences that operators, technicians, and contributors rely upon across desktop, tablet, and remote deployments. This section establishes baseline Windows parity, metadata-driven dashboards, remote client expectations, and plugin integration behaviors so modernization efforts retain legacy productivity while unlocking new workflows.【F:docs/sections/9X_Frontends_Ops/91-ADR-032 - Presets and Layout Linking for Equipment Workflows.md†L7-L35】【F:docs/sections/9X_Frontends_Ops/91-ADR-034 - Metadata-driven dashboards and inspector surfaces.md†L7-L68】

---

## 91.2 Context

- Legacy AgOpenGPS and AgIO WinForms shells remain the primary operator experience.【F:SourceCode/GPS/AgOpenGPS.csproj†L1-L102】
- Avalonia-based modernization must coexist with Windows installers while preparing Linux Core kiosks and remote displays.
- Metadata-driven layer dashboards, presets, and inspectors expand scope and require declarative tooling and governance.【F:docs/sections/9X_Frontends_Ops/91-ADR-034 - Metadata-driven dashboards and inspector surfaces.md†L7-L68】
- Remote gRPC/WebSocket clients, simulation tooling, and plugin manifests introduce capability separation, security, and determinism considerations.【F:docs/sections/9X_Frontends_Ops/91-ADR-051 - Report Builder & Export System.md†L21-L52】【F:docs/sections/9X_Frontends_Ops/91-ADR-011 - Mapping and visualization imagery pipeline.md†L29-L78】

---

## 91.3 Legacy Comparison

| Area / Theme | Legacy Behavior | Identified Limitation | Modernization Opportunity | Reference / Source |
|---------------|-----------------|------------------------|---------------------------|--------------------|
| UI Shell | WinForms desktop with discrete utilities (AgOpenGPS, AgIO, ModSim). | Windows-only workflows and fragmented utilities. | Unified Avalonia shell spanning Windows/Linux while preserving tooling launch points. | Legacy AgOpenGPS releases; 13-ADR-001 shared runtime【F:docs/sections/1X_Platform_Foundations/13-ADR-001 - Use Avalonia for the Nexus Desktop UI Shell.md†L24-L44】 |
| Layout & Dashboards | Hard-coded overlays, limited inspector depth, manual presets. | Slow to onboard new layers; inconsistent diagnostics. | Metadata-driven dashboards, declarative inspectors, and preset orchestration. | ADR-032 presets & layout linking; ADR-034 metadata dashboards【F:docs/sections/9X_Frontends_Ops/91-ADR-032 - Presets and Layout Linking for Equipment Workflows.md†L7-L34】【F:docs/sections/9X_Frontends_Ops/91-ADR-034 - Metadata-driven dashboards and inspector surfaces.md†L7-L68】 |
| Remote & Companion | Remote desktop mirrors and ad-hoc browser experiments. | Safety posture unclear; offline support brittle. | gRPC/WebSocket clients with explicit capability flags, offline cache, and kiosk launchers. | Remote client exploration notes; Linux Core pilots【F:docs/sections/2X_System_Architecture/21-O6 - Linux Core service with remote frontends.md†L6-L44】 |

> **Informative:** Captures modernization drivers inherited from the WinForms suite and metadata roadmap.

---

## 91.4 Definitions

| Term | Definition |
|------|-------------|
| Companion Remote | Mobile or browser client consuming Nexus Core APIs via gRPC or gRPC-Web.【F:docs/sections/2X_System_Architecture/21-O6 - Linux Core service with remote frontends.md†L6-L44】 |
| Layout Preset | Saved arrangement of dashboards, inspectors, and overlays that can be applied across machines or jobs.【F:docs/sections/9X_Frontends_Ops/91-ADR-032 - Presets and Layout Linking for Equipment Workflows.md†L17-L34】 |
| Layer Definition Manager | Declarative catalog where plugins describe visualization metadata, aggregation rules, and inspector surfaces.【F:docs/sections/9X_Frontends_Ops/91-ADR-034 - Metadata-driven dashboards and inspector surfaces.md†L29-L68】 |
| Zone Tool | Core-owned geometry editor shared by plugins for boundary, headland, and keep-out interactions.【F:docs/sections/7X_Mapping_Geospatial/72-ADR-044 - Zone Drawing Framework.md†L29-L74】 |

---

> **Requirement Grammar (RFC-2119):**
> - **MUST / MUST NOT** = mandatory; verification required.
> - **SHOULD / SHOULD NOT** = strong recommendation; justify exceptions.
> - **MAY** = optional; document enabling conditions.

## 91.5 Requirements

### 91.5.1 Baseline & Legacy Parity

| ID | Priority | Category | Summary | Source / C-IDs | Key Metrics / Verification |
|----|-----------|----------|---------|-----------------|-----------------------------|
| R-FE-000 | MUST | Legacy Parity | Maintain WinForms AgOpenGPS desktop UI as primary Windows experience. | Legacy Windows fleet | Windows installer smoke tests + operator regression sessions |
| R-FE-001 | MUST | Legacy Parity | Preserve AgIO companion app for transport and hardware configuration. | AgIO contributors | Hardware IO regression suite |
| R-FE-002 | SHOULD | Legacy Utilities | Ship auxiliary tools (AgDiag, ModSim, GPS_Out, Keypad) for troubleshooting and simulation. | Community utilities | Utility launch regression checklist |
| R-FE-003 | SHOULD | Remote Experience | Provide consistent remote display/control story without regressing Windows workflows. | Community workshops | Pilot remote session checklist |
| R-FE-050 | MUST | Job Lifecycle | Mirror legacy job verbs (New, Resume, Open, Drive-In, Import, Clone, Close). | ADR-030 job lifecycle【F:docs/sections/6X_Core_Domain_Services/62-ADR-030 - Field job sessions and lifecycle services.md†L40-L67】 | UI acceptance tests for job lifecycle |
| R-FE-051 | SHOULD | Job Visibility | Display job metadata, guidance, and preset links with autosave warnings. | ADR-030 job lifecycle【F:docs/sections/6X_Core_Domain_Services/62-ADR-030 - Field job sessions and lifecycle services.md†L41-L86】 | UI regression for job drawer |
| R-FE-090 | MUST | Navigation | Provide Season-first and Farm-first navigators with breadcrumb trails. | ADR-040 season organizers; ADR-041 job sessions【F:docs/sections/3X_Data_Storage/31-ADR-040 - Season Organizers.md†L55-L73】【F:docs/sections/6X_Core_Domain_Services/62-ADR-041 - Job Sessions Lifecycle.md†L55-L73】 | Navigation workflow tests |
| R-FE-091 | MUST | Machine Awareness | Implement machine drawer showing presence, trail presets, and stale indicators. | ADR-047 telemetry mesh【F:docs/sections/4X_Interprocess_Communications/42-ADR-047 - Live Telemetry Mesh.md†L33-L62】 | Mesh simulation acceptance tests |
| R-FE-092 | MUST | Session Panel | Surface environment snapshot, inputs, notes, and provenance badges with inline edits. | Job lifecycle SRS【F:docs/sections/6X_Core_Domain_Services/62_Job_Lifecycle.md†L45-L74】 | Session panel UI regression |

### 91.5.2 Metadata-Driven Dashboards & Presets

| ID | Priority | Category | Summary | Source / C-IDs | Key Metrics / Verification |
|----|-----------|----------|---------|-----------------|-----------------------------|
| R-FE-010 | MUST | Metadata Dashboards | Render dashboards, overlays, and inspectors based on layer metadata without code changes. | ADR-034 metadata dashboards【F:docs/sections/9X_Frontends_Ops/91-ADR-034 - Metadata-driven dashboards and inspector surfaces.md†L7-L68】 | Declarative metadata regression suite |
| R-FE-011 | SHOULD | Preset Tooling | Provide configuration tooling (Layer Definition Manager, presets, drill-down dashboards). | ADR-034 metadata dashboards【F:docs/sections/9X_Frontends_Ops/91-ADR-034 - Metadata-driven dashboards and inspector surfaces.md†L29-L68】 | Metadata editor integration tests |
| R-FE-052 | SHOULD | Preset Switcher | Offer preset selectors, layout diff viewer, and snapshot/live-link indicators. | ADR-032 presets & layout linking【F:docs/sections/9X_Frontends_Ops/91-ADR-032 - Presets and Layout Linking for Equipment Workflows.md†L7-L35】 | Preset diff UX tests |
| R-FE-053 | SHOULD | Task Orchestration | Surface task progress, retries, and failures triggered by preset or layout changes. | ADR-032 presets & layout linking【F:docs/sections/9X_Frontends_Ops/91-ADR-032 - Presets and Layout Linking for Equipment Workflows.md†L17-L34】 | Orchestration telemetry harness |
| R-FE-074 | SHOULD | Analytics Overlays | Provide toggles and accessible legends for crop, genetics, yield, profit, risk, and weather overlays. | Mapping plugin ADRs【F:docs/sections/7X_Mapping_Geospatial/72-ADR-045 - Crop Type Plugin & Layers.md†L29-L71】【F:docs/sections/7X_Mapping_Geospatial/72-ADR-049 - Yield & Analytics Plugin.md†L21-L52】【F:docs/sections/7X_Mapping_Geospatial/72-ADR-052 - Field Health & Risk Plugin.md†L21-L44】【F:docs/sections/7X_Mapping_Geospatial/72-ADR-053 - Weather & Environment Plugin.md†L21-L49】 | Visualization regression pack |
| R-FE-075 | SHOULD | Weather Timeline | Present weather timelines with charting widgets and compliance alerts. | Weather plugin ADR-053【F:docs/sections/7X_Mapping_Geospatial/72-ADR-053 - Weather & Environment Plugin.md†L21-L49】 | Weather timeline UI tests |
| R-FE-076 | SHOULD | Report Builder | Provide report generation flow with preview and dependency readiness. | ADR-051 report builder【F:docs/sections/9X_Frontends_Ops/91-ADR-051 - Report Builder & Export System.md†L21-L52】 | Report builder integration tests |
| R-FE-077 | SHOULD | Print Studio | Deliver print view with map composer templates, legends, and scale bars. | Map Composer guide【F:docs/Plugins/briefs/MapComposer.md†L1-L160】 | Print preview regression checklist |

### 91.5.3 Remote & Companion Experiences

| ID | Priority | Category | Summary | Source / C-IDs | Key Metrics / Verification |
|----|-----------|----------|---------|-----------------|-----------------------------|
| R-FE-004 | COULD | Thin Clients | Introduce thin clients that mirror dashboards when networked. | Remote client community notes | Remote mirror prototype review |
| R-FE-012 | SHOULD | Remote Client Core | Ensure at least one frontend operates purely as a remote client over Core APIs with offline parity. | Remote client option analysis【F:docs/sections/2X_System_Architecture/21-O6 - Linux Core service with remote frontends.md†L6-L44】 | Remote client soak tests |
| R-FE-013 | SHOULD | Safety Posture | Distinguish monitor-only versus control-capable remote clients with capability flags. | Safety posture working notes | Capability flag audit |
| R-FE-014 | SHOULD | Operator Readiness | Capture training, preset migration, and configuration handoff requirements during rollout. | Deployment readiness plan | Field pilot checklist |
| R-FE-020 | SHOULD | Simulation Control | Provide unified simulation bar controlling SimClock for replay and plugin simulators. | Simulation roadmap | Replay automation tests |
| R-FE-021 | SHOULD | Accessibility | Deliver theming and localization hooks for plugin panels. | UI accessibility charter | Accessibility regression suite |
| R-FE-060 | MUST | Companion Rollout | Ship connection center handling discovery, auth, reconnect, and health across gRPC / gRPC-Web. | 13-ADR-001 cross-platform shell【F:docs/sections/1X_Platform_Foundations/13-ADR-001 - Use Avalonia for the Nexus Desktop UI Shell.md†L24-L44】 | Companion connection acceptance tests |
| R-FE-061 | SHOULD | Offline Resilience | Provide offline cache for boundaries, guidance, and coverage with resync. | 13-ADR-001 cross-platform shell【F:docs/sections/1X_Platform_Foundations/13-ADR-001 - Use Avalonia for the Nexus Desktop UI Shell.md†L24-L33】 | Offline cache soak tests |
| R-FE-062 | MUST | Run Mode Switching | Expose `RunMode` toggles to swap `ICoreTransport` implementations without altering view models. | 13-ADR-001 cross-platform shell【F:docs/sections/1X_Platform_Foundations/13-ADR-001 - Use Avalonia for the Nexus Desktop UI Shell.md†L24-L44】 | Mode-switch integration tests |
| R-FE-063 | SHOULD | Feature Gating | Introduce feature flags for mobile deployments while sharing project codebase. | 13-ADR-001 cross-platform shell【F:docs/sections/1X_Platform_Foundations/13-ADR-001 - Use Avalonia for the Nexus Desktop UI Shell.md†L33-L44】 | Feature flag configuration tests |
| R-FE-094 | SHOULD | Sync Dashboard | Provide read-only web dashboard using mirrored `/Seasons/` folders without control actions. | Sync dashboard concept【F:docs/Plugins/briefs/SyncDashboard.md†L1-L150】 | Sync dashboard smoke tests |
| R-FE-095 | SHOULD | Plugin Marketplace | Add plugin catalog UI with manifest validation, compatibility badges, and install workflows. | Plugin catalog design notes【F:docs/Plugins/briefs/PluginCatalog.md†L1-L160】 | Marketplace integration tests |

### 91.5.4 Spatial Constraints & Editing

| ID | Priority | Category | Summary | Source / C-IDs | Key Metrics / Verification |
|----|-----------|----------|---------|-----------------|-----------------------------|
| R-FE-040 | MUST | Constraint Visualization | Render boundary, headland, keep-out, and work-disabled zones with canonical symbology. | Zone tooling ADRs | Constraint overlay visual tests |
| R-FE-041 | SHOULD | Zone Management | Provide zone list, toggles, buffer controls, and provenance/tooltips within map UI. | Zone tooling ADRs | Zone UI regression |
| R-FE-042 | SHOULD | Override Awareness | Surface alerts and undo affordances when automation is gated by zones. | Zone tooling ADRs | Override event telemetry |
| R-FE-070 | MUST | Shared Zone Tool | Provide Core-owned geometry editing toolbar binding to `LayerEditService`. | ADR-044 zone drawing【F:docs/sections/7X_Mapping_Geospatial/72-ADR-044 - Zone Drawing Framework.md†L29-L74】 | Zone tool integration tests |
| R-FE-071 | MUST | Attribute Panels | Render plugin attribute editors with validation and provenance summaries. | ADR-044 zone drawing【F:docs/sections/7X_Mapping_Geospatial/72-ADR-044 - Zone Drawing Framework.md†L29-L74】 | Attribute panel regression |
| R-FE-072 | SHOULD | Collaborative Awareness | Display active editors, mesh sync status, and edit history for multi-machine sessions. | ADR-047 telemetry mesh【F:docs/sections/4X_Interprocess_Communications/42-ADR-047 - Live Telemetry Mesh.md†L33-L62】 | Collaboration simulation tests |
| R-FE-093 | SHOULD | Zone Toolbar Integration | Embed ADR-044 toolbar/attribute panel including locks and presence chips. | ADR-044 zone drawing【F:docs/sections/7X_Mapping_Geospatial/72-ADR-044 - Zone Drawing Framework.md†L29-L74】 | Toolbar integration checklist |

### 91.5.5 Plugin UI Contributions

| ID | Priority | Category | Summary | Source / C-IDs | Key Metrics / Verification |
|----|-----------|----------|---------|-----------------|-----------------------------|
| R-FE-030 | MUST | Plugin Integration | Load plugin-declared panels, config pages, and overlays from manifests at runtime. | Plugin manifest governance | Plugin UI harness |
| R-FE-031 | MUST | Safety Gating | Enforce capability-aware states so control panels hide or become read-only when permissions are denied. | Security & manifest notes | Capability enforcement tests |
| R-FE-032 | SHOULD | Shared Widgets | Provide shared charts, tables, and overlay primitives for plugin reuse. | UI component roadmap | Shared widget regression |
| R-FE-033 | MUST | Lifecycle Hooks | Offer binding helpers for plugin UIs to react to context changes, session events, and mesh presence. | Job lifecycle + zone ADRs【F:docs/sections/6X_Core_Domain_Services/62_Job_Lifecycle.md†L18-L74】【F:docs/sections/7X_Mapping_Geospatial/72-ADR-044 - Zone Drawing Framework.md†L29-L74】 | Plugin lifecycle API tests |

### 91.5.6 Requirement Sources & Rationale

| Req ID | Source | Rationale |
|--------|--------|-----------|
| R-FE-000 | Operator feedback on legacy tooling | Preserve trusted workflows during modernization. |
| R-FE-010 | ADR-034 metadata dashboards | Ensure new layers reach dashboards without bespoke code. |
| R-FE-012 | Linux Core remote client pilots | Unlock headless rigs and remote tablets with safety posture. |
| R-FE-040 | ADR-044 zone drawing framework | Provide consistent constraint visualization across plugins. |
| R-FE-060 | 13-ADR-001 cross-platform shell | Guarantee companion clients connect reliably across transports. |

---

## 91.6 Acceptance Criteria & Verification

Modernization must retain legacy workflows while enabling declarative dashboards, remote clients, and shared editing tooling. Verification spans automated UI regression, deterministic simulation, remote client soak tests, and plugin manifest harnesses.

### 91.6.1 Requirement-to-Verification Map

| Req ID | Verification Type | Artifact / Location | Pass/Fail Threshold |
|--------|-------------------|---------------------|---------------------|
| R-FE-000 | Manual regression + smoke | Windows installer runbook | Operators complete baseline workflow with no blockers |
| R-FE-010 | Automated UI integration | `tes../UI/MetadataDashboardSuite` | Declarative metadata renders correct widgets per layer |
| R-FE-012 | Remote client soak | `tests/remote/CompanionConnectivity.md` | 24-hour session without transport error above SLA |
| R-FE-040 | Visual regression | `tes../UI/ZoneVisualization.snap` | All constraint overlays match reference symbology |
| R-FE-060 | Integration test | `tests/companion/ConnectionCenter.feature` | Discovery + reconnect succeed across transports |

---

## 91.7 Constraints

- Maintain compatibility with Windows installers, shortcuts, and registry expectations while adding Avalonia shells.【F:SourceCode/AgOpenGPS.sln†L6-L35】
- Remote clients must enforce safety gating and capability flags before exposing control actions.【F:docs/sections/2X_System_Architecture/21-O6 - Linux Core service with remote frontends.md†L21-L44】
- Metadata definitions must be versioned and validated through plugin manifest governance to avoid runtime drift.【F:docs/sections/9X_Frontends_Ops/94-ADR-031 - Official Plugin Bundle Dependency Governance.md†L19-L68】
- Layout presets and dashboard metadata MUST remain versioned alongside season/job configuration so operators can reproduce layouts.
- Plugin manifests MUST declare UI contributions using the standardized governance schema from §94.
- Remote clients MUST rely on capability-scoped credentials managed under §95 security policies.

---

## 91.8 Interfaces & Dependencies

- Relies on Core transport abstractions (`ICoreTransport`, `LayerEditService`) defined in Sections 4X and 6X.  
- Consumes plugin manifest metadata via governance described in §94.  
- Publishes dashboards, presets, and job state to Gauges & Machine Panels (§92) and CLI surfaces (§93).

---

## 91.9 Design Considerations

| ID | Consideration | Description |
|----|----------------|-------------|
| C1 | Windows baseline continuity | Preserve WinForms + AgIO launcher experience while introducing Avalonia shells for modernization. |
| C2 | Shared Avalonia shell | Adopt shared .NET 8 + Avalonia runtime with optional Windows-native host shims to reduce code duplication. |
| C3 | Metadata-driven dashboards | Favor declarative overlays, inspectors, and presets curated through Layer Definition Manager to accelerate new layer onboarding.【F:docs/sections/9X_Frontends_Ops/91-ADR-034 - Metadata-driven dashboards and inspector surfaces.md†L7-L68】 |
| C4 | Remote & kiosk clients | Provide gRPC/WebSocket remote clients, kiosk launch scripts, and offline cache to support Linux Core deployments.【F:docs/sections/2X_System_Architecture/21-O6 - Linux Core service with remote frontends.md†L6-L44】 |
| C5 | Preset orchestration | Maintain preset linking, diffing, and orchestration status to keep multi-machine layouts aligned.【F:docs/sections/9X_Frontends_Ops/91-ADR-032 - Presets and Layout Linking for Equipment Workflows.md†L7-L35】 |
| C6 | Simulation parity | Ensure simulation controls, replays, and deterministic datasets mirror hardware workflows to support QA and operator training.【F:docs/sections/9X_Frontends_Ops/96_Quality_Engineering_Release.md†L19-L74】 |
| C7 | Plugin marketplace | Surface plugin catalog with compatibility badges, signed manifests, and rollback controls governed by ADR-031.【F:docs/sections/9X_Frontends_Ops/94-ADR-031 - Official Plugin Bundle Dependency Governance.md†L19-L68】 |

### 91.9.1 Assumptions & Preconditions

- [A1] Metadata schemas and manifests remain versioned and validated before release.  
- [A2] Remote connectivity budgets (latency, packet loss) are published for companion clients.  
- [A3] Operators receive migration guides for presets, dashboards, and remote workflows.

---

## 91.10 Option Overview

No active option proposals are under review; modernization guidance is documented through ADRs and design considerations (C1–C7).

---

## 91.11 Comparison Matrix

| Attribute / Criteria | Legacy Windows Suite | Unified Avalonia + Remote Shell |
|----------------------|----------------------|---------------------------------|
| Core Approach | Separate WinForms apps per utility. | Shared Avalonia UI with plugin manifests. |
| Implementation Effort | Low (maintenance only). | Medium — requires metadata refactor and transport abstraction. |
| Maintainability | Low — fragmented code paths. | High — single codebase with declarative dashboards. |
| Performance | Proven on Windows hardware. | Needs GPU validation on Pi/CM5 but benefits from optimized pipeline. |
| Extensibility | Limited to manual UI changes. | High — plugin manifests describe dashboards and inspectors. |
| Risk Level | Medium — modernization stalls. | Medium — requires training and metadata governance. |

---

## 91.12 Decision Matrix

> **Informative:** Detailed weighting awaits follow-up ADR once remote client pilot exits evaluation. Interim decisions rely on considerations C1–C7.

---

## 91.13 Evaluation & Verification

- Benchmark metadata-driven dashboards against deterministic dataset replays.  
- Validate remote clients and kiosks through 24-hour soak tests over simulated lossy links.  
- Run accessibility audits for theming, localization, and contrast baselines prior to release.

**Acceptance Criteria**

- All **MUST** requirements verified via mapped artifacts.  
- **SHOULD** requirements either satisfied or waived with documented rationale.  
- Remote client deployments document safety gating and offline behaviors.

---

## 91.14 Implementation Policy

*(Reserved — storage locations and credential helpers are defined in supporting ADRs and runbooks.)*

---

## 91.15 Community Sentiment

- Windows suite remains the operational safety net while Avalonia pilots mature.  
- Operators support metadata-driven dashboards when shipped with presets and inspector upgrades instead of manual wiring.【F:docs/sections/9X_Frontends_Ops/91-ADR-034 - Metadata-driven dashboards and inspector surfaces.md†L7-L68】  
- Remote-first clients gain enthusiasm if they reuse Core contracts and avoid retraining existing operators.【F:docs/sections/2X_System_Architecture/21-O6 - Linux Core service with remote frontends.md†L6-L44】

### 91.15.1 Section Change Log

| Date | Summary | Author | PR / Issue |
|------|---------|--------|------------|
| 2025-10-20 | Initial conversion to new SRS template; migrated option notes into design considerations. | Nexus Team (Codex) |  |

---

## 91.16 Traceability

| Requirement ID | Related Considerations | ADR(s) | Verification Artifact | Implementation Reference |
|----------------|------------------------|--------|-----------------------|--------------------------|
| R-FE-000 | C1 | — | Windows installer runbook | Legacy WinForms projects |
| R-FE-010 | C3, C5 | 91-ADR-034 | `tes../UI/MetadataDashboardSuite` | Avalonia dashboard module |
| R-FE-012 | C4 | — | `tests/remote/CompanionConnectivity.md` | CompanionRemote shell |
| R-FE-040 | C6 | 72-ADR-044 | `tes../UI/ZoneVisualization.snap` | Shared zone tool package |
| R-FE-060 | C2, C4 | 13-ADR-001 | `tests/companion/ConnectionCenter.feature` | Connection Center service |

---

## 91.17 Conformance

An implementation **conforms** to §91 when:
1. All **MUST** requirements are satisfied and verified through mapped artifacts.
2. All **SHOULD** requirements are satisfied or explicitly waived with rationale and mitigation.
3. No **MUST NOT** requirements (none defined) are violated during deployment or operation.

---

## Standards Context

This section aligns with ISO/IEC/IEEE 29148:2018 for requirements specification and IEEE 1016:2017 for design documentation. Normative content resides in §91.5 and §91.7; informative context appears elsewhere.

## Mobile companion and embedded roadmap
- **CompanionRemote (today):** Android builds speak gRPC directly while iOS falls back to gRPC-Web through an Envoy/grpcwebproxy sidecar. The connection center covers discovery, TLS/auth, and reconnect states so the same UI ships as a remote monitor for Core + AgIO rigs.【F:docs/sections/1X_Platform_Foundations/13-ADR-001 - Use Avalonia for the Nexus Desktop UI Shell.md†L24-L33】
- **LocalInProc (next):** Package Core as a library referenced by the Avalonia app. DI swaps the transport to an in-process adapter, enabling “lite” offline workflows with feature gates and local telemetry persistence on mobile devices.【F:docs/sections/1X_Platform_Foundations/13-ADR-001 - Use Avalonia for the Nexus Desktop UI Shell.md†L33-L37】
- **LocalOutOfProc (later):** Embed Core as a self-contained binary launched via platform services (Android foreground service, Windows/Linux process). The UI continues using gRPC against `127.0.0.1`, preserving crash isolation, logging, and security patterns shared with desktop shells.【F:docs/sections/1X_Platform_Foundations/13-ADR-001 - Use Avalonia for the Nexus Desktop UI Shell.md†L37-L44】
- **AgIO convergence:** Android platforms extend the same transport abstraction to USB-OTG serial, Bluetooth SPP, and BLE so AgIO features can move in-process once the mobile host proves stable, while iOS companions remain remote-first and rely on BLE/Wi-Fi to reach bridge hardware.【F:docs/sections/1X_Platform_Foundations/13-ADR-001 - Use Avalonia for the Nexus Desktop UI Shell.md†L44-L45】

## Open questions
- Which screens must be mirrored vs. reimagined for mobile?
- How do we share PGN data with remote clients without reimplementing all of AgIO?
