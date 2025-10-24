# 83 — Autosteer Target Models
*(Status: Proposed)*

**Author:** Codex
**Created:** 2025-10-20
**Version:** 0.1.0
**Section ID:** 83
**Editors:** Autonomy Working Group
**Last Updated:** 2025-10-20
**Related Sections:** 81 — Guidance Orchestrator, 82 — Planning, ADR-033
**Upstream Dependencies:** PoseStream, Equipment Profiles, Sections plugin
**Downstream Impacts:** Autosteer controllers, UI overlays, Telemetry dashboards

---

## 83.1 Purpose & Scope

Standardize how Nexus converts planner output (desired path, orientation, and speed targets) into real-time steering commands.
This section defines controller requirements, evaluation metrics, and documentation expectations so diverse algorithms remain interoperable across vehicles and implements.

---

## 83.2 Context

- Guidance Orchestrator publishes catalogs and `SteerTargets` that controllers must track reliably at 25 Hz.
- Operators expect algorithm neutrality with transparent tuning, persistence, and telemetry for debugging.
- Legacy controllers (Pure Pursuit, Stanley) coexist with advanced approaches (MPC) requiring shared interfaces.
- Hardware variations (articulated rigs, sensor feedback) demand extensible models for bias, rate limits, and latency compensation.

---

## 83.3 Legacy Comparison

| Controller | Legacy Behavior | Limitation | Modernization Opportunity | Source |
|------------|-----------------|------------|---------------------------|--------|
| Pure Pursuit | Deterministic geometric look-ahead. | Limited delay compensation; minimal implement modeling. | Retain as baseline with documented latency expectations. | Autosteer archives |
| Stanley | Combines heading and cross-track error. | Partial handling of rate/angle limits; limited row sensor fusion. | Extend with configurable gains and sensor inputs. | Nexus control notes |
| MPC | Prototype only. | High compute load; complex tuning. | Evaluate for advanced rigs with documented resource budgets. | MPC pilot logs |

---

## 83.4 Definitions

| Term | Definition |
|------|-------------|
| `SteerTargets` | Time-series commands containing preview point, curvature, optional speed cap, and row bias. |
| Controller | Algorithm translating planner references into actuator commands or steerable targets. |
| Delay Compensation | Feed-forward or predictive methods that offset command latency. |
| Equivalence Window | Tolerance range for curvature and heading used to judge controller stability across swaths. |

---

> **Requirement Grammar (RFC-2119):**
> - **MUST / MUST NOT** = mandatory controller expectations.
> - **SHOULD / SHOULD NOT** = preferred behaviors with waiver process.
> - **MAY / COULD** = optional enhancements or roadmap items.

## 83.5 Requirements

| ID | Priority | Category | Summary | Verification |
|----|----------|----------|---------|--------------|
| R-AUTO-000 | MUST | Capability | Provide at least one conformant autosteer target generator via a stable interface; algorithm choice not prescribed. | Interface conformance tests. |
| R-AUTO-001 | MUST | Publication | Emit `SteerTargets` at 25 Hz ±5 ms jitter; honor optional `speed_cap_mps` and `row_bias_m`. | Telemetry jitter analysis. |
| R-AUTO-002 | MUST | Pose Inputs | Support fused pose streams ≥ 50 Hz; integrate row/implement sensors when present; reject inputs older than 100 ms. | Pose freshness validation. |
| R-AUTO-003 | SHOULD | Delay Compensation | Publish controller latency metadata and compensate using feed-forward or predictive methods. | Closed-loop latency tests. |
| R-AUTO-004 | SHOULD | Tuning & Persistence | Persist controller gains per equipment profile; expose tunables through Autosteer UI (ADR-033). | UI round-trip tests. |
| R-AUTO-005 | MUST | Verification | Validate controller performance in simulation + field regression; log cross-track, heading, and latency metrics. | QA evidence review. |
| R-AUTO-006 | SHOULD | Implement Geometry | Support articulated and multi-implement rigs via configurable geometry parameters. | HIL geometry tests. |
| R-AUTO-007 | COULD | Adaptive Bias | Accept optional `row_bias_m` decay parameters for sensor fusion experiments. | Experimental controller evaluation. |
| R-AUTO-008 | MUST | Configuration | Allow operators to select compliant controllers via runtime configuration profiles. | Configuration profile integration tests. |
| R-AUTO-009 | MUST | Default Behavior | Provide a default profile that selects a conformant controller with documented performance envelope. | Default profile validation checklist. |
| R-AUTO-010 | SHOULD | Extensibility | Permit additional controllers delivered as plugins when they satisfy interface and performance requirements. | Plugin controller qualification tests. |
| R-AUTO-011 | MUST | API Stability | Maintain backward-compatible autosteer target contracts across releases. | Contract diff analysis. |

---

## 83.6 Controller Comparison

| Controller | Delay Compensation | Rate / Angle Limits | Implement Geometry | Row Feeler Support | Typical Rate | Compute Load |
|------------|--------------------|--------------------|--------------------|--------------------|--------------|--------------|
| Pure Pursuit | No | No | No | External add-on | 10–50 Hz | Very Low |
| Stanley | Partial | Partial | No | Partial (pose fusion only) | 25–50 Hz | Low |
| MPC | Yes | Yes | Yes | Native (direct fusion) | 50–100 Hz | Moderate |

---

## 83.7 Decision Matrix Template

| Criterion | Weight | Pure Pursuit | Stanley | MPC | Custom |
|-----------|--------|---------------|---------|-----|--------|
| Implementation Complexity | 0.1 | 5 | 4 | 2 | — |
| Compute Efficiency | 0.2 | 5 | 4 | 3 | — |
| Delay / Latency Handling | 0.2 | 2 | 3 | 5 | — |
| Implement Geometry Support | 0.2 | 1 | 2 | 5 | — |
| Field Provenance / Stability | 0.2 | 5 | 4 | 3 | — |
| Tuning Transparency | 0.1 | 5 | 4 | 3 | — |
| **Weighted Total** | 1.0 | **x.xx** | **x.xx** | **x.xx** | **x.xx** |

---

## 83.8 Evaluation & Verification

**Performance Benchmarks**
- Target jitter ≤ 5 ms @ 25 Hz.
- Cross-track error ≤ 0.2 m; heading error ≤ 1° (95%).
- Latency < 50 ms end-to-end.
- Seamless fallback transitions under planner swap.

**Test Procedure Summary**
1. Run closed-loop simulation with recorded field data for each controller option.
2. Verify pose fusion latency and timestamp integrity.
3. Execute regression across representative field geometries.
4. Log telemetry for jitter, error, and stability metrics.

**Acceptance Criteria**
- All mandatory requirements (R-AUTO-000 → 007) validated by QA tests.
- Controller selection and fallback decisions recorded in telemetry.
- Operator-visible tuning persists between sessions.

---

## 83.9 Design Considerations

| ID | Consideration | Description |
|----|----------------|-------------|
| C1 | Pure Pursuit baseline | Maintain deterministic, low-compute controller as compatibility anchor. |
| C2 | Stanley enhancements | Extend Stanley-style feedback with configurable delay compensation and rate limits. |
| C3 | MPC investment | Evaluate predictive controllers for articulated rigs with documented CPU/GPU budgets. |
| C4 | Custom / experimental slot | Reserve interface-compliant hooks for third-party or research controllers. |
| C5 | Decision governance | Use weighted decision matrix to document rationale before approving controller changes. |

### 83.9.1 Community Sentiment

- Contributors support algorithm neutrality focused on measurable behavior.
- Shared regression datasets and telemetry dashboards are critical for tuning transparency.
- Plugin architecture should simplify experimentation while safeguarding operator trust.

---

## 83.10 Implementation Policy

*(Reserved — controller selection mechanics are detailed in guidance ADRs.)*

---

## 83.11 Open Questions

- Should MPC controllers expose adaptive horizon length for varying implement response?
- What safety interlocks are required when swapping controllers mid-field?
- How should telemetry sampling scale for long-duration jobs without overwhelming storage?

---

## 83.12 References

- [ADR-033 — Guidance planner and autosteer orchestration](81-ADR-033%20-%20Guidance%20planner%20and%20autosteer%20orchestration.md)
- [ADR-069 — Guidance Orchestrator plugin](81-ADR-069%20-%20Guidance%20Orchestrator%20plugin.md)
- [Guidance lane contracts how-to](../howto/guidance-lane-contracts.md)
