# Nexus SRS Notes

This living document captures clarifications, decisions, and follow-ups that arise while
executing the Nexus plan. Keep it concise, reference specific SRS sections, and link ADRs
where applicable. Move resolved items into the canonical SRS when they graduate from notes
status.

## 2024-xx-xx Ported Math Verification Report

- NX-059 packages the parity datasets and regression notes in
  [`docs/porting/PortedMathVerification.md`](../porting/PortedMathVerification.md) for SRS §5.8.
- Coverage analytics deltas stay within ±0.10 square metres/percentage points while
  exercising the tolerance guard-rails added in NX-055.
- Section control parity scenarios replay V6 captures across speed gating,
  look-ahead activation, and manual suppression paths, with the CSV harness ready for
  future regression captures.
- AutoSteer-Lite tuning checks confirm the translated machine profile reproduces the
  legacy look-ahead multipliers and scaling heuristics documented in NX-058.

## 2024-xx-xx Windows AGiO Serial Autoscanner

- NX-022 introduces the `Aog.Agio.Windows` backend with a COM port auto-scan service and checksum-validated
  GGA/RMC/VTG parser. The scanner logs the detected port/baud pair and latest fix metadata while awaiting the
  upcoming GNSS gRPC wiring.

## 2024-xx-xx Task Crosswalk Anchors

These anchors map the task tracker sections to the evolving SRS outline so backlog entries
can link directly into requirements discussions. Update the references if the canonical SRS
documents move or headings change.

### SRS §21 System Decomposition & Boundaries
- Covers repo scaffolding, ADRs, protobuf, JSON schemas, and capability discovery.

### SRS §14 Build Environment & Tooling
- Includes CI matrices, dev scripts, and packaging automation.

### SRS §96 Quality Engineering & Release
- Tracks quick-start and how-to guides for early adopters.

### SRS §61 Core Domain Services
- Details the headless host, configuration system, event bus, and sim primitives.

### SRS §63 Layers Registry & Journal Contracts
- Describes the Core ↔ AGiO capability negotiation contract.
- Initial proto
  ([`proto/capabilities.proto`](../../Nexus%20SourceCode/proto/capabilities.proto)) defines
  `HandshakeRequest`/`HandshakeResponse`
  with node identity, session identifier, declared capabilities, and rejection metadata to
  unblock Core↔AGiO smoke tests.

### SRS §52 AgIO Service
- Defines hardware backends, timing probes, and legacy gateways.

### SRS §91 UI Shell & Layout
- Covers the Avalonia shell, connection panels, scenario editor, and sim bar.

### SRS §81 Guidance Orchestrator & Simulation
- Registers plugin-provided simulators and shared simulation helpers.

### SRS §82 Planning
- Requirements for planner orchestration, catalog caching, and refresh policies.

### SRS §73 Variable Mapping & Section Control
- Requirements for the Sections plugin and coverage gating.

### SRS §74 Monitoring Systems
- Requirements for planter monitoring services and UI.

### SRS §33 Offline-first, Sync & Replay
- Requirements for replay providers and UI wiring.

### SRS §96 Quality Engineering & Release
- Documents deterministic sim regression, heartbeats, and arming state machine.

### SRS §53 AOG-Link Compatibility
- Requirements for UDP discovery, PGN bridging, and UART framing.

### SRS §61 Core Domain Porting Inventory
- Enumerates legacy algorithms slated for evaluation and porting.

### SRS §73 Coverage & Variable Rate Math
- Specifies coverage computation parity targets.

### SRS §83 Autosteer Target Models
- Specifies controller coverage, fallback parity, and tuning interfaces.

### SRS §61 Kinematics & Controller Gains
- Specifies controller tuning expectations for AutoSteer-Lite.

### SRS §96 Verification & Regression Harnesses
- Captures parity datasets and tolerance guard-rails for ported coverage, section
  control, and AutoSteer math.
- Reference: [`docs/porting/PortedMathVerification.md`](../porting/PortedMathVerification.md).

## 2024-xx-xx Bootstrap

- The engineering plan outlined in [`options/1X/O-STACK-1_DotNet8Avalonia.md`](options/1X/O-STACK-1_DotNet8Avalonia.md)
  is now the active roadmap for Nexus development. All early-phase tasks (Waves 0–2) should
  align with this stack (C#/.NET 8 + Avalonia + gRPC contracts).
- [`tasks.md`](../../tasks.md) tracks the NX-### backlog described in the Engineering Brief.
  Keep it in sync with the SRS milestones and update milestone tags when waves complete.
- Contracts-first policy: define protobuf and JSON schemas before implementing dependent
  services. Once the initial versions land, freeze updates for 72 hours to stabilise
  downstream work.
- Core host health logging honours configuration reloads; updating
  `CoreHost:Health:IntervalSeconds` adjusts the heartbeat cadence without restarting the
  process, and logs confirm when changes are applied.

## Open Questions

- Assign CODEOWNERs for proto, schemas, core, AGiO, plugins, UI. Capture the assignments in
  [`AGENTS.md`](../../AGENTS.md) and create `/CODEOWNERS` to match.
- Define validation tooling for JSON schemas (likely `dotnet` global tool or simple script)
  so CI can enforce schema compliance from Wave 1 onward.

## Related ADRs

- [ADR-001 — Adopt .NET 8 C# Stack](../ADR/ADR-001-dotnet8-runtime.md)
- [ADR-004 — Composite Simulation](../ADR/ADR-004-composite-simulation.md)
- [ADR-018 — Plugin API](../ADR/ADR-018-plugin-api.md)
- [ADR-028 — Stack Boundaries](../ADR/ADR-028-stack-boundaries.md)

Add new sections chronologically with newest entries at the top.
