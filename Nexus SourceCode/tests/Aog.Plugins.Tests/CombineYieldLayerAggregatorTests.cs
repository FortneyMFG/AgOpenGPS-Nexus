using System;
using System.Linq;
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

        CombineYieldLayer? published = null;
        using var subscription = bus.Subscribe<CombineYieldLayer>((layer, _) =>
        {
            published = layer;
            return ValueTask.CompletedTask;
        });

        await aggregator.IngestAsync(new CombineYieldMeasurement(5, 5, 8500, 18));

        published.Should().NotBeNull();
        published!.Header.Sequence.Should().Be(1);
        published.Header.Source.Should().Be("sim");
        published.Header.Frame.Should().Be("field");
        published.Header.Timestamp.ToDateTimeOffset().Should().Be(timeProvider.GetUtcNow());
        published.Crop.Should().Be("Corn");
        published.CellSizeMeters.Should().Be(10);
        published.Cells.Should().HaveCount(1);
        var cell = published.Cells[0];
        cell.Column.Should().Be(0);
        cell.Row.Should().Be(0);
        cell.AverageYieldKgPerHectare.Should().Be(8500);
        cell.AverageMoisturePercent.Should().Be(18);
        cell.SampleCount.Should().Be(1);
    }

    [Fact]
    public async Task IngestAsync_AveragesMultipleSamples()
    {
        var bus = new InMemoryEventBus();
        var aggregator = new CombineYieldLayerAggregator(bus, CreateOptions(), new FakeTimeProvider());

        CombineYieldLayer? published = null;
        using var subscription = bus.Subscribe<CombineYieldLayer>((layer, _) =>
        {
            published = layer;
            return ValueTask.CompletedTask;
        });

        await aggregator.IngestAsync(new CombineYieldMeasurement(9.9, 0.1, 9000, 16));
        await aggregator.IngestAsync(new CombineYieldMeasurement(0.2, 0.2, 11000, 14));

        published.Should().NotBeNull();
        var cell = published!.Cells.Single();
        cell.AverageYieldKgPerHectare.Should().BeApproximately(10000, 1e-3);
        cell.AverageMoisturePercent.Should().BeApproximately(15, 1e-3);
        cell.SampleCount.Should().Be(2);
    }

    [Fact]
    public async Task FlushAsync_PublishesWhenIntervalNotElapsed()
    {
        var bus = new InMemoryEventBus();
        var options = CreateOptions();
        options.PublishInterval = TimeSpan.FromSeconds(30);
        var timeProvider = new FakeTimeProvider();
        var aggregator = new CombineYieldLayerAggregator(bus, options, timeProvider);

        CombineYieldLayer? published = null;
        using var subscription = bus.Subscribe<CombineYieldLayer>((layer, _) =>
        {
            published = layer;
            return ValueTask.CompletedTask;
        });

        await aggregator.IngestAsync(new CombineYieldMeasurement(2, 2, 5000));

        published.Should().BeNull();

        await aggregator.FlushAsync();

        published.Should().NotBeNull();
        published!.Cells.Should().HaveCount(1);
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

        snapshot.Cells.Should().HaveCount(1);
        snapshot.Cells[0].AverageYieldKgPerHectare.Should().Be(4000);
        snapshot.Header.Sequence.Should().Be(0);
    }

    [Fact]
    public async Task IngestAsync_InvalidMeasurementThrows()
    {
        var bus = new InMemoryEventBus();
        var aggregator = new CombineYieldLayerAggregator(bus, CreateOptions(), new FakeTimeProvider());

        var action = () => aggregator.IngestAsync(new CombineYieldMeasurement(-1, 0, 5000)).AsTask();

        await action.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }
}
