# 13 — UI Framework & UX Language (Status: collecting proposals)

## Problem statement
Determine the presentation technologies and layout systems that power both the legacy WinForms front end and the newer WPF work, keeping touch parity and multi-monitor layouts in mind.

## Requirements (from contributors)
- R-UI-000 (MUST, current-AgOpenGPS): Keep the primary desktop UI available as a Windows Forms WinExe with mapping, PGN tools, and OpenGL panels.【F:SourceCode/GPS/AgOpenGPS.csproj†L1-L102】
- R-UI-001 (MUST, current-AgOpenGPS): Continue development of the WPF shell that targets Windows desktop via `UseWPF` for modernized panels.【F:SourceCode/AgOpenGPS.WpfApp/AgOpenGPS.WpfApp.csproj†L1-L15】【F:SourceCode/AgOpenGPS.WpfApp/App.xaml†L1-L10】
- R-UI-002 (SHOULD, current-AgIO): Maintain AgIO’s Windows Forms dialogs that configure UDP, serial, and NTRIP connectivity.【F:SourceCode/AgIO/Source/AgIO.csproj†L1-L54】【F:SourceCode/AgIO/Source/Forms/FormUDP.cs†L1-L160】
- R-UI-003 (SHOULD, current-AgOpenGPS): Preserve multi-monitor window placement helpers already used by the WinForms UI.【F:SourceCode/GPS/Helpers/ScreenHelper.cs†L1-L30】
- R-UI-004 (SHOULD, proposed-Metadata): Deliver metadata-driven widgets so new layers surface in UI panels without code rewrites.【F:docs/SRS/options/9X/O-UI-5_MetadataDrivenDashboards.md†L1-L51】
- R-UI-005 (SHOULD, proposed-LinuxCore): Provide frontends that can attach to a headless Core over gRPC/WebSocket while keeping Windows UX intact for local rigs.【F:docs/SRS/options/9X/O-FRONT-6_RemoteClients.md†L1-L34】
- R-UI-006 (COULD, proposed-LinuxCore): Evaluate kiosk-friendly cross-platform stacks (Qt, Avalonia, Web) with touch parity and offline theming presets.【F:docs/SRS/options/2X/O-BACKEND-6_LinuxCoreService.md†L1-L44】【F:docs/SRS/options/9X/O-FRONT-6_RemoteClients.md†L1-L34】
- R-UI-007 (SHOULD, accessibility): Ensure any future UI stack supports high-DPI scaling, configurable color-contrast presets, and localization hooks (fonts, RTL layouts) so metadata-driven dashboards remain operable for diverse operators across cab lighting conditions.
- R-UI-008 (MUST, shared mobile shell): Keep the Avalonia project free of platform-specific UI forks by driving mobile builds (Android/iOS) through dependency-injected services for transports, storage, and permissions while reusing the same view models and theming.【F:docs/ADR/ADR-003-avalonia-ui.md†L24-L44】
- R-UI-009 (SHOULD, run-mode toggles): Provide a configuration surface (appsettings/UI) that flips between CompanionRemote, LocalInProc, and LocalOutOfProc so QA can validate all modes without rebuilding.【F:docs/ADR/ADR-003-avalonia-ui.md†L24-L42】

## Options
- O-UI-0: Status quo — WinForms primary UI with incremental WPF modernization.
- O-UI-1: Qt/QML.
- O-UI-2: Avalonia (.NET).
- O-UI-3: Flutter.
- O-UI-4: Electron + WebGL (Three.js/MapLibre).
- O-UI-5: [Metadata-driven dashboards layered on existing renderers](../options/9X/O-UI-5_MetadataDrivenDashboards.md) — Declarative overlays and inspectors.
- O-UI-6: Remote client UI connecting to Linux Core via APIs.【F:docs/SRS/options/9X/O-FRONT-6_RemoteClients.md†L1-L34】
- O-UI-7: Avalonia desktop client sharing gRPC contracts across Windows and Linux with optional WinUI/WPF host shells.【F:docs/SRS/options/1X/O-STACK-1_DotNet8Avalonia.md†L1-L47】

## Comparison (quick matrix)
| Option | Pros | Cons | Risks | Borrow |
|---|---|---|---|---|
| O-UI-0 | Retains proven WinForms workflows while prototyping WPF | Windows-specific rendering stack | Limits UX evolution pace | Existing GL control + WPF shell |
| O-UI-1 | Mature, C++ close to current | Licensing/skillset | QML perf pitfalls | Existing GL code |
| O-UI-2 | C# productivity | Native interop | New stack for many | None |
| O-UI-3 | Great touch UI | Desktop maturity | Plugin gaps | None |
| O-UI-4 | Web talent pool | Heavier | Latency; GPU tuning | Reuse AgIO WS |
| O-UI-5 | UI auto-discovers new layers | Requires metadata completeness + perf work | Overwhelming operators with options | Metadata-driven dashboards |
| O-UI-6 | Enables web/remote clients + headless rigs | Depends on Core APIs + network reliability | API drift strands clients | Remote clients plan |
| O-UI-7 | Single codebase, native performance, reuse existing C# skills | Requires Avalonia expertise + theming work | Need to ensure GPU performance on Pi/CM5 | .NET 8 + Avalonia stack |

## Evaluation criteria
Touch ergonomics, latency, GPU access, designer productivity, theming support, ability to span monitors.

## Current sentiment
- Keep WinForms operational while defining what the WPF shell must ship before asking operators to migrate.
- The Avalonia + gRPC client is now the leading candidate for a shared Windows/Linux UI because it keeps the C# skillset while unlocking Pi/CM5 deployments and optional Windows-native shells.【F:docs/SRS/options/1X/O-STACK-1_DotNet8Avalonia.md†L1-L79】
- Contributors want proof that metadata-driven layouts and remote clients can coexist without fragmenting operator workflows before endorsing a wholesale toolkit switch.【F:docs/SRS/options/9X/O-UI-5_MetadataDrivenDashboards.md†L52-L64】【F:docs/SRS/options/9X/O-FRONT-6_RemoteClients.md†L21-L34】
- Mobile pilots should reuse the same Avalonia shell so Android/iOS companions launch quickly and later embed Core with minimal UI churn, demonstrating the value of a single .NET 8 stack.【F:docs/ADR/ADR-003-avalonia-ui.md†L24-L44】【F:docs/SRS/sections/9X/91_UI_Shell_Layout.md†L26-L72】

## Open questions
- Can we abstract OpenGL usage so both WinForms and WPF (or future UI) reuse the renderer?
- What gaps keep us from shipping the WPF front end as a supported UI?
