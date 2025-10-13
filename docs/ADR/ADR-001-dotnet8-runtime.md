# ADR-001: Adopt .NET 8 C# stack for Nexus runtime

## Status
Accepted

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

## References
- [Section 01 — OS Support](../SRS/sections/01_OS_Support.md)
- [Option O-STACK-1 — .NET 8 + Avalonia stack](../SRS/options/O-STACK-1_DotNet8Avalonia.md)
