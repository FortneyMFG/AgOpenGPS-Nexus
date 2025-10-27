# 41 — Service APIs & Contracts
*(Status: Revised)*

**Authors:** Nexus Team (Codex)
**Created:** 2025-10-20  
**Version:** 0.2.0  
**Section ID:** 41  
**Editors:** Interprocess Communications Working Group  
**Last Updated:** 2025-10-26  
**Related Sections:** 21 — System Decomposition & Boundaries, 22 — Process Model & Deployment Topologies, 23 — Threading, Scheduling & Timing, 42 — Transports, 43 — Channel Security, 63 — Layers Registry & Journal Contracts  
**Upstream Dependencies:** 1X — Platform Foundations, 2X — System Architecture  
**Downstream Impacts:** 5X — Hardware IO Device Layer, 7X — Mapping & Geospatial, 8X — Guidance, 9X — Frontends & Ops, 97 — Simulation & Replay  
**Related ADRs:** 41-ADR-002, 41-ADR-062

---

## 41.1 Purpose & Scope

Document the service contracts Nexus Core presents to in-process plugins, the UI bridge, automation tooling, and legacy compatibility adapters. This revision establishes **struct-based C# ABIs as the canonical plugin interface**, while reserving gRPC/WebSocket endpoints for the **UI bridge and remote automation clients**. Typed contracts, manifests, and capability negotiation must remain consistent across these surfaces without multiplying ADRs or bespoke per-plugin agreements.

---

## 41.2 Summary of Contract Surfaces

| Surface | Audience | Contract Form | Ownership Notes |
|---------|----------|---------------|-----------------|
| Struct ABI (`Aog.Contracts.Struct`) | In-process plugins hosted by Core | Versioned `record struct` and immutable views generated from shared schemas. | Canonical source for telemetry, command, and registry data. Governed with ABI diff tests and semantic versioning. |
| UI Bridge (`Aog.Contracts.Bridge`) | Avalonia UI, legacy desktop shells, automation scripts | gRPC and WebSocket endpoints that mirror struct payloads. | Implemented by a bridge plugin that binds Core struct contracts to remote clients. |
| Legacy Compatibility | AgIO PGN/UDP consumers and MCU devices | Binary PGNs, nanopb messages, serial transports. | Remains authoritative for hardware integration; bridged into struct contracts. |

> **Design intent:** One schema drives struct ABIs and bridge payloads. Plugins never consume gRPC directly; remote clients never receive raw structs.

---

## 41.3 Context & Drivers

- Plugin authors requested deterministic, allocation-free access to Core state without gRPC hosting overhead.
- UI and automation teams still need language-neutral contracts with discovery and streaming semantics.
- Legacy PGN consumers must retain parity during the struct ABI migration and bridge rollout.
- Prior revisions scattered requirements across numerous ADRs, obscuring ownership. This section consolidates plugin/API guidance into three accountability areas: **ABI governance**, **bridge parity**, and **registry metadata**.

---

## 41.4 Definitions

| Term | Definition |
|------|------------|
| Struct ABI | Generated `record struct` or immutable view that represents contract data within the Core process.
| Bridge Plugin | Core-hosted adapter that projects struct contracts as gRPC/WebSocket endpoints for remote clients.
| Capability Registry | Shared service that publishes schema hashes, ABI versions, and manifest metadata so hosts negotiate compatibility before activation.
| Manifest | Declarative plugin descriptor that advertises capabilities, transport requirements, feature flags, and ABI targets.
| Compatibility Bridge | Service translating between legacy PGNs/nanopb payloads and struct contracts without data loss.

---

> **Requirement Grammar (RFC-2119)**
> - **MUST / MUST NOT** = mandatory; test must exist.  
> - **SHOULD / SHOULD NOT** = strong recommendation; justify exceptions.  
> - **MAY** = optional; document enabling conditions.  
> Use measurable statements, one behavior per requirement.

---

## 41.5 Requirements

### 41.5.1 Canonical Struct ABI

| ID | Priority | Summary | Verification |
|----|----------|---------|--------------|
| R-STRUCT-001 | MUST | Generate struct contracts and immutable views from the shared schema repository with semantic versioning (major/minor/patch). | ABI diff tests in `/tests/contracts/struct-abi/` gate breaking changes. |
| R-STRUCT-002 | MUST | Provide read-only, allocation-free accessors; mutations route through explicit command services. | Unit tests ensure Core-owned buffers remain unmodified. |
| R-STRUCT-003 | MUST | Publish ABI fingerprints (hash + version) through the capability registry and plugin manifests. | Manifest validation ensures fingerprints match registry data. |
| R-STRUCT-004 | SHOULD | Offer converter helpers for legacy PGNs and nanopb payloads so compatibility bridges stay deterministic. | Replay harness compares PGN ↔ struct translations. |

### 41.5.2 UI Bridge & Remote Surfaces

| ID | Priority | Summary | Verification |
|----|----------|---------|--------------|
| R-BRIDGE-001 | MUST | Provide a bridge plugin that mirrors struct contracts to gRPC/WebSocket clients with payload parity and audit metadata. | `/tests/integration/plugin_bridge/` verifies struct vs gRPC equivalence. |
| R-BRIDGE-002 | MUST | Maintain discovery, capability leasing, and health semantics in the bridge so remote clients observe the same lifecycle events as in-process plugins. | Integration tests cover lease renewals and degraded health signals. |
| R-BRIDGE-003 | SHOULD | Support UI-friendly subscription aggregation (e.g., batched pose updates, consolidated equipment snapshots) without altering struct semantics. | UI smoke tests confirm aggregated feeds match struct data. |
| R-BRIDGE-004 | MUST | Enforce channel security (mutual TLS or token exchange) aligned with Section 43 when exposing gRPC/WebSocket endpoints. | Security tests validate rejection of unauthenticated sessions. |

### 41.5.3 Registry & Manifest Governance

| ID | Priority | Summary | Verification |
|----|----------|---------|--------------|
| R-REG-001 | MUST | Publish capability registry entries that bind schema hashes to struct ABI versions and manifest compatibility windows. | Registry lint in `/tools/registry-lint/` detects mismatches. |
| R-REG-002 | MUST | Require manifests to declare minimum/maximum supported ABI versions and transport expectations (`struct`, `bridge`). | Manifest validation suite enforces field presence and bounds. |
| R-REG-003 | SHOULD | Emit audit logs for capability negotiation, including struct ABI version, manifest ID, and authorization context. | Audit log integration tests confirm entries per activation. |
| R-REG-004 | MAY | Provide optional manifest extensions for UI contribution metadata consumed by the bridge. | UI contract tests ensure extension payloads deserialize correctly. |

---

## 41.6 Acceptance Criteria & Verification

1. **ABI Gate:** Struct contract CI pipelines MUST block incompatible field changes without explicit version increments (R-STRUCT-001/002).
2. **Bridge Parity:** The bridge conformance suite MUST demonstrate payload, error, and audit parity between struct invocations and gRPC endpoints for representative services (R-BRIDGE-001/002/004).
3. **Registry Integrity:** Manifest submission MUST fail when ABI fingerprints or compatibility ranges mismatch registry records (R-STRUCT-003, R-REG-001/002).
4. **Legacy Replay:** PGN replay harnesses MUST achieve byte-for-byte parity when routed through struct converters, ensuring existing hardware integrations remain valid (R-STRUCT-004).

| Requirement | Verification Artifact | Pass Threshold |
|-------------|-----------------------|----------------|
| R-STRUCT-001/002 | `/tests/contracts/struct-abi/` | Additive-only changes pass; breaking changes blocked. |
| R-BRIDGE-001/002 | `/tests/integration/plugin_bridge/` | Struct vs gRPC responses identical for 100% sampled calls. |
| R-REG-001/002 | `/tools/registry-lint/`, `/tests/integration/manifest_validation/` | Lint catches mismatched fingerprints within one CI cycle. |
| R-STRUCT-004 | `/tests/replay/pgn_bridge/` | Zero diff between PGN logs and struct translations. |

---

## 41.7 Constraints & Non-Functional Targets

- Struct ABI calls MUST remain allocation-free in steady state and complete within ≤50 µs p95 for telemetry access.
- Bridge endpoints MUST deliver ≤150 ms p95 request latency intra-host and ≤300 ms p95 for remote WAN clients.
- All surfaces MUST comply with Section 43 security controls and Section 22 scheduling constraints.
- Documentation MUST stay co-located with the schema repository to avoid orphaned ADR fragments; updates reference the consolidated requirements above instead of duplicating ADRs per plugin.

### Non-Functional Classes

- **Performance:** Struct ABI access is lock-free; bridge streaming supports back-pressure with configurable batching.  
- **Reliability:** Bridge restarts retain lease state; manifests revalidate on reconnect.  
- **Security:** Mutual TLS or token enforcement for bridge connections; signed manifest bundles.  
- **Operability:** Structured logs include ABI version, manifest ID, and capability set for each activation.  
- **Maintainability:** Single schema source with automated generation ensures ADR count stays minimal; revisions update this section and the two related ADRs only.

---

## 41.8 Design Considerations

| ID | Consideration | Description |
|----|---------------|-------------|
| C1 | Schema Single Source | Proto/IDL files (or equivalent schema definitions) generate both struct contracts and bridge payloads. |
| C2 | Deterministic Translation | Converter libraries provide deterministic mapping between PGNs, structs, and bridge messages. |
| C3 | Lifecycle Alignment | Capability registry, manifests, and lease management share identical semantics across struct and bridge transports. |
| C4 | Audit & Observability | Audit trails and health metrics report ABI versions and manifest IDs for traceability. |

> **Assumptions:** Legacy PGNs remain operational throughout the struct ABI rollout; bridge plugins ship alongside Core for UI deployments; automation clients consume gRPC/WebSocket endpoints rather than attaching plugins in-process.

---

## 41.9 Change Log

| Date | Summary | Author |
|------|---------|--------|
| 2025-10-26 | Adopt struct ABI as canonical plugin surface, consolidate bridge/registry requirements. | Nexus Team (Codex) |
| 2025-10-20 | Initial draft focusing on gRPC-first contracts. | Nexus Team (Codex) |

