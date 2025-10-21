# 34 — Backup, Retention & Archival
*(Status: Proposed)*

**Author:** Codex
**Created:** 2025-10-20
**Section ID:** 34
**Version:** 0.1.0
**Editors:** @nexus-docs-team
**Last Updated:** 2025-10-20
**Related Sections:** [31 — Domain Data Model](31_Domain_Data_Model.md), [32 — Persistence & Formats](32_Persistence_Formats.md), [33 — Offline-first & Sync](33_Offline_First_Sync.md)
**Upstream Dependencies:** [ADR-025](34-ADR-025 - Data lifecycle and retention policy.md), [ADR-026](../2X_System_Architecture/21-ADR-900 - PoseStream, Layer, and Control Program Roadmap.md)
**Downstream Impacts:** Backup tooling, retention planners, regulatory export workflows, telemetry health dashboards

---

## 34.1 Purpose & Scope

This section defines how Nexus safeguards agronomic data through backups, retention policies, and archival workflows. It covers backup targets, snapshot packaging, differential synchronisation, regulatory exports, and integrity monitoring so that farms can recover from hardware loss, satisfy compliance, and manage storage footprints predictably.

---

## 34.2 Context

- Persistence requirements from Section 32 establish schema, provenance, and storage layouts that backups must preserve.【F:docs/SRS/sections/3X_Data_Storage/32_Persistence_Formats.md†L12-L156】
- Offline-first update strategies in Section 33 depend on deterministic backups before major upgrades.【F:docs/SRS/sections/3X_Data_Storage/33_Offline_First_Sync.md†L34-L94】
- Telemetry health monitoring (Section 64) surfaces backup success/failure metrics that feed operator dashboards.【F:docs/SRS/sections/6X_Core_Domain_Services/64_Telemetry_Health.md†L96-L146】

Assumptions:
- Operators can schedule backups during off-hours with access to removable media or intermittent network shares.
- Backup tooling leverages the same schema registry and provenance metadata enforced by Sections 31–32.

---

## 34.3 Legacy Comparison

| Area / Theme | Legacy Behavior | Identified Limitation | Modernization Opportunity | Reference / Source |
|---------------|-----------------|------------------------|---------------------------|--------------------|
| Backup Targets | Manual copy of job folders to USB. | No verification, easy to miss assets. | Provide resumable exports with checksums and manifest verification. | [Section 33](33_Offline_First_Sync.md) |
| Retention Policy | Ad-hoc manual deletion. | Risk of data loss or bloat. | Automate retention windows per asset class with audit logs. | [ADR-025](34-ADR-025 - Data lifecycle and retention policy.md) |
| Regulatory Exports | One-off scripts. | Inconsistent formats, missing provenance. | Generate signed, versioned regulatory bundles tied to retention plans. | [Section 74](../7X_Mapping_Geospatial/74_Monitoring_Systems.md) |

---

## 34.4 Definitions

| Term | Definition |
|------|-------------|
| Retention Planner | Service or workflow enforcing retention windows and archival triggers per asset class.【F:docs/SRS/sections/3X_Data_Storage/34-ADR-025 - Data lifecycle and retention policy.md†L10-L58】 |
| Differential Manifest | Inventory describing delta between previous and current backup to minimise transfer volume.【F:docs/SRS/sections/3X_Data_Storage/33_Offline_First_Sync.md†L64-L94】 |
| Compliance Bundle | Signed export containing regulatory data (spray logs, yield history) aligned with retention policy.【F:docs/SRS/sections/7X_Mapping_Geospatial/74_Monitoring_Systems.md†L160-L327】 |
| Backup Health Telemetry | Metrics and alerts exposing backup status, timestamps, storage utilisation to operators.【F:docs/SRS/sections/6X_Core_Domain_Services/64_Telemetry_Health.md†L96-L146】 |

---

> **Requirement Grammar (RFC-2119):**
> - **MUST / MUST NOT** = mandatory; test must exist.
> - **SHOULD / SHOULD NOT** = strong recommendation; justify exceptions.
> - **MAY** = optional; document enabling conditions.
>
> **Clarity Checklist:** Avoid weak words: *fast, robust, user-friendly, handle, support, adequate,* etc.
> Prefer measurable forms: *“≤ 250 ms p95,” “error rate < 0.1%,” “99.5% success over 10k trials.”*
> Each requirement: single behavior, single actor, single condition, single metric.

## 34.5 Requirements

### 34.5.1 Summary Requirements

| ID | Priority | Category | Summary | Source / C-IDs | Key Metrics / Verification |
|----|-----------|-----------|---------|-----------------|-----------------------------|
| R-34000 | MUST | Retention Policy | Publish default retention windows for pose journals, tiles, telemetry, firmware catalogs with deterministic purging and audit logs. | R-BACKUP-000 | Retention planner integration tests confirm purge + logging for each asset class.【F:docs/SRS/sections/3X_Data_Storage/34-ADR-025 - Data lifecycle and retention policy.md†L10-L58】 |
| R-34001 | MUST | Backup Targets | Support scheduled exports to removable media and network shares with resumable uploads and checksum verification. | R-BACKUP-001, R-BACKUP-003 | Backup CLI verification logs show checksum match; resumable transfer tests pass. 【F:docs/SRS/sections/3X_Data_Storage/33_Offline_First_Sync.md†L16-L124】 |
| R-34002 | SHOULD | Snapshot Packaging | Provide CLI/UI flows that snapshot job/session state (config, layers, telemetry) into portable bundles for disaster recovery. | R-BACKUP-002 | Snapshot integrity verified via import test; bundle contains manifest + provenance. 【F:docs/SRS/sections/3X_Data_Storage/32_Persistence_Formats.md†L66-L156】 |
| R-34003 | MUST | Differential Sync | Optimise large dataset transfers using differential manifests or deduplicated tiles to minimise bandwidth. | R-BACKUP-003 | Differential backup reduces transfer volume ≥ 50% vs. full copy on benchmark dataset. 【F:docs/SRS/sections/3X_Data_Storage/33_Offline_First_Sync.md†L66-L124】 |
| R-34004 | SHOULD | Regulatory Exports | Generate retention-aligned regulatory reports in open formats (GeoJSON, ISOXML, CSV) with provenance. | R-BACKUP-004 | Compliance bundle validation ensures required datasets present and signed. 【F:docs/SRS/sections/7X_Mapping_Geospatial/74_Monitoring_Systems.md†L160-L327】 |
| R-34005 | MUST | Integrity Monitoring | Surface backup status, last-run timestamps, and storage utilisation in telemetry dashboards and CLI health checks. | R-BACKUP-005 | Health telemetry feed shows success/failure metrics; CLI returns non-zero on failure. 【F:docs/SRS/sections/6X_Core_Domain_Services/64_Telemetry_Health.md†L96-L146】 |

### 34.5.2 Requirement Catalogue

| ID | Priority | Category | Summary | Source / Reference |
|----|-----------|-----------|---------|--------------------|
| R-BACKUP-000 | MUST | Retention | Publish retention windows for pose journals, tiles, telemetry, firmware with deterministic purge + audit logs.【F:docs/SRS/sections/3X_Data_Storage/34-ADR-025 - Data lifecycle and retention policy.md†L10-L58】【F:docs/SRS/sections/6X_Core_Domain_Services/64_Telemetry_Health.md†L96-L146】 |
| R-BACKUP-001 | MUST | Backup Targets | Support scheduled exports to removable media and network shares (SMB/NFS/S3) with resumable uploads and checksum verification.【F:docs/SRS/sections/3X_Data_Storage/33_Offline_First_Sync.md†L16-L124】【F:docs/SRS/sections/2X_System_Architecture/21-O6 - Linux Core service with remote frontends.md†L24-L44】 |
| R-BACKUP-002 | SHOULD | Snapshotting | Provide CLI/UI flows to snapshot job/session state into portable bundles for recovery or cloning.【F:docs/SRS/sections/3X_Data_Storage/32_Persistence_Formats.md†L66-L156】【F:docs/SRS/sections/9X_Frontends_Ops/96_Quality_Engineering_Release.md†L149-L211】 |
| R-BACKUP-003 | MUST | Differential Sync | Optimise large dataset transfers via differential manifests or deduplicated tiles.【F:docs/SRS/sections/3X_Data_Storage/33_Offline_First_Sync.md†L66-L124】 |
| R-BACKUP-004 | SHOULD | Regulatory Export | Generate retention reports (spray logs, yield histories) in open formats with provenance.【F:docs/SRS/sections/7X_Mapping_Geospatial/74_Monitoring_Systems.md†L160-L327】【F:docs/SRS/sections/3X_Data_Storage/32_Persistence_Formats.md†L224-L229】 |
| R-BACKUP-005 | MUST | Integrity Monitoring | Surface backup success/failure, last-run timestamps, storage utilisation via telemetry dashboards and CLI health checks.【F:docs/SRS/sections/6X_Core_Domain_Services/64_Telemetry_Health.md†L96-L146】【F:docs/SRS/sections/6X_Core_Domain_Services/64-O5 - Layer diagnostics and health monitoring.md†L1-L38】 |

### 34.5.3 Requirement Sources & Rationale

| Req ID | Source (issue/discussion/standard) | Rationale (one line) |
|--------|-------------------------------------|----------------------|
| R-34000 | ADR-025 retention council | Ensure predictable storage footprint and audit trail. |
| R-34001 | Offline sync working group | Backups must succeed over intermittent connectivity. |
| R-34002 | Support recovery drills | Snapshot bundles accelerate disaster recovery and fleet cloning. |
| R-34003 | Connectivity working group | Differential sync reduces overnight transfer load. |
| R-34004 | Compliance task force | Regulators demand signed, standardised exports. |
| R-34005 | Telemetry governance review | Operators need real-time visibility into backup health. |

---

## 34.6 Acceptance Criteria & Verification

> **Examples:**
> - Automated unit or integration test coverage thresholds.
> - Simulated scenario replay verification.
> - Manual review or field test sign-off checklist.

### 34.6.1 Requirement-to-Verification Map

| Req ID | Verification Type | Artifact / Location | Pass/Fail Threshold |
|--------|--------------------|---------------------|---------------------|
| R-34000 | Retention simulator | `tests/Retention/PlannerScenarioTests.cs` | Purge executes per schedule; audit log entries persisted. |
| R-34001 | Backup rehearsal | `docs/runbooks/backup-usb-checklist.md` | Checksums verified; resume succeeds after simulated disconnect. |
| R-34002 | Snapshot restore test | `tests/Backups/SnapshotRestore.md` | Restored job/session passes provenance validation. |
| R-34003 | Differential benchmark | `benchmarks/BackupDifferentialBench.cs` | Transfer volume reduced ≥ 50% vs. baseline. |
| R-34004 | Compliance export QA | `tests/Exports/RegulatoryBundleTests.cs` | All required datasets present; signatures validated. |
| R-34005 | Telemetry health check | `tests/Telemetry/BackupHealthTests.cs` | Dashboard + CLI show correct status after simulated success/failure. |

---

## 34.7 Constraints

- Backup bundles MUST preserve schema hashes, provenance metadata, and attachments exactly as stored in Section 32 artifacts.【F:docs/SRS/sections/3X_Data_Storage/32_Persistence_Formats.md†L26-L156】
- Retention automation MUST avoid deleting active-season data unless explicitly confirmed by operators.【F:docs/SRS/sections/3X_Data_Storage/34-ADR-025 - Data lifecycle and retention policy.md†L10-L58】
- Compliance exports MUST be reproducible from retained data and include signatures/audit logs for verification.【F:docs/SRS/sections/7X_Mapping_Geospatial/74_Monitoring_Systems.md†L160-L327】

### 34.7.1 Non-Functional Requirement Classes

- **Performance:** Backup jobs complete overnight (≤ 8 hours for 200 GB dataset) with resumable checkpoints.
- **Reliability & Availability:** Backup tooling recovers from power/network loss without data corruption.
- **Security:** Encryption at rest/in transit governed by ADR-019; credentials stored securely when targeting network shares.
- **Safety:** Backup verification prevents stale or corrupted datasets from feeding guidance workflows.
- **Usability/UX:** Operators receive clear prompts and progress indicators during backup/restore tasks.
- **Operability:** Monitoring surfaces job duration, throughput, error counts, and storage utilisation.
- **Portability:** Backup bundles portable across Windows and Linux rigs; manifests human-readable.
- **Maintainability:** Retention policies versioned with changelog; automation scripts covered by CI tests.

---

## 34.8 Risks & Open Issues

| ID | Description | Impact | Mitigation / Status | Owner |
|----|-------------|--------|---------------------|-------|
| RISK-34-1 | Large datasets may exceed removable media capacity during peak season. | Medium | Support incremental rotation and multi-volume media sets; warn operators early. | @nexus-ops |
| RISK-34-2 | Differential manifests could drift if schema hashes change mid-season. | Medium | Tie manifests to schema hash versions; trigger full backup when mismatch detected. | @nexus-platform |
| ISSUE-34-1 | Need signed compliance bundle format aligning with ADR-019 crypto decisions. | Medium | Draft signature policy pending security review. | @nexus-security |

---

## 34.9 Design Considerations

| ID | Consideration | Description |
|----|----------------|-------------|
| C1 | Deterministic Backups | Backups must capture artefacts exactly as stored, including provenance hashes and schema refs. |
| C2 | Operator Trust | Provide transparent progress, verification, and alerts to build operator confidence. |
| C3 | Bandwidth Efficiency | Differential and resumable transfers minimise downtime and bandwidth usage. |
| C4 | Regulatory Alignment | Exports align with compliance standards and include signatures/audit data. |
| C5 | Lifecycle Integration | Retention ties into Section 32 storage policies and Section 33 update workflows. |

### 34.9.1 Assumptions & Preconditions

- [A1] Operators schedule backups during off-hours with hardware connected to power and stable media/network.【F:docs/SRS/sections/3X_Data_Storage/33_Offline_First_Sync.md†L16-L124】
- [A2] Telemetry infrastructure is deployed to capture backup health metrics and expose dashboards.【F:docs/SRS/sections/6X_Core_Domain_Services/64_Telemetry_Health.md†L96-L146】
- [A3] Schema registry and layer catalogue remain accessible for validating backup contents.【F:docs/SRS/sections/3X_Data_Storage/32_Persistence_Formats.md†L21-L156】

---
