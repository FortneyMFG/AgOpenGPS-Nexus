# Crop Context Plugin

## Overview
The Crop plugin centralises crop-specific context such as varieties, growth stages, and agronomic targets. It aggregates layers and analytics from mapping and telemetry sources to present actionable insights per crop and season.

## Capabilities
- `analytics.crop` shared capability for crop health and growth metrics.
- `layers.crop` shared capability for crop-specific map overlays.
- `reporting.crop` for contributing sections to agronomic reports.

## Core Integration
- `CropLayerIngestionPipeline.cs` orchestrates ingestion of crop-related layers, smoothing and normalising them before publishing to the layer registry.
- `CropAnalyticsService.cs` computes KPIs and exposes them as `CropAnalyticsSnapshot` instances.
- `CropReportSectionContributor.cs` injects crop metrics into reporting workflows.
- Tests in `tests/Aog.Plugins.Tests/Crop/*` validate snapshot calculations and pipeline behaviour.

## UI Integration
- Populates the crop quick select panel (`Nexus SourceCode/src/Aog.UI.Avalonia/ViewModels/CropQuickSelectViewModel.cs`) and related dashboards.
- Planned zip packaging will register block descriptors for crop summaries and provide a configuration dialog for managing crop presets.

## Dependencies
- Relies on Mapping for spatial layers and Field Health for stress indicators.
- Consumes telemetry from Variable Mapping and Weather to adjust recommendations.

## Packaging Notes
- Manifest should reference bundled crop catalogues under `assets/catalog/`.
- Declare optional dependencies on Field Health and Weather; the compatibility evaluator will surface missing data sources if they are not enabled.

## Related Resources
- `docs/Plugins/official/FieldHealth.md` for health score inputs.
- `docs/Plugins/official/Weather.md` for environmental context.
- `docs/Plugins/tutorials/first-plugin.md` demonstrates registering block surfaces similar to crop quick select.

