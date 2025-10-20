# Profit Plugin Requirements (Draft)

## Overview

The Profit plugin aggregates cost records, yield-derived revenue, and profitability overlays to deliver field, farm, and season insights. It coordinates with cost ledgers, yield analytics, and report builders to expose ROI heatmaps and financial summaries.

## Runtime Contracts

- Consume cost ledger entries stored in `CostRecord.v1.json`, inventory ledger balances from `InventoryLot.v1.json`, and yield layers from the Yield plugin to compute `profit.net` overlays registered in the Layer Registry.【F:schemas/CostRecord.v1.json†L1-L120】【F:docs/ADR/ADR-050_CostProfitPlugin.md†L21-L52】
- Attach profitability layers to the active session when generated during field work, ensuring provenance captures contributing cost/yield layer IDs and calculation timestamps.【F:docs/ADR/ADR-041_JobSessions.md†L55-L73】
- Provide APIs for analytics and reporting plugins to query profitability rollups by field, farm, and season, including breakdowns by crop type and genetics context.【F:docs/ADR/ADR-045_CropTypePlugin.md†L29-L71】【F:docs/ADR/ADR-046_GeneticsPlugin.md†L21-L66】【F:docs/ADR/ADR-051_ReportBuilder.md†L21-L52】

## Cost Ledger Management

- Expose UI forms and importers for manual entries (fuel, labor, inputs) and automated deductions sourced from layer provenance (e.g., rate controller logs). Material usage should reconcile against inventory lots, prompting operators when applied quantities exceed expected balances.
- Ensure every ledger entry references scope (`farmId`, `fieldId?`, `jobId?`, `sessionId?`), category, currency, quantity, and authoring metadata as defined in `CostRecord.v1.json`.
- Support offline-first operation with local ledger storage, reconciling with shared stores when connectivity returns per ADR-030. Inventory adjustments log pending transactions that sync once connectivity returns, preserving lot integrity and avoiding double-deduct scenarios.【F:docs/ADR/ADR-030-field-job-sessions.md†L33-L86】

## Inventory & material ledger integration

- Maintain stock ledgers per SKU/lot with quantity on hand, committed work order quantities, and cost basis so Profit analytics can model true margins and purchasing forecasts.【F:docs/ADR/ADR-050_CostProfitPlugin.md†L15-L40】
- Support barcode/QR scanning for intake, transfers, and application reconciliation. When sessions close, the plugin posts `InventoryTransaction` entries keyed to `sessionId` and `workOrderId` to maintain traceability back to field work.【F:docs/SRS/sections/6X_Core_Domain_Services/62_Job_Lifecycle.md†L86-L109】
- Expose dashboards highlighting low stock, expiring lots, and discrepancies between planned vs. as-applied quantities. Operators must be able to adjust counts with audit notes that flow into regulatory exports.【F:docs/plugins/Regulatory.md†L1-L140】

## UX Requirements

- Provide ROI heatmaps with legends, tooltips showing cost/revenue breakdowns, and quick filters for field, farm, and season scopes.
- Offer ledger views with search, filtering, and export actions (CSV/PDF) aligned with report builder templates.
- Surface alerts when ledger entries lack matching yield layers or when profitability falls outside configurable thresholds.

## Exports & Reporting

- Generate CSV/PDF summaries and GeoJSON overlays for Crop Report and Profit Summary templates, embedding provenance references for audit.【F:docs/ADR/ADR-051_ReportBuilder.md†L21-L52】
- Coordinate with analytics exports so profitability results feed into multi-machine dashboards without exposing sensitive data unless share profiles permit it.【F:docs/plugins/MultiMachine.md†L1-L80】

## Compatibility Notes

- Profit calculations must remain deterministic across replays; regression fixtures compare profitability outputs using seeded datasets to guarantee parity.【F:docs/ADR/ADR-004-composite-simulation.md†L43-L78】
- Session IDs replace legacy Run identifiers in ledger exports and overlays to align with job lifecycle changes.【F:docs/ADR/ADR-041_JobSessions.md†L55-L73】
