# Field Health Plugin

## Overview
The Field Health plugin ingests canopy, NDVI, and soil sensor feeds to compute field stress scores. It maintains history snapshots so agronomists can monitor changes over time and trigger alerts when thresholds are exceeded.

## Capabilities
- `analytics.fieldhealth` shared capability for publishing stress indices.
- `layers.fieldhealth` shared capability for map overlays representing health history.
- Optional `notifications` capability for sending alerts to operators.

## Core Integration
- `FieldHealthIngestPipeline.cs` coordinates ingestion from sensors and remote imagery. It normalises data and produces `FieldHealthLayerHistory` snapshots.
- `FieldHealthHistoryEntry.cs` and `FieldHealthHistoryToggles.cs` capture historical observations and operator overrides.
- Ingest options (`FieldHealthIngestOptions.cs`) allow runtime configuration of smoothing, thresholds, and sensor weighting.
- Unit tests should cover ingestion edge cases and history rollups across seasons.

## UI Integration
- Drives the Field Health severity panel (`Nexus SourceCode/src/Aog.UI.Avalonia/ViewModels/FieldHealthSeverityPanelViewModel.cs`).
- Provides layer toggles for the map host and status indicators when stress exceeds configured thresholds.
- Future packaging will expose a configuration dialog allowing operators to manage sensor sources and alert thresholds.

## Dependencies
- Consumes weather data and crop context to adjust health scoring.
- Works closely with Mapping to render tiled overlays.

## Packaging Notes
- Manifest should declare optional dependencies on Weather and Crop plugins; compatibility evaluation will highlight the additional insights they unlock.
- Bundle default alert rules under `assets/rules/` for first-run configuration.

## Related Resources
- `docs/Plugins/official/Crop.md` for crop integration.
- `docs/Plugins/official/Weather.md` for environmental inputs.
- ADRs covering sensor fusion (forthcoming) will describe data contracts in more detail.

