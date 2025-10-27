# 41-ADR-062 — Struct ABI Plugin Contracts with Bridge Adapter

*(Status: Accepted)*

**Authors:** Nexus Team (Codex)  
**Created:** 2025-10-24  
**Last Updated:** 2025-10-26  
**Deciders:** Interprocess Communications WG, SDK WG  
**Section:** 41 — Service APIs & Contracts / 42 — Transports  
**Related Tickets:** NX-PP-018  
**Supersedes / Amends:** Re-scopes ADR-002 by making struct ABIs canonical for plugins while retaining gRPC for the bridge.

---

## 1. Context

ADR-002 originally positioned gRPC/protobuf as the canonical interface for Core, UI, and plugins. Option analysis in SRS §21, §41, and §42 demonstrated that in-process hosting demands allocation-free, low-latency access while the UI still requires language-neutral endpoints. Without a clear decision, teams risked duplicating ADRs for each plugin, inflating governance overhead and blurring ownership of struct experiments already underway.

## 2. Decision

Adopt a **struct-first contract model** for plugins hosted in Core while limiting gRPC/WebSocket usage to a bridge plugin for UI and automation clients.

1. **Schema single source.** Contract schemas (proto/IDL) generate both protobuf payloads and C# `record struct` types inside `Aog.Contracts.Struct`. The struct definitions carry `[StructLayout(LayoutKind.Sequential, Pack = 1)]` and semantic version metadata.
2. **ABI governance.** CI runs ABI diff tests to block breaking changes unless the semantic version is incremented. Manifests and the capability registry publish ABI fingerprints (hash + version) for validation.
3. **Read-only semantics.** Generated structs expose read-only views and spans. Mutations require explicit command APIs so Core retains authority.
4. **Bridge plugin.** `Aog.Contracts.Bridge` provides generated converters that project struct payloads onto gRPC/WebSocket endpoints. Remote clients only interact with the bridge; plugins no longer bind gRPC servers directly.
5. **Legacy compatibility.** Converter helpers translate legacy PGNs/nanopb payloads into struct contracts, ensuring deterministic replay during migration.

## 3. Consequences

### Positive

- Plugins gain zero-copy access with deterministic layouts while sharing schemas with the bridge and automation clients.
- Governance focuses on a single schema repository plus this ADR, reducing ADR sprawl per plugin.
- Bridge implementations become thinner because auto-generated converters guarantee parity with struct payloads.

### Neutral / Follow-up

- Build tooling must emit struct assemblies alongside protobuf clients and publish ABI fingerprints.
- SDK documentation and manifests require updates describing struct expectations, bridge packaging, and validation flows.
- Security review must confirm read-only semantics and guard rails for struct access.

### Negative / Risks

- ABI drift could still slip through if teams bypass CI; mitigated by mandatory ABI diff gates.
- Shared-memory style access increases the blast radius of plugin bugs; Core must enforce read-only handles and defensive copies where necessary.

## 4. Alternatives Considered

1. **Remain gRPC-only.**
   - *Pros:* Minimal surface area; existing tooling unaffected.
   - *Cons:* Keeps high latency for in-process plugins; complicates Linux packaging.

2. **Hand-author struct schemas.**
   - *Pros:* Tailor structs to specific plugins.
   - *Cons:* High drift risk; duplicates schema maintenance; breaks parity with bridge payloads.

3. **Adopt shared-memory serializers (FlatBuffers, Cap'n Proto).**
   - *Pros:* Zero-copy across processes.
   - *Cons:* Requires new tooling, diverges from existing proto ecosystem, delays delivery.

## 5. Rollout & Open Issues

- Update SRS §41 and §42 to reflect struct-first contracts (completed with this revision).
- Extend code generation pipelines to emit struct assemblies and converter helpers; hook ABI diff tests into CI.
- Add manifest fields for ABI fingerprints, minimum/maximum supported versions, and bridge requirements.
- Coordinate with Security WG on memory-safety posture and audit coverage.

---

## Change Log

| Date | Summary | Author |
|------|---------|--------|
| 2025-10-26 | Accepted decision; clarified gRPC scope and governance responsibilities. | Nexus Team (Codex) |
| 2025-10-24 | Initial draft. | Nexus Team (Codex) |

