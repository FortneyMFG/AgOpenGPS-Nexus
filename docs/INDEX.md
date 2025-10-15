# Nexus Documentation Index

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

## System Requirements Specification

- [§02 Data Model — Farm → Field & Season → Job → Session](SRS/sections/02_DataModel.md)
- [§03 Job Lifecycle & Session Management](SRS/sections/03_JobLifecycle.md)
- [§03 Communications & Transports](SRS/sections/03_Comm_Transports.md)
- [§04 Mapping & Layer Governance](SRS/sections/04_MappingLayers.md)
- [§05 Frontends](SRS/sections/05_Frontends.md)
- [§09 Control & Automation](SRS/sections/09_Control_Automation.md)

## Plugin Guides

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
- [Metadata-driven UI style guide](reference/metadata-driven-ui-style-guide.md) — layout and
  theming guidance for ADR-034 dashboards, inspectors, and legends.
- [Report template catalog](reference/report-template-catalog.md) —
  versioned manifests, section contributors, and export governance for ADR-051.

## Contribution Guides

- [AGENTS.md](../AGENTS.md) — repository conventions and task workflow.
- [Plugin contribution guide](CONTRIBUTING-PLUGINS.md)
- [tasks.md](../tasks.md) — active backlog with NX identifiers.

## Operational Playbooks & How-To Guides

- [Runtime baseline enforcement](support/dotnet-runtime-baseline.md) — .NET 8 guardrails and review checklist.
- [Avalonia run modes](howto/avalonia-run-modes.md) — CompanionRemote, LocalInProc, and LocalOutOfProc configuration.
- [Companion metadata parity](howto/companion-metadata-parity.md) — snapshot contract powering ADR-034 remote clients.
- [AOG-Link transport rollout](howto/aog-link-transport-rollout.md) — Ethernet, RS-485, and CAN staging guidance.
- [Performance budget telemetry dashboards](howto/performance-budget-telemetry-dashboards.md) — ADR-026 dashboard provisioning and guardrails.
- [RadioBridge provisioning kit](howto/radio/radiobridge-provisioning.md) — provisioning workflow for ELRS/LoRa bridges (NX-244).
- [Season/session migration playbook](howto/season-session-migration-playbook.md) — ADR-040/041 rollout playbook.
- [Composite simulation fabric GA](scenarios/composite-simulation-fabric.md) — SimClock/SimBus validation steps.
- [gRPC contract governance](qa/grpc-contract-governance.md) — protobuf review and release gating.
- [Plugin lease & manifest governance](plugins/plugin-lease-manifest-governance.md) — ADR-018 compliance checklist.
- [Plugin manifest governance playbook](howto/plugin-manifest-governance.md) — ADR-031 release workflow and artefacts.
- [Device Manager compatibility dashboard](howto/device-manager-compatibility-dashboard.md) — ADR-031 bundle health UI guide.
- [Plugin QA handshake checklist](qa/plugin-qa-handshake.md) — ADR-031 manifest validation process.
