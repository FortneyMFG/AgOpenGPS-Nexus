# ADR-028: Nexus stack responsibilities & handoff boundaries

## Status
Accepted

**Relevant Plugin(s):** Full Stack


## Context
Contributors requested a single reference that maps how firmware, hardware services, the Core runtime, and feature plugins divide responsibilities so features can be planned without blurring safety and contract boundaries. Existing SRS sections and ADRs already define expectations for transports, hardware governance, and plugin lifecycle, but they are scattered across documents, making it easy to misplace functionality or duplicate work.【F:docs/SRS/sections/03_Comm_Transports.md†L3-L35】【F:docs/SRS/sections/06_Hardware_IO.md†L3-L27】【F:docs/ADR/ADR-018-plugin-api.md†L6-L34】

## Decision
Adopt the following layered responsibility map. Each layer owns the concerns listed under "Responsibilities" and must not bypass the contractual boundaries called out under "Boundaries". Data and commands always move up-stack through the exposed contracts rather than by reaching around another layer.

| Layer | Responsibilities | Owns / Publishes | Boundaries |
| --- | --- | --- | --- |
| **Field MCUs (AOG-Link)** | Encode GNSS, IMU, section, rate, and actuator telemetry; execute real-time control loops within firmware-defined budgets; honor watchdogs and heartbeat policy. | Nanopb payloads over UDP, RS-485, or CAN using the AOG-Link frame header. | Never speaks gRPC directly; all host coordination goes through the Bridge. Shares schemas with higher layers but cannot change them unilaterally.【F:docs/ADR/ADR-006-aog-link-mcu-communications.md†L6-L44】 |
| **AgIO / Bridge services** | Discover devices, translate between AOG-Link, legacy PGNs, and the generated gRPC contracts; manage hardware permissions, health telemetry, and capability negotiation as a privileged plugin. | gRPC endpoints defined in `Aog.Abstractions`, device enumeration APIs, health metrics, firmware update channels. | Must not embed guidance or automation policy; forwards only typed data and command intents to Core. Enforces permission gates before exposing raw I/O handles.【F:docs/ADR/ADR-006-aog-link-mcu-communications.md†L18-L45】【F:docs/SRS/sections/06_Hardware_IO.md†L22-L27】 |
| **Core runtime (Aog.Core)** | Own the authoritative pose timeline, control arbitration, data persistence, deterministic simulation clock/bus, and a minimal geospatial kernel (CRS math, tiling helpers, frame counter, and timebase). Host the gRPC services consumed by plugins/UI, enforce permissions, and arbitrate source routing between hardware, simulation, and replay. | Pose, Guidance, SectionControl, LayerRegistry, Mapping contracts, EventBus, Health, Config, and Capabilities gRPC services; SimClock + SimBus; storage snapshots. | Contains no feature-specific automation logic—delegates to plugins. All hardware access happens via AgIO; Core interacts only through the generated contracts and permission gates. The embedded geospatial kernel stays contract-focused and ships null implementations so rigs can run without mapping plugins.【F:docs/ADR/ADR-018-plugin-api.md†L12-L33】【F:docs/ADR/ADR-004-composite-simulation.md†L10-L18】【F:docs/SRS/sections/04_Backend_Services.md†L6-L20】【F:docs/aog-v6-mapping-brief.md†L23-L34】 |
| **Feature plugins** | Implement automation, analytics, transports, simulation providers, mapping engines, and UI contributions within declared capabilities (autosteer, section-control, guidance, rate-control, telemetry). Publish/consume Core services via gRPC, respect lifecycle states, and surface health telemetry. | Capability declarations, SectionState/Guidance RPC clients, Mapping RPC clients/publishers, map overlays, simulation topics, telemetry sinks. | Cannot bypass Core arbitration or open hardware directly without explicit permissions. Must honor enable/disable contracts, constraint gates, and deterministic replay requirements enforced by Core. Mapping plugins run out-of-process for optionality and fault isolation while speaking the frozen `Aog.Abstractions.Mapping` contracts exported by Core.【F:docs/ADR/ADR-018-plugin-api.md†L12-L34】【F:docs/SRS/sections/09_Control_Automation.md†L16-L56】【F:docs/SRS/sections/12_Extensibility_Plugins.md†L6-L80】【F:docs/aog-v6-mapping-brief.md†L35-L53】 |
| **UI shells** | Render plugin and Core data, collect operator intent, expose configuration, and drive simulation controls. Remain declarative consumers of Core/Plugin APIs. | Avalonia panels, dashboards, configuration flows, SimClock controls. | No direct hardware access; all commands route through Core services so automation and safety logs remain authoritative.【F:docs/ADR/ADR-003-avalonia-ui.md†L15-L28】【F:docs/ADR/ADR-018-plugin-api.md†L12-L26】 |

### Plugin capability bands

The plugin manifest declares the capability bands below. Each band maps to the Core contracts it is allowed to call, the data it produces, and the guard rails it must follow.

| Capability band | Typical modules | Required Core surfaces | Key guard rails |
| --- | --- | --- | --- |
| **Guidance** | AB line solvers, headland planners, route optimizers. | PoseStream, Guidance, ZoneService, LayerRegistry. | Must respect SimClock ordering, zone masks, and constraint gating before emitting waypoints or lookahead cues.【F:docs/ADR/ADR-004-composite-simulation.md†L10-L18】【F:docs/SRS/sections/09_Control_Automation.md†L16-L56】 |
| **Autosteer** | Steering controllers, multi-axle kinematics solvers. | PoseStream, Guidance command RPCs, SectionControl (for interlocks), Health. | Commands only apply when Core issues an automation lease; must log operator acknowledgements and drop outputs when constraints disable automation.【F:docs/SRS/sections/09_Control_Automation.md†L16-L56】 |
| **Section & rate control** | Section relays, variable-rate controllers, nozzle diagnostics. | SectionControl, LayerRegistry, TileQuery, Pose zone masks. | Enforce on/auto/off graph, obey keep-out/headland zones, publish health metrics for audits.【F:docs/SRS/sections/09_Control_Automation.md†L16-L56】【F:docs/SRS/sections/12_Extensibility_Plugins.md†L70-L80】 |
| **Telemetry & analytics** | Yield monitors, layer aggregators, replay/telemetry sinks. | EventBus, LayerRegistry, Storage export APIs. | Read-only unless granted `storage.write`; must respect deterministic replay expectations when simulating data.【F:docs/ADR/ADR-018-plugin-api.md†L12-L33】【F:docs/SRS/sections/12_Extensibility_Plugins.md†L48-L80】 |
| **Hardware bridges** | AgIO, ISOBUS transport plugins, sensor gateways. | Device enumeration, hardware lease RPCs, Pose/Section publishers (as producers). | Operate as privileged plugins with device permissions; publish diagnostics and obey health leasing semantics.【F:docs/SRS/sections/06_Hardware_IO.md†L22-L27】【F:docs/ADR/ADR-018-plugin-api.md†L16-L34】 |

### Data flow checklist

1. **Firmware to host:** MCUs encode telemetry in AOG-Link frames. The Bridge validates headers, applies CRC/sequence rules, and translates payloads to the gRPC contracts shared with Core.【F:docs/ADR/ADR-006-aog-link-mcu-communications.md†L12-L44】
2. **Hardware arbitration:** AgIO applies permission gates and reports device health before forwarding pose, section, and rate topics into Core. Legacy PGNs stay encapsulated behind the same gateway.【F:docs/ADR/ADR-006-aog-link-mcu-communications.md†L18-L45】【F:docs/SRS/sections/06_Hardware_IO.md†L22-L54】
3. **Core orchestration:** Core ingests the canonical streams, chooses authoritative producers via source routing, and exposes the resulting state through deterministic services, SimClock, and SimBus.【F:docs/ADR/ADR-004-composite-simulation.md†L10-L18】【F:docs/ADR/ADR-018-plugin-api.md†L12-L26】
4. **Plugin execution:** Plugins subscribe to the exported services, apply their domain logic, and emit commands or analytics only within the scopes granted in their manifest. Health and lease updates flow back into Core for supervision.【F:docs/ADR/ADR-018-plugin-api.md†L12-L34】
5. **Operator interface:** UI shells consume Core and plugin feeds, drive configuration, and display health/constraint status without bypassing arbitration, preserving a single audit trail.【F:docs/ADR/ADR-003-avalonia-ui.md†L15-L28】【F:docs/ADR/ADR-018-plugin-api.md†L12-L26】

## Governance Updates
- **Drift monitoring.** Quarterly audits review each layer against the responsibility matrix. Variances become NX tasks with owners and due dates, and the report archives live in the architecture workspace.
- **CI guardrails.** Static analysis checks flag cross-layer references. Pull requests adding new dependencies must include a justification linking to ADR updates or waivers approved by architecture leads.
- **Program reporting.** Release notes summarize boundary audits so stakeholders know which exceptions remain and when remediation is scheduled.

## Consequences
- **Aligned planning:** Contributors can assign features to the correct layer without reopening earlier ADRs because the responsibilities table summarizes contract boundaries.【F:docs/ADR/ADR-018-plugin-api.md†L12-L34】
- **Safety clarity:** Automation developers see where constraint gates, leases, and watchdogs live, reducing the chance of bypassing Core arbitration.【F:docs/SRS/sections/09_Control_Automation.md†L16-L56】
- **Documentation debt reduction:** Newcomers no longer need to mine multiple ADRs/SRS sections to understand the stack boundary between AgIO, Core, and plugins, improving onboarding and review discussions.【F:docs/SRS/sections/03_Comm_Transports.md†L3-L35】【F:docs/SRS/sections/06_Hardware_IO.md†L3-L27】

