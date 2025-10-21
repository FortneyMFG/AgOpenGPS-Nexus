# Legacy V6 vs. Legacy Dev Comparison

This note captures the most meaningful differences between the legacy AgOpenGPS V6 release line and the community Dev branch. It summarizes where each branch diverges in core foundations and operator experience, then outlines how Nexus is addressing the gaps while keeping migration pathways open.

## Foundational Differences

### Runtime and OS Footprint
- **V6:** Ships only as Windows desktop executables built on WinForms and WPF, so all core services, AgIO, and tooling stay tied to the .NET Framework stack without cross-platform parity.【F:docs/SRS/sections/1X_Platform_Foundations/11-ADR-001 - Adopt .NET 8 C stack for Nexus runtime.md†L25-L27】
- **Legacy Dev:** Continues the same Windows-only WinForms/WPF approach, reflecting incremental modernization without a shared cross-platform runtime or packaging model.【F:docs/SRS/sections/1X_Platform_Foundations/11-ADR-001 - Adopt .NET 8 C stack for Nexus runtime.md†L28-L29】
- **Nexus direction:** Standardizes every first-party component on .NET 8 with platform-specific hardware isolated behind AgIO backends, giving Windows and Linux builds equal footing while keeping contracts shared through `Aog.Abstractions`.【F:docs/SRS/sections/1X_Platform_Foundations/11-ADR-001 - Adopt .NET 8 C stack for Nexus runtime.md†L7-L22】

### Service Contracts and Inter-Process APIs
- **V6:** Coordinates components solely through serial and UDP PGN streams managed inside AgIO and the WinForms host, leaving no typed API surface between modules.【F:docs/SRS/sections/4X_Interprocess_Communications/41-ADR-002 - Expose Nexus services over gRPC protobuf contracts.md†L25-L26】
- **Legacy Dev:** Experiments with SocketCAN and PGN normalization, but still relies on the same PGN transports instead of an extensible contract layer.【F:docs/SRS/sections/4X_Interprocess_Communications/41-ADR-002 - Expose Nexus services over gRPC protobuf contracts.md†L28-L29】
- **Nexus direction:** Establishes gRPC with protobuf IDLs as the authoritative service boundary so Core, UI, plugins, and automation tools share strongly typed, versioned contracts while the Bridge translates to legacy PGNs during migration.【F:docs/SRS/sections/4X_Interprocess_Communications/41-ADR-002 - Expose Nexus services over gRPC protobuf contracts.md†L6-L22】

### MCU Communications and Field Buses
- **V6:** Uses the classic PGN frame (0x80/0x81 header plus CRC) across UDP and serial links, so firmware exchanges fixed-width byte payloads without schemas.【F:docs/SRS/sections/4X_Interprocess_Communications/42-ADR-006 - MCU communications over AOG-Link (nanopb).md†L53-L54】
- **Legacy Dev:** Normalizes those same PGNs (including SocketCAN bridges) yet stays on the legacy framing instead of adopting a typed datagram protocol.【F:docs/SRS/sections/4X_Interprocess_Communications/42-ADR-006 - MCU communications over AOG-Link (nanopb).md†L56-L57】
- **Nexus direction:** Introduces AOG-Link, a nanopb-based datagram with shared protobuf payloads, consistent framing across Ethernet, serial, and CAN, and Bridge translation layers for coexistence with legacy PGNs.【F:docs/SRS/sections/4X_Interprocess_Communications/42-ADR-006 - MCU communications over AOG-Link (nanopb).md†L7-L21】

### Working Directory Layout and Artifact Compatibility
- **V6:** Creates a single `%USERPROFILE%\Documents\AgOpenGPS` tree and keeps all vehicle, tool, and UI state in one `Properties.Settings` aggregate, so field archives and machine profiles are tightly coupled and difficult to share across rigs.【F:docs/porting/Legacy-Dev-Excerpts.md†L7-L19】
- **Legacy Dev:** Moves the suite under `%USERPROFILE%\Documents\AOG`, splits `Vehicles`, `Tools`, and `Fields` into dedicated folders, and persists separate XML payloads for the user display profile, vehicles, and tools, breaking backward compatibility with V6 exports.【F:docs/porting/Legacy-Dev-Excerpts.md†L23-L44】
- **Nexus direction:** Uses importers that translate V6/Dev directories into typed `MachineProfile` aggregates and workspace assets so legacy folders can be staged, validated, and versioned without copying raw settings files.【F:docs/porting/LegacyDataIngest.md†L24-L41】

### Configuration Segmentation and Profile Management
- **V6:** Relies on the monolithic `Properties.Settings.Default` blob where UI, hydraulics, tool geometry, and GNSS preferences live side by side, making profile swaps brittle and hard to audit.【F:docs/porting/Legacy-Dev-Excerpts.md†L33-L39】
- **Legacy Dev:** Promotes explicit `User`, `Vehicle`, and `Tool` settings classes and loads each from its own XML file so operators can mix and match tractors, implements, and display preferences without touching the others.【F:docs/porting/Legacy-Dev-Excerpts.md†L40-L44】
- **Nexus direction:** Consolidates legacy inputs into the `MachineProfile` translator and validation harness, then exposes declarative profiles through gRPC/CLI tooling so vehicles, implements, and UI shells can be versioned independently while still supporting PGN bridges.【F:docs/porting/LegacyDataIngest.md†L24-L48】 It layers Presets/Layout services so operators apply live-linked or snapshot layouts alongside equipment presets without re-editing every profile.【F:docs/SRS/sections/9X_Frontends_Ops/91-ADR-032 - Presets and Layout Linking for Equipment Workflows.md†L7-L35】

### Spatial Constraint Governance
- **V6:** Treats boundaries/headlands as simple text exports with manual overrides, providing no keep-out or work-disabled semantics for automation to enforce.【F:docs/porting/V6-Functionality-Gap-Analysis.md†L16-L25】【F:docs/aog-v6-mapping-brief.md†L23-L34】
- **Legacy Dev:** Mirrors the same boundary/headland-only model, leaving constraint enforcement to operator discretion rather than deterministic gating.【F:docs/porting/Legacy-Dev-Excerpts.md†L46-L61】
- **Nexus direction:** Establishes a ZoneService with buffered boundary, headland, keep-out, and work-disabled polygons so guidance and sections honor constraint gates, log overrides, and display canonical symbology across UIs.【F:docs/SRS/sections/7X_Mapping_Geospatial/72-ADR-027 - Spatial Constraints & Zone Policies.md†L7-L64】

### Simulation and Determinism
- **V6:** Embeds simulation inside the monolithic `CSim` helper, synthesizing GNSS/IMU data in-process without a shared bus or plugin hooks, which limits reuse and determinism.【F:docs/SRS/sections/2X_System_Architecture/21-ADR-004 - Establish the composite simulation fabric (SimClock + SimBus).md†L29-L30】
- **Legacy Dev:** Relies on standalone tools (e.g., ModSim) and direct WinForms wiring, so no authoritative clock or bus exists for multiple modules to share without duplicated logic.【F:docs/SRS/sections/2X_System_Architecture/21-ADR-004 - Establish the composite simulation fabric (SimClock + SimBus).md†L32-L33】
- **Nexus direction:** Builds a composite simulation fabric with a Core-governed SimClock, typed SimBus topics, and deterministic source routing so hardware, replay, and plugins remain in sync across CI and operator workflows.【F:docs/SRS/sections/2X_System_Architecture/21-ADR-004 - Establish the composite simulation fabric (SimClock + SimBus).md†L6-L26】

### Extensibility and Plugin Model
- **V6:** Treats extensions as edits inside the monolithic solution—executables ship without manifests, permission boundaries, or lifecycle governance.【F:docs/SRS/sections/9X_Frontends_Ops/94-ADR-018 - Plugin API Capability Discovery and Runtime Model.md†L36-L38】
- **Legacy Dev:** Maintains that status-quo (Option O-EXT-0), so contributors must fork and rebuild the suite with no security or lifecycle isolation for add-ons.【F:docs/SRS/sections/9X_Frontends_Ops/94-ADR-018 - Plugin API Capability Discovery and Runtime Model.md†L40-L41】
- **Nexus direction:** Defines an out-of-process, manifest-driven plugin architecture over gRPC with explicit capabilities, permissions, health leases, and declarative UI contributions, keeping Core minimal while enabling safe extensibility.【F:docs/SRS/sections/9X_Frontends_Ops/94-ADR-018 - Plugin API Capability Discovery and Runtime Model.md†L6-L33】 The accepted stack responsibilities ADR documents how firmware, Bridge, Core, plugins, and UI shells divide ownership so extensions cannot bypass safety-critical boundaries.【F:docs/SRS/sections/2X_System_Architecture/21-ADR-028 - Nexus stack responsibilities & handoff boundaries.md†L1-L118】

## Operator Experience (UX) Differences

### Desktop Shell and Platform Reach
- **V6:** Operators interact through WinForms with selective WPF panels, so the experience is Windows-only and lacks a cross-platform shell.【F:docs/SRS/sections/1X_Platform_Foundations/13-ADR-003 - Use Avalonia for the cross-platform Nexus UI shell.md†L24-L26】
- **Legacy Dev:** Follows the same pattern, leaving Linux or remote clients to rely on workarounds such as remote desktop mirroring.【F:docs/SRS/sections/1X_Platform_Foundations/13-ADR-003 - Use Avalonia for the cross-platform Nexus UI shell.md†L28-L29】
- **Nexus direction:** Elevates Avalonia as the primary desktop shell consuming the shared gRPC contracts, delivering a single UI codebase that runs natively on Windows and Linux (x64/ARM64) with touch-friendly layouts and optional host shells.【F:docs/SRS/sections/1X_Platform_Foundations/13-ADR-003 - Use Avalonia for the cross-platform Nexus UI shell.md†L6-L22】

### Mobile companions and embedded expansion
- **V6:** Ships no native mobile clients; any tablet workflow depends on remote desktop mirrors or web widgets with limited control fidelity.
- **Legacy Dev:** Mirrors the same Windows-only expectation, so remote monitoring still requires external remote-desktop tooling and offers no pathway to run Core on-device.
- **Nexus direction:** Reuses the Avalonia codebase across Windows, Linux, Android, and iOS by introducing CompanionRemote, LocalInProc, and LocalOutOfProc run modes backed by DI-swappable transports, allowing the same app to start as a remote companion and later embed Core and AgIO on Android hardware while iOS stays remote-first over gRPC-Web.【F:docs/SRS/sections/1X_Platform_Foundations/13-ADR-003 - Use Avalonia for the cross-platform Nexus UI shell.md†L24-L44】【F:docs/SRS/sections/9X_Frontends_Ops/91_UI_Shell_Layout.md†L26-L72】

### Remote Displays, Metadata-Driven Panels, and Simulation Controls
- **V6:** Keeps operators on the Windows desktop suite (AgOpenGPS + AgIO + utilities) with manual wiring for dashboards and simulation tools, limiting remote or declarative UI experiences.【F:docs/SRS/sections/9X_Frontends_Ops/91_UI_Shell_Layout.md†L3-L24】【F:docs/SRS/sections/2X_System_Architecture/21-ADR-004 - Establish the composite simulation fabric (SimClock + SimBus).md†L29-L33】
- **Legacy Dev:** Continues focusing on the same Windows suite, so remote display/control remains ad hoc and dashboards are still hand-crafted rather than metadata-driven.【F:docs/SRS/sections/9X_Frontends_Ops/91_UI_Shell_Layout.md†L3-L33】
- **Nexus direction:** Plans metadata-driven dashboards, remote clients that attach over the Core APIs, and a unified simulation bar tied to the authoritative SimClock so operators blend hardware, replay, and plugin scenarios without context switching.【F:docs/SRS/sections/9X_Frontends_Ops/91_UI_Shell_Layout.md†L10-L34】【F:docs/SRS/sections/9X_Frontends_Ops/91_UI_Shell_Layout.md†L38-L67】

### Plugin-Contributed UI and Safety Awareness
- **V6:** Has no manifest or capability system, so any UI extension requires shipping new binaries and offers no built-in safety gating for control panels.【F:docs/SRS/sections/9X_Frontends_Ops/94-ADR-018 - Plugin API Capability Discovery and Runtime Model.md†L36-L38】
- **Legacy Dev:** Shares the same limitation; contributors rebuild the host UI to add panels, keeping safety-critical controls intertwined with core windows.【F:docs/SRS/sections/9X_Frontends_Ops/94-ADR-018 - Plugin API Capability Discovery and Runtime Model.md†L40-L41】
- **Nexus direction:** Requires plugins to declare panels, overlays, and config pages via schema-driven manifests while Core enforces permission-aware visibility so monitor-only clients stay safe and automation panels appear only when authorized.【F:docs/SRS/sections/9X_Frontends_Ops/94-ADR-018 - Plugin API Capability Discovery and Runtime Model.md†L9-L24】

### Field and Job Workflow
- **V6:** Carries a single active job inside the field folder and relies on manual exports or “Field From Existing” workflows when operators want to resume different passes of the same boundary.【F:docs/aog-v6-mapping-brief.md†L31-L36】
- **Legacy Dev:** Stores each job in its own subdirectory beneath a field (`Fields/<Field>/Jobs/<Job>`), letting operators resume previous passes with painted coverage and sections intact without cloning the base field.【F:docs/porting/Legacy-Dev-Excerpts.md†L46-L61】
- **Nexus direction:** Plans declarative workspace manifests that import legacy boundaries, coverage, and per-job artifacts into versioned datasets so replay, analysis, and Avalonia UIs can target any saved job run while the bridge feeds PGN hardware.【F:docs/porting/LegacyDataIngest.md†L8-L41】 The JobsService formalizes those manifests through versioned metadata, Drive-In discovery, and lifecycle hooks so sessions resume cleanly across Core, UI, and plugins.【F:docs/SRS/sections/6X_Core_Domain_Services/62-ADR-030 - Field job sessions and lifecycle services.md†L7-L86】

### Integrated Rate and Tool Steering Control
- **V6:** Depends on external utilities (e.g., rate-control or tool-steer sketches) with minimal desktop integration, so nozzle rates and implement steering require manual PGN wiring and ad hoc profiles.【F:docs/SRS/references/AgIO_PGN_Baseline.md†L94-L115】
- **Legacy Dev:** Adds on-screen nozzle rate management and a dedicated tool-steer panel that publish PGNs directly from the host, providing built-in calibration sliders and safety toggles for implements.【F:docs/porting/Legacy-Dev-Excerpts.md†L65-L78】
- **Nexus direction:** Treats rate control and tool steering as first-class plugin capabilities exposed through the Bridge/API surface, with regression harnesses that verify Nexus outputs against captured V6 scenarios before shipping drivers.【F:docs/porting/LegacyDataIngest.md†L37-L43】

## Nexus Bridging Strategy

Across each area, Nexus couples the modernized runtime and UX with bridges that keep legacy deployments productive:
- The Bridge service translates gRPC contracts to legacy PGNs and back, letting V6/Dev hardware coexist while Nexus services adopt typed APIs.【F:docs/SRS/sections/4X_Interprocess_Communications/41-ADR-002 - Expose Nexus services over gRPC protobuf contracts.md†L9-L22】
- AOG-Link is designed to run alongside PGN devices during migration, supporting Ethernet, serial, and CAN transports without forcing immediate firmware rewrites.【F:docs/SRS/sections/4X_Interprocess_Communications/42-ADR-006 - MCU communications over AOG-Link (nanopb).md†L7-L21】【F:docs/SRS/sections/4X_Interprocess_Communications/42-ADR-006 - MCU communications over AOG-Link (nanopb).md†L29-L34】
- Metadata-driven UI contributions and plugin manifests allow gradual adoption—operators can continue using the Windows suite while Avalonia shells, remote clients, and declarative dashboards reach parity before becoming defaults.【F:docs/SRS/sections/1X_Platform_Foundations/13-ADR-003 - Use Avalonia for the cross-platform Nexus UI shell.md†L6-L22】【F:docs/SRS/sections/9X_Frontends_Ops/91_UI_Shell_Layout.md†L10-L67】

These guardrails ensure Nexus addresses the structural and UX gaps between legacy V6 and Dev without stranding existing rigs.
