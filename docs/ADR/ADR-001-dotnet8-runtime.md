# ADR-001: Adopt .NET 8 C# stack for Nexus runtime

## Status
Accepted

**Relevant Plugin(s):** Full Stack


## Context
Nexus needs a unified language and runtime that spans the Core guidance engine, AgIO backends, plugins, simulation, and the desktop UI. The System Slices outline pushes for dual-first Windows and Linux support with minimal divergence, while the stack option under review emphasises keeping hardware-specific code isolated behind AgIO backends.【F:docs/SRS/sections/01_OS_Support.md†L4-L55】【F:docs/SRS/options/O-STACK-1_DotNet8Avalonia.md†L1-L47】 Community sentiment already leans toward sharing contracts and tooling across Windows x64 and Linux ARM64 without abandoning existing operators.【F:docs/SRS/sections/01_OS_Support.md†L57-L63】

## Decision
Standardise the Nexus codebase on C# targeting .NET 8 for every first-party component (Core, AgIO, plugins, simulation, and UI). Platform-specific hardware integrations remain encapsulated inside AgIO backends that surface shared abstractions through the `Aog.Abstractions` contracts package. All contributors develop, test, and ship against this single runtime.

## Consequences
- Positive impacts
  - Common tooling, language expertise, and dependency management across all projects.
  - Simplifies cross-platform CI by focusing on .NET 8 builds for Windows x64 and Linux (x64/ARM64).
  - Enables shared simulation and plugin infrastructure without bridging multiple runtimes.
- Negative/mitigated impacts
  - Requires coordinated upgrades when the .NET LTS cycle advances; mitigated via ADR reviews.
  - Demands discipline when wrapping native SDKs; addressed by confining them to AgIO backends.
- Follow-up actions
  - Scaffold the `Aog.Abstractions` package with generated protobuf contracts (NX-003).
  - Define CI lanes that validate Windows and Linux builds for .NET 8 targets (NX-006).

## Legacy Implementation Notes
### AgOpenGPS v6
- Windows remains the only supported runtime, with WinForms and WPF projects compiled as Windows desktop executables, so the stack is tied to the .NET Framework toolchain and lacks cross-platform parity today.【F:docs/SRS/sections/01_OS_Support.md†L6-L13】【F:docs/SRS/sections/02_Framework_UI.md†L6-L17】

### Legacy Dev Branch
- The community dev branch follows the same Windows-only WinForms/WPF approach, reflecting the status-quo option of incremental modernization without a shared cross-platform runtime or packaging story.【F:docs/SRS/sections/01_OS_Support.md†L6-L13】【F:docs/SRS/sections/02_Framework_UI.md†L16-L29】

## Governance Updates
- **Supported runtime roster.** Nexus ships on .NET 8 through November 2026 with quarterly compatibility snapshots. Engineering pre-bakes manifests for the next LTS (currently .NET 10 preview) and publishes a readiness scorecard each June covering Core, AgIO, plugins, and tooling. A go/no-go decision is recorded 90 days before Microsoft GA so dependent teams can stage migrations without fire drills.
- **Compatibility smoke regimen.** QA owns an annual matrix that runs desktop, NativeAOT, and containerised builds across Windows x64, Linux x64/ARM64, and the supported SBC images. The February and August cycles gate feature freeze, while monthly spot checks ensure NuGet dependency drifts stay inside the signed roster.
- **Back-out playbook.** If a runtime uplift regresses hardware integrations, AgIO locks roll-forward, re-issues the prior runtime bundle, and coordinates with Plugin and Firmware owners to certify patched drivers within two weeks. Production rollbacks require communicating operator impact and re-running compatibility smokes before reopening the upgrade window.

## References
- [Section 01 — OS Support](../SRS/sections/01_OS_Support.md)
- [Option O-STACK-1 — .NET 8 + Avalonia stack](../SRS/options/O-STACK-1_DotNet8Avalonia.md)
