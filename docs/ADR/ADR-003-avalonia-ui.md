# ADR-003: Use Avalonia for the cross-platform Nexus UI shell

## Status
Accepted

## Context
Nexus must deliver a desktop experience that runs identically on Windows and Linux hosts while remaining touch-friendly and metadata-driven. The UI framework section documents the need for high-DPI scaling, multi-monitor layouts, and remote clients without abandoning existing operators.【F:docs/SRS/sections/02_Framework_UI.md†L1-L70】 Option O-STACK-1 describes Avalonia as the shared Windows/Linux UI toolkit aligned with the .NET 8 stack, and contributors favour it for reuse of C# expertise and deployability on Raspberry Pi-class hardware.【F:docs/SRS/options/O-STACK-1_DotNet8Avalonia.md†L1-L47】【F:docs/SRS/sections/02_Framework_UI.md†L72-L83】

## Decision
Adopt Avalonia as the primary UI framework for the Nexus desktop shell. The Avalonia client consumes the shared gRPC contracts, supports Windows x64 and Linux (x64/ARM64), and becomes the foundation for metadata-driven dashboards, simulation controls, and remote-client wrappers. Optional host shells (WinUI/WPF) can embed the Avalonia client when Windows-native polish is required, but the Avalonia implementation remains the authoritative cross-platform UI.

## Consequences
- Positive impacts
  - Single UI codebase that runs on Windows and Linux, simplifying feature parity and theming.
  - Aligns with the C#/.NET 8 decision, enabling developers to share components across Core, plugins, and UI.
  - Unlocks kiosk and Raspberry Pi deployments without rewriting the frontend in another toolkit.
- Negative/mitigated impacts
  - Requires onboarding contributors to Avalonia patterns; mitigated through documentation and templates.
  - GPU performance on constrained devices must be validated; addressed by targeted profiling and hardware pilots.
- Follow-up actions
  - Scaffold the Avalonia solution and shell project structure (future NX tasks).
  - Define UI theming, metadata-driven widget strategy, and remote-client story around the Avalonia host.

## Legacy Implementation Notes
### AgOpenGPS v6
- Operators rely on the WinForms UI with selective WPF panels, so the legacy stack is confined to Windows desktops and lacks a cross-platform shell today.【F:docs/SRS/sections/02_Framework_UI.md†L6-L10】【F:docs/SRS/sections/05_Frontends.md†L7-L12】

### Legacy Dev Branch
- The dev branch follows the same pattern—WinForms remains primary with incremental WPF modernization—leaving remote or Linux clients unserved without remote desktop workarounds.【F:docs/SRS/sections/02_Framework_UI.md†L16-L29】【F:docs/SRS/sections/05_Frontends.md†L7-L17】

## References
- [Section 02 — UI Framework](../SRS/sections/02_Framework_UI.md)
- [Option O-STACK-1 — .NET 8 + Avalonia stack](../SRS/options/O-STACK-1_DotNet8Avalonia.md)
