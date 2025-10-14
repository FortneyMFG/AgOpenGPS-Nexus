# ADR Roadmap: PoseStream, Layer, and Control Program

This tracker consolidates current and planned Architecture Decision Records so the Nexus team can stage the PoseStream, section control, and variable-rate overhaul in one place. It anchors task **NX-150** and links each ADR to the SRS requirements that must be satisfied before drafting or promoting the decision for review.【F:tasks.md†L131-L137】 Use it to coordinate sequencing, ensure prerequisite requirements are in place, and keep the community focused on the same backlog of decisions.

## Adopted decisions (foundation)

| ADR | Title | Status | Key SRS coverage | Requirement anchors |
| --- | --- | --- | --- | --- |
| [ADR-001](ADR-001-dotnet8-runtime.md) | Adopt .NET 8 C# stack for Nexus runtime | Accepted | SRS §01 OS Support, §02 UI Framework | R-OS-000…R-OS-006, R-UI-000…R-UI-007【F:docs/ADR/ADR-001-dotnet8-runtime.md†L1-L24】【F:docs/SRS/sections/01_OS_Support.md†L1-L44】【F:docs/SRS/sections/02_Framework_UI.md†L1-L48】 |
| [ADR-002](ADR-002-grpc-contracts.md) | Expose Nexus services over gRPC/protobuf contracts | Accepted | SRS §03 Communications & Transports, §07 Interprocess API | R-COMM-000…R-COMM-013, R-API-000…R-API-012【F:docs/ADR/ADR-002-grpc-contracts.md†L1-L28】【F:docs/SRS/sections/03_Comm_Transports.md†L1-L27】【F:docs/SRS/sections/07_Interprocess_API.md†L1-L18】 |
| [ADR-003](ADR-003-avalonia-ui.md) | Use Avalonia for the cross-platform Nexus UI shell | Accepted | SRS §02 UI Framework, §05 Frontends | R-UI-000…R-UI-007, R-FE-000…R-FE-014【F:docs/ADR/ADR-003-avalonia-ui.md†L1-L22】【F:docs/SRS/sections/02_Framework_UI.md†L1-L48】【F:docs/SRS/sections/05_Frontends.md†L1-L120】 |
| [ADR-004](ADR-004-composite-simulation.md) | Establish the composite simulation fabric (SimClock + SimBus) | Accepted | SRS §04 Backend Services, §12 Extensibility & Plugins | R-BE-000…R-BE-014, R-EXT-030…R-EXT-103【F:docs/ADR/ADR-004-composite-simulation.md†L1-L20】【F:docs/SRS/sections/04_Backend_Services.md†L1-L120】【F:docs/SRS/sections/12_Extensibility_Plugins.md†L51-L74】 |
| [ADR-006](ADR-006-aog-link-mcu-communications.md) | MCU communications over AOG-Link (nanopb) | Accepted | SRS §03 Communications & Transports, §06 Hardware I/O | R-COMM-000…R-COMM-013, R-HW-000…R-HW-014【F:docs/ADR/ADR-006-aog-link-mcu-communications.md†L1-L55】【F:docs/SRS/sections/03_Comm_Transports.md†L1-L27】【F:docs/SRS/sections/06_Hardware_IO.md†L1-L38】 |

## Planned ADRs (staged under NX-150)

Each entry inherits the numbering shown here (ADR-007 through ADR-020) now that ADR-005 has been retired. Draft authors should reference the listed requirements and tasks before opening a proposal.

### ADR-007 — PoseStream & SectionState architecture
- **Scope:** A single, time-ordered PoseStream spanning tractor, implement, toolbar, and section poses with diffed SectionState updates and replay determinism.
- **Key decisions:** Cadence/decimation policy, SectionState diff rules, serialization schema, replay determinism budget, and how to compute layer opportunity metrics.
- **SRS alignment:** Communications (§03), Data Model (§08), Multi-monitor/Control (§09), Extensibility (§12).【F:docs/SRS/sections/03_Comm_Transports.md†L6-L27】【F:docs/SRS/sections/08_Data_Model_Storage.md†L6-L21】【F:docs/SRS/sections/09_MultiMonitor_Headless.md†L6-L15】【F:docs/SRS/sections/12_Extensibility_Plugins.md†L6-L21】
- **Primary requirements:** R-COMM-010, R-COMM-011, R-COMM-020; R-DATA-015; R-CTRL-000…R-CTRL-002; R-EXT-120.【F:docs/SRS/sections/03_Comm_Transports.md†L11-L18】【F:docs/SRS/sections/08_Data_Model_Storage.md†L11-L20】【F:docs/SRS/sections/09_MultiMonitor_Headless.md†L13-L15】【F:docs/SRS/sections/12_Extensibility_Plugins.md†L11-L21】
- **Tasks:** PoseStreamService, SectionStateManager, shared proto/schema definitions, replay fixtures.

### ADR-008 — Equipment → Implement → Toolbar → Section(+Group) hierarchy
- **Scope:** Canonical object model with multiple toolbars per implement, overlapping SectionGroups, master groups, and per-toolbar lookahead/overlap metadata.
- **Key decisions:** Stable IDs, offsets, widths, grouping semantics, and which nodes/groups accept control targets.
- **SRS alignment:** Interprocess API (§07) for schema governance and Control (§09) for group arbitration.【F:docs/SRS/sections/07_Interprocess_API.md†L16-L18】【F:docs/SRS/sections/09_MultiMonitor_Headless.md†L13-L15】
- **Primary requirements:** R-GEO-000…R-GEO-002; R-CTRL-000…R-CTRL-002.【F:docs/SRS/sections/07_Interprocess_API.md†L16-L18】【F:docs/SRS/sections/09_MultiMonitor_Headless.md†L13-L15】
- **Tasks:** Data model updates, configuration editor, migration shims for legacy implements.

### ADR-009 — Persistence & storage (vector stream + tile store)
- **Scope:** Authoritative PoseStream/SectionState vector logs and chunked tile store with value/weight/min/max channels plus indexes and compression.
- **Key decisions:** Tile/cell sizing, codec selection (LZ4/Zstd), crash safety guarantees, registry hash flow, append/compaction strategies.
- **SRS alignment:** Data Model & Storage (§08).【F:docs/SRS/sections/08_Data_Model_Storage.md†L6-L21】
- **Primary requirements:** R-DATA-010…R-DATA-018.【F:docs/SRS/sections/08_Data_Model_Storage.md†L11-L20】
- **Tasks:** TileStore service, index format, compactor, read/write tests.

### ADR-010 — Layer registry & variable-rate framework
- **Scope:** LayerDefinition schema, units, normalization, color ramps, aggregation modes, discovery/versioning; replaces legacy ADR-005.
- **Key decisions:** IDs, schema hashes, numeric precision, display metadata, registry publishing cadence.
- **SRS alignment:** Data Model (§08) and Extensibility (§12).【F:docs/SRS/sections/08_Data_Model_Storage.md†L6-L21】【F:docs/SRS/sections/12_Extensibility_Plugins.md†L6-L21】
- **Primary requirements:** R-DATA-010…R-DATA-014; R-EXT-010; R-EXT-120.【F:docs/SRS/sections/08_Data_Model_Storage.md†L11-L16】【F:docs/SRS/sections/12_Extensibility_Plugins.md†L11-L21】
- **Tasks:** Registry service, validators, units library integration.

### ADR-011 — Mapping & visualization pipeline
- **Scope:** Transform PoseStream samples into ribbons, heatmaps, contours, and legends with bilinear sampling, near-vehicle LOD, opacity stacking, basemap integration.
- **Key decisions:** Render order, GPU texture strategy, interpolation rules, “fresh pass” emphasis, overview pyramid levels.
- **SRS alignment:** Telemetry & Health (§10) and Frontend rendering surfaces.【F:docs/SRS/sections/10_Telemetry_Health.md†L6-L47】【F:docs/SRS/sections/05_Frontends.md†L8-L20】
- **Primary requirements:** R-TH-000…R-TH-021; R-FE-004…R-FE-014.【F:docs/SRS/sections/10_Telemetry_Health.md†L7-L47】【F:docs/SRS/sections/05_Frontends.md†L8-L20】
- **Tasks:** Renderer refactor, shared color-ramp utility, legend/overlay widgets.

### ADR-012 — Multi-session & multi-PoseStream fusion
- **Scope:** Merge rules for multiple PoseStreams within a session and across seasons (e.g., planter + sprayer, prior-year yield to current prescriptions).
- **Key decisions:** Time/space alignment, CRS reprojection, dedupe/priority rules, area-weighted merges, provenance chain handling.
- **SRS alignment:** Data Model & Storage (§08).【F:docs/SRS/sections/08_Data_Model_Storage.md†L6-L21】
- **Primary requirements:** R-DATA-015…R-DATA-018.【F:docs/SRS/sections/08_Data_Model_Storage.md†L17-L20】
- **Tasks:** Fusion service, provenance tracker, conflict policies.

### ADR-013 — Derived products (analytics → prescriptions)
- **Scope:** Convert yield/soil/NDVI PoseStreams and layers into prescriptions with gridding, smoothing, ROI masking, banding/clamping, and QA metrics.
- **Key decisions:** Target functions, QA metrics, parameterizable recipes, output validation loops.
- **SRS alignment:** Testing & Analytics governance (§11).【F:docs/SRS/sections/11_Testing_CI_CDPipelines.md†L6-L49】
- **Primary requirements:** R-CI-021 plus existing replay and provenance requirements (R-CI-010, R-CI-020, R-CI-030).【F:docs/SRS/sections/11_Testing_CI_CDPipelines.md†L11-L19】
- **Tasks:** Derivation pipeline, configurable recipes, QA reports.

### ADR-014 — Interop: prescription & agronomic formats
- **Scope:** Import/export ISOXML TaskData, GeoTIFF/COG rasters, Shapefile/GeoPackage vectors, MBTiles tilesets, CSV grids with consistent units/CRS and attribute mapping.
- **Key decisions:** Canonical internal units, CRS policy, attribute naming, file naming conventions.
- **SRS alignment:** Communications (§03 interop notes) and Data Model (§08 exports).【F:docs/SRS/sections/03_Comm_Transports.md†L6-L27】【F:docs/SRS/sections/08_Data_Model_Storage.md†L6-L16】
- **Primary requirements:** R-DATA-003, R-DATA-014, R-DATA-018; interop expectations in R-COMM-005.【F:docs/SRS/sections/08_Data_Model_Storage.md†L10-L20】【F:docs/SRS/sections/03_Comm_Transports.md†L13-L18】
- **Tasks:** Importers/exporters, unit normalization, sample fixtures.

### ADR-015 — Section control & grouping semantics
- **Scope:** Control graph for on/auto/off states, master group actions, overlapping groups, per-toolbar lookahead/overlap with overrides, and plugin hooks.
- **Key decisions:** Arbitration priorities, gating by SectionGroup, plugin extension hooks, override safety policies.
- **SRS alignment:** Control semantics captured in §09 and plugin integration in §12.【F:docs/SRS/sections/09_MultiMonitor_Headless.md†L13-L45】【F:docs/SRS/sections/12_Extensibility_Plugins.md†L6-L21】
- **Primary requirements:** R-CTRL-000…R-CTRL-002; R-EXT-120.【F:docs/SRS/sections/09_MultiMonitor_Headless.md†L13-L15】【F:docs/SRS/sections/12_Extensibility_Plugins.md†L21-L21】
- **Tasks:** Control engine updates, configuration UI, simulation/replay tests.

### ADR-016 — Firmware/transport: variable-rate & layer PGNs
- **Scope:** CAN/UDP message suite for layer definitions and feedback (E2/E1/E0/DF/E3/E4), node IDs, sequencing/timing, registry hash handshake.
- **Key decisions:** Payload packing, version bits, timeout/handshake strategy, compatibility with existing PGNs.
- **SRS alignment:** Communications & Transports (§03) and Hardware I/O (§06).【F:docs/SRS/sections/03_Comm_Transports.md†L6-L27】【F:docs/SRS/sections/06_Hardware_IO.md†L6-L20】
- **Primary requirements:** R-COMM-010…R-COMM-021; R-HW-010…R-HW-014.【F:docs/SRS/sections/03_Comm_Transports.md†L11-L18】【F:docs/SRS/sections/06_Hardware_IO.md†L10-L20】
- **Tasks:** AgIO bridge updates, firmware stubs, simulators, conformance tests.

### ADR-017 — Profiles & kinematics (hitches, pivot tongues, multi-steer)
- **Scope:** Tractor/implement profiles, hitch linkage models, IMU/GNSS fusion, and which pose is “steered” for autosteer and mapping.
- **Key decisions:** Kinematic models, attachment points, toolbar placement priority, fusion of multiple pose sources.
- **SRS alignment:** Interprocess API (§07 geometry metadata) and Control (§09 lookahead).【F:docs/SRS/sections/07_Interprocess_API.md†L16-L18】【F:docs/SRS/sections/09_MultiMonitor_Headless.md†L13-L15】
- **Primary requirements:** R-GEO-001…R-GEO-002; R-CTRL-002.【F:docs/SRS/sections/07_Interprocess_API.md†L17-L18】【F:docs/SRS/sections/09_MultiMonitor_Headless.md†L15-L15】
- **Tasks:** Kinematics library, profile editor, sensor fusion hooks.

### ADR-018 — Plugin API & capability discovery
- **Scope:** Contracts for publishing/consuming PoseStream deltas, declaring layers, control endpoints, and hardware bindings with permissions and lifecycle governance.
- **Key decisions:** Manifest schema, permission model, lifecycle events, hot-plug strategy, capability discovery handshake.
- **SRS alignment:** Extensibility & Plugins (§12) and Communications (§03) for capability negotiation.【F:docs/SRS/sections/12_Extensibility_Plugins.md†L6-L82】【F:docs/SRS/sections/03_Comm_Transports.md†L6-L27】
- **Primary requirements:** R-EXT-000…R-EXT-120; R-COMM-020…R-COMM-021.【F:docs/SRS/sections/12_Extensibility_Plugins.md†L6-L21】【F:docs/SRS/sections/03_Comm_Transports.md†L17-L18】
- **Tasks:** SDK, sample plugins (planter monitor, rate controller, autosteer), documentation.

### ADR-019 — Provenance, audit, and QA
- **Scope:** Provenance schema, job/session IDs, dataset hashes, quality flags, audit trail expectations across storage, UI, and exports.
- **Key decisions:** Provenance registry contents, chain-of-custody rules, QA badge surfacing, CI hash checks.
- **SRS alignment:** Telemetry & Health (§10) for visualization hooks and Testing/CI (§11) for artifact governance.【F:docs/SRS/sections/10_Telemetry_Health.md†L6-L47】【F:docs/SRS/sections/11_Testing_CI_CDPipelines.md†L6-L49】
- **Primary requirements:** R-TH-010…R-TH-021; R-CI-020.【F:docs/SRS/sections/10_Telemetry_Health.md†L12-L17】【F:docs/SRS/sections/11_Testing_CI_CDPipelines.md†L17-L19】
- **Tasks:** Provenance writer, hash checks in CI, UI badges.

### ADR-020 — Determinism, replay & CI
- **Scope:** Golden replays from PoseStream to identical tiles/exports with defined performance and size budgets.
- **Key decisions:** Hashing scheme, fixture format, pass/fail criteria, CI integration strategy.
- **SRS alignment:** Testing & CI (§11) and Data Model (§08) for replay parity expectations.【F:docs/SRS/sections/11_Testing_CI_CDPipelines.md†L6-L50】【F:docs/SRS/sections/08_Data_Model_Storage.md†L6-L21】
- **Primary requirements:** R-CI-010, R-CI-013, R-CI-030; R-DATA-015.【F:docs/SRS/sections/11_Testing_CI_CDPipelines.md†L11-L19】【F:docs/SRS/sections/08_Data_Model_Storage.md†L17-L17】
- **Tasks:** Replay tool, golden datasets, performance gates.

## How to use this tracker
- **Before drafting an ADR**, confirm the associated requirements are satisfied or add missing ones to the SRS within the relevant section (Sections 03, 07–12 already include new requirement IDs for this program).【F:docs/SRS/sections/03_Comm_Transports.md†L6-L27】【F:docs/SRS/sections/07_Interprocess_API.md†L6-L18】【F:docs/SRS/sections/08_Data_Model_Storage.md†L6-L20】【F:docs/SRS/sections/09_MultiMonitor_Headless.md†L6-L15】【F:docs/SRS/sections/10_Telemetry_Health.md†L6-L17】【F:docs/SRS/sections/11_Testing_CI_CDPipelines.md†L6-L19】【F:docs/SRS/sections/12_Extensibility_Plugins.md†L6-L21】
- **During implementation**, link work items to NX-150 and update this file with progress notes or additional prerequisites discovered by prototypes or field feedback.
- **When an ADR is approved**, move it to the adopted table above and ensure the SRS section references are updated to point to the final record.
