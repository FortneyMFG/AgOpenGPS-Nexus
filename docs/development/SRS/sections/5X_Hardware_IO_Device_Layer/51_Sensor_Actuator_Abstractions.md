# 51 — Sensor & Actuator Abstractions
*(Status: Proposed)*

**Author:** Codex
**Created:** 2025-10-20
**Status:** Proposed
**Version:** 0.1.0
**Section ID:** 51
**Editors:** Hardware & IO Working Group
**Last Updated:** 2025-10-20
**Related Sections:** 21 — Linux Core Service, 52 — AgIO Service, 53 — AOG Link Compatibility
**Upstream Dependencies:** 2X — System Architecture, 4X — Interprocess Communications
**Downstream Impacts:** 6X — Core Domain Services, 7X — Mapping & Geospatial, 9X — Frontends & Ops

---

## 51.1 Purpose & Scope

Establish the abstraction model that lets Nexus Core, AgIO, and plugins interact with steer, section, rate, GNSS, and auxiliary sensors without hard-coding per-device behaviors.
The scope covers legacy PGN flows, modular firmware capabilities, transport discovery, and safety expectations required for deterministic operation across Windows and Linux deployments.

---

## 51.2 Context

- Legacy AgOpenGPS relies on WinForms dialogs and manual PGN routing to configure UDP, serial, and CAN hardware.
- AgIO continues to broker device transports, but Linux pilots require SocketCAN parity, udev naming, and service packaging to avoid bespoke driver work.
- Firmware vendors are exploring richer telemetry (variable-rate layers, high-frequency sensors) that must integrate with configuration tooling and UI surfaces.
- Safety envelopes demand watchdogs, manual overrides, and deterministic fallbacks even when new abstraction layers are introduced.

---

## 51.3 Legacy Comparison

| Area / Theme | Legacy Behavior | Identified Limitation | Modernization Opportunity | Reference / Source |
|--------------|-----------------|-----------------------|---------------------------|--------------------|
| Module Discovery | Manual serial/UDP scans driven from desktop dialogs. | Error-prone setup; limited metadata for automation. | Structured discovery API exposing capabilities and health metrics. | AgIO discovery utilities; FormCommSetGPS dialog【F:SourceCode/AgIO/Source/Forms/FormCommSetGPS.cs†L20-L160】 |
| PGN Contracts | Static PGN catalog maintained in documentation. | Difficult to evolve when adding sensors or Linux gateways. | Capability-aware abstraction that maps hardware channels to logical layers. | AgIO PGN baseline references【F:docs/development/SRS/references/AgIO_PGN_Baseline.md†L1-L120】 |
| Transport Coverage | Windows-first serial and UDP bridges. | SocketCAN/Linux workflows require ad-hoc scripts; high-rate sensors congest UDP. | Unified transport abstraction with SocketCAN, throttling, and firmware-led cadence. | Linux service proposal【F:docs/development/SRS/sections/2X_System_Architecture/21-O6%20-%20Linux%20Core%20service%20with%20remote%20frontends.md†L11-L44】 |

---

## 51.4 Definitions

| Term | Definition |
|------|------------|
| Capability Registry | Catalog describing firmware features, supported layers, and transport metadata published by hardware modules. |
| Layer Definition Manager | Configuration surface that maps physical IO channels to logical agronomic layers for telemetry and control. |
| Authority Token | Retained control lease (typically MQTT) that governs which controller may drive a specific actuator group. |

> **Requirement Grammar (RFC-2119):**
> - **MUST / MUST NOT** = mandatory requirements.
> - **SHOULD / SHOULD NOT** = strong recommendations with waiver process.
> - **MAY** = optional capabilities or roadmap items.

---

## 51.5 Requirements

| ID | Priority | Category | Summary | Source / C-IDs | Key Metrics / Verification |
|----|----------|----------|---------|-----------------|-----------------------------|
| R-HW-000 | MUST | Configuration | Preserve serial port routing for GPS, IMU, steer, machine, and RTCM with UI selection. | Legacy AgIO dialogs | UI smoke tests cover serial assignments across transports. |
| R-HW-001 | MUST | Compatibility | Maintain PGN-based machine and section control framing for legacy auto-steer and relay modules. | AgOpenGPS PGN definitions | Regression suite validates PGN encoding/decoding. |
| R-HW-002 | SHOULD | Ecosystem | Ensure compatibility with SK21/SKx rate control hardware referenced by the community. | Contributor hardware inventory | Bench matrix verifies interoperability with SK21 controllers. |
| R-HW-003 | SHOULD | Diagnostics | Retain UDP scanning and diagnostics that locate steer, machine, IMU, and GPS modules. | AgIO UDP monitor | AgDiag tooling enumerates devices in CI simulations. |
| R-HW-004 | SHOULD | Platform | Provide SocketCAN, udev naming, and bridge services for Linux SBC deployments. | Linux Core service proposal | Dual-OS CI verifies SocketCAN routing + naming conventions. |
| R-HW-005 | MUST | Governance | Document PGN catalog and maintain backward compatibility or adapters for new abstractions. | PGN baseline; PGN bridge SRS | Schema diff tooling alerts on incompatible PGN updates. |
| R-HW-006 | COULD | Discovery | Add standardized capability discovery handshake to reduce manual configuration. | Hardware WG roadmap | Prototype handshake validated in hardware lab. |
| R-HW-010 | MUST | Layer Mapping | Publish firmware capabilities, supported layers, and channel-to-layer mappings for configuration flows. | Variable-rate telemetry proposal | Layer registry integration tests exercise metadata ingestion. |
| R-HW-011 | SHOULD | Performance | Provide presets and throttling controls for high-frequency sensors to avoid congestion. | Variable-rate telemetry proposal | Load tests confirm UDP utilisation within thresholds. |
| R-HW-012 | SHOULD | Standards Alignment | Introduce ISOBUS-inspired condensed work-state PGNs while preserving legacy behavior. | ISOBUS section control notes | PGN conformance harness covers condensed feedback streams. |
| R-HW-013 | MUST | Safety | Require watchdogs, fail-safe defaults, and manual override paths for modular firmware. | Safety policy | Hardware-in-loop tests confirm safe fallback when telemetry drops. |
| R-HW-014 | SHOULD | Compliance | Reserve placeholders for required certifications and field validation (e.g., ISO 25119). | Safety WG feedback | Documentation includes compliance checklist before release. |
| R-HW-020 | MUST | AgIO Plugin Model | Treat AgIO as an out-of-process plugin with discovery, authentication, and lease renewal. | AgIO service SRS | Integration tests verify plugin lifecycle + lease timeouts. |
| R-HW-021 | MUST | Security | Gate raw device access behind permission scopes (`io.device`, `io.can`, `io.serial`). | Security permissions SRS | Permissions tests ensure unauthorized plugins cannot open devices. |
| R-HW-022 | SHOULD | Enumeration | Expose standardized enumeration API for detected devices and capabilities. | Hardware WG backlog | API smoke tests enumerate simulated hardware fleet. |
| R-HW-023 | SHOULD | Health Telemetry | Require hardware plugins to publish transport health metrics for diagnostics. | Telemetry health SRS | Health dashboard surfaces latency and fault counts in QA runs. |

### 51.5.1 Requirement Sources & Rationale

| Req ID | Source | Rationale |
|--------|--------|-----------|
| R-HW-000 — R-HW-003 | Legacy AgIO + AgOpenGPS implementations | Preserve continuity for existing operators while re-platforming. |
| R-HW-004 — R-HW-006 | Linux Core + PGN bridge proposals | Enable Linux-first deployments without regressing safety. |
| R-HW-010 — R-HW-014 | Variable-rate firmware brief & safety governance | Support richer telemetry while enforcing safeguards. |
| R-HW-020 — R-HW-023 | AgIO service + security docs | Formalize AgIO as privileged hardware plugin with observability. |

---

## 51.6 Acceptance Criteria & Verification

- Regression harness validates serial, UDP, and CAN transports for navigation, steer, and section PGNs on Windows and Linux.
- Layer registry integration tests confirm firmware-published capabilities are consumable by configuration flows and UI surfaces.
- Health telemetry dashboards and CLI tooling display latency, packet loss, and heartbeat metrics sourced from hardware plugins.
- Permissions tests ensure only authorized processes may open hardware devices and publish control commands.

### 51.6.1 Requirement-to-Verification Map

| Req ID | Verification Type | Artifact / Location | Pass/Fail Threshold |
|--------|-------------------|---------------------|---------------------|
| R-HW-000 / R-HW-001 | UI regression + PGN tests | `tests/hardware/regression/serial_udp_can/` | PGN round-trips succeed across transports. |
| R-HW-004 | Dual-OS CI | `pipelines/linux-hardware.yml` | SocketCAN enumeration matches baseline fixtures. |
| R-HW-010 / R-HW-011 | Integration tests | `tests/layers/firmware_capabilities/` | Layer metadata ingested without validation errors. |
| R-HW-013 | Hardware-in-loop | `qa/hil/failsafe/` | Watchdog triggers safe-neutral within configured TTL. |
| R-HW-021 | Permission audit | `tests/security/device-permissions/` | Unauthorized plugin attempts are rejected. |

---

## 51.7 Constraints

- Maintain compatibility with existing Arduino/Teensy/ESP32 firmware deployed across the community until migration guides ship.
- Keep transport abstraction performant enough for headless CM5 deployments (<2 ms p50 for steer setpoints when on-box fast paths are active).
- Ensure configuration UX remains approachable; advanced metadata must not overwhelm operators during setup flows.
- Align abstraction updates with plugin SDK versioning to avoid breaking third-party integrations mid-season.

### 51.7.1 Non-Functional Requirement Classes

- **Performance:** Low-latency transport handling and throttled telemetry emission.
- **Reliability:** Deterministic enumeration, fail-safe fallbacks, and watchdog coverage.
- **Security:** Scoped device permissions, authenticated plugin leases, tamper-resistant PGN updates.
- **Maintainability:** Schema-driven capability registry, shared configuration components, documented PGN mapping.
- **Portability:** Windows and Linux parity for transports, including ARM64 SBC deployments.

---

## 51.8 Risks & Open Issues

| ID | Description | Impact | Mitigation / Status | Owner |
|----|-------------|--------|---------------------|-------|
| RISK-51-1 | Operators mis-map hardware channels when richer layer metadata ships. | High | Ship presets, validation rules, and guided setup flows. | @hardware-wg |
| RISK-51-2 | Firmware without capability metadata becomes unusable. | Medium | Provide legacy-only toggle and retain binary PGN flows. | @hardware-wg |
| RISK-51-3 | UDP block congestion for multi-module rigs. | Medium | Allow configurable cadence and prioritization per module. | @agio |
| ISSUE-51-1 | Define handshake standard for capability discovery across transports. | Medium | Prototype within AgIO service; document spec. | @agio |
| ISSUE-51-2 | Determine validation strategy for ISOBUS-inspired condensed PGNs. | Medium | Extend PGN test harness; involve ISOBUS contributors. | @hardware-wg |
| ISSUE-51-3 | Hardware CI coverage for new modules without physical rigs. | High | Expand simulator coverage and loaner pool documentation. | @qa |

---

## 51.9 Design Considerations

| ID | Consideration | Description |
|----|----------------|-------------|
| C1 | Status quo PGN routing | Retain manual serial/UDP PGN management for continuity; useful during migration phases. |
| C2 | Hardware abstraction layer | Introduce per-device drivers with clear contracts, trading maintenance effort for stronger encapsulation. |
| C3 | CANopen / ISOBUS-first | Leverage standards-based modules to reduce bespoke firmware, acknowledging upgrade cost for legacy rigs. |
| C4 | Plug-and-play USB/HID | Support commodity USB/HID devices for auxiliary sensors, mindful of Windows driver management overhead. |
| C5 | Modular firmware layer publishing | Firmware advertises layer capabilities, channel mappings, presets, and throttling to keep configuration declarative while guarding legacy fallbacks. |
| C6 | Linux gateway bridge | Deploy embedded Linux gateways (e.g., CM5) that proxy SocketCAN/serial into Core APIs while maintaining safety budgets. |

### 51.9.1 Assumptions & Preconditions

- [A1] Firmware vendors participate in capability registry definitions prior to rollout.
- [A2] CI simulators accurately reflect transport timing and PGN behavior for regression coverage.
- [A3] Operators retain access to manual override hardware when experimenting with new abstraction layers.

---

## 51.10 Option Overview

| Option ID | Status | Type / Theme | Description | Reference Document |
|-----------|--------|--------------|-------------|--------------------|
| — | — | — | No standalone option documents tracked under Section 51 after rebaseline; design considerations captured in §51.9. | — |

---

## 51.11 Comparison Matrix

| Attribute / Criteria | Legacy PGN Workflow | Modular Firmware Publishing |
|----------------------|---------------------|-----------------------------|
| Implementation Effort | Low — existing tooling | Medium — firmware + UI updates |
| Maintainability | Medium — manual mapping | High — declarative registry |
| Operator Experience | Medium — manual setup | High — guided setup + presets |
| Risk Level | Medium — misconfiguration risk | Medium — requires firmware parity |
| Extensibility | Low — fixed PGNs | High — capability-driven layers |

---

## 51.12 Decision Matrix

### 51.12.1 Weighting Method

| Criterion | Rationale for Inclusion | Weight |
|-----------|------------------------|--------|
| Compatibility | Avoid breaking deployed rigs during modernization. | 0.30 |
| Safety | Maintain watchdogs and deterministic fallbacks. | 0.25 |
| Maintainability | Reduce bespoke firmware forks and configuration drift. | 0.20 |
| Extensibility | Support new sensors and telemetry layers quickly. | 0.15 |
| Operator Experience | Keep setup approachable for field teams. | 0.10 |
| **Total** |  | **1.0** |

### 51.12.2 Scoring Scale

| Score | Meaning | Qualitative Description |
|-------|---------|-------------------------|
| 1 | Very Poor | Fundamentally unsuited; major blockers. |
| 2 | Poor | Feasible but with unacceptable trade-offs. |
| 3 | Fair | Acceptable with mitigations and roadmap. |
| 4 | Good | Strong fit; limited risk. |
| 5 | Excellent | Ideal path with clear execution plan. |

### 51.12.3 Comparative Scoring

| Consideration | Compatibility | Safety | Maintainability | Extensibility | Operator Experience | Weighted Score |
|---------------|--------------|--------|-----------------|--------------|---------------------|----------------|
| C1 — Status quo PGN routing | 5 | 4 | 2 | 2 | 3 | 3.25 |
| C5 — Modular firmware publishing | 4 | 4 | 4 | 5 | 4 | 4.35 |
| C6 — Linux gateway bridge | 4 | 4 | 3 | 4 | 3 | 3.75 |

> **Informative:** Weighted scores guide prioritization but final decisions require ADR sign-off.
