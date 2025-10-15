# Nexus (AgOpenGPS Next Generation)

Nexus is the next-generation AgOpenGPS stack that blends community experience with ambitious 2025 planning. It keeps rigs productive offline, codifies farm → field and season → job → session hierarchies, and opens the door for a governed plugin ecosystem.

## Why Nexus?

- **Offline-first, cloud-optional.** Jobs, sessions, telemetry, and plugins run locally with deterministic folder layouts. Cloud sync is additive and reconciles when devices land back on a network—never a requirement for field work.【F:docs/ADR/ADR-030-field-job-sessions.md†L33-L86】
- **Canonical hierarchies.** Farm → Field geometry and Season → Job → Session operational flows align Core, plugins, and analytics around shared identifiers.【F:docs/SRS/sections/02_DataModel.md†L1-L132】
- **Plugin ecosystem.** Core lifecycle events, the Layer Registry, and manifest governance let crop type, genetics, yield, profit, and telemetry plugins ship independently while sharing provenance and QA policies.【F:docs/ADR/ADR-010-layer-registry-variable-rate.md†L33-L58】【F:docs/ADR/ADR-045_CropTypePlugin.md†L29-L71】【F:docs/ADR/ADR-046_GeneticsPlugin.md†L21-L66】
- **Safety posture.** Remote dashboards default to monitor-only. Telemetry sharing runs through explicit share/subscribe profiles and never grants control without an in-cab lease.【F:docs/SRS/sections/09_Control_Automation.md†L33-L60】【F:docs/plugins/MultiMachine.md†L1-L80】
- **Interop built-in.** ISOXML bridges, external layer ingest, and report builders share the same registry hashes so TaskData, GeoTIFF, and GeoJSON round-trip without drift.【F:docs/ADR/ADR-014-interop-prescription-formats.md†L12-L56】【F:docs/SRS/sections/04_MappingLayers.md†L84-L106】

## Repository Tour

```text
/
├── docs/                  # SRS, ADRs, plugin guides, training, support
│   ├── ADR/               # Architecture Decision Records
│   ├── SRS/               # System Requirements Specification
│   ├── plugins/           # Plugin requirements and planned surfaces
│   ├── INDEX.md           # Quick links into docs
│   └── CONTRIBUTING-PLUGINS.md # Packaging and governance guidance
├── Nexus SourceCode/      # .NET 8 solution (Core, UI, plugins, tests)
├── schemas/               # JSON schemas for jobs, sessions, layers, mesh
├── tasks.md               # Backlog (NX-###) with automation guardrails
└── README.md              # This document
```

## Getting Started

1. Review `docs/INDEX.md` for entry points into the SRS, ADR roadmap, and plugin docs.
2. Pick an NX ticket from `tasks.md`, confirm the owning ADR/SRS sections, and align on scope.
3. Follow `AGENTS.md` for branch naming, ownership bands, and PR expectations. Every change ties to one NX ticket.
4. Run documentation, schema, and test updates together; deterministic storage and replay are core principles.

## Continuous Integration & Release Automation

- The **Nexus CI** workflow (`.github/workflows/ci.yml`) runs on every push and pull request. It restores, builds, and tests the .NET 8 solution on Windows and Linux runners before executing repository linting, contract governance checks, the simulation smoke harness, and packaging smoke tests for each platform.
- The **Nexus Release Packaging** workflow (`.github/workflows/release.yml`) triggers for tags that match `v*` (or manually via workflow dispatch). It rebuilds and tests the solution on dedicated Windows and Linux jobs, packages self-contained single-file binaries using `tools/ci/package-windows.ps1` and `tools/ci/package-linux.ps1`, and publishes zip bundles directly to the GitHub release so operators can download ready-to-run archives for each platform.

## Safety & Remote Access

- Remote dashboards subscribe to telemetry but cannot issue commands unless an operator grants an explicit control lease. Mesh profiles limit what leaves the cab, with profitability and layer edits denied by default.【F:docs/plugins/MultiMachine.md†L1-L80】【F:docs/SRS/sections/09_Control_Automation.md†L33-L60】
- Section control treats advisory `noWorkMask` overlays as guidance only; constraint gates remain in Core and never depend on remote inputs.【F:docs/SRS/sections/09_Control_Automation.md†L61-L71】

## Additional Resources

- [docs/INDEX.md](docs/INDEX.md) — curated links into ADRs, SRS sections, and plugin guides.
- [docs/CONTRIBUTING-PLUGINS.md](docs/CONTRIBUTING-PLUGINS.md) — packaging, manifest, and signing requirements for plugin authors.
- [docs/plugins](docs/plugins) — feature-specific requirements (Mapping, Rate Control, Genetics, Yield, Profit, Multi-Machine, ISOBUS Bridge, Telemetry Logging, Replay, File I/O, and planned Soil/Lab, Map Composer, 3D Terrain).

Nexus continues to evolve in public. Contributions that respect the offline-first, session-aware architecture keep rigs productive today while enabling the ambitious 2025 roadmap.
