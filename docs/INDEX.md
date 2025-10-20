# Nexus Documentation Index

## Platform Areas

- [AgIO subsystem overview](AgIO/README.md) — bridge services, transport adapters, and CM5 deployment entry points.
- [Core runtime overview](Core/README.md) — orchestration services, data flow, and operational guardrails.
- [UI platform overview](ui/README.md) — Avalonia shells, modernization roadmap, and styling governance.

## Architecture Decisions

- [ADR Roadmap](ADR/ADR-roadmap.md) — status, dependencies, and icebox items for the 2025 program.
- [ADR-040 — Season Organizers](ADR/ADR-040_SeasonOrganizers.md)
- [ADR-041 — Job Sessions](ADR/ADR-041_JobSessions.md)
- [ADR-043 — Multi-Field Job Envelopes](ADR/ADR-043_MultiFieldJobEnvelopes.md)
- [ADR-044 — Zone & Layer Drawing Framework](ADR/ADR-044_ZoneDrawingFramework.md)
- [ADR-045 — Crop Type Plugin & Layers](ADR/ADR-045_CropTypePlugin.md)
- [ADR-046 — Genetics Plugin & Layers](ADR/ADR-046_GeneticsPlugin.md)
- [ADR-047 — Live Telemetry Mesh](ADR/ADR-047_LiveTelemetryMesh.md)
- [ADR-048 — RadioBridge](ADR/ADR-048_RadioBridge.md)
- [ADR-049 — Yield & Analytics Plugin](ADR/ADR-049_YieldPlugin.md)
- [ADR-050 — Cost & Profit Plugin](ADR/ADR-050_CostProfitPlugin.md)
- [ADR-051 — Report Builder & Export System](ADR/ADR-051_ReportBuilder.md)
- [ADR-069 — Guidance Orchestrator plugin](ADR/ADR-069_GuidanceOrchestrator.md)

## System Requirements Specification

<<<<<<< HEAD
- [§02 Data Model — Farm → Field & Season → Job → Session](SRS/sections/3X_Data_Storage/31_Domain_Data_Model.md)
- [§03 Job Lifecycle & Session Management](SRS/sections/6X_Core_Domain_Services/62_Job_Lifecycle.md)
- [§03 Communications & Transports](SRS/sections/4X_Interprocess_Communications/42_Transports.md)
- [§04 Mapping & Layer Governance](SRS/sections/7X_Mapping_Geospatial/72_Mapping_Layers_Plugin.md)
- [§05 Frontends](SRS/sections/9X_Frontends_Ops/91_UI_Shell_Layout.md)
- [§09 Control & Automation](SRS/sections/6X_Core_Domain_Services/61_Kinematics_Pose_Fusion.md)
- [§19 Guidance Orchestrator Plugin](SRS/sections/8X_Guidance/81_Guidance_Orchestrator.md)
=======
- [§02 Data Model — Farm → Field & Season → Job → Session](SRS/sections/3X/31_Domain_Data_Model.md)
- [§03 Job Lifecycle & Session Management](SRS/sections/6X/62_Job_Lifecycle.md)
- [§03 Communications & Transports](SRS/sections/4X/42_Transports.md)
- [§04 Mapping & Layer Governance](SRS/sections/7X/72_Mapping_Layers_Plugin.md)
- [§05 Frontends](SRS/sections/9X/91_UI_Shell_Layout.md)
- [§09 Control & Automation](SRS/sections/6X/61_Kinematics_Pose_Fusion.md)
- [§19 Guidance Orchestrator Plugin](SRS/sections/8X/81_Guidance_Orchestrator.md)
>>>>>>> origin/develop

## Guidance Orchestrator Delivery

- [01 — Live Field Builder](guidance/01_live-field-builder.md)
- [02 — Fields2Cover Planner Integration](guidance/02_fields2cover-orchestrator.md)
- [03 — Path Catalog & Sequencer](guidance/03_path-catalog-and-sequencer.md)
- [04 — Execution & Autosteer Contracts](guidance/04_execution-and-autosteer-contracts.md)
- [05 — Refresh Policies & Hysteresis](guidance/05_refresh-policies-and-hysteresis.md)
- [06 — Observability & Telemetry](guidance/06_observability-telemetry.md)
- [99 — Glossary](guidance/99_glossary.md)

## Plugin Guides

- **Architecture & SDK**
  - [Zip Plugin Architecture](plugins/architecture.md)
  - [Core Integration Guide](../Nexus SourceCode/src/Aog.Core/PLUGINS.md)
  - [UI Integration Guide](../Nexus SourceCode/src/Aog.UI.Avalonia/PLUGINS.md)
- **Official Plugin Cards**
  - [Catalogue](plugins/official/README.md) — concise cards for AutoSteer, Sections, Telemetry Logging, Variable Mapping, and more.
- **Legacy Briefs & Deep Dives**
  - [Mapping](plugins/Mapping.md)
  - [Variable Mapping / Prescription](plugins/VariableMapping.md)
  - [Rate Control](plugins/RateControl.md)
  - [Genetics](plugins/Genetics.md)
  - [Yield & Analytics](plugins/Yield.md)
  - [Profit](plugins/Profit.md)
  - [Multi-Machine](plugins/MultiMachine.md)
  - [ISOBUS Bridge](plugins/IsobusBridge.md)
  - [Telemetry Logging](plugins/TelemetryLogging.md)
  - [Replay](plugins/Replay.md)
  - [File I/O](plugins/FileIO.md)
  - Planned: [Soil & Lab Manager](plugins/SoilLab.md), [Map Composer & Print Studio](plugins/MapComposer.md), [3D Terrain & Drainage](plugins/Terrain3D.md)

## Schemas & Examples

- [Season.v1.json](../schemas/Season.v1.json)
- [Session.v1.json](../schemas/Session.v1.json)
- [Job.v1.json](../schemas/Job.v1.json)
- [Layer.v1.json](../schemas/Layer.v1.json)
- [LayerEditEvent.v1.json](../schemas/LayerEditEvent.v1.json)
- [schemas/examples](../schemas/examples) — sample payloads for validation

## Reference

- [CRS normalization matrix](reference/crs-normalization-matrix.md) — canonical storage,
  processing, and audit expectations per ADR-022.
- [Official plugin bundle capability matrix](reference/official-bundle-capability-matrix.md) —
  lease posture and capability coverage for ADR-031 governance.
- [Metadata-driven UI style guide](ui/metadata-driven-ui-style-guide.md) — layout and
  theming guidance for ADR-034 dashboards, inspectors, and legends.
- [Report template catalog](reference/report-template-catalog.md) —
  versioned manifests, section contributors, and export governance for ADR-051.

## Contribution Guides

- [AGENTS.md](../AGENTS.md) — repository conventions and task workflow.
- [Plugin contribution guide](CONTRIBUTING-PLUGINS.md)
- [tasks.md](../tasks.md) — active backlog with NX identifiers.

## Operational Playbooks & How-To Guides

- [Runtime baseline enforcement](support/dotnet-runtime-baseline.md) — .NET 8 guardrails and review checklist.
- [Avalonia run modes](ui/avalonia-run-modes.md) — CompanionRemote, LocalInProc, and LocalOutOfProc configuration.
- [Companion metadata parity](howto/companion-metadata-parity.md) — snapshot contract powering ADR-034 remote clients.
- [Guidance lane publishing contracts](howto/guidance-lane-contracts.md) — ADR-033 lane geometry and preview payloads.
- [AOG-Link transport rollout](AgIO/aog-link-transport-rollout.md) — Ethernet, RS-485, and CAN staging guidance.
- [AOG-Link bridge architecture](AgIO/aog-link-bridge-architecture-guide.md) — visual layer breakdown and operator-facing explainer.
- [Stanley controller parity harness](howto/stanley-controller-parity.md) — deterministic regression checks for the ported controller.
- [Firmware-in-loop stability validation](howto/firmware-in-loop-stability.md) — dynamic look-ahead and constraint regression slice.
- [Performance budget telemetry dashboards](Core/performance-budget-telemetry-dashboards.md) — ADR-026 dashboard provisioning and guardrails.
- [RadioBridge provisioning kit](howto/radio/radiobridge-provisioning.md) — provisioning workflow for ELRS/LoRa bridges (NX-244).
- [Season/session migration playbook](howto/season-session-migration-playbook.md) — ADR-040/041 rollout playbook.
- [Mesh provisioning runbook](howto/mesh-provisioning-runbook.md) — ADR-047/048 connectivity rollout.
- [Mesh retention & privacy operations guide](howto/mesh-retention-privacy-operations-guide.md) — ADR-047 retention planner playbook.
- [Global guardrail regression checks](howto/global-guardrails.md) — retention, performance, and crash acceptance automation.
- [Linux Core operations & observability](Core/linux-core-operations-playbook.md) — NX-462/463 systemd packaging, health signals, and replay validation.
- [Composite simulation fabric GA](scenarios/composite-simulation-fabric.md) — SimClock/SimBus validation steps.
- [gRPC contract governance](qa/grpc-contract-governance.md) — protobuf review and release gating.
- [Plugin lease & manifest governance](plugins/plugin-lease-manifest-governance.md) — ADR-018 compliance checklist.
- [Plugin manifest governance playbook](howto/plugin-manifest-governance.md) — ADR-031 release workflow and artefacts.
- [Device Manager compatibility dashboard](howto/device-manager-compatibility-dashboard.md) — ADR-031 bundle health UI guide.
- [Weather compliance export playbook](howto/weather-compliance-export.md) — ADR-053 reporting workflow for regulatory bundles.
- [Plugin QA handshake checklist](qa/plugin-qa-handshake.md) — ADR-031 manifest validation process.
- [Mesh security and penetration tests](qa/mesh-security-penetration-tests.md) — ADR-047/048 security validation plan.
