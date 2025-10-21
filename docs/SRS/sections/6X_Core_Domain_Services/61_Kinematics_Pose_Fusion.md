# 61 — Kinematics & Pose Fusion (Status: collecting proposals)

## Problem statement
Define how Nexus coordinates operator stations, automation plugins, and firmware so control actions remain deterministic across multi-monitor rigs, headless deployments, and remote clients.

## Requirements (from contributors)

### R-MM — Multi-monitor & Headless baselines
- R-MM-000 (MUST, current-AgOpenGPS): Preserve screen placement helpers that ensure WinForms windows remain visible across multiple monitors.【F:SourceCode/GPS/Helpers/ScreenHelper.cs†L1-L30】
- R-MM-001 (SHOULD, current-AgIO): Keep UDP/serial monitors operable when the UI is minimized or moved off the primary screen for diagnostics.【F:SourceCode/AgIO/Source/Forms/FormUDPMonitor.cs†L8-L70】
- R-MM-002 (SHOULD): Provide a story for running the backend with no local UI while exposing remote displays or APIs.
- R-MM-003 (SHOULD, proposed-LinuxCore): Deliver kiosk/headless launchers for the Linux Core + remote clients (auto-login, fullscreen) so rigs boot directly into an operator-ready view.【F:docs/SRS/sections/2X_System_Architecture/21-O6%20-%20Linux%20Core%20service%20with%20remote%20frontends.md†L6-L23】【F:docs/SRS/sections/9X_Frontends_Ops/91-O6%20-%20Remote%20gRPC-WebSocket%20clients%20backed%20by%20the%20Linux%20Core.md†L10-L18】
- R-MM-004 (COULD): Add locked-down layouts that survive accidental window drags or task-switching.
- R-MM-005 (SHOULD, resilience): Capture watchdog and auto-recovery expectations for kiosk/headless deployments (service restart policies, layout reset scripts, power-loss recovery steps) so rigs can return to an operator-ready state without manual intervention.

### R-CTRL — Control graph & automation semantics
- R-CTRL-000 (MUST, section control graph): Define the authoritative on/auto/off control graph, including master switches and safety gating, so Core, firmware, and plugins arbitrate SectionState changes consistently.
- R-CTRL-001 (SHOULD, grouping semantics): Support overlapping SectionGroups with deterministic priority rules and operator overrides so complex implements remain predictable across UI, control, and simulation surfaces.
- R-CTRL-002 (SHOULD, toolbar lookahead): Allow per-toolbar lookahead/overlap tuning with documented defaults and override bounds so rate controllers and coverage renderers stay in sync.
- R-CTRL-003 (SHOULD, automation lifecycle): Capture enable/disable policies for automation plugins (autosteer, section control, rate) so Core mediates state transitions and logs operator intent.
- R-CTRL-004 (SHOULD, safety interlocks): Document interlock expectations (hydraulic lockouts, seat switches, user acknowledgements) that automation plugins must honor before commanding sections or steering.
- R-CTRL-005 (COULD, remote supervision): Allow privileged remote clients to request control leases with explicit operator acknowledgement workflows to support tele-assist scenarios.
- R-CTRL-006 (MUST, constraint gating): Insert a constraint gate in the control arbiter that prevents autosteer engagement and forces section/rate outputs off when keep-out zones intersect the implement footprint, while trimming guidance terminals at boundaries/headlands.
- R-CTRL-007 (SHOULD, override policy): Provide configurable operator override policies for work-disabled zones (e.g., hold-to-confirm) with audit logging so product shutoff behavior remains transparent and traceable.

### Remote dashboards & telemetry-only sharing
- Remote dashboards default to monitor-only capabilities. They may subscribe to pose, coverage, section state, and report metrics but cannot toggle automation unless the operator grants a time-bounded control lease. Offline rigs operate normally without connectivity.
- Multi-machine share profiles expose telemetry feeds but strip command topics when the connection is flagged untrusted. Operators can promote a remote station to control by issuing an explicit lease inside the cab UI.

### Section control advisory masks
- Section control reads advisory `noWorkMask` data (e.g., from crop protection or regulatory overlays) and displays warnings when entering these areas. The arbiter never blocks sections solely on advisory masks; only enforced keep-out zones from ADR-027 gate automation.
- Advisory acknowledgements log `actor`, `timestamp`, and `reason` so compliance reports can reconcile operator decisions with product usage.

## Options
- O-MM-0: Status quo — Desktop windows with manual layout tools and helper checks.
- O-MM-1: Layout profiles synchronized across displays and saved per rig.
- O-MM-2: Dedicated headless service with remote web UI.
- O-MM-3: Windows kiosk mode packaging for cab computers.
- O-MM-4: Remote-only thin client (tablet) controlling a headless backend.
- O-MM-5: Linux Core kiosk/headless deployment with remote frontends.【F:docs/SRS/sections/2X_System_Architecture/21-O6%20-%20Linux%20Core%20service%20with%20remote%20frontends.md†L6-L44】【F:docs/SRS/sections/9X_Frontends_Ops/91-O6%20-%20Remote%20gRPC-WebSocket%20clients%20backed%20by%20the%20Linux%20Core.md†L1-L34】

### Equipment configuration primitives
- O-HW-7: Multi-steer equipment configurator primitives describe the module catalog, hitch rules, sensor attachments, mode profiles, calibration flows, telemetry surfaces, definition-of-done gates, and the axle-centric runtime graph that [ADR-017](../ADR/ADR-017-profiles-kinematics.md) consumes for articulated, tracked, or multi-axle rigs.【F:docs/SRS/sections/6X_Core_Domain_Services/61-O7%20-%20Multi-steer%20equipment%20configurator%20primitives.md†L1-L420】

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
Operator workflow, recovery from display loss, remote access needs, control determinism, latency budgets, and auditability.

## Current sentiment
- Operators rely on multi-monitor helpers today, but headless support is ad hoc and needs a clearer plan.
- The community wants kiosk scripts and watchdogs bundled with any Linux Core pilot so remote dashboards recover automatically after power loss.【F:docs/SRS/sections/2X_System_Architecture/21-O6%20-%20Linux%20Core%20service%20with%20remote%20frontends.md†L6-L23】【F:docs/SRS/sections/9X_Frontends_Ops/91-O6%20-%20Remote%20gRPC-WebSocket%20clients%20backed%20by%20the%20Linux%20Core.md†L10-L34】
- Control arbitration must consolidate in Core so plugins remain optional and headless deployments retain deterministic behavior.
- Constraint gates for spatial zones need to live in the same arbiter to keep automation deterministic and ensure plugins cannot bypass safety policies.

## Upcoming ADR coverage
- **ADR-007 PoseStream & SectionState architecture** introduces SectionState diffing tied to the control graph, binding R-CTRL-000 through R-CTRL-002 to the unified pose timeline for replay accuracy.【F:docs/SRS/sections/2X_System_Architecture/21-ADR-900 - PoseStream, Layer, and Control Program Roadmap.md†L67-L73】
- **ADR-027 Spatial constraints & zone policies** threads zone masks and constraint gates through the control arbiter, fulfilling R-CTRL-000 and R-CTRL-006…R-CTRL-007 while keeping automation overrides auditable.【F:docs/SRS/sections/2X_System_Architecture/21-ADR-900 - PoseStream, Layer, and Control Program Roadmap.md†L27-L41】
- **ADR-015 Section control & grouping semantics** will finalize arbitration, master group handling, and plugin hooks required by R-CTRL-000 and R-CTRL-001 while keeping toolbar overrides aligned with R-CTRL-002.【F:docs/SRS/sections/2X_System_Architecture/21-ADR-900 - PoseStream, Layer, and Control Program Roadmap.md†L118-L124】
- **ADR-017 Profiles & kinematics** ensures toolbar placement, hitch models, and multi-steer profiles feed the control lookahead policies outlined in R-CTRL-002.【F:docs/SRS/sections/2X_System_Architecture/21-ADR-900 - PoseStream, Layer, and Control Program Roadmap.md†L134-L140】
- **ADR-018 Plugin API & capability discovery** codifies automation lifecycle, permissions, and safety surfaces required by R-CTRL-003 through R-CTRL-005 so plugins stay bounded by Core arbitration.【F:docs/SRS/sections/2X_System_Architecture/21-ADR-900 - PoseStream, Layer, and Control Program Roadmap.md†L142-L148】

## Open questions
- What minimum telemetry needs to be exposed to a headless dashboard?
- How do we secure remote control if the UI isn’t local?
