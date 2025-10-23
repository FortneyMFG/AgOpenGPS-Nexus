# 61 — Kinematics & Pose Fusion
*(Status: Proposed)*

**Author:** Codex
**Created:** 2025-10-20
**Status:** Proposed
**Version:** 0.1.0
**Section ID:** 61
**Editors:** @Codex
**Last Updated:** 2025-10-20
**Related Sections:** 21 System Architecture, 62 Job Lifecycle, 64 Telemetry & Health
**Upstream Dependencies:** 21-ADR-900 PoseStream Roadmap, 41 Transport Contracts
**Downstream Impacts:** Automation Engine, Section Control Arbiter, Telemetry Mesh

---

## 61.1 Purpose & Scope

This section defines the requirements, constraints, and design context for the Nexus
kinematics and pose fusion domain services. It focuses on how Core sequences tractor,
implement, toolbar, and section state so deterministic automation, replay accuracy,
and remote supervision behave consistently across multi-monitor, kiosk, and headless
rig deployments.【F:docs/development/SRS/sections/6X_Core_Domain_Services/61_Kinematics_Pose_Fusion.md†L4-L82】

---

## 61.2 Context

- Depends on a unified PoseStream timeline that merges pose-producing sources with
  deterministic cadence guarantees.
- Interacts with automation plugins (autosteer, section, rate), telemetry meshes,
  and equipment configuration services to arbitrate SectionState changes.
- Out of scope: mathematical solvers that execute kinematics at runtime; these are
  addressed by the kinematics ADR portfolio.

---

## 61.3 Legacy Comparison

| Area / Theme | Legacy Behavior | Identified Limitation | Modernization Opportunity | Reference / Source |
|--------------|-----------------|-----------------------|---------------------------|--------------------|
| Architecture | Multi-monitor helpers and discrete pose logs across AgOpenGPS, AgIO, and plugins. | Independent cadence assumptions break determinism and complicate automation gating. | Fuse pose timelines through PoseStream with deterministic diff ordering. | 【F:docs/development/SRS/sections/6X_Core_Domain_Services/61_Kinematics_Pose_Fusion.md†L8-L45】 |
| Performance | Manual layout recovery and ad hoc headless support. | Operators lose time recovering layouts; headless rigs lack watchdog policies. | Provide kiosk/headless launchers with watchdog recovery and remote dashboards. | 【F:docs/development/SRS/sections/6X_Core_Domain_Services/61_Kinematics_Pose_Fusion.md†L18-L42】 |
| UX / Config | Equipment grouping and overrides maintained through UI heuristics. | Overlapping SectionGroups lack deterministic arbitration rules. | Codify control graph priorities and override policies in Core arbitration. | 【F:docs/development/SRS/sections/6X_Core_Domain_Services/61_Kinematics_Pose_Fusion.md†L45-L74】 |

---

## 61.4 Definitions

| Term | Definition |
|------|------------|
| PoseStream | Unified timeline that sequences tractor, implement, and section pose data with shared cadence rules. |
| SectionState | Deterministic diff describing section automation states emitted on the PoseStream timeline. |
| Constraint Gate | Arbitration stage that enforces spatial keep-out zones before automation actions execute. |
| SectionGroup | Logical grouping of sections with deterministic priority and override semantics. |

---

> **Requirement Grammar (RFC-2119):**
> - **MUST / MUST NOT** = mandatory; test must exist.
> - **SHOULD / SHOULD NOT** = strong recommendation; justify exceptions.
> - **MAY** = optional; document enabling conditions.

## 61.5 Requirements

| ID | Priority | Category | Summary | Source / C-IDs | Key Metrics / Verification |
|----|----------|----------|---------|-----------------|----------------------------|
| R-MM-000 | MUST | UX / Reliability | Preserve multi-monitor helpers so WinForms windows stay visible across displays. | C1 | Regression UI test covering ScreenHelper; manual verification on multi-display rigs. |
| R-MM-001 | SHOULD | Diagnostics | Keep UDP/serial monitors operable when UI is minimized or off primary screen. | C1 | Integration test exercising AgIO monitors via automation harness. |
| R-MM-002 | SHOULD | Deployment | Provide a backend-only mode with remote displays or APIs. | C2 | Headless CI scenario demonstrating remote dashboard attach/detach. |
| R-MM-003 | SHOULD | Deployment | Deliver kiosk/headless launchers for Linux Core with auto-login and fullscreen defaults. | C2 | Systemd unit and e2e boot smoke for kiosk profile ≤ 120 s to ready UI. |
| R-MM-004 | MAY | UX | Add locked-down layouts that survive accidental window manipulation. | C1 | UI smoke verifying layout persistence after restart. |
| R-MM-005 | SHOULD | Resilience | Capture watchdog and auto-recovery expectations for kiosk/headless deployments. | C2 | Fault-injection replay verifying service restarts restore operator-ready state. |
| R-CTRL-000 | MUST | Control Graph | Define authoritative on/auto/off control graph including master switches and safety gating. | C3 | Control arbiter spec + CI tests exercising SectionState transitions. |
| R-CTRL-001 | SHOULD | Control Graph | Support overlapping SectionGroups with deterministic priority rules and overrides. | C3 | Simulation verifying deterministic ordering for conflict scenarios. |
| R-CTRL-002 | SHOULD | Lookahead | Allow per-toolbar lookahead/overlap tuning with defaults and bounds. | C4 | Parameterized tests ensuring lookahead adjustments stay within bounds. |
| R-CTRL-003 | SHOULD | Lifecycle | Capture enable/disable policies for automation plugins with logging. | C3 | Audit log fixtures proving lifecycle transitions recorded. |
| R-CTRL-004 | SHOULD | Safety | Document interlock expectations prior to automation commands. | C5 | Safety checklist validated in integration tests with hardware simulators. |
| R-CTRL-005 | COULD | Remote Supervision | Allow privileged remote clients to request time-bounded control leases with acknowledgements. | C6 | Remote supervision acceptance test verifying lease workflow. |
| R-CTRL-006 | MUST | Safety | Insert constraint gate preventing automation in keep-out zones and trimming terminals. | C5 | Spatial constraint replay verifying automation suppressed during zone intersection. |
| R-CTRL-007 | SHOULD | Override Policy | Provide configurable operator overrides for work-disabled zones with audit logging. | C5 | Log review verifying overrides recorded with actor/timestamp. |

### 61.5.1 Requirement Sources & Rationale

| Req ID | Source (issue/discussion/standard) | Rationale |
|--------|------------------------------------|-----------|
| R-MM-000 | Legacy multi-monitor support threads | Maintains current operator workflows without regressions. |
| R-MM-003 | Linux Core kiosk pilot plan | Enables headless rigs to boot into operational state. |
| R-CTRL-000 | Section control working group | Establishes deterministic arbitration baseline. |
| R-CTRL-006 | Spatial constraints ADR | Ensures safety policies override automation commands. |

---

## 61.6 Acceptance Criteria & Verification

Compliance is validated through UI regression suites, headless boot smoke tests,
constraint-gate simulation runs, and audit log inspections. Automation CI must
prove deterministic ordering of SectionState transitions while fault-injection tests
exercise watchdog recovery scenarios.

### 61.6.1 Requirement-to-Verification Map

| Req ID | Verification Type | Artifact / Location | Pass/Fail Threshold |
|--------|-------------------|---------------------|---------------------|
| R-MM-000 | UI regression | `tests/ui/MultiMonitorLayout.cs` | Windows remain anchored after move/restart cycles. |
| R-MM-003 | System validation | `ci/kiosk/headless_boot.yml` | Boot-to-ready ≤ 120 s, watchdog restart ≤ 30 s. |
| R-CTRL-000 | Simulation | `sim/control/SectionArbiterScenario.json` | No conflicting states; transitions deterministic. |
| R-CTRL-006 | Replay test | `tests/replay/ConstraintGateFixture.jsonl` | Automation suppressed inside keep-out zones. |

---

## 61.7 Constraints

- Must expose PoseStream and SectionState on a shared deterministic timeline.
- Must interoperate with existing AgIO transports and plugin APIs without breaking
  backward compatibility during transition.
- Must comply with automation safety policies and zone constraint governance.

---

## 61.8 Stakeholder Expectations

Operators require predictable layouts and overrides, plugin authors need a stable
control graph interface, and safety reviewers expect traceable automation gating.

---

## 61.9 Design Considerations

Enumerate major factors or guiding themes that shape design options.

| ID | Consideration | Description |
|----|---------------|-------------|
| C1 | Multi-monitor continuity | Preserve legacy window placement helpers and operator muscle memory during migration to new shells. |
| C2 | Headless resilience | Provide kiosk/headless launchers with watchdog policies so remote dashboards and services recover autonomously. |
| C3 | Deterministic control arbitration | Core must arbitrate SectionState transitions with deterministic ordering across plugins and automation modules. |
| C4 | Toolbar lookahead harmonization | PoseStream and toolbar profiles must remain in sync so rate and section outputs align with coverage rendering. |
| C5 | Spatial safety enforcement | Constraint gates must guard automation entry into keep-out zones while recording overrides for audit. |
| C6 | Remote supervision governance | Control leases require operator acknowledgement, traceability, and bounded duration for tele-assist scenarios. |
| C7 | Multi-steer configuration primitives | Equipment configurator must capture articulation, steering modes, and hitch graphs so ADR-017 kinematics and planners share consistent topology metadata. |

### 61.9.1 Assumptions & Preconditions

- [A1] PoseStream cadence ≥ 20 Hz for 48-section rigs.
- [A2] Network time synchronization within ±25 ms for deterministic diff ordering.
- [A3] Operators have authority to approve remote control leases when requested.

#### 61.9.2 Multi-steer configurator detail (from former Option 61-O7)

The equipment configurator supplies starter skeletons (single-frame, center, tandem,
implement articulation) with guided workflows for steering module selection, hitch
coupler definitions, and link graph editing. Profiles declare canonical frames,
unit sets, timestamp skew budgets, and schema hashes. Module catalogs cover front,
rear, coordinated, counter, independent, passive caster, hitch follower, crab,
track differential, and articulation steering behaviors with explicit latency,
backlash, and authority policies. Hitch couplers enumerate degrees of freedom and
train-of-implements rules, while the link graph editor ensures a single rooted
kinematic tree with serialization compatible with ADR-017 ingestion.

---

## 61.10 Option Overview

| Option ID | Status | Type / Theme | Description | Reference Document |
|-----------|--------|--------------|-------------|--------------------|
| 61-O1 | Proposed | Layout Strategy | Preserve desktop window helpers with enhanced persistence. | — |
| 61-O2 | In Review | Headless Deployment | Linux Core kiosk/headless service with remote UI. | 21-O6 Linux Core Service |
| 61-O3 | In Review | Remote Client | Remote-only thin clients controlling a headless backend with leases. | 91-O6 Remote Clients |
| 61-O4 | Exploring | Kiosk Packaging | Windows kiosk packaging for cab computers. | — |
| 61-O5 | Exploring | Watchdog Automation | Combined watchdog + layout recovery scripts. | — |

---

## 61.11 Comparison Matrix

| Attribute / Criteria | 61-O1 | 61-O2 | 61-O3 |
|----------------------|-------|-------|-------|
| Operator Familiarity | High | Medium | Medium |
| Deployment Complexity | Low | Medium | High |
| Headless Support | Low | High | High |
| Remote Supervision | Low | Medium | High |

---

## 61.12 Option Evaluation

Weighted scoring and evidence are documented in forthcoming ADRs; preliminary
analysis favors combining kiosk launchers (61-O2) with deterministic control
arbitration to satisfy safety considerations.

### 61.12.3 Scoring Evidence

| Criterion | 61-O1 Justification | 61-O2 Justification | 61-O3 Justification |
|-----------|---------------------|---------------------|---------------------|
| Implementation Complexity | Minor UI persistence updates. | Requires service packaging and watchdog integration. | Needs remote UX + security review. |
| Performance / Quality Impact | Minimal change. | Ensures deterministic boot and automation gating. | Depends on network latency tolerance. |
| Maintainability | Maintains existing layout code. | New scripts but aligns with Linux Core roadmap. | Higher operational overhead. |
| Extensibility / Roadmap Fit | Limited beyond Windows UI. | Aligns with headless deployment roadmap. | Enables tele-assist scenarios. |
| Ecosystem Alignment | Windows-first approach. | Works with Linux Core investments. | Requires mature remote client stack. |

---

## 61.13 Evaluation & Verification

Benchmark metrics track boot-to-ready times, automation arbitration latency, and
PoseStream frame sizes. Test procedures include replaying deterministic fixtures,
performing kiosk boot simulations, and validating remote control leases.

---

## 61.14 Implementation Policy

Implementations must expose PoseStream services with documented schema evolution
rules, register SectionState diff handlers with constraint gates, and publish kiosk
launcher scripts alongside watchdog configurations for supported platforms.

---

## 61.15 Community Sentiment

Contributors request bundled kiosk scripts and watchdogs, consolidated control
arbitration inside Core, and constraint gates aligned with automation safety.

### 61.15.1 Section Change Log

| Date | Summary | PR / Issue |
|------|---------|------------|
| 2025-10-20 | Initial rewrite using v0.1 template. | #0000 |

---

## 61.16 Traceability

| Requirement ID | Related Option(s) | ADR(s) | Verification Artifact | Implementation Reference |
|----------------|-------------------|--------|-----------------------|--------------------------|
| R-MM-000 | 61-O1 | 61-ADR-007 | `tests/ui/MultiMonitorLayout.cs` | `SourceCode/GPS/Helpers/ScreenHelper.cs` |
| R-CTRL-000 | 61-O2 | 61-ADR-015 | `sim/control/SectionArbiterScenario.json` | `Core/ControlArbiter` |
| R-CTRL-006 | 61-O2, 61-O3 | 61-ADR-027 | `tests/replay/ConstraintGateFixture.jsonl` | `Core/ConstraintGate` |

---

## 61.17 Conformance

An implementation conforms to this section when all **MUST** requirements are verified,
all **SHOULD** requirements are satisfied or explicitly waived, and no **MUST NOT**
requirements are violated. Deterministic pose fusion, control arbitration, and kiosk
resilience must be demonstrated through mapped verification artifacts.

---

## Standards Context

This section aligns with ISO/IEC/IEEE 29148:2018 for requirements specifications and
IEEE 1016:2017 for design descriptions while emphasizing deterministic automation and
safety governance.
