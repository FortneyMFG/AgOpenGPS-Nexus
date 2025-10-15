# Yield & Analytics Plugin Requirements (Draft)

## Overview

Yield plugins ingest combine telemetry, imports, and external datasets to produce yield, moisture, and test weight layers plus aggregated analytics. They coordinate with crop type, genetics, and profit plugins to deliver actionable insights and exports.

## Runtime Contracts

- Subscribe to session events to bind telemetry to the active job/session and capture environment metadata for reporting.【F:docs/SRS/sections/03_JobLifecycle.md†L18-L74】
- Register layer definitions `yield.actual`, `yield.moisture`, and `yield.testWeight` in the Layer Registry. Ensure units, smoothing metadata, and provenance conform to ADR-049.【F:docs/ADR/ADR-049_YieldPlugin.md†L21-L59】【F:docs/ADR/ADR-010-layer-registry-variable-rate.md†L33-L58】
- Support data ingest from machine telemetry, ISOXML TaskData, and CSV/GeoTIFF imports. Normalize to registry expectations (CRS, units) per the NX-113 flow before writing layers.【F:docs/SRS/sections/04_MappingLayers.md†L84-L106】【F:docs/ADR/ADR-014-interop-prescription-formats.md†L12-L56】
- Emit per-field and per-zone aggregates, linking results to crop type and genetics context to enable cross-filter analytics.【F:docs/ADR/ADR-045_CropTypePlugin.md†L29-L71】【F:docs/ADR/ADR-046_GeneticsPlugin.md†L21-L66】

## Analytics & Reporting

- Provide APIs for profit and report builder plugins to request yield × crop type/genetics breakdowns, field/season rollups, and temporal comparisons.【F:docs/ADR/ADR-050_CostProfitPlugin.md†L21-L52】【F:docs/ADR/ADR-051_ReportBuilder.md†L21-L52】
- Maintain QA fixtures verifying moisture/test weight calibration, smoothing algorithms, and aggregation accuracy across representative datasets.
- Surface anomaly detection (e.g., sensor dropouts, excessive smoothing) with actionable warnings and provenance references.

## UX Requirements

- Present yield maps with planned/actual toggles, tooltips showing crop/genetics context, and legend presets for accessibility.
- Offer time-series charts and distribution plots to highlight within-field variability and performance relative to planned prescriptions.

## Compatibility Notes

- Offline operation is mandatory; imports and telemetry logging must persist locally and sync opportunistically when cloud storage is available.【F:docs/ADR/ADR-030-field-job-sessions.md†L33-L86】
- Session IDs replace legacy Run references; exported analytics must reference `sessionId` for replay parity.【F:docs/ADR/ADR-041_JobSessions.md†L55-L73】
