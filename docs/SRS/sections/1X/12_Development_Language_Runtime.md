# 12 — Development Language & Runtime (Status: collecting proposals)

## Problem statement
Identify the supported implementation languages, managed runtimes, and dependency policies so every module—Core, UI, plugins, and firmware tooling—ships from a coherent stack with predictable servicing rules.【F:docs/SRS/sections/1X/11_OS_Support.md†L14-L45】【F:docs/SRS/options/1X/O-STACK-1_DotNet8Avalonia.md†L1-L79】

## Requirements (from contributors)
- R-STACK-000 (MUST, managed runtime): Standardize on .NET 8 as the baseline runtime for Core services, UI shells, AgIO, and command-line tooling so binaries run consistently across Windows x64 and Linux ARM64 targets.【F:docs/SRS/options/1X/O-STACK-1_DotNet8Avalonia.md†L9-L79】
- R-STACK-001 (MUST, language surface): Use C# as the primary implementation language; new components must justify deviations and expose language-agnostic contracts (gRPC, JSON schema) for plugin authors.【F:docs/SRS/sections/2X/21_System_Decomposition_Boundaries.md†L1-L45】【F:docs/SRS/options/2X/O-BACKEND-6_LinuxCoreService.md†L1-L44】
- R-STACK-002 (SHOULD, dependency policy): Maintain a curated dependency allowlist per layer (Core, UI, AgIO, plugins). NuGet additions require compatibility testing on Windows and Linux build lanes before adoption.【F:docs/SRS/sections/9X/96_Quality_Engineering_Release.md†L19-L74】
- R-STACK-003 (MUST, deterministic builds): Lock toolchain versions via `global.json`, ensure repeatable package restore, and sign first-party assemblies to preserve supply-chain integrity.【F:docs/SRS/sections/9X/96_Quality_Engineering_Release.md†L76-L147】
- R-STACK-004 (SHOULD, ABI governance): Version shared contracts (`Aog.Abstractions`, gRPC proto, plugin SDK) alongside runtime updates so plugin binaries remain compatible across servicing updates.【F:docs/SRS/sections/9X/94_Extensibility_Packaging_Updates.md†L17-L120】【F:docs/SRS/options/6X/O-API-5_VersionedLayerSchemas.md†L32-L49】
- R-STACK-005 (MUST, hardware bindings): Abstract OS-specific device bindings (serial, SocketCAN, HID) behind dependency-injected interfaces so runtime updates do not fork the codebase per OS.【F:docs/SRS/sections/5X/51_Sensor_Actuator_Abstractions.md†L6-L82】

## Options
- O-STACK-0: Status quo mix of .NET Framework, .NET 6, and native helper utilities.
- O-STACK-1: Unified .NET 8 stack with Avalonia UI hosts and shared gRPC contracts.【F:docs/SRS/options/1X/O-STACK-1_DotNet8Avalonia.md†L1-L79】
- O-STACK-2: Hybrid stack (.NET 8 Core + Qt/C++ device hosts) for hardware-heavy deployments.

## Comparison (quick matrix)
| Option | Pros | Cons | Risks | Borrow |
|---|---|---|---|---|
| O-STACK-0 | Low migration effort | Fragmented tooling, limited Linux story | Security servicing churn | Existing WinForms/WPF projects |
| O-STACK-1 | One toolchain, cross-platform UI, shared plugin contracts | Requires Avalonia expertise + dual-OS CI | Runtime regressions impact every surface | .NET 8 pilots + Avalonia UX spikes |
| O-STACK-2 | Keep proven native integrations | Two language stacks to maintain | Divergent APIs, harder community onboarding | Legacy AgOpenGPS native helpers |

## Current sentiment
The community is converging on O-STACK-1 because it unlocks Linux and container deployments, keeps C# productivity, and aligns with planned plugin governance while allowing native shims where device SDKs demand them.【F:docs/SRS/sections/1X/11_OS_Support.md†L30-L45】【F:docs/SRS/options/1X/O-STACK-1_DotNet8Avalonia.md†L9-L79】
