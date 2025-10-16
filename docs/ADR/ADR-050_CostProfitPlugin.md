# ADR-050 — Cost & Profit Plugin

- **Status:** Drafting
- **Date:** 2025-03-19
- **Author(s):** Nexus architecture guild
- **NX Task:** NX-190 Comprehensive ADR portfolio review

## Context

Operators want to connect operational costs (seed, chemistry, fuel, labor) and revenue (yield-derived) to produce profit maps and
financial summaries. Existing spreadsheets lack spatial resolution and cannot integrate with live crop, genetics, or yield data.
Nexus needs a plugin that captures structured cost entries, computes profit layers, and exports per-field/season financial
reports.

## Decision

Deliver a Cost & Profit plugin that records cost transactions, links them to jobs/sessions/layers, and generates a
`profit.net` layer stored as `ProfitLayer.v1`. Costs are stored via `CostRecord.v1` documents with categories, amounts, and
provenance. The plugin consumes yield data and crop/genetics context to produce rollups. An Inventory & Material Ledger sub-
module maintains stock positions (seed, chemistry, fertilizer) per lot/batch. As sessions log application rates, the ledger
deducts quantities, updates weighted cost bases, and feeds replenishment forecasts back into Profit analytics.

### Features

- Cost entry table with categories (seed, chem, fuel, labor, misc) and support for bulk imports from CSV or API connectors.
- Automatic ingestion of costs from genetics (seed usage), spraying jobs, and machine telemetry (fuel burn) when available.
- Material ledger UI with barcode/QR scan support for lot intake, transfer, and reconciliation when field logs diverge from
  expected usage.
- Profit heatmap overlay using normalized yield vs. cost per area with color-coded bins and tooltips summarizing contributions.
- Exports for CSV and PDF field/season summaries, including audit-ready breakdowns.

### Data Model

- `CostRecord.v1` includes immutable ID, scope (`farmId`, `fieldId?`, `jobId?`, `sessionId?`), category, amount, currency,
  quantity units, actor, timestamps, optional links to layer IDs, and an optional `inventoryLotId` reference.
- `InventoryLot.v1` tracks SKU, supplier, lot/batch identifiers, quantity on hand, committed quantity (scheduled work orders),
  storage location, acquisition cost, and compliance attributes (e.g., restricted use, expiration).
- `ProfitLayer.v1` extends `Layer.v1` metadata with per-cell profit, revenue, cost, and supporting references.
- Aggregated results stored in `job.extensions["profit.summary"]` and `season.extensions["profit.rollups"]` for analytics and
  report builder.

## Consequences

- Aligns agronomic decisions with financial metrics while preserving traceability.
- Requires strong data validation and unit normalization to prevent misinterpretation.
- Relies on timely availability of yield and cost inputs; plugins must handle missing data gracefully.

## Governance Updates

- **Ledger attestation.** `CostRecord.v1` entries require dual attestation (operator + reviewer) for high-impact categories; CI
  validates that rollups reconcile with inventory ledger balances before releases exit staging.【F:schemas/CostRecord.v1.json†L1-L160】
- **Profit layer certification.** `ProfitLayer.v1` exports carry summary stats and hash manifests so Report Builder and analytics
  consumers confirm the layer matches recorded sessions before generating financial statements.【F:schemas/ProfitLayer.v1.json†L1-L140】【F:docs/ADR/ADR-051_ReportBuilder.md†L9-L70】
- **Cross-plugin gating.** Profit analytics may only publish rollups when Crop, Genetics, Yield, Field Health, and Weather
  plugins expose session-aligned context, preventing partially informed financial summaries.【F:docs/ADR/ADR-045_CropTypePlugin.md†L9-L96】【F:docs/ADR/ADR-049_YieldPlugin.md†L9-L96】【F:docs/ADR/ADR-053_WeatherPlugin.md†L9-L70】

## Amendment — 2025 architecture refresh (NX-190)

- Multi-field envelope splits allocate costs and revenue per field automatically, keeping cross-field jobs auditable without
  manual spreadsheets.【F:docs/ADR/ADR-043_MultiFieldJobEnvelopes.md†L9-L112】
- Profit rollups include session hashes, crop IDs, genetics lots, and weather snapshots so downstream analytics can trace every
  metric back to the exact operating context.【F:schemas/Session.v1.json†L1-L120】【F:docs/ADR/ADR-053_WeatherPlugin.md†L9-L66】
- Mesh presence events trigger incremental profit exports, letting collaborating machines compare live profitability while
  radio bandwidth stays bounded via RadioBridge throttling policies.【F:docs/ADR/ADR-047_LiveTelemetryMesh.md†L21-L70】【F:docs/ADR/ADR-048_RadioBridge.md†L9-L60】

## Alternatives Considered

1. **Keep profit calculations external.** Rejected because it breaks provenance and prevents layering with other analytics.
2. **Embed costs directly in jobs.** Would overload job schema and complicate plugin governance.

## Dependencies

- Consumes yield (ADR-049), crop (ADR-045), and genetics (ADR-046) data for revenue calculations.
- Publishes report sections consumed by ADR-051 Report Builder.
- Integrates with ADR-047 mesh for sharing profit overlays when collaborating.

## SRS Impact

- Covers ledger, cost, and provenance requirements R-DATA-046, R-DATA-050, and R-DATA-051 in §08 Data Model & Storage.【F:docs/SRS/sections/08_Data_Model_Storage.md†L34-L39】
- Supports profit overlays and work order reconciliation described in §05 Frontends (R-FE-074) and §03 Job Lifecycle (R-JOB-042…R-JOB-043).【F:docs/SRS/sections/05_Frontends.md†L29-L30】【F:docs/SRS/sections/03_JobLifecycle.md†L73-L76】
- Establishes plugin responsibilities for economic analytics within §12 Extensibility policies.【F:docs/SRS/sections/12_Extensibility_Plugins.md†L18-L36】
