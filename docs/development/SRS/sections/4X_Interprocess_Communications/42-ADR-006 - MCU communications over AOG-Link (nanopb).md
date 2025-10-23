# 42-ADR-006 — MCU communications over AOG-Link (nanopb)
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
**Related Considerations:** [C1 - Legacy PGN Transport Stewardship](42_Transports.md#c1---legacy-pgn-transport-stewardship), [C3 - Typed Facade & Compatibility Bridge](42_Transports.md#c3---typed-facade--compatibility-bridge)

---

## 1) Context

Nexus needs a unified, typed, and lightweight transport for MCU communications that can operate over Ethernet, RS-485/serial,
or CAN while coexisting with legacy PGN-based modules. Experiments showed that normalizing PGNs alone kept legacy framing but
failed to provide schema evolution or deterministic sequencing across heterogeneous links.【F:docs/development/SRS/sections/4X_Interprocess_Communications/42_Transports.md†L76-L205】

```mermaid
flowchart LR
  A[Legacy UDP/CAN PGNs] --> B[Transport evaluation]
  B --> C[AOG-Link nanopb protocol]
  C --> D[Bridge translation]
```

---

## 2) Decision

Adopt AOG-Link v1, a compact protobuf/nanopb-based datagram protocol, as the standard MCU communications layer.

### Decision Summary

* **Scope:** Host ↔ MCU and MCU ↔ MCU transports across Ethernet/UDP, RS-485/serial, and CAN.
* **Boundary:** Higher-layer typed APIs remain gRPC (ADR-002); PGN compatibility maintained via bridge services.
* **Implementation Level:** Design + code; shared `.proto` schemas compiled via nanopb with transport bindings.

---

## 3) Consequences

**Positive Impacts:**

* Unified schema and type safety across Ethernet, RS-485, CAN, and MCU-to-MCU links.
* Lightweight frames suitable for low-power controllers while compatible with protobuf ecosystems.
* Backward-compatible through bridge translation to existing PGN devices.

**Negative / Mitigated Impacts:**

* Requires firmware updates to emit AOG-Link datagrams — mitigated by staged rollout and bridge shims.
* Introduces a bridge layer during migration — addressed with lightweight host daemons and replay validation.
* Protobuf field discipline must be maintained to preserve nanopb compatibility — enforced via linting and schema review.

**Follow-up Actions:**

* Define `aoglink.v1` protobuf schemas aligned with typed service contracts.
* Implement host and firmware libraries for UDP, RS-485 (COBS + CRC-16), and CAN/CAN-FD bindings.
* Extend bridge logging to surface sequence counters and retransmission events.

---

## 4) Rationale

AOG-Link delivers deterministic headers (`version`, `class`, `type`, `seq`, `src`, `dst`, `len`) and protobuf payloads with
sequence counters and acknowledgements, enabling reliability semantics across all transports. It allows compatibility bridges
to translate between nanopb datagrams, typed gRPC APIs, and legacy PGNs without bespoke codecs.

---

## 5) Alternatives Considered

| Option | Summary | Reason Not Selected |
|--------|---------|---------------------|
| Status quo PGNs | Continue using PGNs for MCU ↔ host messaging. | Lacked schema evolution, sequencing, and lightweight acks. |
| JSON/REST MCU API | Wrap MCU messages in JSON over HTTP. | Payload overhead too large; unsuitable for low-bandwidth links. |
| Vendor-specific CAN stacks | Adopt proprietary CAN SDKs per controller. | Fragments firmware support and sacrifices portability. |

