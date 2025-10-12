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

## Options
- O-FE-0: Status quo — Windows desktop suite (AgOpenGPS + AgIO + utilities).
- O-FE-1: Shared cross-platform UI deployed on Windows/Linux tablets.
- O-FE-2: Native mobile companion (Android/iOS) for monitoring only.
- O-FE-3: Browser-based dashboard fed by WebSockets.
- O-FE-4: Remote desktop appliance dedicated to cab displays.
- O-FE-5: [Metadata-driven dashboards and visualization](../options/O-UI-5_MetadataDrivenDashboards.md) — Layer-aware overlays, inspectors, and presets.
- O-FE-6: [Remote gRPC/WebSocket clients backed by the Linux Core](../options/O-FRONT-6_RemoteClients.md) — Native + browser displays.

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

## Evaluation criteria
Operator familiarity, deployment friction, offline resilience, latency, maintainability.

## Current sentiment
- Keep the Windows suite in place while testing what “remote display” actually needs (mirror vs. control).
- Operators welcome metadata-driven dashboards if they ship with presets and inspector upgrades rather than requiring manual wiring per layer.【F:docs/SRS/options/O-UI-5_MetadataDrivenDashboards.md†L52-L64】
- Remote-first clients are attractive if they piggyback on the Core without forcing Windows operators to learn a new UI overnight.【F:docs/SRS/options/O-FRONT-6_RemoteClients.md†L21-L34】

## Open questions
- Which screens must be mirrored vs. reimagined for mobile?
- How do we share PGN data with remote clients without reimplementing all of AgIO?
