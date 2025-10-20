# Genetics Plugin Requirements (Draft)

## Overview

The Genetics plugin tracks planned and actual seed varieties, barcode scans, and export workflows for agronomic reporting. It integrates with the Zone Drawing Framework for per-zone edits and publishes session-aware layers consumed by analytics and report builders.

## Runtime Contracts

- Subscribe to lifecycle events (`onFarmLoaded`, `onJobLoaded`, `onSessionStart`, `onSessionMetadataChange`, `onSessionEnd`) to preload crop context and capture operator updates. Session events supply the provenance required by ADR-041.【F:docs/SRS/sections/6X/62_Job_Lifecycle.md†L18-L74】
- Register editable layers `genetics.plan` and `genetics.variety` with the Layer Registry. Planned layers reference target hybrids; actual layers record applied varieties, lot numbers, and barcode metadata.【F:docs/ADR/ADR-046_GeneticsPlugin.md†L21-L66】【F:docs/ADR/ADR-010-layer-registry-variable-rate.md†L33-L58】
- Use the LayerEditService to manage per-zone variety edits. Attribute panels expose brand, product, trait stack, treatment, and source information. Every edit emits a `LayerEditEvent.v1` journal for collaborative replay.【F:docs/ADR/ADR-044_ZoneDrawingFramework.md†L29-L74】【F:schemas/LayerEditEvent.v1.json†L1-L140】
- When coverage is active, session-aware writes ensure that as-applied layers include `jobId`, `sessionId`, and provenance linking to barcode scans and operator actions.

## Data Model & Storage

- Store planned and actual layers using the JSON schemas `schemas/GeneticsPlan.v1.json` and `schemas/GeneticsVariety.v1.json`. Fields include seed identifiers, lot numbers, treatment codes, barcode payloads, and authoring metadata.【F:schemas/GeneticsPlan.v1.json†L1-L120】【F:schemas/GeneticsVariety.v1.json†L1-L120】 Layer registry metadata flags `genetics.plan` as planned and `genetics.variety` as session-bound actual layers so controllers negotiate capabilities deterministically.【F:docs/reference/layer-registry-genetics.md†L1-L33】
- Reference crop type context from `cropType.planned`/`cropType.actual` layers to support rotation analytics and agronomic reports.【F:docs/ADR/ADR-045_CropTypePlugin.md†L29-L71】

## UX Requirements

- Provide a variety picker with search and quick-assign workflows for entire fields or selected zones.
- Support barcode scanning (camera, handheld) that populates lot/treatment data automatically and logs provenance.
- Surface applied variety summaries per field and per session, highlighting deviations from the plan for operator review and audit exports.

### Picker UI Implementation (NX-303)

- `GeneticsPickerViewModel` coordinates favorites, recents, search results, and barcode scan state so the desktop shell can render
  ADR-046 workflows even when offline. The design-time sample seeds Pioneer/DEKALB favorites, Asgrow/Corteva recents, and a
  Specialty catalog entry used in regression screenshots. Search tokens span brand, product, trait stack, lot, treatment, source,
  notes, and barcode payloads while `ApplyBarcodeScan` resolves wedge/serial scanner input into selections with clear status
  messaging.【F:Nexus SourceCode/src/Aog.UI.Avalonia/ViewModels/GeneticsPickerViewModel.cs†L1-L308】
- `GeneticsVarietyOptionViewModel` exposes display metadata, accent theming, search tokens, and manual selection commands with
  minimal dependencies so plugin hosts can hydrate the picker from analytics snapshots or on-disk caches without reaching back to
  the engine.【F:Nexus SourceCode/src/Aog.UI.Avalonia/ViewModels/GeneticsVarietyOptionViewModel.cs†L1-L168】

## Exports & Reporting

- Generate CSV and GeoJSON exports listing planned vs. actual varieties, including session timestamps and barcode metadata.
- Feed Report Builder templates with per-field and per-season variety summaries, enabling Crop Reports to analyze rotation and trait adoption.【F:docs/ADR/ADR-051_ReportBuilder.md†L21-L52】
- Coordinate with ISOXML/TaskData exporters to embed variety information where supported by Task Controller specifications.【F:docs/ADR/ADR-014-interop-prescription-formats.md†L12-L56】

## Compatibility Notes

- Offline-first: all edits and barcode scans must persist locally and sync when connectivity returns; cloud is optional per ADR-030.【F:docs/ADR/ADR-030-field-job-sessions.md†L33-L86】
- Legacy terminology should map to Sessions; historical “Run” labels are no longer used in UI or exports.【F:docs/ADR/ADR-041_JobSessions.md†L55-L73】

## QA & Regression Fixtures

- Genetics metrics are captured in `tools/qa/metrics/genetics-regression.json` so the QA dashboard
  and post-run report suites can flag coverage gaps, barcode latency, and feature counts during
  automated regressions aligned with ADR-046.【F:tools/qa/metrics/genetics-regression.json†L1-L11】【F:docs/qa/qa-dashboard.md†L1-L34】
