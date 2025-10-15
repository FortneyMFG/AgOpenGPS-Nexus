# Cost &amp; Profit Plugin Ingestion + Ledger

These building blocks bootstrap the ingestion pipeline outlined in
[ADR-050](../../../docs/ADR/ADR-050_CostProfitPlugin.md) by normalizing cost
entries and tracking material ledger balances.

- `CostRecord`, `CostScope`, and `CostCategory` capture normalized cost events
  that originate from manual entry, CSV imports, or automated connectors.
- `InventoryLotDefinition` and `InventoryLotSnapshot` represent material lots
  and their weighted cost basis.
- `CostLedger` orchestrates ingestion, maintains inventory balances, generates
  cost records when inventory is consumed, and produces aggregated summaries for
  profit analytics.

Together these primitives fulfill task **NX-261** by providing ingestion and
ledger capabilities that downstream analytics and reporting flows can build on.
