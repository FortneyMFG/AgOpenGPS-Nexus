# ADR-031: Official Plugin Bundle Dependency Governance

## Status
Proposed

**Relevant Plugin(s):** Mapping, Autosteer, Section Control, Rate Control, Variable Mapping, Device Manager, Telemetry Logging, UI Shell, Job Tasks


## Context
The Nexus stack now distributes guidance, mapping, rate/section control, IO bridges, telemetry, and UI surfaces as discrete plugins. While ADR-018 defined the plugin API, teams lacked a canonical register of hard/soft dependencies, manifest expectations, and compatibility ranges. Operators could unknowingly load incompatible versions or omit supporting services, leading to runtime faults or degraded functionality across Core, AgIO, and the Avalonia/Web frontends. The newly published plugin dependency map documents cross-domain relationships, but the decision to make that register authoritative and enforceable has not yet been ratified.【F:docs/Plugins/nexus-plugin-dependency-map.md†L1-L421】【F:docs/development/SRS/sections/9X_Frontends_Ops/94_Extensibility_Packaging_Updates.md†L99-L161】

## Decision
Adopt the plugin dependency map as the authoritative specification for the first-party plugin bundle and pair it with automated governance:

1. **Manifest compliance tooling** — The YAML schema in the dependency map becomes normative. All official plugins must ship manifests validated in CI; Core refuses to load manifests that fail schema or compatibility checks.【F:docs/Plugins/nexus-plugin-dependency-map.md†L167-L421】
2. **Dependency classification** — Hard/Soft/Suggest semantics are codified in Core’s loader. Hard dependencies block startup when missing or out of range; Soft dependencies emit warnings and disable associated features; Suggest dependencies surface UI nudges via the Device Manager and UI Shell.【F:docs/Plugins/nexus-plugin-dependency-map.md†L19-L164】【F:docs/development/SRS/sections/9X_Frontends_Ops/91_UI_Shell_Layout.md†L20-L76】
3. **Compatibility ranges** — Core, AgIO, and frontend version ranges in each manifest are enforced before activation, ensuring hardware transport and UI extension contracts remain within supported bounds.【F:docs/development/SRS/sections/5X_Hardware_IO_Device_Layer/51_Sensor_Actuator_Abstractions.md†L16-L54】【F:docs/Plugins/nexus-plugin-dependency-map.md†L353-L421】
4. **Lifecycle checks** — Device Manager and UI Shell surface dependency health badges using the matrix so operators know when required peers are absent or stale.【F:docs/Plugins/nexus-plugin-dependency-map.md†L109-L205】
5. **Roadmap integration** — ADR-031 becomes the umbrella decision covering ongoing manifest maintenance and CI validation tasks listed in the ADR roadmap.【F:docs/development/SRS/sections/2X_System_Architecture/21-ADR-900 - PoseStream, Layer, and Control Program Roadmap.md†L89-L111】
6. **Equivalency via profiles** — Manifests must describe `provides.capabilities` feature matrices, `provides.profiles`, and CI-backed `attestations.conformance` entries so alternative providers can prove compatibility without hard-coded aliases.【F:docs/development/SRS/appendices/schemas/plugin_manifest.schema.json†L153-L332】【F:docs/Plugins/nexus-plugin-dependency-map.md†L36-L86】
7. **Deterministic resolver** — Core interprets `requires.capabilities`, `requires.profiles`, and new relationship types (`peerOf`, `conflictsWith`, `replaces`, `extends`) alongside policy overlays to pick providers, quarantine conflicts, and publish an auditable decision log.【F:docs/development/SRS/sections/9X_Frontends_Ops/94_Extensibility_Packaging_Updates.md†L24-L32】【F:docs/Plugins/nexus-plugin-dependency-map.md†L88-L205】

## Governance Updates
- **Machine-readable digests.** Bundle releases publish signed dependency manifests that CI and operators can ingest to audit compatibility and provenance.
- **Release tooling integration.** Publishing flow cross-checks manifests against live dependency graphs, blocking promotion until conflicts or missing smoke coverage are resolved.
- **Sandbox rehearsals.** Maintainers operate rehearsal environments mirroring production rigs. Successful rehearsals become a release checklist item and results are archived for later audits.
- **Capability matrix.** Maintain the [official bundle capability matrix](../../../../Core/reference/official-bundle-capability-matrix.md) alongside the dependency map so governance reviews track both dependency posture and lease expectations.
- **Conformance attestations.** CI publishes signed attestations for every declared profile/feature combination so dependency resolution can trust third-party replacements and raise targeted remediation guidance.【F:docs/development/SRS/appendices/schemas/plugin_manifest.schema.json†L309-L332】
- **Resolution reports.** Bundle builds archive the resolver’s policy inputs, selected providers, and downgraded edges so operators and support teams can reproduce field rigs and diagnose conflicts.【F:docs/Plugins/nexus-plugin-dependency-map.md†L144-L205】

## Consequences
- **Positive**
  - Prevents misconfigured deployments by failing fast when mandatory services are missing or incompatible.【F:docs/Plugins/nexus-plugin-dependency-map.md†L19-L164】
  - Provides a single source of truth for operators and integrators, reducing onboarding friction and documentation drift.【F:docs/development/SRS/sections/9X_Frontends_Ops/94_Extensibility_Packaging_Updates.md†L99-L161】
  - Enables UI surfaces to reflect dependency health consistently across Avalonia and Web shells.【F:docs/development/SRS/sections/9X_Frontends_Ops/91_UI_Shell_Layout.md†L20-L76】
  - Lays groundwork for catalog governance by ensuring community plugins adhere to the same schema.
- **Negative / mitigations**
  - Stricter loader enforcement could block legacy combinations; mitigated by allowing operators to acknowledge Soft dependency gaps and by publishing migration guides alongside manifest updates.【F:docs/Plugins/nexus-plugin-dependency-map.md†L109-L205】
  - CI validation introduces overhead; mitigated by shared tooling templates and manifest unit tests distributed with plugin repositories.【F:docs/Plugins/nexus-plugin-dependency-map.md†L167-L421】

### Degraded operation & messaging
- **Soft dependency gaps:** Core records explicit `dependency:soft` warnings and forwards them to Device Manager, which shows dismissible notices that explain which automations are paused. JobsService and PresetsService mirror the same status so operators see consistent messaging regardless of entry point.
- **Hard dependency failures:** When a hard dependency cannot be satisfied, Core quarantines the requesting plugin but continues startup for the rest of the bundle. The UI surfaces a blocking modal that links to remediation steps (update, enable missing plugin, or accept degraded mode where available) rather than exiting the application abruptly.
- **Version drift:** If manifests declare ranges outside the currently running Core/AgIO versions, the loader offers an explicit "Launch in compatibility mode" path that tags the session as unsupported. Crash reports include the drift metadata so support can prioritize fixes, and automation features default to safe/manual states.

## Follow-up Actions
- Implement manifest schema validation in CI and as part of the `nexus plugin pack` tooling.
- Extend Core loader to resolve dependency graph orderings and emit actionable diagnostics for missing Hard dependencies.
- Update Device Manager and UI Shell to consume the dependency matrix and show health badges per plugin.
- Publish guidance for community plugins referencing the same schema and dependency classifications.

## References
- [Nexus Plugin Dependency Map](../../../../Plugins/nexus-plugin-dependency-map.md)
- [SRS §12 — Extensibility & Plugins](../9X_Frontends_Ops/94_Extensibility_Packaging_Updates.md)
- [SRS §05 — Frontends](../9X_Frontends_Ops/91_UI_Shell_Layout.md)
- [SRS §06 — Hardware I/O](../5X_Hardware_IO_Device_Layer/51_Sensor_Actuator_Abstractions.md)
- [ADR Roadmap](ADR-roadmap.md)
