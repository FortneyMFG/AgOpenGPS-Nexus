# ADR-001: Adopt .NET 8 C# stack for Nexus runtime

> **In plain terms:** Every Nexus app, service, and plugin runs on the same .NET 8
> toolkit so we stop juggling separate runtimes and can ship Windows and Linux
> builds on the same day.

## Status
Accepted

**Relevant Plugin(s):** Full Stack


## Context
Nexus needs a unified language and runtime that spans the Core guidance engine, AgIO backends, plugins, simulation, and the desktop UI. The System Slices outline pushes for dual-first Windows and Linux support with minimal divergence, while the stack option under review emphasises keeping hardware-specific code isolated behind AgIO backends.【F:docs/SRS/sections/1X_Platform_Foundations/11_OS_Support.md†L4-L55】【F:docs/SRS/sections/1X_Platform_Foundations/11-O1_Unified_DotNet8_Avalonia.md†L1-L47】 Community sentiment already leans toward sharing contracts and tooling across Windows x64 and Linux ARM64 without abandoning existing operators.【F:docs/SRS/sections/1X_Platform_Foundations/11_OS_Support.md†L57-L63】

## Decision
Standardise the Nexus codebase on C# targeting .NET 8 for every first-party component (Core, AgIO, plugins, simulation, and UI). Platform-specific hardware integrations remain encapsulated inside AgIO backends that surface shared abstractions through the `Aog.Abstractions` contracts package. All contributors develop, test, and ship against this single runtime.

**Executive recap:** One runtime, one toolchain, so releases land together and new contributors install a single SDK.

## Consequences
- Positive impacts
  - Common tooling, language expertise, and dependency management across all projects.
  - Simplifies cross-platform CI by focusing on .NET 8 builds for Windows x64 and Linux (x64/ARM64).
  - Enables shared simulation and plugin infrastructure without bridging multiple runtimes.
- Negative/mitigated impacts
  - Requires coordinated upgrades when the .NET LTS cycle advances; mitigated via ADR reviews.
  - Demands discipline when wrapping native SDKs; addressed by confining them to AgIO backends.
- Plain-language impact: Teams can keep helping operators even if a runtime upgrade misbehaves because the rollback plan keeps the previous .NET bundle ready.
- Follow-up actions
  - Scaffold the `Aog.Abstractions` package with generated protobuf contracts (NX-003).
  - Define CI lanes that validate Windows and Linux builds for .NET 8 targets (NX-006).

### Operator-facing pros and cons

| What improves | What stays hard |
|---------------|-----------------|
| Single installer flow for Windows and Linux releases. | Native driver shims still require platform-specific testing. |
| Plugin authors compile once and run anywhere we support. | Contributors must coordinate when Microsoft ships new LTS releases. |
| Simulation tooling and diagnostics reuse the same runtime. | Legacy-only hardware may need extra wrappers before it works cross-platform. |

### What could go wrong—and how we explain it

- **Linux GPU stack regresses:** Operators might notice sluggish UI updates on Pi/CM5. Mitigation: keep Avalonia fallbacks ready and monitor benchmark dashboards weekly.
- **Vendor SDKs lag behind:** A CAN adapter without Linux drivers means Linux rigs lose functionality. Mitigation: run those adapters through AgIO shims so Windows keeps working while we lobby vendors or ship bridge services.
- **Runtime upgrade breaks plugins:** If .NET updates introduce incompatible JIT behavior, we freeze upgrades, repost the prior runtime bundle, and publish guidance in release notes so operators know which download to use.

## Legacy Implementation Notes
### AgOpenGPS v6
- Windows remains the only supported runtime, with WinForms and WPF projects compiled as Windows desktop executables, so the stack is tied to the .NET Framework toolchain and lacks cross-platform parity today.【F:docs/SRS/sections/1X_Platform_Foundations/11_OS_Support.md†L6-L13】【F:docs/SRS/sections/1X_Platform_Foundations/13_UI_Framework_UX.md†L6-L17】

### Legacy Dev Branch
- The community dev branch follows the same Windows-only WinForms/WPF approach, reflecting the status-quo option of incremental modernization without a shared cross-platform runtime or packaging story.【F:docs/SRS/sections/1X_Platform_Foundations/11_OS_Support.md†L6-L13】【F:docs/SRS/sections/1X_Platform_Foundations/13_UI_Framework_UX.md†L16-L29】

## Governance Updates
- **Supported runtime roster.** Nexus ships on .NET 8 through November 2026 with quarterly compatibility snapshots. Engineering pre-bakes manifests for the next LTS (currently .NET 10 preview) and publishes a readiness scorecard each June covering Core, AgIO, plugins, and tooling. A go/no-go decision is recorded 90 days before Microsoft GA so dependent teams can stage migrations without fire drills.
- **Compatibility smoke regimen.** QA owns an annual matrix that runs desktop, NativeAOT, and containerised builds across Windows x64, Linux x64/ARM64, and the supported SBC images. The February and August cycles gate feature freeze, while monthly spot checks ensure NuGet dependency drifts stay inside the signed roster.
- **Back-out playbook.** If a runtime uplift regresses hardware integrations, AgIO locks roll-forward, re-issues the prior runtime bundle, and coordinates with Plugin and Firmware owners to certify patched drivers within two weeks. Production rollbacks require communicating operator impact and re-running compatibility smokes before reopening the upgrade window.

> **Reader tip:** If you are new, install the .NET 8 SDK listed here, run the smoke commands, and you are on the supported path—no parallel toolchains required.

## References
- [Section 11 — OS Support](../SRS/sections/1X_Platform_Foundations/11_OS_Support.md)
- [Option 11-O1 — Unified .NET 8 + Avalonia stack](../SRS/sections/1X_Platform_Foundations/11-O1_Unified_DotNet8_Avalonia.md)
