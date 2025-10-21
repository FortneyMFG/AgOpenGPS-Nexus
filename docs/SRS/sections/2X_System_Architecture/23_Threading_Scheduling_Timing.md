# 23 — Threading, Scheduling & Timing
*(Status: collecting proposals)*

**Section ID:** 23
**Version:** 0.1.0
**Editors:** @nexus-specs, @replay-wg
**Last Updated:** 2025-02-14
**Related Sections:** 21, 22, 24, 61, 64
**Upstream Dependencies:** 11, 21
**Downstream Impacts:** 51, 81, 93

---

## 23.1 Purpose & Scope

Capture timing budgets, threading models, and scheduling primitives required to keep pose fusion, section control, and UI rendering within deterministic bounds across desktop and headless deployments.【F:docs/SRS/sections/6X_Core_Domain_Services/61_Kinematics_Pose_Fusion.md†L1-L156】【F:docs/SRS/sections/6X_Core_Domain_Services/64_Telemetry_Health.md†L12-L96】

---

## 23.2 Context

- SimClock/SimBus from §21 govern deterministic loops for simulation, replay, and hardware ingestion.【F:docs/SRS/sections/2X_System_Architecture/21-ADR-004 - Establish the composite simulation fabric (SimClock + SimBus).md†L14-L46】
- Headless batching (CLI jobs, automation) must not starve real-time threads while honoring CLI SLA targets.【F:docs/SRS/sections/9X_Frontends_Ops/93_Command_Line_Interface.md†L15-L66】
- Remote frontends rely on clock synchronization (PTP/NTP) to keep telemetry overlays aligned with Core state.【F:docs/SRS/sections/2X_System_Architecture/21-O6 - Linux Core service with remote frontends.md†L13-L62】

---

## 23.3 Legacy Comparison

| Area / Theme | Legacy Behavior | Identified Limitation | Modernization Opportunity | Reference / Source |
|--------------|-----------------|-----------------------|---------------------------|--------------------|
| Threading Model | UI manages worker threads manually for pose and mapping loops. | Risk of contention and UI stalls under load. | Adopt hosted services and channels per loop with telemetry instrumentation. | 【F:docs/SRS/sections/2X_System_Architecture/21_System_Decomposition_Boundaries.md†L1-L70】 |
| Timing Budgets | Informal expectations without instrumentation. | Regression detection requires manual observation. | Define explicit per-loop budgets and publish telemetry alerts. | 【F:docs/SRS/sections/6X_Core_Domain_Services/64_Telemetry_Health.md†L28-L96】 |
| Clock Sync | Each executable maintains its own clock. | Drift between processes breaks replay fidelity. | Provide shared SimClock and cross-process synchronization. | 【F:docs/SRS/sections/6X_Core_Domain_Services/61_Kinematics_Pose_Fusion.md†L12-L128】 |

---

## 23.4 Definitions

| Term | Definition |
|------|------------|
| SimClock | Authoritative fixed-step clock shared across Core services and plugins. |
| SimBus | Publish/subscribe fabric carrying deterministic topics (pose, telemetry, sections). |
| Deadline Monitor | Telemetry component logging latency and jitter metrics per loop. |
| Headless Batch | CLI-triggered workload processed without UI interaction. |

---

## 23.5 Requirements

| ID | Priority | Category | Summary | Source / C-IDs | Key Metrics / Verification |
|----|----------|----------|---------|----------------|-----------------------------|
| R-TIME-000 | MUST | Clock | Maintain canonical SimClock driving simulation, replay, and hardware ingestion with drift metrics. | §21, Simulation WG | Drift ≤2 ms vs hardware clock.【F:docs/SRS/sections/6X_Core_Domain_Services/61_Kinematics_Pose_Fusion.md†L12-L84】 |
| R-TIME-001 | MUST | Latency | Pose fusion ≤10 ms, section command ≤20 ms, UI map refresh ≤33 ms with telemetry monitoring. | Timing WG | Telemetry alerts at 90% threshold; CI benchmark enforcement.【F:docs/SRS/sections/6X_Core_Domain_Services/61_Kinematics_Pose_Fusion.md†L86-L156】【F:docs/SRS/sections/6X_Core_Domain_Services/64_Telemetry_Health.md†L28-L94】 |
| R-TIME-002 | SHOULD | Scheduling Model | Use dedicated worker services/channels for high-rate loops; UI threads marshal via observable caches. | Architecture WG | Hosted service harness ensures zero UI thread blocking under load.【F:docs/SRS/sections/2X_System_Architecture/21_System_Decomposition_Boundaries.md†L7-L45】 |
| R-TIME-003 | MUST | Preemption & Priority | Prioritize safety-critical loops above telemetry batching and UI; enforce back-pressure when deadlines slip. | Safety review | Stress test verifies high-priority loops meet deadlines under overload.【F:docs/SRS/sections/6X_Core_Domain_Services/61_Kinematics_Pose_Fusion.md†L52-L156】 |
| R-TIME-004 | SHOULD | Multi-process Sync | Provide cross-process clock sync (PTP/NTP, shared memory fences) keeping remote clients within ±5 ms of Core timeline. | Remote ops WG | Sync benchmark demonstrates ≤5 ms skew.【F:docs/SRS/sections/5X_Hardware_IO_Device_Layer/53_AOG_Link_Compatibility.md†L340-L368】【F:docs/SRS/sections/8X_Guidance/81_Guidance_Orchestrator.md†L102-L166】 |
| R-TIME-005 | MUST | Headless Batching | Guarantee headless batches honor CLI SLA without starving control threads. | Ops CLI | CLI benchmark ensures batch completes while control loops stay within budgets.【F:docs/SRS/sections/9X_Frontends_Ops/93_Command_Line_Interface.md†L15-L66】 |

### 23.5.1 Requirement Sources & Rationale

| Req ID | Source | Rationale |
|--------|--------|-----------|
| R-TIME-000 | Simulation WG | Ensures deterministic playback for CI and training. |
| R-TIME-001 | Timing review (2025-01) | Protects operator experience and control stability. |
| R-TIME-003 | Safety audit | Avoids automation regressions under load. |
| R-TIME-005 | CLI roadmap | Supports remote management and scripted workflows. |

---

## 23.6 Acceptance Criteria & Verification

Automation and replay harnesses must demonstrate latency budgets, scheduling priorities, and cross-process sync before shipping Linux Core pilots.
Telemetry dashboards expose live loop metrics and alert on threshold breaches.

### 23.6.1 Requirement-to-Verification Map

| Req ID | Verification Type | Artifact / Location | Pass/Fail Threshold |
|--------|-------------------|---------------------|---------------------|
| R-TIME-000 | Timing benchmark | `bench/simbus_timing.md` | Drift ≤2 ms |
| R-TIME-001 | Replay harness | `tests/replay/loop_latency.md` | Pose ≤10 ms, Section ≤20 ms, UI ≤33 ms |
| R-TIME-002 | Integration test | `tests/integration/hosted_services.md` | No UI thread blocking events |
| R-TIME-003 | Stress test | `tests/load/high_priority_preemption.md` | Safety loops meet deadlines |
| R-TIME-004 | Sync benchmark | `bench/clock_sync.md` | Skew ≤5 ms |
| R-TIME-005 | CLI benchmark | `tests/cli/headless_batch.md` | Control loops within budgets |

---

## 23.7 Constraints

- Scheduling policies MUST be documented for every high-rate service and remain reproducible under simulation seeds.【F:docs/SRS/sections/2X_System_Architecture/21-ADR-004 - Establish the composite simulation fabric (SimClock + SimBus).md†L14-L46】
- Telemetry MUST surface latency metrics for operators and CI gating (§64).【F:docs/SRS/sections/6X_Core_Domain_Services/64_Telemetry_Health.md†L28-L126】
- Clock synchronization MUST use secure protocols when spanning networks shared with remote clients.

---

## 23.12 Option Evaluation

### 23.12.1 Scheduling Strategies

| Strategy | Summary | Notes |
|----------|---------|-------|
| Cooperative loops | Current approach using manual timers and UI thread marshaling. | Legacy compatibility; limited priority control. |
| Hosted services + channels | .NET hosted services with bounded channels per loop. | Preferred modernization; aligns with §21 layer controllers. |
| Real-time Linux tuning | Apply PREEMPT_RT kernels and priority tuning. | Reserved for advanced deployments; increases maintenance overhead. |

### 23.12.2 Decision Summary

Hosted services and bounded channels are the favored strategy because they align with deterministic SimBus integration while keeping deployment complexity manageable; real-time kernel tuning remains optional for specialized rigs.【F:docs/SRS/sections/2X_System_Architecture/21-O5 - Layer controllers with aggregation pipelines.md†L29-L58】

---

## 23.13 Evaluation & Verification

CI must run replay-driven timing suites on reference hardware (Windows, Linux CM5) before approving schedule-related ADRs.
Field validation captures telemetry snapshots confirming latency budgets during guidance sessions.

---

## 23.14 Implementation Policy

- All hosted services register explicit priority classes (safety, guidance, telemetry, UI) with shared monitoring hooks.
- Introduce deadline monitors publishing to telemetry dashboards and CLI health checks.
- Document recommended thread counts and CPU affinity guidelines for CM5 and desktop targets.

---

## 23.15 Community Sentiment

Contributors view deterministic scheduling as prerequisite for Linux deployments and plugin expansion; shared timing primitives and telemetry instrumentation must land before exposing extensibility hooks that could disrupt control loops.【F:docs/SRS/sections/6X_Core_Domain_Services/61_Kinematics_Pose_Fusion.md†L12-L156】【F:docs/SRS/sections/6X_Core_Domain_Services/64_Telemetry_Health.md†L28-L126】

### 23.15.1 Section Change Log

| Date | Summary | PR / Issue |
|------|---------|------------|
| 2025-02-14 | Reformatted to SRS v2 template with verification table. | #0000 |

---

## 23.16 Traceability

| Requirement ID | Related Option(s) | ADR(s) | Verification Artifact | Implementation Reference |
|----------------|-------------------|--------|-----------------------|--------------------------|
| R-TIME-000 | Hosted services + channels | 21-ADR-004 | `bench/simbus_timing.md` | `docs/SRS/sections/2X_System_Architecture/21-ADR-004 - Establish the composite simulation fabric (SimClock + SimBus).md` |
| R-TIME-001 | Hosted services + channels | 21-ADR-068 | `tests/replay/loop_latency.md` | `docs/SRS/sections/2X_System_Architecture/21-O5 - Layer controllers with aggregation pipelines.md` |
| R-TIME-003 | Hosted services + channels | 21-ADR-028 | `tests/load/high_priority_preemption.md` | `docs/SRS/sections/2X_System_Architecture/21_System_Decomposition_Boundaries.md` |
| R-TIME-005 | Hosted services + channels | 21-ADR-900 | `tests/cli/headless_batch.md` | `docs/SRS/sections/9X_Frontends_Ops/93_Command_Line_Interface.md` |

---

## 23.17 Conformance

Implementations conform when all MUST requirements meet timing thresholds across replay, hardware-in-the-loop, and headless batch scenarios, with telemetry demonstrating compliance and exceptions logged for any deferred **SHOULD** targets.

---

## Standards Context

Aligned with ISO/IEC/IEEE 29148:2018 timing requirement practices and IEEE 1016:2017 guidance for documenting concurrency and scheduling policies in software design descriptions.
