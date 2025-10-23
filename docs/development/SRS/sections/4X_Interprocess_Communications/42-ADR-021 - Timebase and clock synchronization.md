# 42-ADR-021 — Timebase and clock synchronization
*(Status: Proposed)*

**Author:** Codex
**Reviewers:** Interprocess Communications Working Group
**Created:** 2025-10-20
**Last Updated:** 2025-10-20
**Status:** Proposed
**Version:** 0.1.0
**Supersedes:** _None_
**Superseded by:** _None_
**Related SRS:** [42 — Transports](42_Transports.md)
**Related Considerations:** [C5 - Timebase & Telemetry Mesh Governance](42_Transports.md#c5---timebase--telemetry-mesh-governance)

---

## 1) Context

Cross-device coordination requires a canonical time authority to align PoseStream sequencing, telemetry mesh updates, and
control commands. Legacy deployments relied on ad-hoc system clocks, producing drift between devices and inconsistent gating
behavior.【F:docs/sections/4X_Interprocess_Communications/42_Transports.md†L180-L205】

```mermaid
flowchart LR
  A[Uncoordinated clocks] --> B[Pose & telemetry drift]
  B --> C[Canonical timebase]
  C --> D[Synchronized transports]
```

---

## 2) Decision

Establish a canonical time authority derived from GPS or PTP with system clock fallback, accompanied by drift tolerances,
monitoring, and reconciliation policies for firmware and transports.

### Decision Summary

* **Scope:** PoseStream cadence, telemetry mesh timestamps, and transport-level sequencing.
* **Boundary:** Application-level scheduling remains under domain services; this ADR focuses on clock distribution.
* **Implementation Level:** Design + code; clock sync daemons, telemetry metrics, and drift alerting.

---

## 3) Consequences

**Positive Impacts:**

* Deterministic sequencing across PoseStream, layer transports, and telemetry mesh topics.
* Drift monitoring enables proactive alerts and automated reconciliation workflows.
* Shared tooling simplifies CI replay validation and regression analysis.

**Negative / Mitigated Impacts:**

* Requires additional services/daemons on field devices — mitigated by lightweight agents leveraging existing GPS/PTP feeds.
* GPS-denied environments must fall back to system clocks — addressed with drift thresholds and operator alerts.
* Firmware updates needed to emit capture timestamps and sequence numbers — staged through reference implementations.

**Follow-up Actions:**

* Implement clock sync agent supporting GPS, PTP, and system clock fallback with drift metrics.
* Extend transports to include capture timestamps and monotonic counters for reconciliation.
* Publish dashboards and alerts highlighting drift beyond 5 ms and certificate drift for PTP sources.

---

## 4) Rationale

A shared timebase underpins deterministic control and telemetry flows. With GPS/PTP as primary sources and system clock
fallback, transports can reconcile sample capture times, align mesh topics, and guarantee reproducible replay behavior.

---

## 5) Alternatives Considered

| Option | Summary | Reason Not Selected |
|--------|---------|---------------------|
| Per-device clocks | Allow each device to free-run. | Leads to drift and non-deterministic control behavior. |
| NTP-only sync | Use commodity NTP. | Insufficient precision for sub-10 ms control loops. |
| Application-level reconciliation | Let each service handle drift manually. | Duplicated logic and inconsistent mitigation. |

