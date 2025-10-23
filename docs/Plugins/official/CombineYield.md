# Combine Yield Plugin

## Overview
The Combine Yield plugin ingests harvester bus data, normalises yield measurements, and produces agronomic layers that can be replayed or exported. It bridges live combine telemetry with the mapping subsystem so operators and agronomists can analyse performance per swath.

## Capabilities
- `layers.yield` shared capability for publishing raster yield layers.
- `telemetry.logging` shared capability to persist raw and aggregated measurements.
- Optional `fileio.export` integration for shapefile/GeoJSON exports.

## Core Integration
- Aggregation logic lives in `CombineYieldLayerAggregator.cs`; it converts individual `CombineYieldMeasurement` events into tiled summaries aligned with the layer registry.
- `CombineYieldOptions.cs` defines configuration defaults (moisture correction, flow meters, smoothing kernels).
- Utilises the event bus to subscribe to CAN-derived yield messages and emit `LayerUpdate` events consumed by Mapping.
- Regression tests in `tests/Aog.Plugins.Tests/CombineYield/*` (if present) ensure deterministic aggregation; add new fixtures when changing smoothing behaviour.

## UI Integration
- Registers layer providers with the map host so yield heatmaps can be toggled on/off.
- Provides a configuration dialog (planned) for calibration constants; hook into `settings.plugins` injection point when packaging as a zip.
- Surfaces status indicators summarising current yield, moisture, and throughput for the status strip.

## Dependencies
- Depends on Mapping for tile management and on File IO when export features are enabled.
- Should be activated after Device Manager establishes harvester telemetry channels.

## Packaging Notes
- Manifest must declare the `layers` capability and optionally `settings` if the configuration dialog is included.
- Include sample calibration profiles under `assets/calibration/` for quick-start onboarding.

## Related Resources
- `docs/Plugins/Mapping.md` for layer registry semantics.
- `docs/Plugins/tutorials/testing.md` for techniques to generate replay fixtures.
- Agronomic ADRs covering yield normalisation and data quality checks.

