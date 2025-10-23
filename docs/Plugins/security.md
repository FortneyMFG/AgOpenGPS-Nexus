# Plugin Security Guide

Secure plugins protect operator data, respect hardware boundaries, and
maintain deterministic behaviour across the fleet. Use this guide when
authoring new plugins or reviewing third-party submissions.

## Core Principles

- **Least privilege:** Declare only the capabilities and permissions you
  require in `manifest.json`. Governance tooling validates manifests
  against [ADR-031](../development/SRS/sections/9X_Frontends_Ops/94-ADR-031 - Official Plugin Bundle Dependency Governance.md)
  and blocks over-scoped submissions.
- **Capability leases:** Follow the lease patterns documented in the
  [plugin architecture](architecture.md) to avoid long-lived access to
  telemetry, layer edits, or hardware control surfaces.
- **Identity & provenance:** Ensure manifests include publisher and
  provenance metadata so the host can trace updates and enforce rollout
  policy.
- **Telemetry hygiene:** Route diagnostics through the host telemetry
  sinks rather than ad-hoc logging. Sensitive payloads must respect the
  retention workflow outlined in the [mesh retention guide](../development/howto/mesh-retention-privacy-operations-guide.md).

## Required Reading

- [SRS §9.5 Security & Permissions](../development/SRS/sections/9X_Frontends_Ops/95_Security_Permissions.md)
- [Mesh security penetration tests](../development/qa/mesh-security-penetration-tests.md)
- [Plugin manifest governance](../development/howto/plugin-manifest-governance.md)
- [Plugin QA handshake](../development/qa/plugin-qa-handshake.md)

Document mitigations for any security exceptions and capture the
corresponding ticket ID in `tasks.md` so governance reviews stay
auditable.
