# Security & Permissions (Status: collecting proposals)

## Problem statement
Identify how credentials, operator roles, and device access are managed today and what controls are needed as the platform grows.

## Requirements (from contributors)
- R-SEC-000 (MUST, current-AgIO): Continue supporting stored NTRIP credentials while planning a more secure secrets flow (currently saved in application settings).【F:SourceCode/AgIO/Source/Forms/FormNtrip.cs†L58-L120】
- R-SEC-001 (MUST, current-AgOpenGPS): Preserve offline operation without requiring cloud authentication given field connectivity constraints.【F:README.md†L28-L33】
- R-SEC-002 (SHOULD): Provide guidance on user roles/permissions if shared workstations become common.
- R-SEC-003 (SHOULD): Encrypt or obfuscate sensitive config values at rest without breaking existing upgrade paths.
- R-SEC-004 (SHOULD, proposed-LinuxCore): Run the Linux Core under a dedicated service account with least-privilege access to `/dev` devices and config directories, documenting how credentials are stored for remote clients.【F:docs/SRS/options/O-BACKEND-6_LinuxCoreService.md†L21-L44】
- R-SEC-005 (COULD): Add audit logging for configuration changes and remote connections.
- R-SEC-006 (SHOULD, secrets migration): Define an encrypted storage format, backup/restore workflow, and migration plan for existing plaintext secrets before enabling remote Core access.
- R-SEC-007 (SHOULD, audit readiness): Establish minimum audit requirements (timestamped operator actions, remote session trails retained for at least one season) so security-sensitive ADRs have clear acceptance criteria.

## Options
- O-SEC-0: Status quo — Windows user accounts + stored settings for credentials.
- O-SEC-1: Introduce a secrets vault (DPAPI, OS keychain) for sensitive data.
- O-SEC-2: Add in-app user roles with permission gating.
- O-SEC-3: Move to centralized auth (OAuth/OpenID) for remote services.
- O-SEC-4: Provide signed firmware/config packages to prevent tampering.

## Comparison (quick matrix)
| Option | Pros | Cons | Risks | Borrow from existing |
|---|---|---|---|---|
| O-SEC-0 | Works offline, simple | Credentials in plain settings | Machine compromise exposes secrets | Existing settings store |
| O-SEC-1 | Protects passwords | Platform-specific work | Lockouts if key lost | Windows DPAPI |
| O-SEC-2 | Limits accidental changes | Complexity | Operator friction | Settings dialogs |
| O-SEC-3 | Unified identity | Needs internet | Login outages halt work | Web services |
| O-SEC-4 | Prevents config tampering | Signing infrastructure | Firmware update logistics | Release pipeline |

## Evaluation criteria
Offline usability, credential safety, operator workflow impact, implementation complexity, backward compatibility.

## Current sentiment
- Security is light today; we must secure credentials and config changes without breaking offline workflows.
- Any Linux service rollout must demonstrate least-privilege defaults and credential handling before the community adopts it broadly.【F:docs/SRS/options/O-BACKEND-6_LinuxCoreService.md†L21-L44】【F:docs/SRS/options/O-FRONT-6_RemoteClients.md†L21-L34】

## Open questions
- How do we migrate stored passwords when introducing encryption?
- Do we need per-operator audit logs for regulatory compliance?

## Related ADRs

- [ADR-019 — Provenance, Audit, & QA](../../ADR/ADR-019-provenance-audit-qa.md)
- [ADR-024 — Discovery & Identity](../../ADR/ADR-024-discovery-identity.md)
- [ADR-028 — Stack Boundaries](../../ADR/ADR-028-stack-boundaries.md)
- [ADR-031 — Official Plugin Bundle](../../ADR/ADR-031-official-plugin-bundle.md)
