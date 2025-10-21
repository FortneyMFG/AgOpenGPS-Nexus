# 73 — Variable Mapping & Variable Rate Control
*(Status: Proposed)*

**Author:** Codex
**Created:** 2025-10-20
**Version:** 0.1.0
**Section ID:** 73
**Editors:** Agronomy & Automation Working Group
**Last Updated:** 2025-10-20
**Related Sections:** 71 — Mapping Kernel Contracts, 72 — Mapping Layers Plugin, 61 — Kinematics & Pose Fusion, 81 — Guidance Orchestrator
**Upstream Dependencies:** ADR-021, ADR-030, ADR-044, ADR-047, Layer Registry metadata (ADR-010)
**Downstream Impacts:** Rate controllers, Section Control, Analytics & Reporting pipelines

---

## 73.1 Purpose & Scope

Define how variable-rate prescriptions, agronomic overlays, and closed-loop controllers integrate with Nexus Core without fragmenting section control workflows. The section coordinates metadata-driven layers, hardware controllers, telemetry, and safety guarantees.

---

## 73.2 Context

- Variable-rate layers originate from metadata-driven schemas that encode units, valid ranges, and smoothing hints for controllers.【F:docs/SRS/sections/3X_Data_Storage/32-O5%20-%20Metadata-driven%20variable-rate%20layers.md†L1-L64】
- Guidance and pose fusion subsystems provide spatial context, speed, and heading data required for controller lookahead and rate compensation.【F:docs/SRS/sections/6X_Core_Domain_Services/61_Kinematics_Pose_Fusion.md†L86-L156】
- Layer controllers share infrastructure with automation pipelines described in ADR-021 and must respect keep-outs and headlands to maintain safety.【F:docs/SRS/sections/2X_System_Architecture/21-O5%20-%20Layer%20controllers%20with%20aggregation%20pipelines.md†L1-L46】【F:docs/SRS/sections/8X_Guidance/81_Guidance_Orchestrator.md†L94-L180】
- Telemetry and health monitoring capture commanded vs. actual rates, calibration deltas, and diagnostic metrics for audit trails.【F:docs/SRS/sections/6X_Core_Domain_Services/64_Telemetry_Health.md†L24-L126】

---

## 73.3 Legacy Comparison

| Area / Theme | Legacy Behavior | Identified Limitation | Modernization Opportunity | Reference / Source |
|---------------|-----------------|------------------------|---------------------------|--------------------|
| Rate Metadata | Prescriptions relied on shapefile attributes interpreted per controller. | Inconsistent units and scaling across devices. | Adopt schema-governed metadata with controller hints. | Metadata-driven VR RFC |
| Controller Architecture | Centralized rate planner fed multiple sections without per-nozzle control. | Latency and oscillations when traversing headlands or boundaries. | Deploy per-section controllers with smoothing and lag compensation. | ADR-021 notes |
| Telemetry & Audit | Limited logging of commanded vs. actual rates. | Difficult to diagnose calibration drift and closed-loop failures. | Publish full telemetry metrics and journal calibration updates. | Telemetry WG backlog |

---

## 73.4 Definitions

| Term | Definition |
|------|-------------|
| Variable-Rate Layer | Layer registry entry describing planned or actual application rate per spatial cell. |
| Controller Hint | Metadata guiding ramp rates, smoothing windows, or nozzle timing. |
| Closed-Loop Control | Feedback system comparing commanded vs. actual rates to adjust outputs automatically. |
| Keep-Out Zone | Geometry preventing application when automation confidence drops or restricted areas exist. |

---

> **Requirement Grammar (RFC-2119):**
> - **MUST / MUST NOT** = mandatory, testable requirement.
> - **SHOULD / SHOULD NOT** = strong guidance; deviations documented.
> - **MAY** = optional extension gated by telemetry + ADR sign-off.

## 73.5 Requirements

| ID | Priority | Category | Summary | Source / C-IDs | Key Metrics / Verification |
|----|-----------|-----------|---------|-----------------|-----------------------------|
| R-VR-7300 | MUST | Metadata | Variable-rate layers MUST include units, controller hints, valid ranges, and provenance metadata as defined in Layer Registry schemas. | ADR-010, Metadata VR RFC | Schema validation & layer lint tooling. |
| R-VR-7301 | MUST | Controller Architecture | Controllers MUST run per section/nozzle with configurable smoothing and lag compensation before commanding hardware. | ADR-021 | Hardware-in-loop (HIL) tests verifying stability. |
| R-VR-7302 | MUST | Safety | Controllers MUST honor headlands, keep-outs, and manual overrides, degrading to manual rates when confidence drops below threshold. | Guidance Orchestrator SRS | Simulation scenarios verifying safe fallback. |
| R-VR-7303 | SHOULD | Telemetry | Systems SHOULD emit commanded vs. actual rate, error bands, calibration events, and diagnostic metrics into telemetry streams. | Telemetry SRS | Telemetry regression harness measuring data completeness. |
| R-VR-7304 | SHOULD | Import/Export | Pipelines SHOULD ingest/emit ISOXML, Shapefile, GeoJSON prescriptions with conflict resolution and provenance capture. | Persistence Formats SRS | Import/export smoke tests verifying metadata retention. |
| R-VR-7305 | MUST | Plugin Extensibility | SDK MUST expose rate calculator hooks that run within Core safety guardrails without bypassing verification layers. | Extensibility Packaging SRS | Plugin integration tests ensuring guardrail enforcement. |

### 73.5.1 Controller Pipeline Overview

1. **Input Normalization:** Union envelopes provide lookahead geometry while pose fusion supplies speed/heading for rate calculations.
2. **Rate Selection:** Controllers evaluate planned layers, apply agronomic models, and calculate target rates per section/nozzle.
3. **Safety Evaluation:** Keep-outs, headlands, manual overrides, and machine state gating ensure only safe commands proceed.
4. **Command Dispatch:** Hardware adapters receive bounded rate commands; smoothing windows prevent oscillations.
5. **Feedback Capture:** Telemetry logs actual rates, errors, and calibration adjustments for replay and analytics.

---

## 73.6 Acceptance Criteria & Verification

- HIL tests validate closed-loop control stability across speed changes, headland entries, and manual overrides.
- Simulation suites replay historic prescriptions and confirm telemetry captures command vs. actual deviations within tolerance.
- Import/export regression tests confirm schema-governed metadata survives round-trips across supported formats.

### 73.6.1 Requirement-to-Verification Map

| Req ID | Verification Type | Artifact / Location | Pass/Fail Threshold |
|--------|--------------------|---------------------|---------------------|
| R-VR-7300 | Schema lint | `tools/registry/vr_layer_lint.py` | 100% of VR layers populate required metadata fields. |
| R-VR-7301 | HIL test | `tests/hil/vr_controller_closed_loop.md` | Overshoot ≤ 5%, settling time ≤ 3 s. |
| R-VR-7302 | Simulation scenario | `sim/scenarios/headland_shutdown.json` | Controller enters safe state within 250 ms of trigger. |
| R-VR-7303 | Telemetry regression | `tests/telemetry/vr_metrics.cs` | Metrics emitted for ≥ 99% of application intervals. |
| R-VR-7304 | Import/export test | `tests/integration/prescription_roundtrip.cs` | Units + provenance preserved after round-trip. |
| R-VR-7305 | SDK test | `tests/sdk/vr_plugin_guardrails.cs` | Plugins cannot bypass guardrails without explicit waiver. |

---

## 73.7 Constraints

- Controllers must run on embedded hardware with deterministic latency (≤ 50 ms cycle time) and limited CPU budget.
- Calibration workflows require operator acknowledgement before applying derived offsets.
- Rate controllers must integrate with section control hardware supporting CAN/ISOBUS messaging without vendor-specific forks.

### 73.7.1 Non-Functional Requirement Classes

- **Performance:** Control loop latency, overshoot, settling times.
- **Reliability:** Fail-safe degradation, telemetry completeness, persistent calibration logs.
- **Safety:** Headland and keep-out enforcement, manual override priority.
- **Operability:** Diagnostic dashboards, calibration workflows, replay support.
- **Maintainability:** Configurable controller parameters, schema-governed metadata.

---

## 73.8 Risks & Open Issues

| ID | Description | Impact | Mitigation / Status | Owner |
|----|-------------|--------|---------------------|-------|
| RISK-73-1 | Closed-loop control unstable on low-bandwidth hydraulic systems. | High | Tune smoothing + PID coefficients per implement class; gather HIL data. | @automation |
| RISK-73-2 | External prescriptions lack metadata for normalization. | Medium | Provide ingestion wizard + advisory warnings; encourage schema export from advisors. | @product |
| ISSUE-73-1 | Define telemetry retention policy for high-frequency rate metrics. | Medium | Coordinate with telemetry WG; may require compression. | @telemetry |
| ISSUE-73-2 | Determine API for AI-assisted rate recommendations interacting with controller hints. | Medium | Pending ADR; evaluate plugin contracts. | @agronomy |

---

## 73.9 Design Considerations

| ID | Consideration | Description |
|----|----------------|-------------|
| C1 | Metadata Governance | Layer schemas drive interoperability; registry automation prevents drift. |
| C2 | Safety First | Controllers prioritize safe degradation over aggressive optimization. |
| C3 | Telemetry Insight | Rich telemetry enables calibration, diagnostics, and compliance reporting. |
| C4 | Format Interoperability | Supporting ISOXML/GeoJSON/Shapefile preserves compatibility with agronomy partners. |
| C5 | Plugin Extensibility | Rate calculators must integrate without bypassing guardrails or duplicating pipelines. |
| C6 | Hardware Diversity | Controllers must tune to hydraulic, electric, and pneumatic systems with consistent abstractions. |

### 73.9.1 Assumptions & Preconditions

- [A1] Mapping plugins deliver normalized union envelopes and keep-out geometries in real time.
- [A2] Telemetry mesh remains available with sufficient bandwidth for command/feedback metrics.
- [A3] Operators can calibrate implements prior to running variable-rate scenarios.

---

## 73.10 Option Overview

| Option ID | Status | Type / Theme | Description | Reference Document |
|-----------|--------|--------------|-------------|--------------------|
| — | — | — | All trade-offs incorporated as design considerations in §73.9. | — |

---

## 73.11 Comparison Matrix

| Attribute / Criteria | Metadata-Driven VR Pipeline | Legacy Prescription Handling |
|----------------------|------------------------------|-------------------------------|
| Controller Granularity | Per-section/nozzle control with smoothing | Whole-implement rate commands |
| Safety Guarantees | Headlands, keep-outs, manual override gating | Manual operator vigilance |
| Telemetry | Commanded vs. actual, calibration history logged | Minimal rate logging |
| Import/Export | ISOXML/GeoJSON/Shapefile with provenance | Vendor-specific shapefile variants |
| Extensibility | SDK hooks with guardrails | Custom integrations per vendor |
