# ADR Roadmap: PoseStream, Layer, and Control Program

This tracker consolidates current and planned Architecture Decision Records so the Nexus team can stage the PoseStream, section control, and variable-rate overhaul in one place. It links each ADR to the SRS requirements that must be satisfied before drafting or promoting the decision for review.【F:tasks.md†L131-L137】 Use it to coordinate sequencing, ensure prerequisite requirements are in place, and keep the community focused on the same backlog of decisions.

## Governance Updates
- **Living program board.** The roadmap now syncs with the architecture kanban each Monday,
  capturing burndown, dependency risk, and staffing flags. Updates generate summaries in
  [`tasks.md`](../../tasks.md) so execution plans stay aligned.
- **Dependency telemetry.** Automated scripts highlight ADRs blocked on missing registries or tooling, prompting owners to file NX follow-ups before deadlines slip.
- **Communication cadence.** Monthly review meetings publish minutes and action items linked from this roadmap, keeping community contributors informed about sequencing changes.
- **Telemetry artefacts.** `tools/scripts/generate-governance-telemetry.py` aggregates the program board, dependency digests, and published minutes into machine-readable telemetry under `artifacts/governance/`.

## Adopted decisions (foundation)

| ADR | Title | Status | Key SRS coverage | Requirement anchors |
| --- | --- | --- | --- | --- |
| [ADR-001](ADR-001-dotnet8-runtime.md) | Adopt .NET 8 C# stack for Nexus runtime | Accepted | SRS §01 OS Support, §02 UI Framework | R-OS-000…R-OS-006, R-UI-000…R-UI-007【F:docs/ADR/ADR-001-dotnet8-runtime.md†L1-L24】【F:SRS/Sections/1X_Platform_Foundations/11_OS_Support.md†L1-L44】【F:SRS/Sections/1X_Platform_Foundations/13_UI_Framework_UX.md†L1-L48】 |
| [ADR-002](ADR-002-grpc-contracts.md) | Expose Nexus services over gRPC/protobuf contracts | Accepted | SRS §03 Communications & Transports, §07 Interprocess API | R-COMM-000…R-COMM-013, R-API-000…R-API-012【F:docs/ADR/ADR-002-grpc-contracts.md†L1-L28】【F:docs/SRS/sections/4X_Interprocess_Communications/42_Transports.md†L1-L27】【F:docs/SRS/sections/4X_Interprocess_Communications/41_Inter_Application_API.md†L1-L18】 |
| [ADR-003](ADR-003-avalonia-ui.md) | Use Avalonia for the cross-platform Nexus UI shell | Accepted | SRS §02 UI Framework, §05 Frontends | R-UI-000…R-UI-007, R-FE-000…R-FE-014【F:docs/ADR/ADR-003-avalonia-ui.md†L1-L22】【F:SRS/Sections/1X_Platform_Foundations/13_UI_Framework_UX.md†L1-L48】【F:docs/SRS/sections/9X_Frontends_Ops/91_UI_Shell_Layout.md†L1-L120】 |
| [ADR-004](ADR-004-composite-simulation.md) | Establish the composite simulation fabric (SimClock + SimBus) | Accepted | SRS §04 Backend Services, §12 Extensibility & Plugins | R-BE-000…R-BE-014, R-EXT-030…R-EXT-103【F:docs/ADR/ADR-004-composite-simulation.md†L1-L20】【F:docs/SRS/sections/2X_System_Architecture/21_System_Decomposition_Boundaries.md†L1-L120】【F:docs/SRS/sections/9X_Frontends_Ops/94_Extensibility_Packaging_Updates.md†L51-L74】 |
| [ADR-006](ADR-006-aog-link-mcu-communications.md) | MCU communications over AOG-Link (nanopb) | Accepted | SRS §03 Communications & Transports, §06 Hardware I/O | R-COMM-000…R-COMM-013, R-HW-000…R-HW-014【F:docs/ADR/ADR-006-aog-link-mcu-communications.md†L1-L55】【F:docs/SRS/sections/4X_Interprocess_Communications/42_Transports.md†L1-L45】【F:docs/SRS/sections/5X_Hardware_IO_Device_Layer/51_Sensor_Actuator_Abstractions.md†L1-L38】 |
| [ADR-028](ADR-028-stack-boundaries.md) | Nexus stack responsibilities & handoff boundaries | Accepted | SRS §03 Communications & Transports, §06 Hardware I/O, §12 Extensibility & Plugins | R-COMM-000…R-COMM-032, R-HW-000…R-HW-027, R-EXT-000…R-EXT-134【F:docs/ADR/ADR-028-stack-boundaries.md†L1-L118】【F:docs/SRS/sections/4X_Interprocess_Communications/42_Transports.md†L1-L45】【F:docs/SRS/sections/5X_Hardware_IO_Device_Layer/51_Sensor_Actuator_Abstractions.md†L1-L54】【F:docs/SRS/sections/9X_Frontends_Ops/94_Extensibility_Packaging_Updates.md†L1-L80】 |
| [ADR-029](ADR-029-mapping-plugin-architecture.md) | Mapping plugin architecture & geospatial kernel split | Accepted | SRS §03 Communications, §04 Backend Services, §08 Data Model, §12 Extensibility | R-COMM-020…R-COMM-042, R-BE-000…R-BE-014, R-DATA-010…R-DATA-025, R-EXT-010…R-EXT-134【F:docs/ADR/ADR-029-mapping-plugin-architecture.md†L14-L93】【F:docs/SRS/sections/4X_Interprocess_Communications/42_Transports.md†L6-L28】【F:docs/SRS/sections/2X_System_Architecture/21_System_Decomposition_Boundaries.md†L6-L20】【F:docs/SRS/sections/3X_Data_Storage/32_Persistence_Formats.md†L10-L33】【F:docs/SRS/sections/9X_Frontends_Ops/94_Extensibility_Packaging_Updates.md†L6-L80】 |
| [ADR-027](ADR-027-spatial-constraints.md) | Spatial constraints & zone policies | Accepted | SRS §03 Communications, §08 Data Model, §09 Control, §10 Telemetry | R-COMM-020…R-COMM-023, R-DATA-026…R-DATA-028, R-CTRL-000…R-CTRL-007【F:docs/ADR/ADR-027-spatial-constraints.md†L11-L64】【F:docs/SRS/sections/3X_Data_Storage/32_Persistence_Formats.md†L21-L27】【F:docs/SRS/sections/6X_Core_Domain_Services/61_Kinematics_Pose_Fusion.md†L37-L60】 |
| [ADR-040](ADR-040_SeasonOrganizers.md) | Season organizers & context publication | Accepted | SRS §02 Data Model, §03 Job Lifecycle, §04 Backend Services | Season catalog & context publish requirements【F:docs/ADR/ADR-040_SeasonOrganizers.md†L9-L74】【F:docs/SRS/sections/3X_Data_Storage/31_Domain_Data_Model.md†L1-L140】【F:docs/SRS/sections/6X_Core_Domain_Services/62_Job_Lifecycle.md†L1-L64】【F:docs/SRS/sections/2X_System_Architecture/21_System_Decomposition_Boundaries.md†L6-L27】 |
| [ADR-041](ADR-041_JobSessions.md) | Job session lifecycle orchestration | Accepted | SRS §02 Data Model, §03 Job Lifecycle, §04 Backend Services | Session schema, autosave, and lifecycle hooks【F:docs/ADR/ADR-041_JobSessions.md†L9-L148】【F:docs/SRS/sections/6X_Core_Domain_Services/62_Job_Lifecycle.md†L1-L120】【F:docs/SRS/sections/2X_System_Architecture/21_System_Decomposition_Boundaries.md†L6-L40】 |
| [ADR-043](ADR-043_MultiFieldJobEnvelopes.md) | Multi-field job envelopes | Accepted | SRS §02 Data Model, §03 Job Lifecycle, §04 Mapping & Layers | Multi-field mount & analytics requirements【F:docs/ADR/ADR-043_MultiFieldJobEnvelopes.md†L9-L75】【F:docs/SRS/sections/3X_Data_Storage/31_Domain_Data_Model.md†L22-L140】【F:docs/SRS/sections/6X_Core_Domain_Services/62_Job_Lifecycle.md†L59-L112】【F:docs/SRS/sections/7X_Mapping_Geospatial/72_Mapping_Layers_Plugin.md†L1-L44】 |
| [ADR-067](ADR-067-equipment-configuration-kinematics.md) | Axle-centric equipment configuration runtime | Accepted | SRS §06 Hardware I/O, §09 Control & Automation, §10 Telemetry | Multi-steer configurator export & ingestion requirements【F:docs/ADR/ADR-067-equipment-configuration-kinematics.md†L9-L123】【F:docs/SRS/options/5X/O-HW-7_MultiSteerConfigurator.md†L19-L533】【F:docs/SRS/sections/6X_Core_Domain_Services/61_Kinematics_Pose_Fusion.md†L12-L60】 |
| [ADR-068](ADR-068-layer-controllers-runtime.md) | Layer controllers & aggregation runtime | Accepted | SRS §04 Backend Services, §08 Data Model, §10 Telemetry | Layer aggregation, diagnostics, and replay requirements【F:docs/ADR/ADR-068-layer-controllers-runtime.md†L9-L118】【F:docs/SRS/sections/2X_System_Architecture/21_System_Decomposition_Boundaries.md†L6-L35】【F:docs/SRS/sections/3X_Data_Storage/32_Persistence_Formats.md†L10-L33】【F:docs/SRS/sections/6X_Core_Domain_Services/64_Telemetry_Health.md†L6-L41】 |
| [ADR-032](ADR-032-presets-and-layout-linking.md) | Presets and layout linking for equipment workflows | Accepted | SRS §02 Documentation, §03 Job Lifecycle, §05 Frontends | Preset orchestration, layout versioning, and task telemetry requirements【F:docs/ADR/ADR-032-presets-and-layout-linking.md†L14-L108】【F:docs/ADR/ADR-030-field-job-sessions.md†L20-L96】【F:docs/SRS/sections/9X_Frontends_Ops/91_UI_Shell_Layout.md†L22-L88】 |
| [ADR-00XX](ADR-00XX-shm-fastpath-pumpkin-pi.md) | CM5 SHM fastpath + HAL plugin (“Pumpkin Pi”) | Accepted | SRS §03A AOG-Link v1, §06 Hardware I/O, §08A CM5 Integrated Controller | Fast-path HAL, authority tokens, and bridge coexistence for CM5 integrated mode【F:docs/ADR/ADR-00XX-shm-fastpath-pumpkin-pi.md†L1-L76】【F:docs/SRS/sections/5X_Hardware_IO_Device_Layer/53_AOG_Link_Compatibility.md†L287-L339】【F:docs/SRS/sections/5X_Hardware_IO_Device_Layer/51_Sensor_Actuator_Abstractions.md†L6-L27】【F:docs/SRS/sections/5X_Hardware_IO_Device_Layer/54_CM5_Integrated_Controller.md†L3-L78】 |

## Active proposals & in-flight drafts

Draft authors should reference the listed requirements and tasks before opening a proposal so prerequisite SRS coverage is already in place.

### Early unblockers — High fan-in contracts
- **ADR-010 Layer registry (skeleton)** — Ship a minimal `LayerDefinition` schema draft with hash handshake semantics so mapping, presets, and analytics workstreams can start API reviews before the full catalog governance ADR lands.
- **ADR-022 CRS policy (preview)** — [CRS normalization matrix](../reference/crs-normalization-matrix.md)
  plus reprojection rules referenced by ADR-027/ADR-029 to unblock cross-pod alignment on
  spatial math and deterministic replay.
- **ADR-031 Manifest governance (CI stub)** — Land the manifest validator CLI plus CI gate to let plugin teams iterate against a concrete toolchain while the broader governance ADR proceeds through review.

### ADR-054 — Nexus CLI host & plugin verbs
- **Owner:** Packaging & DevEx pod
- **Stage:** Drafting (target review window: 2025-03-14 week)
- **Dependencies:** ADR-001 (.NET 8 runtime), ADR-028 (stack boundaries), ADR-031 (manifest governance), SRS §18 CLI requirements
- **Scope:** Deliver the unified `nx` CLI host with offline filesystem workflows, Core transport negotiation (pipes/UDS/TLS), plugin adapter loading, and structured outputs so operators and automation scripts share one toolchain.【F:docs/ADR/ADR-054_NexusCliHost.md†L16-L56】【F:docs/SRS/sections/9X_Frontends_Ops/93_Command_Line_Interface.md†L1-L66】
- **Key decisions:** Endpoint resolver ordering and overrides, adapter vs. reflection loading contract, packaging targets (dotnet tool + single-file), JSON/NDJSON schema governance, auth token handling, and compatibility checks between CLI, Core, and plugin verbs.【F:docs/ADR/ADR-054_NexusCliHost.md†L20-L56】【F:docs/SRS/sections/9X_Frontends_Ops/93_Command_Line_Interface.md†L19-L48】
- **SRS alignment:** CLI Section (§18) plus dependencies on Communications (§03 transports), Interprocess API (§07 versioning), and Plugin Packaging (§16 manifests).【F:docs/SRS/sections/9X_Frontends_Ops/93_Command_Line_Interface.md†L1-L83】【F:docs/SRS/sections/4X_Interprocess_Communications/42_Transports.md†L1-L45】【F:docs/SRS/sections/4X_Interprocess_Communications/41_Inter_Application_API.md†L1-L40】【F:docs/SRS/sections/9X_Frontends_Ops/94_Extensibility_Packaging_Updates.md#packaging-updates--catalog†L1-L60】
- **Primary requirements:** R-CLI-000…R-CLI-010 covering host unification, offline/live modes, plugin discovery, UX, versioning, and packaging.【F:docs/SRS/sections/9X_Frontends_Ops/93_Command_Line_Interface.md†L19-L66】
- **Tasks:** NX-514 (SRS/ADR alignment), NX-CLI-001 (host scaffold), NX-CLI-002 (endpoint resolver), NX-CLI-004 (plugin manifest loader), NX-CLI-008 (tool packaging).【F:tasks.md†L276-L324】【F:docs/SRS/sections/9X_Frontends_Ops/93_Command_Line_Interface.md†L67-L83】
- **Acceptance hooks:**
  - `nx core status` auto-discovers a running Core via named pipe/UDS within ≤ 500 ms on supported OS targets.
  - `nx plugin list --json` returns manifest-derived metadata and flags compatibility mismatches when plugin semver ranges are violated.
  - `nx diag dump` produces a timestamped archive containing logs, manifests, and version info usable in support workflows.

### Cross-track integration slice — PoseStream → Controller → Guidance preview
- **Vertical scope:** PoseStream ingest, LayerController stub, Section Arbiter happy-path, and Guidance preview widget stitched together with the NullMapping provider.
- **Objective:** Exercise ADR-027, ADR-029, and ADR-030 interfaces in concert before full drafts complete, smoke out capability registry assumptions, and validate degraded-mode messaging when optional plugins are absent.
- **Approach adjustment:** Lead with the ADR-031 manifest validator running inside the replay CI harness so NullMapping, ZoneService, and JobsService mocks must declare capabilities before the slice boots. Follow with Section Arbiter + Guidance preview wiring once manifest gating passes, keeping optional providers behind capability fallbacks rather than hard-coded wiring.
- **Validation harness:** Extend `nexus sim replay --slice cross-track` to drive the slice in headless CI, capturing PoseStream, controller outputs, dependency warnings, and job journal events for regression comparison.
- **Exit criteria:** End-to-end replay that drives the guidance preview using the shared timebase, emits dependency warnings when mapping/plugins are missing, and records a job session journal entry for the run.
- **NX task alignment:** NX-124, NX-126, NX-131, NX-157.


### ADR-030 — Field job sessions & lifecycle services
- **Owner:** Core Owner — Lifecycle & UI pod
- **Stage:** Proposed (target review window: 2025-10-31 week)
- **Dependencies:** ADR-028 (stack boundaries); ADR-029 (mapping kernel availability); ADR-031 (plugin manifest governance)
- **Scope:** Establish a job metadata schema, filesystem job store, and Core-hosted lifecycle service so New/Resume/Open/Drive-In flows share deterministic state across Core, UI, plugins, and import pipelines while remaining compatible with legacy archives.【F:docs/ADR/ADR-030-field-job-sessions.md†L7-L58】
- **Key decisions:** Versioned `aog.job.v1` schema, job folder layout, JobsService verbs, UI drawer/menu parity, plugin lifecycle hooks, and autosave/journaling expectations.【F:docs/ADR/ADR-030-field-job-sessions.md†L13-L86】
- **SRS alignment:** Data Model (§08 job metadata & journaling), Frontends (§05 job menus & drawer), Extensibility (§12 plugin lifecycle hooks).【F:docs/SRS/sections/3X_Data_Storage/32_Persistence_Formats.md†L24-L27】【F:docs/SRS/sections/9X_Frontends_Ops/91_UI_Shell_Layout.md†L22-L25】【F:docs/SRS/sections/9X_Frontends_Ops/94_Extensibility_Packaging_Updates.md†L29-L32】
- **Primary requirements:** R-DATA-029…R-DATA-031; R-FE-050…R-FE-052; R-EXT-140…R-EXT-142.【F:docs/SRS/sections/3X_Data_Storage/32_Persistence_Formats.md†L24-L27】【F:docs/SRS/sections/9X_Frontends_Ops/91_UI_Shell_Layout.md†L22-L25】【F:docs/SRS/sections/9X_Frontends_Ops/94_Extensibility_Packaging_Updates.md†L29-L32】
- **Tasks:** Implement job schema/helpers, JobsService host, Avalonia job drawer/menu, importer hooks, Drive-In geofence indexing, autosave/journaling pipeline.【F:docs/ADR/ADR-030-field-job-sessions.md†L58-L97】
- **Acceptance hooks:**
  - Resume-from-crash scenario must restore the active job within 8 seconds and without duplicating more than one PoseStream segment in audit logs.
  - Legacy archive migration harness must convert ≥ 50 representative jobs with zero schema validation failures and emit warnings for every downgraded field.
  - Drive-In geofence discovery must populate implement entry/exit events with ≤ 50 cm spatial error when evaluated against recorded RTK datasets.
- **NX task alignment:** NX-131, NX-170.

### ADR-032 — Presets & layout linking for equipment workflows
- **Owner:** UI Owner — Device & Layout pod
- **Stage:** Proposed (target sign-off window: 2025-11-07 week)
- **Dependencies:** ADR-030 (job sessions); ADR-031 (plugin manifest governance); ADR-015 (section control semantics)
- **Scope:** Deliver presets that bind equipment, implements, and layouts with live-link or snapshot semantics, plus task orchestration that surfaces progress when presets change machine context.【F:docs/ADR/ADR-032-presets-and-layout-linking.md†L7-L35】
- **Key decisions:** Preset/Layout service contracts, versioned layout documents with inheritance, live-link vs. snapshot behavior, diff/rollback tooling, and task orchestration APIs surfaced in the UI.【F:docs/ADR/ADR-032-presets-and-layout-linking.md†L11-L39】
- **SRS alignment:** Frontends (§05 preset switcher & diff tooling), Data Model (§08 preset/layout provenance), Extensibility (§12 task orchestration & permissions).【F:docs/SRS/sections/9X_Frontends_Ops/91_UI_Shell_Layout.md†L22-L25】【F:docs/SRS/sections/3X_Data_Storage/32_Persistence_Formats.md†L24-L27】【F:docs/SRS/sections/9X_Frontends_Ops/94_Extensibility_Packaging_Updates.md†L29-L32】
- **Primary requirements:** R-FE-050…R-FE-053; R-DATA-029…R-DATA-032; R-EXT-140…R-EXT-143.【F:docs/SRS/sections/9X_Frontends_Ops/91_UI_Shell_Layout.md†L22-L25】【F:docs/SRS/sections/3X_Data_Storage/32_Persistence_Formats.md†L24-L27】【F:docs/SRS/sections/9X_Frontends_Ops/94_Extensibility_Packaging_Updates.md†L29-L32】
- **Tasks:** Presets/Layout services, layout diff tooling, preset switcher UI, task orchestration service, fixture catalog, migration utilities.【F:docs/ADR/ADR-032-presets-and-layout-linking.md†L41-L74】
- **Acceptance hooks:**
  - Preset application regression pack must execute 12 reference scenarios with ≤ 1 frame of section jitter when switching between live-link implements.
  - Layout inheritance diff tool must emit human-readable change summaries with coverage for adds/removes/overrides and demonstrate < 5% false-positive rate across seeded fixtures.
  - Task orchestration telemetry must surface preset apply progress within 500 ms of state change and include correlated job/session IDs for audit.
- **NX task alignment:** NX-130, NX-170.

### ADR-031 — Official plugin bundle & dependency governance
- **Owner:** Plugins Owner — Bundle governance pod
- **Stage:** Proposed (target review window: 2025-11-10 week)
- **Dependencies:** ADR-028 (stack boundaries); ADR-010 (layer registry for manifest hashes); ADR-026 (performance budgets, pending)
- **Scope:** Ratify the authoritative manifest schema, dependency matrices, and compatibility policy for the first-party plugin bundle (guidance, mapping, rate/section control, IO bridges, UI shell) so deployments can validate stack integrity before activation.【F:docs/plugins/nexus-plugin-dependency-map.md†L1-L421】
- **Key decisions:** Manifest compliance tooling, dependency classification (hard/soft/suggest), release cadence for manifest updates, and how Core enforces mismatched ranges during plugin load.【F:docs/plugins/nexus-plugin-dependency-map.md†L13-L421】
- **SRS alignment:** Extensibility & Plugins (§12 R-EXT-120, R-EXT-150), Hardware I/O (§06 AgIO bridge contracts), Frontends (§05 UI contributions).【F:docs/SRS/sections/9X_Frontends_Ops/94_Extensibility_Packaging_Updates.md†L1-L161】【F:docs/SRS/sections/5X_Hardware_IO_Device_Layer/51_Sensor_Actuator_Abstractions.md†L1-L54】【F:docs/SRS/sections/9X_Frontends_Ops/91_UI_Shell_Layout.md†L1-L120】
- **Primary requirements:** R-EXT-120, R-EXT-150, R-HW-020…R-HW-027, R-FE-030…R-FE-032.【F:docs/SRS/sections/9X_Frontends_Ops/94_Extensibility_Packaging_Updates.md†L113-L161】【F:docs/SRS/sections/5X_Hardware_IO_Device_Layer/51_Sensor_Actuator_Abstractions.md†L16-L54】【F:docs/SRS/sections/9X_Frontends_Ops/91_UI_Shell_Layout.md†L20-L76】
- **Tasks:** Promote manifest schema tooling, automate dependency graph validation in CI, publish per-plugin manifests, and surface compatibility status in Device Manager/UI Shell.【F:docs/plugins/nexus-plugin-dependency-map.md†L167-L421】
- **Acceptance hooks:**
  - CI schema validation job must complete in < 90 s and block merges when dependency ranges conflict or when transitive manifests omit required hashes.
  - Core loader integration must quarantine incompatible plugins and emit structured diagnostics (`manifestError`) consumable by UI/telemetry pipelines within 250 ms of load attempt.
  - UI compatibility dashboard must present bundle health with pass/warn/fail states and include remediation links for at least the top 10 first-party plugins.
- **NX task alignment:** NX-134, NX-157, NX-168.

### ADR-068 — Layer controllers & aggregation runtime
- **Owner:** Core Owner — Layer Controllers pod
- **Stage:** Drafting (target review window: 2025-11-14 week)
- **Dependencies:** ADR-029 (mapping kernel split); ADR-010 (layer registry); ADR-027 (zone policies for gating)
- **Scope:** Stand up the per-section layer controller pipeline that normalizes sensor feeds, maintains running accumulators, and emits immutable snapshots so PoseStream, SectionState, and TileStore stay consistent during live runs and replays.【F:docs/SRS/sections/2X_System_Architecture/21_System_Decomposition_Boundaries.md†L6-L18】【F:docs/SRS/options/6X/O-BACKEND-4_LayerControllers.md†L1-L38】
- **Key decisions:** Controller lifecycle (ingest → aggregate → emit), concurrency boundaries between IO, aggregation, and rendering threads, schema hashing/registry handshake with layer definitions, and how controllers surface quality/`rateNA` metadata into dashboards and exports.【F:docs/SRS/options/6X/O-BACKEND-4_LayerControllers.md†L7-L21】【F:docs/SRS/options/7X/O-DATA-5_MetadataDrivenLayers.md†L6-L28】【F:docs/SRS/options/9X/O-TEST-4_LayerReplayCI.md†L6-L19】
- **SRS alignment:** Backend Services (§04 controllers & health budgets), Data Model (§08 layer storage & metadata), Telemetry (§10 diagnostics), Testing (§11 replay determinism).【F:docs/SRS/sections/2X_System_Architecture/21_System_Decomposition_Boundaries.md†L6-L18】【F:docs/SRS/sections/3X_Data_Storage/32_Persistence_Formats.md†L6-L21】【F:docs/SRS/sections/6X_Core_Domain_Services/64_Telemetry_Health.md†L12-L18】【F:docs/SRS/sections/9X_Frontends_Ops/96_Quality_Engineering_Release.md†L6-L19】
- **Primary requirements:** R-BE-010…R-BE-013; R-DATA-010…R-DATA-018; R-TH-020; R-CI-010…R-CI-030.【F:docs/SRS/sections/2X_System_Architecture/21_System_Decomposition_Boundaries.md†L11-L15】【F:docs/SRS/sections/3X_Data_Storage/32_Persistence_Formats.md†L11-L21】【F:docs/SRS/sections/6X_Core_Domain_Services/64_Telemetry_Health.md†L13-L18】【F:docs/SRS/sections/9X_Frontends_Ops/96_Quality_Engineering_Release.md†L11-L19】
- **Tasks:** Implement controller abstractions, DI registry, and buffer pools; wire PoseStream ingestion and replay harnesses; expose quality/diagnostic feeds; update TileStore writers; extend CI with deterministic replay fixtures covering 48–64 row rigs.【F:docs/SRS/options/6X/O-BACKEND-4_LayerControllers.md†L7-L34】【F:docs/SRS/options/9X/O-TEST-4_LayerReplayCI.md†L6-L19】
- **Acceptance hooks:**
  - Controller pipeline must sustain 25 Hz ingest for 48-row rigs with ≤ 22% CPU on aggregation workers and ≤ 500 MB total working-set at steady state.
  - Schema registration handshake must reject mismatched layer hashes with actionable diagnostics and emit `LayerRegistryMismatch` telemetry verified across three replay fixtures.
  - Golden replay outputs from controllers must match analytical baselines within 0.5% per-channel variance over a 60-minute run (yield, rate, downforce).

### ADR-044 — Zone drawing framework
- **Owner:** Core Owner — Mapping & Lifecycle pod
- **Stage:** Drafting (target review window: 2025-04-18 week)
- **Dependencies:** ADR-040/041/043 (context lifecycle), ADR-029 (mapping plugin architecture)
- **Scope:** Provide a shared geometry editing system (`LayerEditService`, toolbar, undo/redo, attribute panels) so Core and plugins draw/edit spatial layers with consistent provenance and IDs.【F:docs/ADR/ADR-044_ZoneDrawingFramework.md†L9-L74】
- **Key decisions:** Editing APIs (`drawLayer`, `editFeature`, `mergeZones`, `splitZone`), event contracts (`onLayerStartEdit`, `onFeatureCommit`), storage of `LayerEditEvent` journals, toolbar UX, and plugin registration for editable layers.【F:docs/ADR/ADR-044_ZoneDrawingFramework.md†L29-L74】
- **SRS alignment:** Data model (§02 layer provenance), Job lifecycle (§03 edit events), Frontends (§05 shared toolbar), Plugins (§12 edit subscriptions).【F:docs/SRS/sections/3X_Data_Storage/31_Domain_Data_Model.md†L1-L160】【F:docs/SRS/sections/6X_Core_Domain_Services/62_Job_Lifecycle.md†L1-L120】【F:docs/SRS/sections/9X_Frontends_Ops/91_UI_Shell_Layout.md†L1-L150】【F:docs/SRS/sections/9X_Frontends_Ops/94_Extensibility_Packaging_Updates.md†L1-L120】
- **Primary requirements:** R-DATA-040…R-DATA-052 (edit provenance), R-UX-030…R-UX-044 (drawing UX), R-EXT-080…R-EXT-099 (plugin edit hooks).
- **Tasks:** Implement LayerEditService, persist `LayerEditEvent` schema, wire toolbar into UI shell, update plugin manifests for editable layer declarations, author regression fixtures for undo/redo and collaborative edits.
- **Acceptance hooks:**
  - Collaborative edit replay reproduces identical geometry after undo/redo with ≤ 1 cm centroid delta.
  - Crop Type and Field Health plugins receive `onFeatureCommit` events and update analytics without manual refresh.
  - LayerEditEvent schema validation passes for at least 10 recorded edit scenarios (draw, merge, split, undo).

### ADR-045 — Crop type plugin & layers
- **Owner:** Plugins Owner — Agronomy pod
- **Stage:** Drafting (target review window: 2025-04-25 week)
- **Dependencies:** ADR-044 (zone tool), ADR-040/041/043 (context lifecycle)
- **Scope:** Deliver planned/actual/history crop layers, extend Field schema with `cropTypeHistory[]`, auto-fill job/session crop context, and expose analytics hooks for rotation queries.【F:docs/ADR/ADR-045_CropTypePlugin.md†L9-L74】
- **Key decisions:** Layer attribute schema, crop history storage strategy, UI flows for quick crop selection, analytics APIs for previous crop queries, and integration with report builder.【F:docs/ADR/ADR-045_CropTypePlugin.md†L29-L71】
- **SRS alignment:** Data model (§02 field crop history), Job lifecycle (§03 crop context on session start), Mapping layers (§04 crop overlays), Analytics (§08 rotation stats).【F:docs/SRS/sections/3X_Data_Storage/31_Domain_Data_Model.md†L90-L200】【F:docs/SRS/sections/6X_Core_Domain_Services/62_Job_Lifecycle.md†L1-L120】【F:docs/SRS/sections/7X_Mapping_Geospatial/72_Mapping_Layers_Plugin.md†L1-L160】【F:docs/SRS/sections/3X_Data_Storage/32_Persistence_Formats.md†L1-L140】
- **Primary requirements:** R-DATA-060…R-DATA-068, R-ANL-020…R-ANL-028, R-UX-050…R-UX-056.
- **Tasks:** Implement crop layer schemas, update Field schema, build quick-select UI, wire analytics API, populate report builder sections.
- **Acceptance hooks:**
  - Editing crop zones updates Field crop history and job/session context within one autosave cycle.
  - Rotation analytics return correct previous-crop values across multi-field jobs in regression fixtures.
  - Report builder crop summary template renders planned vs. actual acreage with provenance links.

### ADR-046 — Genetics plugin & layers
- **Owner:** Plugins Owner — Agronomy pod
- **Stage:** Drafting (target review window: 2025-05-02 week)
- **Dependencies:** ADR-045 (crop context), ADR-044 (zone tool), ADR-047 (telemetry mesh for live sharing)
- **Scope:** Capture planned and actual seed genetics with barcode/lot tracking, auto-write layers during planting, and export CSV/GeoJSON/ISOXML packages.【F:docs/ADR/ADR-046_GeneticsPlugin.md†L9-L74】
- **Key decisions:** Genetics layer schemas, barcode change logging, UI picker and legend behavior, integration with coverage events, export formats, and provenance requirements.【F:docs/ADR/ADR-046_GeneticsPlugin.md†L21-L71】
- **SRS alignment:** Job lifecycle (§03 coverage hooks), Mapping layers (§04 genetics layers), Data model (§08 provenance), Extensibility (§12 plugin registration).【F:docs/SRS/sections/6X_Core_Domain_Services/62_Job_Lifecycle.md†L59-L140】【F:docs/SRS/sections/7X_Mapping_Geospatial/72_Mapping_Layers_Plugin.md†L90-L200】【F:docs/SRS/sections/3X_Data_Storage/32_Persistence_Formats.md†L1-L140】【F:docs/SRS/sections/9X_Frontends_Ops/94_Extensibility_Packaging_Updates.md†L60-L140】
- **Primary requirements:** R-DATA-070…R-DATA-085, R-EXT-110…R-EXT-129, R-UX-060…R-UX-068.
- **Tasks:** Author genetics schemas, build picker UI with barcode support, wire coverage integration, implement export pipelines, add report builder sections.
- **Acceptance hooks:**
  - Live planting session auto-writes genetics layers with ≤ 2s latency from coverage events.
  - Barcode scan updates active variety and records change log entries validated by schema tests.
  - ISOXML export passes validator against representative monitor datasets.

### ADR-047 — Live telemetry mesh
- **Owner:** Core Owner — Connectivity pod
- **Stage:** Drafting (target review window: 2025-05-09 week)
- **Dependencies:** ADR-048 (radio bridge), ADR-040/041/043 (context lifecycle)
- **Scope:** Establish pub/sub mesh over `aog/live/{season}/{job}/{layer}` topics with device/share/subscribe profiles, QoS tiers, and privacy controls for collaborative operations.【F:docs/ADR/ADR-047_LiveTelemetryMesh.md†L9-L70】
- **Key decisions:** Topic taxonomy, presence/trail cadence, QoS tiers, ACL enforcement, UI visualization, layer replication strategy, and LayerEditEvent journal integration.【F:docs/ADR/ADR-047_LiveTelemetryMesh.md†L21-L70】
- **SRS alignment:** Communications (§03 mesh transport), Telemetry (§10 presence/trails), Extensibility (§12 collaborative plugins).【F:docs/SRS/sections/4X_Interprocess_Communications/42_Transports.md†L40-L140】【F:docs/SRS/sections/6X_Core_Domain_Services/64_Telemetry_Health.md†L1-L120】【F:docs/SRS/sections/9X_Frontends_Ops/94_Extensibility_Packaging_Updates.md†L1-L120】
- **Primary requirements:** R-COMM-050…R-COMM-072, R-TH-030…R-TH-042, R-EXT-140…R-EXT-155.
- **Tasks:** Implement mesh service, design presence/trail payloads, build share/subscribe UI, add diagnostics, integrate with zone/yield plugins.
- **Acceptance hooks:**
  - Mesh latency ≤ 300 ms p95 for presence updates across three devices on ELRS link.
  - ACL tests verify unauthorized devices cannot subscribe to protected layers.
  - Offline replay reconstructs collaborative edits from LayerEditEvent journals without divergence.

### ADR-048 — RadioBridge for ELRS/LoRa telemetry
- **Owner:** Core Owner — Connectivity pod
- **Stage:** Drafting (target review window: 2025-05-09 week)
- **Dependencies:** ADR-047 (mesh service)
- **Scope:** Define binary framing, reliability, encryption, and diagnostics for ELRS/LoRa links carrying mesh traffic.【F:docs/ADR/ADR-048_RadioBridge.md†L9-L60】
- **Key decisions:** Header layout, selective repeat ARQ parameters, topic ID registry, encryption scheme, telemetry counters, provisioning workflow.【F:docs/ADR/ADR-048_RadioBridge.md†L17-L60】
- **SRS alignment:** Communications (§03 radio transport), Telemetry (§10 diagnostics), Support docs (§support) for provisioning.【F:docs/SRS/sections/4X_Interprocess_Communications/42_Transports.md†L90-L160】【F:docs/SRS/sections/6X_Core_Domain_Services/64_Telemetry_Health.md†L60-L120】
- **Primary requirements:** R-COMM-073…R-COMM-084, R-TH-043…R-TH-050.
- **Tasks:** Implement framing library, build ELRS/LoRa adapters, author provisioning docs, integrate key management, add CI tests for retry/fec logic.
- **Acceptance hooks:**
  - Packet loss recovery keeps application drop rate < 2% at 5 kbps channel with 10% raw loss.
  - Encryption handshake completes < 1 s and rejects invalid keys with operator-visible alerts.
  - Diagnostics panel displays live RSSI, retry, and drop counters.

### ADR-049 — Yield & analytics plugin
- **Owner:** Plugins Owner — Analytics pod
- **Stage:** Drafting (target review window: 2025-05-16 week)
- **Dependencies:** ADR-045 (crop), ADR-046 (genetics), ADR-044 (zone tool)
- **Scope:** Capture yield/moisture/test weight layers with provenance, smoothing, imports, and analytics APIs (yield by crop/variety).【F:docs/ADR/ADR-049_YieldPlugin.md†L9-L66】
- **Key decisions:** Sensor normalization, aggregation bins, API design, import wizard UX, color ramp expectations, export formats.【F:docs/ADR/ADR-049_YieldPlugin.md†L21-L59】
- **SRS alignment:** Data model (§08 yield storage), Frontends (§05 map overlays), Extensibility (§12 analytics APIs).【F:docs/SRS/sections/9X_Frontends_Ops/91_UI_Shell_Layout.md†L90-L170】【F:docs/SRS/sections/3X_Data_Storage/32_Persistence_Formats.md†L60-L160】【F:docs/SRS/sections/9X_Frontends_Ops/94_Extensibility_Packaging_Updates.md†L80-L140】
- **Primary requirements:** R-DATA-086…R-DATA-110, R-ANL-030…R-ANL-048, R-UX-070…R-UX-082.
- **Tasks:** Build ingest pipeline, persist layers with provenance, implement analytics APIs, develop import wizard and map overlays, produce export tooling.
- **Acceptance hooks:**
  - Yield smoothing pipeline reproduces baseline regression results within ±1.5% across fixtures.
  - Analytics API cross-tests confirm consistent outputs when filtering by crop and variety.
  - Import wizard validates ISOXML datasets and surfaces unit conversions without manual editing.

### ADR-050 — Cost & profit plugin
- **Owner:** Plugins Owner — Analytics pod
- **Stage:** Drafting (target review window: 2025-05-23 week)
- **Dependencies:** ADR-049 (yield), ADR-046 (genetics), ADR-045 (crop)
- **Scope:** Track cost records, compute profit layers, and expose financial rollups per field/farm/season with CSV/PDF exports.【F:docs/ADR/ADR-050_CostProfitPlugin.md†L9-L59】
- **Key decisions:** Cost schema categories, integration with other plugins, profit layer metadata, visualization standards, export formats.【F:docs/ADR/ADR-050_CostProfitPlugin.md†L21-L52】
- **SRS alignment:** Data model (§08 economics), Frontends (§05 financial overlays), Extensibility (§12 analytics/reporting).【F:docs/SRS/sections/9X_Frontends_Ops/91_UI_Shell_Layout.md†L120-L190】【F:docs/SRS/sections/3X_Data_Storage/32_Persistence_Formats.md†L90-L190】【F:docs/SRS/sections/9X_Frontends_Ops/94_Extensibility_Packaging_Updates.md†L100-L160】
- **Primary requirements:** R-DATA-111…R-DATA-128, R-ANL-049…R-ANL-060, R-UX-083…R-UX-092.
- **Tasks:** Implement CostRecord/ProfitLayer schemas, build cost entry UI, wire analytics rollups, integrate exports, feed report builder.
- **Acceptance hooks:**
  - Profit layer generation completes within 2 minutes for 500-acre job with 10 cost categories.
  - CSV/PDF exports balance costs vs. revenue totals within ±$1 rounding tolerance.
  - UI heatmap correctly reflects negative and positive profit zones with accessible color ramps.

### ADR-051 — Report builder & export system
- **Owner:** Core Owner — Reporting pod
- **Stage:** Drafting (target review window: 2025-05-30 week)
- **Dependencies:** ADR-045…ADR-050 for data sources, ADR-047 for shared data access policies
- **Scope:** Centralize report generation via templates, plugin-provided sections, and multi-format exports (PDF/CSV/GeoJSON).【F:docs/ADR/ADR-051_ReportBuilder.md†L9-L55】
- **Key decisions:** Template schema, section registration hooks, export packaging, caching strategy, UI preview flows.【F:docs/ADR/ADR-051_ReportBuilder.md†L21-L52】
- **SRS alignment:** Data model (§08 reporting), Frontends (§05 report UI), Extensibility (§12 plugin hooks).【F:docs/SRS/sections/9X_Frontends_Ops/91_UI_Shell_Layout.md†L150-L210】【F:docs/SRS/sections/3X_Data_Storage/32_Persistence_Formats.md†L120-L210】【F:docs/SRS/sections/9X_Frontends_Ops/94_Extensibility_Packaging_Updates.md†L120-L190】
- **Primary requirements:** R-ANL-061…R-ANL-078, R-UX-093…R-UX-104, R-EXT-156…R-EXT-170.
- **Tasks:** Define template schema, implement report builder service, build preview/share UI, integrate plugin sections, add CI snapshot tests.
- **Acceptance hooks:**
  - Built-in templates generate matching golden PDFs/CSVs for regression fixtures.
  - Plugins can contribute sections declaratively with automated validation of dependencies.
  - Report generation handles offline mode by queuing pending exports and syncing when connectivity returns.

### ADR-052 — Field health & risk plugin
- **Owner:** Plugins Owner — Agronomy pod
- **Stage:** Drafting (target review window: 2025-05-30 week)
- **Dependencies:** ADR-044 (zone tool), ADR-045 (crop context), ADR-049/050 (analytics consumers)
- **Scope:** Capture risk overlays (flood, compaction, weeds, other) with severity metadata and historical views for analytics/reporting.【F:docs/ADR/ADR-052_FieldHealthPlugin.md†L9-L55】
- **Key decisions:** Severity schema, color coding, historical navigation, analytics integration, report builder hooks.【F:docs/ADR/ADR-052_FieldHealthPlugin.md†L21-L44】
- **SRS alignment:** Mapping layers (§04 risk overlays), Data model (§08 risk storage), Frontends (§05 severity legends).【F:docs/SRS/sections/7X_Mapping_Geospatial/72_Mapping_Layers_Plugin.md†L120-L220】【F:docs/SRS/sections/9X_Frontends_Ops/91_UI_Shell_Layout.md†L160-L220】【F:docs/SRS/sections/3X_Data_Storage/32_Persistence_Formats.md†L140-L210】
- **Primary requirements:** R-DATA-129…R-DATA-142, R-ANL-079…R-ANL-086, R-UX-105…R-UX-112.
- **Tasks:** Define risk layer schemas, implement severity legends, integrate analytics callbacks, surface historical toggles, wire report builder sections.
- **Acceptance hooks:**
  - Severity color scale passes accessibility contrast checks and matches analytics outputs.
  - Yield/profit analytics adjust when risk severity changes in regression fixtures.
  - Report builder scouting section lists observed risks with timestamps and observers.

### ADR-053 — Weather & environment plugin
- **Owner:** Plugins Owner — Analytics pod
- **Stage:** Drafting (target review window: 2025-06-06 week)
- **Dependencies:** ADR-040/041 (session metadata), ADR-047 (optional live sharing)
- **Scope:** Log weather snapshots, ingest sensor/API data, render environmental overlays, and extend session metadata for analytics and compliance.【F:docs/ADR/ADR-053_WeatherPlugin.md†L9-L55】
- **Key decisions:** Auto-logging cadence, overlay visualization, API/import integration, data retention, report builder hooks.【F:docs/ADR/ADR-053_WeatherPlugin.md†L21-L49】
- **SRS alignment:** Job lifecycle (§03 weather events), Data model (§02 session weather), Mapping layers (§04 weather overlay), Frontends (§05 weather UI).【F:docs/SRS/sections/3X_Data_Storage/31_Domain_Data_Model.md†L120-L210】【F:docs/SRS/sections/6X_Core_Domain_Services/62_Job_Lifecycle.md†L80-L160】【F:docs/SRS/sections/7X_Mapping_Geospatial/72_Mapping_Layers_Plugin.md†L150-L230】【F:docs/SRS/sections/9X_Frontends_Ops/91_UI_Shell_Layout.md†L170-L230】
- **Primary requirements:** R-DATA-143…R-DATA-156, R-ANL-087…R-ANL-094, R-UX-113…R-UX-120.
- **Tasks:** Extend session schema with weather snapshots, build sensor/API ingest pipeline, implement overlay rendering, provide timeline UI, integrate with report builder.
- **Acceptance hooks:**
  - Auto-logging writes weather snapshots at configured interval with drift < 10 seconds across a one-hour session.
  - Weather overlay visualizes rainfall/temp/wind vectors with validated unit conversions.
  - Report builder weather sections include timeline summaries and comply with export format requirements.

### ADR-033 — Guidance planner & autosteer orchestration
- **Owner:** Core Owner — Guidance & Autonomy pod
- **Stage:** Drafting (target review window: 2025-11-21 week)
- **Dependencies:** ADR-027 (zone policies); ADR-068 (layer controllers for lookahead metadata); ADR-015 (section control semantics)
- **Scope:** Reconcile the legacy guidance math (AB/curve/turn planners, Stanley controller) with Nexus PoseStream and zone gating so guidance plugins, autosteer firmware, and UI share deterministic lane, turn, and lookahead policies.【F:docs/porting/V6-Inventory.md†L5-L44】【F:docs/SRS/sections/6X_Core_Domain_Services/61_Kinematics_Pose_Fusion.md†L16-L24】
- **Key decisions:** Canonical lane model (straight, curve, adaptive), turn template selection and preview publishing, lookahead scheduling tied to PoseStream cadence, error damping/anti-windup expectations, and how constraint/zone masks bias path outputs prior to control arbitration.【F:docs/porting/V6-Inventory.md†L7-L19】【F:docs/ADR/ADR-027-spatial-constraints.md†L7-L44】【F:docs/SRS/sections/6X_Core_Domain_Services/61_Kinematics_Pose_Fusion.md†L16-L24】
- **SRS alignment:** Control & Automation (§09 control graph, automation lifecycle), Interprocess API (§07 geometry metadata), Extensibility (§12 plugin contracts).【F:docs/SRS/sections/6X_Core_Domain_Services/61_Kinematics_Pose_Fusion.md†L16-L24】【F:docs/SRS/sections/4X_Interprocess_Communications/41_Inter_Application_API.md†L16-L24】【F:docs/SRS/sections/9X_Frontends_Ops/94_Extensibility_Packaging_Updates.md†L6-L28】
- **Primary requirements:** R-CTRL-000…R-CTRL-007; R-GEO-000…R-GEO-002; R-EXT-120.【F:docs/SRS/sections/6X_Core_Domain_Services/61_Kinematics_Pose_Fusion.md†L16-L24】【F:docs/SRS/sections/4X_Interprocess_Communications/41_Inter_Application_API.md†L16-L24】【F:docs/SRS/sections/9X_Frontends_Ops/94_Extensibility_Packaging_Updates.md†L21-L28】
- **Tasks:** Publish guidance lane/turn schemas, port Stanley/pure-pursuit control with deterministic fixtures, integrate constraint-aware lookahead, define plugin hooks for lane publishing, and ensure autosteer outputs respect Core arbitration leases.【F:docs/porting/V6-Inventory.md†L7-L19】【F:docs/ADR/ADR-027-spatial-constraints.md†L7-L44】【F:docs/SRS/sections/6X_Core_Domain_Services/61_Kinematics_Pose_Fusion.md†L16-L24】
- **Acceptance hooks:**
  - Guidance path regression suite must stay within ≤ 4 cm lateral RMS error vs. V6 legacy traces across AB, curve, and adaptive headland fixtures.
  - Constraint fault-injection tests must force autosteer disengage within 150 ms of a zone mask conflict and log the controlling constraint in telemetry.
  - Firmware loop-in-the-loop bench must demonstrate closed-loop stability with steady-state steering error ≤ 2° at 15 km/h using recorded PoseStream inputs.

### ADR-034 — Metadata-driven dashboards & inspector surfaces
- **Owner:** UI Owner — Telemetry & Visualization pod
- **Stage:** Drafting (target review window: 2025-11-28 week)
- **Dependencies:** ADR-010 (layer registry); ADR-068 (layer controllers); ADR-029 (mapping plugin APIs)
- **Scope:** Deliver the layer-aware dashboard/inspector refactor so overlays, charts, and presets consume the layer registry and controller metadata without hard-coded IDs, enabling declarative visualization across desktop and companion clients.【F:docs/SRS/sections/9X_Frontends_Ops/91_UI_Shell_Layout.md†L10-L27】【F:docs/SRS/options/9X/O-UI-5_MetadataDrivenDashboards.md†L1-L28】
- **Key decisions:** Binding strategy between layer definitions and UI widgets, preset catalog and layout persistence, inspector/tooltip data contracts, performance budgets for rich overlays, and how remote/headless modes reuse the same metadata-driven components.【F:docs/SRS/options/9X/O-UI-5_MetadataDrivenDashboards.md†L6-L28】【F:docs/SRS/sections/9X_Frontends_Ops/91_UI_Shell_Layout.md†L19-L27】【F:docs/SRS/options/9X/O-UI-5_MetadataDrivenDashboards.md†L25-L36】
- **SRS alignment:** Frontends (§05 metadata-driven UI, spatial overlays, remote readiness), Telemetry (§10 diagnostics surfacing), Extensibility (§12 plugin UI contributions).【F:docs/SRS/sections/9X_Frontends_Ops/91_UI_Shell_Layout.md†L10-L33】【F:docs/SRS/sections/6X_Core_Domain_Services/64_Telemetry_Health.md†L12-L18】【F:docs/SRS/sections/9X_Frontends_Ops/94_Extensibility_Packaging_Updates.md†L30-L34】
- **Primary requirements:** R-FE-010…R-FE-014; R-FE-040…R-FE-042; R-FE-030…R-FE-032; R-TH-020.【F:docs/SRS/sections/9X_Frontends_Ops/91_UI_Shell_Layout.md†L10-L33】【F:docs/SRS/sections/6X_Core_Domain_Services/64_Telemetry_Health.md†L12-L18】
- **Tasks:** Refactor map overlays and dashboards to read layer metadata, implement inspector/legend components, ship preset catalogs and configuration tooling, and wire feature flags plus replay benchmarks for 48–64 row rigs.【F:docs/SRS/options/9X/O-UI-5_MetadataDrivenDashboards.md†L6-L28】【F:docs/SRS/options/9X/O-UI-5_MetadataDrivenDashboards.md†L25-L36】【F:docs/SRS/options/9X/O-TEST-4_LayerReplayCI.md†L6-L19】
- **Acceptance hooks:**
  - UI automation for dashboards/inspectors must cover at least 30 metadata-driven widgets with > 90% branch coverage in Avalonia UI tests.
  - Replay performance benchmark must render 48-row rigs at ≥ 45 FPS average with ≤ 5 dropped frames per minute on reference GPU (MX450) using layer-heavy fixtures.
  - Remote companion mode must consume the same metadata contracts and pass contract conformance tests that validate schema parity and field-level ACLs.

### ADR-007 — PoseStream & SectionState architecture
- **Owner:** Core Owner — PoseStream pod
- **Stage:** In Review (target sign-off window: 2025-10-24 week)
- **Dependencies:** ADR-001…ADR-004 (runtime foundations); ADR-028 (stack boundaries)
- **Scope:** A single, time-ordered PoseStream spanning tractor, implement, toolbar, and section poses with diffed SectionState updates, unified vector logs, and event/opportunity metrics.
- **Key decisions:** Cadence/decimation policy, one pose timeline for all layers, SectionState diff rules, optional per-layer micro-streams when plugin cadence diverges, replay determinism budgets, and how to compute opportunity vs. event tallies.
- **SRS alignment:** Communications (§03), Data Model (§08), Control & Automation (§09), Extensibility (§12).【F:docs/SRS/sections/4X_Interprocess_Communications/42_Transports.md†L6-L28】【F:docs/SRS/sections/3X_Data_Storage/32_Persistence_Formats.md†L6-L31】【F:docs/SRS/sections/6X_Core_Domain_Services/61_Kinematics_Pose_Fusion.md†L8-L22】【F:docs/SRS/sections/9X_Frontends_Ops/94_Extensibility_Packaging_Updates.md†L6-L28】
- **Primary requirements:** R-COMM-010, R-COMM-011, R-COMM-020; R-DATA-015; R-CTRL-000…R-CTRL-002; R-EXT-120.【F:docs/SRS/sections/4X_Interprocess_Communications/42_Transports.md†L11-L28】【F:docs/SRS/sections/3X_Data_Storage/32_Persistence_Formats.md†L17-L31】【F:docs/SRS/sections/6X_Core_Domain_Services/61_Kinematics_Pose_Fusion.md†L16-L22】【F:docs/SRS/sections/9X_Frontends_Ops/94_Extensibility_Packaging_Updates.md†L21-L28】
- **Tasks:** PoseStreamService, SectionStateManager, event/opportunity schema definitions, shared proto/schema updates, replay fixtures.
- **Acceptance hooks:**
  - PoseStream diff compression must limit per-frame payload to ≤ 2.5 KB median at 20 Hz for 48-section rigs, validated via replay harness.
  - Opportunity/event tallies computed from PoseStream must match analytical goldens within 1% per hectare across three reference jobs.
  - SectionState diff sequencing must remain monotonic under forced clock skew ±25 ms, verified through integration fault-injection.

### ADR-008 — Equipment → Implement → Toolbar → Section(+Group) hierarchy
- **Owner:** Core Owner — Equipment Modeling pod
- **Stage:** Drafting (target review window: 2025-10-31 week)
- **Dependencies:** ADR-007 (PoseStream architecture); ADR-015 (section control semantics)
- **Scope:** Canonical object model with multiple toolbars per implement, overlapping SectionGroups, master groups, and per-toolbar lookahead/overlap metadata with kinematic placeholders for ADR-017.
- **Key decisions:** Stable IDs, offsets, widths, grouping semantics, overlapping SectionGroup arbitration examples, and which nodes/groups accept control targets prior to kinematic link integration.
- **SRS alignment:** Interprocess API (§07) for schema governance and Control (§09) for group arbitration.【F:docs/SRS/sections/4X_Interprocess_Communications/41_Inter_Application_API.md†L6-L28】【F:docs/SRS/sections/6X_Core_Domain_Services/61_Kinematics_Pose_Fusion.md†L16-L22】
- **Primary requirements:** R-GEO-000…R-GEO-002; R-CTRL-000…R-CTRL-002.【F:docs/SRS/sections/4X_Interprocess_Communications/41_Inter_Application_API.md†L16-L28】【F:docs/SRS/sections/6X_Core_Domain_Services/61_Kinematics_Pose_Fusion.md†L16-L22】
- **Tasks:** Data model updates, configuration editor, overlapping group/master arbitration examples, kinematic placeholder registry, migration shims for legacy implements.
- **Acceptance hooks:**
  - Equipment hierarchy JSON schema must validate and round-trip 30 legacy V5/V6 implement configs with zero structural diffs aside from expected ID normalization.
  - Overlapping SectionGroup arbitration tests must demonstrate deterministic master override order with ≤ 50 ms resolution latency under concurrent commands.
  - Configuration editor UX must guard against invalid overlap definitions, surfacing inline errors with contextual guidance validated through automated UI tests.

### ADR-009 — Persistence & storage: Vector PoseStream + Layer TileStore
- **Owner:** Core Owner — Data Persistence pod
- **Stage:** In Review (target sign-off window: 2025-10-28 week)
- **Dependencies:** ADR-007 (PoseStream architecture); ADR-010 (layer registry); ADR-028 (stack boundaries)
- **Scope:** Authoritative PoseStream/SectionState vector logs and chunked tile store with value/weight/min/max channels, deterministic transforms between the two, and support for deployments that retain either (or both).
- **Key decisions:** Tile/cell sizing, codec selection (LZ4/Zstd), crash safety guarantees, registry hash flow, append/compaction strategies, and vector↔tile determinism.
- **SRS alignment:** Data Model & Storage (§08).【F:docs/SRS/sections/3X_Data_Storage/32_Persistence_Formats.md†L6-L31】
- **Primary requirements:** R-DATA-015…R-DATA-025.【F:docs/SRS/sections/3X_Data_Storage/32_Persistence_Formats.md†L17-L31】
- **Tasks:** TileStore service, index format, compactor, vector↔tile transform validation, read/write tests.
- **Acceptance hooks:**
  - TileStore compaction must maintain write amplification ≤ 1.8x under sustained 20 Hz ingest across 2-hour replay fixtures.
  - Vector↔tile transforms must round-trip within 0.25% aggregate error for value/weight/min/max channels using analytical baselines.
  - Crash-safety harness must demonstrate zero data loss when power is pulled mid-write, validated via fsync instrumentation.

### ADR-010 — Layer registry & variable-rate framework
- **Owner:** Core Owner — Layer Governance pod
- **Stage:** Drafting (target review window: 2025-11-05 week)
- **Dependencies:** ADR-009 (TileStore persistence); ADR-031 (plugin governance); ADR-014 (interop formats)
- **Scope:** LayerDefinition schema, units, normalization, color ramps, aggregation modes, discovery/versioning; supersedes earlier variable-rate catalog drafts.
- **Key decisions:** IDs, schema hashes, numeric precision, display metadata, registry publishing cadence, and compatibility with plugin manifests.
- **SRS alignment:** Data Model (§08) and Extensibility (§12).【F:docs/SRS/sections/3X_Data_Storage/32_Persistence_Formats.md†L6-L31】【F:docs/SRS/sections/9X_Frontends_Ops/94_Extensibility_Packaging_Updates.md†L6-L28】
- **Primary requirements:** R-DATA-010…R-DATA-014; R-EXT-010; R-EXT-120.【F:docs/SRS/sections/3X_Data_Storage/32_Persistence_Formats.md†L11-L21】【F:docs/SRS/sections/9X_Frontends_Ops/94_Extensibility_Packaging_Updates.md†L11-L28】
- **Tasks:** Registry service, validators, units library integration, manifest tooling.
- **Acceptance hooks:**
  - LayerDefinition validator must reject inconsistent units/precision combos with explicit error codes and cover ≥ 95% rule branches in unit tests.
  - Registry publish cadence automation must generate signed manifests and propagate updates to plugin registry within 3 minutes, proven via CI pipeline run.
  - Variable-rate framework must normalize three sample agronomic layers (yield, as-applied, prescription) within ±0.5% numeric drift and emit color ramps consistent with design spec screenshots.

### ADR-011 — Mapping & visualization: Basemaps & imagery pipeline
- **Owner:** UI Owner — Mapping & Imagery pod
- **Stage:** Drafting (target review window: 2025-11-12 week)
- **Dependencies:** ADR-029 (mapping plugin architecture); ADR-010 (layer registry); ADR-034 (metadata dashboards)
- **Scope:** Transform PoseStream samples into ribbons, heatmaps, contours, and legends with bilinear sampling, near-vehicle supersampling, basemap caching, attribution, and offline fallbacks.
- **Key decisions:** Render order, GPU texture strategy, interpolation rules, fresh-pass emphasis, ribbon overlay policy, basemap tile caching (disk LRU + offline fallback), and attribution requirements.
- **SRS alignment:** Telemetry & Health (§10) and Frontend rendering surfaces (§05).【F:docs/SRS/sections/6X_Core_Domain_Services/64_Telemetry_Health.md†L6-L47】【F:docs/SRS/sections/9X_Frontends_Ops/91_UI_Shell_Layout.md†L6-L33】
- **Primary requirements:** R-TH-000…R-TH-021; R-FE-004…R-FE-032.【F:docs/SRS/sections/6X_Core_Domain_Services/64_Telemetry_Health.md†L7-L21】【F:docs/SRS/sections/9X_Frontends_Ops/91_UI_Shell_Layout.md†L13-L23】
- **Tasks:** Renderer refactor, shared color-ramp utility, basemap cache manager, legend/overlay widgets.
- **Acceptance hooks:**
  - Basemap cache manager must achieve ≥ 92% cache hit rate on offline replay while respecting ≤ 3 GB disk footprint.
  - Imagery pipeline must sustain ≥ 55 FPS rendering for ribbons/heatmaps on reference GPU with GPU utilization ≤ 80%.
  - Attribution overlay must pass automated screenshot diffs across 5 basemap providers ensuring licensing text placement accuracy.

### ADR-012 — Multi-session & multi-PoseStream fusion
- **Owner:** Core Owner — Data Fusion pod
- **Stage:** Drafting (target review window: 2025-11-19 week)
- **Dependencies:** ADR-009 (TileStore persistence); ADR-022 (CRS policy, pending); ADR-010 (layer registry)
- **Scope:** Merge rules for multiple PoseStreams within a session and across seasons (planter + sprayer, prior-year yield to current prescriptions) including CRS reprojection steps.
- **Key decisions:** Time/space alignment, CRS reprojection (ADR-022), dedupe/priority rules, authority hierarchy (prescription beats historical average unless quality < threshold), area-weighted merges, provenance chain handling.
- **SRS alignment:** Data Model & Storage (§08) and Control & Automation (§09).【F:docs/SRS/sections/3X_Data_Storage/32_Persistence_Formats.md†L17-L31】【F:docs/SRS/sections/6X_Core_Domain_Services/61_Kinematics_Pose_Fusion.md†L16-L22】
- **Primary requirements:** R-DATA-017…R-DATA-022; R-CTRL-001.【F:docs/SRS/sections/3X_Data_Storage/32_Persistence_Formats.md†L19-L26】【F:docs/SRS/sections/6X_Core_Domain_Services/61_Kinematics_Pose_Fusion.md†L18-L18】
- **Tasks:** Fusion service, provenance tracker, priority/arbitration policies, CRS reprojection utilities.
- **Acceptance hooks:**
  - Fusion outputs must maintain ≤ 3 cm positional drift after CRS reprojection when merging two RTK-quality PoseStreams.
  - Priority arbitration tests must uphold configured authority hierarchy in 100% of 200 randomized merge scenarios, logging the winning source for audit.
  - Provenance tracker must persist full lineage for merged layers and export to JSON with < 200 ms serialization overhead for 10k-sample jobs.

### ADR-013 — Derived products (analytics → prescriptions)
- **Owner:** Core Owner — Analytics & Prescriptions pod
- **Stage:** Drafting (target review window: 2025-12-02 week)
- **Dependencies:** ADR-012 (fusion); ADR-010 (layer registry); ADR-014 (interop formats)
- **Scope:** Convert yield/soil/NDVI PoseStreams and layers into prescriptions with gridding, smoothing, ROI masking, banding/clamping, reproducible recipes, and QA metrics.
- **Key decisions:** Target functions, QA metrics, parameterizable recipes with parameter hashes, output validation loops, QA report content (coverage, variance, RMSE vs. target).
- **SRS alignment:** Testing & Analytics governance (§11) and Data Model (§08).【F:docs/SRS/sections/9X_Frontends_Ops/96_Quality_Engineering_Release.md†L6-L19】【F:docs/SRS/sections/3X_Data_Storage/32_Persistence_Formats.md†L17-L31】
- **Primary requirements:** R-CI-021 plus R-CI-010, R-CI-020, R-CI-030; R-DATA-017…R-DATA-022.【F:docs/SRS/sections/9X_Frontends_Ops/96_Quality_Engineering_Release.md†L11-L19】【F:docs/SRS/sections/3X_Data_Storage/32_Persistence_Formats.md†L19-L26】
- **Tasks:** Derivation pipeline, configurable recipe format (YAML/JSON), QA report generator, regression fixtures.
- **Acceptance hooks:**
  - Prescription derivation must complete within 4 minutes for a 160-acre field on reference hardware, including smoothing and ROI masking stages.
  - QA report generator must compute coverage/variance/RMSE metrics with ≤ 0.5% deviation from analytical goldens across 10 recipe fixtures.
  - Recipe hashing must produce stable identifiers across OS/platform combinations with zero mismatches in cross-platform regression run.

### ADR-014 — Interop: prescription & agronomic formats
- **Owner:** Core Owner — Interop & Data Exchange pod
- **Stage:** Drafting (target review window: 2025-11-26 week)
- **Dependencies:** ADR-022 (CRS policy, pending); ADR-010 (layer registry); ADR-013 (derived products)
- **Scope:** Import/export ISOXML TaskData, GeoTIFF/COG rasters, Shapefile/GeoPackage vectors, MBTiles tilesets with consistent units/CRS and attribute mapping.
- **Key decisions:** Terminology (use “Prescription” for VR drivers), canonical internal units, CRS policy (ADR-022), prefer ISOXML TaskData for vector prescriptions, COG GeoTIFF for raster, GeoPackage as vector fallback, attribute naming, file naming conventions.
- **SRS alignment:** Communications (§03 interop notes) and Data Model (§08 exports).【F:docs/SRS/sections/4X_Interprocess_Communications/42_Transports.md†L6-L28】【F:docs/SRS/sections/3X_Data_Storage/32_Persistence_Formats.md†L6-L26】
- **Primary requirements:** R-DATA-003, R-DATA-014, R-DATA-018; R-COMM-005; R-DATA-019…R-DATA-022.【F:docs/SRS/sections/3X_Data_Storage/32_Persistence_Formats.md†L10-L26】【F:docs/SRS/sections/4X_Interprocess_Communications/42_Transports.md†L13-L28】
- **Tasks:** Importers/exporters, unit normalization, CRS audit logging, sample fixtures.
- **Acceptance hooks:**
  - ISOXML TaskData importer/exporter must retain 100% of task attributes across round-trip with ≤ 2 cm spatial error verified against canonical fixtures.
  - GeoTIFF/COG normalization must preserve raster statistics (mean, stdev) within 0.2% for 10 sample datasets after compression/decompression.
  - Interop audit log must capture CRS transformations and unit conversions for every import/export with structured entries consumed by telemetry pipelines.

### ADR-015 — Section control & grouping semantics
- **Owner:** Core Owner — Control Systems pod
- **Stage:** Drafting (target review window: 2025-11-08 week)
- **Dependencies:** ADR-008 (equipment hierarchy); ADR-027 (zone policies); ADR-007 (PoseStream diffing)
- **Scope:** Control graph for on/auto/off states, master group actions, overlapping groups, per-toolbar lookahead/overlap with overrides, and plugin hooks aligned with ADR-018.
- **Key decisions:** Arbitration priorities (manual > plugin > auto), toolbar-level lookahead/minimum overlap defaults with group/section overrides, safety interlocks, plugin extension hooks.
- **SRS alignment:** Control semantics captured in §09 and plugin integration in §12.【F:docs/SRS/sections/6X_Core_Domain_Services/61_Kinematics_Pose_Fusion.md†L16-L22】【F:docs/SRS/sections/9X_Frontends_Ops/94_Extensibility_Packaging_Updates.md†L6-L28】
- **Primary requirements:** R-CTRL-000…R-CTRL-005; R-EXT-120; R-EXT-134.【F:docs/SRS/sections/6X_Core_Domain_Services/61_Kinematics_Pose_Fusion.md†L16-L22】【F:docs/SRS/sections/9X_Frontends_Ops/94_Extensibility_Packaging_Updates.md†L21-L28】
- **Tasks:** Control engine updates, configuration UI, arbitration policy documentation, simulation/replay tests.
- **Acceptance hooks:**
  - Section control simulator must keep overlap error ≤ 8% against agronomic goldens across 10 replay fixtures with varying headland strategies.
  - Manual override priority must pre-empt plugin commands within 100 ms and log actor + duration in telemetry.
  - Safety interlock tests must assert sections fail closed when heartbeat loss > 300 ms, proven via integration harness.

### ADR-016 — Firmware/transport: variable-rate & layer PGNs
- **Owner:** AGiO Owner — Transport & Firmware pod
- **Stage:** Drafting (target review window: 2025-11-18 week)
- **Dependencies:** ADR-010 (layer registry); ADR-015 (section control semantics); ADR-031 (plugin governance)
- **Scope:** CAN/UDP message suite for layer definitions and feedback (E2/E1/E0/DF/E3/E4), node IDs, sequencing/timing, registry hash handshake, and timeout/heartbeat semantics.
- **Key decisions:** Payload packing, version bits, registry hash handshake (ADR-010), timeout/heartbeat strategy, compatibility with existing PGNs, degraded-mode behavior.
- **SRS alignment:** Communications & Transports (§03) and Hardware I/O (§06).【F:docs/SRS/sections/4X_Interprocess_Communications/42_Transports.md†L6-L28】【F:docs/SRS/sections/5X_Hardware_IO_Device_Layer/51_Sensor_Actuator_Abstractions.md†L6-L34】
- **Primary requirements:** R-COMM-010…R-COMM-032; R-HW-010…R-HW-023.【F:docs/SRS/sections/4X_Interprocess_Communications/42_Transports.md†L11-L28】【F:docs/SRS/sections/5X_Hardware_IO_Device_Layer/51_Sensor_Actuator_Abstractions.md†L9-L34】
- **Tasks:** AgIO bridge updates, firmware stubs, simulators, conformance tests, degraded-mode heartbeat handling.
- **Acceptance hooks:**
  - Firmware simulators must demonstrate end-to-end PGN exchange with ≤ 15 ms jitter at 20 Hz over CAN and ≤ 25 ms over UDP.
  - Registry hash handshake must fail-safe to degraded mode within 200 ms when mismatched hashes are detected, logging error codes for diagnostics.
  - Heartbeat watchdog tests must prove sections fail closed after 300 ms missed heartbeats and recover automatically when heartbeat resumes.

### ADR-017 — Profiles & kinematics (hitches, pivot tongues, multi-steer)
- **Owner:** Core Owner — Kinematics pod
- **Stage:** Drafting (target review window: 2025-12-05 week)
- **Dependencies:** ADR-008 (equipment hierarchy); ADR-007 (PoseStream); ADR-033 (guidance planner)
- **Scope:** Tractor/implement profiles, hitch linkage models, IMU/GNSS fusion, and which pose is “steered” for autosteer and mapping.
- **Key decisions:** Kinematic models, attachment points, toolbar placement priority, fusion of multiple pose sources, integration with ADR-008 placeholders.
- **SRS alignment:** Interprocess API (§07 geometry metadata) and Control (§09 lookahead).【F:docs/SRS/sections/4X_Interprocess_Communications/41_Inter_Application_API.md†L16-L28】【F:docs/SRS/sections/6X_Core_Domain_Services/61_Kinematics_Pose_Fusion.md†L16-L22】
- **Primary requirements:** R-GEO-001…R-GEO-002; R-CTRL-002.【F:docs/SRS/sections/4X_Interprocess_Communications/41_Inter_Application_API.md†L17-L18】【F:docs/SRS/sections/6X_Core_Domain_Services/61_Kinematics_Pose_Fusion.md†L18-L18】
- **Tasks:** Kinematics library, profile editor, sensor fusion hooks, validation fixtures.
- **Acceptance hooks:**
  - Kinematic simulations must track hitch articulation with ≤ 2 cm error over 100 m path when compared to motion capture baselines.
  - Multi-steer fusion must converge within 5 cycles after pose source switch and avoid oscillation > 1° yaw amplitude.
  - Profile editor must enforce attachment constraints preventing invalid geometry and export deterministic JSON validated against schema tests.

### ADR-018 — Plugin API & capability discovery
- **Owner:** Plugins Owner — Platform SDK pod
- **Stage:** Drafting (target review window: 2025-12-09 week)
- **Dependencies:** ADR-031 (plugin governance); ADR-007 (PoseStream); ADR-010 (layer registry); ADR-015 (section control)
- **Scope:** Contracts for publishing/consuming PoseStream deltas, declaring layers, control endpoints, hardware bindings, permissions, lifecycle governance, and UI contributions between Core, plugins, frontends, and AgIO.
- **Key decisions:** Manifest schema, permission model, lifecycle events, hot-plug strategy, capability discovery handshake, UI contribution model, AgIO as privileged plugin.
- **SRS alignment:** Extensibility & Plugins (§12), Communications (§03 plugin transport), Frontends (§05 UI contributions), Hardware I/O (§06 AgIO plugin), Control (§09 automation lifecycle).【F:docs/SRS/sections/9X_Frontends_Ops/94_Extensibility_Packaging_Updates.md†L6-L28】【F:docs/SRS/sections/4X_Interprocess_Communications/42_Transports.md†L20-L28】【F:docs/SRS/sections/9X_Frontends_Ops/91_UI_Shell_Layout.md†L20-L23】【F:docs/SRS/sections/5X_Hardware_IO_Device_Layer/51_Sensor_Actuator_Abstractions.md†L23-L34】【F:docs/SRS/sections/6X_Core_Domain_Services/61_Kinematics_Pose_Fusion.md†L16-L22】
- **Primary requirements:** R-EXT-000…R-EXT-134; R-COMM-030…R-COMM-032; R-FE-030…R-FE-032; R-HW-020…R-HW-023; R-CTRL-003…R-CTRL-005.【F:docs/SRS/sections/9X_Frontends_Ops/94_Extensibility_Packaging_Updates.md†L6-L28】【F:docs/SRS/sections/4X_Interprocess_Communications/42_Transports.md†L20-L28】【F:docs/SRS/sections/9X_Frontends_Ops/91_UI_Shell_Layout.md†L20-L23】【F:docs/SRS/sections/5X_Hardware_IO_Device_Layer/51_Sensor_Actuator_Abstractions.md†L23-L34】【F:docs/SRS/sections/6X_Core_Domain_Services/61_Kinematics_Pose_Fusion.md†L20-L22】
- **Tasks:** SDK, sample plugins (planter monitor, rate controller, autosteer), manifest tooling, permission gate, UI contribution host, health/lease services.
- **Acceptance hooks:**
  - Plugin capability handshake must complete within 400 ms and emit structured capability manifests consumed by Core registry.
  - Permission gate must enforce deny-by-default for privileged capabilities and log audit events with actor, plugin, and timestamp for 100% of access grants.
  - Sample plugins must pass contract conformance tests covering pose ingest, layer publish, and control endpoint usage with ≥ 95% coverage of SDK surface area.

### ADR-019 — Provenance, audit, and QA
- **Owner:** Core Owner — Data Governance pod
- **Stage:** Drafting (target review window: 2025-12-12 week)
- **Dependencies:** ADR-009 (persistence); ADR-023 (session model); ADR-013 (derived products)
- **Scope:** Provenance schema, job/session IDs, dataset hashes, quality flags, audit trail expectations across storage, UI, and exports.
- **Key decisions:** Provenance registry contents, chain-of-custody rules, QA badge surfacing, CI hash checks, linkage with ADR-023 sessions.
- **SRS alignment:** Telemetry & Health (§10) and Testing/CI (§11).【F:docs/SRS/sections/6X_Core_Domain_Services/64_Telemetry_Health.md†L6-L21】【F:docs/SRS/sections/9X_Frontends_Ops/96_Quality_Engineering_Release.md†L6-L19】
- **Primary requirements:** R-TH-010…R-TH-012; R-CI-020…R-CI-030; R-DATA-018…R-DATA-025.【F:docs/SRS/sections/6X_Core_Domain_Services/64_Telemetry_Health.md†L12-L16】【F:docs/SRS/sections/9X_Frontends_Ops/96_Quality_Engineering_Release.md†L17-L19】【F:docs/SRS/sections/3X_Data_Storage/32_Persistence_Formats.md†L20-L31】
- **Tasks:** Provenance writer, hash checks in CI, UI badges, audit log pipeline.
- **Acceptance hooks:**
  - Provenance records must hash-stamp PoseStream/tile artifacts with SHA-256 and match golden hashes across 100% of regression fixtures.
  - UI badges must reflect QA state transitions within 2 seconds of provenance updates and expose drill-down linking to dataset history.
  - Audit pipeline must export signed JSON logs for 30-day retention with ≤ 5% size overhead vs. raw event stream.

### ADR-020 — Determinism, replay & CI
- **Owner:** Core Owner — Verification & CI pod
- **Stage:** Drafting (target review window: 2025-12-16 week)
- **Dependencies:** ADR-007 (PoseStream); ADR-009 (TileStore); ADR-026 (performance budgets)
- **Scope:** Golden replays from PoseStream to identical tiles/exports with defined performance and size budgets.
- **Key decisions:** Hashing scheme, fixture format, pass/fail criteria, CI integration strategy, integration with ADR-026 performance budgets.
- **SRS alignment:** Testing & CI (§11) and Data Model (§08).【F:docs/SRS/sections/9X_Frontends_Ops/96_Quality_Engineering_Release.md†L6-L19】【F:docs/SRS/sections/3X_Data_Storage/32_Persistence_Formats.md†L17-L31】
- **Primary requirements:** R-CI-010, R-CI-013, R-CI-030; R-DATA-015; R-DATA-025.【F:docs/SRS/sections/9X_Frontends_Ops/96_Quality_Engineering_Release.md†L11-L19】【F:docs/SRS/sections/3X_Data_Storage/32_Persistence_Formats.md†L17-L31】
- **Tasks:** Replay tool, golden datasets, performance gates, hash verification tooling.
- **Acceptance hooks:**
  - Replay harness must execute 60-minute fixtures in < 12 minutes wall-clock on CI agents while maintaining determinism across OS variants.
  - Hash verification tooling must detect single-sample perturbations and fail CI within 1 minute of regression detection.
  - CI pipeline must publish determinism trend reports (hash deltas, runtime budgets) for every merge to `main` and retain 30-day history.

### ADR-021 — Timebase & clock sync
- **Owner:** Core Owner — Timing & Diagnostics pod
- **Stage:** Drafting (target review window: 2025-12-18 week)
- **Dependencies:** ADR-007 (PoseStream); ADR-020 (determinism & CI); ADR-016 (firmware transport)
- **Scope:** Define the canonical clock for PoseStream sequencing, GPS vs. system vs. PTP/RTK epochs, drift handling, sequence numbers, per-node latency budgets, and firmware timestamp reconciliation.
- **Key decisions:** Time authority selection, drift detection/compensation, sequence numbering, latency budget enforcement, diagnostics exposure.
- **SRS alignment:** Communications (§03 timebase), Control (§09 automation timing), Extensibility (§12 lifecycle metrics).【F:docs/SRS/sections/4X_Interprocess_Communications/42_Transports.md†L20-L28】【F:docs/SRS/sections/6X_Core_Domain_Services/61_Kinematics_Pose_Fusion.md†L16-L22】【F:docs/SRS/sections/9X_Frontends_Ops/94_Extensibility_Packaging_Updates.md†L23-L28】
- **Primary requirements:** R-COMM-020, R-COMM-040…R-COMM-042; R-CTRL-003; R-EXT-134.【F:docs/SRS/sections/4X_Interprocess_Communications/42_Transports.md†L17-L28】【F:docs/SRS/sections/6X_Core_Domain_Services/61_Kinematics_Pose_Fusion.md†L20-L22】【F:docs/SRS/sections/9X_Frontends_Ops/94_Extensibility_Packaging_Updates.md†L24-L28】
- **Tasks:** Timebase service, drift monitors, sequence enforcement in PoseStream, latency budget CI checks.
- **Acceptance hooks:**
  - Drift monitors must detect ≥ 2 ms/minute drift within 3 minutes and trigger resync routines validated against simulated clock skew.
  - PoseStream sequence enforcement must guarantee monotonic sequence numbers across distributed nodes with ≤ 1 frame reorder incidents in 24-hour soak tests.
  - Diagnostics UI must surface end-to-end latency histograms updating at least once per second with telemetry coverage of 95% of nodes.

### ADR-022 — CRS/units & precision policy
- **Owner:** Core Owner — Geospatial Standards pod
- **Stage:** Drafting (target review window: 2025-12-20 week)
- **Dependencies:** ADR-010 (layer registry); ADR-014 (interop); ADR-029 (mapping kernel)
- **Scope:** Establish project CRS defaults (WGS84 vs. per-field projections), on-disk numeric types, canonical units, and reprojection/rounding rules for imports/exports.
- **Key decisions:** Default CRS selection rules, per-field projection switching, numeric precision tiers (f16/f32/u16), rounding policy, unit normalization, reprojection audit logging.
- **SRS alignment:** Data Model & Storage (§08) and Interop (§03).【F:docs/SRS/sections/3X_Data_Storage/32_Persistence_Formats.md†L22-L31】【F:docs/SRS/sections/4X_Interprocess_Communications/42_Transports.md†L6-L28】
- **Primary requirements:** R-DATA-019…R-DATA-022; R-DATA-014; R-COMM-021.【F:docs/SRS/sections/3X_Data_Storage/32_Persistence_Formats.md†L22-L26】【F:docs/SRS/sections/4X_Interprocess_Communications/42_Transports.md†L17-L28】
- **Tasks:** CRS/units policy doc, conversion utilities, reprojection audit hooks, unit registry integration tests.
- **Acceptance hooks:**
  - CRS policy must choose projections with ≤ 1 cm distortion across 640-acre test extents and document fallback strategies.
  - Conversion utilities must pass 50-asset regression suite covering units (imperial/metric) with ≤ 0.1% conversion error.
  - Reprojection audit hooks must emit structured telemetry for every transform with coverage proven via automated import/export scenarios.

### ADR-023 — Session/job model & provenance graph
- **Owner:** Core Owner — Session Orchestration pod
- **Stage:** Drafting (target review window: 2025-12-09 week)
- **Dependencies:** ADR-030 (job sessions); ADR-019 (provenance); ADR-007 (PoseStream)
- **Scope:** Define sessions, jobs, equipment/implement selection, profile snapshotting, and how multiple PoseStreams and layers attach to a session for provenance (e.g., combine yield → fertilizer VR).
- **Key decisions:** Session lifecycle, job identifiers, profile snapshot storage, PoseStream attachment rules, provenance graph schema linking ADR-019 and ADR-013 outputs.
- **SRS alignment:** Backend Services (§04), Data Model (§08 lifecycle), Control (§09 automation states).【F:docs/SRS/sections/2X_System_Architecture/21_System_Decomposition_Boundaries.md†L6-L28】【F:docs/SRS/sections/3X_Data_Storage/32_Persistence_Formats.md†L28-L31】【F:docs/SRS/sections/6X_Core_Domain_Services/61_Kinematics_Pose_Fusion.md†L16-L22】
- **Primary requirements:** R-BE-000…R-BE-014; R-DATA-017…R-DATA-025; R-CTRL-003.【F:docs/SRS/sections/2X_System_Architecture/21_System_Decomposition_Boundaries.md†L6-L28】【F:docs/SRS/sections/3X_Data_Storage/32_Persistence_Formats.md†L19-L31】【F:docs/SRS/sections/6X_Core_Domain_Services/61_Kinematics_Pose_Fusion.md†L20-L22】
- **Tasks:** Session service, provenance graph schema, equipment/profile snapshot handling, UI wiring.
- **Acceptance hooks:**
  - Session lifecycle tests must prove create/resume/switch flows maintain referential integrity across PoseStreams and layers with zero orphaned references.
  - Snapshot storage must persist implement/equipment state with < 500 ms serialization latency and ≤ 5% storage overhead vs. raw config.
  - Provenance graph builder must emit DAGs validated against schema with cycle detection rejecting invalid attachments in integration tests.

### ADR-024 — Discovery & identity
- **Owner:** AGiO Owner — Connectivity pod
- **Stage:** Drafting (target review window: 2025-12-15 week)
- **Dependencies:** ADR-018 (plugin API); ADR-031 (plugin governance); ADR-016 (firmware transport)
- **Scope:** Node/rig IDs, capability handshake (ties to ADR-018), multi-controller scenarios (multiple ESP32s), user-visible naming, and permissioned discovery across transports.
- **Key decisions:** Identity schema, discovery watchers, capability handshakes, lease renewals, naming conventions, security model (mTLS/policy).
- **SRS alignment:** Communications (§03 plugin transport), Hardware I/O (§06 AgIO plugin), Extensibility (§12 lifecycle & security).【F:docs/SRS/sections/4X_Interprocess_Communications/42_Transports.md†L20-L28】【F:docs/SRS/sections/5X_Hardware_IO_Device_Layer/51_Sensor_Actuator_Abstractions.md†L23-L34】【F:docs/SRS/sections/9X_Frontends_Ops/94_Extensibility_Packaging_Updates.md†L23-L28】
- **Primary requirements:** R-COMM-030…R-COMM-032; R-HW-020…R-HW-023; R-EXT-130…R-EXT-134.【F:docs/SRS/sections/4X_Interprocess_Communications/42_Transports.md†L20-L28】【F:docs/SRS/sections/5X_Hardware_IO_Device_Layer/51_Sensor_Actuator_Abstractions.md†L23-L34】【F:docs/SRS/sections/9X_Frontends_Ops/94_Extensibility_Packaging_Updates.md†L23-L28】
- **Tasks:** Capabilities registry, discovery watcher, lease/heartbeat services, operator-facing identity management UI.
- **Acceptance hooks:**
  - Discovery handshake must complete within 2 seconds for 5-node rigs and populate identity registry with unique IDs verified across transports.
  - Security model must enforce mTLS authentication for remote nodes and reject unauthenticated clients with actionable telemetry.
  - Operator identity UI must allow rename/retire flows audited via automated UI tests with telemetry events emitted for every change.

## Linked backlog items

- **NX-113 — External layer ingest GA:** Tracks the GeoTIFF/COG and GeoJSON normalization flow referenced by ADR-010, ADR-014, and the Mapping SRS. The backlog entry now depends on ADR-044 for deterministic edits and ADR-051 for report exports.
- **NX-165 — ISOXML bridge general availability:** Connects ADR-014, the Rate Control plugin, and the ISOBUS Bridge documentation to deliver full task exchange workflows.
- **NX-170 — Job tasks GA:** Anchors TaskService orchestration (ADR-032) so Jobs can emit structured work orders once lifecycle and session contracts settle.

## Icebox & planned explorations

- **Soil & Lab Manager (Planned):** Placeholder for sampling workflows, lab import schemas, and agronomic recommendations surfaced through Profit and Crop Reports.
- **Map Composer & Print Studio (Planned):** Staging area for offline map layout tools that export PDF/GeoJSON packets from ADR-051 templates.
- **3D Terrain & Drainage (Planned):** Captures LiDAR/RTK elevation ingest, drainage modeling, and 3D rendering requirements feeding future guidance features.

### ADR-025 — Data lifecycle & retention
- **Owner:** Core Owner — Storage Operations pod
- **Stage:** Drafting (target review window: 2025-12-22 week)
- **Dependencies:** ADR-009 (persistence); ADR-019 (provenance); ADR-020 (determinism & CI)
- **Scope:** On-device vs. removable storage policies, compaction triggers, background re-encode workflows (LZ4→Zstd), export/archive rules, privacy flags, and retention SLAs.
- **Key decisions:** Retention windows, compaction cadence, archival/export flows, privacy tagging, storage budgeting per layer.
- **SRS alignment:** Data Model (§08 lifecycle), Backend Services (§04 health budgets), Telemetry (§10 retention).【F:docs/SRS/sections/3X_Data_Storage/32_Persistence_Formats.md†L28-L31】【F:docs/SRS/sections/2X_System_Architecture/21_System_Decomposition_Boundaries.md†L13-L20】【F:docs/SRS/sections/6X_Core_Domain_Services/64_Telemetry_Health.md†L12-L16】
- **Primary requirements:** R-DATA-013, R-DATA-023…R-DATA-025; R-BE-013; R-TH-012.【F:docs/SRS/sections/3X_Data_Storage/32_Persistence_Formats.md†L15-L31】【F:docs/SRS/sections/2X_System_Architecture/21_System_Decomposition_Boundaries.md†L13-L20】【F:docs/SRS/sections/6X_Core_Domain_Services/64_Telemetry_Health.md†L12-L16】
- **Tasks:** Retention planner, compaction scheduler, background maintenance jobs, privacy flag propagation, export/archive tooling.
- **Acceptance hooks:**
  - Retention planner must enforce configurable retention windows and prove compliance via automated tests covering 30, 90, 365-day policies.
  - Compaction scheduler must maintain disk utilization ≤ 70% under heavy ingest workloads while keeping maintenance CPU ≤ 15% average.
  - Privacy flag propagation must redact sensitive fields in exports with zero leakage verified via automated diffing of sanitized vs. raw datasets.

### ADR-026 — Performance budgets
- **Owner:** Core Owner — Performance Engineering pod
- **Stage:** Drafting (target review window: 2026-01-09 week)
- **Dependencies:** ADR-020 (determinism & CI); ADR-021 (timebase); ADR-025 (data lifecycle)
- **Scope:** CPU/IO targets (pose ingest < X ms, tile flush < Y ms), file size budgets (MB/acre/layer), renderer FPS guarantees with N active layers, and CI guardrails tying to ADR-020 replays.
- **Key decisions:** Reference hardware targets, instrumentation strategy, alert thresholds, integration with CI and telemetry dashboards, cross-component budgeting (Core, UI, plugins).
- **SRS alignment:** Communications (§03 latency), Backend Services (§04 health metrics), Telemetry (§10 observability), Data Model (§08 maintenance).【F:docs/SRS/sections/4X_Interprocess_Communications/42_Transports.md†L12-L28】【F:docs/SRS/sections/2X_System_Architecture/21_System_Decomposition_Boundaries.md†L13-L20】【F:docs/SRS/sections/6X_Core_Domain_Services/64_Telemetry_Health.md†L12-L16】【F:docs/SRS/sections/3X_Data_Storage/32_Persistence_Formats.md†L28-L31】
- **Primary requirements:** R-COMM-012, R-COMM-042; R-BE-013; R-DATA-024; R-TH-010…R-TH-012.【F:docs/SRS/sections/4X_Interprocess_Communications/42_Transports.md†L15-L28】【F:docs/SRS/sections/2X_System_Architecture/21_System_Decomposition_Boundaries.md†L13-L20】【F:docs/SRS/sections/3X_Data_Storage/32_Persistence_Formats.md†L29-L31】【F:docs/SRS/sections/6X_Core_Domain_Services/64_Telemetry_Health.md†L12-L16】
- **Tasks:** Performance budget doc, instrumentation hooks, CI alerts, telemetry dashboards, regression fixtures.
- **Acceptance hooks:**
  - Budget catalog must publish explicit CPU/IO/FPS thresholds for reference hardware and export machine-readable JSON consumed by CI.
  - Instrumentation must capture 95th percentile latency metrics and push to telemetry dashboards within 60 seconds of run completion.
  - Alerting pipeline must page on-call when budgets exceed thresholds for two consecutive CI runs, verified via synthetic breach tests.
## How to use this tracker
- **Before drafting an ADR**, confirm the associated requirements are satisfied or add missing ones to the SRS within the relevant section (Sections 03, 07–12 already include new requirement IDs for this program).【F:docs/SRS/sections/4X_Interprocess_Communications/42_Transports.md†L6-L28】【F:docs/SRS/sections/4X_Interprocess_Communications/41_Inter_Application_API.md†L6-L28】【F:docs/SRS/sections/3X_Data_Storage/32_Persistence_Formats.md†L6-L31】【F:docs/SRS/sections/6X_Core_Domain_Services/61_Kinematics_Pose_Fusion.md†L8-L22】【F:docs/SRS/sections/6X_Core_Domain_Services/64_Telemetry_Health.md†L6-L17】【F:docs/SRS/sections/9X_Frontends_Ops/96_Quality_Engineering_Release.md†L6-L19】【F:docs/SRS/sections/9X_Frontends_Ops/94_Extensibility_Packaging_Updates.md†L6-L28】
- **During implementation**, link work items to the corresponding NX task in
  [`tasks.md`](../../tasks.md) and update this file with progress notes or additional
  prerequisites discovered by prototypes or field feedback.【F:tasks.md†L131-L149】
- **When an ADR is approved**, move it to the adopted table above and ensure the SRS section references are updated to point to the final record.

### Global acceptance hooks for PoseStream, layer, and control proposals
- **Deterministic replay parity:** TileStore + PoseStream fixtures must hash-identical across two consecutive runs on the CI reference hardware (Intel i5-8365U, Windows 11) and the Linux build agent (Ampere Altra), with tolerance ≤ 1 hash bucket mismatch across 100k samples.
- **Pose ingest CPU budget:** Combined PoseStream ingestion, SectionState diffing, and TileStore persistence must consume ≤ 18% of a single core on the reference i5-8365U at 20 Hz input, and ≤ 35% aggregate across four worker threads on the Ampere Altra arm64 host.
- **ISOXML/interop guardrail:** ISOXML TaskData, GeoTIFF (COG), and GeoPackage round-trips must maintain spatial error ≤ 5 cm RMS and attribute drift ≤ 0.5% for regression fixtures defined in `/tools/schemas/samples/vr/`.
- **Crash recovery:** Each ADR’s implementation must pass the composite crash-recovery replay where Core is terminated mid-ingest and resumes with no more than one duplicated SectionState transition and no lost zone-mask updates.
