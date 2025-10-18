using System;
using System.Collections.Generic;
using Aog.Core.V1;
using Aog.Plugins.PlanterMonitor;
using Microsoft.Extensions.Time.Testing;
using FluentAssertions;
using Xunit;

namespace Aog.Plugins.Tests;

public sealed class PlanterMonitorAnalyticsAggregatorTests
{
    private static PlanterMonitorOptions CreateOptions(int rowCount = 8) => new()
    {
        RowCount = rowCount,
        SkipThreshold = 0.25,
        DoubleThreshold = 0.25,
        Frame = "vehicle",
        Source = "sim"
    };

    [Fact]
    public void Ingest_SingleMeasurementProducesRowSummary()
    {
        var options = CreateOptions();
        var timeProvider = new FakeTimeProvider(DateTimeOffset.Parse("2024-03-01T12:00:00Z"));
        var aggregator = new PlanterMonitorAnalyticsAggregator(options, timeProvider);

        aggregator.Ingest(new RowPopulationMeasurement(2, 10.0, 8.0));
        var snapshot = aggregator.CreateSnapshot();

        snapshot.Timestamp.Should().Be(timeProvider.GetUtcNow());
        snapshot.TotalMeasurements.Should().Be(1);
        snapshot.ActiveRowCount.Should().Be(1);
        snapshot.SkipRowCount.Should().Be(1);
        snapshot.DoubleRowCount.Should().Be(0);
        snapshot.UnknownRowCount.Should().Be(0);
        snapshot.AveragePopulationErrorPerMeter.Should().Be(2.0);
        snapshot.AverageSkipRate.Should().Be(0.8);
        snapshot.AverageDoubleRate.Should().Be(0);
        snapshot.MaxSkipRate.Should().Be(0.8);
        snapshot.MaxDoubleRate.Should().Be(0);
        snapshot.OverallQuality.Should().Be(PlanterRowQuality.Skip);

        snapshot.Rows.Should().ContainSingle();
        var row = snapshot.Rows[0];
        row.RowIndex.Should().Be(2);
        row.SampleCount.Should().Be(1);
        row.AverageTargetPopulationPerMeter.Should().Be(10.0);
        row.AverageActualPopulationPerMeter.Should().Be(8.0);
        row.AveragePopulationErrorPerMeter.Should().Be(2.0);
        row.AverageSkipRate.Should().Be(0.8);
        row.AverageDoubleRate.Should().Be(0);
        row.SkipCount.Should().Be(1);
        row.DoubleCount.Should().Be(0);
        row.UnknownCount.Should().Be(0);
        row.LatestQuality.Should().Be(PlanterRowQuality.Skip);
        row.LatestSkipRate.Should().Be(0.8);
        row.LatestDoubleRate.Should().Be(0);
        row.LatestTargetPopulationPerMeter.Should().Be(10.0);
        row.LatestActualPopulationPerMeter.Should().Be(8.0);
    }

    [Fact]
    public void Ingest_MultipleRowsTracksAggregates()
    {
        var options = CreateOptions();
        var aggregator = new PlanterMonitorAnalyticsAggregator(options, new FakeTimeProvider());

        var measurements = new List<RowPopulationMeasurement>
        {
            new(0, 12.0, 12.0),
            new(1, 12.0, 14.4),
            new(2, 12.0, 6.0),
            new(3, 0.0, 0.0)
        };

        aggregator.Ingest(measurements);
        var snapshot = aggregator.CreateSnapshot();

        snapshot.TotalMeasurements.Should().Be(4);
        snapshot.ActiveRowCount.Should().Be(4);
        snapshot.SkipRowCount.Should().Be(1);
        snapshot.DoubleRowCount.Should().Be(1);
        snapshot.UnknownRowCount.Should().Be(1);
        snapshot.OverallQuality.Should().Be(PlanterRowQuality.Double);

        snapshot.Rows.Should().HaveCount(4);
        snapshot.Rows[0].LatestQuality.Should().Be(PlanterRowQuality.Ok);
        snapshot.Rows[1].LatestQuality.Should().Be(PlanterRowQuality.Double);
        snapshot.Rows[2].LatestQuality.Should().Be(PlanterRowQuality.Skip);
        snapshot.Rows[3].LatestQuality.Should().Be(PlanterRowQuality.Unknown);
    }

    [Fact]
    public void Ingest_RepeatedMeasurementsAccumulateStatistics()
    {
        var options = CreateOptions();
        var aggregator = new PlanterMonitorAnalyticsAggregator(options, new FakeTimeProvider());

        aggregator.Ingest(new RowPopulationMeasurement(0, 10.0, 9.0));
        aggregator.Ingest(new RowPopulationMeasurement(0, 10.0, 11.0));
        aggregator.Ingest(new RowPopulationMeasurement(0, 10.0, 10.0));

        var snapshot = aggregator.CreateSnapshot();
        snapshot.TotalMeasurements.Should().Be(3);
        snapshot.ActiveRowCount.Should().Be(1);
        snapshot.Rows.Should().ContainSingle();
        var row = snapshot.Rows[0];
        row.SampleCount.Should().Be(3);
        row.AverageTargetPopulationPerMeter.Should().Be(10.0);
        row.AverageActualPopulationPerMeter.Should().BeApproximately(10.0, 1e-6);
        row.AveragePopulationErrorPerMeter.Should().BeApproximately(0.6666666, 1e-6);
        row.SkipCount.Should().Be(1);
        row.DoubleCount.Should().Be(1);
        row.UnknownCount.Should().Be(0);
        row.LatestQuality.Should().Be(PlanterRowQuality.Ok);
    }

    [Fact]
    public void Ingest_InvalidRowIndex_Throws()
    {
        var aggregator = new PlanterMonitorAnalyticsAggregator(CreateOptions(), new FakeTimeProvider());

        var act = () => aggregator.Ingest(new RowPopulationMeasurement(-1, 10.0, 10.0));

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Ingest_RowBeyondConfiguredCount_Throws()
    {
        var options = CreateOptions(rowCount: 2);
        var aggregator = new PlanterMonitorAnalyticsAggregator(options, new FakeTimeProvider());

        var act = () => aggregator.Ingest(new RowPopulationMeasurement(5, 10.0, 10.0));

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Ingest_BatchNull_Throws()
    {
        var aggregator = new PlanterMonitorAnalyticsAggregator(CreateOptions(), new FakeTimeProvider());

        Action act = () => aggregator.Ingest(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void CreateSnapshot_EmptyAggregatorUsesUnknownQuality()
    {
        var timeProvider = new FakeTimeProvider(DateTimeOffset.Parse("2024-05-01T06:00:00Z"));
        var aggregator = new PlanterMonitorAnalyticsAggregator(CreateOptions(), timeProvider);

        var snapshot = aggregator.CreateSnapshot();

        snapshot.TotalMeasurements.Should().Be(0);
        snapshot.ActiveRowCount.Should().Be(0);
        snapshot.OverallQuality.Should().Be(PlanterRowQuality.Unknown);
        snapshot.Rows.Should().BeEmpty();
        snapshot.Timestamp.Should().Be(timeProvider.GetUtcNow());
    }
}
