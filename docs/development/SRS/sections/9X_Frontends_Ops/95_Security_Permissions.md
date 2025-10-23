# 95 — Security & Permissions
*(Status: Proposed)*

**Author:** Codex  
**Created:** 2025-10-20  
**Version:** 0.1.0  
**Section ID:** 95  
**Editors:** Frontend & Operations Working Group  
**Last Updated:** 2025-10-20  
**Related Sections:** 21 — System Architecture, 91 — UI Shell & Layout, 94 — Extensibility & Packaging Updates, 96 — Quality Engineering & Release  
**Upstream Dependencies:** 1X — Platform Foundations, 5X — Hardware IO Device Layer  
**Downstream Impacts:** Plugin governance, deployment pipelines, audit & compliance tooling

---

## 95.1 Purpose & Scope

Define the security posture and permission model for Nexus frontends and operations. This section addresses credential storage, offline requirements, service accounts, audit logging, and plugin permission enforcement so modernization efforts do not compromise operator trust or regulatory readiness.【F:docs/sections/2X_System_Architecture/21-O6 - Linux Core service with remote frontends.md†L21-L44】【F:docs/sections/9X_Frontends_Ops/94_Extensibility_Packaging_Updates.md†L230-L334】

---

## 95.2 Context

- Legacy applications store credentials in plaintext settings to satisfy offline workflows.  
- Linux Core deployments and remote clients introduce multi-user, multi-machine environments requiring least privilege and audit trails.  
- Plugin manifests declare capabilities that must be enforced at runtime to prevent untrusted bundles from issuing control commands.  
- Regulatory expectations demand traceability for operator actions, remote sessions, and configuration changes.

---

## 95.3 Legacy Comparison

| Area / Theme | Legacy Behavior | Identified Limitation | Modernization Opportunity | Reference / Source |
|---------------|-----------------|------------------------|---------------------------|--------------------|
| Credential Storage | Plaintext settings per executable. | Secrets exposed if workstation compromised. | Encrypted secrets vault with migration plan. | AgIO NTRIP settings【F:SourceCode/AgIO/Source/Forms/FormNtrip.cs†L58-L120】 |
| Offline Access | Full functionality without authentication. | No concept of operator roles or audit. | Offline-ready credential cache with optional role enforcement. | README offline notes【F:README.md†L28-L33】 |
| Plugin Permissions | Implicit trust of loaded assemblies. | Untrusted code can access sensitive APIs. | Manifest-scoped permission gate tied to capability registry. | ADR-031 governance |

---

## 95.4 Definitions

| Term | Definition |
|------|-------------|
| Secrets Vault | Encrypted storage for credentials (DPAPI, OS keychain, libsecret) with backup/restore workflow. |
| Permission Scope | Named capability (`pose.read`, `section.command`, etc.) required to access protected APIs. |
| Service Account | Dedicated OS user for Linux Core deployments with constrained device and filesystem access. |
| Audit Trail | Timestamped log capturing operator actions, remote sessions, and configuration mutations for compliance. |

---

> **Requirement Grammar (RFC-2119):**
> - **MUST / MUST NOT** = mandatory; verification required.
> - **SHOULD / SHOULD NOT** = strong recommendation; justify exceptions.
> - **MAY** = optional; document enabling conditions.

### 95.4.1 Security Model Overview

- **Capability-gated access.** Plugins request scoped leases (`steering.command`, `pose.read`, `settings.write`, etc.) within their manifests; the host enforces these scopes before exposing APIs or hardware endpoints.【F:docs/development/SRS/appendices/samples/plugins/sections/1.1.0.json†L65-L122】【F:docs/development/SRS/sections/9X_Frontends_Ops/94_Extensibility_Packaging_Updates.md†L120-L186】
- **Hardware control safeguards.** Discovery and identity services authenticate devices, validate signed commands, and track watchdog status so authority transfers remain deterministic.【F:docs/development/SRS/sections/4X_Interprocess_Communications/42-ADR-024 - Discovery and identity services.md†L13-L76】【F:docs/development/SRS/sections/5X_Hardware_IO_Device_Layer/54_CM5_Integrated_Controller.md†L63-L78】
- **Role-aligned permission levels.** Operators map Monitor, Operate, Configure, and Manage roles to scope bundles, enabling shared rigs to enforce least privilege while preserving offline access.【F:docs/development/SRS/sections/9X_Frontends_Ops/95_Security_Permissions.md†L45-L88】
- **Authentication surfaces.** Local sessions integrate with OS user accounts and hardware security keys, while remote clients rely on mTLS or OIDC tokens with session lifetimes aligned to §43 channel policies.【F:docs/development/SRS/sections/4X_Interprocess_Communications/43_Channel_Security.md†L1-L115】【F:docs/development/SRS/sections/9X_Frontends_Ops/93_Command_Line_Interface.md†L67-L144】
- **Data protection.** Config stores encrypt sensitive values, manifests may require signing, and telemetry pipelines transport security events for centralized monitoring and alerts.【F:docs/development/SRS/sections/3X_Data_Storage/33_Offline_First_Sync.md†L123-L179】【F:docs/development/SRS/sections/9X_Frontends_Ops/94_Extensibility_Packaging_Updates.md†L186-L211】
- **Audit & monitoring.** Provenance services capture security events (access attempts, configuration edits, hardware commands) and drive alerting when violations occur, satisfying ADR-019 obligations.【F:docs/development/SRS/sections/6X_Core_Domain_Services/64-ADR-019 - Provenance audit and QA governance.md†L21-L86】

The JSON samples and configuration snippets in Appendix profiles demonstrate how manifests and deployment descriptors encode these policies, giving implementers concrete templates while keeping the SRS as the canonical security specification.

## 95.5 Requirements

| ID | Priority | Category | Summary | Source / C-IDs | Key Metrics / Verification |
|----|-----------|----------|---------|-----------------|-----------------------------|
| R-SEC-000 | MUST | Credential Storage | Continue supporting stored NTRIP credentials while planning migration to secure secrets flow. | AgIO NTRIP form【F:SourceCode/AgIO/Source/Forms/FormNtrip.cs†L58-L120】 | Secrets migration test plan |
| R-SEC-001 | MUST | Offline Operation | Preserve offline operation without requiring cloud auth due to connectivity constraints. | Field usage notes【F:README.md†L28-L33】 | Offline smoke tests |
| R-SEC-002 | SHOULD | Role Guidance | Provide guidance on user roles/permissions for shared workstations. | Community support backlog | Role-based UX documentation |
| R-SEC-003 | SHOULD | At-rest Protection | Encrypt or obfuscate sensitive config values without breaking upgrade paths. | Security backlog | Migration scripts + regression |
| R-SEC-004 | SHOULD | Linux Core Hardening | Run Linux Core under dedicated service account with least-privilege access; document credential storage for remote clients. | Linux Core ADR【F:docs/sections/2X_System_Architecture/21-O6 - Linux Core service with remote frontends.md†L21-L44】 | Systemd unit + credential audit |
| R-SEC-005 | COULD | Audit Logging | Add audit logging for configuration changes and remote connections. | Provenance ADR-019 | Audit prototype results |
| R-SEC-006 | SHOULD | Secrets Migration | Define encrypted storage format and migration plan for existing plaintext secrets. | Security roadmap | Migration acceptance tests |
| R-SEC-007 | SHOULD | Audit Retention | Establish minimum audit retention (timestamped actions, remote session trails for ≥ one season). | ADR-019 provenance | Audit retention verification |
| R-SEC-008 | MUST | Plugin Permission Gate | Enforce manifest-declared permission scopes via runtime gate preventing unauthorized capability access. | ADR-031 governance | Permission enforcement tests |

### 95.5.1 Requirement Sources & Rationale

| Req ID | Source | Rationale |
|--------|--------|-----------|
| R-SEC-000 | Legacy credential handling | Maintain continuity while planning secure migration. |
| R-SEC-001 | Operator feedback | Offline workflows remain critical in the field. |
| R-SEC-004 | Linux Core pilots | Ensure remote deployments follow least-privilege practices. |
| R-SEC-007 | Provenance ADR-019 | Provide audit readiness for regulatory reviews. |
| R-SEC-008 | Plugin governance | Prevent untrusted bundles from exercising critical APIs. |

---

## 95.6 Acceptance Criteria & Verification

Security controls undergo automated tests, manual audits, and migration rehearsals. Offline smoke tests confirm usability, while plugin permission gates are validated against manifest fixtures.

### 95.6.1 Requirement-to-Verification Map

| Req ID | Verification Type | Artifact / Location | Pass/Fail Threshold |
|--------|-------------------|---------------------|---------------------|
| R-SEC-000 | Migration rehearsal | `tests/security/SecretsMigration.feature` | Existing credentials migrate to encrypted store without loss |
| R-SEC-001 | Offline regression | `tests/security/OfflineModeSuite` | All core workflows succeed without network connectivity |
| R-SEC-004 | System hardening audit | `docs/security/LinuxCoreHardening.md` | Service account restricts device/config access; credential storage documented |
| R-SEC-007 | Audit verification | `tests/security/AuditRetention.feature` | Audit trail retains events for minimum retention window |
| R-SEC-008 | Permission gate tests | `tests/security/PluginScopeEnforcement.cs` | Unauthorized capability access blocked 100% |

---

## 95.7 Constraints

- Offline operation remains non-negotiable; authentication flows must support air-gapped rigs.  
- Secrets storage must include recovery/backup guidance to avoid permanent lockouts.  
- Permission gates must integrate with plugin manifest schema defined in §94 without imposing breaking changes.

---

## 95.8 Interfaces & Dependencies

- Shares manifest scopes and governance rules with §94 Extensibility.  
- Exposes audit data to §96 Quality Engineering for compliance reporting.  
- Relies on transport hardening and remote client posture defined in §91 and §21.

---

## 95.9 Design Considerations

| ID | Consideration | Description |
|----|----------------|-------------|
| C1 | Secrets vault strategy | Adopt DPAPI (Windows) and OS keychains/libsecret (Linux) with consistent abstraction for backups. |
| C2 | Offline-first auth | Provide credential caching and grace windows so operators remain productive without connectivity. |
| C3 | Service account hardening | Document systemd profiles, device access, and credential rotation for Linux Core deployments. |
| C4 | Audit fidelity | Capture operator identity, plugin source, and action context for replay and compliance. |
| C5 | Plugin scope taxonomy | Maintain canonical permission scope list aligned with capability registry to simplify governance. |

### 95.9.1 Assumptions & Preconditions

- [A1] Operators can provision secure storage mechanisms (DPAPI, libsecret) on supported platforms.  
- [A2] Plugin authors declare permission scopes accurately within manifest metadata.  
- [A3] Audit storage retains sufficient space for per-season retention policies.

---

## 95.10 Option Overview

No alternative security framework is under evaluation; modernization proceeds via considerations C1–C5 while maintaining offline compatibility.

---

## 95.11 Comparison Matrix

| Attribute / Criteria | Plaintext Baseline | Hardened Secrets & Permissions |
|----------------------|--------------------|--------------------------------|
| Credential Safety | Low | High — encrypted storage & rotation |
| Offline Support | High | High — cached credentials with grace windows |
| Plugin Governance | Low | High — runtime permission enforcement |
| Auditability | Low | High — structured audit trails |
| Implementation Complexity | Low | Medium — requires vault abstraction & migration |

---

## 95.12 Decision Matrix

> **Informative:** Detailed weighting deferred until secrets vault pilot confirms cross-platform feasibility; scope decisions align with considerations C1–C5 and ADR-019/ADR-031 guidance.

---

## 95.13 Evaluation & Verification

- Execute credential migration rehearsals covering backup/restore and lockout scenarios.  
- Perform penetration testing against remote client authentication flows.  
- Review audit logs for completeness after simulated incidents (remote connection, preset change, plugin install).

**Acceptance Criteria**

- Encrypted storage and permission gates verified across Windows and Linux deployments.  
- Offline workflows documented with operator guidance.  
- Audit retention meets minimum season-long requirement with export tooling.

---

## 95.14 Implementation Policy

- Store secrets using OS-specific vault providers with fallback to encrypted files guarded by hardware-derived keys.  
- Document credential rotation procedures in `docs/security/credential-rotation.md`.  
- Publish permission scope taxonomy and mapping to manifest metadata in `docs/Plugins/permission-scopes.md`.

---

## 95.15 Community Sentiment

- Operators demand security improvements that do not jeopardize offline workflows.  
- Contributors expect clear permission scopes and enforcement before expanding plugin ecosystem.  
- Linux Core adopters require documented service account hardening and credential policies.【F:docs/sections/2X_System_Architecture/21-O6 - Linux Core service with remote frontends.md†L21-L44】

### 95.15.1 Section Change Log

| Date | Summary | PR / Issue |
|------|---------|------------|
| 2025-10-20 | Converted to new SRS template; formalized security requirements and considerations. | #0000 |

---

## 95.16 Traceability

| Requirement ID | Considerations | ADR(s) | Verification Artifact | Implementation Reference |
|----------------|----------------|--------|-----------------------|--------------------------|
| R-SEC-000 | C1 | — | `tests/security/SecretsMigration.feature` | Credential storage service |
| R-SEC-001 | C2 | — | `tests/security/OfflineModeSuite` | Offline operation runbook |
| R-SEC-004 | C3 | 21-O6 | `docs/security/LinuxCoreHardening.md` | Linux Core deployment scripts |
| R-SEC-007 | C4 | ADR-019 | `tests/security/AuditRetention.feature` | Audit logging pipeline |
| R-SEC-008 | C5 | ADR-031 | `tests/security/PluginScopeEnforcement.cs` | Plugin permission middleware |

---

## 95.17 Conformance

Implementations conform when encrypted credential storage is in place, offline workflows remain functional, audit trails meet retention policy, and plugin permission gates block unauthorized access.

---

## Standards Context

Aligns with OWASP Application Security Verification Standard (ASVS) controls for secrets management, authentication, and logging, while honoring agricultural field requirements for offline capability.
