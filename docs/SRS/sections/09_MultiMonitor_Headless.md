# Multi-Monitor & Headless (Status: collecting proposals)

## Problem statement
Describe how the suite behaves across multiple displays, kiosk rigs, or headless deployments that rely on remote control.

## Requirements (from contributors)
- R-MM-000 (MUST, current-AgOpenGPS): Preserve screen placement helpers that ensure WinForms windows remain visible across multiple monitors.【F:SourceCode/GPS/Helpers/ScreenHelper.cs†L1-L30】
- R-MM-001 (SHOULD, current-AgIO): Keep UDP/serial monitors operable when the UI is minimized or moved off the primary screen for diagnostics.【F:SourceCode/AgIO/Source/Forms/FormUDPMonitor.cs†L8-L70】
- R-MM-002 (SHOULD): Provide a story for running the backend with no local UI while exposing remote displays or APIs.
- R-MM-003 (SHOULD, proposed-LinuxCore): Deliver kiosk/headless launchers for the Linux Core + remote clients (auto-login, fullscreen) so rigs boot directly into an operator-ready view.【F:docs/SRS/options/O-BACKEND-6_LinuxCoreService.md†L6-L23】【F:docs/SRS/options/O-FRONT-6_RemoteClients.md†L10-L18】
- R-MM-004 (COULD): Add locked-down layouts that survive accidental window drags or task-switching.
- R-MM-005 (SHOULD, resilience): Capture watchdog and auto-recovery expectations for kiosk/headless deployments (service restart policies, layout reset scripts, power-loss recovery steps) so rigs can return to an operator-ready state without manual intervention.
- R-CTRL-000 (MUST, section control graph): Define the authoritative on/auto/off control graph, including master switches and safety gating, so Core, firmware, and plugins arbitrate SectionState changes consistently.
- R-CTRL-001 (SHOULD, grouping semantics): Support overlapping SectionGroups with deterministic priority rules and operator overrides so complex implements remain predictable across UI, control, and simulation surfaces.
- R-CTRL-002 (SHOULD, toolbar lookahead): Allow per-toolbar lookahead/overlap tuning with documented defaults and override bounds so rate controllers and coverage renderers stay in sync.

## Options
- O-MM-0: Status quo — Desktop windows with manual layout tools and helper checks.
- O-MM-1: Layout profiles synchronized across displays and saved per rig.
- O-MM-2: Dedicated headless service with remote web UI.
- O-MM-3: Windows kiosk mode packaging for cab computers.
- O-MM-4: Remote-only thin client (tablet) controlling a headless backend.
- O-MM-5: Linux Core kiosk/headless deployment with remote frontends.【F:docs/SRS/options/O-BACKEND-6_LinuxCoreService.md†L6-L44】【F:docs/SRS/options/O-FRONT-6_RemoteClients.md†L1-L34】

## Comparison (quick matrix)
| Option | Pros | Cons | Risks | Borrow from existing |
|---|---|---|---|---|
| O-MM-0 | No new infra, proven today | Manual recovery if windows lost | Operators lose time fixing layouts | ScreenHelper utilities |
| O-MM-1 | Repeatable layouts | Requires profile sync | Profile corruption risk | Current settings model |
| O-MM-2 | Works for remote cabinets | Needs service packaging | More failure points | AgIO transports |
| O-MM-3 | Prevents tampering | Windows-only | Harder debugging | Installer scripts |
| O-MM-4 | Device flexibility | Network-dependent | Latency for control actions | WebSocket plans |
| O-MM-5 | Boots straight into Core + UI combo, works for SBCs | Requires Linux packaging + service supervision | API/bridge regressions can strand headless rigs | Linux Core kiosk plan |

## Evaluation criteria
Operator workflow, recovery from display loss, remote access needs, deployment complexity, latency.

## Current sentiment
- Operators rely on multi-monitor helpers today, but headless support is ad hoc and needs a clearer plan.
- The community wants kiosk scripts and watchdogs bundled with any Linux Core pilot so remote dashboards recover automatically after power loss.【F:docs/SRS/options/O-BACKEND-6_LinuxCoreService.md†L6-L23】【F:docs/SRS/options/O-FRONT-6_RemoteClients.md†L10-L34】

## Upcoming ADR coverage
- **ADR-007 PoseStream & SectionState architecture** introduces SectionState diffing tied to the control graph, binding R-CTRL-000 through R-CTRL-002 to the unified pose timeline for replay accuracy.【F:docs/ADR/ADR-roadmap.md†L38-L91】
- **ADR-015 Section control & grouping semantics** will finalize arbitration, master group handling, and plugin hooks required by R-CTRL-000 and R-CTRL-001 while keeping toolbar overrides aligned with R-CTRL-002.【F:docs/ADR/ADR-roadmap.md†L228-L253】
- **ADR-017 Profiles & kinematics** ensures toolbar placement, hitch models, and multi-steer profiles feed the control lookahead policies outlined in R-CTRL-002.【F:docs/ADR/ADR-roadmap.md†L142-L167】

## Open questions
- What minimum telemetry needs to be exposed to a headless dashboard?
- How do we secure remote control if the UI isn’t local?
