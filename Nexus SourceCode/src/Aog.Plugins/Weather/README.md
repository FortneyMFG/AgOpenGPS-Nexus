# Weather Plugin Building Blocks

These helpers wire the weather ingest pipeline (NX-271) into concrete features that
support ADR-053.

- `WeatherSensorAdapter` normalizes third-party sensor readings via
  `WeatherSensorReading` and forwards them to `WeatherIngestPipeline`. The adapter
  tracks the last known planar position for each source so downstream systems can
  associate snapshots with map coordinates.
- `WeatherOverlayFeed` converts published `WeatherObservation` instances into
  `AgronomicLayerDocument` overlays. The feed works with any `WeatherOverlayMetric`
  and publishes layers through the core event bus for UI consumption.
- `WeatherReportSummarySectionContributor` and
  `WeatherReportTimelineSectionContributor` register session-scoped report
  sections with the report builder. Both rely on
  `IWeatherSnapshotProvider` to source ordered snapshots before rendering
  aggregate summaries and detailed observation timelines, fulfilling NX-274.

Together the adapter and overlay feed unlock live weather telemetry across the
sensor stack, satisfying NX-272 and NX-273.
