# System Slices Map

This index lists every active section in the SRS with quick links. Each section stays decision-neutral until an ADR is written.

| ID | Section | Scope |
|----|---------|-------|
| 01 | [OS Support](sections/01_OS_Support.md) | Supported operating systems, deployment modes, and hardware assumptions. |
| 02 | [UI Framework](sections/02_Framework_UI.md) | Cross-platform UI stack, interaction models, and theming. |
| 03 | [Communications & Transports](sections/03_Comm_Transports.md) | Serial, UDP, CAN, and higher-level messaging (gRPC/WebSocket) strategies including layer PGN sequencing. |
| 04 | [Backend Services](sections/04_Backend_Services.md) | Core services: navigation, mapping, storage, rules/automation with metadata-driven layer controllers. |
| 05 | [Frontends](sections/05_Frontends.md) | Native, web, and remote display experiences with metadata-driven dashboards. |
| 06 | [Hardware I/O](sections/06_Hardware_IO.md) | Sensor/actuator integration, GPIO, safety interlocks, and firmware-discovered layer channels. |
| 07 | [Interprocess API](sections/07_Interprocess_API.md) | Message schemas, versioning, and compatibility policies with layer definition registries. |
| 08 | [Data Model & Storage](sections/08_Data_Model_Storage.md) | Field data structures, tiles, compression, export formats, and layer catalogs. |
| 09 | [Multi-monitor & Headless](sections/09_MultiMonitor_Headless.md) | Layout strategies, remote control, kiosk mode. |
| 10 | [Telemetry & Health](sections/10_Telemetry_Health.md) | Metrics, alerts, offline buffering, fleet monitoring, and layer diagnostics. |
| 11 | [Testing & CI/CD Pipelines](sections/11_Testing_CI_CDPipelines.md) | QA automation, hardware-in-the-loop, release packaging, replay suites. |
| 12 | [Extensibility & Plugins](sections/12_Extensibility_Plugins.md) | Plugin lifecycle, sandboxing, community contributions, layer registries. |
| 13 | [Security & Permissions](sections/13_Security_Permissions.md) | Auth, authorization, data protection. |
| 14 | [Offline-first & Updates](sections/14_Offline_First_Updates.md) | Packaging, delta updates, rollback, resilience. |
