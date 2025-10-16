# System Slices Map

This index lists every active section in the SRS with quick links. Each section stays decision-neutral until an ADR is written.

| ID | Section | Scope | Dependencies / sequencing hints |
|----|---------|-------|----------------------------------|
| 01 | [OS Support](sections/01_OS_Support.md) | Supported operating systems, deployment modes, and hardware assumptions. | Baseline for hardware targets feeding 02, 04, 05, and 11; Linux pilots depend on R-OS-006.
| 02 | [UI Framework](sections/02_Framework_UI.md) | Cross-platform UI stack, interaction models, and theming. | Builds on 01; metadata dashboards depend on 04 and 08 readiness.
| 03 | [Communications & Transports](sections/03_Comm_Transports.md) | Serial, UDP, CAN, and higher-level messaging (gRPC/WebSocket) strategies including layer PGN sequencing. | Supplies contracts for 04, 06, 07; Linux Core ADRs require PGN bridge and latency budgets.
| 04 | [Backend Services](sections/04_Backend_Services.md) | Core services: navigation, mapping, storage, rules/automation with metadata-driven layer controllers. | Depends on 03 and 08; service health metrics unlock Linux Core options and remote clients.
| 05 | [Frontends](sections/05_Frontends.md) | Native, web, and remote display experiences with metadata-driven dashboards. | Relies on 02 and 04; remote control states require 03 latency budgets and 13 security policies.
| 06 | [Hardware I/O](sections/06_Hardware_IO.md) | Sensor/actuator integration, GPIO, safety interlocks, and firmware-discovered layer channels. | Coupled to 03 transports and 04 controllers; safety interlocks gate 05/14 rollouts.
| 07 | [Interprocess API](sections/07_Interprocess_API.md) | Message schemas, versioning, and compatibility policies with layer definition registries. | Shares registries with 03/08; plugin governance (12) depends on release policy R-API-012.
| 08 | [Data Model & Storage](sections/08_Data_Model_Storage.md) | Field data structures, tiles, compression, export formats, and layer catalogs. | Provides persistence for 04, 05, 07, and 10; retention envelopes align with 14 update strategies.
| 09 | [Control & Automation](sections/09_Control_Automation.md) | Control graph, automation lifecycle, multi-monitor/headless UX. | Builds on 02 and 05; auto-recovery hooks require 11 CI coverage for kiosk scripts.
| 10 | [Telemetry & Health](sections/10_Telemetry_Health.md) | Metrics, alerts, offline buffering, fleet monitoring, and layer diagnostics. | Consumes 03 transports and 04 services; alert/privacy policies feed 13 security reviews.
| 11 | [Testing & CI/CD Pipelines](sections/11_Testing_CI_CDPipelines.md) | QA automation, hardware-in-the-loop, release packaging, replay suites. | Validates 03–10; readiness levels for Linux Core require R-CI-010 and R-CI-013 coverage.
| 12 | [Extensibility & Plugins](sections/12_Extensibility_Plugins.md) | Plugin lifecycle, sandboxing, community contributions, layer registries. | Depends on 07 schema governance and 13 security to approve DI extension ADRs.
| 13 | [Security & Permissions](sections/13_Security_Permissions.md) | Auth, authorization, data protection. | Prerequisite for remote clients (05), modern transports (03), and Core rollout (04/14).
| 14 | [Offline-first & Updates](sections/14_Offline_First_Updates.md) | Packaging, delta updates, rollback, resilience. | Built on 01 packaging targets, 03 compatibility guarantees, 04 service health, and 11 artifact standards.
| 15 | [Engine & Machine Gauges](sections/15_Engine_Machine_Gauges.md) | Read-only engine telemetry, gauge PGNs, JSON definitions, and shared UI behaviors. | Builds on 03 transport contracts and 05 frontend layouts; feeds 04 service controllers and 10 telemetry dashboards.
| 16 | [Plugin Packaging, Updates, and Catalog](sections/16_Plugin_Packaging_Updates.md) | Plugin archive layout, manifest schema, permissions, catalog governance, and side-loading policies. | Relies on 12 extensibility scaffolding and 14 update distribution; sets trust inputs for 17 firmware updates.
| 17 | [Device Firmware Updates](sections/17_Device_Firmware_Updates.md) | Device identity discovery, cataloged firmware releases, orchestrated flashing flows, and offline bundles. | Depends on 03 transports, 14 offline strategies, and 16 packaging policies; informs hardware readiness in 06.
| 18 | [Command Line Interface](sections/18_Command_Line_Interface.md) | Unified `nx` host, transport negotiation, plugin verb discovery, and packaging strategy for automation workflows. | Builds on 03 transports, 07 interprocess APIs, 12 plugin governance, and 16 packaging policies; feeds docs in Section G tasks.

## Upcoming ADR program

PoseStream, layer, and control workstreams captured in the [ADR roadmap](../ADR/ADR-roadmap.md) span Sections 03, 07–12. Track requirements R-COMM-020/R-COMM-021, R-GEO-000–R-GEO-002, R-DATA-015–R-DATA-018, R-CTRL-000–R-CTRL-002, R-TH-020/R-TH-021, R-CI-020–R-CI-030, and R-EXT-120 as prerequisites for those ADRs before promoting related options to review.
