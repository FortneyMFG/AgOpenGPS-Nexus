# AgOpenGPS Next SRS

Welcome to the Software Requirements Specification (SRS) workspace for the next generation of AgOpenGPS.

## What lives here

- **Software Requirements Specification (SRS)** – The living catalogue of _what_ the Nexus platform must do. It captures scope, constraints, and success criteria before any code is written.
- **Architecture Decision Records (ADRs)** – Lightweight memos that explain _why_ we chose a solution once the community agrees on a direction. Every ADR links back to the requirements and options that shaped the choice.
- **Options, decision matrices, and references** – Supporting material that keeps trade-off discussions grounded in data and traceable from requirement ➜ option ➜ decision.

Think of the SRS as the map and the ADRs as the signposts we install along the route. Reading them together helps newcomers understand the current plan and gives maintainers the context needed to revisit earlier choices.

### Quick start for new readers

1. Skim the [Project charter & vision guardrails](01_Project_Charter.md) to understand the product direction and deliberate omissions.
2. Jump to the [System slices](02_System_Slices.md) index and find the area that matches your question (e.g., UI, hardware IO, guidance).
3. Open the corresponding section under [`sections/`](sections/) to see requirements (`R-` IDs), open questions (`Q-` IDs), and option stubs (`O-` IDs).
4. Follow links into ADRs when you need the final decision, implementation guardrails, or rollout notes.

Returning contributors can skip straight to sections flagged "Under review" or "Ready for ADR" to catch the latest work-in-progress discussions.

## Revision log
| Date | Summary | Key slices |
|------|---------|------------|

## How this SRS is organized
- **Project charter & vision guardrails** capture what the next release aspires to solve and what is intentionally out-of-scope.
- **System slices** map every focus area (OS, UI, comms, storage, etc.) to an individual
  section document under [`sections/`](sections/).
- **Sections** collect requirements and enumerate options. They are intentionally decision-neutral—decisions live in Architecture Decision Records (ADRs).
- **Options** can be expanded in dedicated files using the
  [`docs/templates/OPTION.md`](../templates/OPTION.md) template when deeper analysis is
  needed.
- **References** house canonical specs (e.g., PGN catalogs) that new options must remain compatible with unless an ADR says otherwise.
- **ADRs** document finalized decisions. Each ADR references the section(s) and options involved so we preserve traceability.

## Document index

### Core overview
- [Project charter & vision guardrails](01_Project_Charter.md)
- [System slices](02_System_Slices.md)
- [Working notes](NOTES.md)

### Section catalog
#### 1X — Platform Foundations
- [11 — OS Support](sections/1X_Platform_Foundations/11_OS_Support.md)
- [12 — Development Language & Runtime](sections/1X_Platform_Foundations/12_Development_Language_Runtime.md)
- [13 — UI Framework & UX Language](sections/1X_Platform_Foundations/13_UI_Framework_UX.md)
- [14 — Build Environment & Tooling](sections/1X_Platform_Foundations/14_Build_Tooling.md)

#### 2X — System Architecture
- [21 — System Decomposition & Boundaries](sections/2X_System_Architecture/21_System_Decomposition_Boundaries.md)
- [22 — Process Model & Deployment Topologies](sections/2X_System_Architecture/22_Process_Model_Deployment.md)
- [23 — Threading, Scheduling & Timing](sections/2X_System_Architecture/23_Threading_Scheduling_Timing.md)
- [24 — Configuration & Environment](sections/2X_System_Architecture/24_Configuration_Environment.md)
- [26 — Units, Conventions & Coordinate Systems](sections/2X_System_Architecture/26_Units_Conventions_Coordinate_Systems.md)

#### 3X — Data & Storage
- [31 — Domain Data Model](sections/3X_Data_Storage/31_Domain_Data_Model.md)
- [32 — Persistence & Formats](sections/3X_Data_Storage/32_Persistence_Formats.md)
- [33 — Offline-first & Sync](sections/3X_Data_Storage/33_Offline_First_Sync.md)
- [34 — Backup, Retention & Archival](sections/3X_Data_Storage/34_Backup_Retention_Archival.md)

#### 4X — Interprocess & Communications
- [41 — Service APIs & Contracts](sections/4X_Interprocess_Communications/41_Service_APIs_Contracts.md)
- [42 — Transports](sections/4X_Interprocess_Communications/42_Transports.md)
- [43 — Channel Security](sections/4X_Interprocess_Communications/43_Channel_Security.md)

#### 5X — Hardware I/O & Device Layer
- [51 — Sensor & Actuator Abstractions](sections/5X_Hardware_IO_Device_Layer/51_Sensor_Actuator_Abstractions.md)
- [52 — AgIO Service](sections/5X_Hardware_IO_Device_Layer/52_AgIO_Service.md)
- [53 — AOG-Link Compatibility](sections/5X_Hardware_IO_Device_Layer/53_AOG_Link_Compatibility.md)
- [54 — CM5 Integrated Controller](sections/5X_Hardware_IO_Device_Layer/54_CM5_Integrated_Controller.md)
- [55 — Firmware Interfaces & Updates](sections/5X_Hardware_IO_Device_Layer/55_Firmware_Interfaces_Updates.md)

#### 6X — Core Domain Services
- [61 — Kinematics & Pose Fusion](sections/6X_Core_Domain_Services/61_Kinematics_Pose_Fusion.md)
- [62 — Job Lifecycle](sections/6X_Core_Domain_Services/62_Job_Lifecycle.md)
- [63 — Layers Registry & Journal Contracts](sections/6X_Core_Domain_Services/63_Layers_Registry_Journal.md)
- [64 — Telemetry & Health](sections/6X_Core_Domain_Services/64_Telemetry_Health.md)

#### 7X — Mapping & Geospatial
- [71 — Mapping Kernel & Registry Contracts](sections/7X_Mapping_Geospatial/71_Mapping_Kernel_Registry_Contracts.md)
- [72 — Mapping Layers Plugin](sections/7X_Mapping_Geospatial/72_Mapping_Layers_Plugin.md)
- [73 — Variable Mapping](sections/7X_Mapping_Geospatial/73_Variable_Mapping.md)
- [74 — Monitoring Systems](sections/7X_Mapping_Geospatial/74_Monitoring_Systems.md)
- [75 — Tiling & Rendering Services](sections/7X_Mapping_Geospatial/75_Tiling_Rendering_Services.md)
- [76 — Geospatial Extensibility](sections/7X_Mapping_Geospatial/76_Geospatial_Extensibility.md)
- [77 — Variable Rate Control](sections/7X_Mapping_Geospatial/77_Variable_Rate_Control.md)

#### 8X — Guidance
- [81 — Guidance Orchestrator](sections/8X_Guidance/81_Guidance_Orchestrator.md)
- [82 — Planning](sections/8X_Guidance/82_Planning.md)
- [83 — Autosteer Target Models](sections/8X_Guidance/83_Autosteer_Target_Models.md)

#### 9X — Frontends & Ops
- [91 — UI Shell & Layout](sections/9X_Frontends_Ops/91_UI_Shell_Layout.md)
- [92 — Gauges & Machine Panels](sections/9X_Frontends_Ops/92_Gauges_Machine_Panels.md)
- [93 — Command Line Interface](sections/9X_Frontends_Ops/93_Command_Line_Interface.md)
- [94 — Extensibility, Packaging & Updates](sections/9X_Frontends_Ops/94_Extensibility_Packaging_Updates.md)
- [95 — Security & Permissions](sections/9X_Frontends_Ops/95_Security_Permissions.md)
- [96 — Quality Engineering & Release](sections/9X_Frontends_Ops/96_Quality_Engineering_Release.md)
- [97 — Simulation & Replay](sections/9X_Frontends_Ops/97_Simulation_Replay.md)

### Reference library
- [Reference library overview](references/README.md)
- [Core reference catalog](references/core/README.md)
- [Guidance reference catalog](references/guidance/README.md)
- [Mapping reference catalog](references/mapping/README.md)

### Option catalog
#### 1X — Platform Foundations
- [11-O1 – Unified .NET 8 + Avalonia](sections/1X_Platform_Foundations/11-O1_Unified_DotNet8_Avalonia.md)

#### 2X — System Architecture
- [O-BACKEND-6 – Linux Core Service](sections/2X_System_Architecture/21_System_Decomposition_Boundaries.md)

#### 4X — Interprocess & Communications
- [C2 – Variable-rate Layer Streams](sections/4X_Interprocess_Communications/42_Transports.md#c2---versioned-variable-rate-layer-streams)
- [C3 – PGN Compatibility Bridge](sections/4X_Interprocess_Communications/42_Transports.md#c3---typed-facade--compatibility-bridge)
- [C6 – Gauge Telemetry Channels](sections/4X_Interprocess_Communications/42_Transports.md#c6---gauge-telemetry-channels)

#### 5X — Hardware I/O & Device Layer
- [O-HW-5 – Modular Layer Firmware](sections/5X_Hardware_IO_Device_Layer/51-O5%20-%20Modular%20firmware%20publishing%20variable-rate%20layers.md)
- [O-HW-7 – MultiSteer Configurator](sections/6X_Core_Domain_Services/61-O7%20-%20Multi-steer%20equipment%20configurator%20primitives.md)

#### 6X — Core Domain Services
- [C2 – Versioned Layer Registries](sections/4X_Interprocess_Communications/41_Service_APIs_Contracts.md#c2---versioned-layer-registries)
- [O-BACKEND-4 – Layer Controllers](sections/2X_System_Architecture/21-O5%20-%20Layer%20controllers%20with%20aggregation%20pipelines.md)
- [O-TELE-4 – Layer Diagnostics](sections/6X_Core_Domain_Services/64-O5%20-%20Layer%20diagnostics%20and%20health%20monitoring.md)

#### 7X — Mapping & Geospatial
- [C-DATA-8 – Metadata-driven Layer Catalog](sections/3X_Data_Storage/32_Persistence_Formats.md#3293-metadata-driven-layer-catalog-guidelines)

#### 9X — Frontends & Ops
- [Remote Clients & Companion Shell](sections/9X_Frontends_Ops/91_UI_Shell_Layout.md#919-design-considerations)
- [Layer Replay CI](sections/9X_Frontends_Ops/96_Quality_Engineering_Release.md#969-design-considerations)
- [Metadata-driven Dashboards](sections/9X_Frontends_Ops/91_UI_Shell_Layout.md#919-design-considerations)

### Appendices
- [DFU catalog schema](appendices/DFU_Catalog_Schema.md)
- [Gauge ID registry](appendices/GaugeId_Registry.md)
- **Schema definitions**
  - [Plugin catalog schema](appendices/schemas/plugin_catalog.schema.json)
  - [Plugin manifest schema](appendices/schemas/plugin_manifest.schema.json)
- **Sample manifests & datasets**
  - [Equipment manifest sample – articulated tractor](appendices/samples/examples/articulated-tractor.v1.json)
  - [Scenario library index](appendices/samples/scenarios/library.json)
  - [Scenario performance matrix](appendices/samples/scenarios/performance-matrix.json)
  - [Legacy auto-run manifest](appendices/samples/scenarios/legacy-auto-run/legacy-auto-run.json)
  - [Legacy auto-run soak report sample](appendices/samples/scenarios/legacy-auto-run/verification/soak-report.sample.json)
  - Plugin manifest samples
    - [Autosteer](appendices/samples/plugins/autosteer/1.0.0.json)
    - [Autosteer Lite](appendices/samples/plugins/autosteer-lite/1.2.0.json)
    - [Device manager](appendices/samples/plugins/device-manager/1.0.0.json)
    - [File IO](appendices/samples/plugins/file-io/1.0.0.json)
    - [GNSS + IMU fusion](appendices/samples/plugins/gnss-imu-fusion/1.0.0.json)
    - [ISOBUS bridge](appendices/samples/plugins/isobus-bridge/1.0.0.json)
    - [Job tasks](appendices/samples/plugins/job-tasks/1.0.0.json)
    - [NTRIP client](appendices/samples/plugins/ntrip-client/1.0.0.json)
    - [Planter monitor](appendices/samples/plugins/planter-monitor/1.0.0.json)
    - [Replay](appendices/samples/plugins/replay/1.0.0.json)
    - [Sections](appendices/samples/plugins/sections/1.1.0.json)
    - [Telemetry logging](appendices/samples/plugins/telemetry-logging/1.0.0.json)

### References
- [AgIO PGN baseline](references/AgIO_PGN_Baseline.md)
- [AgOpenGPS hardware platforms](references/AgOpenGPS_Hardware_Platforms.md)
- [ISOBUS section control](references/ISOBUS_Section_Control.md)

### ADR index
- [12-ADR-001 — .NET 8 runtime](sections/1X_Platform_Foundations/12-ADR-001 - Adopt .NET 8 LTS Runtime.md)
- [ADR – 002 gRPC contracts](sections/4X_Interprocess_Communications/41-ADR-002 - Expose Nexus services over gRPC protobuf contracts.md)
- [13-ADR-001 — Avalonia UI](sections/1X_Platform_Foundations/13-ADR-001 - Use Avalonia for the Nexus Desktop UI Shell.md)
- [ADR – 004 Composite simulation](sections/2X_System_Architecture/21-ADR-004 - Establish the composite simulation fabric (SimClock + SimBus).md)
- [ADR – 006 AOG Link MCU communications](sections/4X_Interprocess_Communications/42-ADR-006 - MCU communications over AOG-Link (nanopb).md)
- [ADR – 007 PoseStream section-state architecture](sections/6X_Core_Domain_Services/61-ADR-007 - PoseStream and SectionState architecture.md)
- [ADR – 008 Equipment hierarchy](sections/6X_Core_Domain_Services/61-ADR-008 - Equipment Implement Toolbar Section hierarchy.md)
- [ADR – 009 PoseStream vector tile-store persistence](sections/3X_Data_Storage/32-ADR-009 - PoseStream vector logs and layer TileStore persistence.md)
- [ADR – 00XX SHM fastpath for Pumpkin Pi](sections/5X_Hardware_IO_Device_Layer/54-ADR-00XX - 00XX CM5 SHM Fastpath + HAL Plugin (Pumpkin Pi).md)
- [ADR – 010 Layer registry for variable rate](sections/3X_Data_Storage/32-ADR-010 - Layer registry and variable-rate framework.md)
- [ADR – 011 Mapping visualization imagery](sections/9X_Frontends_Ops/91-ADR-011 - Mapping and visualization imagery pipeline.md)
- [ADR – 012 Multi-session PoseStream fusion](sections/3X_Data_Storage/32-ADR-012 - Multi-session and multi-PoseStream fusion.md)
- [ADR – 013 Derived products & analytics prescriptions](sections/3X_Data_Storage/32-ADR-013 - Derived products and prescription analytics.md)
- [ADR – 014 Interop prescription formats](sections/3X_Data_Storage/32-ADR-014 - Interop for prescription and agronomic formats.md)
- [ADR – 015 Section control grouping semantics](sections/6X_Core_Domain_Services/61-ADR-015 - Section control and grouping semantics.md)
- [ADR – 016 Firmware transport for variable-rate PGNs](sections/4X_Interprocess_Communications/42-ADR-016 - Firmware and transport for variable-rate layer PGNs.md)
- [ADR – 017 Profiles & kinematics](sections/6X_Core_Domain_Services/61-ADR-017 - Equipment profiles and kinematics.md)
- [ADR – 018 Plugin API](sections/9X_Frontends_Ops/94-ADR-018 - Plugin API Capability Discovery and Runtime Model.md)
- [ADR – 019 Provenance, audit, and QA](sections/6X_Core_Domain_Services/64-ADR-019 - Provenance audit and QA governance.md)
- [ADR – 020 Determinism replay CI](sections/9X_Frontends_Ops/96-ADR-020 - Determinism replay and CI guardrails.md)
- [ADR – 021 Timebase clock sync](sections/4X_Interprocess_Communications/42-ADR-021 - Timebase and clock synchronization.md)
- [ADR – 022 CRS units precision policy](sections/3X_Data_Storage/32-ADR-022 - CRS units and precision policy.md)
- [ADR – 023 Session & job model](sections/6X_Core_Domain_Services/62-ADR-023 - Session and job model with provenance graph.md)
- [ADR – 024 Discovery & identity](sections/4X_Interprocess_Communications/42-ADR-024 - Discovery and identity services.md)
- [ADR – 025 Data lifecycle & retention](sections/3X_Data_Storage/34-ADR-025 - Data lifecycle and retention policy.md)
- [ADR – 026 Performance budgets](sections/9X_Frontends_Ops/96-ADR-026 - Performance budgets and instrumentation.md)
- [ADR – 027 Spatial constraints](sections/7X_Mapping_Geospatial/72-ADR-027 - Spatial Constraints & Zone Policies.md)
- [ADR – 028 Stack boundaries](sections/2X_System_Architecture/21-ADR-028 - Nexus stack responsibilities & handoff boundaries.md)
- [ADR – 029 Mapping plugin architecture](sections/7X_Mapping_Geospatial/72-ADR-029 - Mapping as a plugin with a minimal geospatial kernel in Core.md)
- [ADR – 030 Field job sessions](sections/6X_Core_Domain_Services/62-ADR-030 - Field job sessions and lifecycle services.md)
- [ADR – 031 Official plugin bundle](sections/9X_Frontends_Ops/94-ADR-031 - Official Plugin Bundle Dependency Governance.md)
- [ADR – 032 Presets and layout linking](sections/9X_Frontends_Ops/91-ADR-032 - Presets and Layout Linking for Equipment Workflows.md)
- [ADR – 033 Guidance planner & autosteer](sections/8X_Guidance/81-ADR-033 - Guidance planner and autosteer orchestration.md)
- [ADR – 034 Metadata-driven dashboards](sections/9X_Frontends_Ops/91-ADR-034 - Metadata-driven dashboards and inspector surfaces.md)
- [ADR – 040 Season organizers](sections/3X_Data_Storage/31-ADR-040 - Season Organizers.md)
- [ADR – 041 Job sessions](sections/6X_Core_Domain_Services/62-ADR-041 - Job Sessions Lifecycle.md)
- [ADR – 043 Multi-field job envelopes](sections/3X_Data_Storage/31-ADR-043 - Multi-Field Job Envelopes.md)
- [ADR – 044 Zone drawing framework](sections/7X_Mapping_Geospatial/72-ADR-044 - Zone Drawing Framework.md)
- [ADR – 045 Crop type plugin](sections/7X_Mapping_Geospatial/72-ADR-045 - Crop Type Plugin & Layers.md)
- [ADR – 046 Genetics plugin](sections/7X_Mapping_Geospatial/72-ADR-046 - Genetics Plugin & Layers.md)
- [ADR – 047 Live telemetry mesh](sections/4X_Interprocess_Communications/42-ADR-047 - Live Telemetry Mesh.md)
- [ADR – 048 Radio bridge](sections/4X_Interprocess_Communications/42-ADR-048 - RadioBridge for ELRS LoRa Telemetry.md)
- [ADR – 049 Yield plugin](sections/7X_Mapping_Geospatial/72-ADR-049 - Yield & Analytics Plugin.md)
- [ADR – 050 Cost & profit plugin](sections/7X_Mapping_Geospatial/72-ADR-050 - Cost & Profit Plugin.md)
- [ADR – 051 Report builder](sections/9X_Frontends_Ops/91-ADR-051 - Report Builder & Export System.md)
- [ADR – 052 Field health plugin](sections/7X_Mapping_Geospatial/72-ADR-052 - Field Health & Risk Plugin.md)
- [ADR – 053 Weather plugin](sections/7X_Mapping_Geospatial/72-ADR-053 - Weather & Environment Plugin.md)
- [ADR – 054 Nexus CLI host](sections/9X_Frontends_Ops/93-ADR-054 - Nexus CLI Host and Plugin Verb Architecture.md)
- [ADR – 067 Equipment configuration kinematics](sections/6X_Core_Domain_Services/61-ADR-067 - Equipment configuration and axle-centric kinematics runtime.md)
- [ADR – 068 Layer controllers runtime](sections/2X_System_Architecture/21-ADR-068 - Layer Controllers & Aggregation Runtime.md)
- [ADR – 069 Guidance orchestrator](sections/8X_Guidance/81-ADR-069 - Guidance Orchestrator plugin.md)
- [ADR roadmap](sections/2X_System_Architecture/21-ADR-900 - PoseStream, Layer, and Control Program Roadmap.md)

## Workflow expectations
1. Start discussion in the matching GitHub Discussion for the section.
2. Open PRs to add or refine requirements (R-IDs) and options (O-IDs).
3. Maintainers review for clarity and formatting; contributors stay neutral until an ADR is published.
4. Cluster options into **decision families** (exclusive vs. composable) inside each section before deep evaluation. Document any sequencing (e.g., "pick OS target before UI skin").
5. When a family needs structured comparison, spin up a decision-matrix doc, capture scoring data, and link it from the section. This keeps the section readable while preserving analysis artifacts.
6. Once the community agrees, capture the outcome in an ADR that links back to the relevant section table.

## Status transitions
### Section status flow
- **Collecting proposals**: The default state. Entry criteria: a problem statement exists and at least one requirement is documented. Exit criteria: requirements cover baseline success metrics (e.g., hardware, latency, safety) and open questions are narrowed to decision-ready prompts.
- **Under review**: Maintainers believe the requirement set is complete enough to evaluate options. Exit criteria: decision matrix (if needed) linked, traceability matrix row completed, and blocking dependencies (see system slice index) addressed.
- **Ready for ADR**: Consensus has formed around a preferred option family and draft acceptance criteria exist. Exit criteria: ADR author identified and rollout/validation requirements captured.
- **Decided**: ADR merged. Ongoing tweaks require explicit ADR updates or follow-on requirements.

### Option status flow
- **Draft**: Option is being fleshed out; dependencies and readiness gates may be incomplete.
- **Under comparison**: Option participates in a decision matrix or structured evaluation; entry requires dependency prerequisites to be listed.
- **Candidate decision**: Option is the favored approach pending ADR write-up and validation/rollout checklists.
- **Retired**: Option remains in history but is no longer recommended; note the superseding ADR.

## Conventions
- **IDs**: `R-` for requirements, `O-` for options, `Q-` for open questions, and `ADR-` for decisions.
- **Status labels** in headings track whether a section is collecting proposals, under review, or decided.
- **Borrowables** highlight concrete code or assets from AgOpenGPS/AgIO or other projects that we can reuse.
- **Linting ideas**: unique IDs, table formatting, and link validation can be automated in CI.

## Traceability matrix
_This matrix links every requirement (R-) to the options, references, and eventual ADR homes that will satisfy it. “TBD” ADRs signal where future decisions will land once validation gates are met._

### Section 11 — OS Support
| Requirement | Options / Workstreams | References & tooling | ADR placeholder |
|-------------|----------------------|----------------------|-----------------|
| R-OS-000 | O-OS-0, O-OS-3, O-OS-6 | AgOpenGPS WinExe (legacy baseline) | ADR-OS-001 (TBD) |
| R-OS-001 | O-OS-0, O-OS-3, O-OS-6 | AgIO WinForms host (legacy baseline) | ADR-OS-001 (TBD) |
| R-OS-002 | O-OS-0, O-OS-6 | [Project README deployment notes](../../README.md) | ADR-OS-001 (TBD) |
| R-OS-003 | O-OS-3, O-OS-5 | Screen helper (legacy baseline) | ADR-OS-002 (TBD) |
| R-OS-004 | O-OS-5, O-OS-6 | [Linux Core option](sections/2X_System_Architecture/21_System_Decomposition_Boundaries.md) | ADR-OS-003 (TBD) |
| R-OS-005 | O-OS-5, O-OS-6 | [Linux Core option](sections/2X_System_Architecture/21_System_Decomposition_Boundaries.md) | ADR-OS-003 (TBD) |
| R-OS-006 | O-OS-3, O-OS-5, O-OS-6 | [Baseline assumptions](01_Project_Charter.md#104-baseline-field-assumptions) | ADR-OS-004 (TBD) |

### Section 13 — UI Framework & UX Language
| Requirement | Options / Workstreams | References & tooling | ADR placeholder |
|-------------|----------------------|----------------------|-----------------|
| R-UI-000 | O-UI-0 | WinForms project (legacy baseline) | ADR-UI-001 (TBD) |
| R-UI-001 | Retired | WPF shell maintenance removed; operators remain on AgOpenGPS v6 WinForms until Nexus UI ships. | ADR-UI-001 (TBD) |
| R-UI-002 | O-UI-0 | AgIO dialogs (legacy baseline) | ADR-UI-002 (TBD) |
| R-UI-003 | O-UI-0, O-UI-5 | Screen helper (legacy baseline) | ADR-UI-003 (TBD) |
| R-UI-004 | O-UI-5 | [Metadata dashboards](sections/9X_Frontends_Ops/91-O5%20-%20Metadata-driven%20dashboards%20and%20visualization.md) | ADR-UI-004 (TBD) |
| R-UI-005 | O-UI-6, O-UI-7 | [Remote clients](sections/9X_Frontends_Ops/91-O6%20-%20Remote%20gRPC-WebSocket%20clients%20backed%20by%20the%20Linux%20Core.md) | ADR-UI-005 (TBD) |
| R-UI-006 | O-UI-1, O-UI-2, O-UI-4, O-UI-7 | [Linux Core option](sections/2X_System_Architecture/21_System_Decomposition_Boundaries.md) | ADR-UI-006 (TBD) |
| R-UI-007 | O-UI-2, O-UI-5, O-UI-7 | [Accessibility presets](sections/9X_Frontends_Ops/91_UI_Shell_Layout.md) | ADR-UI-007 (TBD) |

### Section 42 — Transports
| Requirement | Options / Workstreams | References & tooling | ADR placeholder |
|-------------|----------------------|----------------------|-----------------|
| R-COMM-000 | O-COMM-0 | Comm settings dialog (legacy baseline) | ADR-COMM-001 (TBD) |
| R-COMM-001 | O-COMM-0 | UDP tooling (legacy baseline) | ADR-COMM-001 (TBD) |
| R-COMM-002 | O-COMM-0, C3 | PGN designer (legacy baseline) | ADR-COMM-002 (TBD) |
| R-COMM-003 | O-COMM-0 | NTRIP UI (legacy baseline) | ADR-COMM-003 (TBD) |
| R-COMM-010 | C2 | [Variable-rate layer streams](sections/4X_Interprocess_Communications/42_Transports.md#c2---versioned-variable-rate-layer-streams) | ADR-COMM-004 (TBD) |
| R-COMM-011 | C2 | [Diagnostics hooks](sections/6X_Core_Domain_Services/64-O5%20-%20Layer%20diagnostics%20and%20health%20monitoring.md) | ADR-COMM-004 (TBD) |
| R-COMM-004 | O-COMM-2, C3, C6 | [Linux Core option](sections/2X_System_Architecture/21-O6%20-%20Linux%20Core%20service%20with%20remote%20frontends.md) | ADR-COMM-005 (TBD) |
| R-COMM-005 | C3, C6 | [PGN bridge](sections/4X_Interprocess_Communications/42_Transports.md#c3---typed-facade--compatibility-bridge) | ADR-COMM-005 (TBD) |
| R-COMM-012 | C3, C5 | [Latency budgets](sections/4X_Interprocess_Communications/42_Transports.md) | ADR-COMM-006 (TBD) |
| R-COMM-013 | C3, C5 | [Security slice](sections/9X_Frontends_Ops/95_Security_Permissions.md) | ADR-COMM-006 (TBD) |

### Section 21 — System Decomposition & Boundaries
| Requirement | Options / Workstreams | References & tooling | ADR placeholder |
|-------------|----------------------|----------------------|-----------------|
| R-BE-000 | O-BE-0 | ApplicationCore (legacy baseline) | ADR-BE-001 (TBD) |
| R-BE-001 | O-BE-0 | Field streamer (legacy baseline) | ADR-BE-001 (TBD) |
| R-BE-002 | O-BE-0, O-BE-5 | Shared projects (legacy baseline) | ADR-BE-002 (TBD) |
| R-BE-003 | O-BE-1 | Automation APIs (legacy baseline) | ADR-BE-003 (TBD) |
| R-BE-010 | O-BE-5 | [Layer controllers](sections/2X_System_Architecture/21_System_Decomposition_Boundaries.md) | ADR-BE-004 (TBD) |
| R-BE-011 | O-BE-5 | [Replay CI](sections/9X_Frontends_Ops/96-O5%20-%20Replay-driven%20CI%20and%20rollout%20for%20layers.md) | ADR-BE-004 (TBD) |
| R-BE-004 | O-BE-6, O-BE-7 | [Linux Core option](sections/2X_System_Architecture/21-O6%20-%20Linux%20Core%20service%20with%20remote%20frontends.md) | ADR-BE-005 (TBD) |
| R-BE-012 | C3, C6 | [PGN bridge](sections/4X_Interprocess_Communications/42_Transports.md#c3---typed-facade--compatibility-bridge) | ADR-BE-005 (TBD) |
| R-BE-013 | O-BE-6, O-BE-7 | [Service health targets](sections/2X_System_Architecture/21_System_Decomposition_Boundaries.md) | ADR-BE-006 (TBD) |
| R-BE-014 | O-BE-6, O-BE-7 | [Fail-safe expectations](sections/2X_System_Architecture/21_System_Decomposition_Boundaries.md) | ADR-BE-006 (TBD) |

### Section 91 — UI Shell & Layout
| Requirement | Options / Workstreams | References & tooling | ADR placeholder |
|-------------|----------------------|----------------------|-----------------|
| R-FE-000 | O-FE-0 | WinForms project (legacy baseline) | ADR-FE-001 (TBD) |
| R-FE-001 | O-FE-0 | AgIO project (legacy baseline) | ADR-FE-001 (TBD) |
| R-FE-002 | O-FE-0 | Solution utilities (legacy baseline) | ADR-FE-002 (TBD) |
| R-FE-003 | O-FE-6, O-FE-7 | [Remote clients option](sections/9X_Frontends_Ops/91_UI_Shell_Layout.md#919-design-considerations) | ADR-FE-003 (TBD) |
| R-FE-010 | C3 | [Metadata dashboards](sections/9X_Frontends_Ops/91_UI_Shell_Layout.md#919-design-considerations) | ADR-FE-004 (TBD) |
| R-FE-011 | C3 | [Metadata dashboards](sections/9X_Frontends_Ops/91_UI_Shell_Layout.md#919-design-considerations) | ADR-FE-004 (TBD) |
| R-FE-004 | O-FE-6 | [Remote clients option](sections/9X_Frontends_Ops/91_UI_Shell_Layout.md#919-design-considerations) | ADR-FE-003 (TBD) |
| R-FE-012 | O-FE-6, O-FE-7 | [Remote clients option](sections/9X_Frontends_Ops/91_UI_Shell_Layout.md#919-design-considerations) | ADR-FE-005 (TBD) |
| R-FE-013 | O-FE-6, O-FE-7 | [Safety posture notes](sections/9X_Frontends_Ops/91_UI_Shell_Layout.md) | ADR-FE-005 (TBD) |
| R-FE-014 | O-FE-5 | [Training & presets](sections/9X_Frontends_Ops/91_UI_Shell_Layout.md) | ADR-FE-006 (TBD) |

### Section 51 — Sensor & Actuator Abstractions
| Requirement | Options / Workstreams | References & tooling | ADR placeholder |
|-------------|----------------------|----------------------|-----------------|
| R-HW-000 | O-HW-0 | Comm settings (legacy baseline) | ADR-HW-001 (TBD) |
| R-HW-001 | O-HW-0, O-HW-6 | PGN designer (legacy baseline) | ADR-HW-001 (TBD) |
| R-HW-002 | O-HW-0 | [Project README](../../README.md) | ADR-HW-002 (TBD) |
| R-HW-003 | O-HW-0 | UDP tool (legacy baseline) | ADR-HW-003 (TBD) |
| R-HW-010 | O-HW-5 | [Modular firmware option](sections/5X_Hardware_IO_Device_Layer/51-O5%20-%20Modular%20firmware%20publishing%20variable-rate%20layers.md) | ADR-HW-004 (TBD) |
| R-HW-011 | O-HW-5 | [Modular firmware option](sections/5X_Hardware_IO_Device_Layer/51-O5%20-%20Modular%20firmware%20publishing%20variable-rate%20layers.md) | ADR-HW-004 (TBD) |
| R-HW-004 | O-HW-6 | [Linux Core option](sections/2X_System_Architecture/21-O6%20-%20Linux%20Core%20service%20with%20remote%20frontends.md) | ADR-HW-005 (TBD) |
| R-HW-005 | C3 | [PGN bridge](sections/4X_Interprocess_Communications/42_Transports.md#c3---typed-facade--compatibility-bridge) | ADR-HW-005 (TBD) |
| R-HW-006 | O-HW-5 | [Capability discovery](sections/5X_Hardware_IO_Device_Layer/51_Sensor_Actuator_Abstractions.md) | ADR-HW-006 (TBD) |
| R-HW-012 | O-HW-5 | [ISOBUS reference](references/ISOBUS_Section_Control.md) | ADR-HW-007 (TBD) |
| R-HW-013 | O-HW-5, O-HW-6 | [Safety interlocks](sections/5X_Hardware_IO_Device_Layer/51_Sensor_Actuator_Abstractions.md) | ADR-HW-008 (TBD) |
| R-HW-014 | O-HW-5, O-HW-6 | [Certification placeholders](sections/5X_Hardware_IO_Device_Layer/51_Sensor_Actuator_Abstractions.md) | ADR-HW-008 (TBD) |

### Section 41 — Service APIs & Contracts
| Requirement | Options / Workstreams | References & tooling | ADR placeholder |
|-------------|----------------------|----------------------|-----------------|
| R-API-000 | O-API-0 | PGN designer (legacy baseline) | ADR-API-001 (TBD) |
| R-API-001 | O-API-0 | UDP monitor (legacy baseline) | ADR-API-001 (TBD) |
| R-API-002 | O-API-0 | NTRIP settings (legacy baseline) | ADR-API-002 (TBD) |
| R-API-003 | C2, C3 | [Schema policy](sections/4X_Interprocess_Communications/41_Service_APIs_Contracts.md) | ADR-API-003 (TBD) |
| R-API-010 | C2 | [Versioned registries](sections/4X_Interprocess_Communications/41_Service_APIs_Contracts.md#c2---versioned-layer-registries) | ADR-API-004 (TBD) |
| R-API-011 | C2 | [Versioned registries](sections/4X_Interprocess_Communications/41_Service_APIs_Contracts.md#c2---versioned-layer-registries) | ADR-API-004 (TBD) |
| R-API-004 | C3 | [PGN bridge](sections/4X_Interprocess_Communications/42_Transports.md#c3---typed-facade--compatibility-bridge) | ADR-API-005 (TBD) |
| R-API-005 | C3 | [Handshake notes](sections/4X_Interprocess_Communications/41_Service_APIs_Contracts.md) | ADR-API-006 (TBD) |
| R-API-012 | C2, C4 | [Release management policy](sections/4X_Interprocess_Communications/41_Service_APIs_Contracts.md) | ADR-API-007 (TBD) |

### Section 32 — Persistence & Formats
| Requirement | Options / Workstreams | References & tooling | ADR placeholder |
|-------------|----------------------|----------------------|-----------------|
| R-DATA-000 | O-DATA-0 | Field streamer (legacy baseline) | ADR-DATA-001 (TBD) |
| R-DATA-001 | O-DATA-0 | SQLite usage (legacy baseline) | ADR-DATA-001 (TBD) |
| R-DATA-002 | O-DATA-0 | Shared libraries (legacy baseline) | ADR-DATA-002 (TBD) |
| R-DATA-003 | O-DATA-0, C-DATA-8 | [Export formats](sections/3X_Data_Storage/32_Persistence_Formats.md) | ADR-DATA-003 (TBD) |
| R-DATA-010 | C-DATA-8 | [Metadata layers](sections/3X_Data_Storage/32_Persistence_Formats.md#3293-metadata-driven-layer-catalog-guidelines) | ADR-DATA-004 (TBD) |
| R-DATA-011 | C-DATA-8 | [Metadata layers](sections/3X_Data_Storage/32_Persistence_Formats.md#3293-metadata-driven-layer-catalog-guidelines) | ADR-DATA-004 (TBD) |
| R-DATA-012 | C-DATA-8 | [Metadata layers](sections/3X_Data_Storage/32_Persistence_Formats.md#3293-metadata-driven-layer-catalog-guidelines) | ADR-DATA-005 (TBD) |
| R-DATA-004 | C-DATA-8 | [Compression ideas](sections/3X_Data_Storage/32_Persistence_Formats.md) | ADR-DATA-006 (TBD) |
| R-DATA-013 | C-DATA-8 | [Retention policy](sections/3X_Data_Storage/32_Persistence_Formats.md) | ADR-DATA-007 (TBD) |
| R-DATA-014 | C-DATA-8 | [Schema hashes](sections/3X_Data_Storage/32_Persistence_Formats.md) | ADR-DATA-007 (TBD) |
| R-DATA-026 | C-DATA-8 | [Spatial constraints](sections/3X_Data_Storage/32_Persistence_Formats.md) | [ADR-027](sections/7X_Mapping_Geospatial/72-ADR-027 - Spatial Constraints & Zone Policies.md) |
| R-DATA-027 | C-DATA-8 | [Buffered footprints](sections/3X_Data_Storage/32_Persistence_Formats.md) | [ADR-027](sections/7X_Mapping_Geospatial/72-ADR-027 - Spatial Constraints & Zone Policies.md) |
| R-DATA-028 | C-DATA-8 | [Indexed queries](sections/3X_Data_Storage/32_Persistence_Formats.md) | [ADR-027](sections/7X_Mapping_Geospatial/72-ADR-027 - Spatial Constraints & Zone Policies.md) |
| R-DATA-040 | O-DATA-5 | [Zone edit provenance](sections/3X_Data_Storage/32_Persistence_Formats.md) | [ADR-044](sections/7X_Mapping_Geospatial/72-ADR-044 - Zone Drawing Framework.md) |
| R-DATA-041 | O-DATA-5 | [Crop history schema](sections/3X_Data_Storage/32_Persistence_Formats.md) | [ADR-045](sections/7X_Mapping_Geospatial/72-ADR-045 - Crop Type Plugin & Layers.md) |
| R-DATA-042 | O-DATA-5 | [Session weather snapshot](sections/3X_Data_Storage/32_Persistence_Formats.md) | [ADR-053](sections/7X_Mapping_Geospatial/72-ADR-053 - Weather & Environment Plugin.md) |
| R-DATA-043 | O-DATA-5 | [Plugin attribute schemas](sections/3X_Data_Storage/32_Persistence_Formats.md) | [ADR-044](sections/7X_Mapping_Geospatial/72-ADR-044 - Zone Drawing Framework.md) |
| R-DATA-044 | O-DATA-5 | [Genetics records](sections/3X_Data_Storage/32_Persistence_Formats.md) | [ADR-046](sections/7X_Mapping_Geospatial/72-ADR-046 - Genetics Plugin & Layers.md) |
| R-DATA-045 | O-DATA-5 | [Yield layers](sections/3X_Data_Storage/32_Persistence_Formats.md) | [ADR-049](sections/7X_Mapping_Geospatial/72-ADR-049 - Yield & Analytics Plugin.md) |
| R-DATA-046 | O-DATA-5 | [Cost & profit schemas](sections/3X_Data_Storage/32_Persistence_Formats.md) | [ADR-050](sections/7X_Mapping_Geospatial/72-ADR-050 - Cost & Profit Plugin.md) |
| R-DATA-047 | O-DATA-5 | [Risk overlays](sections/3X_Data_Storage/32_Persistence_Formats.md) | [ADR-052](sections/7X_Mapping_Geospatial/72-ADR-052 - Field Health & Risk Plugin.md) |
| R-DATA-048 | O-DATA-5 | [Weather overlays](sections/3X_Data_Storage/32_Persistence_Formats.md) | [ADR-053](sections/7X_Mapping_Geospatial/72-ADR-053 - Weather & Environment Plugin.md) |
| R-DATA-049 | O-DATA-5 | [Report templates](sections/3X_Data_Storage/32_Persistence_Formats.md) | [ADR-051](sections/9X_Frontends_Ops/91-ADR-051 - Report Builder & Export System.md) |
| R-DATA-050 | O-DATA-5 | [Inventory ledger](sections/3X_Data_Storage/32_Persistence_Formats.md) | [ADR-050](sections/7X_Mapping_Geospatial/72-ADR-050 - Cost & Profit Plugin.md) |
| R-DATA-051 | O-DATA-5 | [Inventory provenance](sections/3X_Data_Storage/32_Persistence_Formats.md) | [ADR-050](sections/7X_Mapping_Geospatial/72-ADR-050 - Cost & Profit Plugin.md) |

### Section 61 — Kinematics & Pose Fusion
| Requirement | Options / Workstreams | References & tooling | ADR placeholder |
|-------------|----------------------|----------------------|-----------------|
| R-MM-000 | O-MM-0 | Screen helper (legacy baseline) | ADR-MM-001 (TBD) |
| R-MM-001 | O-MM-0 | UDP monitor (legacy baseline) | ADR-MM-001 (TBD) |
| R-MM-002 | O-MM-2 | [Headless plans](sections/6X_Core_Domain_Services/61_Kinematics_Pose_Fusion.md) | ADR-MM-002 (TBD) |
| R-MM-003 | O-MM-2, O-OS-5 | [Linux Core option](sections/2X_System_Architecture/21_System_Decomposition_Boundaries.md) | ADR-MM-003 (TBD) |
| R-MM-004 | O-MM-2 | [Layout locking](sections/6X_Core_Domain_Services/61_Kinematics_Pose_Fusion.md) | ADR-MM-004 (TBD) |
| R-MM-005 | O-MM-2 | [Auto-recovery expectations](sections/6X_Core_Domain_Services/61_Kinematics_Pose_Fusion.md) | ADR-MM-005 (TBD) |

### Section 64 — Telemetry & Health
| Requirement | Options / Workstreams | References & tooling | ADR placeholder |
|-------------|----------------------|----------------------|-----------------|
| R-TH-000 | O-TH-0 | UDP monitor (legacy baseline) | ADR-TH-001 (TBD) |
| R-TH-001 | O-TH-0 | Event viewer (legacy baseline) | ADR-TH-001 (TBD) |
| R-TH-002 | O-TH-0 | Inspector tools (legacy baseline) | ADR-TH-002 (TBD) |
| R-TH-003 | O-TH-3 | [Telemetry feeds](sections/6X_Core_Domain_Services/64_Telemetry_Health.md) | ADR-TH-003 (TBD) |
| R-TH-004 | O-TH-3, O-OS-5 | [Linux Core option](sections/2X_System_Architecture/21_System_Decomposition_Boundaries.md) | ADR-TH-004 (TBD) |
| R-TH-010 | O-TH-4 | [Layer diagnostics](sections/6X_Core_Domain_Services/64-O5%20-%20Layer%20diagnostics%20and%20health%20monitoring.md) | ADR-TH-005 (TBD) |
| R-TH-011 | O-TH-4 | [Layer diagnostics](sections/6X_Core_Domain_Services/64-O5%20-%20Layer%20diagnostics%20and%20health%20monitoring.md) | ADR-TH-005 (TBD) |
| R-TH-005 | O-TH-3 | [Health scoring](sections/6X_Core_Domain_Services/64_Telemetry_Health.md) | ADR-TH-006 (TBD) |
| R-TH-012 | O-TH-3 | [Governance policies](sections/6X_Core_Domain_Services/64_Telemetry_Health.md) | ADR-TH-007 (TBD) |

### Section 96 — Quality Engineering & Release
| Requirement | Options / Workstreams | References & tooling | ADR placeholder |
|-------------|----------------------|----------------------|-----------------|
| R-CI-000 | O-CI-0 | Test projects (legacy baseline) | ADR-CI-001 (TBD) |
| R-CI-001 | O-CI-0 | [Manual publish flow](../../README.md) | ADR-CI-001 (TBD) |
| R-CI-002 | O-CI-0 | [Linting ideas](sections/9X_Frontends_Ops/96_Quality_Engineering_Release.md) | ADR-CI-002 (TBD) |
| R-CI-003 | O-CI-1 | [Packaging criteria](sections/9X_Frontends_Ops/96_Quality_Engineering_Release.md) | ADR-CI-003 (TBD) |
| R-CI-010 | O-CI-4 | [Replay CI option](sections/9X_Frontends_Ops/96-O5%20-%20Replay-driven%20CI%20and%20rollout%20for%20layers.md) | ADR-CI-004 (TBD) |
| R-CI-011 | O-CI-4 | [Replay CI option](sections/9X_Frontends_Ops/96-O5%20-%20Replay-driven%20CI%20and%20rollout%20for%20layers.md) | ADR-CI-004 (TBD) |
| R-CI-004 | O-CI-3 | [Linux Core option](sections/2X_System_Architecture/21_System_Decomposition_Boundaries.md) | ADR-CI-005 (TBD) |
| R-CI-005 | O-CI-4 | [Hardware-in-loop](sections/9X_Frontends_Ops/96_Quality_Engineering_Release.md) | ADR-CI-006 (TBD) |
| R-CI-012 | O-CI-3 | [Release assurance](sections/9X_Frontends_Ops/96_Quality_Engineering_Release.md) | ADR-CI-007 (TBD) |
| R-CI-013 | O-CI-4 | [Fixture governance](sections/9X_Frontends_Ops/96_Quality_Engineering_Release.md) | ADR-CI-007 (TBD) |

### Section 94 — Extensibility, Packaging & Updates
| Requirement | Options / Workstreams | References & tooling | ADR placeholder |
|-------------|----------------------|----------------------|-----------------|
| R-EXT-000 | O-EXT-0 | Shared projects (legacy baseline) | ADR-EXT-001 (TBD) |
| R-EXT-001 | O-EXT-0 | Solution utilities (legacy baseline) | ADR-EXT-001 (TBD) |
| R-EXT-002 | O-EXT-1, O-EXT-3, O-EXT-5 | [Plugin boundary](sections/9X_Frontends_Ops/94_Extensibility_Packaging_Updates.md) | ADR-EXT-002 (TBD) |
| R-EXT-003 | O-EXT-1, O-EXT-3, O-EXT-5 | [Templates](sections/9X_Frontends_Ops/94_Extensibility_Packaging_Updates.md) | ADR-EXT-002 (TBD) |
| R-EXT-010 | O-EXT-3, O-EXT-5 | [Layer controllers](sections/2X_System_Architecture/21_System_Decomposition_Boundaries.md) | ADR-EXT-003 (TBD) |
| R-EXT-004 | O-EXT-1 | [Sandboxing notes](sections/9X_Frontends_Ops/94_Extensibility_Packaging_Updates.md) | ADR-EXT-004 (TBD) |
| R-EXT-011 | O-EXT-3, O-EXT-5 | [Governance requirements](sections/9X_Frontends_Ops/94_Extensibility_Packaging_Updates.md) | ADR-EXT-005 (TBD) |
| R-PKG-000 | Workstream TBD | [Packaging flow](sections/9X_Frontends_Ops/94_Extensibility_Packaging_Updates.md#packaging-updates--catalog) | ADR-PKG-001 (TBD) |
| R-PKG-005 | Workstream TBD | [Permission prompts](sections/9X_Frontends_Ops/94_Extensibility_Packaging_Updates.md#packaging-updates--catalog) | ADR-PKG-002 (TBD) |
| R-PKG-020 | Workstream TBD | [Catalog schema](appendices/schemas/plugin_catalog.schema.json) | ADR-PKG-003 (TBD) |

### Section 95 — Security & Permissions
| Requirement | Options / Workstreams | References & tooling | ADR placeholder |
|-------------|----------------------|----------------------|-----------------|
| R-SEC-000 | O-SEC-0 | NTRIP dialog (legacy baseline) | ADR-SEC-001 (TBD) |
| R-SEC-001 | O-SEC-0 | [Offline workflow](../../README.md) | ADR-SEC-001 (TBD) |
| R-SEC-002 | O-SEC-2 | [Role guidance](sections/9X_Frontends_Ops/95_Security_Permissions.md) | ADR-SEC-002 (TBD) |
| R-SEC-003 | O-SEC-1 | [Secrets plan](sections/9X_Frontends_Ops/95_Security_Permissions.md) | ADR-SEC-003 (TBD) |
| R-SEC-004 | O-SEC-3 | [Linux Core option](sections/2X_System_Architecture/21_System_Decomposition_Boundaries.md) | ADR-SEC-004 (TBD) |
| R-SEC-005 | O-SEC-3 | [Audit logging](sections/9X_Frontends_Ops/95_Security_Permissions.md) | ADR-SEC-005 (TBD) |
| R-SEC-006 | O-SEC-1, O-SEC-4 | [Secrets migration](sections/9X_Frontends_Ops/95_Security_Permissions.md) | ADR-SEC-006 (TBD) |
| R-SEC-007 | O-SEC-3 | [Audit readiness](sections/9X_Frontends_Ops/95_Security_Permissions.md) | ADR-SEC-006 (TBD) |

### Section 33 — Offline-first & Sync
| Requirement | Options / Workstreams | References & tooling | ADR placeholder |
|-------------|----------------------|----------------------|-----------------|
| R-UPD-000 | O-UPD-0 | [Offline distribution](../../README.md) | ADR-UPD-001 (TBD) |
| R-UPD-001 | O-UPD-0 | [Publish workflow](../../README.md) | ADR-UPD-001 (TBD) |
| R-UPD-002 | O-UPD-2, O-UPD-4 | [Rollback guidance](sections/3X_Data_Storage/33_Offline_First_Sync.md) | ADR-UPD-002 (TBD) |
| R-UPD-003 | O-UPD-5 | [Staged updates](sections/3X_Data_Storage/33_Offline_First_Sync.md) | ADR-UPD-003 (TBD) |
| R-UPD-004 | O-UPD-5 | [Linux Core option](sections/2X_System_Architecture/21_System_Decomposition_Boundaries.md) | ADR-UPD-004 (TBD) |
| R-UPD-005 | O-UPD-1 | [Background updates](sections/3X_Data_Storage/33_Offline_First_Sync.md) | ADR-UPD-005 (TBD) |
| R-UPD-006 | O-UPD-5 | [Validation checklist](sections/3X_Data_Storage/33_Offline_First_Sync.md) | ADR-UPD-006 (TBD) |

### Section 74 — Monitoring Systems
| Requirement | Options / Workstreams | References & tooling | ADR placeholder |
|-------------|----------------------|----------------------|-----------------|
| R-GA-000 | Workstream TBD | [Gauge mappings](sections/7X_Mapping_Geospatial/74_Monitoring_Systems.md) | ADR-GA-001 (TBD) |
| R-GA-001 | Workstream TBD | [Gauge telemetry channels](sections/4X_Interprocess_Communications/42_Transports.md#c6---gauge-telemetry-channels) | ADR-GA-002 (TBD) |
| R-GA-002 | Workstream TBD | [Gauge configuration JSON](sections/7X_Mapping_Geospatial/74_Monitoring_Systems.md) | ADR-GA-003 (TBD) |
| R-GA-003 | Workstream TBD | [Gauge UI behaviors](sections/7X_Mapping_Geospatial/74_Monitoring_Systems.md) | ADR-GA-004 (TBD) |
| R-GA-004 | Workstream TBD | [Smoothing rules](sections/7X_Mapping_Geospatial/74_Monitoring_Systems.md) | ADR-GA-005 (TBD) |
| R-GA-005 | Workstream TBD | [Gauge capability discovery](sections/7X_Mapping_Geospatial/74_Monitoring_Systems.md) | ADR-GA-006 (TBD) |

### Section 55 — Firmware Interfaces & Updates
| Requirement | Options / Workstreams | References & tooling | ADR placeholder |
|-------------|----------------------|----------------------|-----------------|
| DFU-001 | Workstream TBD | [Identity discovery](sections/5X_Hardware_IO_Device_Layer/55_Firmware_Interfaces_Updates.md) | ADR-DFU-001 (TBD) |
| DFU-004 | Workstream TBD | [Update orchestration](sections/5X_Hardware_IO_Device_Layer/55_Firmware_Interfaces_Updates.md) | ADR-DFU-002 (TBD) |
| DFU-008 | Workstream TBD | [Offline bundles](sections/5X_Hardware_IO_Device_Layer/55_Firmware_Interfaces_Updates.md) | ADR-DFU-003 (TBD) |

### Section 93 — Command Line Interface
| Requirement | Options / Workstreams | References & tooling | ADR placeholder |
|-------------|----------------------|----------------------|-----------------|
| R-CLI-000…R-CLI-003 | O-CLI-0 | [Unified host requirements](sections/9X_Frontends_Ops/93_Command_Line_Interface.md) | ADR-054 (TBD) |
| R-CLI-004…R-CLI-007 | O-CLI-0 | [CLI UX & config discovery notes](sections/9X_Frontends_Ops/93_Command_Line_Interface.md) | ADR-054 (TBD) |
| R-CLI-008…R-CLI-010 | O-CLI-0 | [Versioning & packaging requirements](sections/9X_Frontends_Ops/93_Command_Line_Interface.md) | ADR-054 (TBD) |

## Glossary
- **AgIO**: Companion I/O service that provides network, CAN, and serial connectivity for AgOpenGPS.
- **ADR**: Architecture Decision Record capturing the context, choice, and consequences of an agreed solution.
- **Headless**: Running without a directly attached display, controlled remotely or via automation.
- **Kiosk mode**: Locked-down runtime experience intended for field operators with minimal UI.
- **Multi-monitor**: Use of two or more displays to show different dashboards or controls simultaneously.
- **Remote UI**: User interface accessed via another device (tablet, browser, thin client).
