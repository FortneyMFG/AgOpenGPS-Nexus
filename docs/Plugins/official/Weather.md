# Weather Plugin

## Overview
The Weather plugin ingests live and historical weather data, computes agronomic indices, and distributes the results to other plugins. It helps drive decisions around spraying, planting, and field scouting.

## Capabilities
- `weather.ingest` shared capability for fetching observations and forecasts.
- `weather.analytics` shared capability for publishing derived indices (e.g., Delta T, growing degree days).
- Optional `notifications` capability for alerting operators to weather-driven advisories.

## Core Integration
- `WeatherIngestPipeline.cs` retrieves data from configured providers, normalises observations, and writes snapshots.
- `WeatherComputation.cs` calculates derived metrics, while `WeatherObservation.cs` encapsulates raw readings.
- `WeatherIngestOptions.cs` declares configuration options such as API keys, polling intervals, and unit preferences.
- Tests should mock provider APIs and validate metric computations across temperature/humidity edge cases.

## UI Integration
- Updates the status strip with current conditions and warnings.
- Provides map overlays for precipitation or temperature gradients when combined with Mapping.
- Planned zip packaging will expose configuration dialogs for API credentials and station selection.

## Dependencies
- Enhances Crop, Field Health, and Variable Mapping plugins by supplying environmental context.
- Requires network access; declare this in the manifest permissions and surface consent prompts in the Plugin Manager.

## Packaging Notes
- Bundle sample API responses or offline station data under `assets/fixtures/` so the plugin can operate in offline demos.
- Manifest should document supported providers to assist operators.

## Related Resources
- `docs/Plugins/briefs/Weather.md` (legacy guide) provides additional background.
- `docs/Plugins/official/Crop.md` and `FieldHealth.md` discuss how weather influences analytics.
- ADRs around environmental data ingestion (forthcoming) will cover provider governance.

