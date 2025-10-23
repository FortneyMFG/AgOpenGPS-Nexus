# 54 — CM5 Integrated Controller
*(Status: Proposed)*

**Author:** Codex
**Created:** 2025-10-20
**Status:** Proposed
**Version:** 0.1.0
**Section ID:** 54
**Editors:** Hardware & IO Working Group
**Last Updated:** 2025-10-20
**Related Sections:** 51 — Sensor & Actuator Abstractions, 53 — AOG-Link Compatibility, 55 — Firmware Interfaces & Updates
**Upstream Dependencies:** 2X — System Architecture, 4X — Interprocess Communications
**Downstream Impacts:** 6X — Core Domain Services, 8X — Guidance, plugins/pumpkin-pi

---

## 54.1 Purpose & Scope

Describe the deterministic control architecture required when a Compute Module 5 (CM5) hosts navigation, steer control, AgIO bridge, and Pumpkin Pi HAL on a single device.
Capture the fast-path, authority, deployment, and observability expectations that keep CM5 integrated rigs interoperable with external controllers and UI clients.

---

## 54.2 Context

- CM5 pilots co-locate Core navigation, steer control, MQTT broker, and Pumpkin Pi fast-path HAL on a single ARM64 host.
- External microcontrollers and legacy AgIO modules must continue to observe and participate in control flows via UDP, serial, CAN-FD, and MQTT transports.
- Deterministic latency (<2 ms p50 for steer commands) is mandatory to avoid control jitter when UI or bridge workloads spike.
- Authority negotiation relies on retained MQTT tokens, while safety policies demand predictable fallbacks when health signals degrade.

---

## 54.3 Legacy Comparison

| Area / Theme | Legacy Behavior | Identified Limitation | Modernization Opportunity | Reference / Source |
|--------------|-----------------|-----------------------|---------------------------|--------------------|
| Control Path | UDP/serial routed through AgIO on external PCs. | Additional hops introduce latency and jitter. | Shared-memory fast path with mirrored telemetry for observers. | Pumpkin Pi brief【F:docs/Plugins/pumpkin-pi.md†L1-L55】 |
| Authority | Manual overrides; no retained lease tokens. | Competing controllers risk conflicting outputs. | MQTT authority tokens with TTL + acknowledgement semantics. | AOG-Link spec【F:docs/development/SRS/sections/5X_Hardware_IO_Device_Layer/53_AOG_Link_Compatibility.md†L120-L220】 |
| Scheduling | Mixed workloads on Windows tablets. | Unpredictable jitter under CPU load. | PREEMPT_RT kernels, CPU affinity, and locked memory on CM5. | CM5 guidance【F:docs/AgIO/cm5.md†L6-L33】 |

---

## 54.4 Definitions

| Term | Definition |
|------|------------|
| Pumpkin Pi | HAL + broker service that manages shared-memory fast path and mirrors telemetry for CM5 deployments. |
| Authority Token | Retained MQTT topic indicating which controller may command a specific actuator group. |
| Fast Path | Shared-memory ring (`/dev/shm/aoglink_steer`) and eventfd notifications enabling <2 ms p50 steer updates. |
| HAL (Hardware Abstraction Layer) | CM5-local service providing deterministic IO access to actuators and sensors. |

> **Requirement Grammar (RFC-2119):**
> - **MUST / MUST NOT** = mandatory requirements.
> - **SHOULD / SHOULD NOT** = strong recommendations with waiver process.
> - **MAY** = optional capabilities or roadmap items.

---

## 54.5 Requirements

| ID | Priority | Category | Summary | Source / C-IDs | Key Metrics / Verification |
|----|----------|----------|---------|-----------------|-----------------------------|
| R-CM5-000 | MUST | Fast Path | Provide shared-memory fast path for `SteerTarget` with <2 ms p50 latency and mirrored MQTT topics. | Pumpkin Pi docs | Latency bench tests show <2 ms p50, <5 ms p95 under load. |
| R-CM5-001 | MUST | AgIO Coexistence | Keep AgIO bridge online for UDP, serial, CAN-FD, and MQTT-SN adapters even when fast path active. | AOG-Link + AgIO specs | Mixed transport regression suite validates parity. |
| R-CM5-002 | MUST | Authority | Implement retained authority tokens (`aog/v1/ctrl/authority/{group}`) with ≤150 ms acknowledgement. | AOG-Link spec | Authority arbitration tests enforce TTL expiry + safe fallback. |
| R-CM5-003 | MUST | Scheduling | Run steer-ctrl and Pumpkin Pi with real-time scheduling, CPU affinity, and locked memory on PREEMPT_RT kernels. | CM5 deployment notes | Systemd units enforce `rtprio`, `cpuset`, and `mlockall` settings. |
| R-CM5-004 | SHOULD | MCU Coexistence | Allow external MCUs to observe fast-path setpoints and assume control via authority tokens without schema changes. | AOG-Link spec | Integration tests confirm external takeover without errors. |
| R-CM5-005 | MUST | Fault Handling | Detect expired authority tokens or missing health heartbeats within three intervals and drive neutral outputs. | Safety policy | Failsafe tests confirm neutral output on expiry. |
| R-CM5-006 | SHOULD | Deployment | Ship reference `pumpkin.yaml`, systemd units, and health logging for CM5 images. | Pumpkin Pi docs | Install scripts validate configuration on fresh image. |
| R-CM5-007 | SHOULD | Observability | Surface fast-path metrics (latency, missed events) via telemetry and CLI dashboards. | Telemetry SRS | Dashboards expose metrics; alerts fire on threshold breach. |
| R-CM5-008 | COULD | Offline Mode | Allow CM5 to run autonomous fallback when disconnected from remote MQTT brokers. | Field feedback | Documented fallback mode tested in lab scenario. |

### 54.5.1 Requirement Sources & Rationale

| Req ID | Source | Rationale |
|--------|--------|-----------|
| R-CM5-000 — R-CM5-003 | Pumpkin Pi + CM5 deployment docs | Guarantee deterministic control loops on integrated hardware. |
| R-CM5-004 — R-CM5-007 | Firmware + safety coordination | Maintain interoperability and observability across controllers. |
| R-CM5-008 | Field feedback | Support resilient operation during connectivity loss. |

---

## 54.6 Architecture Overview

- **Process Layout:** Distinguish host/broker services (Core gRPC, adapters, MQTT) from real-time control services (GPS ingest, steer-ctrl, HAL). Pumpkin Pi supervises the fast path and telemetry mirroring.【F:docs/Plugins/pumpkin-pi.md†L1-L55】
- **Data Paths:** Navigation publishes `SteerTarget` into shared memory, triggers eventfd wake-ups, and mirrors payloads to MQTT topics (`aog/v1/bus/nav/steer_target`). External controllers continue to consume UDP/serial PGNs through AgIO.【F:docs/development/SRS/sections/5X_Hardware_IO_Device_Layer/53_AOG_Link_Compatibility.md†L180-L220】
- **Authority:** Retained MQTT tokens arbitrate actuator control; Pumpkin Pi acknowledges or relinquishes control within TTL budgets.【F:docs/development/SRS/sections/5X_Hardware_IO_Device_Layer/53_AOG_Link_Compatibility.md†L200-L240】

### 54.6.1 Operational Sequence

1. System boots PREEMPT_RT kernel, starts MQTT broker, AgIO bridge, Pumpkin Pi, and steer-ctrl services.
2. Pumpkin Pi validates shared-memory schema version and initializes fast-path buffers.
3. Navigation publishes steer targets; Pumpkin Pi relays to steer-ctrl and mirrors to MQTT/UDP bridges.
4. Authority tokens coordinate control ownership; expired or missing acknowledgements trigger neutral outputs.
5. Health telemetry streams latency, event counts, and authority state to monitoring dashboards.

---

## 54.7 Deployment Guidance

- Provide systemd unit templates with `ConditionPathExists=/dev/shm/aoglink_steer` checks to prevent stale schema mismatches.
- Ensure `pumpkin.yaml` defines actuator groups, authority TTLs, and fallback behaviors consistent with Section 51 abstractions.
- Document required Linux capabilities (`cap_sys_nice`, `cap_ipc_lock`) and CPU affinity recommendations for CM5 images.
- Supply validation scripts that confirm shared memory, eventfd wiring, and telemetry endpoints before enabling steer outputs.

---

## 54.8 Risks & Open Issues

| ID | Description | Impact | Mitigation / Status | Owner |
|----|-------------|--------|---------------------|-------|
| RISK-54-1 | Shared-memory schema drift between Core and Pumpkin Pi causes crashes. | High | Version shared memory header; add migration checks. | @pumpkin-pi |
| RISK-54-2 | Authority token TTL misconfiguration leads to tug-of-war between controllers. | Medium | Provide presets and validation warnings in UI. | @ui |
| RISK-54-3 | PREEMPT_RT kernel availability on CM5 supply chain uncertain. | Medium | Publish supported kernel list and fallback configs. | @platform |
| ISSUE-54-1 | Determine offline mode behavior when MQTT broker unavailable. | Medium | Evaluate local broker fallback; document ops procedures. | @ops |
| ISSUE-54-2 | Clarify logging retention for fast-path telemetry on constrained storage. | Low | Coordinate with telemetry retention policy. | @ops |

---

## 54.9 Design Considerations

| ID | Consideration | Description |
|----|----------------|-------------|
| C1 | Fast-path exclusivity | Decide when to bypass AgIO entirely vs. mirror telemetry for observers. |
| C2 | External controller handoff | Define expectations for MCUs assuming authority without downtime. |
| C3 | Scheduling policy | Balance PREEMPT_RT requirements with general-purpose workloads on CM5. |
| C4 | Health telemetry granularity | Determine which metrics are mandatory vs. optional for field diagnostics. |
| C5 | Offline resilience | Support safe operation when network connectivity is intermittent. |

### 54.9.1 Assumptions & Preconditions

- [A1] CM5 images include PREEMPT_RT kernel and required capabilities by default.
- [A2] Operators deploy MQTT brokers locally or on-box to minimize network latency.
- [A3] External controllers honor authority tokens and fail-safe policies defined in Section 53.

---

## 54.10 Option Overview

| Option ID | Status | Type / Theme | Description | Reference Document |
|-----------|--------|--------------|-------------|--------------------|
| — | — | — | No standalone option documents retained for Section 54 after rebaseline; see §54.9 for considerations. | — |

---

## 54.11 Comparison Matrix

| Attribute / Criteria | Legacy PC + AgIO | CM5 Integrated Controller |
|----------------------|-------------------|---------------------------|
| Latency | Medium — network + process hops | High — shared-memory fast path |
| Deployment Complexity | Low — off-the-shelf PCs | Medium — CM5 image management |
| Observability | Medium — AgIO telemetry only | High — mirrored MQTT + fast-path metrics |
| Resilience | Medium — dependent on external PC | High — local authority + watchdogs |
| Extensibility | Low — limited authority semantics | High — token-based arbitration |

---

## 54.12 Decision Matrix

| Consideration | Compatibility | Safety | Maintainability | Extensibility | Weighted Score |
|---------------|--------------|--------|-----------------|--------------|----------------|
| C1 — Fast-path exclusivity | 4 | 5 | 3 | 4 | 4.05 |
| C2 — External controller handoff | 5 | 5 | 4 | 4 | 4.45 |
| C3 — Scheduling policy | 3 | 4 | 4 | 3 | 3.55 |
| C4 — Health telemetry granularity | 3 | 4 | 4 | 3 | 3.45 |
| C5 — Offline resilience | 4 | 4 | 3 | 4 | 3.95 |

> **Informative:** Weighted scores assume compatibility 0.25, safety 0.30, maintainability 0.20, extensibility 0.15, operator experience 0.10.
