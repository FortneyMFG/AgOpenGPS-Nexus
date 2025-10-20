# System Slices Map

This index lists every active section in the SRS with quick links. Each section stays decision-neutral until an ADR is written.

| ID | Section | Scope | Dependencies / sequencing hints |
|----|---------|-------|----------------------------------|
<<<<<<< HEAD
| 11 | [OS Support](sections/1X_Platform_Foundations/11_OS_Support.md) | Supported operating systems, deployment models, and hardware assumptions. | Baseline for hardware targets feeding 12, 21, and 52; Linux pilots depend on R-OS-006. |
| 12 | [Development Language & Runtime](sections/1X_Platform_Foundations/12_Development_Language_Runtime.md) | Managed runtime, language policy, dependency governance. | Builds on 11; contract versioning shared with 63 and 94. |
| 13 | [UI Framework & UX Language](sections/1X_Platform_Foundations/13_UI_Framework_UX.md) | Cross-platform UI stack, MVVM conventions, theming. | Depends on 11/12; metadata dashboards rely on 63 and 71 readiness. |
| 14 | [Build Environment & Tooling](sections/1X_Platform_Foundations/14_Build_Tooling.md) | Toolchains, reproducible builds, signing, developer onboarding. | Feeds 96 quality gates and 94 packaging standards. |
| 21 | [System Decomposition & Boundaries](sections/2X_System_Architecture/21_System_Decomposition_Boundaries.md) | Core vs. AgIO vs. UI vs. plugin responsibilities. | Consumes 11–13 inputs; informs 22–24 design envelopes. |
| 22 | [Process Model & Deployment Topologies](sections/2X_System_Architecture/22_Process_Model_Deployment.md) | In-process, split-core, remote, and container deployments. | Depends on 21 decomposition; remote clients require 41–43 security and transport guarantees. |
| 23 | [Threading, Scheduling & Timing](sections/2X_System_Architecture/23_Threading_Scheduling_Timing.md) | Shared clocks, latency budgets, scheduling primitives. | Drives 61 pose fusion and 81 guidance loops; telemetry coverage in 64. |
| 24 | [Configuration & Environment](sections/2X_System_Architecture/24_Configuration_Environment.md) | Profiles, secrets, feature flags, environment detection. | Consumes 41/63 registries; rollout gating in 94/95. |
| 31 | [Domain Data Model](sections/3X_Data_Storage/31_Domain_Data_Model.md) | Farm → season → job → session hierarchies, provenance. | Feeds 32 persistence and 62 lifecycle orchestration. |
| 32 | [Persistence & Formats](sections/3X_Data_Storage/32_Persistence_Formats.md) | Layer schemas, tile stores, export formats. | Consumed by 63 registries, 75 tiling, 34 retention. |
| 33 | [Offline-first & Sync](sections/3X_Data_Storage/33_Offline_First_Sync.md) | Local caches, sync strategies, conflict handling. | Builds on 32; dependencies for 22 remote deployments and 34 backups. |
| 34 | [Backup, Retention & Archival](sections/3X_Data_Storage/34_Backup_Retention_Archival.md) | Retention policies, backups, regulatory exports. | Depends on 32 persistence and 33 sync flows; telemetry surfaced via 64. |
| 41 | [Inter-Application API](sections/4X_Interprocess_Communications/41_Inter_Application_API.md) | gRPC contracts, versioning, compatibility policies. | Shares registries with 63; consumed by 52 AgIO and 93 CLI. |
| 42 | [Transports](sections/4X_Interprocess_Communications/42_Transports.md) | UDP/TCP, Serial, SocketCAN, BLE/Wi-Fi transports. | Supplies contracts for 41, 51, 52; Linux Core ADRs require latency budgets. |
| 43 | [Channel Security](sections/4X_Interprocess_Communications/43_Channel_Security.md) | TLS/mTLS, identity, key rotation, channel audit. | Prerequisite for remote clients (22/91) and plugin governance (94/95). |
| 51 | [Sensor & Actuator Abstractions](sections/5X_Hardware_IO_Device_Layer/51_Sensor_Actuator_Abstractions.md) | Hardware abstraction layers, safety interlocks. | Coupled to 42 transports and 52 AgIO; informs 61/73 control loops. |
| 52 | [AgIO Service](sections/5X_Hardware_IO_Device_Layer/52_AgIO_Service.md) | Driver lifecycle, discovery, hot-swap policies. | Builds on 41/42/51; surfaces health into 64 telemetry. |
| 53 | [AOG-Link Compatibility](sections/5X_Hardware_IO_Device_Layer/53_AOG_Link_Compatibility.md) | AOG-Link v0/v1 wire formats, discovery, bridging. | Feeds 52 and 54; timing constraints for 23. |
| 54 | [CM5 Integrated Controller](sections/5X_Hardware_IO_Device_Layer/54_CM5_Integrated_Controller.md) | CM5 I/O maps, watchdogs, co-located Core/AgIO. | Depends on 51–53; informs 22 topologies and 55 firmware flows. |
| 55 | [Firmware Interfaces & Updates](sections/5X_Hardware_IO_Device_Layer/55_Firmware_Interfaces_Updates.md) | Bootloaders, DFU orchestration, version policy. | Requires 42 transports and 94 packaging trust; surfaces results in 64 telemetry. |
| 61 | [Kinematics & Pose Fusion](sections/6X_Core_Domain_Services/61_Kinematics_Pose_Fusion.md) | Fusion stack, pose quality, automation readiness. | Depends on 51/52 inputs; drives 23 scheduling and 81 guidance. |
| 62 | [Job Lifecycle](sections/6X_Core_Domain_Services/62_Job_Lifecycle.md) | Session state machine, audit trail, lifecycle events. | Builds on 31 domain model; feeds 63 registries and 91 UI flows. |
| 63 | [Layers Registry & Journal Contracts](sections/6X_Core_Domain_Services/63_Layers_Registry_Journal.md) | Layer registry governance, journal APIs, provenance. | Consumed by 71–76 mapping sections and 94 plugin governance. |
| 64 | [Telemetry & Health](sections/6X_Core_Domain_Services/64_Telemetry_Health.md) | Metrics, alerts, diagnostics, health workflows. | Depends on 23 timing and 52/61 services; feeds 96 QA and 94 packaging policy. |
| 71 | [Mapping Kernel Contracts](sections/7X_Mapping_Geospatial/71_Mapping_Kernel_Contracts.md) | Core mapping APIs, registry guarantees. | Requires 63 registries; feeds 72–76 plugin work. |
| 72 | [Mapping Layers Plugin](sections/7X_Mapping_Geospatial/72_Mapping_Layers_Plugin.md) | Plugin-facing layer behaviors, editing flows. | Depends on 71 and 63; informs 73/76 extensibility. |
| 73 | [Variable Mapping & Variable Rate Control](sections/7X_Mapping_Geospatial/73_Variable_Mapping_Rate_Control.md) | Variable-rate schemas, controller loops. | Builds on 61, 71, 72; hardware actions via 51/52. |
| 74 | [Monitoring Systems](sections/7X_Mapping_Geospatial/74_Monitoring_Systems.md) | Gauge telemetry, monitoring overlays, analytics. | Consumes 42 transports and 64 telemetry; UI hooks in 92. |
| 75 | [Tiling & Rendering Services](sections/7X_Mapping_Geospatial/75_Tiling_Rendering_Services.md) | Tile storage, GPU upload, rendering budgets. | Depends on 32 persistence and 91 UI shells; performance covered in 96. |
| 76 | [Geospatial Extensibility](sections/7X_Mapping_Geospatial/76_Geospatial_Extensibility.md) | Custom layer types, CRS, extensibility governance. | Requires 63 registries and 71/72 contracts; plugin discovery in 94. |
| 81 | [Guidance Orchestrator](sections/8X_Guidance/81_Guidance_Orchestrator.md) | Boundary management, keep-outs, orchestrator UX. | Depends on 61 fusion, 73 rate policies, and 91 UI shell. |
| 82 | [Planning](sections/8X_Guidance/82_Planning.md) | Planner integration, caching, and refresh policies. | Builds on 81 orchestrator, 23 timing, and 64 telemetry. |
| 83 | [Autosteer Target Models](sections/8X_Guidance/83_Autosteer_Target_Models.md) | Controller models, fallback parity, and tuning workflows. | Depends on 61 pose fusion, 81 orchestrator, and 82 planning. |
| 91 | [UI Shell & Layout](sections/9X_Frontends_Ops/91_UI_Shell_Layout.md) | Desktop/tablet shells, layout/docking, remote clients. | Consumes 11–13 foundations and 71 mapping contracts; depends on 95 security for remote control. |
| 92 | [Gauges & Machine Panels](sections/9X_Frontends_Ops/92_Gauges_Machine_Panels.md) | Gauge layout, units, operator UX policies. | Depends on 74 monitoring data and 91 layout infrastructure. |
| 93 | [Command Line Interface](sections/9X_Frontends_Ops/93_Command_Line_Interface.md) | Headless operations, scripting verbs, automation flows. | Builds on 41 APIs, 42 transports, and 94 plugin governance. |
| 94 | [Extensibility, Packaging & Updates](sections/9X_Frontends_Ops/94_Extensibility_Packaging_Updates.md) | Plugin lifecycle, manifests, catalogs, simulation contracts. | Depends on 63 registries and 95 security; informs 55 firmware updates. |
| 95 | [Security & Permissions](sections/9X_Frontends_Ops/95_Security_Permissions.md) | AuthN/Z, capability gating, data protection. | Required by 22 remote deployments, 94 plugin governance, and 43 channel security. |
| 96 | [Quality Engineering & Release](sections/9X_Frontends_Ops/96_Quality_Engineering_Release.md) | Testing strategy, CI/CD, release criteria, soak coverage. | Validates 11–95; relies on 14 build tooling and feeds release gates. |
=======
| 11 | [OS Support](sections/1X/11_OS_Support.md) | Supported operating systems, deployment models, and hardware assumptions. | Baseline for hardware targets feeding 12, 21, and 52; Linux pilots depend on R-OS-006. |
| 12 | [Development Language & Runtime](sections/1X/12_Development_Language_Runtime.md) | Managed runtime, language policy, dependency governance. | Builds on 11; contract versioning shared with 63 and 94. |
| 13 | [UI Framework & UX Language](sections/1X/13_UI_Framework_UX.md) | Cross-platform UI stack, MVVM conventions, theming. | Depends on 11/12; metadata dashboards rely on 63 and 71 readiness. |
| 14 | [Build Environment & Tooling](sections/1X/14_Build_Tooling.md) | Toolchains, reproducible builds, signing, developer onboarding. | Feeds 96 quality gates and 94 packaging standards. |
| 21 | [System Decomposition & Boundaries](sections/2X/21_System_Decomposition_Boundaries.md) | Core vs. AgIO vs. UI vs. plugin responsibilities. | Consumes 11–13 inputs; informs 22–24 design envelopes. |
| 22 | [Process Model & Deployment Topologies](sections/2X/22_Process_Model_Deployment.md) | In-process, split-core, remote, and container deployments. | Depends on 21 decomposition; remote clients require 41–43 security and transport guarantees. |
| 23 | [Threading, Scheduling & Timing](sections/2X/23_Threading_Scheduling_Timing.md) | Shared clocks, latency budgets, scheduling primitives. | Drives 61 pose fusion and 81 guidance loops; telemetry coverage in 64. |
| 24 | [Configuration & Environment](sections/2X/24_Configuration_Environment.md) | Profiles, secrets, feature flags, environment detection. | Consumes 41/63 registries; rollout gating in 94/95. |
| 31 | [Domain Data Model](sections/3X/31_Domain_Data_Model.md) | Farm → season → job → session hierarchies, provenance. | Feeds 32 persistence and 62 lifecycle orchestration. |
| 32 | [Persistence & Formats](sections/3X/32_Persistence_Formats.md) | Layer schemas, tile stores, export formats. | Consumed by 63 registries, 75 tiling, 34 retention. |
| 33 | [Offline-first & Sync](sections/3X/33_Offline_First_Sync.md) | Local caches, sync strategies, conflict handling. | Builds on 32; dependencies for 22 remote deployments and 34 backups. |
| 34 | [Backup, Retention & Archival](sections/3X/34_Backup_Retention_Archival.md) | Retention policies, backups, regulatory exports. | Depends on 32 persistence and 33 sync flows; telemetry surfaced via 64. |
| 41 | [Inter-Application API](sections/4X/41_Inter_Application_API.md) | gRPC contracts, versioning, compatibility policies. | Shares registries with 63; consumed by 52 AgIO and 93 CLI. |
| 42 | [Transports](sections/4X/42_Transports.md) | UDP/TCP, Serial, SocketCAN, BLE/Wi-Fi transports. | Supplies contracts for 41, 51, 52; Linux Core ADRs require latency budgets. |
| 43 | [Channel Security](sections/4X/43_Channel_Security.md) | TLS/mTLS, identity, key rotation, channel audit. | Prerequisite for remote clients (22/91) and plugin governance (94/95). |
| 51 | [Sensor & Actuator Abstractions](sections/5X/51_Sensor_Actuator_Abstractions.md) | Hardware abstraction layers, safety interlocks. | Coupled to 42 transports and 52 AgIO; informs 61/73 control loops. |
| 52 | [AgIO Service](sections/5X/52_AgIO_Service.md) | Driver lifecycle, discovery, hot-swap policies. | Builds on 41/42/51; surfaces health into 64 telemetry. |
| 53 | [AOG-Link Compatibility](sections/5X/53_AOG_Link_Compatibility.md) | AOG-Link v0/v1 wire formats, discovery, bridging. | Feeds 52 and 54; timing constraints for 23. |
| 54 | [CM5 Integrated Controller](sections/5X/54_CM5_Integrated_Controller.md) | CM5 I/O maps, watchdogs, co-located Core/AgIO. | Depends on 51–53; informs 22 topologies and 55 firmware flows. |
| 55 | [Firmware Interfaces & Updates](sections/5X/55_Firmware_Interfaces_Updates.md) | Bootloaders, DFU orchestration, version policy. | Requires 42 transports and 94 packaging trust; surfaces results in 64 telemetry. |
| 61 | [Kinematics & Pose Fusion](sections/6X/61_Kinematics_Pose_Fusion.md) | Fusion stack, pose quality, automation readiness. | Depends on 51/52 inputs; drives 23 scheduling and 81 guidance. |
| 62 | [Job Lifecycle](sections/6X/62_Job_Lifecycle.md) | Session state machine, audit trail, lifecycle events. | Builds on 31 domain model; feeds 63 registries and 91 UI flows. |
| 63 | [Layers Registry & Journal Contracts](sections/6X/63_Layers_Registry_Journal.md) | Layer registry governance, journal APIs, provenance. | Consumed by 71–76 mapping sections and 94 plugin governance. |
| 64 | [Telemetry & Health](sections/6X/64_Telemetry_Health.md) | Metrics, alerts, diagnostics, health workflows. | Depends on 23 timing and 52/61 services; feeds 96 QA and 94 packaging policy. |
| 71 | [Mapping Kernel Contracts](sections/7X/71_Mapping_Kernel_Contracts.md) | Core mapping APIs, registry guarantees. | Requires 63 registries; feeds 72–76 plugin work. |
| 72 | [Mapping Layers Plugin](sections/7X/72_Mapping_Layers_Plugin.md) | Plugin-facing layer behaviors, editing flows. | Depends on 71 and 63; informs 73/76 extensibility. |
| 73 | [Variable Mapping & Variable Rate Control](sections/7X/73_Variable_Mapping_Rate_Control.md) | Variable-rate schemas, controller loops. | Builds on 61, 71, 72; hardware actions via 51/52. |
| 74 | [Monitoring Systems](sections/7X/74_Monitoring_Systems.md) | Gauge telemetry, monitoring overlays, analytics. | Consumes 42 transports and 64 telemetry; UI hooks in 92. |
| 75 | [Tiling & Rendering Services](sections/7X/75_Tiling_Rendering_Services.md) | Tile storage, GPU upload, rendering budgets. | Depends on 32 persistence and 91 UI shells; performance covered in 96. |
| 76 | [Geospatial Extensibility](sections/7X/76_Geospatial_Extensibility.md) | Custom layer types, CRS, extensibility governance. | Requires 63 registries and 71/72 contracts; plugin discovery in 94. |
| 81 | [Guidance Orchestrator](sections/8X/81_Guidance_Orchestrator.md) | Boundary management, keep-outs, orchestrator UX. | Depends on 61 fusion, 73 rate policies, and 91 UI shell. |
| 82 | [Planning](sections/8X/82_Planning.md) | Planner integration, caching, and refresh policies. | Builds on 81 orchestrator, 23 timing, and 64 telemetry. |
| 83 | [Autosteer Target Models](sections/8X/83_Autosteer_Target_Models.md) | Controller models, fallback parity, and tuning workflows. | Depends on 61 pose fusion, 81 orchestrator, and 82 planning. |
| 91 | [UI Shell & Layout](sections/9X/91_UI_Shell_Layout.md) | Desktop/tablet shells, layout/docking, remote clients. | Consumes 11–13 foundations and 71 mapping contracts; depends on 95 security for remote control. |
| 92 | [Gauges & Machine Panels](sections/9X/92_Gauges_Machine_Panels.md) | Gauge layout, units, operator UX policies. | Depends on 74 monitoring data and 91 layout infrastructure. |
| 93 | [Command Line Interface](sections/9X/93_Command_Line_Interface.md) | Headless operations, scripting verbs, automation flows. | Builds on 41 APIs, 42 transports, and 94 plugin governance. |
| 94 | [Extensibility, Packaging & Updates](sections/9X/94_Extensibility_Packaging_Updates.md) | Plugin lifecycle, manifests, catalogs, simulation contracts. | Depends on 63 registries and 95 security; informs 55 firmware updates. |
| 95 | [Security & Permissions](sections/9X/95_Security_Permissions.md) | AuthN/Z, capability gating, data protection. | Required by 22 remote deployments, 94 plugin governance, and 43 channel security. |
| 96 | [Quality Engineering & Release](sections/9X/96_Quality_Engineering_Release.md) | Testing strategy, CI/CD, release criteria, soak coverage. | Validates 11–95; relies on 14 build tooling and feeds release gates. |
>>>>>>> origin/develop


## Upcoming ADR program

PoseStream, layer, and control workstreams captured in the [ADR roadmap](../ADR/ADR-roadmap.md) span Sections 41–43, 61–64, and 71–76. Track requirements R-COMM-020/R-COMM-021, R-GEO-000–R-GEO-002, R-DATA-015–R-DATA-018, R-CTRL-000–R-CTRL-002, R-TH-020/R-TH-021, R-CI-020–R-CI-030, and R-EXT-120 as prerequisites for those ADRs before promoting related options to review.
