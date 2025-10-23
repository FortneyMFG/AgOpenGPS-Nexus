# 33 — Offline-first & Sync
*(Status: Proposed)*

**Author:** Codex
**Created:** 2025-10-20
**Section ID:** 33
**Version:** 0.1.0
**Editors:** @nexus-docs-team
**Last Updated:** 2025-10-20
**Related Sections:** [31 — Domain Data Model](31_Domain_Data_Model.md), [32 — Persistence & Formats](32_Persistence_Formats.md), [34 — Backup, Retention & Archival](34_Backup_Retention_Archival.md)
**Upstream Dependencies:** [ADR-019](../../ADR/ADR-019-provenance-audit-qa.md), [ADR-024](../../ADR/ADR-024-discovery-identity.md), [ADR-028](../../ADR/ADR-028-stack-boundaries.md)
**Downstream Impacts:** Release packaging workflows, Core/AgIO update channels, backup exporters, retention planners

---

## 33.1 Purpose & Scope

This section defines how Nexus distributes software, applies updates, and synchronises data across rigs that frequently operate without reliable connectivity. It addresses packaging formats, rollback strategies, staged deployments, and validation guardrails so that operators can update safely from USB drives, local networks, or intermittent internet access.

---

## 33.2 Context

- Legacy AgOpenGPS deployments rely on manual zip downloads and `dotnet publish` outputs for offline installation.【F:docs/development/SRS/sections/3X_Data_Storage/33_Offline_First_Sync.md†L6-L33】
- Upcoming Linux Core packaging (Debian, Docker, AppImage) requires offline-safe distribution and rollback instructions.【F:docs/development/SRS/sections/2X_System_Architecture/21_System_Decomposition_Boundaries.md†L6-L23】
- Release governance (ADR-019, ADR-024, ADR-028) imposes provenance, identity, and stack boundary checks that must hold during offline updates.【F:docs/development/SRS/sections/3X_Data_Storage/33_Offline_First_Sync.md†L36-L120】

Constraints:
- Operators may defer updates for months; tooling must validate compatibility before applying packages.
- Update packages must function without continuous connectivity or remote registries.

---

## 33.3 Legacy Comparison

| Area / Theme | Legacy Behavior | Identified Limitation | Modernization Opportunity | Reference / Source |
|---------------|-----------------|------------------------|---------------------------|--------------------|
| Distribution | Manual zip download/unpack per release. | Error-prone, lacks validation. | Provide scripted installers with checksum verification and rollback steps. | [README.md](../../README.md) |
| Rollback | Manual directory backups. | No structured rollback guidance. | Ship documented rollback playbooks per package type. | [Section 34](34_Backup_Retention_Archival.md) |
| Staged Deployments | Whole-stack updates only. | Risk of controller/AgIO mismatch. | Support staged updates with compatibility checks per component. | [ADR-024](../../ADR/ADR-024-discovery-identity.md) |

---

## 33.4 Definitions

| Term | Definition |
|------|-------------|
| Offline-first Update | Package and workflow that installs without internet access, typically via USB or LAN share.【F:docs/development/SRS/sections/3X_Data_Storage/33_Offline_First_Sync.md†L6-L33】 |
| Staged Deployment | Sequential update of Core, UI, controllers with compatibility validation between stages.【F:docs/development/SRS/sections/3X_Data_Storage/33_Offline_First_Sync.md†L34-L94】 |
| Delta Package | Differential update artifact that transfers only changed bits to conserve bandwidth.【F:docs/development/SRS/sections/3X_Data_Storage/33_Offline_First_Sync.md†L64-L94】 |
| Rollback Checklist | Documented steps operators follow to revert to prior build with integrity verification.【F:docs/development/SRS/sections/3X_Data_Storage/33_Offline_First_Sync.md†L34-L120】 |

---

> **Requirement Grammar (RFC-2119):**
> - **MUST / MUST NOT** = mandatory; test must exist.
> - **SHOULD / SHOULD NOT** = strong recommendation; justify exceptions.
> - **MAY** = optional; document enabling conditions.
>
> **Clarity Checklist:** Avoid weak words: *fast, robust, user-friendly, handle, support, adequate,* etc.
> Prefer measurable forms: *“≤ 250 ms p95,” “error rate < 0.1%,” “99.5% success over 10k trials.”*
> Each requirement: single behavior, single actor, single condition, single metric.

## 33.5 Requirements

### 33.5.1 Summary Requirements

| ID | Priority | Category | Summary | Source / C-IDs | Key Metrics / Verification |
|----|-----------|-----------|---------|-----------------|-----------------------------|
| R-33000 | MUST | Distribution | Provide offline-capable packages (zip, Debian, AppImage, Docker) with documented install flows. | R-UPD-000, R-UPD-004 | Release pipelines publish signed artifacts with install guides; checksum verification required.【F:docs/development/SRS/sections/3X_Data_Storage/33_Offline_First_Sync.md†L6-L64】 |
| R-33001 | MUST | Rollback | Document rollback workflows per package type so rigs revert to known-good builds without re-imaging. | R-UPD-002, R-UPD-004 | Rollback checklist validated in QA; prior version retained locally. 【F:docs/development/SRS/sections/3X_Data_Storage/33_Offline_First_Sync.md†L34-L94】 |
| R-33002 | SHOULD | Staged Updates | Allow staged Core/AgIO/controller updates with compatibility gates and validation prompts. | R-UPD-003, R-UPD-006 | Compatibility matrix enforced via tooling; failure blocks staged rollout.【F:docs/development/SRS/sections/3X_Data_Storage/33_Offline_First_Sync.md†L34-L120】 |
| R-33003 | COULD | Delta Delivery | Offer delta packages or background downloaders respecting limited connectivity. | R-UPD-005 | Optional service passes bandwidth utilization checks; falls back to full packages if validation fails.【F:docs/development/SRS/sections/3X_Data_Storage/33_Offline_First_Sync.md†L64-L94】 |
| R-33004 | MUST | Provenance & Integrity | Capture validation, checksum, and compatibility results before marking update successful. | R-UPD-006 | Update workflow logs stored for audit; failure triggers rollback instructions.【F:docs/development/SRS/sections/3X_Data_Storage/33_Offline_First_Sync.md†L34-L120】 |

### 33.5.2 Detailed Requirement Catalogue

| ID | Priority | Category | Summary | Source / Reference |
|----|-----------|-----------|---------|--------------------|
| R-UPD-000 | MUST | Distribution | Preserve zip-based distribution for offline installs.【F:docs/development/SRS/sections/3X_Data_Storage/33_Offline_First_Sync.md†L6-L24】 |
| R-UPD-001 | MUST | Build Output | Keep `dotnet publish` outputs for manual deployment workflows.【F:docs/development/SRS/sections/3X_Data_Storage/33_Offline_First_Sync.md†L6-L24】 |
| R-UPD-002 | SHOULD | Rollback | Provide rollback guidance enabling rigs to revert builds without re-imaging.【F:docs/development/SRS/sections/3X_Data_Storage/33_Offline_First_Sync.md†L34-L64】 |
| R-UPD-003 | SHOULD | Staged Deployment | Allow staged updates (AgOpenGPS vs. AgIO vs. controllers) with compatibility protection.【F:docs/development/SRS/sections/3X_Data_Storage/33_Offline_First_Sync.md†L34-L94】 |
| R-UPD-004 | SHOULD | Packaging | Provide Debian packages, Docker images, AppImage builds with rollback instructions while preserving zip releases.【F:docs/development/SRS/sections/2X_System_Architecture/21_System_Decomposition_Boundaries.md†L6-L23】 |
| R-UPD-005 | COULD | Delta Delivery | Add delta packages or background downloaders mindful of limited connectivity.【F:docs/development/SRS/sections/3X_Data_Storage/33_Offline_First_Sync.md†L64-L94】 |
| R-UPD-006 | SHOULD | Validation | Capture validation/rollback checklists (hash verification, compatibility checks, firmware coordination).【F:docs/development/SRS/sections/3X_Data_Storage/33_Offline_First_Sync.md†L34-L120】 |

### 33.5.3 Requirement Sources & Rationale

| Req ID | Source (issue/discussion/standard) | Rationale (one line) |
|--------|-------------------------------------|----------------------|
| R-33000 | Field deployment retrospective | Offline rigs require package access without internet. |
| R-33001 | Support escalation reviews | Operators need structured rollback paths to recover quickly. |
| R-33002 | Controller compatibility workshop | Avoid mismatched firmware/host combinations. |
| R-33003 | Connectivity working group | Delta delivery reduces overnight transfer time. |
| R-33004 | Release governance checklist | Provenance logs needed for compliance and troubleshooting. |

---

## 33.6 Acceptance Criteria & Verification

> **Examples:**
> - Automated unit or integration test coverage thresholds.
> - Simulated scenario replay verification.
> - Manual review or field test sign-off checklist.

### 33.6.1 Requirement-to-Verification Map

| Req ID | Verification Type | Artifact / Location | Pass/Fail Threshold |
|--------|--------------------|---------------------|---------------------|
| R-33000 | Release rehearsal | `docs/release/offline-install-checklist.md` | All offline install steps validated on Windows & Linux. |
| R-33001 | Rollback drill | `docs/release/rollback-playbook.md` | Rollback completes within 15 minutes with integrity checks. |
| R-33002 | Compatibility matrix | `tools/update-manager/tests/CompatibilityMatrixTests.cs` | Staged update aborted when matrix violation detected. |
| R-33003 | Bandwidth test | `tests/Updates/DeltaPackageSimulation.md` | Delta delivery reduces transfer size ≥ 40% vs. full package. |
| R-33004 | Audit log review | `logs/update/` retention | Validation + checksum entries recorded for each update. |

---

## 33.7 Constraints

- Update tooling MUST run without admin/root requirements beyond those documented in package guides.【F:docs/development/SRS/sections/3X_Data_Storage/33_Offline_First_Sync.md†L6-L64】
- Packages MUST be installable offline with only artefacts included in the release bundle or on provided media.【F:docs/development/SRS/sections/3X_Data_Storage/33_Offline_First_Sync.md†L6-L64】
- Rollback instructions MUST preserve configuration and operator data, relying on retention policies from Section 34.【F:docs/development/SRS/sections/3X_Data_Storage/34_Backup_Retention_Archival.md†L6-L20】

### 33.7.1 Non-Functional Requirement Classes

- **Performance:** Installer runtime ≤ 10 minutes on reference hardware with SSD; delta downloads complete during typical overnight windows.
- **Reliability & Availability:** Update process tolerates power loss via resumable steps and integrity checks.
- **Security:** Packages signed; validation includes checksum + signature verification before execution (see ADR-019).
- **Safety:** Update workflows ensure controller firmware remains compatible to avoid unsafe field behaviour.
- **Usability/UX:** Step-by-step guides include screenshots/log outputs; prompts warn before compatibility breaks.
- **Operability:** Logging captures every update stage for support; operators can export logs for remote assistance.
- **Portability:** Packaging supports Windows and Linux target rigs with consistent instructions.
- **Maintainability:** Update scripts version-controlled with automated tests for compatibility matrices.

---

## 33.8 Risks & Open Issues

| ID | Description | Impact | Mitigation / Status | Owner |
|----|-------------|--------|---------------------|-------|
| RISK-33-1 | Operators may skip validation steps when offline, risking corrupted installs. | Medium | Provide automated validation tool that blocks proceed until checks pass. | @nexus-ops |
| RISK-33-2 | Staged updates could leave rigs partially upgraded if compatibility data is stale. | Medium | Ship compatibility manifest with each release and expire caches after 24 hours. | @nexus-platform |
| ISSUE-33-1 | Delta package tooling not yet standardised across Windows/Linux builds. | High | Evaluate rsync-style differentials vs. Binary delta formats; ADR follow-up planned. | @nexus-build |

---

## 33.9 Design Considerations

| ID | Consideration | Description |
|----|----------------|-------------|
| C1 | Offline-first Workflow | All update steps must succeed without internet; optional accelerators cannot be mandatory. |
| C2 | Compatibility Governance | Update tooling references compatibility matrices and firmware manifests before proceeding. |
| C3 | Operator Guidance | Clear instructions, logs, and prompts reduce dependency on remote support. |
| C4 | Rollback Safety | Always retain prior version artifacts and configs for quick recovery. |
| C5 | Bandwidth Awareness | Delta and background downloads must respect rural connectivity constraints. |
| C6 | Auditability | Validation logs and checksums support troubleshooting and compliance audits. |

### 33.9.1 Assumptions & Preconditions

- [A1] Operators can access at least one trusted device with internet to download updates before transferring offline.【F:docs/development/SRS/sections/3X_Data_Storage/33_Offline_First_Sync.md†L6-L64】
- [A2] Compatibility manifests are published alongside every release and versioned within the package bundle.【F:docs/development/SRS/sections/3X_Data_Storage/33_Offline_First_Sync.md†L34-L120】
- [A3] Backup tooling from Section 34 is in place before major updates, ensuring data can be restored if rollback fails.【F:docs/development/SRS/sections/3X_Data_Storage/34_Backup_Retention_Archival.md†L6-L20】

### 33.9.2 Related ADRs

- [ADR-019 — Provenance, Audit, & QA](../../ADR/ADR-019-provenance-audit-qa.md)
- [ADR-024 — Discovery & Identity](../../ADR/ADR-024-discovery-identity.md)
- [ADR-028 — Stack Boundaries](../../ADR/ADR-028-stack-boundaries.md)

---
