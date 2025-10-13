# Nexus (AgOpenGPS Next Generation)

Nexus is the from-scratch successor to AgOpenGPS. This repository collects the plans,
requirements, and source roots for building a modular, cross-platform guidance and
application platform that cleanly separates the core orchestrator, hardware I/O host,
plugin ecosystem, and front-end experiences.

## Mission Snapshot

- **Modular architecture:** Core, AGiO backends, plugins, and UI live in dedicated
  projects that communicate over gRPC contracts generated from `/Nexus SourceCode/proto`.
- **Simulation-first:** A composite simulator drives deterministic development,
  headless testing, and the “Sim Bar” experience described in
  [`docs/SRS/options/O-STACK-1_DotNet8Avalonia.md`](docs/SRS/options/O-STACK-1_DotNet8Avalonia.md).
- **Legacy friendly:** Teensy/ESP32 AIO users stay productive through UART/Ethernet
  compatibility layers and UDP gateways.
- **Target platforms:** Windows desktops and Raspberry Pi/CM5 hosts, with or without
  external hardware beyond optional GPS.

AgOpenGPS Nexus starts development here, following the engineering blueprint captured in
[`docs/SRS/options/O-STACK-1_DotNet8Avalonia.md`](docs/SRS/options/O-STACK-1_DotNet8Avalonia.md)
and the broader Software Requirements Specification (SRS) set under `/docs/SRS`.

## Read Me First

1. **SRS is authoritative.** Requirements, process flows, and acceptance criteria all
   originate in `/docs/SRS`. Begin each task by reviewing the relevant SRS section.
2. **Track work in `tasks.md`.** Tickets are organised by lane and wave so AIs and humans
   can stay out of each other’s way. Update statuses and human-verification fields as you
   close out work.
3. **Follow the contribution playbook.** The root `AGENTS.md` spells out ownership bands,
   branch naming, PR expectations, and how contract freezes are handled.
4. **Log discoveries in the SRS notes.** When clarifying requirements or implementation
   constraints, append them to `docs/SRS/NOTES.md` so the next contributor benefits.

## Architecture Decision Records (ADRs)

- [ADR-001: Adopt .NET 8 C# stack for Nexus runtime](docs/ADR/ADR-001-dotnet8-runtime.md)
- [ADR-002: Expose Nexus services over gRPC/protobuf contracts](docs/ADR/ADR-002-grpc-contracts.md)
- [ADR-003: Use Avalonia for the cross-platform Nexus UI shell](docs/ADR/ADR-003-avalonia-ui.md)
- [ADR-004: Establish the composite simulation fabric (SimClock + SimBus)](docs/ADR/ADR-004-composite-simulation.md)

## Repository Layout

```text
/
├── docs/                  # SRS, ADRs, how-tos, templates
│   ├── SRS/               # System Requirements (single source of truth)
│   └── NOTES.md           # Living notes about the SRS canon and clarifications
├── Nexus SourceCode/      # Nexus .NET 8 solution (Avalonia UI bootstrap, tests)
│   ├── AgOpenGPS.Nexus.sln
│   ├── Directory.Build.props
│   ├── README.md
│   ├── src/               # Production projects (Avalonia shell lives here)
│   └── tests/             # Test projects
├── Legacy SourceCode -V6/ # Historical AgOpenGPS materials for reference
├── tasks.md               # Active backlog with per-lane ticket tracking
├── AGENTS.md              # Contribution conventions and automation guardrails
└── README.md              # This document
```

## Getting Started

1. Fork or clone the repo.
2. Read `AGENTS.md` for contribution rules, CODEOWNER areas, and PR workflow.
3. Pick a ticket from `tasks.md`, review the linked SRS content, and create a
   short-lived feature branch (`feat/NX-###-short-label`).
4. Ship code, docs, and tests together. Every change should leave the repo runnable and
   well-documented.

### Dev scripts

Cross-platform helpers live under `tools/scripts`:

- `./tools/scripts/nexus.sh run core` (or `agio`/`ui`) runs the relevant host via
  `dotnet run` on Unix-like systems. Pass additional arguments after `--` to forward them
  to the underlying host.
- `pwsh ./tools/scripts/nexus.ps1 sim` launches the simulation entry point on Windows
  PowerShell (Core or Desktop). Until the dedicated SimHost lands, both scripts reuse
  the Core host for `sim`; override the default by setting `NEXUS_SIM_PROJECT` once the
  simulation host project is available.

Override default project locations by exporting `NEXUS_CORE_PROJECT`,
`NEXUS_AGIO_PROJECT`, `NEXUS_UI_PROJECT`, or `NEXUS_SIM_PROJECT`. All paths are resolved
relative to the repository root so future solution files can slot in without editing the
scripts.

### Packaging

- `pwsh ./tools/ci/package-windows.ps1` builds the Windows single-file publish, `.zip`,
  and installer bundle described in [docs/howto/windows-packaging.md](docs/howto/windows-packaging.md).
- `pwsh ./tools/ci/release.ps1 -Channel nightly -Version 0.5.0-beta1` orchestrates the signed
  release pipeline. It reuses the platform-specific packaging scripts, optionally signs
  Windows executables when a certificate is provided, stages artifacts under
  `artifacts/release/<channel>/<timestamp>`, and emits a manifest with SHA-256 hashes for
  downstream promotion.

When Wave 0 tickets (NX-001, NX-002, NX-006) are completed the repository skeleton will be
ready for the broader contract and simulation work described in the phase plan. Tag
milestones as outlined in the engineering brief to keep parallel teams aligned.

## Additional Resources

- [AgOpenGPS community forum](https://discourse.agopengps.com/) — stay in touch with power
  users and hardware builders.
- [Legacy boards & firmware](https://github.com/agopengps-official/Boards) — reference
  designs for Teensy/ESP32 controllers.
- [Avalonia UI](https://www.avaloniaui.net/) — cross-platform UI framework targeted in the
  O-STACK plan.

Nexus development has officially begun. Let’s build the next generation of open precision
agriculture tooling together.
