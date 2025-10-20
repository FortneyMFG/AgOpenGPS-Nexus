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

## Governance Updates

- **Topic ID escrow.** Topic hash registries are promoted through the manifest governance program; releases must attach signed
  registry diffs and replay fixtures covering all advertised tiers before publication.【F:docs/ADR/ADR-031-official-plugin-bundle.md†L17-L70】
- **Firmware compliance.** Transport adapters include compatibility manifests listing supported firmware revisions and
  handshake features; CI rejects builds that downgrade retry windows or omit selective-repeat coverage tests.【F:docs/ADR/ADR-047_LiveTelemetryMesh.md†L21-L70】
- **Security rotations.** Key provisioning docs mandate quarterly rotation rehearsals with captured telemetry proving old keys
  are revoked and new ones sync across the fleet without breaking mesh connectivity.【F:schemas/Device.v1.json†L1-L120】

## Amendment — 2025 architecture refresh (NX-190)

- Envelope-aware throttling ensures multi-field jobs prioritize field-local deltas first, reducing congestion when multiple
  plugins publish edits simultaneously.【F:docs/ADR/ADR-043_MultiFieldJobEnvelopes.md†L9-L112】
- Session metadata hashed into frame headers lets replay tools stitch radio captures back to specific job timelines without
  manual bookkeeping.【F:schemas/Session.v1.json†L1-L120】
- Weather, Field Health, and Profit alerts inherit the same retry policies as coverage traffic so operator notifications remain
  consistent even when bandwidth drops during collaborative edits.【F:docs/ADR/ADR-052_FieldHealthPlugin.md†L9-L66】【F:docs/ADR/ADR-050_CostProfitPlugin.md†L9-L70】

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

- [Section 42 — Transports](../SRS/sections/4X_Interprocess_Communications/42_Transports.md)
- [Section 64 — Telemetry & Health](../SRS/sections/6X_Core_Domain_Services/64_Telemetry_Health.md)
- [Section 94 — Extensibility, Packaging & Updates](../SRS/sections/9X_Frontends_Ops/94_Extensibility_Packaging_Updates.md)
