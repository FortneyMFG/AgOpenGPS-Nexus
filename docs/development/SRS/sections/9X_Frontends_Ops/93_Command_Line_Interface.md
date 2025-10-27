# 93 — Command Line & Automation Surfaces
*(Status: Draft — grid-aware alignment)*

**Authors:** Operations & Automation Guild  
**Version:** 1.0.0  
**Section ID:** 93  
**Related Sections:** 91 — UI Shell, 94 — Extensibility, 95 — Security  
**Upstream Dependencies:** 4X — Interprocess Communications, 6X — Core Domain Services  
**Downstream Impacts:** 96 — Quality Engineering & Release

---

## 93.1 Purpose

Define the Nexus command-line interface (`nx`) and automation endpoints that complement the grid-based shell. The CLI must discover the same workspace metadata, plugin slots, and configuration surfaces used by the UI so scripted workflows can configure layouts, blocks, and presets without manual edits.

---

## 93.2 Scope & Context

- Replace disparate legacy scripts with a unified CLI that manages plugins, layouts, telemetry captures, and remote sessions.  
- Provide headless access for CI, remote diagnostics, and kiosk provisioning.  
- Ensure CLI interactions respect capability gating and produce audit trails aligned with the grid workspace model.

---

## 93.3 Functional Requirements

| ID | Priority | Requirement | Notes |
|----|----------|-------------|-------|
| R-CLI-01 | MUST | Workspace Introspection | List available workspaces, grids, and slot assignments so operators can script layout deployments. |
| R-CLI-02 | MUST | Layout Apply | Apply, diff, and rollback `WorkspaceLayout` manifests, with validation against plugin slot constraints. |
| R-CLI-03 | MUST | Plugin Lifecycle | Install, update, and remove plugin packages while verifying declared block/panel contributions. |
| R-CLI-04 | MUST | Capability Awareness | Enforce the same permission checks as the UI, failing fast when the caller lacks required scopes. |
| R-CLI-05 | SHOULD | Simulation Hooks | Launch deterministic simulation or replay sessions and attach them to specific workspace presets (§97). |
| R-CLI-06 | SHOULD | Remote Session Control | Bootstrap remote clients, publish connection tokens, and monitor status via telemetry mesh events. |
| R-CLI-07 | SHOULD | Prompt Automation | Trigger configuration prompts (e.g., Edit Prompt flows) using JSON patches so CI can preseed settings.【F:docs/UI/UI_Demo.html†L181-L221】 |
| R-CLI-08 | MUST | Audit Logging | Emit structured logs for every mutating operation (layout apply, plugin install) with before/after snapshots. |

---

## 93.4 Non-Functional Requirements

- **Portability:** Ship as a .NET tool with native packaging for Windows and Linux.  
- **Offline Support:** All layout and plugin commands operate offline, deferring remote sync until connectivity resumes.  
- **Extensibility:** Plugins may ship CLI verbs by declaring manifest contributions mapped to capability scopes.  
- **Usability:** Provide descriptive errors, dry-run mode, and structured output (JSON) to integrate with automation pipelines.

---

## 93.5 Integration

- Consumes workspace schemas from §91 and validates them using §94 manifest governance.  
- Leverages §95 security tokens to authenticate commands, including hardware keys or OIDC flows.  
- Publishes telemetry to §97 simulation and replay harnesses for deterministic validation of scripted changes.

---

## 93.6 Open Questions

1. Should remote layout apply stream incremental diffs or require full manifest replacements?  
2. How do plugin-supplied verbs advertise UI impacts (e.g., new blocks) for review before execution?  
3. What minimum telemetry is required to monitor headless layout deployment success?
