# ADR-048 — RadioBridge for ELRS/LoRa Telemetry

- **Status:** Drafting
- **Date:** 2025-03-19
- **Author(s):** Nexus architecture guild
- **NX Task:** NX-190 Comprehensive ADR portfolio review

## Context

Rural deployments frequently rely on low-bandwidth radios (ELRS, LoRa) for inter-machine communication. These transports require
binary framing, retransmission policies, and bandwidth shaping tuned to agricultural operations. The Live Telemetry Mesh (ADR-047)
needs a bridge that adapts mesh topics to radio-friendly frames with predictable latency and resilience.

## Decision

Implement a RadioBridge module that encapsulates binary framing, encryption, acknowledgement, and replay rules for ELRS/LoRa
links. The bridge exposes a pluggable transport interface so additional radio stacks can be supported later without changing mesh
semantics.

### Framing & Reliability

- Frames carry a compact header (`version`, `topicHash`, `payloadType`, `sequence`, `ackId`), CRC16, and compressed payloads.
- Uses selective repeat ARQ with a replay window of 32 packets and adaptive resend intervals based on RSSI/packet loss.
- Supports optional forward error correction blocks when configured for high-loss environments.

### Integration

- Mesh topics map to numeric IDs via a shared registry distributed with the plugin manifest. Devices negotiate supported tiers
  during handshake.
- Encryption optional but recommended: AES-CCM with pre-shared keys stored in device profiles. Authentication failures trigger
  quarantine mode with operator alerts.
- Bridge exposes telemetry counters (latency, retries, drop rate) to the UI for diagnostics.

## Consequences

- Provides deterministic behavior for low-bandwidth collaboration while remaining extensible to future radios.
- Requires provisioning workflows for keys and topic registries.
- Adds complexity to device setup; tooling must simplify configuration.

## Alternatives Considered

1. **Use TCP over cellular modems.** Not reliable enough in remote fields and increases operating costs.
2. **Generic LoRa chat protocols.** Lack tight integration with Nexus topics and provenance requirements.

## Dependencies

- Consumed by ADR-047 Live Telemetry Mesh.
- Coordinates with ADR-050 Profit and ADR-052 Field Health for remote alert delivery when mesh connectivity is available.

## SRS Impact

- Adds radio framing requirements to §03 Communications & Transports.
- Extends §10 Telemetry diagnostics with radio health counters.
- Updates plugin documentation to describe provisioning steps and ACL configuration.

## References

- [Section 03 — Communications & Transports](../SRS/sections/03_Comm_Transports.md)
- [Section 10 — Telemetry & Health](../SRS/sections/10_Telemetry_Health.md)
- [Section 12 — Extensibility & Plugins](../SRS/sections/12_Extensibility_Plugins.md)
