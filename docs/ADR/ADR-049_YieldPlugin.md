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

## Governance Updates

- **Calibration bundles.** Yield ingest must attach calibration artifacts (header, sensor offsets, reference passes) that replay
  in the composite simulation harness before analytics updates can ship.【F:docs/ADR/ADR-004-composite-simulation.md†L9-L43】
- **Schema enforcement.** `YieldActual.v1`, `YieldMoisture.v1`, and `YieldTestWeight.v1` revisions trigger compatibility scans
  across Profit, Field Health, and Report Builder templates, ensuring downstream consumers stay in lockstep.【F:schemas/YieldActual.v1.json†L1-L140】【F:docs/ADR/ADR-050_CostProfitPlugin.md†L9-L70】【F:docs/ADR/ADR-051_ReportBuilder.md†L9-L70】
- **Fixture parity.** Regression packs now include collaborative harvest scenarios over mesh links to confirm streamed yield
  deltas stay within latency and determinism budgets when RadioBridge retransmits packets.【F:docs/ADR/ADR-047_LiveTelemetryMesh.md†L21-L70】【F:docs/ADR/ADR-048_RadioBridge.md†L9-L60】

## Amendment — 2025 architecture refresh (NX-190)

- Yield analytics consume the session-linked crop and genetics references so profitability overlays and rotation reporting
  derive accurate per-variety metrics without manual reconciliation.【F:schemas/Session.v1.json†L1-L120】【F:docs/ADR/ADR-045_CropTypePlugin.md†L9-L96】【F:docs/ADR/ADR-046_GeneticsPlugin.md†L9-L87】
- Multi-field jobs emit per-field yield aggregates alongside the union envelope, unlocking report templates and alerts that
  differentiate parcels even during continuous harvest passes.【F:docs/ADR/ADR-043_MultiFieldJobEnvelopes.md†L9-L112】
- Weather and Field Health overlays can subscribe to live yield deltas to correlate stress indicators during harvest, creating
  richer scouting feedback loops.【F:docs/ADR/ADR-052_FieldHealthPlugin.md†L9-L66】【F:docs/ADR/ADR-053_WeatherPlugin.md†L9-L66】

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
