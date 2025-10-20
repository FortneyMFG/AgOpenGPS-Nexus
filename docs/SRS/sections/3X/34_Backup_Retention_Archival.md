# 34 — Backup, Retention & Archival (Status: collecting proposals)

## Problem statement
Define how Nexus stores, rotates, and archives jobs, telemetry, firmware, and plugin artifacts so farms can meet compliance requirements and recover from hardware loss without data loss.【F:docs/SRS/sections/3X/32_Persistence_Formats.md†L12-L156】【F:docs/SRS/sections/3X/33_Offline_First_Sync.md†L8-L120】

## Requirements (from contributors)
- R-BACKUP-000 (MUST, retention policy): Publish default retention windows for pose journals, layer tiles, telemetry, and firmware catalogs with overrides per profile; expired artifacts must be purged deterministically with audit logs.【F:docs/SRS/sections/6X/64_Telemetry_Health.md†L96-L146】【F:docs/SRS/options/6X/O-TELE-4_LayerDiagnostics.md†L1-L38】
- R-BACKUP-001 (MUST, backup targets): Support scheduled exports to removable media and network shares (SMB/NFS/S3) with resumable uploads and checksum verification.【F:docs/SRS/sections/3X/33_Offline_First_Sync.md†L16-L124】【F:docs/SRS/options/2X/O-BACKEND-6_LinuxCoreService.md†L24-L44】
- R-BACKUP-002 (SHOULD, snapshotting): Provide CLI and UI flows that snapshot job/session state (config, layers, telemetry) into portable bundles for disaster recovery or fleet cloning.【F:docs/SRS/sections/3X/32_Persistence_Formats.md†L66-L156】【F:docs/SRS/sections/9X/96_Quality_Engineering_Release.md†L149-L211】
- R-BACKUP-003 (MUST, differential sync): Optimize large dataset transfers using differential manifests or deduplicated tiles to minimize bandwidth for overnight uploads.【F:docs/SRS/sections/3X/33_Offline_First_Sync.md†L66-L124】
- R-BACKUP-004 (SHOULD, regulatory export): Support generating retention reports (e.g., spray logs, yield histories) in open formats (GeoJSON, ISOXML, CSV) aligned to retention policies for compliance audits.【F:docs/SRS/sections/7X/74_Monitoring_Systems.md†L160-L327】【F:docs/SRS/options/7X/O-DATA-5_MetadataDrivenLayers.md†L1-L64】
- R-BACKUP-005 (MUST, integrity monitoring): Surface backup success/failure, last-run timestamps, and storage utilization in telemetry dashboards and CLI health checks.【F:docs/SRS/sections/6X/64_Telemetry_Health.md†L96-L146】【F:docs/SRS/options/6X/O-TELE-4_LayerDiagnostics.md†L1-L38】

## Current sentiment
Operators need first-class backup tooling before trusting remote deployments; contributors are prioritizing deterministic retention policies and resumable exports so offline rigs can sync once connectivity resumes.【F:docs/SRS/sections/3X/33_Offline_First_Sync.md†L16-L124】【F:docs/SRS/sections/6X/64_Telemetry_Health.md†L96-L146】
