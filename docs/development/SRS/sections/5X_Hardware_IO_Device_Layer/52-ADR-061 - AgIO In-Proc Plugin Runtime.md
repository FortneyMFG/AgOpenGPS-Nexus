# 52-ADR-061 — AgIO In-Process Plugin Runtime

*(Status: Drafting)*

**Authors:** Nexus Team (Codex)
**Created:** 2025-10-24
**Last Updated:** 2025-10-24

---


- **Status:** Draft
- **Deciders:** Hardware & IO WG, Core WG
- **Date:** 2025-10-24
- **Section:** 52 — AgIO Service
- **Related Tickets:** NX-PP-018
- **Supersedes / Amends:** Complements ADR-028 (service split); proposes parallel in-proc option.

## 1. Context

The Nexus stack currently treats AgIO as a privileged sidecar process (ADR-028). That design cleanly isolates driver crashes and
aligns with existing gRPC-based plugin transports. However, several deployment profiles push for a single-binary footprint:

- **Constrained rigs** (CM5, ruggedized tablets) want fewer processes and simpler service orchestration.
- **Simulation / CI harnesses** already host drivers in-process for determinism and faster iteration.
- **Plugin option analysis** in SRS §21.5.3 and §52-O2 now models a Core-hosted AgIO plugin exchanging read-only C# structs with
  other plugins while a dedicated bridge plugin maintains gRPC/WebSocket endpoints for remote clients.

We must document how an in-proc AgIO plugin coexists with the managed-service model so SRS guidance stays decision neutral until
the working group makes a final call.

## 2. Decision

Offer **AgIO as a Core-hosted plugin** in addition to the existing sidecar. The in-proc mode:

1. Loads AgIO through the Core plugin container using a versioned struct/record ABI shared with other plugins.
2. Exposes the same capabilities, leases, and health telemetry via a **Bridge plugin** that projects gRPC/WebSocket endpoints.
3. Keeps all structs read-only; command pathways continue to flow through explicit request APIs (gRPC or struct commands) so Core
   preserves authority and audit trails.
4. Ships ABI compatibility tests that fail the build when structs diverge from protobuf contract definitions.
5. Requires explicit configuration (bundle profile, manifest flag, or CLI switch) to opt into the in-proc layout so operators can
   choose between sidecar and plugin deployments per environment.

## 3. Consequences

### Positive

- **Simpler packaging** for single-binary releases; no separate service supervisor required on Windows or minimal Linux installs.
- **Zero-copy exchanges** between AgIO and other plugins (rate control, guidance) when they share structs in-process, reducing
  latency and allocations.
- **Deterministic simulation parity**: the same struct feeds drive both live and replay scenarios without network serialization.

### Neutral / Requires follow-up

- Bridge plugin must be bundled whenever remote UIs or automation clients need gRPC/WebSocket access.
- Release tooling must choose the correct deployment manifest (sidecar vs in-proc) and ensure only one host is active at a time.

### Negative / Risks

- Shared fault domain: an AgIO crash now terminates Core unless the plugin container catches the fault; mitigation requires the
  existing supervision hooks plus aggressive restart policies.
- ABI drift can break Core-hosted plugins if struct versions are not carefully managed.
- Platform security review must revisit permission scopes because AgIO now shares process memory with Core.

## 4. Alternatives Considered

1. **Keep only the sidecar (status quo).**
   - *Pros:* Maximum fault isolation, existing tooling unchanged.
   - *Cons:* Adds service management overhead for constrained devices; duplicates config when Core already runs.

2. **Embed AgIO directly into Core without plugin/container boundaries.**
   - *Pros:* Simplest possible footprint.
   - *Cons:* Loses manifest governance, restart semantics, and breaks ADR-018 capability leasing.

3. **Move all drivers to a new hardware microservice.**
   - *Pros:* Strong isolation and potentially different runtimes per driver class.
   - *Cons:* Increases latency and operational complexity; diverges from SRS §21 option analysis.

## 5. Rollout & Open Issues

- Define deployment profiles (`bundles/`) that toggle sidecar vs in-proc hosting.
- Update CI pipelines to run ABI compatibility tests and bridge parity harnesses (ties to R-AGIO-008).
- Coordinate with Security WG on revised threat modeling for shared-process deployments.
- Document operator guidance covering when to choose each option and how to migrate between them.

---

## Change Log

| Date | Summary | Author | PR / Issue |
|------|---------|--------|------------|
| 2025-10-24 | Initial draft | Nexus Team (Codex) |  |

