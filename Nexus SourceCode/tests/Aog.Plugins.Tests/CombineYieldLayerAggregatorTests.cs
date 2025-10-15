using System;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Aog.Core.Eventing;
using Aog.Core.V1;
using Aog.Plugins.CombineYield;
using FluentAssertions;
using Xunit;

namespace Aog.Plugins.Tests;

public sealed class CombineYieldLayerAggregatorTests
{
    private static CombineYieldOptions CreateOptions() => new()
    {
        CellSizeMeters = 10,
        PublishInterval = TimeSpan.Zero,
        Crop = "Corn",
        Frame = "field",
        Source = "sim"
    };

    [Fact]
    public async Task IngestAsync_PublishesLayerWithSingleCell()
    {
        var bus = new InMemoryEventBus();
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2024, 1, 1, 8, 0, 0, TimeSpan.Zero));
        var aggregator = new CombineYieldLayerAggregator(bus, CreateOptions(), timeProvider);

        CombineYieldLayerPublication? published = null;
        using var subscription = bus.Subscribe<CombineYieldLayerPublication>((publication, _) =>
        {
            published = publication;
            return ValueTask.CompletedTask;
        });

        await aggregator.IngestAsync(new CombineYieldMeasurement(5, 5, 8500, 18));

        published.Should().NotBeNull();
        published!.Layer.Header.Sequence.Should().Be(1);
        published.Layer.Header.Source.Should().Be("sim");
        published.Layer.Header.Frame.Should().Be("field");
        published.Layer.Header.Timestamp.ToDateTimeOffset().Should().Be(timeProvider.GetUtcNow());
        published.Layer.Crop.Should().Be("Corn");
        published.Layer.CellSizeMeters.Should().Be(10);
        published.Layer.Cells.Should().HaveCount(1);
        var cell = published.Layer.Cells[0];
        cell.Column.Should().Be(0);
        cell.Row.Should().Be(0);
        cell.AverageYieldKgPerHectare.Should().Be(8500);
        cell.AverageMoisturePercent.Should().Be(18);
        cell.SampleCount.Should().Be(1);

        published.Metadata.Should().NotBeNull();
        published.Metadata.Grid.CellSizeMeters.Should().Be(10);
        published.Metadata.Grid.Projection.Should().Be("EPSG:4978");
        published.Metadata.Smoothing.Method.Should().Be("movingAverage");
        published.Metadata.Statistics.SampleCount.Should().Be(1);
        published.Metadata.Statistics.Mean.Should().Be(8500);
        published.Metadata.Statistics.Median.Should().Be(8500);
        published.Metadata.Statistics.StdDev.Should().Be(0);
        published.Metadata.Statistics.TotalMassKg.Should().BeApproximately(85, 1e-6);
        published.Metadata.Aggregation.Bins.Scheme.Should().Be("quantile");
        published.Metadata.Aggregation.Bins.Count.Should().Be(5);
        published.Metadata.Calibration.ProfileId.Should().Be("calibration:default");
        published.Metadata.Aggregation.Scopes.Should().Contain("job");
        published.Metadata.Aggregation.Scopes.Should().Contain("field");

        published.Provenance.Source.Should().Be("sim");
        published.Provenance.Transform.Should().Be("aggregate:combine-yield");
        published.Provenance.Actor.Should().Be("plugin:combine-yield");
        published.Provenance.CreatedAt.Should().Be(timeProvider.GetUtcNow());

        var expectedHash = ComputeExpectedHash(published.Layer);
        published.Provenance.Hash.Should().Be(expectedHash);
    }

    [Fact]
    public async Task IngestAsync_AveragesMultipleSamples()
    {
        var bus = new InMemoryEventBus();
        var aggregator = new CombineYieldLayerAggregator(bus, CreateOptions(), new FakeTimeProvider());

        CombineYieldLayerPublication? published = null;
        using var subscription = bus.Subscribe<CombineYieldLayerPublication>((publication, _) =>
        {
            published = publication;
            return ValueTask.CompletedTask;
        });

        await aggregator.IngestAsync(new CombineYieldMeasurement(9.9, 0.1, 9000, 16));
        await aggregator.IngestAsync(new CombineYieldMeasurement(0.2, 0.2, 11000, 14));

        published.Should().NotBeNull();
        var cell = published!.Layer.Cells.Single();
        cell.AverageYieldKgPerHectare.Should().BeApproximately(10000, 1e-3);
        cell.AverageMoisturePercent.Should().BeApproximately(15, 1e-3);
        cell.SampleCount.Should().Be(2);
        published.Metadata.Statistics.SampleCount.Should().Be(2);
        published.Metadata.Statistics.Mean.Should().BeApproximately(10000, 1e-3);
    }

    [Fact]
    public async Task FlushAsync_PublishesWhenIntervalNotElapsed()
    {
        var bus = new InMemoryEventBus();
        var options = CreateOptions();
        options.PublishInterval = TimeSpan.FromSeconds(30);
        var timeProvider = new FakeTimeProvider();
        var aggregator = new CombineYieldLayerAggregator(bus, options, timeProvider);

        CombineYieldLayerPublication? published = null;
        using var subscription = bus.Subscribe<CombineYieldLayerPublication>((publication, _) =>
        {
            published = publication;
            return ValueTask.CompletedTask;
        });

        await aggregator.IngestAsync(new CombineYieldMeasurement(2, 2, 5000));

        published.Should().BeNull();

        await aggregator.FlushAsync();

        published.Should().NotBeNull();
        published!.Layer.Cells.Should().HaveCount(1);
    }

    [Fact]
    public async Task CreateLayerSnapshot_ReturnsCurrentAggregates()
    {
        var bus = new InMemoryEventBus();
        var options = CreateOptions();
        options.PublishInterval = TimeSpan.FromMinutes(1);
        var timeProvider = new FakeTimeProvider();
        var aggregator = new CombineYieldLayerAggregator(bus, options, timeProvider);

        Func<Task> act = () => aggregator.IngestAsync(new CombineYieldMeasurement(1, 1, 4000)).AsTask();

        await act.Should().NotThrowAsync();

        var snapshot = aggregator.CreateLayerSnapshot();

        snapshot.Layer.Cells.Should().HaveCount(1);
        snapshot.Layer.Cells[0].AverageYieldKgPerHectare.Should().Be(4000);
        snapshot.Layer.Header.Sequence.Should().Be(0);
        snapshot.Provenance.Hash.Should().NotBeNullOrEmpty();
        snapshot.Metadata.Statistics.SampleCount.Should().Be(1);
    }

    [Fact]
    public async Task IngestAsync_UpdatesProvenanceHashWhenCellsChange()
    {
        var bus = new InMemoryEventBus();
        var aggregator = new CombineYieldLayerAggregator(bus, CreateOptions(), new FakeTimeProvider());

        CombineYieldLayerPublication? first = null;
        CombineYieldLayerPublication? second = null;
        using var subscription = bus.Subscribe<CombineYieldLayerPublication>((publication, _) =>
        {
            if (first is null)
            {
                first = publication;
            }
            else
            {
                second = publication;
            }

            return ValueTask.CompletedTask;
        });

        await aggregator.IngestAsync(new CombineYieldMeasurement(5, 5, 8500, 18));
        await aggregator.IngestAsync(new CombineYieldMeasurement(25, 25, 9000, 20));

        first.Should().NotBeNull();
        second.Should().NotBeNull();
        second!.Provenance.Hash.Should().NotBe(first!.Provenance.Hash);
    }

    [Fact]
    public async Task IngestAsync_InvalidMeasurementThrows()
    {
        var bus = new InMemoryEventBus();
        var aggregator = new CombineYieldLayerAggregator(bus, CreateOptions(), new FakeTimeProvider());

        var action = () => aggregator.IngestAsync(new CombineYieldMeasurement(-1, 0, 5000)).AsTask();

        await action.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task Smoothing_BlendsNeighboringCells()
    {
        var bus = new InMemoryEventBus();
        var options = CreateOptions();
        options.SmoothingKernelSize = 3;
        options.OutlierClampFraction = 0;
        var aggregator = new CombineYieldLayerAggregator(bus, options, new FakeTimeProvider());

        CombineYieldLayerPublication? latest = null;
        using var subscription = bus.Subscribe<CombineYieldLayerPublication>((publication, _) =>
        {
            latest = publication;
            return ValueTask.CompletedTask;
        });

        await aggregator.IngestAsync(new CombineYieldMeasurement(1, 1, 12000));
        await aggregator.IngestAsync(new CombineYieldMeasurement(11, 1, 6000));

        latest.Should().NotBeNull();
        var cells = latest!.Layer.Cells.OrderBy(cell => cell.Column).ToArray();
        cells.Should().HaveCount(2);
        cells[0].AverageYieldKgPerHectare.Should().BeApproximately(9000, 1e-6);
        cells[1].AverageYieldKgPerHectare.Should().BeApproximately(9000, 1e-6);
        latest.Metadata.Statistics.SampleCount.Should().Be(2);
        latest.Metadata.Statistics.StdDev.Should().Be(0);
        latest.Metadata.Aggregation.Bins.Count.Should().Be(options.BinningBinCount);
    }

    private static string ComputeExpectedHash(CombineYieldLayer layer)
    {
        var builder = new StringBuilder();
        builder.AppendFormat(
            CultureInfo.InvariantCulture,
            "{0};{1:F3};",
            layer.Crop,
            layer.CellSizeMeters);

        foreach (var cell in layer.Cells.OrderBy(cell => cell.Row).ThenBy(cell => cell.Column))
        {
            builder.AppendFormat(
                CultureInfo.InvariantCulture,
                "{0},{1},{2:G17},{3:G17},{4};",
                cell.Column,
                cell.Row,
                cell.AverageYieldKgPerHectare,
                cell.AverageMoisturePercent,
                cell.SampleCount);
        }

        var bytes = Encoding.UTF8.GetBytes(builder.ToString());
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }
}
