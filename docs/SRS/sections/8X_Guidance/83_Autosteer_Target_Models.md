# 83 — Autosteer Target Models (Status: drafting)

## 83.1 Purpose
Define how Nexus converts planner output (desired path, orientation, and speed targets) into real-time steering commands.  This section standardizes controller requirements, compares available algorithms, and outlines evaluation metrics to ensure consistent performance across vehicles and implements.

---

## 83.2 Controller Requirements
| ID | Priority | Category | Summary | Key Metrics |
|----|-----------|-----------|----------|--------------|
| R-AUTO-000 | MUST | Capability | Provide **at least one conformant autosteer target generator** via a stable interface. The specific algorithm is **not prescribed** by this SRS. | Conformance tests pass with any selected controller. |
| R-AUTO-001 | MUST | Publication | Emit `SteerTargets` at **25 Hz ±5 ms jitter**, honoring optional inputs `speed_cap_mps` and `row_bias_m`. | Measured target jitter ≤ 5 ms. |
| R-AUTO-002 | MUST | Pose Inputs | Support fused pose streams ≥ 50 Hz; integrate row/implement sensors when present; reject inputs older than 100 ms. | Pose freshness monitored in telemetry. |
| R-AUTO-003 | SHOULD | Delay Compensation | Provide per-controller latency and steer-rate compensation using feed‑forward or predictive methods. | Latency ≤ 50 ms at 95th percentile. |
| R-AUTO-004 | SHOULD | Tuning & Persistence | Persist controller gains per equipment profile and expose parameters through the Autosteer UI per ADR‑033. | Tunables round‑trip verified. |
| R-AUTO-005 | MUST | Verification | Validate controller performance in simulation and field regression tests; log cross-track, heading, and latency metrics. | QA metrics within specified tolerances. |

---

## 83.3 Controller Options Overview
This subsection is **informative** and enumerates candidate algorithms that may satisfy the above requirements. The SRS does not prescribe a specific control law; conformant controllers SHALL meet interface and performance requirements regardless of implementation method.

| Option ID | Name | Type | Description | Reference Document |
|-----------|------|------|-------------|--------------------|
| **83‑O1** | **Pure Pursuit** | Geometric | Uses look‑ahead geometry to compute curvature toward a forward point along the path. Simple, deterministic baseline. | 83‑O1_PurePursuit.md |
| **83‑O2** | **Stanley** | Feedback | Combines heading and cross‑track errors with a proportional term scaled by speed for stable curvature tracking. | 83‑O2_Stanley.md |
| **83‑O3** | **MPC (Model Predictive Control)** | Predictive / Optimization | Predicts vehicle and implement states ahead in time to minimize total error and control effort with constraints. | 83‑O3_MPC.md |
| **83‑O4** | **Custom / Experimental** | Interface‑compliant | Placeholder for any compatible algorithm meeting performance criteria. | — |

**Document naming convention:** Option specs live alongside this file using the pattern `83‑OX_<Name>.md` (e.g., `83‑O1_PurePursuit.md`). When an option document does not exist yet, leave the **Reference Document** cell as `—`.

---

## 83.4 Comparison Matrix
| Controller | Delay Compensation | Rate / Angle Limits | Implement Geometry | Row Feeler Support | Typical Rate | Compute Load |
|-------------|--------------------|--------------------|--------------------|--------------------|---------------|---------------|
| **Pure Pursuit** | No | No | No | External add‑on | 10–50 Hz | Very Low |
| **Stanley** | Partial | Partial | No | Partial (pose fusion only) | 25–50 Hz | Low |
| **MPC** | Yes | Yes | Yes | Native (direct fusion) | 50–100 Hz | Moderate |

---

## 83.5 Decision Matrix (Template)
The following decision matrix template helps document rationale when selecting or approving controller options. Each candidate is scored against key evaluation criteria; higher totals indicate stronger suitability. Scores are 1 (low) – 5 (high).

| Criterion | Weight | Pure Pursuit | Stanley | MPC | Custom |
|------------|--------|---------------|----------|------|---------|
| Implementation Complexity | 0.1 | 5 | 4 | 2 | — |
| Compute Efficiency | 0.2 | 5 | 4 | 3 | — |
| Delay / Latency Handling | 0.2 | 2 | 3 | 5 | — |
| Implement Geometry Support | 0.2 | 1 | 2 | 5 | — |
| Field Provenance / Stability | 0.2 | 5 | 4 | 3 | — |
| Tuning Transparency | 0.1 | 5 | 4 | 3 | — |
| **Weighted Total** | 1.0 | **x.xx** | **x.xx** | **x.xx** | **x.xx** |

---

## 83.6 Evaluation & Verification
**Performance Benchmarks**
- Target jitter ≤ 5 ms @ 25 Hz.
- Cross‑track error ≤ 0.2 m; heading error ≤ 1° (95 %).
- Latency < 50 ms end‑to‑end.
- Seamless fallback transitions under planner swap.

**Test Procedure Summary**
1. Run closed‑loop simulation with recorded field data for each controller option.
2. Verify pose fusion latency and timestamp integrity.
3. Execute regression across representative field geometries.
4. Log telemetry for jitter, error, and stability metrics.

**Acceptance Criteria**
- All mandatory requirements (R‑AUTO‑000 → 005) validated by QA tests.
- Controller selection and fallback decisions recorded in telemetry.
- Operator‑visible tuning persisted between sessions.

---

## 83.7 Implementation Policy
- System SHALL allow runtime controller selection via configuration profile.
- A **default profile** SHALL be provided that selects a conformant controller; algorithm choice is **implementation-specific**.
- Additional controllers MAY be distributed as plugins provided they meet all interface and performance requirements.
- Controller interfaces SHALL remain backward-compatible with existing `SteerTarget` messages.

---

## 83.8 Community Sentiment
Recent contributor discussions highlight consensus around maintaining an open, flexible control architecture rather than prescribing a specific algorithm. Developers and operators value predictable performance, transparent tuning, and compatibility with legacy Pure Pursuit controllers while leaving space for modern approaches like MPC. The community favors:

- **Algorithm neutrality** — focus on measurable behavior, not implementation.
- **Shared test data** — open regression datasets for controller verification.
- **Plugin extensibility** — easy path for experimentation and third-party controllers.
- **Field feedback loops** — telemetry and QA dashboards to inform tuning defaults.

Overall sentiment supports a standards-driven interface with room for innovation, ensuring interoperability across hardware and controller generations.
