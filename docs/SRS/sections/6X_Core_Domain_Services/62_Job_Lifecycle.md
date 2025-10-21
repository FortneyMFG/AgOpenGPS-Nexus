# 62 — Job Lifecycle & Session Management
*(Status: Proposed)*

**Author:** Codex
**Created:** 2025-10-20
**Status:** Proposed
**Version:** 0.1.0
**Section ID:** 62
**Editors:** @Codex
**Last Updated:** 2025-10-20
**Related Sections:** 61 Kinematics & Pose Fusion, 63 Layer Registry, 72 Mapping Layers Plugin
**Upstream Dependencies:** 31-ADR-043 Multi-Field Envelopes, 62-ADR-041 Job Sessions Lifecycle
**Downstream Impacts:** TaskService, Telemetry Logging, Analytics Pipelines

---

## 62.1 Purpose & Scope

This section defines lifecycle orchestration for farms, seasons, jobs, and sessions.
It governs the state machine, events, journaling cadence, multi-field mounting,
resume flows, and work-order integration so plugins, analytics, and automation retain
consistent provenance and auditability.【F:docs/SRS/sections/6X_Core_Domain_Services/62_Job_Lifecycle.md†L4-L96】

---

## 62.2 Context

- Depends on Core services exposing farm/season/job/session contexts with immutable IDs.
- Interacts with mapping layers, telemetry logging, TaskService work orders, and
  weather ingestion pipelines.
- Out of scope: UI layout specifics or analytics algorithms that consume session data.

---

## 62.3 Legacy Comparison

| Area / Theme | Legacy Behavior | Identified Limitation | Modernization Opportunity | Reference / Source |
|--------------|-----------------|-----------------------|---------------------------|--------------------|
| Lifecycle Control | Manual job/session toggles with limited provenance. | Resume flows rely on ad hoc caches and can lose context. | Formalize mount → session start ordering with deterministic events. | 【F:docs/SRS/sections/6X_Core_Domain_Services/62_Job_Lifecycle.md†L8-L61】 |
| Journaling | Autosave triggered by operator actions. | Crash recovery may replay incomplete batches inconsistently. | Define autosave cadence, journaling checkpoints, and deterministic recovery. | 【F:docs/SRS/sections/6X_Core_Domain_Services/62_Job_Lifecycle.md†L63-L74】 |
| Multi-field Support | Single-field focus with manual mounts. | Multi-field jobs require duplicated work. | Support mount/unmount APIs and provenance for multi-field workflows. | 【F:docs/SRS/sections/6X_Core_Domain_Services/62_Job_Lifecycle.md†L76-L88】 |

---

## 62.4 Definitions

| Term | Definition |
|------|------------|
| Session | Time-bounded execution period within a job capturing telemetry and provenance. |
| Autosave | Periodic persistence of session state, coverage, and metadata triggered by cadence rules. |
| Work Order | TaskService entity representing planned work with presets, implements, and checklists. |
| Context Diff | Snapshot delta emitted when farm/season/job/session identifiers or metadata change. |

---

## 62.5 Requirements

| ID | Priority | Category | Summary | Source / C-IDs | Key Metrics / Verification |
|----|----------|----------|---------|-----------------|----------------------------|
| R-JOB-000 | MUST | Lifecycle | Maintain deterministic lifecycle states (Planned, Mounted, Active, Paused, Closed, Completed). | C1 | State-machine tests verifying transitions and event ordering. |
| R-JOB-001 | MUST | Event Bus | Emit ordered lifecycle events (`onFarmLoaded`…`onSessionEnd`) with authoritative context payloads. | C1 | Integration test harness replaying mount/resume flows. |
| R-JOB-002 | MUST | Journaling | Autosave at ≤60 s cadence or after 5 MB coverage delta with crash-safe persistence. | C2 | Fault-injection test verifying recovery replays complete batches. |
| R-JOB-003 | SHOULD | Provenance | Append provenance metadata (jobId, sessionId, actor, timestamps) to layer and session documents. | C3 | Schema validation ensuring provenance fields present. |
| R-JOB-004 | SHOULD | Multi-field | Support batch mount/unmount of multiple fields with deterministic context diffs. | C4 | API contract test verifying union envelopes and diff notifications. |
| R-JOB-005 | SHOULD | Resume | Provide "Resume Last Session" workflow restoring context and pending journals. | C5 | UX regression verifying resume replays pending batches. |
| R-JOB-006 | SHOULD | UX Flow | Offer season-first and farm-first navigation while maintaining immutable identifiers. | C6 | UI integration ensuring ID continuity across flows. |
| R-JOB-040 | MUST | Work Orders | Launching a work order auto-opens a session with provenance for presets, implements, and assignees. | C7 | TaskService integration test verifying provenance fields recorded. |
| R-JOB-041 | SHOULD | Companion Sync | Mobile clients sync checklists/notes into `Session.notes[]` with actor/timestamp. | C7 | Sync test ensuring bidirectional updates. |
| R-JOB-042 | MUST | Task Events | Emit task state transitions (Assigned → In Progress → Completed/Cancelled) with telemetry payloads. | C7 | Event stream verification with Profit & Telemetry subscribers. |
| R-JOB-043 | SHOULD | Inventory Reconciliation | Reconcile work-order material reservations with Inventory Ledger updates. | C7 | Ledger test ensuring quantity deltas captured. |

### 62.5.1 Requirement Sources & Rationale

| Req ID | Source | Rationale |
|--------|--------|-----------|
| R-JOB-000 | Session lifecycle working group | Provides deterministic workflow for plugins. |
| R-JOB-002 | Autosave governance notes | Prevents data loss and ensures crash recovery. |
| R-JOB-040 | TaskService charter | Aligns field execution with assigned work orders. |
| R-JOB-042 | Profit & telemetry integration plan | Enables downstream analytics without polling. |

---

## 62.6 Acceptance Criteria & Verification

Lifecycle validation relies on integration suites that replay mount/resume sequences,
fault-injection tests validating autosave durability, and API contract tests covering
multi-field mounts, work-order launches, and provenance sync. Manual review confirms
UX flows preserve immutable IDs while automated checks audit emitted event payloads.

### 62.6.1 Requirement-to-Verification Map

| Req ID | Verification Type | Artifact / Location | Pass/Fail Threshold |
|--------|-------------------|---------------------|---------------------|
| R-JOB-000 | Simulation | `tests/sim/JobLifecycleStateMachine.json` | All transitions exercised without invalid states. |
| R-JOB-002 | Fault Injection | `tests/replay/AutosaveCrashRecovery.jsonl` | Recovery restores last committed autosave. |
| R-JOB-040 | Integration | `tests/integration/TaskServiceWorkOrder.cs` | Session metadata includes workOrderId + preset hash. |
| R-JOB-043 | Ledger Test | `tests/integration/InventoryReconciliation.cs` | Ledger quantities adjusted within ±1% tolerance. |

---

## 62.7 Constraints

- Must maintain immutable IDs for farm, season, job, and session entities once issued.
- Must respect privacy policies for notes and telemetry when syncing across devices.
- Must operate offline-first, syncing when connectivity is restored without data loss.

---

## 62.8 Stakeholder Expectations

Operators expect intuitive resume flows and clear session timelines, managers require
work-order orchestration with audit trails, and analytics teams rely on deterministic
journal playback to compute coverage, profit, and compliance metrics.

---

## 62.9 Design Considerations

| ID | Consideration | Description |
|----|---------------|-------------|
| C1 | Ordered lifecycle events | Plugins and UI shells depend on deterministic event sequencing across mount and resume scenarios. |
| C2 | Crash-safe journaling | Autosave cadences and atomic writes ensure recovery without user intervention. |
| C3 | Provenance completeness | Session and layer records require actor, timestamp, and source metadata for replay and audits. |
| C4 | Multi-field orchestration | Batch mounting fields must compute union envelopes while retaining per-field statistics. |
| C5 | Resume fidelity | Pending journals and cached context need restoration without duplicate side effects. |
| C6 | Navigation flexibility | Season-first and farm-first flows must reach the same immutable entities. |
| C7 | Work-order alignment | TaskService integration must remain authoritative for presets, checklists, and inventory. |
| C8 | Weather and telemetry hooks | Weather auto-logging and telemetry subscriptions must update session context consistently. |

### 62.9.1 Assumptions & Preconditions

- [A1] Autosave storage is available locally even in offline deployments.
- [A2] Operators authenticate before modifying session metadata or work orders.
- [A3] Weather providers deliver updates at ≥15 min cadence when configured.

---

## 62.10 Option Overview

| Option ID | Status | Type / Theme | Description | Reference Document |
|-----------|--------|--------------|-------------|--------------------|
| 62-O1 | Proposed | Resume Flow | Persist UI state for "Resume Last Session" shortcut. | — |
| 62-O2 | In Review | Offline Sync | Deferred journal upload with conflict resolution. | 32 Persistence Formats |
| 62-O3 | Exploring | Work-order Guided Flow | TaskService-driven job picker with presets. | 62-ADR-041 |

---

## 62.11 Comparison Matrix

| Attribute / Criteria | 62-O1 | 62-O2 | 62-O3 |
|----------------------|-------|-------|-------|
| Operator Effort | Low | Medium | Medium |
| Implementation Complexity | Low | Medium | High |
| Offline Resilience | Medium | High | Medium |
| Work-order Adoption | Medium | Medium | High |

---

## 62.12 Option Evaluation

Preliminary evaluation favors combining offline sync (62-O2) with work-order guided
flows (62-O3) to satisfy TaskService alignment while maintaining resume convenience.
Detailed scoring is deferred to ADR updates.

---

## 62.13 Evaluation & Verification

Verification benchmarks track autosave latency, resume time-to-ready, and work-order
latency from assignment to session creation. Regression suites replay historical jobs
and confirm provenance completeness.

---

## 62.14 Implementation Policy

Core must expose lifecycle APIs with versioned payloads, persist session documents
atomically, and document TaskService integration contracts. Client shells must respect
immutable IDs and use provided journaling hooks.

---

## 62.15 Community Sentiment

Contributors emphasize deterministic resume flows, richer work-order orchestration,
and audit-ready provenance for collaborative crews.

### 62.15.1 Section Change Log

| Date | Summary | PR / Issue |
|------|---------|------------|
| 2025-10-20 | Initial rewrite using v0.1 template. | #0000 |

---

## 62.16 Traceability

| Requirement ID | Related Option(s) | ADR(s) | Verification Artifact | Implementation Reference |
|----------------|-------------------|--------|-----------------------|--------------------------|
| R-JOB-000 | 62-O1 | 62-ADR-041 | `tests/sim/JobLifecycleStateMachine.json` | `Core/JobLifecycleService` |
| R-JOB-002 | 62-O2 | 62-ADR-030 | `tests/replay/AutosaveCrashRecovery.jsonl` | `Core/Journaling` |
| R-JOB-040 | 62-O3 | 62-ADR-041 | `tests/integration/TaskServiceWorkOrder.cs` | `Services/TaskServiceClient` |

---

## 62.17 Conformance

Implementations conform when lifecycle events follow the defined state machine,
autosave policies meet cadence and durability requirements, and work-order provenance
is preserved without violating privacy or offline constraints.

---

## Standards Context

This section aligns with ISO/IEC/IEEE 29148:2018 and IEEE 1016:2017 guidance for
requirements and design descriptions, emphasizing determinism, auditability, and
cross-service orchestration.
