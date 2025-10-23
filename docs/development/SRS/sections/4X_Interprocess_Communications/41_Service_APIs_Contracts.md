# 41 — Service APIs & Contracts
*(Status: Proposed)*

**Author:** Codex
**Created:** 2025-10-20
**Version:** 0.1.0
**Section ID:** 41
**Editors:** Interprocess Communications Working Group
**Last Updated:** 2025-10-24
**Related Sections:** 21 — System Decomposition & Boundaries, 22 — Process Model & Deployment Topologies, 23 — Threading, Scheduling & Timing, 42 — Transports, 43 — Channel Security, 63 — Layers Registry & Journal Contracts
**Upstream Dependencies:** 1X — Platform Foundations, 2X — System Architecture
**Downstream Impacts:** 5X — Hardware IO Device Layer, 7X — Mapping & Geospatial, 9X — Frontends & Ops, 97 — Simulation & Replay

---

## 41.1 Purpose & Scope

Define the cross-process and cross-application APIs that connect Nexus Core, UI shells, automation tooling, and companion
services. This section governs the contracts, versioning practices, and lifecycle expectations for gRPC, WebSocket, and
registry surfaces so that headless Linux deployments and legacy Windows tooling can interoperate without bespoke bridges.

---

## 41.2 Context

- AgOpenGPS and AgIO historically exchange binary PGNs over UDP/serial for steering, section control, and telemetry.
- Linux pilots require typed APIs that expose the same semantics to remote front-ends and automation services.
- Plugins and third-party tools need predictable versioning, schema discovery, and capability negotiation mechanisms.
- Legacy PGN clients must remain functional throughout the migration to typed contracts.

> **Related Guides:** [AOG-Link Bridge Architecture Guide](../../../AgIO/aog-link-bridge-architecture-guide.md), deployment playbooks in `/docs/ops/`.

---

## 41.3 Legacy Comparison

| Area / Theme | Legacy Behavior | Identified Limitation | Modernization Opportunity | Reference / Source |
|---------------|-----------------|------------------------|---------------------------|--------------------|
| Runtime APIs | UDP/serial PGNs exchanged via AgIO utilities. | No formal schema or version discovery; binary parsing burden on clients. | Publish typed contracts with negotiated versions and compatibility bridges. | Legacy AgIO toolchain |
| Metadata Registries | Manual spreadsheets for layer IDs, quality rules, and units. | Frequent drift between firmware, UI, and analytics tooling. | Centralize registries with schema hashes and capability negotiation. | Community registry backlog |
| Plugin Lifecycle | Plugins register ad-hoc over config files and shared memory. | No standard handshake or audit logging, making enforcement difficult. | Define gRPC capabilities service and manifest schema enforced by Core. | Nexus plugin discussions |

> **Informative:** Captures historical context and modernization drivers.

---

## 41.4 Definitions

| Term | Definition |
|------|-------------|
| PGN | Parameter Group Number used by AgOpenGPS UDP/CAN protocols.
| Capability Registry | Shared catalog enumerating services, layers, and plugins with semantic versions.
| Compatibility Bridge | Service that translates between legacy PGNs and typed contracts without data loss.
| Manifest | Declarative description of plugin capabilities, UI contributions, and feature flags.

---

> **Requirement Grammar (RFC-2119):**
> - **MUST / MUST NOT** = mandatory; test must exist.
> - **SHOULD / SHOULD NOT** = strong recommendation; justify exceptions.
> - **MAY** = optional; document enabling conditions.
>
> **Clarity Checklist:** Avoid weak words: *fast, robust, user-friendly, handle, support, adequate,* etc.
> Prefer measurable forms: *“≤ 250 ms p95,” “error rate < 0.1%,” “99.5% success over 10k trials.”*
> Each requirement: single behavior, single actor, single condition, single metric.

## 41.5 Requirements

### 41.5.1 Core Telemetry Contracts

| ID | Priority | Category | Summary | Source / C-IDs | Key Metrics / Verification |
|----|-----------|-----------|---------|----------------|-----------------------------|
| R-CORE-001 | MUST | Compatibility | Maintain the existing PGN catalog for steer, machine, relay, and section messaging. | C1 | Regression replay of legacy PGN payloads over bridge. |
| R-CORE-002 | MUST | Diagnostics | Expose UDP/PGN monitor tooling for inspectors and third-party clients. | C1 | CLI tool decodes ≥ 30 PGNs/s with checksum validation. |
| R-CORE-003 | SHOULD | Versioning | Provide compatibility guidance for protobuf/JSON schemas alongside PGNs. | C2 | Version matrix published and enforced in CI lint. |
| R-CORE-004 | SHOULD | Bridge | Publish canonical PGN reference and bridge hooks for typed APIs. | C3 | Bridge integration tests cover ≥ 95% PGN catalog. |

### 41.5.2 Control & Guidance Services

| ID | Priority | Category | Summary | Source / C-IDs | Key Metrics / Verification |
|----|-----------|-----------|---------|----------------|-----------------------------|
| R-CTRL-001 | SHOULD | Configuration | Continue surfacing GNSS correction settings until equivalent typed API lands. | C1 | Feature parity checklist between PGN and typed surfaces. |
| R-CTRL-002 | MUST | Capabilities | Provide Capabilities gRPC service for plugin registration and leasing. | C3 | Integration tests validate lease renewal and rejection flows. |
| R-CTRL-003 | MUST | Service Catalog | Publish typed gRPC contracts for Pose, Equipment, SectionControl, LayerRegistry, TileQuery, Config, EventBus, Guidance, and Health. | C3 | Contract proto repo with lint + backward compatibility checks. |
| R-CTRL-004 | SHOULD | Audit | Require audit metadata on control-affecting RPCs. | C4 | Audit log integration test ensures identity + timestamp captured. |
| R-CTRL-005 | MUST | Geometry | Provide canonical equipment hierarchy with stable IDs and offsets. | C2 | Schema published with integration test verifying IDs. |
| R-CTRL-006 | SHOULD | Control Semantics | Document SectionGroup semantics and overrides for deterministic gating. | C2 | Behavior verified in control simulator. |

### 41.5.3 Plugin & Registry Surfaces

| ID | Priority | Category | Summary | Source / C-IDs | Key Metrics / Verification |
|----|-----------|-----------|---------|----------------|-----------------------------|
| R-REG-001 | MUST | Metadata | Publish versioned layer definitions, quality rules, and schema hashes. | C2 | Registry diff detection triggers on hash mismatch in CI. |
| R-REG-002 | SHOULD | Integrity | Reserve ID ranges, enforce monotonic timestamps, and fail on schema mismatch. | C2 | Contract tests confirm rejection on hash mismatch. |
| R-REG-003 | SHOULD | Governance | Adopt semantic versioning, deprecation periods, and compatibility tests. | C2 | Release checklist with version gating automation. |
| R-REG-004 | SHOULD | Discovery | Expose manifest metadata so plugins populate control/visualization surfaces. | C3 | Plugin manifest validation suite passes baseline cases. |
| R-REG-005 | MAY | Negotiation | Document handshake messages for capability discovery across processes. | C3 | gRPC discovery endpoint coverage in contract tests. |
| R-REG-006 | SHOULD | UI Contracts | Define declarative schema for plugin UI contributions consumed by frontend APIs. | C4 | UI schema contract tests ensure layout metadata loads. |
| R-REG-007 | SHOULD | Manifest Flags | Include feature flags and semantic versions in plugin manifests. | C3 | Manifest schema validated against compatibility rules. |

### 41.5.4 Requirement Sources & Rationale

| Req ID | Source (issue/discussion/standard) | Rationale (one line) |
|--------|-------------------------------------|----------------------|
| R-CORE-001 | Legacy PGN workflow; community field logs | Preserve compatibility during transition. |
| R-CORE-004 | Bridge working group discussions | Typed APIs must not strand existing hardware. |
| R-CTRL-002 | Plugin lifecycle review | Enforce consistent capability negotiation. |
| R-CTRL-004 | Safety and audit reviews | Command traces must be attributable. |
| R-REG-001 | Layer registry proposals | Shared schema prevents drift between firmware/UI. |

## 41.6 Acceptance Criteria & Verification

- Regression replay suite MUST validate PGN ↔ typed API parity across at least three representative field logs (see §41.5.1 R-CORE-001–R-CORE-004).
- Contract code generation pipelines MUST block incompatible protobuf/schema changes without explicit version increments (see §41.5.2 R-CTRL-003 and §41.5.3 R-REG-003).
- Plugin onboarding checklist MUST verify capability registration, manifest validation, and audit logging before approval (see §41.5.2 R-CTRL-002 and R-CTRL-004, plus §41.5.3 R-REG-004–R-REG-007).

### 41.6.1 Requirement-to-Verification Map

| Req ID | Verification Type | Artifact / Location | Pass/Fail Threshold |
|--------|--------------------|---------------------|---------------------|
| R-CORE-001 | Replay harness | `/tests/replay/pgn_bridge/` | 100% PGN diff coverage, zero checksum errors. |
| R-REG-001 | CI lint | `/tools/registry-lint/` | Schema hash drift detected within one CI cycle. |
| R-CTRL-003 | Contract tests | `/tests/contracts/grpc/` | Backward compatibility gate passes on PR merges. |
| R-CTRL-004 | Integration tests | `/tests/integration/audit_logging/` | All control RPCs emit audit trail entries. |

---

## 41.7 Constraints

- Core API surfaces MUST be consumable from both Windows and Linux hosts.
- PGN compatibility MUST be preserved until field telemetry, firmware, and analytics migrate together.
- All public APIs MUST enforce authenticated channels as defined in Section 43 (Channel Security).

### 41.7.1 Non-Functional Requirement Classes

- **Performance:** gRPC request p95 ≤ 150 ms intra-host; PGN bridge adds ≤ 10 ms overhead.
- **Reliability & Availability:** Bridge services restartable without dropping in-flight leases; manifests revalidated on reconnect.
- **Security:** Mutual TLS for gRPC services; signed manifest bundles for plugins.
- **Operability:** Structured logs with schema version and PGN identifiers; health endpoints advertise capability sets.
- **Maintainability:** Proto definitions versioned with backward compatibility tests; registry updates documented.

---

## 41.8 Risks & Open Issues

| ID | Description | Impact | Mitigation / Status | Owner |
|----|-------------|--------|---------------------|-------|
| RISK-41-1 | Schema drift between firmware and typed APIs. | High | Registry hash negotiation and CI lint. | @interop-wg |
| RISK-41-2 | Bridge latency impacts real-time control. | Medium | Enforce ≤10 ms overhead; benchmark under load. | @interop-wg |
| ISSUE-41-1 | Define manifest schema for UI contributions. | Medium | Draft schema in plugin repo; coordinate with UI WG. | @interop-wg |

---

## 41.9 Design Considerations

| ID | Consideration | Description |
|----|----------------|-------------|
| C1 | PGN Compatibility Horizon | Legacy PGN transports remain authoritative until typed services cover all control paths. |
| C2 | Versioned Layer Registries | Shared layer definitions, units, and quality rules require schema hashes and negotiated IDs. |
| C3 | Typed API Bridge Strategy | Bridge services must translate PGNs ↔ typed gRPC without data loss or added coupling. |
| C4 | Plugin UX & Audit Guarantees | UI contributions and control RPCs require declarative manifests plus audit hooks. |

### 41.9.1 Assumptions & Preconditions

- [A1] Field-deployed firmware continues emitting PGNs during the transition window.
- [A2] Registry publishing pipeline can distribute schema hashes alongside artifacts within one release cycle.
- [A3] Plugin manifests are distributed via signed bundles managed by Nexus release tooling.

#### C1 - PGN Compatibility Horizon

- Maintain binary PGN payloads for steer, section, relay, and machine telemetry during migration phases.
- Ensure diagnostic tooling (e.g., UDP monitor) remains available for integrators relying on PGN inspection.
- Document capability discovery messages so typed clients can negotiate available services without breaking PGN peers.

#### C2 - Versioned Layer Registries

- Layer definition records include schema version, IDs, names, units, normalization ranges, aggregation modes, composite rules,
  display ranges, smoothing parameters, alarm bands, quality rules, derived layer bindings, storage precision, and cadence hints.
- `sourceMappings` describe how hardware inputs populate logical layers, keeping analytics and visualization in sync.
- Registry negotiation exchanges schema hashes and validity bitmaps; mismatches trigger fast-fail with remediation guidance.
- Units registry reserves IDs (1–239 core, 240–255 third-party) with collision linting in CI.

#### C3 - Typed API Bridge Strategy

- Bridge services publish canonical PGN references, translation hooks, and validation flows for typed APIs.
- Sequencing ensures monotonic timestamps and compatibility with capability leasing semantics.
- Bridge implementation MUST sustain PGN throughput without compromising low-latency control paths.

#### C4 - Plugin UX & Audit Guarantees

- Plugin manifests declare UI panels, overlays, and configuration surfaces consumed by frontend APIs without embedding arbitrary UI code.
- Feature flags and semantic version ranges gate plugin activation; mismatches trigger downgrade or rejection flows.
- Control-affecting RPCs include operator/plugin identity and timestamps to satisfy audit requirements.

---
