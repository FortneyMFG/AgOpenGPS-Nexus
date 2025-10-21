# 22 — Process Model & Deployment Topologies
*(Status: collecting proposals)*

**Section ID:** 22
**Version:** 0.1.0
**Editors:** @nexus-specs, @deployment-wg
**Last Updated:** 2025-02-14
**Related Sections:** 21, 23, 24
**Upstream Dependencies:** 11, 21
**Downstream Impacts:** 41, 42, 91, 94

---

## 22.1 Purpose & Scope

Describe supported process layouts and deployment patterns for Nexus so critical guidance loops stay deterministic while enabling remote clients and analytics workloads.【F:docs/SRS/sections/2X_System_Architecture/21-O6 - Linux Core service with remote frontends.md†L1-L44】【F:docs/SRS/sections/9X_Frontends_Ops/91_UI_Shell_Layout.md†L13-L76】

---

## 22.2 Context

- Legacy rigs run all services in-process for simplicity, but modernization needs headless deployments with remote UI shells.【F:docs/SRS/sections/2X_System_Architecture/21_System_Decomposition_Boundaries.md†L1-L101】
- Deterministic SimClock/SimBus expectations from §21 require shared timing coordination across processes.【F:docs/SRS/sections/2X_System_Architecture/21-ADR-004 - Establish the composite simulation fabric (SimClock + SimBus).md†L14-L46】
- Packaging strategy must satisfy Windows quick starts and Linux CM5/service deployments simultaneously.【F:docs/SRS/sections/1X_Platform_Foundations/11_OS_Support.md†L14-L45】

---

## 22.3 Legacy Comparison

| Area / Theme | Legacy Behavior | Identified Limitation | Modernization Opportunity | Reference / Source |
|--------------|-----------------|-----------------------|---------------------------|--------------------|
| Hosting Model | Single executable hosts UI, Core, and AgIO. | Blocks remote displays and Linux pilots. | Introduce Core daemon with gRPC bridge. | 【F:docs/SRS/sections/2X_System_Architecture/21-O6 - Linux Core service with remote frontends.md†L1-L62】 |
| Deployment | Manual installer copies binaries and config. | Hard to automate fleet updates. | Provide container + package manifests. | 【F:docs/SRS/sections/1X_Platform_Foundations/11_OS_Support.md†L14-L45】 |
| Recovery | UI reboot restarts everything. | No health policy or watchdog separation. | systemd/service watchdogs with telemetry endpoints. | 【F:docs/SRS/sections/2X_System_Architecture/21-O6 - Linux Core service with remote frontends.md†L9-L62】 |

---

## 22.4 Definitions

| Term | Definition |
|------|------------|
| LocalInProc | Single-process deployment containing UI, Core, AgIO, and plugins. |
| LocalOutOfProc | UI and Core run on same machine but use IPC/gRPC boundaries. |
| RemoteClient | UI or automation client connects to Core over network. |
| Containerized Stack | Core, AgIO, and telemetry packaged for managed hosting with remote frontends. |

---

## 22.5 Requirements

| ID | Priority | Category | Summary | Source / C-IDs | Key Metrics / Verification |
|----|----------|----------|---------|----------------|-----------------------------|
| R-PROC-000 | MUST | Legacy Mode | Support single-process Windows host with deterministic timing hooks. | Legacy rigs | Replay timing jitter ≤2 ms p95 in in-process mode.【F:docs/SRS/sections/2X_System_Architecture/21_System_Decomposition_Boundaries.md†L1-L70】 |
| R-PROC-001 | SHOULD | Split Core | Provide out-of-process Core service with gRPC/IPC bindings. | Linux Core proposal | UI reconnect latency ≤500 ms during Core restarts.【F:docs/SRS/sections/2X_System_Architecture/21-O6 - Linux Core service with remote frontends.md†L1-L62】 |
| R-PROC-002 | SHOULD | Distributed Rigs | Enable remote clients with resync policies for pose/state. | Remote ops WG | Network reconnect recovers state within 3 s.【F:docs/SRS/sections/9X_Frontends_Ops/91_UI_Shell_Layout.md†L33-L95】 |
| R-PROC-003 | MUST | Containerization | Ship container manifests (Docker/Podman) for Core+AgIO+telemetry sinks. | Operations backlog | Smoke test ensures services boot under 60 s with health endpoints up.【F:docs/SRS/sections/1X_Platform_Foundations/11_OS_Support.md†L14-L45】 |
| R-PROC-004 | SHOULD | Analytics Offload | Allow optional worker processes for ML/reporting via journals. | Analytics roadmap | Worker load MUST not increase control loop latency >5 ms.【F:docs/SRS/sections/9X_Frontends_Ops/94_Extensibility_Packaging_Updates.md†L17-L211】 |
| R-PROC-005 | MUST | Timebase | Share authoritative timing source across processes for deterministic loops. | Simulation WG | SimClock drift ≤5 ms between processes.【F:docs/SRS/sections/6X_Core_Domain_Services/61_Kinematics_Pose_Fusion.md†L12-L128】 |

### 22.5.1 Requirement Sources & Rationale

| Req ID | Source | Rationale |
|--------|--------|-----------|
| R-PROC-000 | Legacy ops review | Maintains upgrade path for current field users. |
| R-PROC-001 | Linux Core RFC | Enables cross-platform adoption without duplicate logic. |
| R-PROC-003 | Fleet ops sync (2025-01) | Required for managed deployments. |
| R-PROC-005 | Simulation WG notes | Avoids drift between automation loops. |

---

## 22.6 Acceptance Criteria & Verification

- Smoke tests confirm each topology boots with expected health endpoints and telemetry sinks.
- Replay suites validate timing budgets across IPC boundaries before promoting releases.

### 22.6.1 Requirement-to-Verification Map

| Req ID | Verification Type | Artifact / Location | Pass/Fail Threshold |
|--------|-------------------|---------------------|---------------------|
| R-PROC-000 | Replay benchmark | `tests/replay/inproc_timing.md` | Jitter ≤2 ms p95 |
| R-PROC-001 | Integration test | `tests/integration/ipc_reconnect.md` | Reconnect <500 ms |
| R-PROC-003 | CI deployment | `pipelines/container_smoke.yml` | Boot <60 s, healthy |
| R-PROC-005 | Timing benchmark | `bench/synced_clock.md` | Drift ≤5 ms |

---

## 22.7 Constraints

- Packaging MUST support Windows installers and Debian packages using the same configuration schema (§24).【F:docs/SRS/sections/1X_Platform_Foundations/11_OS_Support.md†L14-L45】
- Headless deployments MUST expose gRPC + WebSocket APIs consistent with §21 options.【F:docs/SRS/sections/2X_System_Architecture/21-O6 - Linux Core service with remote frontends.md†L1-L62】
- Remote clients MUST authenticate via shared configuration/secret policy from §24.

---

## 22.12 Option Evaluation

### 22.12.1 Deployment Patterns

| Pattern | Description | Targets |
|---------|-------------|---------|
| LocalInProc | Current WinForms/AgIO solution, all services in one process. | Windows field PCs |
| LocalOutOfProc | Core daemon + desktop UI on same machine via gRPC IPC. | Windows & Linux workstations |
| RemoteClient | Core + AgIO on CM5/edge device; UI tablets connect over network. | CM5, Linux SBCs, Windows tablets |
| Containerized | Core/AgIO packaged in containers with remote UI and CLI control. | Farm server rooms, managed fleets |

### 22.12.2 Decision Summary

Maintain LocalInProc for legacy rigs while investing in LocalOutOfProc and RemoteClient topologies to unlock Linux deployments and multi-display scenarios; containerized stack provides managed fleet story.【F:docs/SRS/sections/2X_System_Architecture/21-O6 - Linux Core service with remote frontends.md†L1-L62】

---

## 22.13 Evaluation & Verification

Field pilots must validate reconnect flow, telemetry health, and container lifecycle (start/stop/restart) across representative hardware before marking the section “final”.

---

## 22.14 Implementation Policy

- Ship reference systemd service files and Windows services templates aligned with packaging outputs.
- Provide CLI tooling for topology detection and health diagnostics.
- Document network port usage and TLS requirements for remote clients.

---

## 22.15 Community Sentiment

The community favors a hybrid approach: retain LocalInProc for quick-start rigs while investing in Linux-friendly Core daemon deployments to decouple UI refresh cadence from guidance-critical loops.【F:docs/SRS/sections/2X_System_Architecture/21-O6 - Linux Core service with remote frontends.md†L1-L62】【F:docs/SRS/sections/9X_Frontends_Ops/91_UI_Shell_Layout.md†L33-L95】

### 22.15.1 Section Change Log

| Date | Summary | PR / Issue |
|------|---------|------------|
| 2025-02-14 | Reformatted to SRS v2 template and added verification map. | #0000 |

---

## 22.16 Traceability

| Requirement ID | Related Option(s) | ADR(s) | Verification Artifact | Implementation Reference |
|----------------|-------------------|--------|-----------------------|--------------------------|
| R-PROC-000 | LocalInProc | — | `tests/replay/inproc_timing.md` | `SourceCode/AgOpenGPS.Core/ApplicationCore.cs` |
| R-PROC-001 | LocalOutOfProc, RemoteClient | 21-ADR-028 | `tests/integration/ipc_reconnect.md` | `docs/SRS/sections/2X_System_Architecture/21-O6 - Linux Core service with remote frontends.md` |
| R-PROC-003 | Containerized | 21-ADR-028 | `pipelines/container_smoke.yml` | `packaging/` manifests |
| R-PROC-005 | All patterns | 21-ADR-004 | `bench/synced_clock.md` | `docs/SRS/sections/2X_System_Architecture/21-ADR-004 - Establish the composite simulation fabric (SimClock + SimBus).md` |

---

## 22.17 Conformance

Conformance requires demonstrating shared timing, health visibility, and deployment artifacts for each supported topology while documenting exceptions for any deferred **SHOULD** requirements.

---

## Standards Context

Aligned with ISO/IEC/IEEE 29148:2018 deployment considerations and IEEE 1016:2017 interface documentation practices for distributed systems.
