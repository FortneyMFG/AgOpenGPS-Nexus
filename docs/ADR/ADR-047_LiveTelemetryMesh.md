# ADR-047 — Live Telemetry Mesh

- **Status:** Drafting
- **Date:** 2025-03-19
- **Author(s):** Nexus architecture guild
- **NX Task:** NX-190 Comprehensive ADR portfolio review

## Context

Multi-machine collaboration requires low-latency sharing of presence, coverage, and layer updates across tractors, sprayers, and
scouts working the same job. Existing approaches rely on ad-hoc TCP servers or manual file syncing, which fail in rural network
conditions and lack access control. Nexus needs a mesh-friendly transport that integrates with plugin context, respects privacy,
and feeds UI overlays in real time.

## Decision

Create a Live Telemetry Mesh plugin composed of two parts: a pub/sub overlay (`LiveMeshService`) using topics
`aog/live/{season}/{job}/{layer}` and a RadioBridge module for ELRS/LoRa transport. Devices publish presence heartbeats, trails,
coverage tiles, and select layer deltas over the mesh. Subscription profiles define which data tiers a device consumes.

### Entities & Topics

- **Device** — Immutable ID, human-readable label, hardware capabilities, and permissions.
- **Presence** — Online/offline state, pose, and session metadata broadcast at 2 Hz on `.../presence` topics.
- **ShareProfile** — Declares which data tiers (Presence, Trails, Coverage, Layers) the device publishes per job.
- **SubscribeProfile** — Filters inbound tiers and layers; stored per-device with ACL checks.
- Topics follow `aog/live/{seasonId}/{jobId}/{layerNamespace}`; layer namespace may be `coverage`, `trail`, `zone`, or plugin
  specific (e.g., `cropType.actual`).

### QoS & Reliability

- Presence tier uses UDP-like broadcast with expiry timers (stale after 5 seconds).
- Coverage and layer deltas support QoS 1 semantics with replay window; acknowledgements handled by RadioBridge when operating
  over constrained links.
- Privacy enforced through ACLs tied to Share/Subscribe profiles; encrypted payloads on radio links when keys provisioned.

### Integration Points

- Plugins (Yield, Profit, Field Health) may opt into layer replication by registering with the mesh service.
- UI renders remote machines with icons, stale indicators, and trail polylines. Operators can subscribe/unsubscribe layers per
device.
- The mesh service records incoming deltas via `LayerEditEvent` journals for audit and offline replay.

## Consequences

- Enables collaborative operations with deterministic context sharing.
- Introduces complexity in QoS management and access control; requires tooling to manage profiles and keys.
- Necessitates careful bandwidth budgeting for low-rate radios.

## Alternatives Considered

1. **Centralized MQTT broker.** Rejected due to dependency on backhaul connectivity and single-point failure for offline farms.
2. **File sync via cloud storage.** Too slow and lacks live presence information.

## Dependencies

- Relies on ADR-040/041/043 context broadcasts to scope topics.
- RadioBridge details defined in ADR-048.
- Mesh data consumed by plugins from ADR-045–ADR-053.

## SRS Impact

- Updates §03 Communications & Transports with mesh QoS, topics, and ACL rules.
- Extends §10 Telemetry with presence/trail expectations and stale indicators.
- Adds plugin documentation requirements for share/subscribe UI in §05 Frontends.

## References

- [Section 03 — Communications & Transports](../SRS/sections/03_Comm_Transports.md)
- [Section 05 — Frontends](../SRS/sections/05_Frontends.md)
- [Section 10 — Telemetry & Health](../SRS/sections/10_Telemetry_Health.md)
