# Cost & Profit Plugin Building Blocks

These building blocks bootstrap the ingestion pipeline outlined in
[ADR-050](../../../docs/development/SRS/sections/7X_Mapping_Geospatial/72-ADR-050 - Cost & Profit Plugin.md) by normalizing cost
entries and tracking material ledger balances.

- `CostRecord`, `CostScope`, and `CostCategory` capture normalized cost events
  that originate from manual entry, CSV imports, or automated connectors.
- `InventoryLotDefinition` and `InventoryLotSnapshot` represent material lots
  and their weighted cost basis.
- `CostLedger` orchestrates ingestion, maintains inventory balances, generates
  cost records when inventory is consumed, and produces aggregated summaries for
  profit analytics.
- `CostEntryDraft` and `CostEntryOrchestrationService` fulfill **NX-262** by
  generating deterministic identifiers, stamping metadata, and applying
  idempotency to cost capture flows before persisting records to the ledger.
- `RevenueContribution`, `ProfitAnalyticsRollupService`, and `ProfitRollup`
  deliver the **NX-263** analytics layer by combining ledger summaries with
  revenue inputs to compute per-currency profit and gross margin totals.
- `ProfitCell`, `ProfitExportOptions`, `ProfitExportPipeline`, and
  `ProfitExportRow` complete **NX-264** by turning spatial profit samples into
  exportable `ProfitLayer.v1` documents alongside per-currency summary rows for
  CSV/PDF pipelines.

Together these primitives fulfill tasks **NX-261** through **NX-264** by
providing ingestion, ledger, analytics, and export capabilities that downstream
UI and reporting flows can build on.
