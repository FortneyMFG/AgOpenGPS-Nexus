# 41-ADR-062 — Struct ABI Plugin Contracts with Bridge Adapter

*(Status: Drafting)*

**Authors:** Nexus Team (Codex)
**Created:** 2025-10-24
**Last Updated:** 2025-10-24

---


- **Status:** Draft
- **Deciders:** Interprocess Communications WG, SDK WG
- **Date:** 2025-10-24
- **Section:** 41 — Service APIs & Contracts / 42 — Transports
- **Related Tickets:** NX-PP-018
- **Supersedes / Amends:** Complements ADR-002 (gRPC contracts) by defining a parallel struct ABI surface.

## 1. Context

ADR-002 established gRPC/Protobuf contracts as the canonical plugin interface. As option analysis in SRS §21.5.3, §41.5.4, and
§42.5 now shows, Nexus also wants to host selected plugins in-process (e.g., AgIO, guidance, mapping) without gRPC serialization
costs. Contributors requested a decision record that documents how struct/record ABIs relate to the existing protobuf files, how
they remain versioned, and how remote clients still attach via gRPC/WebSocket bridges.

## 2. Decision

Define a **dual-surface contract strategy**:

1. **Shared schema source.** Protobuf files remain the single source of truth. A code-generation step produces both the protobuf
   messages and C# `record struct` types in a `Contracts.Struct` assembly.
2. **ABI governance.** Each struct type carries `[StructLayout(LayoutKind.Sequential, Pack = 1)]` metadata plus a semantic version
   attribute. Automated ABI tests compare field order, size, and alignment against the previous release.
3. **Read-only semantics.** Structs expose read-only spans/refs. Mutations require command APIs (still routed through gRPC or
   dedicated struct commands) so Core retains control authority.
4. **Bridge plugin.** A `Contracts.Bridge` package provides converters that project struct payloads over gRPC/WebSocket when
   plugins run in-process. Remote clients see the same protobuf contracts as today.
5. **Capability discovery.** The capability registry publishes both protobuf schema hashes and struct ABI versions so hosts verify
   compatibility before loading plugins.

## 3. Consequences

### Positive

- In-process plugins get zero-copy access to telemetry and control data while sharing the exact same schema as gRPC clients.
- Bridge implementations stay simple because generated converters handle struct↔protobuf translation.
- ABI/version metadata appears alongside existing manifest validation, enabling tooling to warn about incompatible plugins.

### Neutral / Requires follow-up

- Build pipelines must run ABI diff tests and package the generated struct assemblies.
- Documentation must clearly describe when to choose struct vs gRPC and how to write bridge-friendly plugins.

### Negative / Risks

- ABI drift could still ship if version annotations are ignored; mitigation relies on CI gates.
- Shared-memory access increases the blast radius of plugin bugs; Core must enforce read-only handles diligently.

## 4. Alternatives Considered

1. **Remain gRPC-only.**
   - *Pros:* Keeps surface area minimal; existing tooling unaffected.
   - *Cons:* Misses latency and packaging wins for in-proc deployments; contradicts SRS option analysis.

2. **Define independent struct schemas by hand.**
   - *Pros:* Tailor structs to in-proc performance.
   - *Cons:* High drift risk; duplicate maintenance effort; bridge becomes error-prone.

3. **Adopt shared-memory serialization (flatbuffers, cap'n proto).**
   - *Pros:* Zero-copy across processes.
   - *Cons:* Requires new tooling, reworks ADR-002 commitments, and complicates legacy compatibility.

## 5. Rollout & Open Issues

- Extend contract generation tooling to emit struct assemblies and bridge converters (ties to R-STRUCT-003, R-COMM-050…052).
- Update plugin SDK documentation to describe struct ABI usage, version negotiation, and bridge packaging.
- Define manifest fields for struct ABI versions and enforce them at load time.
- Coordinate with Security WG to review memory-safety implications of exposing struct pointers to plugins.

---

## Change Log

| Date | Summary | Author | PR / Issue |
|------|---------|--------|------------|
| 2025-10-24 | Initial draft | Nexus Team (Codex) |  |

