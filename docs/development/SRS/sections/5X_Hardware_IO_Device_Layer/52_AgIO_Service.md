# 52 — AgIO Service
*(Status: Proposed)*

**Author:** Codex
**Created:** 2025-10-20
**Status:** Proposed
**Version:** 0.1.0
**Section ID:** 52
**Editors:** Hardware & IO Working Group
**Last Updated:** 2025-10-20
**Related Sections:** 21 — Linux Core Service, 51 — Sensor & Actuator Abstractions, 94 — Extensibility & Packaging Updates
**Upstream Dependencies:** 2X — System Architecture, 4X — Interprocess Communications
**Downstream Impacts:** 6X — Core Domain Services, 8X — Guidance, 9X — Frontends & Ops

---

## 52.1 Purpose & Scope

Define the responsibilities, lifecycle, and observability of AgIO as the hardware integration host.
Clarify how AgIO operates as a privileged service across desktop and headless deployments while maintaining compatibility with legacy in-process builds.

---

## 52.2 Context

- AgIO historically shipped in-process with WinForms-based AgOpenGPS builds and directly owned serial/UDP hardware.
- Linux pilots require AgIO to run as a managed service with permission-gated transports, health telemetry, and remote configuration.
- Plugin architecture treats AgIO as an extension host coordinating drivers, enumerations, and simulator shims for deterministic replay.
- Safety and compliance policies mandate explicit lease management and command gating before actuating steer or section hardware.

---

## 52.3 Legacy Comparison

| Area / Theme | Legacy Behavior | Identified Limitation | Modernization Opportunity | Reference / Source |
|--------------|-----------------|-----------------------|---------------------------|--------------------|
| Process Model | Embedded directly in desktop UI process. | Tight coupling prevents headless deployments and restarts. | Run as standalone service with gRPC/WebSocket transports. | Linux Core service proposal【F:docs/development/SRS/sections/2X_System_Architecture/21-O6%20-%20Linux%20Core%20service%20with%20remote%20frontends.md†L1-L44】 |
| Driver Lifecycle | Manual start/stop with limited retry logic. | Device swaps require reboots; USB churn unstable. | Declarative driver model with retry, watchdog, and hot-plug support. | Section 51 requirements【F:docs/development/SRS/sections/5X_Hardware_IO_Device_Layer/51_Sensor_Actuator_Abstractions.md†L28-L116】 |
| Observability | Diagnostics confined to desktop dialogs. | Limited telemetry for remote troubleshooting. | Emit structured health metrics and CLI summaries for every transport. | Telemetry & health sections【F:docs/development/SRS/sections/6X_Core_Domain_Services/64_Telemetry_Health.md†L28-L126】 |

---

## 52.4 Definitions

| Term | Definition |
|------|------------|
| Driver Host | AgIO subsystem responsible for loading, supervising, and restarting transport drivers. |
| Lease Authority | Mechanism ensuring only authenticated controllers can issue actuator commands through AgIO. |
| Transport Adapter | Plugin-provided shim exposing serial, CAN, UDP, MQTT-SN, or simulated hardware endpoints. |

> **Requirement Grammar (RFC-2119):**
> - **MUST / MUST NOT** = mandatory requirements.
> - **SHOULD / SHOULD NOT** = strong recommendations with waiver process.
> - **MAY** = optional capabilities or roadmap items.

---

## 52.5 Requirements

| ID | Priority | Category | Summary | Source / C-IDs | Key Metrics / Verification |
|----|----------|----------|---------|-----------------|-----------------------------|
| R-AGIO-000 | MUST | Process Boundary | Support running AgIO out-of-process via gRPC/WebSocket while remaining embeddable for legacy builds. | Linux Core service brief | Launch scripts start/stop AgIO independently with integration tests exercising both modes. |
| R-AGIO-001 | MUST | Driver Lifecycle | Provide hot-pluggable driver model with discovery, retry, and watchdog policies for transport stability. | Section 51 dependencies | Simulated USB/CAN churn in CI recovers without manual intervention. |
| R-AGIO-002 | SHOULD | Capability Registry | Publish available transports, IO boards, and capabilities through plugin registry endpoints. | Packaging & extensibility SRS | Registry API enumerates all active drivers with metadata checks. |
| R-AGIO-003 | MUST | Safety Gating | Enforce permission checks and automatic manual override fallbacks for steer/section commands. | Security permissions SRS | Command audit tests confirm unauthorized requests are rejected within <100 ms. |
| R-AGIO-004 | SHOULD | Health Reporting | Emit per-transport latency, packet loss, and device fault metrics accessible via telemetry/CLI. | Telemetry health SRS | Health dashboards show metrics for all registered transports. |
| R-AGIO-005 | MUST | Simulation Parity | Mirror hardware drivers with deterministic simulation shims for CI and replay workflows. | Replay-driven CI brief | Replay suite exercises simulated transports without divergence. |
| R-AGIO-006 | SHOULD | Configuration UX | Provide APIs that allow UI to surface device states, update firmware, and manage leases without direct hardware access. | Firmware update SRS | UI tests fetch device inventory and initiate updates via AgIO APIs. |
| R-AGIO-007 | COULD | Multi-tenant Isolation | Allow multiple AgIO instances per host for test labs while isolating device access. | Lab deployment feedback | Optional multi-instance configuration documented and validated on Linux. |

### 52.5.1 Requirement Sources & Rationale

| Req ID | Source | Rationale |
|--------|--------|-----------|
| R-AGIO-000 — R-AGIO-001 | Linux Core initiative | Enable headless CM5/edge deployments without desktop coupling. |
| R-AGIO-002 — R-AGIO-004 | Packaging, security, and telemetry specs | Provide reliable discovery, gating, and diagnostics. |
| R-AGIO-005 — R-AGIO-006 | Replay/QA roadmap | Ensure CI and UX flows remain deterministic and operator-friendly. |
| R-AGIO-007 | Lab feedback | Support advanced test configurations without blocking baseline deployment. |

---

## 52.6 Acceptance Criteria & Verification

- Service supervisor scripts start, monitor, and restart AgIO independently of Core UI on Windows and Linux.
- Capability registry endpoints expose live driver metadata consumed by UI/device manager panels.
- Permissions enforcement audit demonstrates unauthorized command attempts are rejected and manual overrides trigger failsafe states.
- Replay and simulation suites run against AgIO shims to validate deterministic behavior without physical hardware.

### 52.6.1 Requirement-to-Verification Map

| Req ID | Verification Type | Artifact / Location | Pass/Fail Threshold |
|--------|-------------------|---------------------|---------------------|
| R-AGIO-000 | Integration test | `tests/agio/service-boundary/` | Start/stop success for embedded + external modes. |
| R-AGIO-001 | Chaos test | `qa/hardware/driver-hotplug/` | Device reconnect succeeds within retry window. |
| R-AGIO-003 | Security test | `tests/security/agio-command-audits/` | Unauthorized commands denied; overrides logged. |
| R-AGIO-004 | Telemetry check | `dashboards/agio-health.json` | Metrics populate for each transport without gaps. |
| R-AGIO-005 | Replay suite | `tests/replay/agio-shim/` | Simulated transports match recorded traces. |

---

## 52.7 Constraints

- Maintain binary compatibility for legacy AgIO plugins until new SDK contracts are published.
- Keep service startup within acceptable boot budgets for CM5 images (<15 s including driver enumeration).
- Ensure logging and telemetry output remain bounded to avoid saturating low-bandwidth field deployments.
- Align security scopes with platform-wide permission taxonomy to avoid fragmented policy management.

### 52.7.1 Non-Functional Requirement Classes

- **Performance:** Rapid driver initialization, minimal command latency under supervision.
- **Reliability:** Automatic recovery from device churn and process crashes.
- **Security:** Enforced permission scopes, audited command pathways, TLS for remote transports.
- **Maintainability:** Declarative driver configuration, consistent plugin lifecycle management.
- **Portability:** Cross-platform packaging for Windows services, Linux systemd units, and container deployments.

---

## 52.8 Risks & Open Issues

| ID | Description | Impact | Mitigation / Status | Owner |
|----|-------------|--------|---------------------|-------|
| RISK-52-1 | Standalone AgIO service introduces new points of failure during boot. | Medium | Provide watchdog + restart policies; document health checks. | @agio |
| RISK-52-2 | Permission scoping blocks community plugins without updated manifests. | Medium | Publish migration guides and allow transitional compatibility mode. | @security |
| RISK-52-3 | Health metrics overwhelm telemetry storage in low-bandwidth environments. | Low | Offer sampling controls and local buffering. | @ops |
| ISSUE-52-1 | Decide where lease authority lives when multiple AgIO instances share transports. | Medium | Evaluate centralized authority broker in Section 53. | @architecture |
| ISSUE-52-2 | Define upgrade path for legacy configuration dialogs interfacing with new APIs. | Medium | Coordinate with UI roadmap in Section 94. | @ui |

---

## 52.9 Design Considerations

| ID | Consideration | Description |
|----|----------------|-------------|
| C1 | Embedded vs. service boundary | Balance ease of deployment for legacy desktop builds with benefits of standalone service supervision. |
| C2 | Driver packaging | Determine whether drivers ship as plugins, containers, or OS packages while maintaining signature and update policies. |
| C3 | Observability depth | Calibrate telemetry granularity to support diagnostics without overwhelming constrained networks. |
| C4 | Security posture | Define minimum permission scopes, credential storage, and rotation workflows for hardware access. |
| C5 | Simulation fidelity | Ensure driver shims reflect hardware timing characteristics to keep replay reliable. |

### 52.9.1 Assumptions & Preconditions

- [A1] Shared identity and permission services are available for AgIO to authenticate controllers.
- [A2] Operators can install updated drivers or plugins via documented packaging workflows.
- [A3] UI surfaces adopt new APIs for device inventory and firmware updates.

---

## 52.10 Option Overview

| Option ID | Status | Type / Theme | Description | Reference Document |
|-----------|--------|--------------|-------------|--------------------|
| — | — | — | No standalone option documents retained for Section 52 after rebaseline; see §52.9 for considerations. | — |

---

## 52.11 Comparison Matrix

| Attribute / Criteria | Embedded AgIO (Legacy) | Managed AgIO Service |
|----------------------|------------------------|----------------------|
| Deployment Effort | Low | Medium |
| Resilience | Low — UI crash stops hardware | High — service watchdogs restart drivers |
| Observability | Low — local dialogs only | High — telemetry + CLI |
| Security | Medium — implicit trust | High — scoped permissions |
| Extensibility | Medium — limited plugin lifecycle | High — declarative driver packaging |

---

## 52.12 Decision Matrix

| Consideration | Compatibility | Safety | Maintainability | Extensibility | Weighted Score |
|---------------|--------------|--------|-----------------|--------------|----------------|
| C1 — Embedded vs. service boundary | 5 | 4 | 3 | 3 | 3.75 |
| C2 — Driver packaging | 4 | 3 | 4 | 4 | 3.85 |
| C3 — Observability depth | 3 | 4 | 4 | 3 | 3.45 |
| C4 — Security posture | 3 | 5 | 4 | 3 | 3.70 |
| C5 — Simulation fidelity | 4 | 4 | 4 | 4 | 4.00 |

> **Informative:** Weighted scores assume compatibility weight 0.25, safety 0.25, maintainability 0.20, extensibility 0.20, and operator experience 0.10.
