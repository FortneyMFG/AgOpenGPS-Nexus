# Extensibility & Plugins (Status: collecting proposals)

## Problem statement
Outline how developers extend AgOpenGPS (custom tools, integrations, UI modules) without forking core code, and what constraints exist today.

## Requirements (from contributors)
- R-EXT-000 (MUST, current-AgOpenGPS): Continue exposing shared libraries (`AgLibrary`, `AgOpenGPS.Core`) that downstream executables reference for customization.【F:SourceCode/GPS/AgOpenGPS.csproj†L32-L48】【F:SourceCode/AgIO/Source/AgIO.csproj†L23-L33】
- R-EXT-001 (MUST, current-AgOpenGPS): Keep multiple companion executables (AgIO, ModSim, GPS_Out, Keypad, AgDiag) available for extension via source modifications.【F:SourceCode/AgOpenGPS.sln†L6-L35】
- R-EXT-002 (SHOULD): Define a plugin boundary (UI, PGN handlers, analytics) that avoids shipping forked executables for every variation.
- R-EXT-003 (SHOULD): Provide guidelines or templates for third-party modules so they integrate with packaging and settings.
- R-EXT-010 (SHOULD, proposed-variable-layer): Allow plugins/modules to register new telemetry layers via dependency injection and published ID registries so they appear in dashboards without core code edits.【F:docs/SRS/options/O-BACKEND-4_LayerControllers.md†L19-L33】【F:docs/SRS/options/O-API-5_VersionedLayerSchemas.md†L32-L49】
- R-EXT-080 (MUST, zone editing contracts): Require plugins that author spatial layers to integrate with LayerEditService (`registerEditableLayer`, `onLayerStartEdit`, `onFeatureCommit`, `onLayerUndo/Redo`) so Core enforces consistent geometry handling and provenance.【F:docs/ADR/ADR-044_ZoneDrawingFramework.md†L29-L74】
- R-EXT-081 (MUST, context bus): Publish strongly typed lifecycle events (`onFarmLoaded`, `onSeasonLoaded`, `onJobLoaded`, `onContextChanged`, `onSessionStart/Pause/Resume/End`, `onSessionWeatherUpdate`) and SDK helpers so plugins subscribe deterministically without bespoke event plumbing.【F:docs/SRS/sections/03_JobLifecycle.md†L18-L74】
- R-EXT-082 (SHOULD, analytics APIs): Provide shared query surfaces (`getYieldByCrop`, `getYieldByVariety`, `getPreviousCrop`, profit summaries) so plugins consume crop, genetics, yield, and profit analytics without duplicating aggregation logic.【F:docs/ADR/ADR-045_CropTypePlugin.md†L57-L71】【F:docs/ADR/ADR-049_YieldPlugin.md†L21-L59】【F:docs/ADR/ADR-050_CostProfitPlugin.md†L21-L52】
- R-EXT-083 (SHOULD, financial hooks): Allow plugins to submit `CostRecord` entries, consume profit overlays, and subscribe to cost-change events for downstream automation (e.g., invoice exports).【F:docs/ADR/ADR-050_CostProfitPlugin.md†L21-L52】
- R-EXT-084 (SHOULD, report builder integration): Expose `registerReportSection()` and `onReportGenerate()` hooks with dependency declarations so plugins contribute report content safely.【F:docs/ADR/ADR-051_ReportBuilder.md†L21-L52】
- R-EXT-004 (COULD): Support sandboxing or capability declarations for plugins to protect critical operations.
- R-EXT-011 (SHOULD, governance): Establish contribution governance for community plugins (review queues, namespace reservation, security vetting) before enabling DI registration so unsafe modules cannot bypass safety-critical boundaries.
- R-EXT-020 (SHOULD, official-bundle): Ship first-party capabilities (desktop UI, AgIO bridge, gauges, variable-rate controllers) as separately versioned plugins that install alongside core but can be disabled for headless or minimal deployments.
- R-EXT-030 (SHOULD, proposed-composite-sim): Allow plugins to register simulation providers that consume/publish typed topics through a shared SimBus so they can inject deterministic test data without special-case wiring in Core.
- R-EXT-031 (SHOULD, proposed-composite-sim): Require simulation providers to respect the authoritative SimClock and seeded RNG so multi-plugin scenarios replay identically across machines and CI lanes.
- R-EXT-100 (MUST, NX-099): Ship an ISOBUS communications plugin that registers as a transport provider within the managed plugin manifest so CAN frames and PGN payloads flow onto the shared SimBus and hardware routing graph without bespoke wiring.【F:docs/SRS/sections/06_Hardware_IO.md†L16-L54】
- R-EXT-101 (MUST, NX-099): Normalize ISO 11783 condensed work state, gauge, and diagnostics PGNs (289–291, 65279, 65242, 65263, 65265, 65266, 64964) into the Layer/Telemetry registries so section control, machine status, and gauge widgets consume consistent topic IDs regardless of physical bus source.【F:docs/SRS/references/ISOBUS_Section_Control.md†L1-L33】【F:docs/SRS/sections/15_Engine_Machine_Gauges.md†L4-L134】
- R-EXT-102 (SHOULD, NX-099): Surface plugin-provided diagnostics (bus load, address-claim status, last-PGN timestamps, fault codes) through the standard plugin health contract so the desktop UI and headless monitors can present actionable alerts without bespoke code paths.【F:docs/SRS/sections/16_Plugin_Packaging_Updates.md†L7-L58】
- R-EXT-103 (SHOULD, NX-099): Offer declarative mapping metadata that links configured implements to their ISOBUS Device Descriptor Identifiers (DDIs) and section counts, enabling UI manifests to auto-populate section control panels and gauge groups with accurate labels and units.【F:docs/SRS/appendices/GaugeId_Registry.md†L12-L20】【F:docs/SRS/references/ISOBUS_Section_Control.md†L1-L33】
- R-EXT-120 (MUST, capability discovery): Require plugin manifests to declare produced/consumed PoseStream topics, layer definitions, control endpoints, and hardware bindings so Core can authorize and route capabilities deterministically.
- R-EXT-150 (MUST, NX-134): Maintain an authoritative dependency register, manifest catalog, and compatibility matrix for all first-party plugins so operators and Core can validate stack composition before activation.【F:docs/plugins/nexus-plugin-dependency-map.md†L1-L421】
- R-EXT-151 (MUST, equivalency profiles): Manifest `provides.capabilities` and `provides.profiles` entries must advertise contract versions, optional feature flags, and conformance attestations so alternate providers can prove compatibility without bespoke aliases.【F:docs/plugins/nexus-plugin-dependency-map.md†L36-L86】【F:docs/ADR/ADR-031-official-plugin-bundle.md†L25-L74】
- R-EXT-152 (MUST, dependency semantics): Loader enforces `requires.capabilities`, `requires.profiles`, and relationship hints (`peerOf`, `conflictsWith`, `replaces`, `extends`) when solving the plugin graph so peer agreements, migrations, and decorator chains remain deterministic.【F:docs/plugins/nexus-plugin-dependency-map.md†L88-L143】【F:docs/ADR/ADR-031-official-plugin-bundle.md†L49-L83】
- R-EXT-153 (SHOULD, provider policy): Publish a deterministic multi-provider selection policy (admin pins → site policy → highest compatible version → provider hints) with audit logs so operators understand which plugin satisfied each capability edge.【F:docs/plugins/nexus-plugin-dependency-map.md†L144-L205】

### R-EXT — Plugin lifecycle & security
- R-EXT-130 (MUST, lifecycle states): Standardize plugin lifecycle states (discovered, verified, started, healthy, degraded, stopped) with observable transitions so Core can supervise hot-plug workflows and automated recovery.
- R-EXT-131 (MUST, permission gate): Enforce a policy-driven permission gate (pose.read, section.command, storage.write, io.device, config.manage) during plugin registration so untrusted modules cannot bypass safety-critical services.
- R-EXT-132 (SHOULD, manifest signing): Support optional signing/verification of plugin bundles with operator overrides documented for air-gapped rigs.
- R-EXT-133 (SHOULD, remote plugins): Document requirements for remote plugin connections (mTLS, leases, restart policies) so edge clusters and cab computers behave consistently.
- R-EXT-134 (SHOULD, audit trails): Capture plugin health, command history, and configuration edits in a provenance stream to satisfy Section 09 control audit requirements.
- R-EXT-140 (MUST, job lifecycle hooks): Expose plugin lifecycle callbacks (`onJobOpen`, `onJobSave`, `onJobClose`, `onJobImport`) gated by `jobs.lifecycle` permissions so importers, analytics, and automation modules participate deterministically in job workflows.【F:docs/ADR/ADR-030-field-job-sessions.md†L47-L86】
- R-EXT-141 (SHOULD, preset orchestration): Provide SDK helpers and permissions for presets/layout services so plugins can request preset applications, contribute validators, and register dependency graph observers without bypassing Core arbitration.【F:docs/ADR/ADR-032-presets-and-layout-linking.md†L7-L34】
- R-EXT-142 (SHOULD, Drive-In providers): Allow plugins to register Drive-In discovery sources and job importers while Core enforces schema validation and provenance logging for contributed assets.【F:docs/ADR/ADR-030-field-job-sessions.md†L47-L86】
- R-EXT-143 (SHOULD, task orchestration API): Standardize background task submission, progress streaming, and retry semantics so preset-related preparation work remains observable and restartable across plugins and UI shells.【F:docs/ADR/ADR-032-presets-and-layout-linking.md†L17-L34】

## Options
- O-EXT-0: Status quo — Extend by modifying source projects and rebuilding.
- O-EXT-1: Introduce a managed plugin API (MEF/AssemblyLoadContext) for UI and logic extensions.
- O-EXT-2: Expose scripting hooks (Python/Lua) for automation and custom workflows.
- O-EXT-3: Offer gRPC/webhook extension points for out-of-process services.
- O-EXT-4: Package optional modules as NuGet packages consumed by the desktop apps.
- O-EXT-5: Managed plugin manifests using `AssemblyLoadContext` + shared gRPC contracts so plugins run identically on Windows and Linux.【F:docs/SRS/options/O-STACK-1_DotNet8Avalonia.md†L13-L47】

## Comparison (quick matrix)
| Option | Pros | Cons | Risks | Borrow from existing |
|---|---|---|---|---|
| O-EXT-0 | Simple, aligns with current repos | Requires full rebuilds | Fork divergence | Shared libraries |
| O-EXT-1 | Controlled extension points | Loader/security complexity | Plugin crashes impact runtime | MEF patterns |
| O-EXT-2 | Rapid prototyping | Performance + safety concerns | Scripts can break guidance | Existing automation | 
| O-EXT-3 | Language-agnostic | Requires transport layer | Network failures | PGN bridge |
| O-EXT-4 | Versioned distribution | Package management overhead | Dependency hell | NuGet ecosystem |
| O-EXT-5 | Consistent plugin surface across OSes, reuse C# skillset | Requires loader governance + ABI testing | Plugin bugs propagate via shared contracts | .NET 8 + Avalonia stack |

## Evaluation criteria
Safety, maintainability, ease for contributors, performance impact, packaging complexity.

## Current sentiment
- Developers fork today; we need a plugin surface that honors safety-critical boundaries while reducing merge burden.
- Layer metadata + ID registries are expected to become the bridge for safe third-party modules once DI hooks exist.【F:docs/SRS/options/O-BACKEND-4_LayerControllers.md†L34-L47】【F:docs/SRS/options/O-API-5_VersionedLayerSchemas.md†L32-L64】
- The unified .NET 8 plugin runtime (shared gRPC contracts + manifest loader) is the leading proposal because it supports cross-platform simulation, replay, and device plugins without per-OS rewrites.【F:docs/SRS/options/O-STACK-1_DotNet8Avalonia.md†L9-L79】
- Treating the UI and advanced agronomy modules as “official plugins” keeps the default install familiar while letting operators toggle them off to run core services headless.【F:docs/SRS/sections/16_Plugin_Packaging_Updates.md†L7-L58】
- Contributors want the simulation surface to live inside the plugin contract so device, agronomy, and automation modules can share deterministic scenarios without recompiling Core or duplicating ModSim logic.
- The official plugin dependency map now enumerates cross-domain hard/soft requirements, manifest metadata, and compatibility ranges to guide bundle validation and satisfy R-EXT-150.【F:docs/plugins/nexus-plugin-dependency-map.md†L1-L421】

## Composite simulation blueprint

- **SimClock** — fixed-step controller (default 10 ms) exposed over plugin APIs so play/pause/seek/speed adjustments from the frontend drive every simulator in lockstep.
- **SimBus** — typed publish/subscribe channel with last-value caching for topics such as pose, IMU, sections, and planter row status; plugins publish fake device readings and consume peer outputs through this bus.
- **Source routing** — Core-owned priority rules pick between hardware, simulation, and replay producers per topic, allowing hardware inputs (e.g., manual section switch) to override simulator outputs without tearing down the scenario.
- **Plugin sim providers** — plugins declare the topics they produce/consume, configure scenarios, and start/stop alongside the SimClock so agronomy and steering models remain modular.
  - Manifests register their declared providers with the simulation catalog at load time so the shared registry always lists plugin outputs and wiring metadata.
- **Hardware-in-the-loop passthrough** — real inputs flow onto the same SimBus topics at higher priority so mixed rigs stay predictable while training operators.

## Proposed plugin tiers

| Tier | Examples | Notes |
|---|---|---|
| Core services | Headless navigation, guidance solver, settings store | Remains lightweight, always installed. |
| First-party plugins | Default desktop UI, AgIO transport manager, gauges, variable-rate controllers, ISO-BUS tooling | Bundled by default, versioned independently, can be disabled to run headless. |
| Community plugins | Enterprise dashboards, specialized device support, analytics | Distributed via catalog governance and permission review. |

## ISOBUS communications plugin expectations

- **Manifest & lifecycle** — The plugin advertises itself as an `IsoBusTransport` provider, exposes configuration for CAN adapter selection and PGN filters, and follows the shared plugin lifecycle hooks so it can be hot-reloaded in desktop and headless deployments.【F:docs/SRS/sections/06_Hardware_IO.md†L16-L54】
- **Routing integration** — PGNs decoded by the plugin populate LayerDefinitions, SectionControllers, and Gauge topics that already exist for UDP transports, ensuring mixed-transport rigs see a single authoritative topic namespace.【F:docs/SRS/references/ISOBUS_Section_Control.md†L1-L33】【F:docs/SRS/sections/15_Engine_Machine_Gauges.md†L4-L134】
- **Diagnostics & UI hooks** — Health metrics (bus utilization, last-frame timestamps, device address-claim status) and implement metadata (section labels, DDIs, rate controller presence) feed into the plugin health UI surfaces defined in Section 16, enabling dashboards to highlight wiring faults or mismatched implement profiles.【F:docs/SRS/sections/16_Plugin_Packaging_Updates.md†L7-L58】【F:docs/SRS/appendices/GaugeId_Registry.md†L12-L20】
- **Simulation parity** — When the plugin runs in simulation mode it must respect the SimClock and deterministic replay requirements so PGN playback sequences can be validated alongside hardware captures, aligning with the composite simulation blueprint outlined above.【F:docs/SRS/sections/12_Extensibility_Plugins.md†L48-L80】

## Open questions
- Which features are safe to expose via scripting vs. compiled plugins?
- How do we version plugin APIs alongside firmware expectations?

## Upcoming ADR coverage
- **ADR-007 PoseStream & SectionState architecture** couples plugin topic manifests to the unified pose timeline and SectionState diffs, informing capability declarations required by R-EXT-120.【F:docs/ADR/ADR-roadmap.md†L67-L73】
- **ADR-018 Plugin API & capability discovery** will finalize manifest schema, permissions, and lifecycle expectations that deliver on R-EXT-000 through R-EXT-120 while enabling hot-plug workflows.【F:docs/ADR/ADR-roadmap.md†L142-L148】
- **ADR-024 Discovery & identity** will define plugin/node identity, capability handshakes, and lease semantics required by R-COMM-030…R-COMM-032 and R-EXT-130…R-EXT-134.【F:docs/ADR/ADR-roadmap.md†L190-L196】
- **ADR-044 Zone drawing framework** codifies LayerEditService APIs referenced by R-EXT-080.【F:docs/ADR/ADR-roadmap.md†L150-L171】
- **ADR-045 Crop type plugin**, **ADR-046 Genetics plugin**, **ADR-049 Yield plugin**, **ADR-050 Cost & profit plugin**, and **ADR-051 Report builder** define the analytics/reporting hooks referenced in R-EXT-082…R-EXT-084.【F:docs/ADR/ADR-roadmap.md†L172-L320】

## Related specifications
- Packaging, distribution, and catalog requirements: see Section 16 `Plugin Packaging, Updates, and Catalog`.
- Device updater plugins and DFU orchestration surface: see [Section 17 — Device Firmware Updates](17_Device_Firmware_Updates.md).
