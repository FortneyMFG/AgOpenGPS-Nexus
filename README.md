# Nexus (AgOpenGPS Next Generation)

Nexus is the from-scratch successor to AgOpenGPS. This repository collects the plans,
requirements, and source roots for building a modular, cross-platform guidance and
application platform that cleanly separates the core orchestrator, hardware I/O host,
plugin ecosystem, and front-end experiences.

## Mission Snapshot

- **Modular architecture:** Core, AGiO backends, plugins, and UI live in dedicated
  projects that communicate over gRPC contracts generated from `/Nexus Source Code/proto`.
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
