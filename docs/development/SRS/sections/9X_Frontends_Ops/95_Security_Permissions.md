# 95 — Security, Permissions & Workspace Trust
*(Status: Draft — grid-aware rewrite)*

**Authors:** Security Working Group  
**Version:** 1.0.0  
**Section ID:** 95  
**Related Sections:** 91 — UI Shell, 92 — Blocks, 94 — Extensibility  
**Upstream Dependencies:** 2X — System Architecture, 4X — Channel Security  
**Downstream Impacts:** 96 — Quality Engineering, 97 — Simulation & Replay

---

## 95.1 Purpose

Govern how the grid workspace enforces permissions, authenticates contributors, and safeguards operator trust. Every block, panel, and prompt derives from plugin manifests that declare capability scopes; the shell must respect these declarations across local and remote clients.

---

## 95.2 Role & Capability Model

- **Roles:** Monitor, Operate, Configure, Manage. Roles map to capability bundles controlling which slots become visible or interactive.  
- **Capabilities:** Fine-grained scopes (`telemetry.read`, `machine.control`, `layout.write`, `plugin.install`). Plugins request capabilities per contribution; the host issues leases during activation.  
- **Contextual Enforcement:** Permissions apply per workspace—e.g., a remote Monitor role may view blocks but cannot move or resize them.

---

## 95.3 Requirements

| ID | Priority | Requirement | Notes |
|----|----------|-------------|-------|
| R-SEC-01 | MUST | Manifest Validation | Reject plugin contributions missing capability declarations or targeting unauthorized slots. |
| R-SEC-02 | MUST | Workspace Guardrails | When a role lacks `layout.write`, configuration mode remains locked (unlock button disabled).【F:docs/UI/UI_Demo.html†L16-L55】 |
| R-SEC-03 | MUST | Block Gating | Action blocks require explicit control capabilities; otherwise they render as read-only tiles. |
| R-SEC-04 | SHOULD | Session Isolation | Each operator session maintains independent workspace state unless explicitly shared. |
| R-SEC-05 | MUST | Audit & Provenance | Record every layout change, including actor, source plugin, and before/after state. |
| R-SEC-06 | SHOULD | Remote Hardening | Remote companions authenticate via mTLS or token exchange and receive least-privilege workspace snapshots. |
| R-SEC-07 | MUST | Prompt Safety | Configuration prompts sanitize inputs, enforce schema validation, and respect capability gating. |
| R-SEC-08 | SHOULD | Tamper Detection | Detect unexpected layout diffs (e.g., corrupted manifests) and fall back to last-known-good presets. |

---

## 95.4 Security Operations

- **Credential Storage:** Securely store API keys and device credentials used by plugins; never expose secrets through blocks or prompts.  
- **Offline Mode:** Cache permissions for offline work but require revalidation when connectivity returns.  
- **Incident Response:** Provide tooling to disable compromised plugins and revert associated layouts fleet-wide.  
- **Telemetry:** Emit security events (failed unlock, unauthorized block load) for monitoring.

---

## 95.5 Testing & Verification

- Static analysis for manifest scopes and workspace schema compliance.  
- Automated UI tests toggling roles to verify gating behavior.  
- Penetration tests targeting remote companion flows and CLI automation.  
- Replay scenarios ensure unauthorized mutations cannot propagate through §97 simulation pipelines.

---

## 95.6 Open Questions

1. How do we grant temporary layout editing rights (e.g., technician override) without full role escalation?  
2. Should remote clients receive encrypted workspace manifests tied to device identity?  
3. What recovery workflow resets layouts after suspected tampering while preserving operator customizations?
