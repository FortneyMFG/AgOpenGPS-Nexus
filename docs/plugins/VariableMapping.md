# Variable Mapping & Prescription Plugin Requirements (Draft)

## Overview

Variable mapping plugins orchestrate the prescription engine defined in ADR-013 and surface planned vs. actual variable-rate layers for controllers, analytics, and exports. They must honor registry governance, provenance hashing, and QA fixtures so agronomic decisions remain auditable.

## Pipeline Responsibilities

- **Recipe governance:** Accept YAML/JSON recipes that conform to ADR-013 schemas, verify manifest signatures, and compute SHA-256 hashes recorded alongside generated layers. Hash fingerprints must match Layer Registry entries for planned layers (`vr.planned.*`).【F:docs/ADR/ADR-013-derived-products-analytics-prescriptions.md†L12-L76】【F:docs/ADR/ADR-010-layer-registry-variable-rate.md†L33-L58】
- **ROI masks:** Treat zone masks sourced from the LayerEditService (ADR-044) as Region of Interest filters. Prescriptions must clip outputs to the selected zones and log provenance referencing the mask layer ID.【F:docs/ADR/ADR-044_ZoneDrawingFramework.md†L29-L74】
- **Planned vs. actual:** Publish planned layers under `vr.planned.*` IDs and reconcile them against as-applied `vr.actual.*` layers emitted by rate controllers. Provenance must include recipe hash, source layers, and session metadata.【F:docs/SRS/sections/04_MappingLayers.md†L65-L106】
- **QA harness:** Execute regression fixtures that compare generated outputs against golden datasets (≤4 minutes for a 160-acre field). Failures block promotion until variance and RMSE fall within ADR-013 tolerances.【F:docs/ADR/ADR-013-derived-products-analytics-prescriptions.md†L60-L71】
- **Interop:** Coordinate with the ISOXML bridge so TaskData exports embed recipe hashes and layer IDs, ensuring round-trip fidelity for NX-165.【F:docs/ADR/ADR-014-interop-prescription-formats.md†L12-L56】

## Session Integration

- Subscribe to session lifecycle hooks to preload crop type, genetics, and profitability overlays before generating prescriptions. Session metadata supplies environment and operator context for audit logs.【F:docs/SRS/sections/03_JobLifecycle.md†L18-L74】
- When prescriptions update mid-session, append the new layer ID to `session.layerRefs[]` and emit `onSessionMetadataChange` events so mapping and rate plugins refresh caches.

## Diagnostics & Reporting

- Log QA metrics (coverage, variance, RMSE) with provenance so Report Builder templates can surface recipe effectiveness in Crop and Profit reports.【F:docs/ADR/ADR-051_ReportBuilder.md†L15-L52】
- Provide diff tooling between planned and actual layers to highlight under/over-application zones for agronomic review.

## Compatibility Notes

- Plugins must operate offline using local layer catalogs and recipes; cloud sync is optional and reconciles when connectivity returns, per ADR-030 storage guidance.【F:docs/ADR/ADR-030-field-job-sessions.md†L33-L86】
- Legacy “Run” terminology should map to Sessions; exported metadata must use session IDs for compatibility with analytics and replay tooling.【F:docs/ADR/ADR-041_JobSessions.md†L55-L73】
