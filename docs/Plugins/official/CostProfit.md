# Cost & Profit Analytics Plugin

## Overview
The Cost & Profit plugin tracks operational costs, revenue, and profitability metrics across fields and seasons. It aggregates consumption data from other plugins (rate control, telemetry) and provides financial insights that feed agronomic decision making.

## Capabilities
- `analytics.cost` shared capability for writing to the financial ledger.
- `analytics.profit` shared capability for publishing profitability layers and reports.
- Optional `fileio.export` capability when exporting ledgers to CSV or accounting systems.

## Core Integration
- `CostLedger.cs` persists normalised cost entries and supports reconciliation across seasons.
- `CostEntryOrchestrationService.cs` orchestrates ingestion of drafts (`CostEntryDraft.cs`) from upstream plugins like Variable Mapping or Rate Control.
- The service publishes financial snapshots onto the event bus so dashboards can refresh in real time.
- Tests in `tests/Aog.Plugins.Tests/CostProfit/*` validate ledger calculations, rounding rules, and multi-currency handling.

## UI Integration
- Provides block widgets summarising profit per hectare and cost breakdowns. When packaged as a zip, register these via `IBlockProvider`.
- Exposes detailed reports through a window descriptor (planned) that mirrors the legacy profitability dashboard.
- Pushes alerts to the status strip when expenses exceed configured thresholds.

## Dependencies
- Depends on telemetry from Rate Control and Variable Mapping to calculate application costs.
- Requires File IO when exporting ledgers and Device Manager when reconciling machine identities.

## Packaging Notes
- Manifest should declare storage requirements; the plugin stores ledger data under the plugin-specific cache path exposed by `IHostServices.Paths`.
- Bundle default cost category templates in `assets/templates/` to simplify onboarding.

## Related Resources
- `docs/Plugins/official/VariableMapping.md` for upstream rate inputs.
- `docs/Plugins/briefs/TelemetryLogging.md` for how raw telemetry is archived.
- Financial ADRs (forthcoming) will cover accounting integrations.

