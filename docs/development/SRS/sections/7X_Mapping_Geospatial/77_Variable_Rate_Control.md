# 77 — Variable Rate Control
*(Status: Proposed)*

**Authors:** Nexus Team (Codex)
**Created:** 2025-10-24
**Version:** 0.1.0
**Section ID:** 77
**Editors:** Agronomy & Automation Working Group
**Last Updated:** 2025-10-24
**Related Sections:** 51 — Sensor & Actuator Abstractions, 52 — AgIO Service, 61 — Kinematics & Pose Fusion, 73 — Variable Mapping, 81 — Guidance Orchestrator, 96 — Quality Engineering & Release
**Upstream Dependencies:** ADR-021, ADR-030, ADR-047, ADR-068, Layer Registry metadata (ADR-010)
**Downstream Impacts:** Rate controllers, section control, analytics & reporting pipelines, safety certification

---

## 77.1 Purpose & Scope

Define the closed-loop control architecture that transforms mapped agronomic intent into safe, bounded hardware commands. The section covers controller pipelines, safety interlocks, telemetry expectations, and hardware handoff so Nexus can execute prescriptions deterministically across implements and simulators.【F:docs/sections/6X_Core_Domain_Services/61_Kinematics_Pose_Fusion.md†L86-L156】【F:docs/sections/5X_Hardware_IO_Device_Layer/51_Sensor_Actuator_Abstractions.md†L16-L92】

---

## 77.2 Context

- Controllers rely on normalized rate layers produced in §73 along with pose fusion speed/heading for lookahead and compensation.
- AgIO (§52) and hardware abstraction layers (§51) provide deterministic transport for rate commands and feedback.
- Guidance orchestration (§81) enforces keep-outs, headlands, and manual overrides that controller loops must honor.
- Replay harnesses in §97 validate controller behavior under deterministic simulation, providing release gates for §96.

---

## 77.3 Definitions

| Term | Definition |
|------|------------|
| Closed-Loop Control | Feedback system comparing commanded vs. actual rates to adjust outputs automatically. |
| Controller Hint | Metadata guiding ramp rates, smoothing windows, or nozzle timing sourced from §73. |
| Safety Interlock | Hardware or software guard preventing commands when prerequisites fail (pressure, flow, arming state). |
| Section/Nozzle Controller | Control loop instance producing rate commands for a bounded physical channel. |
| Device Handoff | Transition where controller outputs transfer to hardware transports managed by §51/§52. |

---

> **Requirement Grammar (RFC-2119):**
> - **MUST / MUST NOT** = mandatory, testable requirement.
> - **SHOULD / SHOULD NOT** = strong recommendation; document deviations.
> - **MAY** = optional extension gated by telemetry + ADR sign-off.

## 77.4 Requirements

| ID | Priority | Category | Summary | Source / C-IDs | Key Metrics / Verification |
|----|-----------|----------|---------|-----------------|-----------------------------|
| R-VRC-7700 | MUST | Controller Architecture | Controllers MUST execute per section/nozzle with configurable smoothing and lag compensation before commanding hardware. | ADR-021 | Hardware-in-loop (HIL) tests verifying stability. |
| R-VRC-7701 | MUST | Safety | Controllers MUST honor headlands, keep-outs, machine states, and manual overrides, degrading to manual rates when confidence drops below threshold. | Guidance Orchestrator SRS | Simulation scenarios verifying safe fallback. |
| R-VRC-7702 | MUST | Device Handoff | Rate commands MUST transition through §51 abstractions and §52 AgIO transports with bounded latency budgets documented in §23. | Hardware IO charter | Bench tests confirm latency ≤ 50 ms end-to-end. |
| R-VRC-7703 | SHOULD | Telemetry | Systems SHOULD emit commanded vs. actual rate, error bands, calibration events, and diagnostic metrics into telemetry streams. | Telemetry SRS | Telemetry regression harness measuring data completeness. |
| R-VRC-7704 | SHOULD | Calibration | Controllers SHOULD surface calibration workflows with operator acknowledgement before applying derived offsets. | Automation WG notes | QA suite validates calibration prompts and audit logging. |
| R-VRC-7705 | MUST | Simulation Fidelity | Controllers MUST support deterministic replay via §97 Simulation & Replay fixtures using SimClock and seeded RNG. | ADR-004 | Replay harness compares commanded vs. actual within tolerance. |

### 77.4.1 Controller Pipeline Overview

1. **Input Normalization:** Pose fusion supplies speed/heading; §73 delivers target rates and controller hints.
2. **Rate Selection:** Controllers apply agronomic models and lookahead to calculate target rates per section/nozzle.
3. **Safety Evaluation:** Keep-outs, headlands, manual overrides, and machine state gating ensure only safe commands proceed.
4. **Command Dispatch:** Hardware adapters receive bounded rate commands via §51/§52; smoothing windows prevent oscillations.
5. **Feedback Capture:** Telemetry logs actual rates, errors, and calibration adjustments for replay and analytics.

---

## 77.5 Acceptance Criteria & Verification

- HIL tests validate closed-loop control stability across speed changes, headland entries, and manual overrides.
- Simulation suites in §97 replay historic prescriptions and confirm telemetry captures command vs. actual deviations within tolerance.
- Device latency tests verify rate commands reach AgIO transports within defined budgets.
- Calibration workflows undergo QA to ensure operator acknowledgements are recorded in §63 journals.

### 77.5.1 Requirement-to-Verification Map

| Req ID | Verification Type | Artifact / Location | Pass/Fail Threshold |
|--------|--------------------|---------------------|---------------------|
| R-VRC-7700 | HIL test | `tests/hil/vr_controller_closed_loop.md` | Overshoot ≤ 5%, settling time ≤ 3 s. |
| R-VRC-7701 | Simulation scenario | `sim/scenarios/headland_shutdown.json` | Controller enters safe state within 250 ms of trigger. |
| R-VRC-7702 | Latency bench | `tests/hil/rate_command_latency.md` | End-to-end latency ≤ 50 ms (p95). |
| R-VRC-7703 | Telemetry regression | `tests/telemetry/vr_metrics.cs` | Metrics emitted for ≥ 99% of application intervals. |
| R-VRC-7704 | QA checklist | `qa/playbooks/rate_calibration.md` | All calibration steps require acknowledgement. |
| R-VRC-7705 | Replay harness | `tests/sim/replay_variable_rate.cs` | Commanded vs. actual delta ≤ 2%. |

---

## 77.6 Constraints

- Controllers must operate on embedded hardware with deterministic latency (≤ 50 ms cycle time) and constrained CPU budgets.
- Safety interlocks cannot be bypassed by plugins; overrides require §95 Security permissions.
- Hardware transports vary (hydraulic, electric, pneumatic) and must remain abstracted through §51 adapters.

### 77.6.1 Non-Functional Requirement Classes

- **Performance:** Control loop latency, overshoot, settling times.
- **Reliability:** Fail-safe degradation, telemetry completeness, persistent calibration logs.
- **Safety:** Headland and keep-out enforcement, manual override priority.
- **Operability:** Diagnostic dashboards, calibration workflows, replay support.
- **Maintainability:** Configurable controller parameters, schema-governed metadata from §73.

---

## 77.7 Risks & Open Issues

| ID | Description | Impact | Mitigation / Status | Owner |
|----|-------------|--------|---------------------|-------|
| RISK-77-1 | Closed-loop control unstable on low-bandwidth hydraulic systems. | High | Tune smoothing + PID coefficients per implement class; gather HIL data. | @automation |
| RISK-77-2 | Transport latency spikes cause controller oscillations. | Medium | Instrument §52 transports; raise alarms through §64 telemetry. | @platform |
| ISSUE-77-1 | Determine API for AI-assisted rate recommendations interacting with controller hints. | Medium | Pending ADR; evaluate plugin contracts. | @agronomy |

---

## 77.8 Design Considerations

| ID | Consideration | Description |
|----|----------------|-------------|
| C1 | Safety First | Controllers prioritize safe degradation over aggressive optimization. |
| C2 | Deterministic Replay | Replay harnesses and §97 fixtures validate reproducibility before release. |
| C3 | Hardware Diversity | Controllers must tune to hydraulic, electric, and pneumatic systems with consistent abstractions. |
| C4 | Telemetry Insight | Rich telemetry enables calibration, diagnostics, and compliance reporting. |
| C5 | Plugin Extensibility | Rate calculators must integrate without bypassing guardrails or duplicating pipelines. |

### 77.8.1 Assumptions & Preconditions

- [A1] Mapping plugins deliver normalized union envelopes and keep-out geometries in real time.
- [A2] Telemetry mesh remains available with sufficient bandwidth for command/feedback metrics.
- [A3] Operators calibrate implements before running variable-rate scenarios.

---

## 77.9 Option Overview

| Option ID | Status | Type / Theme | Description | Reference Document |
|-----------|--------|--------------|-------------|--------------------|
| — | — | — | All trade-offs incorporated as design considerations in §77.8. | — |

---

## Section Change Log

| Date | Summary | Author | PR / Issue |
|------|---------|--------|------------|
| 2025-10-24 | Initial draft | Nexus Team (Codex) |  |

