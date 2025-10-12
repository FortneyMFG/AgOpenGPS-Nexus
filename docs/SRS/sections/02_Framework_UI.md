# UI Framework (Status: collecting proposals)

## Problem statement
Determine the presentation technologies and layout systems that power both the legacy WinForms front end and the newer WPF work, keeping touch parity and multi-monitor layouts in mind.

## Requirements (from contributors)
- R-UI-000 (MUST, current-AgOpenGPS): Keep the primary desktop UI available as a Windows Forms WinExe with mapping, PGN tools, and OpenGL panels.【F:SourceCode/GPS/AgOpenGPS.csproj†L1-L102】
- R-UI-001 (MUST, current-AgOpenGPS): Continue development of the WPF shell that targets Windows desktop via `UseWPF` for modernized panels.【F:SourceCode/AgOpenGPS.WpfApp/AgOpenGPS.WpfApp.csproj†L1-L15】【F:SourceCode/AgOpenGPS.WpfApp/App.xaml†L1-L10】
- R-UI-002 (SHOULD, current-AgIO): Maintain AgIO’s Windows Forms dialogs that configure UDP, serial, and NTRIP connectivity.【F:SourceCode/AgIO/Source/AgIO.csproj†L1-L54】【F:SourceCode/AgIO/Source/Forms/FormUDP.cs†L1-L160】
- R-UI-003 (SHOULD, current-AgOpenGPS): Preserve multi-monitor window placement helpers already used by the WinForms UI.【F:SourceCode/GPS/Helpers/ScreenHelper.cs†L1-L30】
- R-UI-004 (SHOULD, proposed-Metadata): Deliver metadata-driven widgets so new layers surface in UI panels without code rewrites.【F:docs/SRS/options/O-UI-5_MetadataDrivenDashboards.md†L1-L51】
- R-UI-005 (SHOULD, proposed-LinuxCore): Provide frontends that can attach to a headless Core over gRPC/WebSocket while keeping Windows UX intact for local rigs.【F:docs/SRS/options/O-FRONT-6_RemoteClients.md†L1-L34】
- R-UI-006 (COULD, proposed-LinuxCore): Evaluate kiosk-friendly cross-platform stacks (Qt, Avalonia, Web) with touch parity and offline theming presets.【F:docs/SRS/options/O-BACKEND-6_LinuxCoreService.md†L1-L44】【F:docs/SRS/options/O-FRONT-6_RemoteClients.md†L1-L34】

## Options
- O-UI-0: Status quo — WinForms primary UI with incremental WPF modernization.
- O-UI-1: Qt/QML.
- O-UI-2: Avalonia (.NET).
- O-UI-3: Flutter.
- O-UI-4: Electron + WebGL (Three.js/MapLibre).
- O-UI-5: [Metadata-driven dashboards layered on existing renderers](../options/O-UI-5_MetadataDrivenDashboards.md) — Declarative overlays and inspectors.
- O-UI-6: Remote client UI connecting to Linux Core via APIs.【F:docs/SRS/options/O-FRONT-6_RemoteClients.md†L1-L34】

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

## Evaluation criteria
Touch ergonomics, latency, GPU access, designer productivity, theming support, ability to span monitors.

## Current sentiment
- Keep WinForms operational while defining what the WPF shell must ship before asking operators to migrate.
- Contributors want proof that metadata-driven layouts and remote clients can coexist without fragmenting operator workflows before endorsing a wholesale toolkit switch.【F:docs/SRS/options/O-UI-5_MetadataDrivenDashboards.md†L52-L64】【F:docs/SRS/options/O-FRONT-6_RemoteClients.md†L21-L34】

## Open questions
- Can we abstract OpenGL usage so both WinForms and WPF (or future UI) reuse the renderer?
- What gaps keep us from shipping the WPF front end as a supported UI?
