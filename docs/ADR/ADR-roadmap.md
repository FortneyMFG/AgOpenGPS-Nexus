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

### ADR-029 — Mapping plugin architecture & geospatial kernel split
- **Scope:** Move mapping engines into plugins while Core keeps a minimal geospatial kernel (CRS transforms, tiling helpers, monotonic timebase, deterministic replay scaffolding, null providers). Ensure headless rigs and alternate pose sources can run without mapping while variable-rate and sections consume a stable Mapping API.【F:docs/ADR/ADR-029-mapping-plugin-architecture.md†L7-L84】
- **Key decisions:** Mapping contracts in `Aog.Abstractions`, capability registry entries (`mapping:raster@v1`, `mapping:vector@v2`), event bus fan-out, replay taps, and plugin lifecycle/health semantics.【F:docs/ADR/ADR-029-mapping-plugin-architecture.md†L19-L84】
- **SRS alignment:** Communications (§03 timebase/event bus), Backend Services (§04 replay/registry), Data Model (§08 layers), Extensibility (§12 plugin lifecycle).【F:docs/SRS/sections/03_Comm_Transports.md†L6-L28】【F:docs/SRS/sections/04_Backend_Services.md†L6-L20】【F:docs/SRS/sections/08_Data_Model_Storage.md†L6-L31】【F:docs/SRS/sections/12_Extensibility_Plugins.md†L6-L80】
- **Primary requirements:** R-COMM-020…R-COMM-042; R-BE-000…R-BE-014; R-DATA-010…R-DATA-025; R-EXT-010…R-EXT-134.【F:docs/SRS/sections/03_Comm_Transports.md†L11-L28】【F:docs/SRS/sections/04_Backend_Services.md†L6-L20】【F:docs/SRS/sections/08_Data_Model_Storage.md†L11-L31】【F:docs/SRS/sections/12_Extensibility_Plugins.md†L11-L80】
- **Tasks:** Draft Mapping proto/service updates, implement NullMapping/NullPose, prototype grid mapping plugin with replay fixtures, extend plugin loader for capability-gated presets.【F:docs/ADR/ADR-029-mapping-plugin-architecture.md†L86-L93】
- **Acceptance hooks:** Deterministic replay fixtures covering Pose→RateHint/SectionMask flows, capability registry integration tests, restart/isolation fault-injection scenarios.

### ADR-007 — PoseStream & SectionState architecture
- **Scope:** A single, time-ordered PoseStream spanning tractor, implement, toolbar, and section poses with diffed SectionState updates, unified vector logs, and event/opportunity metrics.
- **Key decisions:** Cadence/decimation policy, one pose timeline for all layers, SectionState diff rules, optional per-layer micro-streams when plugin cadence diverges, replay determinism budgets, and how to compute opportunity vs. event tallies.
- **SRS alignment:** Communications (§03), Data Model (§08), Control & Automation (§09), Extensibility (§12).【F:docs/SRS/sections/03_Comm_Transports.md†L6-L28】【F:docs/SRS/sections/08_Data_Model_Storage.md†L6-L31】【F:docs/SRS/sections/09_Control_Automation.md†L8-L22】【F:docs/SRS/sections/12_Extensibility_Plugins.md†L6-L28】
- **Primary requirements:** R-COMM-010, R-COMM-011, R-COMM-020; R-DATA-015; R-CTRL-000…R-CTRL-002; R-EXT-120.【F:docs/SRS/sections/03_Comm_Transports.md†L11-L28】【F:docs/SRS/sections/08_Data_Model_Storage.md†L17-L31】【F:docs/SRS/sections/09_Control_Automation.md†L16-L22】【F:docs/SRS/sections/12_Extensibility_Plugins.md†L21-L28】
- **Tasks:** PoseStreamService, SectionStateManager, event/opportunity schema definitions, shared proto/schema updates, replay fixtures.
- **Acceptance hooks:** Determinism fixture (TileStore hash matches), pose-ingest CPU budget (<Z% on reference hardware), ISOXML round-trip tolerance.

### ADR-008 — Equipment → Implement → Toolbar → Section(+Group) hierarchy
- **Scope:** Canonical object model with multiple toolbars per implement, overlapping SectionGroups, master groups, and per-toolbar lookahead/overlap metadata with kinematic placeholders for ADR-017.
- **Key decisions:** Stable IDs, offsets, widths, grouping semantics, overlapping SectionGroup arbitration examples, and which nodes/groups accept control targets prior to kinematic link integration.
- **SRS alignment:** Interprocess API (§07) for schema governance and Control (§09) for group arbitration.【F:docs/SRS/sections/07_Interprocess_API.md†L6-L28】【F:docs/SRS/sections/09_Control_Automation.md†L16-L22】
- **Primary requirements:** R-GEO-000…R-GEO-002; R-CTRL-000…R-CTRL-002.【F:docs/SRS/sections/07_Interprocess_API.md†L16-L28】【F:docs/SRS/sections/09_Control_Automation.md†L16-L22】
- **Tasks:** Data model updates, configuration editor, overlapping group/master arbitration examples, kinematic placeholder registry, migration shims for legacy implements.
- **Acceptance hooks:** Determinism fixture (TileStore hash matches), pose-ingest CPU budget (<Z% on reference hardware), ISOXML round-trip tolerance.

### ADR-009 — Persistence & storage: Vector PoseStream + Layer TileStore
- **Scope:** Authoritative PoseStream/SectionState vector logs and chunked tile store with value/weight/min/max channels, deterministic transforms between the two, and support for deployments that retain either (or both).
- **Key decisions:** Tile/cell sizing, codec selection (LZ4/Zstd), crash safety guarantees, registry hash flow, append/compaction strategies, and vector↔tile determinism.
- **SRS alignment:** Data Model & Storage (§08).【F:docs/SRS/sections/08_Data_Model_Storage.md†L6-L31】
- **Primary requirements:** R-DATA-015…R-DATA-025.【F:docs/SRS/sections/08_Data_Model_Storage.md†L17-L31】
- **Tasks:** TileStore service, index format, compactor, vector↔tile transform validation, read/write tests.
- **Acceptance hooks:** Determinism fixture (TileStore hash matches), pose-ingest CPU budget (<Z% on reference hardware), ISOXML round-trip tolerance.

### ADR-010 — Layer registry & variable-rate framework
- **Scope:** LayerDefinition schema, units, normalization, color ramps, aggregation modes, discovery/versioning; replaces legacy ADR-005.
- **Key decisions:** IDs, schema hashes, numeric precision, display metadata, registry publishing cadence, and compatibility with plugin manifests.
- **SRS alignment:** Data Model (§08) and Extensibility (§12).【F:docs/SRS/sections/08_Data_Model_Storage.md†L6-L31】【F:docs/SRS/sections/12_Extensibility_Plugins.md†L6-L28】
- **Primary requirements:** R-DATA-010…R-DATA-014; R-EXT-010; R-EXT-120.【F:docs/SRS/sections/08_Data_Model_Storage.md†L11-L21】【F:docs/SRS/sections/12_Extensibility_Plugins.md†L11-L28】
- **Tasks:** Registry service, validators, units library integration, manifest tooling.
- **Acceptance hooks:** Determinism fixture (TileStore hash matches), pose-ingest CPU budget (<Z% on reference hardware), ISOXML round-trip tolerance.

### ADR-011 — Mapping & visualization: Basemaps & imagery pipeline
- **Scope:** Transform PoseStream samples into ribbons, heatmaps, contours, and legends with bilinear sampling, near-vehicle supersampling, basemap caching, attribution, and offline fallbacks.
- **Key decisions:** Render order, GPU texture strategy, interpolation rules, fresh-pass emphasis, ribbon overlay policy, basemap tile caching (disk LRU + offline fallback), and attribution requirements.
- **SRS alignment:** Telemetry & Health (§10) and Frontend rendering surfaces (§05).【F:docs/SRS/sections/10_Telemetry_Health.md†L6-L47】【F:docs/SRS/sections/05_Frontends.md†L6-L33】
- **Primary requirements:** R-TH-000…R-TH-021; R-FE-004…R-FE-032.【F:docs/SRS/sections/10_Telemetry_Health.md†L7-L21】【F:docs/SRS/sections/05_Frontends.md†L13-L23】
- **Tasks:** Renderer refactor, shared color-ramp utility, basemap cache manager, legend/overlay widgets.
- **Acceptance hooks:** Determinism fixture (TileStore hash matches), pose-ingest CPU budget (<Z% on reference hardware), ISOXML round-trip tolerance.

### ADR-012 — Multi-session & multi-PoseStream fusion
- **Scope:** Merge rules for multiple PoseStreams within a session and across seasons (planter + sprayer, prior-year yield to current prescriptions) including CRS reprojection steps.
- **Key decisions:** Time/space alignment, CRS reprojection (ADR-022), dedupe/priority rules, authority hierarchy (prescription beats historical average unless quality < threshold), area-weighted merges, provenance chain handling.
- **SRS alignment:** Data Model & Storage (§08) and Control & Automation (§09).【F:docs/SRS/sections/08_Data_Model_Storage.md†L17-L31】【F:docs/SRS/sections/09_Control_Automation.md†L16-L22】
- **Primary requirements:** R-DATA-017…R-DATA-022; R-CTRL-001.【F:docs/SRS/sections/08_Data_Model_Storage.md†L19-L26】【F:docs/SRS/sections/09_Control_Automation.md†L18-L18】
- **Tasks:** Fusion service, provenance tracker, priority/arbitration policies, CRS reprojection utilities.
- **Acceptance hooks:** Determinism fixture (TileStore hash matches), pose-ingest CPU budget (<Z% on reference hardware), ISOXML round-trip tolerance.

### ADR-013 — Derived products (analytics → prescriptions)
- **Scope:** Convert yield/soil/NDVI PoseStreams and layers into prescriptions with gridding, smoothing, ROI masking, banding/clamping, reproducible recipes, and QA metrics.
- **Key decisions:** Target functions, QA metrics, parameterizable recipes with parameter hashes, output validation loops, QA report content (coverage, variance, RMSE vs. target).
- **SRS alignment:** Testing & Analytics governance (§11) and Data Model (§08).【F:docs/SRS/sections/11_Testing_CI_CDPipelines.md†L6-L19】【F:docs/SRS/sections/08_Data_Model_Storage.md†L17-L31】
- **Primary requirements:** R-CI-021 plus R-CI-010, R-CI-020, R-CI-030; R-DATA-017…R-DATA-022.【F:docs/SRS/sections/11_Testing_CI_CDPipelines.md†L11-L19】【F:docs/SRS/sections/08_Data_Model_Storage.md†L19-L26】
- **Tasks:** Derivation pipeline, configurable recipe format (YAML/JSON), QA report generator, regression fixtures.
- **Acceptance hooks:** Determinism fixture (TileStore hash matches), pose-ingest CPU budget (<Z% on reference hardware), ISOXML round-trip tolerance.

### ADR-014 — Interop: prescription & agronomic formats
- **Scope:** Import/export ISOXML TaskData, GeoTIFF/COG rasters, Shapefile/GeoPackage vectors, MBTiles tilesets with consistent units/CRS and attribute mapping.
- **Key decisions:** Terminology (use “Prescription” for VR drivers), canonical internal units, CRS policy (ADR-022), prefer ISOXML TaskData for vector prescriptions, COG GeoTIFF for raster, GeoPackage as vector fallback, attribute naming, file naming conventions.
- **SRS alignment:** Communications (§03 interop notes) and Data Model (§08 exports).【F:docs/SRS/sections/03_Comm_Transports.md†L6-L28】【F:docs/SRS/sections/08_Data_Model_Storage.md†L6-L26】
- **Primary requirements:** R-DATA-003, R-DATA-014, R-DATA-018; R-COMM-005; R-DATA-019…R-DATA-022.【F:docs/SRS/sections/08_Data_Model_Storage.md†L10-L26】【F:docs/SRS/sections/03_Comm_Transports.md†L13-L28】
- **Tasks:** Importers/exporters, unit normalization, CRS audit logging, sample fixtures.
- **Acceptance hooks:** Determinism fixture (TileStore hash matches), pose-ingest CPU budget (<Z% on reference hardware), ISOXML round-trip tolerance.

### ADR-015 — Section control & grouping semantics
- **Scope:** Control graph for on/auto/off states, master group actions, overlapping groups, per-toolbar lookahead/overlap with overrides, and plugin hooks aligned with ADR-018.
- **Key decisions:** Arbitration priorities (manual > plugin > auto), toolbar-level lookahead/minimum overlap defaults with group/section overrides, safety interlocks, plugin extension hooks.
- **SRS alignment:** Control semantics captured in §09 and plugin integration in §12.【F:docs/SRS/sections/09_Control_Automation.md†L16-L22】【F:docs/SRS/sections/12_Extensibility_Plugins.md†L6-L28】
- **Primary requirements:** R-CTRL-000…R-CTRL-005; R-EXT-120; R-EXT-134.【F:docs/SRS/sections/09_Control_Automation.md†L16-L22】【F:docs/SRS/sections/12_Extensibility_Plugins.md†L21-L28】
- **Tasks:** Control engine updates, configuration UI, arbitration policy documentation, simulation/replay tests.
- **Acceptance hooks:** Determinism fixture (TileStore hash matches), pose-ingest CPU budget (<Z% on reference hardware), ISOXML round-trip tolerance.

### ADR-016 — Firmware/transport: variable-rate & layer PGNs
- **Scope:** CAN/UDP message suite for layer definitions and feedback (E2/E1/E0/DF/E3/E4), node IDs, sequencing/timing, registry hash handshake, and timeout/heartbeat semantics.
- **Key decisions:** Payload packing, version bits, registry hash handshake (ADR-010), timeout/heartbeat strategy, compatibility with existing PGNs, degraded-mode behavior.
- **SRS alignment:** Communications & Transports (§03) and Hardware I/O (§06).【F:docs/SRS/sections/03_Comm_Transports.md†L6-L28】【F:docs/SRS/sections/06_Hardware_IO.md†L6-L34】
- **Primary requirements:** R-COMM-010…R-COMM-032; R-HW-010…R-HW-023.【F:docs/SRS/sections/03_Comm_Transports.md†L11-L28】【F:docs/SRS/sections/06_Hardware_IO.md†L9-L34】
- **Tasks:** AgIO bridge updates, firmware stubs, simulators, conformance tests, degraded-mode heartbeat handling.
- **Acceptance hooks:** Determinism fixture (TileStore hash matches), pose-ingest CPU budget (<Z% on reference hardware), ISOXML round-trip tolerance.

### ADR-017 — Profiles & kinematics (hitches, pivot tongues, multi-steer)
- **Scope:** Tractor/implement profiles, hitch linkage models, IMU/GNSS fusion, and which pose is “steered” for autosteer and mapping.
- **Key decisions:** Kinematic models, attachment points, toolbar placement priority, fusion of multiple pose sources, integration with ADR-008 placeholders.
- **SRS alignment:** Interprocess API (§07 geometry metadata) and Control (§09 lookahead).【F:docs/SRS/sections/07_Interprocess_API.md†L16-L28】【F:docs/SRS/sections/09_Control_Automation.md†L16-L22】
- **Primary requirements:** R-GEO-001…R-GEO-002; R-CTRL-002.【F:docs/SRS/sections/07_Interprocess_API.md†L17-L18】【F:docs/SRS/sections/09_Control_Automation.md†L18-L18】
- **Tasks:** Kinematics library, profile editor, sensor fusion hooks, validation fixtures.
- **Acceptance hooks:** Determinism fixture (TileStore hash matches), pose-ingest CPU budget (<Z% on reference hardware), ISOXML round-trip tolerance.

### ADR-018 — Plugin API & capability discovery
- **Scope:** Contracts for publishing/consuming PoseStream deltas, declaring layers, control endpoints, hardware bindings, permissions, lifecycle governance, and UI contributions between Core, plugins, frontends, and AgIO.
- **Key decisions:** Manifest schema, permission model, lifecycle events, hot-plug strategy, capability discovery handshake, UI contribution model, AgIO as privileged plugin.
- **SRS alignment:** Extensibility & Plugins (§12), Communications (§03 plugin transport), Frontends (§05 UI contributions), Hardware I/O (§06 AgIO plugin), Control (§09 automation lifecycle).【F:docs/SRS/sections/12_Extensibility_Plugins.md†L6-L28】【F:docs/SRS/sections/03_Comm_Transports.md†L20-L28】【F:docs/SRS/sections/05_Frontends.md†L20-L23】【F:docs/SRS/sections/06_Hardware_IO.md†L23-L34】【F:docs/SRS/sections/09_Control_Automation.md†L16-L22】
- **Primary requirements:** R-EXT-000…R-EXT-134; R-COMM-030…R-COMM-032; R-FE-030…R-FE-032; R-HW-020…R-HW-023; R-CTRL-003…R-CTRL-005.【F:docs/SRS/sections/12_Extensibility_Plugins.md†L6-L28】【F:docs/SRS/sections/03_Comm_Transports.md†L20-L28】【F:docs/SRS/sections/05_Frontends.md†L20-L23】【F:docs/SRS/sections/06_Hardware_IO.md†L23-L34】【F:docs/SRS/sections/09_Control_Automation.md†L20-L22】
- **Tasks:** SDK, sample plugins (planter monitor, rate controller, autosteer), manifest tooling, permission gate, UI contribution host, health/lease services.
- **Acceptance hooks:** Determinism fixture (TileStore hash matches), pose-ingest CPU budget (<Z% on reference hardware), ISOXML round-trip tolerance.

### ADR-019 — Provenance, audit, and QA
- **Scope:** Provenance schema, job/session IDs, dataset hashes, quality flags, audit trail expectations across storage, UI, and exports.
- **Key decisions:** Provenance registry contents, chain-of-custody rules, QA badge surfacing, CI hash checks, linkage with ADR-023 sessions.
- **SRS alignment:** Telemetry & Health (§10) and Testing/CI (§11).【F:docs/SRS/sections/10_Telemetry_Health.md†L6-L21】【F:docs/SRS/sections/11_Testing_CI_CDPipelines.md†L6-L19】
- **Primary requirements:** R-TH-010…R-TH-012; R-CI-020…R-CI-030; R-DATA-018…R-DATA-025.【F:docs/SRS/sections/10_Telemetry_Health.md†L12-L16】【F:docs/SRS/sections/11_Testing_CI_CDPipelines.md†L17-L19】【F:docs/SRS/sections/08_Data_Model_Storage.md†L20-L31】
- **Tasks:** Provenance writer, hash checks in CI, UI badges, audit log pipeline.
- **Acceptance hooks:** Determinism fixture (TileStore hash matches), pose-ingest CPU budget (<Z% on reference hardware), ISOXML round-trip tolerance.

### ADR-020 — Determinism, replay & CI
- **Scope:** Golden replays from PoseStream to identical tiles/exports with defined performance and size budgets.
- **Key decisions:** Hashing scheme, fixture format, pass/fail criteria, CI integration strategy, integration with ADR-026 performance budgets.
- **SRS alignment:** Testing & CI (§11) and Data Model (§08).【F:docs/SRS/sections/11_Testing_CI_CDPipelines.md†L6-L19】【F:docs/SRS/sections/08_Data_Model_Storage.md†L17-L31】
- **Primary requirements:** R-CI-010, R-CI-013, R-CI-030; R-DATA-015; R-DATA-025.【F:docs/SRS/sections/11_Testing_CI_CDPipelines.md†L11-L19】【F:docs/SRS/sections/08_Data_Model_Storage.md†L17-L31】
- **Tasks:** Replay tool, golden datasets, performance gates, hash verification tooling.
- **Acceptance hooks:** Determinism fixture (TileStore hash matches), pose-ingest CPU budget (<Z% on reference hardware), ISOXML round-trip tolerance.

### ADR-021 — Timebase & clock sync
- **Scope:** Define the canonical clock for PoseStream sequencing, GPS vs. system vs. PTP/RTK epochs, drift handling, sequence numbers, per-node latency budgets, and firmware timestamp reconciliation.
- **Key decisions:** Time authority selection, drift detection/compensation, sequence numbering, latency budget enforcement, diagnostics exposure.
- **SRS alignment:** Communications (§03 timebase), Control (§09 automation timing), Extensibility (§12 lifecycle metrics).【F:docs/SRS/sections/03_Comm_Transports.md†L20-L28】【F:docs/SRS/sections/09_Control_Automation.md†L16-L22】【F:docs/SRS/sections/12_Extensibility_Plugins.md†L23-L28】
- **Primary requirements:** R-COMM-020, R-COMM-040…R-COMM-042; R-CTRL-003; R-EXT-134.【F:docs/SRS/sections/03_Comm_Transports.md†L17-L28】【F:docs/SRS/sections/09_Control_Automation.md†L20-L22】【F:docs/SRS/sections/12_Extensibility_Plugins.md†L24-L28】
- **Tasks:** Timebase service, drift monitors, sequence enforcement in PoseStream, latency budget CI checks.
- **Acceptance hooks:** Determinism fixture (TileStore hash matches), pose-ingest CPU budget (<Z% on reference hardware), ISOXML round-trip tolerance.

### ADR-022 — CRS/units & precision policy
- **Scope:** Establish project CRS defaults (WGS84 vs. per-field projections), on-disk numeric types, canonical units, and reprojection/rounding rules for imports/exports.
- **Key decisions:** Default CRS selection rules, per-field projection switching, numeric precision tiers (f16/f32/u16), rounding policy, unit normalization, reprojection audit logging.
- **SRS alignment:** Data Model & Storage (§08) and Interop (§03).【F:docs/SRS/sections/08_Data_Model_Storage.md†L22-L31】【F:docs/SRS/sections/03_Comm_Transports.md†L6-L28】
- **Primary requirements:** R-DATA-019…R-DATA-022; R-DATA-014; R-COMM-021.【F:docs/SRS/sections/08_Data_Model_Storage.md†L22-L26】【F:docs/SRS/sections/03_Comm_Transports.md†L17-L28】
- **Tasks:** CRS/units policy doc, conversion utilities, reprojection audit hooks, unit registry integration tests.
- **Acceptance hooks:** Determinism fixture (TileStore hash matches), pose-ingest CPU budget (<Z% on reference hardware), ISOXML round-trip tolerance.

### ADR-023 — Session/job model & provenance graph
- **Scope:** Define sessions, jobs, equipment/implement selection, profile snapshotting, and how multiple PoseStreams and layers attach to a session for provenance (e.g., combine yield → fertilizer VR).
- **Key decisions:** Session lifecycle, job identifiers, profile snapshot storage, PoseStream attachment rules, provenance graph schema linking ADR-019 and ADR-013 outputs.
- **SRS alignment:** Backend Services (§04), Data Model (§08 lifecycle), Control (§09 automation states).【F:docs/SRS/sections/04_Backend_Services.md†L6-L28】【F:docs/SRS/sections/08_Data_Model_Storage.md†L28-L31】【F:docs/SRS/sections/09_Control_Automation.md†L16-L22】
- **Primary requirements:** R-BE-000…R-BE-014; R-DATA-017…R-DATA-025; R-CTRL-003.【F:docs/SRS/sections/04_Backend_Services.md†L6-L28】【F:docs/SRS/sections/08_Data_Model_Storage.md†L19-L31】【F:docs/SRS/sections/09_Control_Automation.md†L20-L22】
- **Tasks:** Session service, provenance graph schema, equipment/profile snapshot handling, UI wiring.
- **Acceptance hooks:** Determinism fixture (TileStore hash matches), pose-ingest CPU budget (<Z% on reference hardware), ISOXML round-trip tolerance.

### ADR-024 — Discovery & identity
- **Scope:** Node/rig IDs, capability handshake (ties to ADR-018), multi-controller scenarios (multiple ESP32s), user-visible naming, and permissioned discovery across transports.
- **Key decisions:** Identity schema, discovery watchers, capability handshakes, lease renewals, naming conventions, security model (mTLS/policy).
- **SRS alignment:** Communications (§03 plugin transport), Hardware I/O (§06 AgIO plugin), Extensibility (§12 lifecycle & security).【F:docs/SRS/sections/03_Comm_Transports.md†L20-L28】【F:docs/SRS/sections/06_Hardware_IO.md†L23-L34】【F:docs/SRS/sections/12_Extensibility_Plugins.md†L23-L28】
- **Primary requirements:** R-COMM-030…R-COMM-032; R-HW-020…R-HW-023; R-EXT-130…R-EXT-134.【F:docs/SRS/sections/03_Comm_Transports.md†L20-L28】【F:docs/SRS/sections/06_Hardware_IO.md†L23-L34】【F:docs/SRS/sections/12_Extensibility_Plugins.md†L23-L28】
- **Tasks:** Capabilities registry, discovery watcher, lease/heartbeat services, operator-facing identity management UI.
- **Acceptance hooks:** Determinism fixture (TileStore hash matches), pose-ingest CPU budget (<Z% on reference hardware), ISOXML round-trip tolerance.

### ADR-025 — Data lifecycle & retention
- **Scope:** On-device vs. removable storage policies, compaction triggers, background re-encode workflows (LZ4→Zstd), export/archive rules, privacy flags, and retention SLAs.
- **Key decisions:** Retention windows, compaction cadence, archival/export flows, privacy tagging, storage budgeting per layer.
- **SRS alignment:** Data Model (§08 lifecycle), Backend Services (§04 health budgets), Telemetry (§10 retention).【F:docs/SRS/sections/08_Data_Model_Storage.md†L28-L31】【F:docs/SRS/sections/04_Backend_Services.md†L13-L20】【F:docs/SRS/sections/10_Telemetry_Health.md†L12-L16】
- **Primary requirements:** R-DATA-013, R-DATA-023…R-DATA-025; R-BE-013; R-TH-012.【F:docs/SRS/sections/08_Data_Model_Storage.md†L15-L31】【F:docs/SRS/sections/04_Backend_Services.md†L13-L20】【F:docs/SRS/sections/10_Telemetry_Health.md†L12-L16】
- **Tasks:** Retention planner, compaction scheduler, background maintenance jobs, privacy flag propagation, export/archive tooling.
- **Acceptance hooks:** Determinism fixture (TileStore hash matches), pose-ingest CPU budget (<Z% on reference hardware), ISOXML round-trip tolerance.

### ADR-026 — Performance budgets
- **Scope:** CPU/IO targets (pose ingest < X ms, tile flush < Y ms), file size budgets (MB/acre/layer), renderer FPS guarantees with N active layers, and CI guardrails tying to ADR-020 replays.
- **Key decisions:** Reference hardware targets, instrumentation strategy, alert thresholds, integration with CI and telemetry dashboards, cross-component budgeting (Core, UI, plugins).
- **SRS alignment:** Communications (§03 latency), Backend Services (§04 health metrics), Telemetry (§10 observability), Data Model (§08 maintenance).【F:docs/SRS/sections/03_Comm_Transports.md†L12-L28】【F:docs/SRS/sections/04_Backend_Services.md†L13-L20】【F:docs/SRS/sections/10_Telemetry_Health.md†L12-L16】【F:docs/SRS/sections/08_Data_Model_Storage.md†L28-L31】
- **Primary requirements:** R-COMM-012, R-COMM-042; R-BE-013; R-DATA-024; R-TH-010…R-TH-012.【F:docs/SRS/sections/03_Comm_Transports.md†L15-L28】【F:docs/SRS/sections/04_Backend_Services.md†L13-L20】【F:docs/SRS/sections/08_Data_Model_Storage.md†L29-L31】【F:docs/SRS/sections/10_Telemetry_Health.md†L12-L16】
- **Tasks:** Performance budget doc, instrumentation hooks, CI alerts, telemetry dashboards, regression fixtures.
- **Acceptance hooks:** Determinism fixture (TileStore hash matches), pose-ingest CPU budget (<Z% on reference hardware), ISOXML round-trip tolerance.
## How to use this tracker
- **Before drafting an ADR**, confirm the associated requirements are satisfied or add missing ones to the SRS within the relevant section (Sections 03, 07–12 already include new requirement IDs for this program).【F:docs/SRS/sections/03_Comm_Transports.md†L6-L28】【F:docs/SRS/sections/07_Interprocess_API.md†L6-L28】【F:docs/SRS/sections/08_Data_Model_Storage.md†L6-L31】【F:docs/SRS/sections/09_Control_Automation.md†L8-L22】【F:docs/SRS/sections/10_Telemetry_Health.md†L6-L17】【F:docs/SRS/sections/11_Testing_CI_CDPipelines.md†L6-L19】【F:docs/SRS/sections/12_Extensibility_Plugins.md†L6-L28】
- **During implementation**, link work items to NX-150 and update this file with progress notes or additional prerequisites discovered by prototypes or field feedback.
- **When an ADR is approved**, move it to the adopted table above and ensure the SRS section references are updated to point to the final record.
