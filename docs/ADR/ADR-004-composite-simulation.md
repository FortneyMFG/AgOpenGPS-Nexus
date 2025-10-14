# ADR-004: Establish the composite simulation fabric (SimClock + SimBus)

## Status
Accepted

## Context
Nexus development depends on deterministic simulation for CI, operator training, and plugin validation. The backend and extensibility sections highlight the need for a composite simulation loop where Core owns the authoritative clock, plugins publish to a shared bus, and hardware inputs can pre-empt simulated data without duplicating routing logic.【F:docs/SRS/sections/04_Backend_Services.md†L1-L70】【F:docs/SRS/sections/12_Extensibility_Plugins.md†L18-L71】 Option O-STACK-1 reinforces this model by positioning AgIO’s simulation backend alongside Windows and Linux backends using the same contracts.【F:docs/SRS/options/O-STACK-1_DotNet8Avalonia.md†L9-L47】

## Decision
Create a composite simulation fabric governed by the Core service:
- **SimClock** — fixed-step, seekable clock (default 10 ms) that drives Core processing, replay, and plugin simulators.
- **SimBus** — typed publish/subscribe channel with last-value caching for canonical topics (pose, IMU, sections, telemetry) shared by hardware and simulated producers.
- **Source routing** — priority rules owned by Core that select hardware, simulation, or replay producers per topic so hardware-in-the-loop overrides remain deterministic.
Plugins register simulation providers against this fabric and must respect seeded RNGs and SimClock state to guarantee reproducibility across machines and CI lanes.

## Consequences
- Positive impacts
  - Deterministic replay and simulation flows accelerate development, CI, and operator training.
  - Shared fabric avoids per-plugin duplication of timing, routing, and state management.
  - Enables hybrid scenarios where real hardware augments simulated data without bespoke wiring.
- Negative/mitigated impacts
  - Requires rigorous topic/version governance to keep SimBus schemas stable; mitigated by aligning with the gRPC contracts and layer registries.
  - Adds scheduling complexity inside Core; addressed via profiling and headless smoke tests.
- Follow-up actions
  - Define the topic catalog and schema ownership within the forthcoming protobuf contracts (NX-003/NX-004).
  - Implement regression vectors and seeded scenarios as part of the CI smoke suite (future NX testing tasks).

## Legacy Implementation Notes
### AgOpenGPS v6
- Simulation lives inside the monolithic `CSim` helper, which synthesizes GNSS/IMU data in-process without a shared bus or external plugin hooks, limiting reuse and determinism across tools.【F:docs/porting/V6-Inventory.md†L45-L49】

### Legacy Dev Branch
- Current dev tooling still depends on standalone utilities such as ModSim and direct wiring in the WinForms app, so there is no authoritative clock/bus that multiple modules can share without duplicating logic.【F:docs/SRS/sections/05_Frontends.md†L7-L17】【F:docs/SRS/sections/12_Extensibility_Plugins.md†L51-L56】

## References
- [Section 04 — Backend Services](../SRS/sections/04_Backend_Services.md)
- [Section 12 — Extensibility & Plugins](../SRS/sections/12_Extensibility_Plugins.md)
- [Option O-STACK-1 — .NET 8 + Avalonia stack](../SRS/options/O-STACK-1_DotNet8Avalonia.md)
