# 23 — Threading, Scheduling & Timing (Status: collecting proposals)

## Problem statement
Capture timing budgets, threading models, and scheduling primitives required to keep pose fusion, section control, and UI rendering within deterministic bounds across desktop and headless deployments.【F:docs/SRS/sections/6X/61_Kinematics_Pose_Fusion.md†L1-L156】【F:docs/SRS/sections/6X/64_Telemetry_Health.md†L12-L96】

## Requirements (from contributors)
- R-TIME-000 (MUST, deterministic clock): Maintain a canonical SimClock/Timebase that drives simulation, replay, and hardware ingestion; consumers must support fixed-step updates and publish drift metrics.【F:docs/SRS/sections/6X/61_Kinematics_Pose_Fusion.md†L12-L84】
- R-TIME-001 (MUST, latency budgets): Define per-loop budgets—pose fusion ≤10 ms, section command ≤20 ms, UI map refresh ≤33 ms—and monitor them via telemetry to guard regressions.【F:docs/SRS/sections/6X/61_Kinematics_Pose_Fusion.md†L86-L156】【F:docs/SRS/sections/6X/64_Telemetry_Health.md†L28-L94】
- R-TIME-002 (SHOULD, scheduling model): Use dedicated worker services (e.g., hosted services, channels) for high-rate loops and message pumps; UI threads should only marshal state via observable caches.【F:docs/SRS/sections/2X/21_System_Decomposition_Boundaries.md†L7-L34】【F:docs/SRS/options/6X/O-BACKEND-4_LayerControllers.md†L19-L46】
- R-TIME-003 (MUST, preemption & priority): Prioritize safety-critical loops (pose fusion, autosteer arbitration) above telemetry batching, plugin analytics, and UI updates; enforce back-pressure when deadlines slip.【F:docs/SRS/sections/6X/61_Kinematics_Pose_Fusion.md†L52-L156】
- R-TIME-004 (SHOULD, multi-process sync): Provide cross-process clock synchronization (PTP/NTP, shared memory fences) so remote clients and AgIO stay within ±5 ms of the authoritative Core timeline.【F:docs/SRS/sections/5X/53_AOG_Link_Compatibility.md†L340-L368】【F:docs/SRS/sections/8X/81_Guidance_Orchestrator.md†L102-L166】
- R-TIME-005 (MUST, headless batching): Guarantee that headless batches (CLI commands, automated job replay) yield within CLI SLA targets without starving real-time control threads.【F:docs/SRS/sections/9X/93_Command_Line_Interface.md†L15-L66】

## Current sentiment
Deterministic scheduling is viewed as a prerequisite for both Linux deployments and advanced analytics; contributors are prioritizing shared timing primitives and performance telemetry before expanding plugin hooks that could disrupt control loops.【F:docs/SRS/sections/6X/61_Kinematics_Pose_Fusion.md†L12-L156】【F:docs/SRS/sections/6X/64_Telemetry_Health.md†L28-L126】
