# 42-ADR-024 — Discovery and identity services
*(Status: Proposed)*

**Author:** Codex
**Reviewers:** Interprocess Communications Working Group
**Created:** 2025-10-20
**Last Updated:** 2025-10-20
**Status:** Proposed
**Version:** 0.1.0
**Supersedes:** _None_
**Superseded by:** _None_
**Related SRS:** [42 — Transports](42_Transports.md)
**Related Considerations:** [C1 - Legacy PGN Transport Stewardship](42_Transports.md#c1---legacy-pgn-transport-stewardship), [C4 - Plugin Leases & Security Enforcement](42_Transports.md#c4---plugin-leases--security-enforcement)

---

## 1) Context

Multiple controllers, plugins, and firmware nodes must discover each other, exchange capabilities, and present operator-friendly
identities across transports. Current discovery flows lack consistent naming, leases, and security posture, creating confusion and
risk in multi-node rigs.【F:docs/development/SRS/sections/4X_Interprocess_Communications/42_Transports.md†L134-L205】

```mermaid
flowchart LR
  A[Ad-hoc discovery] --> B[Inconsistent identities]
  B --> C[Standardized registry]
  C --> D[Governed leases + security]
```

---

## 2) Decision

Define discovery, identity, and security expectations aligned with plugin APIs, firmware transports, and manifest governance.

### Decision Summary

* **Scope:** Identity schemas, discovery watchers, capability handshakes, and operator workflows.
* **Boundary:** Transport-level encryption governed by ADR-043 (Channel Security); this ADR focuses on identity orchestration.
* **Implementation Level:** Design + code; registry services, UI surfaces, and telemetry instrumentation.

---

## 3) Consequences

**Positive Impacts:**

* Operators gain clarity managing multi-node rigs via consistent naming and lifecycle workflows.
* Security posture improves with mTLS requirements and actionable telemetry when authentication fails.
* Discovery integrates with manifests and capability registries, enabling deterministic plugin enablement.

**Negative / Mitigated Impacts:**

* Increased implementation complexity — mitigated via shared registry services and UI tooling.
* Requires coordination across teams (Core, AgIO, UI) — addressed by joint working group milestones.
* Offline provisioning workflows add surface area — covered by zero-touch onboarding guidelines.

**Follow-up Actions:**

* Implement identity schemas with lease renewals, retirement flows, and audit logging.
* Build discovery watchers that complete within two seconds for five-node rigs across transports.
* Deliver desktop `IdentityRegistryViewModel` for rename/retire operations emitting provenance events.

---

## 4) Rationale

Standardized identity services reduce operator confusion, enforce security policies, and tie discovery to capability registries,
ensuring plugins and firmware negotiate permissions before streaming control data. Audit trails feed provenance pipelines for
compliance and diagnostics.

---

## 5) Alternatives Considered

| Option | Summary | Reason Not Selected |
|--------|---------|---------------------|
| Status quo discovery | Continue ad-hoc UDP announcements. | Inconsistent naming, no leases, weak security. |
| Manual registry management | Rely on operators editing config files. | Error-prone, lacks telemetry, unsuitable for multi-node rigs. |
| External identity provider | Outsource discovery to third-party IAM. | Adds dependencies and complexity unsuited for offline rigs. |

