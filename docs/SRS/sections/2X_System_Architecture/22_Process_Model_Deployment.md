# 22 — Process Model & Deployment Topologies (Status: collecting proposals)

## Problem statement
Define how Nexus components split across processes, containers, and devices so guidance-critical workloads stay responsive while enabling remote clients and cloud-assisted analytics.【F:docs/SRS/options/2X/O-BACKEND-6_LinuxCoreService.md†L1-L44】【F:docs/SRS/sections/9X_Frontends_Ops/91_UI_Shell_Layout.md†L13-L76】

## Requirements (from contributors)
- R-PROC-000 (MUST, single-machine): Support an in-process mode where UI, Core services, and AgIO run in one .NET host for Windows quick-start rigs while keeping deterministic timing hooks for simulation.【F:docs/SRS/sections/2X_System_Architecture/21_System_Decomposition_Boundaries.md†L1-L45】
- R-PROC-001 (SHOULD, split-core): Provide an out-of-process Core service with gRPC/IPC bindings so UI shells and headless agents can run on separate processes or hardware without duplicating business logic.【F:docs/SRS/options/2X/O-BACKEND-6_LinuxCoreService.md†L1-L44】
- R-PROC-002 (SHOULD, distributed rigs): Enable distributed deployments where Core runs on CM5 or an edge PC while UI tablets connect over gRPC/WebSocket, including reconnection/resync policies.【F:docs/SRS/sections/9X_Frontends_Ops/91_UI_Shell_Layout.md†L33-L72】
- R-PROC-003 (MUST, container/headless): Ship container manifests (Docker/Podman) for Core + AgIO + telemetry sinks so farms can host Nexus as a managed service with OTA updates.【F:docs/SRS/sections/1X_Platform_Foundations/11_OS_Support.md†L14-L45】
- R-PROC-004 (SHOULD, analytics offload): Allow heavy analytics (machine learning, reporting) to run in optional worker processes that subscribe to Core journals without impacting guidance loops.【F:docs/SRS/sections/9X_Frontends_Ops/94_Extensibility_Packaging_Updates.md†L17-L120】
- R-PROC-005 (MUST, deterministic clocks): Provide a shared timing source and scheduling policy (SimClock, hardware clocks) so multi-process deployments keep pose fusion and automation loops within latency budgets.【F:docs/SRS/sections/6X_Core_Domain_Services/61_Kinematics_Pose_Fusion.md†L12-L128】

## Deployment patterns
| Pattern | Description | Targets |
|---|---|---|
| LocalInProc | Current WinForms/AgIO solution, all services in one process | Windows field PCs |
| LocalOutOfProc | Core daemon + desktop UI on same machine using gRPC IPC | Windows & Linux workstations |
| RemoteClient | Core + AgIO on CM5/edge device, UI tablets attach over network | CM5, Linux SBCs, Windows tablets |
| Containerized | Core/AgIO packaged in containers with remote UI and CLI control | Farm server rooms, managed fleets |

## Current sentiment
The community favors a hybrid approach: keep LocalInProc for legacy rigs while investing in LocalOutOfProc and RemoteClient topologies that unlock Linux deployments and remote displays without fragmenting the codebase.【F:docs/SRS/options/2X/O-BACKEND-6_LinuxCoreService.md†L1-L44】【F:docs/SRS/sections/9X_Frontends_Ops/91_UI_Shell_Layout.md†L33-L95】
