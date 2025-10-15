# ADR-003: Use Avalonia for the cross-platform Nexus UI shell

## Status
Accepted

## Context
Nexus must deliver a desktop experience that runs identically on Windows and Linux hosts while remaining touch-friendly and metadata-driven. The UI framework section documents the need for high-DPI scaling, multi-monitor layouts, and remote clients without abandoning existing operators.【F:docs/SRS/sections/02_Framework_UI.md†L1-L70】 Option O-STACK-1 describes Avalonia as the shared Windows/Linux UI toolkit aligned with the .NET 8 stack, and contributors favour it for reuse of C# expertise and deployability on Raspberry Pi-class hardware.【F:docs/SRS/options/O-STACK-1_DotNet8Avalonia.md†L1-L47】【F:docs/SRS/sections/02_Framework_UI.md†L72-L83】

## Decision
Adopt Avalonia as the primary UI framework for the Nexus desktop shell. The Avalonia client consumes the shared gRPC contracts, supports Windows x64 and Linux (x64/ARM64), and becomes the foundation for metadata-driven dashboards, simulation controls, and remote-client wrappers. Optional host shells (WinUI/WPF) can embed the Avalonia client when Windows-native polish is required, but the Avalonia implementation remains the authoritative cross-platform UI.

## Mobile and companion roadmap
The Avalonia footprint also unlocks native Android and iOS builds so the same codebase can ship as a remote companion now and later host Core + AgIO locally. We will structure the client around three dependency-injected run modes that swap the `ICoreTransport` implementation without rewriting views or view models:

1. **CompanionRemote:** Ship the UI as-is on mobile and connect to Core/AgIO running on a Windows/Linux host over gRPC (Android) or gRPC-Web (iOS or restricted networks). A connection center handles discovery (mDNS/manual), reconnect, health, and authentication workflows so tablets and phones mirror desktop capabilities safely.【F:docs/SRS/sections/05_Frontends.md†L26-L29】【F:docs/SRS/sections/05_Frontends.md†L68-L69】
2. **LocalInProc:** Package the Core runtime as a library and host it inside the Avalonia process. The UI swaps the transport to an in-process adapter, reuses the same view models, and exposes feature toggles so operators can run “lite” workflows on mobile hardware before adding hardware I/O.【F:docs/SRS/sections/05_Frontends.md†L28-L71】
3. **LocalOutOfProc:** Bundle Core as a platform-specific binary and start it locally (e.g., Android foreground service) while the UI speaks loopback gRPC. This keeps crash isolation and matches how desktop shells talk to Core today, making it easier to reuse diagnostics, logging, and permission flows.【F:docs/SRS/sections/05_Frontends.md†L28-L72】

Connection policy, offline caches, and feature gating flow from shared configuration so the same Avalonia client can pivot between remote monitoring and fully embedded rigs without branching the UI stack. Platform hosts contribute only the glue for permissions (USB/BLE/notifications) and storage policies, keeping the app surface identical across Windows, Linux, Android, and iOS.【F:docs/SRS/sections/01_OS_Support.md†L14-L44】【F:docs/SRS/sections/02_Framework_UI.md†L15-L47】

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

## Governance Updates
- **Performance acceptance matrix.** Device bands (desktop, rugged tablet, mobile) must sustain ≥45 FPS, ≤80 ms input latency, and ≤1.2× baseline memory at steady state. Benchmark tables accompany every quarterly release candidate along with GPU trace captures for regression analysis.
- **Quarterly UX smoke.** Scheduled runs in March, June, September, and December replay scripted tours across Windows, Linux, Android, and iOS. Regressions raise Sev2 defects that block release freeze until resolved or signed off by UX leadership.
- **Golden screenshot packs.** Automated baseline renders detect deltas >1.5% pixel change. Approved updates require paired accessibility checks (contrast, keyboard focus order) and updated documentation for plugin authors who embed shared widgets.

## Legacy Implementation Notes
### AgOpenGPS v6
- Operators rely on the WinForms UI with selective WPF panels, so the legacy stack is confined to Windows desktops and lacks a cross-platform shell today.【F:docs/SRS/sections/02_Framework_UI.md†L6-L10】【F:docs/SRS/sections/05_Frontends.md†L7-L12】

### Legacy Dev Branch
- The dev branch follows the same pattern—WinForms remains primary with incremental WPF modernization—leaving remote or Linux clients unserved without remote desktop workarounds.【F:docs/SRS/sections/02_Framework_UI.md†L16-L29】【F:docs/SRS/sections/05_Frontends.md†L7-L17】

## References
- [Section 02 — UI Framework](../SRS/sections/02_Framework_UI.md)
- [Option O-STACK-1 — .NET 8 + Avalonia stack](../SRS/options/O-STACK-1_DotNet8Avalonia.md)
