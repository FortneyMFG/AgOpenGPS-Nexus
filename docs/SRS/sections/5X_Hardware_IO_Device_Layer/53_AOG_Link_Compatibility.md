# 53 — AOG-Link Compatibility
*(Status: Proposed)*

**Author:** Codex
**Created:** 2025-10-20
**Status:** Proposed
**Version:** 0.1.0
**Section ID:** 53
**Editors:** Hardware & IO Working Group
**Last Updated:** 2025-10-20
**Related Sections:** 51 — Sensor & Actuator Abstractions, 54 — CM5 Integrated Controller, 6X — Core Domain Services
**Upstream Dependencies:** 2X — System Architecture, 4X — Interprocess Communications
**Downstream Impacts:** 7X — Mapping & Geospatial, 8X — Guidance, 9X — Frontends & Ops

---

## 53.1 Purpose & Scope

Define the interoperability requirements for AOG-Link v1 (“AOG-Link/Next”) while maintaining compatibility with legacy UDP PGN workflows.
The scope covers protocol structure, message catalogue, transport bindings, bridge behavior, and capability negotiation necessary for Core, AgIO, firmware, and CM5 integrations.

---

## 53.2 Context

- Legacy hardware exchanges PGN-based UDP frames (“AOG-Link v0”) with limited type information and no version negotiation.
- Modern deployments require a transport-agnostic framing that can operate across UDP, USB-CDC serial, CAN(FD), and MQTT fan-out without breaking latency budgets.
- Shared protobuf schemas simplify integration with Core/AgIO plugins and enable deterministic simulation and replay.
- Security hardening remains out of scope for the initial release; deployments rely on existing network isolation and permission scopes from Sections 51 and 52.

---

## 53.3 Legacy Comparison

| Area / Theme | Legacy Behavior | Identified Limitation | Modernization Opportunity | Reference / Source |
|--------------|-----------------|-----------------------|---------------------------|--------------------|
| Message Semantics | Raw PGN payloads per transport. | Requires bespoke parsers; hard to extend. | Single protobuf package (`aoglink.v1`) shared across transports. | Protocol draft【F:docs/SRS/sections/5X_Hardware_IO_Device_Layer/53_AOG_Link_Compatibility.md†L1-L160】 |
| Transport Support | UDP-exclusive framing. | Cannot reuse across USB, CAN(FD), or MQTT. | Unified 8-byte header + CRC enabling multi-transport bindings. | Protocol draft【F:docs/SRS/sections/5X_Hardware_IO_Device_Layer/53_AOG_Link_Compatibility.md†L33-L120】 |
| Capability Negotiation | Static firmware behavior. | No versioning or feature discovery. | Semver negotiation via `MGMT.Hello` and role advertisement. | Protocol draft【F:docs/SRS/sections/5X_Hardware_IO_Device_Layer/53_AOG_Link_Compatibility.md†L61-L120】 |

---

## 53.4 Definitions

| Term | Definition |
|------|------------|
| AOG-Link v0 / Classic | Existing UDP datagrams currently deployed in legacy rigs. |
| AOG-Link v1 / Next | Transport-agnostic protocol defined in this section. |
| Service (`svc`) | One-byte namespace grouping related RPC-style commands. |
| Method (`mth`) | One-byte identifier for a specific command/telemetry message inside a service. |
| Role | Node’s advertised function: `HOST`, `CTRL`, `SENSOR`, `ACTUATOR`, `BRIDGE`. |
| Frame | Header + payload unit transported over supported bindings, optionally followed by CRC. |

> **Requirement Grammar (RFC-2119):**
> - **MUST / MUST NOT** = mandatory requirements.
> - **SHOULD / SHOULD NOT** = strong recommendations with waiver process.
> - **MAY** = optional capabilities or roadmap items.

---

## 53.5 Requirements

| ID | Priority | Category | Summary | Source / C-IDs | Key Metrics / Verification |
|----|----------|----------|---------|-----------------|-----------------------------|
| R-AOGL-000 | MUST | Message Catalogue | Provide shared protobuf package (`aoglink.v1`) with semver governance for all transports. | NX-115 protocol charter | Schema CI ensures backward-compatible proto evolution. |
| R-AOGL-001 | MUST | Transport Framing | Use fixed 8-byte header + optional CRC to support UDP, serial, CAN(FD), and MQTT fan-out. | Protocol draft | Golden-frame fixtures validated across bindings. |
| R-AOGL-002 | MUST | Latency Budget | Maintain <2 ms p50 steer/control latency across UDP and shared-memory bridges. | CM5 integration SRS | Bench harness measures latency under load. |
| R-AOGL-003 | SHOULD | Capability Negotiation | Advertise roles, feature sets, and semantic versions via `MGMT.Hello`. | Firmware roadmap | Compatibility tests verify downgrade paths. |
| R-AOGL-004 | MUST | Legacy Bridging | Provide bridge behaviors that translate between v0 PGNs and v1 payloads without data loss. | AgIO bridge requirements | Regression suite covers PGN↔protobuf translations. |
| R-AOGL-005 | SHOULD | Health Telemetry | Report per-transport counters, RTT, drop counts, and authority state via `MGMT.Health`. | Telemetry needs | Health dashboards display counters within tolerance. |
| R-AOGL-006 | SHOULD | Error Handling | Define negative acknowledgements and retry semantics for firmware flashing and control loops. | Firmware update SRS | NACK/resend behavior validated in simulator harness. |
| R-AOGL-007 | COULD | Security Envelope | Reserve fields/hooks for future authentication without breaking payload layouts. | Security backlog | Placeholder fields documented; no enforcement in v1. |

### 53.5.1 Requirement Sources & Rationale

| Req ID | Source | Rationale |
|--------|--------|-----------|
| R-AOGL-000 — R-AOGL-002 | NX-115 scope & CM5 latency targets | Ensure deterministic control paths while unifying schema definitions. |
| R-AOGL-003 — R-AOGL-006 | Firmware + telemetry backlog | Support evolvable firmware and diagnostics across deployments. |
| R-AOGL-007 | Security backlog | Preserve forward-compatibility for future hardening. |

---

## 53.6 Architecture Overview

- **Service Map:** Payloads reside in `aoglink.v1` protobuf package. Nanopb powers MCU builds; .NET/Go/C++ toolchains serve hosts.
- **Role Discovery:** `MGMT.Hello` advertises `roles_mask`, firmware version, supported transports, and optional capabilities for semver negotiation.
- **Health & Authority:** `MGMT.Health` streams counters, RTT, voltage/temperature, and authority status to aid troubleshooting during control anomalies.
- **Bridge Function:** Dedicated nodes translate between v0 PGNs and v1 payloads to protect legacy rigs while enabling incremental rollout.

### 53.6.1 Message Catalogue

| Service ID | Name | Methods (examples) | Notes |
|------------|------|--------------------|-------|
| 0x01 | NAV | `SetSteerTarget`, `SetABLine`, `Pause`, `Resume` | Time-critical navigation + guidance commands. |
| 0x02 | SENSORS | `GpsFix`, `ImuSample`, `WheelTicks` | Sensor telemetry with optional batching. |
| 0x03 | CTRL | `SteerStatus`, `SectionStatus` | Actuator state feedback mirrored to Core/UI. |
| 0x04 | MGMT | `Hello`, `Health`, `FwChunk`, `Ack`, `Nack` | Negotiation, health, firmware update flows. |
| 0x05 | AUX (reserved) | `AuxCommand`, `AuxStatus` | Placeholder for ISOBUS/auxiliary controllers. |

### 53.6.2 Frame Format

Every frame begins with an 8-byte header followed by the protobuf payload and, when required, a CRC-16-CCITT trailer.

```
+--------+-----+-------+---------+---------+------+------+
| PFX    | VER | FLAGS | MSG_ID  | LEN     | SVC  | MTH  |
| 0xA5   | 0x1 | 1 b   | 2 bytes | 2 bytes | 1 b  | 1 b  |
+--------+-----+-------+---------+---------+------+------+
```

- **PFX** — Sync byte shared across transports.
- **VER** — Wire-format major version (0x1 for v1.x.y). Frames with mismatched major version MUST be rejected.
- **FLAGS** — Bitfield capturing fragmentation, acknowledgements, and priority. Upper nibble used for CAN(FD) segmentation.
- **MSG_ID** — Monotonic counter per session enabling dedupe/replay protection.
- **LEN** — Payload length in bytes (excluding header/CRC).
- **SVC/MTH** — Identify command/telemetry tuple.

Serial and CAN(FD) bindings append CRC-16-CCITT; UDP MAY omit CRC when underlying transport already provides checksums.

### 53.6.3 Capability Negotiation & Roles

1. Nodes send `MGMT.Hello` with `roles_mask`, firmware semver, supported transports, and feature flags.
2. Peers respond with compatibility result (`OK`, `DOWNLEVEL`, `UNSUPPORTED`).
3. Optional `FeatureEnable` messages coordinate advanced behaviors (e.g., firmware chunk compression).
4. Authority negotiation leverages retained MQTT topics or shared memory tokens per Section 54 when CM5 fast paths are active.

### 53.6.4 Transport Bindings

| Transport | Encoding Details | Notes |
|-----------|------------------|-------|
| UDP | Native frames with optional CRC. | Primary deployment path; retains compatibility with legacy ports. |
| USB-CDC Serial | Frames wrapped with CRC-16 and optional COBS encoding. | Ensures resilience on noisy links. |
| CAN(FD) | Header/payload segmented into 64-byte chunks with continuation flags. | Maintains deterministic latency for steering loops. |
| MQTT | Binary frames published to retained topics (`aoglink/{svc}/{mth}`). | Enables fan-out to dashboards; limited to non-real-time use. |
| Shared Memory (CM5) | Frames bypassed for steer setpoints, using ring buffer per Section 54. | Maintains <2 ms p50 latency while mirroring to MQTT. |

---

## 53.7 Operational Guidance

- Maintain dual-stack deployments (v0 + v1) until field validation confirms parity for steer and section loops.
- Reserve MSG_ID ranges per controller to avoid collisions when multiple transports coexist.
- Mirror key control payloads onto MQTT topics for observability, even when shared-memory fast paths handle actuation.
- Provide firmware flashing and diagnostics through `MGMT.FwChunk`/`Ack` flows, leveraging Section 55 DFU orchestration.

---

## 53.8 Risks & Open Issues

| ID | Description | Impact | Mitigation / Status | Owner |
|----|-------------|--------|---------------------|-------|
| RISK-53-1 | Transport fragmentation errors on CAN(FD) cause control jitter. | High | Extend hardware-in-loop testing; add sequence enforcement in firmware. | @hardware-wg |
| RISK-53-2 | Legacy PGN translation omits rare fields, breaking backward compatibility. | Medium | Maintain PGN↔proto fixtures and regression logs. | @agio |
| RISK-53-3 | MQTT fan-out introduces stale telemetry if not rate-limited. | Low | Publish sampling guidance; enforce TTL on retained topics. | @ops |
| ISSUE-53-1 | Define long-term security/auth strategy for v1 frames. | Medium | Coordinate with security backlog and Section 95. | @security |
| ISSUE-53-2 | Clarify firmware chunk size limits across transports. | Low | Document defaults and allow negotiation in `MGMT.Hello`. | @firmware |

---

## 53.9 Design Considerations

| ID | Consideration | Description |
|----|----------------|-------------|
| C1 | Dual-stack rollout | Run v0 and v1 concurrently with bridges to protect existing rigs during migration. |
| C2 | Transport prioritization | Choose which transports handle real-time loops vs. observability fan-out to balance latency and reach. |
| C3 | Semver governance | Define how minor/patch releases add services while keeping MCU firmware lightweight. |
| C4 | Security roadmap | Plan for authentication and encryption hooks without breaking current deployments. |
| C5 | Firmware tooling | Align protobuf generation, Nanopb configs, and CI pipelines across vendors. |

### 53.9.1 Assumptions & Preconditions

- [A1] Firmware teams adopt protobuf tooling and commit to semver negotiation semantics.
- [A2] AgIO bridge services remain online during migration to mediate between v0 and v1 nodes.
- [A3] Shared telemetry infrastructure handles additional health metrics without saturating storage.

---

## 53.10 Option Overview

| Option ID | Status | Type / Theme | Description | Reference Document |
|-----------|--------|--------------|-------------|--------------------|
| — | — | — | No standalone option documents retained for Section 53 after rebaseline; refer to §53.9 for considerations. | — |

---

## 53.11 Comparison Matrix

| Attribute / Criteria | AOG-Link v0 | AOG-Link v1 |
|----------------------|-------------|-------------|
| Message Semantics | Fixed PGNs | Protobuf catalogue |
| Transport Coverage | UDP only | UDP, Serial, CAN(FD), MQTT |
| Latency Control | Medium — manual tuning | High — shared header + prioritization |
| Extensibility | Low — bespoke updates | High — semver + feature flags |
| Observability | Low — limited metadata | High — structured health telemetry |

---

## 53.12 Decision Matrix

| Consideration | Compatibility | Safety | Maintainability | Extensibility | Weighted Score |
|---------------|--------------|--------|-----------------|--------------|----------------|
| C1 — Dual-stack rollout | 5 | 4 | 3 | 3 | 3.85 |
| C2 — Transport prioritization | 4 | 5 | 3 | 4 | 3.95 |
| C3 — Semver governance | 4 | 4 | 4 | 5 | 4.35 |
| C4 — Security roadmap | 3 | 4 | 4 | 4 | 3.75 |
| C5 — Firmware tooling | 4 | 4 | 5 | 4 | 4.25 |

> **Informative:** Weighted scores assume compatibility 0.25, safety 0.25, maintainability 0.20, extensibility 0.20, operator experience 0.10.
