# Planter Monitor Plugin

The Planter Monitor plugin publishes per-row population telemetry and aggregates
singulation analytics for dashboards and telemetry sinks. The
`PlanterMonitorPublisher` class pushes `PlanterRowStatus` messages onto the core
`IEventBus` while the `PlanterMonitorAnalyticsAggregator` introduced for NX-169
tracks running statistics for each row.

## Analytics Aggregator

The aggregator consumes `RowPopulationMeasurement` samples and produces a
`PlanterMonitorAnalyticsSnapshot`:

```csharp
var options = new PlanterMonitorOptions { RowCount = 12 };
var timeProvider = new FakeTimeProvider(DateTimeOffset.Parse("2024-05-01T12:00:00Z"));
var aggregator = new PlanterMonitorAnalyticsAggregator(options, timeProvider);

aggregator.Ingest(new RowPopulationMeasurement(3, 10.0, 9.2));
aggregator.Ingest(new RowPopulationMeasurement(4, 10.0, 11.0));

PlanterMonitorAnalyticsSnapshot snapshot = aggregator.CreateSnapshot();
```

The snapshot captures:

- Total measurements processed and the capture timestamp.
- Per-row averages (target, actual, population error, skip/double rates).
- Severity counts (rows currently in skip/double/unknown states) and worst-case
  metrics for alerting.

Snapshots are immutable and safe to share with UI panels, telemetry loggers, or
other observers without additional locking.

## Manifest Updates

The plugin manifest advertises the new analytics capability via
`supportedCapabilities` and declares the corresponding leases. The
`planter.monitor.analytics` lease uses shared mode so downstream services can
observe analytics without blocking telemetry publication.
