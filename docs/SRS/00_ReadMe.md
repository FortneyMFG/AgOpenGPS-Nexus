# AgOpenGPS Next SRS

Welcome to the Software Requirements Specification (SRS) workspace for the next generation of AgOpenGPS. This folder curates requirements, options, and discussions before the community makes architecture decisions.

## Revision log
| Date | Summary | Key slices |
|------|---------|------------|

## How this SRS is organized
- **Vision & Non-goals** capture what the next release aspires to solve and what is intentionally out-of-scope.
- **System slices** map every focus area (OS, UI, comms, storage, etc.) to an individual
  section document under [`sections/`](sections/).
- **Sections** collect requirements and enumerate options. They are intentionally decision-neutral—decisions live in Architecture Decision Records (ADRs).
- **Options** can be expanded in dedicated files using the
  [`docs/templates/OPTION.md`](../templates/OPTION.md) template when deeper analysis is
  needed.
- **Decision matrices** use [`docs/templates/DECISION_MATRIX.md`](../templates/DECISION_MATRIX.md)
  to score mutually exclusive option families once requirements are stable.
- **References** house canonical specs (e.g., PGN catalogs) that new options must remain compatible with unless an ADR says otherwise.
- **ADRs** document finalized decisions. Each ADR references the section(s) and options involved so we preserve traceability.

## Document index

### Core overview
- [Vision & Non-goals](01_Vision_NonGoals.md)
- [System slices](02_System_Slices.md)
- [Working notes](NOTES.md)

### Section catalog
#### 1X — Platform Foundations
- [11 — OS Support](sections/1X/11_OS_Support.md)
- [12 — Development Language & Runtime](sections/1X/12_Development_Language_Runtime.md)
- [13 — UI Framework & UX Language](sections/1X/13_UI_Framework_UX.md)
- [14 — Build Environment & Tooling](sections/1X/14_Build_Tooling.md)

#### 2X — System Architecture
- [21 — System Decomposition & Boundaries](sections/2X/21_System_Decomposition_Boundaries.md)
- [22 — Process Model & Deployment Topologies](sections/2X/22_Process_Model_Deployment.md)
- [23 — Threading, Scheduling & Timing](sections/2X/23_Threading_Scheduling_Timing.md)
- [24 — Configuration & Environment](sections/2X/24_Configuration_Environment.md)

#### 3X — Data & Storage
- [31 — Domain Data Model](sections/3X/31_Domain_Data_Model.md)
- [32 — Persistence & Formats](sections/3X/32_Persistence_Formats.md)
- [33 — Offline-first & Sync](sections/3X/33_Offline_First_Sync.md)
- [34 — Backup, Retention & Archival](sections/3X/34_Backup_Retention_Archival.md)

#### 4X — Interprocess & Communications
- [41 — Inter-Application API](sections/4X/41_Inter_Application_API.md)
- [42 — Transports](sections/4X/42_Transports.md)
- [43 — Channel Security](sections/4X/43_Channel_Security.md)

#### 5X — Hardware I/O & Device Layer
- [51 — Sensor & Actuator Abstractions](sections/5X/51_Sensor_Actuator_Abstractions.md)
- [52 — AgIO Service](sections/5X/52_AgIO_Service.md)
- [53 — AOG-Link Compatibility](sections/5X/53_AOG_Link_Compatibility.md)
- [54 — CM5 Integrated Controller](sections/5X/54_CM5_Integrated_Controller.md)
- [55 — Firmware Interfaces & Updates](sections/5X/55_Firmware_Interfaces_Updates.md)

#### 6X — Core Domain Services
- [61 — Kinematics & Pose Fusion](sections/6X/61_Kinematics_Pose_Fusion.md)
- [62 — Job Lifecycle](sections/6X/62_Job_Lifecycle.md)
- [63 — Layers Registry & Journal Contracts](sections/6X/63_Layers_Registry_Journal.md)
- [64 — Telemetry & Health](sections/6X/64_Telemetry_Health.md)

#### 7X — Mapping & Geospatial
- [71 — Mapping Kernel Contracts](sections/7X/71_Mapping_Kernel_Contracts.md)
- [72 — Mapping Layers Plugin](sections/7X/72_Mapping_Layers_Plugin.md)
- [73 — Variable Mapping & Variable Rate Control](sections/7X/73_Variable_Mapping_Rate_Control.md)
- [74 — Monitoring Systems](sections/7X/74_Monitoring_Systems.md)
- [75 — Tiling & Rendering Services](sections/7X/75_Tiling_Rendering_Services.md)
- [76 — Geospatial Extensibility](sections/7X/76_Geospatial_Extensibility.md)

#### 8X — Guidance
- [81 — Guidance Orchestrator](sections/8X/81_Guidance_Orchestrator.md)
- [82 — Planning & Autosteer Targets](sections/8X/82_Planning_Autosteer_Targets.md)

#### 9X — Frontends & Ops
- [91 — UI Shell & Layout](sections/9X/91_UI_Shell_Layout.md)
- [92 — Gauges & Machine Panels](sections/9X/92_Gauges_Machine_Panels.md)
- [93 — Command Line Interface](sections/9X/93_Command_Line_Interface.md)
- [94 — Extensibility, Packaging & Updates](sections/9X/94_Extensibility_Packaging_Updates.md)
- [95 — Security & Permissions](sections/9X/95_Security_Permissions.md)
- [96 — Quality Engineering & Release](sections/9X/96_Quality_Engineering_Release.md)

### Option catalog
#### 1X — Platform Foundations
- [O-STACK-1 – .NET 8 + Avalonia](options/1X/O-STACK-1_DotNet8Avalonia.md)

#### 2X — System Architecture
- [O-BACKEND-6 – Linux Core Service](options/2X/O-BACKEND-6_LinuxCoreService.md)

#### 4X — Interprocess & Communications
- [O-COMM-5 – Variable-rate PGNs](options/4X/O-COMM-5_VariableRatePGNs.md)
- [O-COMM-6 – PGN Compatibility Bridge](options/4X/O-COMM-6_PGNCompatibilityBridge.md)
- [O-COMM-7 – Gauge Telemetry PGNs](options/4X/O-COMM-7_GaugeTelemetryPGNs.md)

#### 5X — Hardware I/O & Device Layer
- [O-HW-5 – Modular Layer Firmware](options/5X/O-HW-5_ModularLayerFirmware.md)
- [O-HW-7 – MultiSteer Configurator](options/5X/O-HW-7_MultiSteerConfigurator.md)

#### 6X — Core Domain Services
- [O-API-5 – Versioned Layer Schemas](options/6X/O-API-5_VersionedLayerSchemas.md)
- [O-BACKEND-4 – Layer Controllers](options/6X/O-BACKEND-4_LayerControllers.md)
- [O-TELE-4 – Layer Diagnostics](options/6X/O-TELE-4_LayerDiagnostics.md)

#### 7X — Mapping & Geospatial
- [O-DATA-5 – Metadata-driven Layers](options/7X/O-DATA-5_MetadataDrivenLayers.md)

#### 9X — Frontends & Ops
- [O-FRONT-6 – Remote Clients](options/9X/O-FRONT-6_RemoteClients.md)
- [O-TEST-4 – Layer Replay CI](options/9X/O-TEST-4_LayerReplayCI.md)
- [O-UI-5 – Metadata-driven Dashboards](options/9X/O-UI-5_MetadataDrivenDashboards.md)

### Appendices
- [DFU catalog schema](appendices/DFU_Catalog_Schema.md)
- [Gauge ID registry](appendices/GaugeId_Registry.md)
- [Plugin catalog schema](appendices/plugin_catalog.schema.json)
- [Plugin manifest schema](appendices/plugin_manifest.schema.json)

### References
- [AgIO PGN baseline](references/AgIO_PGN_Baseline.md)
- [AgOpenGPS hardware platforms](references/AgOpenGPS_Hardware_Platforms.md)
- [ISOBUS section control](references/ISOBUS_Section_Control.md)

### ADR index
- [ADR – 001 .NET 8 runtime](../ADR/ADR-001-dotnet8-runtime.md)
- [ADR – 002 gRPC contracts](../ADR/ADR-002-grpc-contracts.md)
- [ADR – 003 Avalonia UI](../ADR/ADR-003-avalonia-ui.md)
- [ADR – 004 Composite simulation](../ADR/ADR-004-composite-simulation.md)
- [ADR – 006 AOG Link MCU communications](../ADR/ADR-006-aog-link-mcu-communications.md)
- [ADR – 007 PoseStream section-state architecture](../ADR/ADR-007-posestream-sectionstate-architecture.md)
- [ADR – 008 Equipment hierarchy](../ADR/ADR-008-equipment-hierarchy.md)
- [ADR – 009 PoseStream vector tile-store persistence](../ADR/ADR-009-posestream-vector-tilestore-persistence.md)
- [ADR – 00XX SHM fastpath for Pumpkin Pi](../ADR/ADR-00XX-shm-fastpath-pumpkin-pi.md)
- [ADR – 010 Layer registry for variable rate](../ADR/ADR-010-layer-registry-variable-rate.md)
- [ADR – 011 Mapping visualization imagery](../ADR/ADR-011-mapping-visualization-imagery.md)
- [ADR – 012 Multi-session PoseStream fusion](../ADR/ADR-012-multi-session-posestream-fusion.md)
- [ADR – 013 Derived products & analytics prescriptions](../ADR/ADR-013-derived-products-analytics-prescriptions.md)
- [ADR – 014 Interop prescription formats](../ADR/ADR-014-interop-prescription-formats.md)
- [ADR – 015 Section control grouping semantics](../ADR/ADR-015-section-control-grouping-semantics.md)
- [ADR – 016 Firmware transport for variable-rate PGNs](../ADR/ADR-016-firmware-transport-variable-rate-pgns.md)
- [ADR – 017 Profiles & kinematics](../ADR/ADR-017-profiles-kinematics.md)
- [ADR – 018 Plugin API](../ADR/ADR-018-plugin-api.md)
- [ADR – 019 Provenance, audit, and QA](../ADR/ADR-019-provenance-audit-qa.md)
- [ADR – 020 Determinism replay CI](../ADR/ADR-020-determinism-replay-ci.md)
- [ADR – 021 Timebase clock sync](../ADR/ADR-021-timebase-clock-sync.md)
- [ADR – 022 CRS units precision policy](../ADR/ADR-022-crs-units-precision-policy.md)
- [ADR – 023 Session & job model](../ADR/ADR-023-session-job-model.md)
- [ADR – 024 Discovery & identity](../ADR/ADR-024-discovery-identity.md)
- [ADR – 025 Data lifecycle & retention](../ADR/ADR-025-data-lifecycle-retention.md)
- [ADR – 026 Performance budgets](../ADR/ADR-026-performance-budgets.md)
- [ADR – 027 Spatial constraints](../ADR/ADR-027-spatial-constraints.md)
- [ADR – 028 Stack boundaries](../ADR/ADR-028-stack-boundaries.md)
- [ADR – 029 Mapping plugin architecture](../ADR/ADR-029-mapping-plugin-architecture.md)
- [ADR – 030 Field job sessions](../ADR/ADR-030-field-job-sessions.md)
- [ADR – 031 Official plugin bundle](../ADR/ADR-031-official-plugin-bundle.md)
- [ADR – 032 Presets and layout linking](../ADR/ADR-032-presets-and-layout-linking.md)
- [ADR – 033 Guidance planner & autosteer](../ADR/ADR-033-guidance-planner-autosteer.md)
- [ADR – 034 Metadata-driven dashboards](../ADR/ADR-034-metadata-driven-dashboards.md)
- [ADR – 040 Season organizers](../ADR/ADR-040_SeasonOrganizers.md)
- [ADR – 041 Job sessions](../ADR/ADR-041_JobSessions.md)
- [ADR – 043 Multi-field job envelopes](../ADR/ADR-043_MultiFieldJobEnvelopes.md)
- [ADR – 044 Zone drawing framework](../ADR/ADR-044_ZoneDrawingFramework.md)
- [ADR – 045 Crop type plugin](../ADR/ADR-045_CropTypePlugin.md)
- [ADR – 046 Genetics plugin](../ADR/ADR-046_GeneticsPlugin.md)
- [ADR – 047 Live telemetry mesh](../ADR/ADR-047_LiveTelemetryMesh.md)
- [ADR – 048 Radio bridge](../ADR/ADR-048_RadioBridge.md)
- [ADR – 049 Yield plugin](../ADR/ADR-049_YieldPlugin.md)
- [ADR – 050 Cost & profit plugin](../ADR/ADR-050_CostProfitPlugin.md)
- [ADR – 051 Report builder](../ADR/ADR-051_ReportBuilder.md)
- [ADR – 052 Field health plugin](../ADR/ADR-052_FieldHealthPlugin.md)
- [ADR – 053 Weather plugin](../ADR/ADR-053_WeatherPlugin.md)
- [ADR – 054 Nexus CLI host](../ADR/ADR-054_NexusCliHost.md)
- [ADR – 067 Equipment configuration kinematics](../ADR/ADR-067-equipment-configuration-kinematics.md)
- [ADR – 068 Layer controllers runtime](../ADR/ADR-068-layer-controllers-runtime.md)
- [ADR – 069 Guidance orchestrator](../ADR/ADR-069_GuidanceOrchestrator.md)
- [ADR roadmap](../ADR/ADR-roadmap.md)

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

## Glossary
- **AgIO**: Companion I/O service that provides network, CAN, and serial connectivity for AgOpenGPS.
- **ADR**: Architecture Decision Record capturing the context, choice, and consequences of an agreed solution.
- **Headless**: Running without a directly attached display, controlled remotely or via automation.
- **Kiosk mode**: Locked-down runtime experience intended for field operators with minimal UI.
- **Multi-monitor**: Use of two or more displays to show different dashboards or controls simultaneously.
- **Remote UI**: User interface accessed via another device (tablet, browser, thin client).
