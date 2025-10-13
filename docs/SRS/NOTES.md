# Nexus SRS Notes

This living document captures clarifications, decisions, and follow-ups that arise while
executing the Nexus plan. Keep it concise, reference specific SRS sections, and link ADRs
where applicable. Move resolved items into the canonical SRS when they graduate from notes
status.

## 2024-xx-xx Task Crosswalk Anchors

These anchors map the task tracker sections to the evolving SRS outline so backlog entries
can link directly into requirements discussions. Update the references if the canonical SRS
documents move or headings change.

### SRS §2.1 Foundations & Contracts
- Covers repo scaffolding, ADRs, protobuf, JSON schemas, and capability discovery.

### SRS §2.7 Packaging & DevEx
- Includes CI matrices, dev scripts, and packaging automation.

### SRS §2.8 Documentation
- Tracks quick-start and how-to guides for early adopters.

### SRS §3.1 Core Services
- Details the headless host, configuration system, event bus, and sim primitives.

### SRS §3.2 Capabilities Exchange
- Describes the Core ↔ AGiO capability negotiation contract.

### SRS §3.3 AGiO Services
- Defines hardware backends, timing probes, and legacy gateways.

### SRS §3.4 UI Shell
- Covers the Avalonia shell, connection panels, scenario editor, and sim bar.

### SRS §3.5 Simulation Providers
- Registers plugin-provided simulators and shared simulation helpers.

### SRS §3.6 AutoSteer
- Requirements for the AutoSteer-Lite plugin and controller integration.

### SRS §3.7 Sections Control
- Requirements for the Sections plugin and coverage gating.

### SRS §3.8 Planter Monitor
- Requirements for planter monitoring services and UI.

### SRS §3.9 Replay Services
- Requirements for replay providers and UI wiring.

### SRS §4.2 Safety & QA
- Documents deterministic sim regression, heartbeats, and arming state machine.

### SRS §4.3 Legacy Compatibility
- Requirements for UDP discovery, PGN bridging, and UART framing.

### SRS §5.1 V6 Porting Inventory
- Enumerates legacy algorithms slated for evaluation and porting.

### SRS §5.2 Coverage Math
- Specifies coverage computation parity targets.

### SRS §5.3 Path Generation
- Specifies AB/curve/headland generation parity targets.

### SRS §5.4 Controller Gains
- Specifies controller tuning expectations for AutoSteer-Lite.

## 2024-xx-xx Bootstrap

- The engineering plan outlined in [`options/O-STACK-1_DotNet8Avalonia.md`](options/O-STACK-1_DotNet8Avalonia.md)
  is now the active roadmap for Nexus development. All early-phase tasks (Waves 0–2) should
  align with this stack (C#/.NET 8 + Avalonia + gRPC contracts).
- `tasks.md` tracks the NX-### backlog described in the Engineering Brief. Keep it in sync
  with the SRS milestones and update milestone tags when waves complete.
- Contracts-first policy: define protobuf and JSON schemas before implementing dependent
  services. Once the initial versions land, freeze updates for 72 hours to stabilise
  downstream work.

## Open Questions

- Assign CODEOWNERs for proto, schemas, core, AGiO, plugins, UI. Capture the assignments in
  `AGENTS.md` and create `/CODEOWNERS` to match.
- Define validation tooling for JSON schemas (likely `dotnet` global tool or simple script)
  so CI can enforce schema compliance from Wave 1 onward.

Add new sections chronologically with newest entries at the top.
