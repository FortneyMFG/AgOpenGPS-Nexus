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
provenance. The plugin consumes yield data and crop/genetics context to produce rollups.

### Features

- Cost entry table with categories (seed, chem, fuel, labor, misc) and support for bulk imports from CSV or API connectors.
- Automatic ingestion of costs from genetics (seed usage), spraying jobs, and machine telemetry (fuel burn) when available.
- Profit heatmap overlay using normalized yield vs. cost per area with color-coded bins and tooltips summarizing contributions.
- Exports for CSV and PDF field/season summaries, including audit-ready breakdowns.

### Data Model

- `CostRecord.v1` includes immutable ID, scope (`farmId`, `fieldId?`, `jobId?`, `sessionId?`), category, amount, currency,
  quantity units, actor, timestamps, and optional links to layer IDs.
- `ProfitLayer.v1` extends `Layer.v1` metadata with per-cell profit, revenue, cost, and supporting references.
- Aggregated results stored in `job.extensions["profit.summary"]` and `season.extensions["profit.rollups"]` for analytics and
  report builder.

## Consequences

- Aligns agronomic decisions with financial metrics while preserving traceability.
- Requires strong data validation and unit normalization to prevent misinterpretation.
- Relies on timely availability of yield and cost inputs; plugins must handle missing data gracefully.

## Alternatives Considered

1. **Keep profit calculations external.** Rejected because it breaks provenance and prevents layering with other analytics.
2. **Embed costs directly in jobs.** Would overload job schema and complicate plugin governance.

## Dependencies

- Consumes yield (ADR-049), crop (ADR-045), and genetics (ADR-046) data for revenue calculations.
- Publishes report sections consumed by ADR-051 Report Builder.
- Integrates with ADR-047 mesh for sharing profit overlays when collaborating.

## SRS Impact

- Adds economic requirements to §08 Data Model & Storage and §12 Extensibility.
- Updates §05 Frontends with profit visualization expectations.
- Documents export/reporting needs in §17 Device/Firmware? (No) — Instead update §05 and §12 plus §08.
