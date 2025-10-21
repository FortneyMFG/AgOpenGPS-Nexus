# ADR-004: Establish the composite simulation fabric (SimClock + SimBus)

## Status
Accepted

**Relevant Plugin(s):** Simulation & Replay providers, Mapping, Autosteer, Section Control, Rate Control, Telemetry Logging


## Context
Nexus development depends on deterministic simulation for CI, operator training, and plugin validation. The backend and extensibility sections highlight the need for a composite simulation loop where Core owns the authoritative clock, plugins publish to a shared bus, and hardware inputs can pre-empt simulated data without duplicating routing logic.【F:docs/SRS/sections/2X_System_Architecture/21_System_Decomposition_Boundaries.md†L1-L70】【F:docs/SRS/sections/9X_Frontends_Ops/94_Extensibility_Packaging_Updates.md†L18-L71】 Option 11-O1 reinforces this model by positioning AgIO’s simulation backend alongside Windows and Linux backends using the same contracts.【F:docs/SRS/Sections/1X_Platform_Foundations/11-O1_Unified_DotNet8_Avalonia.md†L9-L47】

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

## Governance Updates
- **SimBus topic registry.** Core maintains a signed YAML registry enumerating topic names, payload schemas, version history, and maximum payload sizes. Pull requests that introduce new topics must update the registry and attach determinism fixtures before CI accepts the change.
- **Determinism lint tooling.** A command-line validator rejects builds when topics lack registered schemas or publish payloads exceeding size budgets. Plugin authors receive local tooling to rehearse registration before opening PRs.
- **Regression fixture cadence.** Quarterly scenario packs replay weather, GNSS drift, and failure injections. New topics must supply at least two chaos scripts (e.g., packet duplication, latency spikes) that Core incorporates into the shared suite.

## Amendment — 2025 architecture refresh (NX-190)

- Replay fixtures now cover multi-field job envelopes (ADR-043) and session timelines (ADR-041). Scenario packs include start/stop session sequences, collaborative zone edits, and layer reuse to verify provenance in headless runs.
- LayerEditEvent journals emitted from ADR-044 editing sessions must replay deterministically. The fixture catalog adds TODOs for collaborative edit meshes once ADR-047 mesh replication ships.
- Profit, genetics, and yield plugins consume replay outputs to validate cross-plugin analytics. Sim harnesses capture their layers and compare planned vs. actual aggregates as part of CI.

## Legacy Implementation Notes
### AgOpenGPS v6
- Simulation lives inside the monolithic `CSim` helper, which synthesizes GNSS/IMU data in-process without a shared bus or external plugin hooks, limiting reuse and determinism across tools.【F:docs/porting/V6-Inventory.md†L45-L49】

### Legacy Dev Branch
- Current dev tooling still depends on standalone utilities such as ModSim and direct wiring in the WinForms app, so there is no authoritative clock/bus that multiple modules can share without duplicating logic.【F:docs/SRS/sections/9X_Frontends_Ops/91_UI_Shell_Layout.md†L7-L17】【F:docs/SRS/sections/9X_Frontends_Ops/94_Extensibility_Packaging_Updates.md†L51-L56】

## References
- [Section 21 — System Decomposition & Boundaries](../SRS/sections/2X_System_Architecture/21_System_Decomposition_Boundaries.md)
- [Section 94 — Extensibility, Packaging & Updates](../SRS/sections/9X_Frontends_Ops/94_Extensibility_Packaging_Updates.md)
- [Option 11-O1 — Unified .NET 8 + Avalonia stack](../SRS/Sections/1X_Platform_Foundations/11-O1_Unified_DotNet8_Avalonia.md)
