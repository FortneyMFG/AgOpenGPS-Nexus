# 41-ADR-002 — Expose Nexus services over gRPC/WebSocket bridge contracts
*(Status: Accepted — Scoped)*

**Authors:** Nexus Team (Codex)  
**Reviewers:** Interprocess Communications Working Group  
**Created:** 2025-10-20  
**Last Updated:** 2025-10-26  
**Version:** 0.2.0  
**Supersedes:** _None_  
**Superseded by:** Scoped alongside [41-ADR-062](41-ADR-062%20-%20Struct%20ABI%20Plugin%20Contracts.md)  
**Related SRS:** [41 — Service APIs & Contracts](41_Service_APIs_Contracts.md), [42 — Transports](42_Transports.md)

---

## 1) Context

Nexus requires a typed, language-neutral surface for UI shells, automation tooling, and remote integrations. Initial drafts positioned gRPC/protobuf as the universal interface for Core, UI, and plugins. Subsequent analysis (see SRS §41/§42 and ADR-062) determined that plugins hosted in-process demand struct-based contracts for latency and packaging reasons. gRPC remains critical for remote access, but it should now be scoped to the **bridge plugin** rather than Core plugins directly. This ADR captures the refined scope so documentation no longer implies gRPC-first plugins or spawns redundant ADRs for each surface.

---

## 2) Decision

Adopt gRPC/WebSocket contracts generated from shared schemas as the **authoritative remote access surface** exposed by the bridge plugin. In-process plugins consume struct ABIs defined by ADR-062; the bridge converts between structs and protobuf payloads for remote clients.

### Decision Summary

* **Scope:** Remote UI shells, automation tooling, and companion services connecting over process boundaries.  
* **Boundary:** Plugins hosted inside Core consume struct contracts. Legacy hardware continues using PGNs/nanopb via AgIO transports.  
* **Implementation Level:** Generated clients and servers in `Aog.Contracts.Bridge` backed by converters emitted alongside struct ABIs.

### Implementation Sketch

1. **Schema repository.** Maintain `.proto` (or equivalent IDL) definitions under `Nexus SourceCode/proto/`. Generation emits both struct contracts (`Aog.Contracts.Struct`) and bridge clients/servers (`Aog.Contracts.Bridge`).
2. **Bridge hosting.** Ship a bridge plugin that binds generated services to ASP.NET Core gRPC and WebSocket hosts. The plugin enforces capability leasing, manifests, and Section 43 security requirements.
3. **Parity tests.** CI runs bridge parity suites that compare struct invocations to gRPC responses to guarantee payload, error, and audit parity.
4. **Rollout.** Publish UI/automation endpoints via the bridge while maintaining PGN compatibility through AgIO. Plugins remain struct-only, reducing hosting overhead.

---

## 3) Consequences

**Positive Impacts**

- Remote clients obtain strongly typed, versioned APIs with streaming semantics and security controls.
- Bridge code remains thin because converters share the same schema as struct contracts, reducing maintenance.
- Documentation now references only two ADRs (002 + 062) for contract governance, avoiding ADR sprawl.

**Negative / Mitigated Impacts**

- Bridge hosting introduces overhead relative to raw structs — mitigated via shared ASP.NET Core infrastructure and connection pooling.
- Parity testing expands CI obligations — mitigated by automated harnesses in `/tests/integration/plugin_bridge/`.
- Requires disciplined schema governance — handled by shared repo ownership and ABI/proto diff tests.

**Follow-up Actions**

- Ensure bridge manifests declare gRPC/WebSocket endpoints and required credentials.
- Update developer guides to clarify when to use struct APIs versus gRPC clients.
- Coordinate with Ops to provide deployment runbooks for bridge hosting.

---

## 4) Rationale

gRPC/WebSocket surfaces deliver deterministic contracts and streaming required by UI and automation scenarios, while struct ABIs cover low-latency plugin needs. Scoping gRPC to the bridge plugin harmonizes both goals and prevents divergent contract governance.

---

## 5) Alternatives Considered

| Option | Summary | Reason Not Selected |
|--------|---------|---------------------|
| gRPC everywhere | Keep original plan where plugins and UI use gRPC. | Adds latency for in-process plugins; duplicates hosting overhead. |
| REST/JSON facade | Provide REST endpoints for remote clients. | Lacks streaming semantics; increases latency. |
| MQTT/AMQP | Adopt brokered pub/sub stack. | Requires new infrastructure; overlaps with telemetry mesh work. |

---

## Change Log

| Date | Summary | Author |
|------|---------|--------|
| 2025-10-26 | Scoped ADR to bridge plugin and aligned with struct-first decision. | Nexus Team (Codex) |
| 2025-10-20 | Initial draft (gRPC-first). | Nexus Team (Codex) |

