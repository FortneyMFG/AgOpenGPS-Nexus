# 55 — Firmware Interfaces & Updates
*(Status: Proposed)*

**Authors:** Nexus Team (Codex)
**Created:** 2025-10-20
**Status:** Proposed
**Version:** 0.1.0
**Section ID:** 55
**Editors:** Hardware & IO Working Group
**Last Updated:** 2025-10-20
**Related Sections:** 51 — Sensor & Actuator Abstractions, 52 — AgIO Service, 53 — AOG-Link Compatibility
**Upstream Dependencies:** 2X — System Architecture, 4X — Interprocess Communications
**Downstream Impacts:** 7X — Mapping & Geospatial, 9X — Frontends & Ops, Plugin SDKs

---

## 55.1 Purpose & Scope

Define the discovery, notification, and execution flows that keep heterogeneous firmware fleets (AVR, ESP32, Teensy, STM32, CAN controllers) up to date with verified binaries.
Scope includes device identity broadcasts, catalog governance, orchestrator behavior, updater plugins, and UI/UX expectations for safe operator workflows.

---

## 55.2 Context

- Operators deploy mixed hardware with ad-hoc flashing tools and inconsistent provenance tracking.
- Firmware updates must span multiple transports (serial, UDP/OTA, CAN bootloaders) while respecting safety policies and operator consent.
- AgIO and plugin architecture provide natural surfaces for enumerating devices, coordinating updaters, and mirroring health telemetry.
- Offline or bandwidth-constrained scenarios require support for staged channels and portable bundles without sacrificing verification.

---

## 55.3 Legacy Comparison

| Area / Theme | Legacy Behavior | Identified Limitation | Modernization Opportunity | Reference / Source |
|--------------|-----------------|-----------------------|---------------------------|--------------------|
| Device Discovery | Manual identification per controller. | Difficult to track firmware provenance. | Periodic identity frames + JSON “Device Hello”. | Legacy notes【F:docs/sections/5X_Hardware_IO_Device_Layer/55_Firmware_Interfaces_Updates.md†L19-L60】 |
| Update Tooling | Vendor-specific utilities (avrdude, esptool, etc.). | Fragmented UX; risky for operators. | Unified orchestrator with plugin-provided updaters. | DFU overview【F:docs/sections/5X_Hardware_IO_Device_Layer/55_Firmware_Interfaces_Updates.md†L31-L110】 |
| Safety Controls | Manual procedures. | No consistent verification or rollback. | Signed catalogs, checksums, safety pre-checks, and staged channels. | DFU expectations【F:docs/sections/5X_Hardware_IO_Device_Layer/55_Firmware_Interfaces_Updates.md†L43-L160】 |

---

## 55.4 Definitions

| Term | Definition |
|------|------------|
| Device Identity Frame | Periodic broadcast (PGN 0xD4 or JSON) containing vendor, product, MCU, firmware version, and capability flags. |
| DFU Catalog | Signed JSON manifest listing firmware assets, rollout channels, compatibility rules, and signatures. |
| Update Plan | Structured record chosen by orchestrator describing device, target firmware, method, and pre-check requirements. |
| Updater Plugin | Out-of-process component implementing flashing workflows per MCU/transport. |

> **Requirement Grammar (RFC-2119):**
> - **MUST / MUST NOT** = mandatory requirements.
> - **SHOULD / SHOULD NOT** = strong recommendations with waiver process.
> - **MAY** = optional capabilities or roadmap items.

---

## 55.5 Requirements

| ID | Priority | Category | Summary | Source / C-IDs | Key Metrics / Verification |
|----|----------|----------|---------|-----------------|-----------------------------|
| DFU-001 | MUST | Discovery | Emit runtime device identity including vendor, model, MCU, firmware version, and capabilities over all transports. | Firmware identity spec | CI fixtures validate identity frames across PGN/JSON formats. |
| DFU-002 | MUST | UX & Consent | Present update notifications with changelog, compatibility, and pre-checks; require explicit operator confirmation. | UI policy | UI automation ensures confirmation gating before flashing. |
| DFU-003 | MUST | Safety Controls | Enforce signature/checksum verification, power/link guardrails, and bounded timeouts before flashing. | Safety guidelines | Negative tests fail when verification missing or timeouts exceeded. |
| DFU-004 | MUST | Transport Coverage | Support OTA (UDP/Wi-Fi), serial/USB, CAN bootloader, and DFU flows via consistent APIs. | Hardware matrix | Integration suite exercises each transport path. |
| DFU-005 | MUST | Orchestrator | Maintain transactional update plans with rollback states and progress telemetry. | AgIO orchestrator design | Replay of failed updates leaves device in safe state. |
| DFU-006 | SHOULD | Rollout Channels | Offer staged channels (stable/beta) and allow device pinning to chosen channel/version. | Release governance | Catalog schema exposes channels; orchestrator honors overrides. |
| DFU-007 | SHOULD | Plugin Extensibility | Allow third-party plugins to register updaters without modifying core logic. | Plugin SDK | Sample plugin demonstrates registration + execution. |
| DFU-008 | COULD | Offline Bundles | Support portable update bundles with identical verification artifacts. | Field feedback | Offline bundle test passes signature + checksum verification. |
| DFU-009 | SHOULD | Telemetry | Stream update progress and result metrics to dashboards and logs. | Ops requirements | Telemetry pipeline records success/failure events. |

### 55.5.1 Requirement Sources & Rationale

| Req ID | Source | Rationale |
|--------|--------|-----------|
| DFU-001 — DFU-005 | Firmware WG + safety governance | Ensure updates are discoverable, verifiable, and fail-safe. |
| DFU-006 — DFU-008 | Release & field feedback | Provide flexible rollout strategies for diverse deployments. |
| DFU-009 | Ops requirements | Maintain observability for audits and troubleshooting. |

---

## 55.6 Architecture Overview

1. **Device Identity Broadcast:** Modules emit identity frames at 1–2 Hz with capability flags, health indicators, and optional JSON payload for richer diagnostics.
2. **Catalog Retrieval:** Core fetches signed DFU catalog (Ed25519) listing firmware assets, compatible hardware, rollout channels, and instructions. Hashes and optional signatures accompany binary assets.
3. **Update Orchestrator:** AgIO/Core compares discovered devices to catalog entries, generates `UpdatePlan`, and surfaces “Update available” prompts with safety pre-checks.
4. **Updater Plugins:** Specialized executables (avrdude, esptool, teensy_loader_cli, dfu-util, CAN bootloaders) execute flashing sequences via gRPC, streaming progress states and telemetry.
5. **UI & UX:** Device list surfaces firmware version, health status, update badges, and guided flows with snooze/remind options; notifications remain non-blocking but persistent.

### 55.6.1 Identity Frame Details

- **Binary PGN 0xD4 Layout:**
  - byte0 `vendorId`, byte1 `productId`, byte2 `variantId`, byte3 `mcuId`
  - byte4 upper nibble `fwMajor`, lower nibble `fwMinor`; byte5 `fwPatch`
  - byte6 capability bitfield (OTA, CAN boot, USB DFU, dual-bank, voltage telemetry, reserved bits)
  - byte7 health flags (voltage low, thermal warning, uptime bucket)
- **Extended Device Hello (optional):** JSON payload containing `fwVersion`, `bootloader`, `hwVersion`, `api`, `serial`, `capabilities`, and power metrics.

### 55.6.2 Supported Update Methods

| Method | Typical MCU | Transport | Tooling | Notes |
|--------|-------------|-----------|---------|-------|
| Serial AVR | ATmega328p/ATmega2560 | USB CDC | `avrdude` (stk500/Caterina) | Requires reset/boot button guidance. |
| Teensy HID | MK20/i.MX RT | USB HID | `teensy_loader_cli` | Prefers external power; verify after flash. |
| ESP32 OTA | ESP32 | Wi-Fi/UDP/TCP | ESP-IDF OTA | Auto-select when OTA capability advertised. |
| ESP32 Serial | ESP32 | USB UART | `esptool.py` | Fallback when OTA unavailable or fails verification. |
| STM32 DFU | STM32 | USB DFU | `dfu-util` | Handles DFU detach/attach cycles and CRC checks. |
| CAN Bootloader | STM32/other CAN MCUs | CAN / ISO-TP / UDS | Vendor tools / SocketCAN helpers | Requires deterministic CAN session management. |
| Offline Bundle | Any | USB storage | Catalog-driven orchestrator | Optional; verifies signatures before flash. |

---

## 55.7 Operational Guidance

- Require operators to review changelog and safety checklist before confirming updates; provide printable checklists for field work.
- Log every update attempt (success, failure, cancel) with device identity, firmware version, operator, and timestamp for audit.
- Provide throttling and scheduling controls for OTA updates to avoid saturating shared field networks during peak operations.
- Maintain rollback plans and stored prior firmware packages for critical controllers.

---

## 55.8 Risks & Open Issues

| ID | Description | Impact | Mitigation / Status | Owner |
|----|-------------|--------|---------------------|-------|
| RISK-55-1 | Firmware flashing interrupts active field work causing downtime. | Medium | Offer scheduling windows and pause/resume workflows. | @ops |
| RISK-55-2 | Catalog compromise distributes malicious firmware. | High | Enforce signature verification, publish revocation steps. | @security |
| RISK-55-3 | Operators skip safety pre-checks leading to bricked devices. | Medium | Embed mandatory checklist, provide simulator training. | @hardware-wg |
| ISSUE-55-1 | Determine retention policy for historical firmware binaries. | Low | Coordinate with release governance. | @release |
| ISSUE-55-2 | Clarify process for third-party vendors to submit signed firmware. | Medium | Extend catalog schema with vendor signatures + approvals. | @hardware-wg |

---

## 55.9 Design Considerations

| ID | Consideration | Description |
|----|----------------|-------------|
| C1 | Identity cadence | Balance broadcast frequency against bandwidth constraints for large fleets. |
| C2 | Catalog hosting | Decide between community-hosted catalogs vs. vendor-specific endpoints with mirroring. |
| C3 | Offline workflows | Support USB/offline bundles for dealers without reliable connectivity. |
| C4 | Plugin governance | Define certification requirements for third-party updater plugins. |
| C5 | Safety automation | Automate as many pre-checks as possible while preserving operator oversight. |

### 55.9.1 Assumptions & Preconditions

- [A1] Vendors provide signed firmware assets aligned with catalog schema.
- [A2] Operators have access to required transports (serial cables, Wi-Fi, CAN adapters) during maintenance windows.
- [A3] UI surfaces integrate update APIs from AgIO without bypassing consent workflows.

---

## 55.10 Option Overview

| Option ID | Status | Type / Theme | Description | Reference Document |
|-----------|--------|--------------|-------------|--------------------|
| — | — | — | No standalone option documents retained for Section 55 after rebaseline; see §55.9 for considerations. | — |

---

## 55.11 Comparison Matrix

| Attribute / Criteria | Ad-hoc Vendor Tools | Nexus Orchestrated Updates |
|----------------------|---------------------|---------------------------|
| Discoverability | Low | High — automatic identity frames |
| Safety | Low — manual processes | High — signatures, pre-checks, rollback |
| Operator Effort | High — multiple tools | Medium — guided flows |
| Observability | Low | High — telemetry + audit logs |
| Extensibility | Low — vendor-specific | High — plugin architecture |

---

## 55.12 Decision Matrix

| Consideration | Compatibility | Safety | Maintainability | Extensibility | Weighted Score |
|---------------|--------------|--------|-----------------|--------------|----------------|
| C1 — Identity cadence | 4 | 4 | 3 | 3 | 3.55 |
| C2 — Catalog hosting | 4 | 4 | 4 | 4 | 4.00 |
| C3 — Offline workflows | 4 | 3 | 3 | 4 | 3.55 |
| C4 — Plugin governance | 3 | 4 | 4 | 4 | 3.75 |
| C5 — Safety automation | 3 | 5 | 4 | 3 | 3.95 |

> **Informative:** Weighted scores assume compatibility 0.25, safety 0.30, maintainability 0.20, extensibility 0.15, operator experience 0.10.

---

## Section Change Log

| Date | Summary | Author | PR / Issue |
|------|---------|--------|------------|
| 2025-10-20 | Initial draft | Nexus Team (Codex) |  |

