# 42 — Transports
*(Status: Proposed)*

**Author:** Codex
**Created:** 2025-10-20
**Status:** Proposed
**Version:** 0.1.0
**Section ID:** 42
**Editors:** Interprocess Communications Working Group
**Last Updated:** 2025-10-20
**Related Sections:** 21 — System Decomposition & Boundaries, 53 — AOG-Link Compatibility, 61 — Kinematics & Pose Fusion
**Upstream Dependencies:** 1X — Platform Foundations, 2X — System Architecture
**Downstream Impacts:** 5X — Hardware IO Device Layer, 6X — Core Domain Services, 7X — Mapping & Geospatial

---

## 42.1 Purpose & Scope

Define how field devices, guidance engines, and remote clients exchange data across serial, UDP, CAN, radio, and higher-level
transports with resiliency, deterministic timing, and observability. This section governs transport selection, bridging
strategies, sequencing requirements, and telemetry mesh expectations for both legacy PGN flows and emerging typed APIs.

---

## 42.2 Context

- AgIO manages serial and UDP PGNs that power steer, section, and telemetry flows today.
- Linux pilots require gRPC/WebSocket facades while maintaining byte-level parity with legacy transports.
- Variable-rate controllers and analytics expect schema negotiation, sequencing, and integrity checks on layer data.
- Remote operations demand secure channels, mesh sharing, and bandwidth-aware throttling across heterogeneous links.

---

## 42.3 Legacy Comparison

| Area / Theme | Legacy Behavior | Identified Limitation | Modernization Opportunity | Reference / Source |
|---------------|-----------------|------------------------|---------------------------|--------------------|
| Serial/UDP PGNs | Fixed PGN framing via AgIO utilities. | No sequencing beyond ad-hoc counters; limited schema awareness. | Introduce versioned PGNs with registry handshakes and retries. | AgIO toolchain |
| Transport Facades | Windows-first UDP/serial bridging. | Lacks typed APIs for Linux/headless deployments. | Layer gRPC/WebSocket facade with deterministic PGN bridge. | Linux pilot reports |
| Telemetry Sharing | Ad-hoc UDP multicast and file export. | No authenticated mesh or throttled radio support. | Build share profiles, mesh QoS, and RadioBridge abstraction. | Community telemetry backlog |

> **Informative:** Captures historical context and modernization drivers.

---

## 42.4 Definitions

| Term | Definition |
|------|-------------|
| AOG-Link | Nanopb-based datagram protocol for MCU ↔ host communications defined in Section 53.
| PoseStream | Authoritative stream of pose, velocity, and zone mask samples consumed across services.
| Share Profile | Operator policy describing which telemetry topics exit a cab and what external data is ingested.
| RadioBridge | Abstraction over ELRS, LoRa, XBee, or similar radios with acknowledgement and replay semantics.

---

> **Requirement Grammar (RFC-2119):**
> - **MUST / MUST NOT** = mandatory; test must exist.
> - **SHOULD / SHOULD NOT** = strong recommendation; justify exceptions.
> - **MAY** = optional; document enabling conditions.
>
> **Clarity Checklist:** Avoid weak words: *fast, robust, user-friendly, handle, support, adequate,* etc.
> Prefer measurable forms: *“≤ 250 ms p95,” “error rate < 0.1%,” “99.5% success over 10k trials.”*
> Each requirement: single behavior, single actor, single condition, single metric.

## 42.5 Requirements

| ID | Priority | Category | Summary | Source / C-IDs | Key Metrics / Verification |
|----|-----------|-----------|---------|----------------|-----------------------------|
| R-COMM-000 | MUST | Compatibility | Preserve serial port configuration pipeline for GPS, IMU, steer, and machine modules. | C1 | Serial integration tests cover default baud/port combos. |
| R-COMM-001 | MUST | Discovery | Maintain UDP discovery, scanning, and monitoring workflows. | C1 | UDP monitor decodes ≥ 30 PGNs/s with checksum validation. |
| R-COMM-002 | MUST | PGN Transport | Continue emitting and receiving CAN/UDP PGNs for control flows. | C1 | Replay harness validates PGN parity vs legacy logs. |
| R-COMM-003 | SHOULD | Corrections | Support NTRIP over TCP alongside UDP/serial routing. | C1 | GNSS correction test verifies failover and reconnect. |
| R-COMM-004 | SHOULD | Facade | Provide gRPC/WebSocket facade that coexists with PGNs. | C3 | Bridge integration tests validate typed facade parity. |
| R-COMM-005 | MUST | Bridge Integrity | Preserve byte-for-byte PGN framing or provide deterministic bridge. | C3 | Round-trip diff < 1 byte difference on regression logs. |
| R-COMM-010 | MUST | Layer Streams | Deliver versioned PGNs with sequencing and schema negotiation. | C2 | CI ensures schema hash negotiation and sequence checks. |
| R-COMM-011 | SHOULD | Diagnostics | Enforce monotonic timestamps, bounds checks, and bad-sample counters. | C2 | Transport tests inject faults and verify rejection. |
| R-COMM-012 | SHOULD | Latency Budgets | Document and enforce latency/error budgets for new channels. | C3 | Benchmarks confirm ≤100 ms control RTT, ≤0.1% loss. |
| R-COMM-020 | MUST | Pose Cadence | Publish canonical PoseStream cadence and sequencing policy. | C3 | Replay diff ensures deterministic ordering across clients. |
| R-COMM-021 | SHOULD | Layer Handshake | Extend registry handshake with chunking, retry, and back-pressure semantics. | C2 | Integration test verifies handshake negotiation. |
| R-COMM-022 | MUST | Zone Service | Expose ZoneService gRPC API for boundary/headland polygons. | C3 | Contract tests stream ≥ 10 zones with provenance metadata. |
| R-COMM-023 | MUST | Pose Zone Mask | Embed PoseZoneMask with zone identifiers and registry hash. | C3 | Replay verifies deterministic gating decisions. |
| R-COMM-030 | MUST | Plugin Transport | Define plugin registration, leases, and heartbeats over transports. | C3 | Plugin integration suite validates lease renewals. |
| R-COMM-031 | MUST | Permissions | Enforce authenticated sessions and capability permissions per connection. | C4 | Security tests confirm unauthorized access is rejected. |
| R-COMM-032 | SHOULD | Health Reporting | Publish health/metrics RPC expectations and degraded-state signaling. | C4 | Health endpoint returns status within 200 ms under load. |
| R-COMM-040 | MUST | Time Authority | Establish canonical timebase with documented tolerances. | C5 | Clock drift tests confirm ≤5 ms drift across nodes. |
| R-COMM-041 | SHOULD | Timestamp Reconcile | Require capture timestamps/sequence numbers for reconciliation. | C5 | Integration tests realign device clocks within tolerance. |
| R-COMM-042 | SHOULD | End-to-End Latency | Document latency budgets per topic. | C5 | Monitoring dashboards alert when thresholds exceeded. |

### 42.5.1 Requirement Sources & Rationale

| Req ID | Source (issue/discussion/standard) | Rationale (one line) |
|--------|-------------------------------------|----------------------|
| R-COMM-000 | AgIO serial configuration backlog | Ensure continuity for deployed rigs. |
| R-COMM-004 | Linux remote-core pilots | Typed facade enables headless deployments. |
| R-COMM-010 | Layer registry proposals | Sequencing prevents telemetry drift. |
| R-COMM-030 | Plugin lifecycle reviews | Plugins require deterministic leasing. |
| R-COMM-040 | Timebase working sessions | Shared clock anchors determinism. |

---

## 42.6 Acceptance Criteria & Verification

- Transport regression suite MUST replay historical PGN logs against bridge outputs with byte-for-byte parity.
- Performance benchmarks MUST confirm transport latency budgets before enabling remote pilots.
- Mesh simulations MUST validate share profile enforcement, store-and-forward windows, and RadioBridge throttling under loss.

### 42.6.1 Requirement-to-Verification Map

| Req ID | Verification Type | Artifact / Location | Pass/Fail Threshold |
|--------|--------------------|---------------------|---------------------|
| R-COMM-002 | Replay harness | `/tests/replay/pgn_transport/` | Zero mismatched PGN frames over baseline logs. |
| R-COMM-010 | CI lint | `/tools/layer-registry-lint/` | Schema hash drift detected within one CI cycle. |
| R-COMM-030 | Integration tests | `/tests/integration/plugin_transport/` | Lease renewals succeed; unauthorized clients rejected. |
| R-COMM-040 | Clock sync tests | `/tests/simulation/timebase/` | Drift ≤5 ms after 30-minute run. |

---

## 42.7 Constraints

- Transports MUST operate across Windows and Linux hosts without conditional compilation forks.
- Offline scenarios MUST degrade gracefully, buffering telemetry for at least 30 minutes before discard.
- Security controls MUST align with Section 43 channel policies, including mutual TLS or token negotiation where applicable.

### 42.7.1 Non-Functional Requirement Classes

- **Performance:** Control loop RTT ≤ 100 ms; monitoring channels ≤ 500 ms; radio links respect configured bitrate ceilings.
- **Reliability & Availability:** Bridge and mesh services restart without data loss, leveraging replay windows and acknowledgements.
- **Security:** Mutual authentication on typed transports; encrypted radio links where hardware permits; share profiles enforce ACLs.
- **Operability:** Structured logs include transport IDs, schema hashes, and sequence counters; health endpoints expose back-pressure state.
- **Maintainability:** Transport bindings share codecs and configuration schema; registry updates documented with automated linting.

---

## 42.8 Risks & Open Issues

| ID | Description | Impact | Mitigation / Status | Owner |
|----|-------------|--------|---------------------|-------|
| RISK-42-1 | Bridge latency exceeds control budgets under load. | High | Benchmark with replay suite and optimize batching. | @interop-wg |
| RISK-42-2 | Mesh share profiles misconfigured, leaking sensitive data. | Medium | Provide templates and CI validation for profiles. | @interop-wg |
| ISSUE-42-1 | Define telemetry topic prioritization for RadioBridge throttling. | Medium | Draft prioritization table with Ops WG. | @interop-wg |

---

## 42.9 Design Considerations

| ID | Consideration | Description |
|----|----------------|-------------|
| C1 | Legacy PGN Transport Stewardship | Serial and UDP PGNs remain authoritative; configuration tooling must persist across OS targets. |
| C2 | Versioned Variable-Rate Layer Streams | Layer PGNs require schema hashes, sequencing, and registry negotiation to prevent drift. |
| C3 | Typed Facade & Compatibility Bridge | gRPC/WebSocket APIs must coexist with PGNs via deterministic translation. |
| C4 | Plugin Leases & Security Enforcement | Transport-level leasing, permissions, and health semantics govern plugin behavior. |
| C5 | Timebase & Telemetry Mesh Governance | Canonical timebase, mesh share profiles, and RadioBridge policies ensure deterministic multi-device coordination. |
| C6 | Gauge Telemetry Channels | Dedicated gauge PGNs deliver engine and machine data with backwards compatibility and diagnostics. |

### 42.9.1 Assumptions & Preconditions

- [A1] Field hardware continues emitting legacy PGNs during typed transport rollout.
- [A2] Mesh participants maintain connectivity sufficient for heartbeat exchange (≥1 per 5 s) or trigger failover.
- [A3] RadioBridge deployments negotiate bitrate limits prior to enabling higher-rate telemetry topics.

#### C1 - Legacy PGN Transport Stewardship

- Preserve serial port management for GPS, IMU, steer, and machine modules with configurable baud/port settings.
- Maintain UDP discovery, scanning, and monitoring workflows used for field module supervision.
- Continue publishing canonical PGN references and regression logs to validate compatibility during upgrades.

#### C2 - Versioned Variable-Rate Layer Streams

- PGNs `0xE1`, `0xE0`, and `0xDE` carry analog and binary layer samples with sequence counters to detect loss.
- `0xE4`/`0xE3` commands, definition handshake `0xE2`, and aggregated summaries `0xDF` coordinate retries and acknowledgements.
- Layer IDs reserve ranges (1=Working, 2=Flow State, 10=Actual/Commanded, 20=Downforce, 30=Yield, 40=Moisture, 240–255 third-party).
- Samples outside expected bounds trigger `badSample` counters; monotonic timestamps accompany payloads for reconciliation.

#### C3 - Typed Facade & Compatibility Bridge

- Bridge services translate PGNs to typed gRPC/WebSocket events without altering payload semantics.
- Sequencing and schema hashes ensure typed clients detect drift and request resynchronization.
- Byte-for-byte validation across replay logs confirms deterministic translation and acceptable latency overhead.

#### C4 - Plugin Leases & Security Enforcement

- Core exposes capability directories, lease heartbeats, and permissions gating (pose.read, section.command, storage.write).
- Health RPCs (Ping, GetStatus, GetMetrics) report degraded states, enabling operators to diagnose lagging transports.
- All plugin connections authenticate (local policy or certificates) and emit structured audit logs for control actions.

#### C5 - Timebase & Telemetry Mesh Governance

- Canonical time authority derived from GPS or PTP with system clock fallback; drift tolerance ≤5 ms across nodes.
- Mesh topics include `presence`, `poseTrail`, `coverage`, `layerEdit`, and `sessionState` with QoS budgets (presence 1 Hz, trails ≤2 Hz, coverage ≤1 Hz aggregated).
- Share profiles define export/import policies, allow/deny lists, and privacy defaults (sensitive feeds opt-in).
- Store-and-forward queues retain up to 20 MB/device, replaying payloads with hash validation; stale payloads drop after 30 minutes unless marked archival.

#### C6 - Gauge Telemetry Channels

- Gauge telemetry PGNs (0xDA, 0xD9, 0xD8) reuse existing AgOpenGPS framing to deliver engine and machine metrics.
- Capability discovery advertises supported gauges and validity heartbeats for dashboards to pre-provision tiles.
- Diagnostics tooling decodes raw payload bytes, engineering values, and source metadata to support troubleshooting.

---
