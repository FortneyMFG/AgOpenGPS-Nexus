# 97 — Simulation & Replay
*(Status: Proposed)*

**Author:** Codex
**Created:** 2025-10-24
**Version:** 0.1.0
**Section ID:** 97
**Editors:** Simulation & Quality Working Group
**Last Updated:** 2025-10-24
**Related Sections:** 23 — Threading, Scheduling & Timing, 41 — Service APIs & Contracts, 63 — Layers Registry & Journal Contracts, 77 — Variable Rate Control, 96 — Quality Engineering & Release
**Upstream Dependencies:** ADR-004, ADR-021, ADR-068, Layer Registry metadata, Job lifecycle services
**Downstream Impacts:** CI replay gates, plugin certification, telemetry analytics, hardware-in-loop workflows

---

## 97.1 Purpose & Scope

Define deterministic simulation and replay services that allow Nexus to capture, timewarp, and re-run agronomic sessions for QA, development, and operator training. The section governs SimClock/SimBus expectations, record/replay fidelity, synthetic sensor providers, and golden run management used by §96 release gates.【F:docs/sections/2X_System_Architecture/21-ADR-004 - Establish the composite simulation fabric (SimClock + SimBus).md†L1-L68】

---

## 97.2 Context

- Composite simulation fabric (ADR-004) unifies SimClock and SimBus to orchestrate deterministic event delivery across plugins and services.
- Journals (§63) and contracts (§41) capture telemetry, commands, and layer updates required for faithful replay.
- Guidance and control loops (§77, §81) rely on simulation fixtures to validate safety interlocks before field deployment.
- Packaging workflows (§94) must bundle simulation providers and golden datasets for offline QA and training.

---

## 97.3 Definitions

| Term | Definition |
|------|------------|
| SimClock | Deterministic clock controlling simulation time progression and time dilation. |
| SimBus | Publish/subscribe fabric that delivers simulation topics with ordering guarantees tied to SimClock. |
| Golden Run | Canonical recording used to verify regression behavior across releases. |
| Synthetic Sensor | Provider that emits simulated hardware signals (GPS, flow, IMU) for closed-loop testing. |
| Timewarp | Capability to accelerate, pause, or rewind simulation timelines while maintaining determinism. |

---

> **Requirement Grammar (RFC-2119):**
> - **MUST / MUST NOT** = mandatory, testable requirement.
> - **SHOULD / SHOULD NOT** = strong recommendation; document deviations.
> - **MAY** = optional extension gated by telemetry + ADR sign-off.

## 97.4 Requirements

| ID | Priority | Category | Summary | Source / C-IDs | Key Metrics / Verification |
|----|-----------|----------|---------|-----------------|-----------------------------|
| R-SIM-9700 | MUST | Determinism | SimClock and SimBus MUST deliver events deterministically given identical inputs and seeds. | ADR-004 | Replay harness compares event ordering hashes. |
| R-SIM-9701 | MUST | Record/Replay | Simulation stack MUST capture telemetry, commands, and layer updates via §63 journals and expose replay APIs for §96 gates. | Journal charter | Golden run suite diff checks outputs. |
| R-SIM-9702 | SHOULD | Synthetic Sensors | Provide pluggable synthetic sensors (GPS, IMU, flow, sections) with parameterizable noise models. | Simulation blueprint | Sensor provider tests validate parameter bounds. |
| R-SIM-9703 | MUST | Timewarp | Simulation MUST support pause/resume and variable playback rates without breaking determinism. | ADR-004 | Timewarp regression harness validates consistent outputs. |
| R-SIM-9704 | SHOULD | Hardware Shadowing | Enable hardware shadow mode where live hardware runs alongside simulation for A/B validation. | Automation WG notes | Shadow tests compare live vs. simulated telemetry. |
| R-SIM-9705 | MUST | Golden Run Governance | Golden datasets MUST include provenance metadata, seed values, and acceptance thresholds versioned with §96 release criteria. | QA governance | Release checklist verifies golden package contents. |

---

## 97.5 Acceptance Criteria & Verification

- Replay harness re-runs golden datasets and asserts telemetry, command, and layer hashes match baselines.
- Synthetic sensor provider tests validate noise models and parameter ranges.
- Timewarp scenarios exercise pause/resume and accelerated playback while verifying determinism.
- Release pipeline checks confirm golden datasets are versioned, signed, and distributed with packaging outputs.

### 97.5.1 Requirement-to-Verification Map

| Req ID | Verification Type | Artifact / Location | Pass/Fail Threshold |
|--------|--------------------|---------------------|---------------------|
| R-SIM-9700 | Replay harness | `tests/sim/replay_determinism.cs` | Event ordering hash matches baseline. |
| R-SIM-9701 | Golden run diff | `tests/sim/golden_suite.yaml` | Telemetry/layer hashes within tolerance. |
| R-SIM-9702 | Provider tests | `tests/sim/synthetic_sensor_specs.cs` | Noise parameters stay in configured bounds. |
| R-SIM-9703 | Timewarp regression | `tests/sim/timewarp_regression.md` | Replay output identical across playback rates. |
| R-SIM-9704 | Shadow bench | `tests/hil/shadow_mode.md` | Simulated vs. live telemetry delta ≤ defined threshold. |
| R-SIM-9705 | Release checklist | `qa/playbooks/golden_dataset.md` | Package includes provenance + acceptance thresholds. |

---

## 97.6 Interfaces & Dependencies

- §41 defines service contracts used to stream simulation topics; §52 uses the same contracts for hardware mirroring.
- §63 journals provide the authoritative event store for record/replay; §32 persistence ensures datasets remain durable.
- §96 quality gates invoke simulation suites to decide release readiness; results feed telemetry dashboards in §64.
- §94 packaging must distribute simulation providers and golden datasets with manifests referencing this section.

---

## 97.7 Risks & Open Issues

| ID | Description | Impact | Mitigation / Status | Owner |
|----|-------------|--------|---------------------|-------|
| RISK-97-1 | Golden run datasets grow large, impacting CI runtimes. | Medium | Provide tiered dataset sizes (smoke, regression, soak) and incremental diffs. | @qa |
| RISK-97-2 | Synthetic sensor models diverge from hardware characteristics. | High | Continuously compare with bench captures; adjust noise parameters via ADR updates. | @simulation |
| ISSUE-97-1 | Need deterministic GPU path for rendering-centric simulations. | Medium | Track under mapping WG; evaluate headless renderer instrumentation. | @mapping |

---

## 97.8 Decision History

- Simulation content moved from §94 to dedicated section to clarify responsibilities (2025-10-24).

