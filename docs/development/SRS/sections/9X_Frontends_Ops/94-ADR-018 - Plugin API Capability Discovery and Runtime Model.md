# ADR-018: Plugin API, Capability Discovery, and Runtime Model

## Status
Proposed

**Relevant Plugin(s):** Full Stack


## Context
The Nexus roadmap requires a plugin model that keeps Core minimal while letting automation, visualization, and hardware bridges evolve independently. Contributors need out-of-process plugins that register over gRPC, declare capabilities, expose UI contributions, and operate under explicit permissions so headless rigs remain deterministic.【F:docs/development/SRS/sections/4X_Interprocess_Communications/42_Transports.md†L20-L28】【F:docs/development/SRS/sections/9X_Frontends_Ops/91_UI_Shell_Layout.md†L20-L23】【F:docs/development/SRS/sections/5X_Hardware_IO_Device_Layer/51_Sensor_Actuator_Abstractions.md†L23-L34】【F:docs/development/SRS/sections/6X_Core_Domain_Services/61_Kinematics_Pose_Fusion.md†L16-L22】【F:docs/development/SRS/sections/9X_Frontends_Ops/94_Extensibility_Packaging_Updates.md†L6-L28】 Core today hosts transport, storage, and control logic tightly coupled with built-in UIs and AgIO; introducing hot-pluggable services without governance risks safety regressions, drift in contracts, and incompatible UI extensions.

## Decision
Establish a gRPC-first plugin architecture composed of:

- **Core services:** The headless Nexus runtime exports canonical gRPC contracts (Pose, SectionControl, Equipment, LayerRegistry, TileQuery, Config, Capabilities, Guidance, EventBus, Health) over authenticated channels. Core owns arbitration, data persistence, and permission checks but contains no feature-specific automation logic.
- **Plugin processes:** Each plugin ships as an out-of-process binary with a `plugin.json` manifest describing metadata (id, version, compatible Core API range), declared capabilities (autosteer, section-control, rate-control, guidance-lines, layer-publisher, ui-panel, ui-config-page, map-overlay, io-bridge, telemetry), required/implemented gRPC contracts, and requested permissions. Plugins register via `Capabilities.Register` and maintain a lease using periodic heartbeats; Core can restart or quarantine a plugin when health endpoints report degraded status.
- **Permissions & security:** Core enforces a policy-driven permission gate (`pose.read`, `pose.write`, `section.command`, `rate.command`, `storage.read`, `storage.write`, `config.manage`, `io.device`, `io.can`, `io.serial`). Local deployments use signed manifests or operator approval prompts; remote plugins require mTLS with certificate allow-lists. Every control-affecting RPC records plugin identity for audit trails.
- **UI contributions:** Plugins declare panels, configuration pages, and map overlays through declarative schemas returned by `GetUiContributions`. Frontends (Avalonia shells) render contributions using shared widget libraries—no plugin UI code loads in-process. Capability flags drive visibility/read-only behavior based on granted permissions and health state.
- **AgIO as privileged plugin:** The hardware bridge becomes a first-class plugin exposing device enumeration, firmware telemetry (E2/E1/E0/DF/E3/E4 flows), and command channels while honoring the same lifecycle, permission, and health semantics. Core never embeds device drivers directly.
- **Lifecycle model:** Plugins move through discovered → verified → started → healthy → degraded → stopped states. Core watches manifest directories and allows hot-plug/start/stop without restarting Core or the UI. Health RPCs provide heartbeat, metrics, and error surfaces; repeated failures trigger exponential backoff and operator notifications.
- **Versioning & compatibility:** Core and plugin contracts use semantic versioning with feature flags for preview services. Manifests specify acceptable Core API ranges; incompatible plugins fail registration with actionable messages. Capabilities include schema hashes (e.g., layer definitions, registry hashes) to detect drift before data exchange.

## Governance Updates
- **Security threat model.** Capability manifests undergo threat modeling covering privilege escalation, manifest tampering, and supply-chain compromise. Mitigations feed into mandatory signed capability leases maintained in source control.
- **Signed leases.** Runtime only activates capabilities when presented with a signed lease from the operator or fleet administrator. Leases encode scope, expiry, and audit references, and CI rejects unsigned manifests.
- **Reference policies.** Deployment playbooks ship pre-built policy bundles for single-rig, co-op, and enterprise fleets. Operators can apply templates directly, reducing bespoke analysis while preserving least-privilege defaults.

## Consequences
- **Positive impacts**
  - Core stays minimal and deterministic while enabling rapid plugin innovation (automation, analytics, hardware bridges).
  - UI shells render plugin contributions declaratively, keeping the desktop, headless, and remote experiences consistent.【F:docs/development/SRS/sections/9X_Frontends_Ops/91_UI_Shell_Layout.md†L20-L23】
  - Permission gates and lease health checks provide safety boundaries for automation, addressing control requirements R-CTRL-003…R-CTRL-005 and hardware governance R-HW-020…R-HW-023.【F:docs/development/SRS/sections/6X_Core_Domain_Services/61_Kinematics_Pose_Fusion.md†L20-L22】【F:docs/development/SRS/sections/5X_Hardware_IO_Device_Layer/51_Sensor_Actuator_Abstractions.md†L23-L34】
  - AgIO evolves independently as an I/O plugin while still presenting hardware telemetry through Core.
- **Negative/mitigated impacts**
  - Increased process count and gRPC connections add operational overhead; mitigated by shared hosting templates and instrumentation.
  - Manifest signing and policy review introduce release friction; mitigated via tooling and documented operator override workflows.
  - Plugin crashes or slow health responses can degrade UX; mitigated by leases, circuit breakers, and operator restart controls.
- **Follow-up actions**
  - Implement Capabilities registry, manifest watcher, and permission gate in Core.
  - Publish SDKs/stubs for sample plugins (planter monitor, rate controller, guidance, autosteer) and UI contribution host components.
  - Migrate AgIO into the plugin runtime and expose device enumeration + firmware bridge contracts under the new permission model.
  - Add CI smoke tests that load/unload plugins, verify permission enforcement, and ensure disabling a plugin removes its outputs until reenabling reproduces identical results.

## Legacy Implementation Notes
### AgOpenGPS v6
- Extension today means editing the shared solution directly—AgOpenGPS, AgIO, and companion tools link common libraries but ship as monolithic executables without any manifest or permission boundary.【F:docs/development/SRS/sections/9X_Frontends_Ops/94_Extensibility_Packaging_Updates.md†L6-L24】【F:docs/development/SRS/sections/9X_Frontends_Ops/94_Extensibility_Packaging_Updates.md†L30-L41】

### Legacy Dev Branch
- The dev branch follows the same status-quo model (Option O-EXT-0), so contributors fork source to add features and must rebuild the entire suite, with no lifecycle governance or security gating around extensions.【F:docs/development/SRS/sections/9X_Frontends_Ops/94_Extensibility_Packaging_Updates.md†L30-L56】

## References
- [Section 42 — Transports](../4X_Interprocess_Communications/42_Transports.md)
- [Section 91 — UI Shell & Layout](../9X_Frontends_Ops/91_UI_Shell_Layout.md)
- [Section 51 — Sensor & Actuator Abstractions](../5X_Hardware_IO_Device_Layer/51_Sensor_Actuator_Abstractions.md)
- [Section 61 — Kinematics & Pose Fusion](../6X_Core_Domain_Services/61_Kinematics_Pose_Fusion.md)
- [Section 94 — Extensibility, Packaging & Updates](../9X_Frontends_Ops/94_Extensibility_Packaging_Updates.md)
- [ADR-002 — Expose Nexus services over gRPC/protobuf contracts](ADR-002-grpc-contracts.md)
- [ADR-004 — Composite simulation fabric](ADR-004-composite-simulation.md)
