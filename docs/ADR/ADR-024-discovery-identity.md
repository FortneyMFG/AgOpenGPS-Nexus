# ADR-024: Discovery and identity services

## Status
Drafting (target review window: 2025-12-15 week)

## Context
Multiple controllers, plugins, and firmware nodes must discover each other, exchange capabilities, and present operator-friendly identities across transports. Current discovery flows lack consistent naming, leases, and security posture, creating confusion and risk in multi-node rigs. ADR-024 defines discovery, identity, and security expectations aligned with ADR-018 plugin API, ADR-016 firmware transports, and ADR-031 manifest governance.

## Decision
- Specify identity schemas and naming conventions for rigs, nodes, and capabilities, including lease renewals and retirement flows.
- Implement discovery watchers and capability handshakes that complete quickly across transports while honoring permission models.
- Enforce security controls (e.g., mTLS) for remote nodes with actionable telemetry when authentication fails.
- Provide operator-facing UI for managing identities (rename, retire) with audit logging tied to provenance pipelines.

## Consequences
- Operators gain clarity when managing multi-node rigs, improving troubleshooting and governance.
- Security requirements increase implementation complexity but mitigate unauthorized access risks.
- Discovery and identity services must integrate tightly with manifests and capability registries, requiring coordination across teams.

## Governance Updates
- **Zero-touch provisioning.** Device onboarding supports QR-code and pre-shared bootstrap token flows that enroll hardware without manual certificate copying.
- **Disaster recovery.** Compromised identities trigger automated certificate revocation, credential rotation checklists, and offline recovery steps documented in the incident response runbook.
- **Lease tuning.** Identity leases include adaptive renewal timers tolerant of high-latency field links while maintaining revocation responsiveness.

## Validation
- Discovery handshake must complete within two seconds for five-node rigs and populate the identity registry with unique IDs verified across transports.
- Security model must enforce mTLS authentication and reject unauthenticated clients with structured telemetry events.
- Operator identity UI must support rename/retire flows covered by automated UI tests with telemetry for every change.

## References
- [Communications & transports requirements](../SRS/sections/03_Comm_Transports.md)
- [Hardware I/O requirements](../SRS/sections/06_Hardware_IO.md)
- [Extensibility & plugin requirements](../SRS/sections/12_Extensibility_Plugins.md)
- [ADR-016: Firmware and transport for variable-rate layer PGNs](ADR-016-firmware-transport-variable-rate-pgns.md)
- [ADR-018: Plugin API and capability discovery](ADR-018-plugin-api.md)
- [ADR-031: Official plugin bundle governance](ADR-031-official-plugin-bundle.md)
