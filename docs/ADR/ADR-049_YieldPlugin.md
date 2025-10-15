# ADR-049 — Yield & Analytics Plugin

- **Status:** Drafting
- **Date:** 2025-03-19
- **Author(s):** Nexus architecture guild
- **NX Task:** NX-190 Comprehensive ADR portfolio review

## Context

Harvest operations produce yield, moisture, and quality data streams that must be normalized, smoothed, and attributed to crop
and genetics decisions. Legacy tooling stores logs without consistent spatial context or provenance, making cross-season analytics
painful. Nexus requires a dedicated Yield plugin that captures sensor data, handles imports, and exposes APIs for analytics and
reporting.

## Decision

Deliver a Yield plugin owning three layer families (`yield.actual`, `yield.moisture`, `yield.testWeight`) with provenance chains
back to combines, imports, or manual entries. The plugin provides analytics APIs (`yieldByCrop`, `yieldByVariety`) and integrates
with the Crop Type and Genetics plugins to supply context.

### Data Flow

- Sensor ingest (CAN/ISOBUS, file imports) feeds a normalization pipeline applying calibration, lag correction, and smoothing.
- Layers stored via Core tile store with `Layer.v1` provenance referencing source device, calibration profile, and processing
  steps.
- Aggregations computed per field, job, season, crop, and variety; results published to `job.extensions["yield.stats"]` and
  `season.extensions["yield.rollups"]`.

### APIs & UI

- `getYieldByCrop(scope)` and `getYieldByVariety(scope)` return aggregated metrics (mean, median, std-dev, total mass) scoped to
  farm/season/job/session.
- Import wizard guides operators through loading monitor files (ISOXML, shapefile, CSV) with validation and unit conversion.
- Map visualizations use color gradients with quantile or equal-interval bins; UI exposes smoothing controls and statistics pane.
- Exports include CSV summaries and GeoJSON tiles containing aggregated metrics and provenance references.

## Consequences

- Enables analytics-driven profitability and crop planning by tying yield back to crop and genetics data.
- Requires robust calibration handling and sensor quality checks to avoid misleading analytics.
- Introduces additional storage and compute load for smoothing/aggregation pipelines.

## Alternatives Considered

1. **Keep yield in general analytics plugin.** Rejected because harvest-specific calibration and smoothing require dedicated
   logic and close coupling with field operations.
2. **Rely on OEM monitors for analytics.** Not acceptable for Nexus' open, cross-device ecosystem.

## Dependencies

- Consumes crop context from ADR-045 and genetics context from ADR-046.
- Optionally publishes to Profit (ADR-050) and Report Builder (ADR-051).
- Uses Zone Drawing Framework (ADR-044) for manual corrections or overrides.

## SRS Impact

- Meets yield layer metadata expectations in §08 Data Model & Storage (R-DATA-045).【F:docs/SRS/sections/08_Data_Model_Storage.md†L33-L34】
- Powers analytics overlays and legends referenced by R-FE-074 in §05 Frontends.【F:docs/SRS/sections/05_Frontends.md†L29-L30】
- Documents plugin APIs consumed by §12 Extensibility for analytics and export hooks.【F:docs/SRS/sections/12_Extensibility_Plugins.md†L18-L36】
