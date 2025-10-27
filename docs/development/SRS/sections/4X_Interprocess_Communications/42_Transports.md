# 42 — Transports
*(Status: Revised)*

**Authors:** Nexus Team (Codex)
**Created:** 2025-10-20  
**Version:** 0.2.0  
**Section ID:** 42  
**Editors:** Interprocess Communications Working Group  
**Last Updated:** 2025-10-26  
**Related Sections:** 21 — System Decomposition & Boundaries, 41 — Service APIs & Contracts, 53 — AOG-Link Compatibility, 61 — Kinematics & Pose Fusion  
**Upstream Dependencies:** 1X — Platform Foundations, 2X — System Architecture  
**Downstream Impacts:** 5X — Hardware IO Device Layer, 6X — Core Domain Services, 7X — Mapping & Geospatial  
**Related ADRs:** 41-ADR-062, 42-ADR-006, 42-ADR-047

---

## 42.1 Purpose & Scope

Describe the transport stack that moves telemetry, control commands, and registry metadata between Nexus Core, in-process plugins, bridge services, and external devices. This revision aligns transports with the struct-first plugin strategy: **struct exchanges power in-process plugins**, **gRPC/WebSocket bridges serve remote UI and automation clients**, and **legacy PGN flows remain authoritative for hardware**. The section focuses on timing, reliability, and lifecycle expectations rather than enumerating separate ADRs per transport.

---

## 42.2 Transport Topology Overview

```
+-----------------+      Struct ABI      +-----------------+
|  Core Services  |<-------------------->| In-Process Plug. |
+-----------------+                      +-----------------+
        |                                          |
        |  PGN / nanopb                           |
        v                                          v
+-----------------+      Bridge (gRPC/WebSocket)  +-----------------+
| Legacy Devices  |<----------------------------->| UI / Automation |
+-----------------+                               +-----------------+
```

- Struct ABIs provide zero-copy access inside the Core host.
- The bridge plugin projects struct payloads over authenticated gRPC/WebSocket channels.
- Legacy transports (serial, UDP, CAN, nanopb) connect hardware and remain the source of truth for PGNs; converters translate into structs.

---

## 42.3 Context & Drivers

- Field deployments rely on AgIO-managed PGNs and nanopb messages; these MUST remain intact while new contracts mature.
- Hosting plugins in-process removes gRPC hosting overhead and simplifies packaging for Linux targets.
- UI shells and automation tooling still require language-neutral, authenticated endpoints.
- Previous drafts scattered requirements across ADR-002, ADR-047, and struct option analyses; this revision consolidates expectations into a single transport charter tied to the struct-first decision.

---

## 42.4 Definitions

| Term | Definition |
|------|------------|
| Struct Transport | In-process exchange of generated struct contracts with immutable views and ABI governance. |
| Bridge Transport | gRPC/WebSocket projection of struct payloads implemented by the bridge plugin. |
| Legacy PGN Transport | UDP/serial/CAN exchanges defined by AgIO and AOG-Link (nanopb) for MCU integration. |
| Share Profile | Operator-defined policy describing telemetry topics permitted to exit the cab and external feeds admitted. |
| PoseStream | Canonical stream of pose/velocity/zone data consumed across services. |

---

## 42.5 Requirements

### 42.5.1 Legacy & Hardware Transports

| ID | Priority | Summary | Verification |
|----|----------|---------|--------------|
| R-LEG-000 | MUST | Preserve serial/UDP configuration tooling for GPS, IMU, steer, and machine modules. | Serial integration tests cover default baud/port combinations. |
| R-LEG-001 | MUST | Maintain PGN emission/ingestion with deterministic framing and checksum validation. | Replay harness validates byte-for-byte parity vs legacy logs. |
| R-LEG-002 | SHOULD | Extend variable-rate layer PGNs with sequencing and schema negotiation hooks tied to registry hashes. | CI lint checks schema hashes; replay verifies sequence handling. |
| R-LEG-003 | SHOULD | Provide converter helpers that translate PGNs/nanopb payloads into struct contracts for Core consumption. | `/tests/replay/pgn_bridge/` ensures deterministic conversions. |

### 42.5.2 Struct Transport Expectations

| ID | Priority | Summary | Verification |
|----|----------|---------|--------------|
| R-STRUCT-010 | MUST | Expose struct transports for telemetry, command, registry, and event streams with lease semantics shared across plugins. | `/tests/integration/plugin_transport/` validates lease renewal and heartbeats. |
| R-STRUCT-011 | MUST | Guarantee lock-free, allocation-free reads for steady-state telemetry access with ≤50 µs p95 latency. | Microbench suite in `/tests/perf/struct_transport/` enforces latency budget. |
| R-STRUCT-012 | SHOULD | Support deterministic batching for bursty topics (e.g., guidance waypoints) while preserving ordering metadata. | Benchmarks verify ordering and latency bounds. |
| R-STRUCT-013 | MUST | Publish transport health, back-pressure, and ABI fingerprints to the capability registry. | Registry lint confirms metadata presence. |

### 42.5.3 Bridge Transport Expectations

| ID | Priority | Summary | Verification |
|----|----------|---------|--------------|
| R-BRIDGE-010 | MUST | Mirror struct transport semantics over gRPC/WebSocket, including leasing, errors, and audit metadata. | `/tests/integration/plugin_bridge/` compares struct vs bridge flows. |
| R-BRIDGE-011 | MUST | Enforce security controls from Section 43 (mutual TLS or signed tokens) and expose health endpoints. | Security regression tests validate rejection of unauthenticated sessions. |
| R-BRIDGE-012 | SHOULD | Offer bandwidth-aware subscription policies (e.g., PoseStream down-sampling) configurable per share profile. | Mesh tests in `/tests/simulation/telemetry_mesh/` validate throttling. |
| R-BRIDGE-013 | MAY | Provide UI-optimized aggregations (batched equipment snapshots, fused health summaries) so long as raw struct parity remains testable. | UI smoke tests confirm aggregation correctness. |

### 42.5.4 Mesh & Timebase Coordination

| ID | Priority | Summary | Verification |
|----|----------|---------|--------------|
| R-MESH-020 | MUST | Maintain canonical timebase with ≤5 ms drift across bridge participants. | Clock sync tests in `/tests/simulation/timebase/`. |
| R-MESH-021 | MUST | Support share profiles that govern outbound/inbound telemetry topics across radio or IP links. | `/tests/integration/share_profiles/` enforces policy adherence. |
| R-MESH-022 | SHOULD | Provide degraded-state signaling when bandwidth drops below configured thresholds. | Mesh simulations inject loss and verify health reports. |

---

## 42.6 Acceptance Criteria & Verification

1. **Legacy Continuity:** Serial/UDP regression suites MUST pass without PGN diffs when struct converters are introduced (R-LEG-000/001/003).
2. **Struct Performance:** Perf benchmarks MUST confirm struct telemetry access meets the ≤50 µs p95 latency goal before plugins may ship on a release train (R-STRUCT-010/011).
3. **Bridge Parity & Security:** Bridge conformance suites MUST validate parity with struct transports and enforce Section 43 controls (R-BRIDGE-010/011).
4. **Mesh Governance:** Share profile tests MUST prevent disallowed telemetry topics from leaving the cab and trigger alerts on sustained bandwidth drops (R-MESH-021/022).

| Requirement Group | Verification Artifact | Pass Threshold |
|-------------------|-----------------------|----------------|
| Legacy (R-LEG-000…003) | `/tests/replay/pgn_transport/`, `/tests/replay/pgn_bridge/` | Zero checksum or payload diffs. |
| Struct (R-STRUCT-010…013) | `/tests/integration/plugin_transport/`, `/tests/perf/struct_transport/` | Lease renewals succeed; latency budget met. |
| Bridge (R-BRIDGE-010…013) | `/tests/integration/plugin_bridge/`, `/tests/security/bridge_auth/` | Auth required; parity coverage ≥95% of contract surface. |
| Mesh (R-MESH-020…022) | `/tests/simulation/timebase/`, `/tests/simulation/telemetry_mesh/` | Drift ≤5 ms; share profile policy violations blocked. |

---

## 42.7 Constraints & Non-Functional Targets

- Struct transports MUST be confined to the Core process; remote access occurs solely through the bridge.
- Bridge services MUST reconnect without losing lease state and resume streaming within 5 s of recovery.
- Radio bridges MUST respect configured bitrate ceilings and queue telemetry for ≥30 minutes before discard.
- Logging MUST include transport ID, ABI version, and share profile identifier for traceability.

### Non-Functional Classes

- **Performance:** Control loops ≤100 ms RTT; monitoring channels ≤500 ms; struct telemetry ≤50 µs p95.  
- **Reliability:** Replayable queues ensure deterministic restart; share profiles survive reconnect.  
- **Security:** Mutual authentication and encrypted channels per Section 43; signed manifest bundles for struct transports.  
- **Operability:** Health endpoints expose back-pressure, lease counts, and active share profiles.  
- **Maintainability:** Transport guidance lives in this section plus three ADRs (Struct ABI, AOG-Link, Telemetry Mesh) to avoid ADR sprawl.

---

## 42.8 Risks & Open Issues

| ID | Description | Impact | Mitigation / Status | Owner |
|----|-------------|--------|---------------------|-------|
| RISK-42-1 | Struct transport latency regressions from plugin misuse. | High | Provide diagnostics counters and perf tests; escalate via SDK WG. | @interop-wg |
| RISK-42-2 | Bridge bandwidth exhaustion for remote UI sessions. | Medium | Enforce share profiles and adaptive down-sampling. | @interop-wg |
| RISK-42-3 | ABI drift between struct generators and legacy PGN converters. | Medium | Tie converter builds to schema repo CI. | @interop-wg |
| ISSUE-42-1 | Need prioritization matrix for radio telemetry topics. | Medium | Coordinate with Ops WG; tracked in telemetry mesh ADR follow-up. | @interop-wg |

---

## 42.9 Change Log

| Date | Summary | Author |
|------|---------|--------|
| 2025-10-26 | Align transports with struct-first plugin strategy and consolidate requirements. | Nexus Team (Codex) |
| 2025-10-20 | Initial draft emphasizing gRPC-first facades. | Nexus Team (Codex) |

