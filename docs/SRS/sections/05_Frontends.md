# Frontends (Status: collecting proposals)

## Problem statement
Describe the operator-facing applications (desktop, mobile, remote) and how they coordinate guidance, mapping, and diagnostics experiences.

## Requirements (from contributors)
- R-FE-000 (MUST, current-AgOpenGPS): Keep the WinForms-based AgOpenGPS desktop front end available as the primary operator UI.【F:SourceCode/GPS/AgOpenGPS.csproj†L1-L102】
- R-FE-001 (MUST, current-AgIO): Maintain the AgIO companion app used to configure transports and hardware modules.【F:SourceCode/AgIO/Source/AgIO.csproj†L1-L60】
- R-FE-002 (SHOULD, current-AgOpenGPS): Continue providing auxiliary tools (AgDiag, ModSim, GPS_Out, Keypad) within the solution for troubleshooting and simulation.【F:SourceCode/AgOpenGPS.sln†L6-L35】
- R-FE-003 (SHOULD): Offer a consistent story for remote display/control without regressing current Windows workflows.
- R-FE-010 (MUST, proposed-variable-layer): Make dashboards, overlays, and inspectors metadata-driven so new layer types light up without code changes.【F:docs/SRS/options/O-UI-5_MetadataDrivenDashboards.md†L1-L29】
- R-FE-011 (SHOULD, proposed-variable-layer): Provide configuration tooling (Layer Definition Manager, section presets) and drill-down dashboards that reuse aggregation metadata while respecting multi-monitor layouts.【F:docs/SRS/options/O-UI-5_MetadataDrivenDashboards.md†L30-L51】
- R-FE-004 (COULD): Introduce thin clients (web/tablet) that mirror dashboards when networked.
- R-FE-012 (SHOULD, proposed-LinuxCore): Ensure at least one frontend can operate purely as a remote client over the Core APIs (gRPC/WebSocket) while maintaining offline workflows for Windows rigs.【F:docs/SRS/options/O-FRONT-6_RemoteClients.md†L1-L34】
- R-FE-013 (SHOULD, safety posture): Distinguish monitor-only remote clients from control-capable clients with explicit capability flags so safety-critical actions are disabled on unreliable links by default.
- R-FE-014 (SHOULD, operator readiness): Capture training, preset migration, and configuration handoff requirements when metadata-heavy dashboards roll out so operators can transition without losing saved layouts.
- R-FE-020 (SHOULD, proposed-composite-sim): Surface a unified simulation bar that drives play/pause/seek/speed for the authoritative SimClock so operators, replay, and plugin simulators stay synchronized.

## Options
- O-FE-0: Status quo — Windows desktop suite (AgOpenGPS + AgIO + utilities).
- O-FE-1: Shared cross-platform UI deployed on Windows/Linux tablets.
- O-FE-2: Native mobile companion (Android/iOS) for monitoring only.
- O-FE-3: Browser-based dashboard fed by WebSockets.
- O-FE-4: Remote desktop appliance dedicated to cab displays.
- O-FE-5: [Metadata-driven dashboards and visualization](../options/O-UI-5_MetadataDrivenDashboards.md) — Layer-aware overlays, inspectors, and presets.
- O-FE-6: [Remote gRPC/WebSocket clients backed by the Linux Core](../options/O-FRONT-6_RemoteClients.md) — Native + browser displays.
- O-FE-7: Avalonia desktop frontend consuming shared gRPC contracts with optional Windows-native shell hosting.【F:docs/SRS/options/O-STACK-1_DotNet8Avalonia.md†L1-L47】

## Comparison (quick matrix)
| Option | Pros | Cons | Risks | Borrow from existing |
|---|---|---|---|---|
| O-FE-0 | Operators know it; no new hardware | Windows dependency | Hard to reach tablets | Current WinForms/WPF apps |
| O-FE-1 | Single codebase | Migration workload | Requires retraining | WPF experiments |
| O-FE-2 | Lightweight access | Limited control surface | Safety-critical actions from phone | UDP telemetry |
| O-FE-3 | Platform-neutral | Needs auth + hosting | Browser perf in cab | AgIO WebSocket ideas |
| O-FE-4 | Simple to deploy | Extra device to manage | Network latency | Existing remote desktop setups |
| O-FE-5 | Declarative visuals, richer diagnostics | Large UI refactor and performance tuning | Risk of overwhelming operators with options | Current WPF/OpenGL renderer + metadata plan |
| O-FE-6 | Enables headless rigs, remote tablets, and browser displays | Requires Core APIs + session/state sync | Network failures impact UX | Remote clients plan |
| O-FE-7 | Shared codebase, C# productivity, Pi/CM5-ready | Requires Avalonia investment + GPU validation | Need fallbacks for Windows-native integrations | .NET 8 + Avalonia stack |

## Evaluation criteria
Operator familiarity, deployment friction, offline resilience, latency, maintainability.

## Current sentiment
- Keep the Windows suite in place while testing what “remote display” actually needs (mirror vs. control).
- Operators welcome metadata-driven dashboards if they ship with presets and inspector upgrades rather than requiring manual wiring per layer.【F:docs/SRS/options/O-UI-5_MetadataDrivenDashboards.md†L52-L64】
- The Avalonia desktop frontend is now viewed as the preferred successor because it keeps one C# codebase and can slide into the Windows quick-start flow before expanding to Pi/CM5 deployments.【F:docs/SRS/options/O-STACK-1_DotNet8Avalonia.md†L1-L79】
- Remote-first clients are attractive if they piggyback on the Core without forcing Windows operators to learn a new UI overnight.【F:docs/SRS/options/O-FRONT-6_RemoteClients.md†L21-L34】
- Simulation tooling should reuse the same controls regardless of data source so operators can blend hardware inputs with plugin-provided scenarios without context switching.

## Open questions
- Which screens must be mirrored vs. reimagined for mobile?
- How do we share PGN data with remote clients without reimplementing all of AgIO?
