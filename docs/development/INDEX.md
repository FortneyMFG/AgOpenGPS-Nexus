---
owner: developer-enablement
status: active
last_reviewed: 2024-05-10
related_tickets: []
---

# Developer Guide

This guide summarizes local environment setup, repeatable build commands, and key
references for Nexus contributors. For architecture or product scope, start with the
[SRS overview](SRS/00_ReadMe.md) and the [documentation index](../INDEX.md).

## Prerequisites

Install the following tools before cloning the repository:

- .NET 8 SDK
- Git 2.40+
- Visual Studio 2022, VS Code, or another IDE with C# support
- Docker Desktop (optional, for containerized testing and packaging)

## Initial Setup

```bash
# Clone and enter the workspace
git clone https://github.com/FortneyMFG/AgOpenGPS-Nexus.git
cd AgOpenGPS-Nexus

# Restore dependencies
dotnet restore
```

> Tip: Run `dotnet tool restore` if you add local tooling through `dotnet-tools.json`.

## Build & Test Commands

```bash
# Full solution build
dotnet build

# Build a specific project (example: core services)
dotnet build "Nexus SourceCode/src/Aog.Core/Aog.Core.csproj"

# Run all tests
dotnet test

# Filter tests by category
dotnet test --filter "Category=Integration"

# Simulation smoke tests (requires nexus CLI tooling)
nexus sim smoke
```

Record the commands you execute in your PR summary and keep `tasks.md` in sync with
status updates.

## Workflow Expectations

1. Select a ready ticket from `tasks.md` and branch from `main` (`feat/NX-###-slug`).
2. Design before coding—outline requirements or ADR updates as needed.
3. Keep changes scoped; update documentation and validation artifacts alongside code.
4. Run required checks (build, tests, smoke) and capture logs for reviewers.
5. Reference the ticket ID in commit messages and pull requests.

## Key References

- [Runtime baseline enforcement](../Core/support/dotnet-runtime-baseline.md)
- [Avalonia run modes](../UI/avalonia-run-modes.md)
- [Plugin lease & manifest governance](../Plugins/plugin-lease-manifest-governance.md)
- [Guidance lane publishing contracts](howto/guidance-lane-contracts.md)
- [Plugin QA handshake checklist](qa/plugin-qa-handshake.md)
- [Linux core operations playbook](../Core/linux-core-operations-playbook.md)

These references evolve with the platform—check the linked documents for the latest
procedures and cross-link updates from your PRs.
