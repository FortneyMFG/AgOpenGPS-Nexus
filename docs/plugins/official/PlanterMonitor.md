# Planter Monitor Plugin

## Overview
The Planter Monitor plugin oversees row unit performance in real time. It calculates skips, doubles, and singulation metrics, enabling operators to react quickly to planter issues and capture clean datasets for agronomic analysis.

## Capabilities
- `planter.monitor` exclusive capability for ingesting planter telemetry.
- `planter.analytics` shared capability for publishing aggregated metrics.
- Optional `notifications` capability for row alerting.

## Core Integration
- `PlanterMonitorPublisher.cs` subscribes to planter bus telemetry and emits row updates.
- `PlanterMonitorAnalyticsAggregator.cs` and `PlanterMonitorMath.cs` compute aggregate metrics per row and per machine.
- `PlanterMonitorAnalyticsSnapshot.cs` encapsulates the metrics consumed by dashboards and logging.
- Configuration is centralised in `PlanterMonitorOptions.cs`.
- Tests should validate metrics across a variety of skip/double scenarios to ensure consistent behaviour.

## UI Integration
- Powers the Planter panel view-model (`Nexus SourceCode/src/Aog.UI.Avalonia/ViewModels/PlanterPanelViewModel.cs`) and status strip indicators.
- Provides toolbar commands to toggle row monitoring and open diagnostics windows.
- When packaged, register block descriptors for per-row heatmaps and dialogs for calibration workflows.

## Dependencies
- Relies on Telemetry Logging to persist planter telemetry and on Mapping for spatial context.
- Integrates with Job Tasks so planter sessions align with job lifecycle state.

## Packaging Notes
- Manifest should request permissions required to access the planter telemetry transport (serial/network).
- Bundle calibration defaults and row layout templates under `assets/calibration/`.

## Related Resources
- `docs/plugins/official/TelemetryLogging.md` for telemetry persistence.
- `docs/plugins/official/VariableMapping.md` to understand how planter data feeds rate adjustments.
- Existing documentation under `docs/plugins/planter-monitor.md` (legacy) can provide additional history.

