# 43 — Channel Security
*(Status: Proposed)*

**Author:** Codex
**Created:** 2025-10-20
**Status:** Proposed
**Version:** 0.1.0
**Section ID:** 43
**Editors:** Interprocess Communications Working Group
**Last Updated:** 2025-10-20
**Related Sections:** 42 — Transports, 95 — Security & Permissions, 96 — Quality Engineering & Release
**Upstream Dependencies:** 1X — Platform Foundations, 2X — System Architecture
**Downstream Impacts:** 6X — Core Domain Services, 7X — Mapping & Geospatial, 9X — Frontends & Ops

---

## 43.1 Purpose & Scope

Establish authentication, encryption, authorization, and audit policies for inter-process and inter-device links (gRPC,
WebSocket, UDP bridges, BLE, SocketCAN) so Nexus deployments remain secure across headless Linux nodes, remote clients, and
legacy PGN bridges.

---

## 43.2 Context

- Remote front-ends and automation tooling require secure access to Core APIs over shared networks.
- Field rigs often operate offline, demanding pairing workflows that do not rely on constant internet connectivity.
- Transport bridges must balance encryption requirements with legacy hardware that lacks crypto acceleration.
- Security events must integrate with telemetry pipelines for centralized monitoring and alerting.

---

## 43.3 Legacy Comparison

| Area / Theme | Legacy Behavior | Identified Limitation | Modernization Opportunity | Reference / Source |
|---------------|-----------------|------------------------|---------------------------|--------------------|
| Channel Encryption | Ad-hoc TLS usage; many PGN paths unencrypted. | Inconsistent authentication; difficult certificate management. | Standardize on TLS 1.3/mTLS with automated provisioning. | Security & permissions backlog |
| Identity Management | Manual credential sharing per device. | No revocation or rotation plan; shared secrets linger. | Issue per-device certificates with renewal workflows. | Field deployment notes |
| Audit Visibility | Logs stored locally without aggregation. | Hard to detect handshake failures or policy overrides. | Emit audit events into centralized telemetry streams. | Ops observability reports |

> **Informative:** Captures historical context and modernization drivers.

---

## 43.4 Definitions

| Term | Definition |
|------|-------------|
| mTLS | Mutual TLS requiring both client and server certificates.
| Pairing Token | Short-lived credential used to bootstrap a new device without network access.
| Capability Scope | Enumerated permission (e.g., `pose.read`, `section.command`) bound to authenticated channels.
| Security Audit Event | Structured log capturing handshake results, certificate status, and policy overrides.

---

> **Requirement Grammar (RFC-2119):**
> - **MUST / MUST NOT** = mandatory; test must exist.
> - **SHOULD / SHOULD NOT** = strong recommendation; justify exceptions.
> - **MAY** = optional; document enabling conditions.
>
> **Clarity Checklist:** Avoid weak words: *fast, robust, user-friendly, handle, support, adequate,* etc.
> Prefer measurable forms: *“≤ 250 ms p95,” “error rate < 0.1%,” “99.5% success over 10k trials.”*
> Each requirement: single behavior, single actor, single condition, single metric.

## 43.5 Requirements

| ID | Priority | Category | Summary | Source / C-IDs | Key Metrics / Verification |
|----|-----------|-----------|---------|----------------|-----------------------------|
| R-CHAN-000 | MUST | Encryption | Require TLS 1.3/mTLS for gRPC/WebSocket channels and DTLS/HMAC for UDP/SocketCAN where supported. | C1 | Security tests confirm negotiation; legacy fallback documented. |
| R-CHAN-001 | MUST | Identity | Issue per-device certificates tied to Nexus identity records with renewal and revocation. | C2 | PKI automation integration tests rotate certs without downtime. |
| R-CHAN-002 | SHOULD | Rotation | Automate key rotation on configurable cadence with rolling restarts. | C2 | Rotation suite rotates within ≤5 minutes, zero guidance interruption. |
| R-CHAN-003 | MUST | Authorization | Bind channel capabilities to authenticated identities enforcing least privilege. | C3 | Access control tests reject unauthorized capability requests. |
| R-CHAN-004 | SHOULD | Observability | Emit audit events (handshake success/failure, cert expiry, overrides) into telemetry pipelines. | C4 | Telemetry dashboards display events within 60 seconds. |
| R-CHAN-005 | MUST | Offline Pairing | Provide offline pairing workflows (QR codes, short-lived tokens) issuing unique credentials per device. | C5 | Offline pairing test provisions device in ≤3 minutes without network. |

### 43.5.1 Requirement Sources & Rationale

| Req ID | Source (issue/discussion/standard) | Rationale (one line) |
|--------|-------------------------------------|----------------------|
| R-CHAN-000 | Security & permissions reviews | Ensure secure channels across remote deployments. |
| R-CHAN-001 | PKI backlog | Provide revocation and renewal pathways. |
| R-CHAN-003 | Capability enforcement workshop | Prevent unauthorized control actions. |
| R-CHAN-005 | Offline deployment reports | Maintain security parity without internet access. |

---

## 43.6 Acceptance Criteria & Verification

- Automated security test suite MUST establish TLS/mTLS sessions, rotate certificates, and validate capability scoping.
- Offline pairing drills MUST complete without internet connectivity and produce audit trails of issued credentials.
- Telemetry dashboards MUST surface audit events with severity metadata for operations follow-up.

### 43.6.1 Requirement-to-Verification Map

| Req ID | Verification Type | Artifact / Location | Pass/Fail Threshold |
|--------|--------------------|---------------------|---------------------|
| R-CHAN-000 | Integration tests | `/tests/security/channel_tls/` | 100% handshake success under supported transports. |
| R-CHAN-002 | Chaos tests | `/tests/security/rotation/` | Rolling rotation completes within SLA; zero dropped sessions. |
| R-CHAN-003 | Authorization tests | `/tests/security/capability_acl/` | Unauthorized capability requests rejected. |
| R-CHAN-004 | Telemetry verification | `/dashboards/security-audit/` | Events visible within 60 seconds with severity tags. |

---

## 43.7 Constraints

- Security baselines MUST apply uniformly to Windows, Linux, and embedded bridges.
- Certificate storage MUST tolerate offline renewal windows without exposing private keys.
- Audit logs MUST avoid sensitive payload leakage while retaining actor, channel, and policy metadata.

### 43.7.1 Non-Functional Requirement Classes

- **Performance:** TLS handshakes complete ≤ 500 ms; channel overhead adds ≤ 5% latency to control loops.
- **Reliability & Availability:** Credential rotation avoids downtime; fallback policies documented for hardware without crypto acceleration.
- **Security:** Key material stored in OS-protected keystores; revocation lists synchronized on startup and every 15 minutes when online.
- **Operability:** Dashboards and alerts highlight expiring certificates ≥ 7 days in advance; audit logs searchable by device/channel.
- **Maintainability:** PKI automation scripts versioned with Infrastructure-as-Code; pairing workflows documented with operator runbooks.

---

## 43.8 Risks & Open Issues

| ID | Description | Impact | Mitigation / Status | Owner |
|----|-------------|--------|---------------------|-------|
| RISK-43-1 | Legacy hardware lacks crypto acceleration for DTLS/HMAC. | Medium | Provide documented fallback plus roadmap for hardware refresh. | @interop-wg |
| RISK-43-2 | Certificate rotation interrupts ongoing guidance. | High | Stage rehearsals in CI; support overlapping cert validity windows. | @interop-wg |
| ISSUE-43-1 | Define standardized offline pairing UX. | Medium | Coordinate with Ops WG to prototype QR/token flows. | @interop-wg |

---

## 43.9 Design Considerations

| ID | Consideration | Description |
|----|----------------|-------------|
| C1 | Encrypted Transport Baseline | All typed channels prefer TLS 1.3/mTLS with DTLS/HMAC for UDP bridges. |
| C2 | Device Identity Lifecycle | Certificates issued per device with automated renewal and revocation. |
| C3 | Capability-Scoped Authorization | Channel permissions enforce least privilege for pose, section, storage operations. |
| C4 | Security Observability | Telemetry pipelines capture audit events for monitoring and incident response. |
| C5 | Offline Pairing & Recovery | Field rigs pair securely without internet while retaining audit trails and revocation control. |

### 43.9.1 Assumptions & Preconditions

- [A1] PKI automation tooling can operate in connected environments and cache revocation state for offline spans.
- [A2] Operators have access to secure devices (tablet/phone) to complete QR or token-based pairing steps.
- [A3] Telemetry backends accept signed events from field rigs once connectivity resumes, replaying buffered audit logs.

#### C1 - Encrypted Transport Baseline

- TLS 1.3 with mutual authentication is the default for gRPC/WebSocket channels; DTLS or per-frame HMAC secures UDP/SocketCAN when hardware supports it.
- Fallback modes document acceptable risk (e.g., isolated networks) and require explicit operator acknowledgement.
- Cipher suites align with industry recommendations (AES-GCM, ChaCha20-Poly1305) and disable deprecated algorithms.

#### C2 - Device Identity Lifecycle

- Certificates bind to Nexus identity records (CM5, tablets, servers) with issuance logs stored centrally.
- Renewal automation rotates certificates on a 30-day cadence with overlapping validity windows.
- Revocation lists load at Core startup and refresh when connectivity is available.

#### C3 - Capability-Scoped Authorization

- Capabilities enumerated as `pose.read`, `section.command`, `storage.write`, etc., with policy definitions per role.
- Bridge and Core enforce capability checks before streaming or accepting commands.
- Audit logs include capability decisions and actor identity for traceability.

#### C4 - Security Observability

- Audit events capture handshake success/failure, certificate expiry, policy overrides, and offline pairing results.
- Telemetry pipelines tag events with severity; dashboards alert on critical thresholds (e.g., <7 days to certificate expiry).
- Operators review audit reports during release readiness and after incidents.

#### C5 - Offline Pairing & Recovery

- Offline rigs issue pairing tokens (QR or numeric) that expire within minutes and map to unique device certificates.
- Pairing tool caches issued credentials securely until connectivity returns for central reconciliation.
- Recovery procedures document how to revoke compromised tokens and reprovision devices without reinstalling transports.

---
