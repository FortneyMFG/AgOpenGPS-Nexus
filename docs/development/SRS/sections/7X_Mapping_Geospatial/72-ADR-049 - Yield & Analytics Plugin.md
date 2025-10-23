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
  in the composite simulation harness before analytics updates can ship.【F:docs/development/SRS/sections/2X_System_Architecture/21-ADR-004 - Establish the composite simulation fabric (SimClock + SimBus).md†L9-L43】
- **Schema enforcement.** `YieldActual.v1`, `YieldMoisture.v1`, and `YieldTestWeight.v1` revisions trigger compatibility scans
  across Profit, Field Health, and Report Builder templates, ensuring downstream consumers stay in lockstep.【F:schemas/YieldActual.v1.json†L1-L140】【F:docs/development/SRS/sections/7X_Mapping_Geospatial/72-ADR-050 - Cost & Profit Plugin.md†L9-L70】【F:docs/development/SRS/sections/9X_Frontends_Ops/91-ADR-051 - Report Builder & Export System.md†L9-L70】
- **Fixture parity.** Regression packs now include collaborative harvest scenarios over mesh links to confirm streamed yield
  deltas stay within latency and determinism budgets when RadioBridge retransmits packets.【F:docs/development/SRS/sections/4X_Interprocess_Communications/42-ADR-047 - Live Telemetry Mesh.md†L21-L70】【F:docs/development/SRS/sections/4X_Interprocess_Communications/42-ADR-048 - RadioBridge for ELRS LoRa Telemetry.md†L9-L60】

## Amendment — 2025 architecture refresh (NX-190)

- Yield analytics consume the session-linked crop and genetics references so profitability overlays and rotation reporting
  derive accurate per-variety metrics without manual reconciliation.【F:schemas/Session.v1.json†L1-L120】【F:docs/development/SRS/sections/7X_Mapping_Geospatial/72-ADR-045 - Crop Type Plugin & Layers.md†L9-L96】【F:docs/development/SRS/sections/7X_Mapping_Geospatial/72-ADR-046 - Genetics Plugin & Layers.md†L9-L87】
- Multi-field jobs emit per-field yield aggregates alongside the union envelope, unlocking report templates and alerts that
  differentiate parcels even during continuous harvest passes.【F:docs/development/SRS/sections/3X_Data_Storage/31-ADR-043 - Multi-Field Job Envelopes.md†L9-L112】
- Weather and Field Health overlays can subscribe to live yield deltas to correlate stress indicators during harvest, creating
  richer scouting feedback loops.【F:docs/development/SRS/sections/7X_Mapping_Geospatial/72-ADR-052 - Field Health & Risk Plugin.md†L9-L66】【F:docs/development/SRS/sections/7X_Mapping_Geospatial/72-ADR-053 - Weather & Environment Plugin.md†L9-L66】

## Alternatives Considered

1. **Keep yield in general analytics plugin.** Rejected because harvest-specific calibration and smoothing require dedicated
   logic and close coupling with field operations.
2. **Rely on OEM monitors for analytics.** Not acceptable for Nexus' open, cross-device ecosystem.

## Dependencies

- Consumes crop context from ADR-045 and genetics context from ADR-046.
- Optionally publishes to Profit (ADR-050) and Report Builder (ADR-051).
- Uses Zone Drawing Framework (ADR-044) for manual corrections or overrides.

## SRS Impact

- Meets yield layer metadata expectations in §08 Data Model & Storage (R-DATA-045).【F:docs/development/SRS/sections/3X_Data_Storage/32_Persistence_Formats.md†L33-L34】
- Powers analytics overlays and legends referenced by R-FE-074 in §05 Frontends.【F:docs/development/SRS/sections/9X_Frontends_Ops/91_UI_Shell_Layout.md†L29-L30】
- Documents plugin APIs consumed by §12 Extensibility for analytics and export hooks.【F:docs/development/SRS/sections/9X_Frontends_Ops/94_Extensibility_Packaging_Updates.md†L18-L36】
