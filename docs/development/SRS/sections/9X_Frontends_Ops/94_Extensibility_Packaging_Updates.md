# 94 — Extensibility, Packaging & Updates
*(Status: Proposed)*

**Author:** Codex  
**Created:** 2025-10-20  
**Version:** 0.1.0  
**Section ID:** 94  
**Editors:** Frontend & Operations Working Group  
**Last Updated:** 2025-10-24
**Related Sections:** 11 — OS Support, 21 — System Architecture, 62 — Job Lifecycle, 72 — Zone Drawing Framework, 97 — Simulation & Replay
**Upstream Dependencies:** 1X — Platform Foundations, 5X — Hardware IO Device Layer  
**Downstream Impacts:** Plugin catalog, packaging pipelines, security policies, manifest governance

---

## 94.1 Purpose & Scope

Define the extensibility surface, packaging policies, and update workflows that allow Nexus contributors to add functionality without forking core executables. This section establishes plugin APIs, manifest governance, and distribution rules so official and community modules remain safe, maintainable, and deterministic across Windows and Linux deployments.【F:docs/Plugins/nexus-plugin-dependency-map.md†L1-L421】【F:docs/sections/9X_Frontends_Ops/94-ADR-031 - Official Plugin Bundle Dependency Governance.md†L19-L83】

---

## 94.2 Context

- Legacy AgOpenGPS relies on shared libraries and multiple executables that contributors modify directly.  
- Metadata-driven dashboards and zone editing demand declarative plugin contracts across UI, telemetry, and analytics.  
- ISOBUS integration and remote deployments require deterministic manifests and capability discovery to remain safe.
- Packaging updates must cover MSI, deb/rpm, and plugin bundles distributed via catalog UI (§91) and CLI (§93).

---

## 94.3 Legacy Comparison

| Area / Theme | Legacy Behavior | Identified Limitation | Modernization Opportunity | Reference / Source |
|---------------|-----------------|------------------------|---------------------------|--------------------|
| Extensibility | Fork core executables to add features. | Merge burden and divergent safety posture. | Managed plugin API with manifest governance. | Legacy repos; ADR-031 governance |
| Packaging | Manual installers per executable. | No plugin catalog or compatibility matrix. | Versioned plugin bundles validated via dependency map. | Plugin dependency map【F:docs/Plugins/nexus-plugin-dependency-map.md†L1-L421】 |
| Packaging QA | Manual installers per executable. | No plugin catalog or compatibility matrix. | Versioned plugin bundles validated via dependency map. | Plugin dependency map【F:docs/Plugins/nexus-plugin-dependency-map.md†L1-L421】 |

---

## 94.4 Definitions

| Term | Definition |
|------|-------------|
| Plugin Manifest | Declarative description of capabilities, dependencies, and lifecycle hooks loaded by the plugin host. |
| Capability Registry | Authoritative list of topics, layers, and services available for plugin consumption/production. |
| Official Bundle | Curated set of first-party plugins distributed with Nexus releases under ADR-031 governance. |

---

> **Requirement Grammar (RFC-2119):**
> - **MUST / MUST NOT** = mandatory; verification required.
> - **SHOULD / SHOULD NOT** = strong recommendation; justify exceptions.
> - **MAY** = optional; document enabling conditions.

### 94.4.1 Architecture Overview

- **Contract-first plugins.** Packages ship as deterministic ZIP archives containing the manifest, managed assemblies, and optional assets. Manifests declare capabilities, permission scopes, and dependency relationships enforced by the host before activation.【F:docs/Plugins/architecture.md†L15-L88】【F:docs/development/SRS/appendices/samples/plugins/autosteer/1.0.0.json†L1-L76】
- **Capability leases.** Runtime access is governed by manifest-declared leases so exclusive surfaces (e.g., guidance control) cannot be pre-empted without arbitration, aligning with §95 security policies.【F:docs/development/SRS/appendices/samples/plugins/autosteer/1.0.0.json†L77-L140】【F:docs/development/SRS/sections/9X_Frontends_Ops/95_Security_Permissions.md†L47-L88】
- **Deterministic execution.** Simulation and replay harnesses treat plugins as deterministic workloads via collectible `AssemblyLoadContext` boundaries, ensuring predictable teardown and hot-reload semantics across desktop and headless deployments.【F:docs/Plugins/performance.md†L34-L112】
- **Resource management.** Hosts isolate plugins within bounded resource envelopes (memory, threads, IO) and surface telemetry so catalog governance can reject bundles that exceed stated budgets.【F:docs/Plugins/performance.md†L117-L188】【F:docs/development/SRS/sections/9X_Frontends_Ops/96_Quality_Engineering_Release.md†L101-L156】

#### Core Plugin Bands

1. **Guidance & control** — AutoSteer, Section Control, Rate Control, and future guidance orchestrators feed arbitration pipelines and control loops.【F:docs/Plugins/briefs/Guidance.md†L10-L92】【F:docs/development/SRS/appendices/samples/plugins/sections/1.1.0.json†L1-L122】
2. **Data & analytics** — Mapping, telemetry logging, crop/coverage analytics, and profit analysis expose dashboards and reports without modifying Core binaries.【F:docs/Plugins/briefs/Analytics.md†L9-L86】【F:docs/development/SRS/appendices/samples/plugins/telemetry-logging/1.0.0.json†L1-L88】
3. **Hardware integration** — ISOBUS Bridge, Device Manager, and AgIO sidecars bridge physical transports into capability-aware services while maintaining watchdog compliance.【F:docs/Plugins/briefs/IsobusBridge.md†L1-L112】【F:docs/development/SRS/appendices/samples/plugins/device-manager/1.0.0.json†L1-L124】
4. **Operational workflows** — Job Tasks, File IO, compatibility evaluators, and Sync Dashboard streamline setup, migration, and reporting for operators and support teams.【F:docs/Plugins/briefs/JobTasks.md†L9-L72】【F:docs/development/SRS/appendices/samples/plugins/file-io/1.0.0.json†L1-L110】

#### Developer Quick Start

1. [Provision the development environment](../../development/INDEX.md#initial-setup) with Nexus SDKs and manifest tooling.
2. Scaffold a plugin via the tutorials, implement capability declarations, and bind to required services.【F:docs/Plugins/tutorials/first-plugin.md†L1-L128】
3. Exercise integration tests and deterministic replay harnesses before packaging.【F:docs/development/testing.md†L42-L101】
4. Package and publish via the manifest governance workflow described in §94.5, ensuring catalog metadata and optional signatures are attached.【F:docs/Plugins/plugin-lease-manifest-governance.md†L20-L112】

## 94.5 Requirements

### 94.5.1 Baseline Extensibility

| ID | Priority | Category | Summary | Source / C-IDs | Key Metrics / Verification |
|----|-----------|----------|---------|-----------------|-----------------------------|
| R-EXT-000 | MUST | Shared Libraries | Continue exposing shared libraries (`AgLibrary`, `AgOpenGPS.Core`) for downstream customization. | Legacy WinForms projects【F:SourceCode/GPS/AgOpenGPS.csproj†L32-L48】【F:SourceCode/AgIO/Source/AgIO.csproj†L23-L33】 | Build artifacts include libraries |
| R-EXT-001 | MUST | Companion Executables | Keep companion executables (AgIO, ModSim, GPS_Out, Keypad, AgDiag) available for extension. | Solution baseline【F:SourceCode/AgOpenGPS.sln†L6-L35】 | Installer contains utilities |
| R-EXT-002 | SHOULD | Plugin Boundary | Define plugin boundaries (UI, PGN handlers, analytics) to avoid per-variation forks. | Extensibility WG | Plugin host design review |
| R-EXT-003 | SHOULD | Templates & Docs | Provide templates/guides for third-party modules integrating with packaging and settings. | Community onboarding | Documentation publication |

### 94.5.2 Metadata & Analytics Integration

| ID | Priority | Category | Summary | Source / C-IDs | Key Metrics / Verification |
|----|-----------|----------|---------|-----------------|-----------------------------|
| R-EXT-010 | SHOULD | Layer Registration | Allow plugins to register telemetry layers via DI and ID registries so dashboards update without code edits. | Layer controllers & schemas【F:docs/sections/2X_System_Architecture/21-O5 - Layer controllers with aggregation pipelines.md†L19-L33】【F:docs/sections/4X_Interprocess_Communications/41-O5 - Versioned layer schemas and quality metadata.md†L32-L49】 | Metadata registry integration tests |
| R-EXT-080 | MUST | Zone Editing | Require spatial plugins to integrate with `LayerEditService` for consistent geometry handling. | ADR-044 zone framework【F:docs/sections/7X_Mapping_Geospatial/72-ADR-044 - Zone Drawing Framework.md†L29-L74】 | Zone editing contract tests |
| R-EXT-081 | MUST | Context Bus | Publish strongly typed lifecycle events with SDK helpers for deterministic subscription. | Job lifecycle docs【F:docs/sections/6X_Core_Domain_Services/62_Job_Lifecycle.md†L18-L74】 | Context bus integration suite |
| R-EXT-082 | SHOULD | Analytics APIs | Provide shared analytics queries (yield, crop, profit) to avoid duplicate aggregation logic. | Mapping plugin ADRs【F:docs/sections/7X_Mapping_Geospatial/72-ADR-045 - Crop Type Plugin & Layers.md†L57-L71】【F:docs/sections/7X_Mapping_Geospatial/72-ADR-049 - Yield & Analytics Plugin.md†L21-L59】【F:docs/sections/7X_Mapping_Geospatial/72-ADR-050 - Cost & Profit Plugin.md†L21-L52】 | Analytics API unit tests |
| R-EXT-083 | SHOULD | Financial Hooks | Allow plugins to submit `CostRecord` entries and consume profit overlays for automation. | Cost & profit ADR【F:docs/sections/7X_Mapping_Geospatial/72-ADR-050 - Cost & Profit Plugin.md†L21-L52】 | Financial integration tests |
| R-EXT-084 | SHOULD | Report Builder | Expose report hooks so plugins contribute sections with declared dependencies. | ADR-051 report builder【F:docs/sections/9X_Frontends_Ops/91-ADR-051 - Report Builder & Export System.md†L21-L52】 | Report contribution tests |

### 94.5.3 ISOBUS Integration

| ID | Priority | Category | Summary | Source / C-IDs | Key Metrics / Verification |
|----|-----------|----------|---------|-----------------|-----------------------------|
| R-EXT-100 | MUST | ISOBUS Transport Plugin | Ship ISOBUS communications plugin registering as transport provider in managed manifest. | Hardware IO abstractions【F:docs/sections/5X_Hardware_IO_Device_Layer/51_Sensor_Actuator_Abstractions.md†L16-L54】 | ISOBUS plugin acceptance |
| R-EXT-101 | MUST | ISO 11783 Normalization | Normalize ISO 11783 PGNs into Layer/Telemetry registries for consistent topic IDs. | ISOBUS references【F:docs/development/SRS/references/ISOBUS_Section_Control.md†L1-L33】【F:docs/sections/7X_Mapping_Geospatial/74_Monitoring_Systems.md†L4-L134】 | Telemetry registry tests |
| R-EXT-102 | SHOULD | Diagnostics Surface | Expose plugin diagnostics (bus load, address claims, faults) via standard health contract. | Plugin health guidelines | Diagnostics UI regression |
| R-EXT-103 | SHOULD | Declarative Implement Metadata | Map implements to DDIs/section counts for UI auto-population. | Gauge registry & references【F:docs/appendices/GaugeId_Registry.md†L12-L20】【F:docs/development/SRS/references/ISOBUS_Section_Control.md†L1-L33】 | Implement metadata tests |

### 94.5.4 Governance & Bundles

| ID | Priority | Category | Summary | Source / C-IDs | Key Metrics / Verification |
|----|-----------|----------|---------|-----------------|-----------------------------|
| R-EXT-011 | SHOULD | Community Governance | Establish review queues, namespace reservations, and security vetting before DI registration. | ADR-031 governance【F:docs/sections/9X_Frontends_Ops/94-ADR-031 - Official Plugin Bundle Dependency Governance.md†L19-L83】 | Governance policy publication |
| R-EXT-020 | SHOULD | Official Bundle | Ship first-party capabilities as versioned plugins that can be disabled for headless deployments. | Packaging updates catalog | Bundle packaging tests |
| R-EXT-120 | MUST | Capability Declarations | Require manifests to declare produced/consumed topics, layers, control endpoints, and hardware bindings. | ADR-018 capability discovery【F:docs/sections/9X_Frontends_Ops/94-ADR-018 - Plugin API Capability Discovery and Runtime Model.md†L19-L66】 | Manifest schema validation |
| R-EXT-150 | MUST | Dependency Register | Maintain authoritative dependency register and compatibility matrix. | Plugin dependency map【F:docs/Plugins/nexus-plugin-dependency-map.md†L1-L421】 | Catalog CI validation |
| R-EXT-151 | MUST | Equivalency Profiles | Require manifests to advertise contract versions, feature flags, and conformance attestations. | Dependency map + ADR-031【F:docs/Plugins/nexus-plugin-dependency-map.md†L36-L86】【F:docs/sections/9X_Frontends_Ops/94-ADR-031 - Official Plugin Bundle Dependency Governance.md†L25-L74】 | Equivalency profile tests |
| R-EXT-152 | MUST | Dependency Semantics | Enforce `requires`, `peerOf`, `conflictsWith`, `replaces`, `extends` semantics when solving plugin graph. | Dependency map + ADR-031【F:docs/Plugins/nexus-plugin-dependency-map.md†L88-L143】【F:docs/sections/9X_Frontends_Ops/94-ADR-031 - Official Plugin Bundle Dependency Governance.md†L49-L83】 | Solver integration tests |
| R-EXT-153 | SHOULD | Provider Selection Policy | Publish deterministic provider selection policy with audit logs. | Dependency map【F:docs/Plugins/nexus-plugin-dependency-map.md†L144-L205】 | Provider audit logging tests |

### 94.5.5 Lifecycle & Security

| ID | Priority | Category | Summary | Source / C-IDs | Key Metrics / Verification |
|----|-----------|----------|---------|-----------------|-----------------------------|
| R-EXT-004 | COULD | Sandboxing | Support sandboxing/capability declarations to protect critical operations. | Security roadmap | Sandbox feasibility study |
| R-EXT-130 | MUST | Lifecycle States | Standardize plugin lifecycle states (discovered, verified, started, healthy, degraded, stopped) with observable transitions. | Plugin lifecycle notes | Lifecycle telemetry tests |
| R-EXT-131 | MUST | Permission Gate | Enforce policy-driven permission gate during registration (`pose.read`, `section.command`, etc.). | §95 Security & Permissions | Permission enforcement tests |
| R-EXT-132 | SHOULD | Manifest Signing | Support optional signing/verification with operator overrides for air-gapped rigs. | Security governance | Signing validation tests |
| R-EXT-133 | SHOULD | Remote Plugins | Document requirements for remote plugin connections (mTLS, leases, restart policies). | Remote deployment plan | Remote plugin integration tests |
| R-EXT-134 | SHOULD | Audit Trails | Capture plugin health, command history, configuration edits for provenance. | Section 61 control audit | Audit logging tests |
| R-EXT-140 | MUST | Job Lifecycle Hooks | Expose plugin callbacks for job workflows gated by `jobs.lifecycle` permissions. | ADR-030 job lifecycle【F:docs/sections/6X_Core_Domain_Services/62-ADR-030 - Field job sessions and lifecycle services.md†L47-L86】 | Job lifecycle integration |
| R-EXT-141 | SHOULD | Preset Orchestration | Provide SDK helpers for presets/layout services with arbitration safeguards. | ADR-032 presets & layout linking【F:docs/sections/9X_Frontends_Ops/91-ADR-032 - Presets and Layout Linking for Equipment Workflows.md†L7-L34】 | Preset orchestration tests |
| R-EXT-142 | SHOULD | Drive-In Providers | Allow registration of Drive-In discovery/importers with schema validation and provenance logging. | ADR-030 job lifecycle【F:docs/sections/6X_Core_Domain_Services/62-ADR-030 - Field job sessions and lifecycle services.md†L47-L86】 | Drive-In provider tests |
| R-EXT-143 | SHOULD | Task Orchestration API | Standardize background task submission, progress, retry semantics for preset operations. | ADR-032 presets & layout linking【F:docs/sections/9X_Frontends_Ops/91-ADR-032 - Presets and Layout Linking for Equipment Workflows.md†L17-L34】 | Task orchestration harness |

### 94.5.6 Requirement Sources & Rationale

| Req ID | Source | Rationale |
|--------|--------|-----------|
| R-EXT-000 | Legacy shared libraries | Preserve existing extension points during transition. |
| R-EXT-010 | Layer metadata roadmap | Enable metadata-driven dashboards without code duplication. |
| R-EXT-100 | ISOBUS integration plan | Provide official CAN/ISOBUS bridge to avoid forks. |
| R-EXT-150 | Plugin dependency map | Ensure operators validate bundle compatibility before activation. |
| R-EXT-130 | Operations resilience | Monitor plugin health and enable deterministic recovery. |

---

## 94.6 Acceptance Criteria & Verification

Extensibility features must pass manifest schema validation, dependency solver tests, lifecycle telemetry checks, and packaging smoke runs. Packaging pipelines verify Windows/Linux bundles and plugin catalog metadata before release.

### 94.6.1 Requirement-to-Verification Map

| Req ID | Verification Type | Artifact / Location | Pass/Fail Threshold |
|--------|-------------------|---------------------|---------------------|
| R-EXT-010 | Integration | `tes../Plugins/LayerRegistry.feature` | Layers appear in dashboards without code change |
| R-EXT-100 | Hardware-in-loop | `tes../Plugins/IsobusTransportSuite` | ISOBUS plugin routes PGNs to SimBus + hardware graph |
| R-EXT-120 | Schema validation | `schem../Plugins/manifest.schema.json` | All manifests declare capabilities/bindings |
| R-EXT-150 | Catalog CI | `tests/catalog/CompatibilityMatrix.cs` | Dependency solver resolves official bundle combinations |
| R-EXT-130 | Lifecycle telemetry | `tes../Plugins/LifecycleState.feature` | State transitions emit expected events |

---

## 94.7 Constraints

- Plugin manifests must remain backward-compatible across patch releases or provide migration scripts documented in release notes.  
- Packaging must adhere to OS security policies (code signing, notarization, repository trust).  
- Update orchestrators must respect §95 security gates and staged rollout approvals.

---

## 94.8 Interfaces & Dependencies

- Depends on capability discovery and manifest schema defined in ADR-018 and ADR-031.  
- Integrates with UI Shell (§91) and CLI (§93) for plugin catalog management.  
- Shares telemetry and replay expectations with §96 Quality Engineering & Release.

---

## 94.9 Design Considerations

| ID | Consideration | Description |
|----|----------------|-------------|
| C1 | Managed plugin runtime | Favor .NET 8 `AssemblyLoadContext` loader with shared gRPC contracts for cross-platform parity.【F:docs/sections/1X_Platform_Foundations/11-O1_Unified_DotNet8_Avalonia.md†L9-L79】 |
| C2 | Plugin catalog UX | Provide catalog UI + CLI surfaces for installing, updating, and auditing plugins with dependency visualization. |
| C3 | Update Orchestration | Deliver safe update flows with staged rollouts, catalog gating, and rollback hooks. |
| C4 | Community Onboarding | Offer templates, documentation, and review processes to encourage safe third-party contributions. |
| C5 | Governance Transparency | Publish compatibility matrices, provider selection logs, and security attestations for operator trust. |

### 94.9.1 Assumptions & Preconditions

- [A1] Official bundle maintainers update dependency matrix alongside releases.  
- [A2] Plugin authors adopt manifest schema and lifecycle hooks.  
- [A3] Operators manage signing certificates and credential stores per §95 guidance.

---

## 94.10 Option Overview

No alternative extensibility proposals are under evaluation; modernization focuses on considerations C1–C5 and the managed plugin runtime described in ADR-031.

---

## 94.11 Comparison Matrix

| Attribute / Criteria | Legacy Fork Model | Managed Plugin Runtime |
|----------------------|-------------------|------------------------|
| Maintainability | Low — forks diverge quickly. | High — governed manifests and shared runtime. |
| Safety | Medium — bespoke changes risk bypassing checks. | High — permission gates and audit trails. |
| Deployment | Manual rebuilds per change. | Catalog-driven updates with compatibility checks. |
| Update Agility | Manual packaging flows. | Catalog + CLI orchestrate staged rollouts. |
| Operator Control | Limited toggles. | Enable/disable plugins per deployment profile. |

---

## 94.12 Decision Matrix

> **Informative:** Weighted scoring deferred until managed plugin runtime reaches beta; governance decisions currently tracked in ADR-031.

---

## 94.13 Evaluation & Verification

- Run dependency solver against official bundle combinations before each release.  
- Run catalog smoke tests on Windows/Linux bundles, verifying staged rollout metadata.
- Perform security audits on manifest signing, permission gates, and remote plugin policies.

**Acceptance Criteria**

- All **MUST** requirements pass automated validation and manual sign-off.  
- Plugin catalog publishes compatibility matrix with traceable provenance.  
- Catalog publishes staged rollout metadata with rollback paths validated.

---

## 94.14 Implementation Policy

- Store manifests in `plugins/<PluginName>/manifest.json` validated during build and release pipelines.  
- Publish plugin catalog metadata to `docs/Plugins/catalog.json` consumed by UI and CLI surfaces.  
- Require versioned migration scripts for breaking manifest changes, referenced in §96 release notes.

---

## 94.15 Community Sentiment

- Contributors favor managed plugin runtime to reduce merge debt while keeping safety boundaries.  
- Operators request transparent compatibility matrices before enabling new bundles.  
- QA stakeholders advocate for tighter catalog + §97 simulation integration to keep replay fixtures discoverable.

### 94.15.1 Section Change Log

| Date | Summary | PR / Issue |
|------|---------|------------|
| 2025-10-20 | Converted to new SRS template; organized requirements into governance, simulation, and lifecycle groups. | #0000 |

---

## 94.16 Traceability

| Requirement ID | Considerations | ADR(s) | Verification Artifact | Implementation Reference |
|----------------|----------------|--------|-----------------------|--------------------------|
| R-EXT-010 | C1, C4 | 21-O5, 41-O5 | `tes../Plugins/LayerRegistry.feature` | Layer registry service |
| R-EXT-100 | C1, C3 | — | `tes../Plugins/IsobusTransportSuite` | ISOBUS plugin package |
| R-EXT-120 | C2, C5 | 94-ADR-018, 94-ADR-031 | `schem../Plugins/manifest.schema.json` | Manifest schema repository |
| R-EXT-150 | C2, C5 | 94-ADR-031 | `tests/catalog/CompatibilityMatrix.cs` | Plugin catalog pipeline |
| R-EXT-130 | C5 | — | `tes../Plugins/LifecycleState.feature` | Plugin host runtime |

---

## 94.17 Conformance

Implementations conform when all **MUST** requirements pass verification, manifests comply with governance policies, and operators can manage plugins via catalog and CLI without resorting to forks.

---

## Standards Context

Draws on OSGi-style module governance, semantic versioning (SemVer 2.0.0), and ISO 11783 interoperability guidelines to balance extensibility with deterministic operations.

## Proposed plugin tiers

| Tier | Examples | Notes |
|---|---|---|
| Core services | Headless navigation, guidance solver, settings store | Remains lightweight, always installed. |
| First-party plugins | Default desktop UI, AgIO transport manager, gauges, variable-rate controllers, ISO-BUS tooling | Bundled by default, versioned independently, can be disabled to run headless. |
| Community plugins | Enterprise dashboards, specialized device support, analytics | Distributed via catalog governance and permission review. |

## ISOBUS communications plugin expectations

- **Manifest & lifecycle** — The plugin advertises itself as an `IsoBusTransport` provider, exposes configuration for CAN adapter selection and PGN filters, and follows the shared plugin lifecycle hooks so it can be hot-reloaded in desktop and headless deployments.【F:docs/sections/5X_Hardware_IO_Device_Layer/51_Sensor_Actuator_Abstractions.md†L16-L54】
- **Routing integration** — PGNs decoded by the plugin populate LayerDefinitions, SectionControllers, and Gauge topics that already exist for UDP transports, ensuring mixed-transport rigs see a single authoritative topic namespace.【F:docs/development/SRS/references/ISOBUS_Section_Control.md†L1-L33】【F:docs/sections/7X_Mapping_Geospatial/74_Monitoring_Systems.md†L4-L134】
- **Diagnostics & UI hooks** — Health metrics (bus utilization, last-frame timestamps, device address-claim status) and implement metadata (section labels, DDIs, rate controller presence) feed into the plugin health UI surfaces defined in Section 16, enabling dashboards to highlight wiring faults or mismatched implement profiles.【F:docs/sections/9X_Frontends_Ops/94_Extensibility_Packaging_Updates.md#packaging-updates--catalog†L7-L58】【F:docs/appendices/GaugeId_Registry.md†L12-L20】
- **Simulation parity** — When the plugin runs in simulation mode it must follow §97 Simulation & Replay policies so PGN playback sequences can be validated alongside hardware captures.【F:docs/sections/9X_Frontends_Ops/97_Simulation_Replay.md†L1-L120】

## Open questions
- Which features are safe to expose via scripting vs. compiled plugins?
- How do we version plugin APIs alongside firmware expectations?

## Upcoming ADR coverage
- **ADR-007 PoseStream & SectionState architecture** couples plugin topic manifests to the unified pose timeline and SectionState diffs, informing capability declarations required by R-EXT-120.【F:docs/sections/2X_System_Architecture/21-ADR-900 - PoseStream, Layer, and Control Program Roadmap.md†L67-L73】
- **ADR-018 Plugin API & capability discovery** will finalize manifest schema, permissions, and lifecycle expectations that deliver on R-EXT-000 through R-EXT-120 while enabling hot-plug workflows.【F:docs/sections/2X_System_Architecture/21-ADR-900 - PoseStream, Layer, and Control Program Roadmap.md†L142-L148】
- **ADR-024 Discovery & identity** will define plugin/node identity, capability handshakes, and lease semantics required by R-COMM-030…R-COMM-032 and R-EXT-130…R-EXT-134.【F:docs/sections/2X_System_Architecture/21-ADR-900 - PoseStream, Layer, and Control Program Roadmap.md†L190-L196】
- **ADR-044 Zone drawing framework** codifies LayerEditService APIs referenced by R-EXT-080.【F:docs/sections/2X_System_Architecture/21-ADR-900 - PoseStream, Layer, and Control Program Roadmap.md†L150-L171】
- **ADR-045 Crop type plugin**, **ADR-046 Genetics plugin**, **ADR-049 Yield plugin**, **ADR-050 Cost & profit plugin**, and **ADR-051 Report builder** define the analytics/reporting hooks referenced in R-EXT-082…R-EXT-084.【F:docs/sections/2X_System_Architecture/21-ADR-900 - PoseStream, Layer, and Control Program Roadmap.md†L172-L320】

## Related specifications
- Packaging, distribution, and catalog requirements: see Section 16 `Plugin Packaging, Updates, and Catalog`.
- Device updater plugins and DFU orchestration surface: see [Section 55 — Firmware Interfaces & Updates](17_Device_Firmware_Updates.md).

## Packaging, updates & catalog
### Problem statement
AgOpenGPS needs a consistent, auditable way to package, distribute, verify, and update plugins so operators can extend the system without risking downtime or unsafe behaviour. The specification must cover archive layout, manifest expectations, trust and permission models, offline installs, and catalog governance so that community and enterprise contributors can ship modules confidently.

### Requirements (from contributors)
- R-PKG-000 (MUST, packaging): Plugins ship as deterministic `.zip` archives with a top-level `plugin.json` manifest and optional `backend/`, `ui/`, `assets/`, and `contrib/` folders plus recommended notices (LICENSE/NOTICE/changelog).
- R-PKG-001 (MUST, manifest): `plugin.json` follows the published schema covering identity, compatibility targets, backend and UI entrypoints, permission declarations, contributions, signing metadata, optional GitHub update metadata, and reserved dependency metadata.【F:docs/appendices/schemas/plugin_manifest.schema.json†L1-L118】
- R-PKG-002 (MUST, install locations): Hosts install to `<data_roo../Plugins/<id>/<version>/` (system) or `~/.a../Plugins/<id>/<version>/` (per-user) and maintain `<..../Plugins/<id>/current → <version>` for rollback.
- R-PKG-003 (MUST, integrity): Installers validate the ZIP against SHA-256 values supplied by the catalog or a bundled `checksums.txt` for side-loaded packages before activation.
- R-PKG-004 (SHOULD, signing): When `signing` metadata is present, verify the Ed25519 signature against trusted public keys; warn or block unsigned packages based on policy.【F:docs/appendices/schemas/plugin_manifest.schema.json†L118-L142】
- R-PKG-005 (MUST, permissions): The Plugin Manager surfaces requested permissions (serial, SocketCAN, network, filesystem, etc.) during install/enable and requires explicit approval.【F:docs/appendices/schemas/plugin_manifest.schema.json†L86-L117】
- R-PKG-006 (MUST, isolation): Backend plugins execute under restricted OS users and cannot bind beyond localhost without granted permissions; the host enforces declared capabilities.
- R-PKG-007 (MUST, manifests vNext): `plugin.json` captures `provides.capabilities[].version/features` and `provides.profiles[]` entries with CI-conformance attestations so capability swaps and profile equivalency stay machine-verifiable.【F:docs/appendices/schemas/plugin_manifest.schema.json†L153-L236】【F:docs/sections/9X_Frontends_Ops/94-ADR-031 - Official Plugin Bundle Dependency Governance.md†L25-L74】
- R-PKG-008 (MUST, dependency expressions): `requires.capabilities[]`, `requires.profiles[]`, and relationship arrays (`peerOf`, `conflictsWith`, `replaces`, `extends`) describe peer expectations and migration rules that Core enforces during graph resolution.【F:docs/appendices/schemas/plugin_manifest.schema.json†L237-L332】【F:docs/Plugins/nexus-plugin-dependency-map.md†L88-L143】
- R-PKG-009 (SHOULD, provider policy): Plugin bundles may ship policy overlays (e.g., `[capability."mapping:vector"]`) documenting admin pins, allow-multiple flags, and provider priorities so deterministic selection is auditable and reproducible.【F:docs/Plugins/nexus-plugin-dependency-map.md†L144-L205】
- R-PKG-010 (SHOULD, updates): Provide an optional auto-update utility that respects per-plugin channels (stable/beta/pinned), SemVer policies, and the declared compatibility ranges.
- R-PKG-011 (SHOULD, update flow): Updates download, verify, stage into versioned folders, retain the previous build, switch via the `current` symlink (restart or hot-reload), and expose a one-click rollback.
- R-PKG-012 (SHOULD, caching): Cache catalog indexes and release metadata locally, observe HTTP caching headers (ETag/If-Modified-Since), and support mirrored sources for restricted networks.
- R-PKG-020 (MUST, catalog): Approved catalogs publish signed `catalog.json` indexes that describe plugins, channels, checksums, signatures, compatibility, permissions, and icon URLs per the schema.【F:docs/appendices/schemas/plugin_catalog.schema.json†L1-L84】【F:docs/appendices/schemas/plugin_catalog.schema.json†L84-L120】
- R-PKG-021 (SHOULD, governance): Catalog entries are managed through PR-based workflows with CI validation of icons, checksums, signatures, SemVer ranges, and compatibility fields.
- R-PKG-022 (SHOULD, UI capabilities): The Plugin Manager supports browsing/searching catalog entries, filtering by category/permissions, installing/updating/uninstalling, toggling channels (global or per-plugin), viewing changelogs, highlighting permission diffs, and showing trust tiers plus health/log links.
- R-PKG-030 (MUST, side-loading): Users can install by dragging a plugin ZIP into the UI or pointing to a filesystem path; the host validates manifests, checksums, and warns for unsigned packages.
- R-PKG-031 (MUST, offline): Accept `file://` or removable-media indexes containing `catalog.json` plus assets to enable offline environments.
- R-PKG-040 (MUST, bundled deps): Initial releases forbid transitive dependencies; each plugin bundles what it needs. Future dependency metadata remains reserved in the schema.【F:docs/appendices/schemas/plugin_manifest.schema.json†L142-L152】
- R-PKG-050 (MUST, trust policy): Trust tiers differentiate approved (catalog + signed), third-party signed, and unsigned plugins; default policy accepts the first two and prompts on unsigned, while admin mode can enforce “approved only”.
- R-PKG-060 (MUST, runtime management): Core services validate archives, enforce permissions, sandbox processes, manage backend lifecycle (start/stop/retry with backoff), and expose health/log telemetry for the UI.
- R-PKG-070 (MUST, verification suite): Acceptance coverage includes catalog install success, side-loaded install with checksum validation, auto-update staging/switchover/rollback, unsigned warning flows, signature mismatch rejection, permission diff prompts, and backend crash containment.

### Options
- O-PKG-0: Status quo — Share ad-hoc binaries/assemblies without a manifest or catalog.
- O-PKG-1: Standardized ZIP packaging with manifest + catalog governance (proposed).
- O-PKG-2: Adopt external package managers (NuGet/MSIX) for plugins instead of custom ZIP archives.

### Comparison (quick matrix)
| Option | Pros | Cons | Risks | Borrow from existing |
|---|---|---|---|---|
| O-PKG-0 | Zero new tooling | No trust model, manual installs | Unsafe/unknown plugins | Current ZIP drops |
| O-PKG-1 | Deterministic installs, catalog UX, rollback | Requires signing + governance infra | Catalog compromise or tooling debt | VS Code extensions, Arduino Library Manager |
| O-PKG-2 | Leverages mature ecosystems | Heavyweight, platform-specific | Harder offline support, dependency drift | NuGet/MSIX packaging |

### Evaluation criteria
Safety, offline usability, rollback resilience, governance overhead, contributor ergonomics, and compatibility with existing release workflows.

### Current sentiment
The team prefers the standardized ZIP manifest approach (O-PKG-1) because it delivers predictable installs, optional automation, and a curated catalog while staying close to current ZIP distribution habits and allowing offline media workflows.

### Open questions
- What tooling ships with the SDK to help authors build/sign ZIPs?
- How are enterprise catalogs authenticated and distributed to fleets with no outbound internet?
- Do we need to predefine additional permission scopes (e.g., CAN bus filters, camera access) before beta release?

### Acceptance criteria
- Catalog and side-loaded installs succeed only when manifests and checksums validate; unsigned packages prompt users for confirmation.
- Auto-update discovers a newer version, stages it, switches via `current`, and supports a one-click rollback.
- A signed plugin with a mismatched checksum is rejected; permission deltas block updates until the operator approves them.
- Backend plugin crashes do not bring down the host; restart/backoff policies are logged and surfaced in the Plugin Manager.

### References
- Plugin manifest schema: Appendix `plugin_manifest.schema.json`.【F:docs/appendices/schemas/plugin_manifest.schema.json†L1-L152】
- Catalog index schema: Appendix `plugin_catalog.schema.json`.【F:docs/appendices/schemas/plugin_catalog.schema.json†L1-L120】
