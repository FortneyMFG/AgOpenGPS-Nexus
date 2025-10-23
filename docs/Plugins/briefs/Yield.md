# Yield & Analytics Plugin Requirements (Draft)

## Overview

Yield plugins ingest combine telemetry, imports, and external datasets to produce yield, moisture, and test weight layers plus aggregated analytics. They coordinate with crop type, genetics, and profit plugins to deliver actionable insights and exports.

## Runtime Contracts

- Subscribe to session events to bind telemetry to the active job/session and capture environment metadata for reporting.【F:docs/development/SRS/sections/6X_Core_Domain_Services/62_Job_Lifecycle.md†L18-L74】
- Register layer definitions `yield.actual`, `yield.moisture`, and `yield.testWeight` in the Layer Registry. Ensure units, smoothing metadata, and provenance conform to ADR-049.【F:docs/development/SRS/sections/7X_Mapping_Geospatial/72-ADR-049 - Yield & Analytics Plugin.md†L21-L59】【F:docs/development/SRS/sections/3X_Data_Storage/32-ADR-010 - Layer registry and variable-rate framework.md†L33-L58】
- Support data ingest from machine telemetry, ISOXML TaskData, and CSV/GeoTIFF imports. Normalize to registry expectations (CRS, units) per the NX-113 flow before writing layers.【F:docs/development/SRS/sections/7X_Mapping_Geospatial/72_Mapping_Layers_Plugin.md†L84-L106】【F:docs/development/SRS/sections/3X_Data_Storage/32-ADR-014 - Interop for prescription and agronomic formats.md†L12-L56】
- Emit per-field and per-zone aggregates, linking results to crop type and genetics context to enable cross-filter analytics.【F:docs/development/SRS/sections/7X_Mapping_Geospatial/72-ADR-045 - Crop Type Plugin & Layers.md†L29-L71】【F:docs/development/SRS/sections/7X_Mapping_Geospatial/72-ADR-046 - Genetics Plugin & Layers.md†L21-L66】

## Layer Metadata Expectations

Each yield layer persists structured metadata describing the grid, smoothing pipeline, calibration profile, and aggregation bins exposed to the UI and analytics surfaces.

- `metadata.grid.cellSizeMeters` / `projection` describe the rasterization grid used for tiles, guaranteeing consumers can align tiles with other agronomic overlays.【F:schemas/YieldActual.v1.json†L38-L70】
- `metadata.smoothing` records the algorithm (`none`, `movingAverage`, `gaussian`, `kalman`, or `savitzkyGolay`) plus window, lag compensation, and pass count so replay pipelines and QA fixtures can reproduce normalization choices.【F:schemas/YieldActual.v1.json†L71-L110】
- `metadata.calibration` captures the applied calibration profile (ID, sensor model, moisture basis for test weight, notes, factors) to satisfy provenance requirements and enable troubleshooting of normalization drift.【F:schemas/YieldActual.v1.json†L111-L148】【F:schemas/YieldTestWeight.v1.json†L111-L151】
- `metadata.aggregation` defines rollup scopes and the binning scheme (quantile, equalInterval, or custom) shared with front-ends, including explicit breakpoints for custom palettes.【F:schemas/YieldActual.v1.json†L149-L204】
- `metadata.statistics` publishes summary metrics (count, mean, median, std-dev, range, and total mass for yield.actual) that feed analytics APIs and report templates without reprocessing tiles.【F:schemas/YieldActual.v1.json†L205-L241】

The runtime implementation leverages `CombineYieldLayerAggregator` to apply configurable kernel smoothing and produce `YieldLayerMetadata` snapshots covering grid, calibration, aggregation bins, and statistics. Import tooling such as `YieldImportService` reuses the same pipeline when normalised `CombineYieldMeasurement` samples arrive from ISOXML, shapefile, or CSV sources.

Clients relying on historical ad-hoc `extensions` fields should migrate to these structured properties; plugin-owned analytics may continue to live under `extensions` with namespace-qualified keys for downstream consumers.

## Analytics & Reporting

- Provide APIs for profit and report builder plugins to request yield × crop type/genetics breakdowns, field/season rollups, and temporal comparisons.【F:docs/development/SRS/sections/7X_Mapping_Geospatial/72-ADR-050 - Cost & Profit Plugin.md†L21-L52】【F:docs/development/SRS/sections/9X_Frontends_Ops/91-ADR-051 - Report Builder & Export System.md†L21-L52】
- Maintain QA fixtures verifying moisture/test weight calibration, smoothing algorithms, and aggregation accuracy across representative datasets.
- Surface anomaly detection (e.g., sensor dropouts, excessive smoothing) with actionable warnings and provenance references.

## UX Requirements

- Present yield maps with planned/actual toggles, tooltips showing crop/genetics context, and legend presets for accessibility.
- Offer time-series charts and distribution plots to highlight within-field variability and performance relative to planned prescriptions.

### Overlay UX baseline (NX-304)

- The map overlay must derive its legend bins directly from `YieldLayerMetadata.Aggregation.Bins` so UI palettes stay in lockstep with analytics exports. Equal-interval and quantile strategies should be rendered with explicit break labels, while custom bins surface author-supplied captions without re-ordering.【F:Nexus SourceCode/tests/Aog.Plugins.Tests/CombineYield/YieldRegressionFixtureTests.cs†L130-L194】
- Overlay tooltips display smoothed yield/moisture pairs using the same precision as the regression fixtures to guarantee parity between QA captures and operator experiences.【F:Nexus SourceCode/tests/Aog.Plugins.Tests/CombineYield/YieldRegressionFixtureTests.cs†L64-L119】
- Regression fixtures stored at `tests/Aog.Plugins.Tests/CombineYield/Data/YieldRegressionFixture.json` gate overlay updates in CI; any UX change that modifies binning, smoothing metadata, or provenance hashes must update the fixture and accompanying screenshot diffs before merge.【F:Nexus SourceCode/tests/Aog.Plugins.Tests/CombineYield/Data/YieldRegressionFixture.json†L1-L124】

## Compatibility Notes

- Offline operation is mandatory; imports and telemetry logging must persist locally and sync opportunistically when cloud storage is available.【F:docs/development/SRS/sections/6X_Core_Domain_Services/62-ADR-030 - Field job sessions and lifecycle services.md†L33-L86】
- Session IDs replace legacy Run references; exported analytics must reference `sessionId` for replay parity.【F:docs/development/SRS/sections/6X_Core_Domain_Services/62-ADR-041 - Job Sessions Lifecycle.md†L55-L73】
