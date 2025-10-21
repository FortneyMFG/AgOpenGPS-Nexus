# ADR-006: MCU communications over AOG-Link (nanopb)

## Status
Accepted

**Relevant Plugin(s):** AgIO Host Services, Device Manager, Autosteer, Section Control, Rate Control, Planter Monitor, ISOBUS Bridge


## Context
Nexus needs a unified, typed, and lightweight transport for MCU communications that can operate over Ethernet, RS-485/serial, or CAN while coexisting with legacy PGN-based modules. The goal is to simplify firmware and host interfaces by standardizing on a single schema and message framing that’s portable across all MCU classes (ESP32, Teensy, STM32, etc.) and consistent with the protobuf contracts used in higher layers.【F:docs/SRS/sections/4X_Interprocess_Communications/42_Transports.md†L60-L112】 Legacy UDP/PGN modules must remain functional, but new Nexus firmware will use a modern datagram approach leveraging the same protobuf definitions used in gRPC contracts, compiled via nanopb for embedded targets.

## Decision
Adopt AOG-Link v1, a compact protobuf/nanopb-based datagram protocol, as the standard MCU communications layer.

- **Common frame header:** Every packet begins with `version`, `class`, `type`, `seq`, `src`, `dst`, and `len` fields followed by the protobuf payload. Serial links append a CRC-16 after the payload. This layout is reused verbatim across UDP and RS-485 and maps naturally onto CAN extended identifiers for filtering.
- **Payload format:** protobuf/nanopb messages compiled from shared `.proto` definitions, tuned for fixed-width numeric fields and small enums so typical payloads remain ≤48 bytes.
- **Transports:**
  - Ethernet/Wi-Fi: UDP multicast (239.10.6.1:16666) for telemetry/events plus directed unicast for command/reply flows.
  - Serial/RS-485: COBS-framed binary packets with CRC-16, supporting 115200–1Mbaud multi-drop with host-directed slotting or token passing.
  - CAN/CAN-FD: Extended 29-bit identifiers structured as `priority (3) | class (2) | type (10) | dest (8) | src (8)` so silicon filters can isolate message classes. CAN-FD single frames carry `[ver][seq][len][flags][protobuf…]`; larger payloads fall back to ISO-TP or a simple fragment header `[ver][seq][frag_idx][frag_cnt][payload…]` flagged in-band.
- **Bridge translation:** AgIO/Bridge services translate between AOG-Link datagrams and the gRPC contracts consumed by Core/UI/Plugins, and between AOG-Link and PGN for legacy modules.
- **Reliability semantics:** Command paths mark `flags.needs_ack` and expect an acknowledgement echoing `class=ack`, `type=originalType`, and `seq`; telemetry remains fire-and-forget but consumers may detect drops via sequence counters.
- **Versioning:** AOG-Link packets carry version headers and message IDs, allowing parallel support for legacy PGNs while negotiating protobuf evolution.
- **MCU-to-MCU:** Direct AOG-Link datagrams support optional speed, rate, heartbeat, and section-state sharing among Nexus modules without involving the host, using the same arbitration (role + priority) scheme applied to host↔MCU traffic.

## Consequences

### Positive
- Unified schema and type safety across all MCU links.
- Lightweight enough for low-power controllers while compatible with the main protobuf ecosystem.
- Works identically across Ethernet, RS-485, and CAN without extra stacks.
- Backward-compatible through Bridge translation to existing PGN UDP devices.

### Negative / mitigated
- Requires new firmware for existing UDP modules to gain AOG-Link support.
- Introduces another layer (Bridge) during migration; mitigated by a lightweight host daemon.
- Protobuf field discipline must be maintained to preserve nanopb compatibility.

## Follow-up actions
- Define `aog-link.proto` schemas under `Aog.Link.V1`, reusing message IDs aligned with gRPC contracts.
- Implement AOG-Link drivers in AgIO/Bridge for:
  - Ethernet (UDP multicast/unicast with retry logic for commands)
  - RS-485/serial (COBS + CRC framing and slot/token scheduling)
  - CAN/CAN-FD (AOG-CAN header mapping plus ISO-TP or lightweight fragmentation)
- Extend the Bridge service to translate:
  - gRPC ⇄ AOG-Link (nanopb datagrams)
  - AOG-Link ⇄ PGN UDP (legacy)
- Update SRS “03 — Communications & Transports” with the new protocol architecture and example frames.
- Add firmware tasks for:
  - nanopb integration and packet encoder/decoder
  - AOG-Link discovery/heartbeat/time-sync support (including multicast announce and 1 Hz heartbeats)
  - Direct MCU↔MCU sharing (speed, rate, sections with stale-source handling)
- Deprecate PGN expansion in favor of static legacy maintenance.

## Governance Updates
- **Compliance kit.** Firmware partners receive a nanopb generator bundle, loopback transport simulator, and packet-capture fixtures that mirror production line noise. Certification requires passing the automated suite and publishing logs before hardware ships.
- **Interoperability tiers.** Deployment guides now classify controllers as Legacy PGN-only, Hybrid, or Full AOG-Link. Each tier lists supported capabilities, fallback behaviors, and upgrade prerequisites so operators can plan migrations.
- **Support cadence.** Annual interoperability summits review firmware updates, transport findings, and telemetry from the field. Findings convert into backlog tasks with explicit owners and due dates.

## Legacy Implementation Notes
### AgOpenGPS v6
- MCU and host communications ride on the classic PGN frame (0x80/0x81 header, CRC trailer) across UDP and serial links, so firmware today exchanges fixed-width byte payloads without protobuf schemas.【F:docs/SRS/references/AgIO_PGN_Baseline.md†L1-L24】

### Legacy Dev Branch
- Dev experiments focus on normalizing those same PGNs—including SocketCAN bridges—but still depend on the legacy framing rather than nanopb-based datagrams.【F:docs/SRS/sections/4X_Interprocess_Communications/42-O6%20-%20PGN%20compatibility%20bridge%20layered%20over%20new%20APIs.md†L7-L36】

## References
- [Section 42 — Transports](../SRS/sections/4X_Interprocess_Communications/42_Transports.md)
- [Option O-COMM-6 — PGN compatibility bridge](../SRS/sections/4X_Interprocess_Communications/42-O6%20-%20PGN%20compatibility%20bridge%20layered%20over%20new%20APIs.md)
- [ADR-002 — Expose Nexus services over gRPC/protobuf contracts](ADR-002-grpc-contracts.md)
