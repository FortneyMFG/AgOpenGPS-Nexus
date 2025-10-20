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

## Governance Updates

- **Profile registry.** Share and subscribe profiles are versioned artifacts in the manifest bundle; CI blocks deployments that
  omit ACL policies or exceed the per-tier payload budgets defined for radio transports.【F:schemas/ShareProfile.v1.json†L1-L120】【F:schemas/SubscribeProfile.v1.json†L1-L120】
- **Presence accountability.** Presence heartbeats now include session IDs, mounted field sets, and profile hashes so the audit
  trail links mesh events to the authoritative job state emitted by JobsService.【F:schemas/Session.v1.json†L1-L120】【F:docs/ADR/ADR-030-field-job-sessions.md†L13-L96】
- **Key rotation playbook.** RadioBridge integrations must document rolling key rotations and publish test vectors covering
  encryption handshake success/failure paths before an operator bundle can ship.【F:docs/ADR/ADR-048_RadioBridge.md†L17-L60】

## Amendment — 2025 architecture refresh (NX-190)

- Mesh broadcasts include deterministic seeds and layer edit provenance so Zone Drawing undo stacks reconcile edits regardless
  of mesh topology or transport retries.【F:docs/ADR/ADR-044_ZoneDrawingFramework.md†L9-L74】
- Crop, Genetics, Yield, and Profit plugins register analytics windows keyed off mesh presence events to align streaming
  overlays with the same session boundaries used in replay and report builder exports.【F:docs/ADR/ADR-045_CropTypePlugin.md†L9-L96】【F:docs/ADR/ADR-049_YieldPlugin.md†L9-L70】【F:docs/ADR/ADR-050_CostProfitPlugin.md†L9-L70】
- Multi-field envelopes propagate into mesh topic routing so devices receive only the layers relevant to their mounted fields,
  reducing bandwidth and simplifying analytics splits in collaborative jobs.【F:docs/ADR/ADR-043_MultiFieldJobEnvelopes.md†L9-L112】

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

<<<<<<< HEAD
- [Section 42 — Transports](../SRS/sections/4X_Interprocess_Communications/42_Transports.md)
- [Section 91 — UI Shell & Layout](../SRS/sections/9X_Frontends_Ops/91_UI_Shell_Layout.md)
- [Section 64 — Telemetry & Health](../SRS/sections/6X_Core_Domain_Services/64_Telemetry_Health.md)
=======
- [Section 42 — Transports](../SRS/sections/4X/42_Transports.md)
- [Section 91 — UI Shell & Layout](../SRS/sections/9X/91_UI_Shell_Layout.md)
- [Section 64 — Telemetry & Health](../SRS/sections/6X/64_Telemetry_Health.md)
>>>>>>> origin/develop
