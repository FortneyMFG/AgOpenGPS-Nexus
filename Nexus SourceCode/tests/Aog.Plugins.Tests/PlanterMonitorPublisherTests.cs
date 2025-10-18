using System;
using System.Threading;
using System.Threading.Tasks;
using Aog.Core.Eventing;
using Aog.Core.V1;
using Aog.Plugins.PlanterMonitor;
using Microsoft.Extensions.Time.Testing;
using FluentAssertions;
using Xunit;

namespace Aog.Plugins.Tests;

public sealed class PlanterMonitorPublisherTests
{
    private static PlanterMonitorOptions CreateOptions() => new()
    {
        RowCount = 16,
        SkipThreshold = 0.25,
        DoubleThreshold = 0.25,
        Frame = "vehicle",
        Source = "sim"
    };

    [Fact]
    public async Task PublishAsync_SingleMeasurementPublishesStatus()
    {
        var bus = new InMemoryEventBus();
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.Zero));
        var publisher = new PlanterMonitorPublisher(bus, CreateOptions(), timeProvider);

        PlanterRowStatus? captured = null;
        using var subscription = bus.Subscribe<PlanterRowStatus>((message, _) =>
        {
            captured = message;
            return ValueTask.CompletedTask;
        });

        var measurement = new RowPopulationMeasurement(3, 10.0, 10.0);
        await publisher.PublishAsync(measurement);

        captured.Should().NotBeNull();
        captured!.RowIndex.Should().Be((uint)measurement.RowIndex);
        captured.TargetPopulationPerMeter.Should().Be(measurement.TargetPopulationPerMeter);
        captured.ActualPopulationPerMeter.Should().Be(measurement.ActualPopulationPerMeter);
        captured.SkipRate.Should().Be(0);
        captured.DoubleRate.Should().Be(0);
        captured.Quality.Should().Be(PlanterRowQuality.Ok);
        captured.Header.Should().NotBeNull();
        captured.Header.Sequence.Should().Be(1);
        captured.Header.Source.Should().Be("sim");
        captured.Header.Frame.Should().Be("vehicle");
        captured.Header.Timestamp.ToDateTimeOffset().Should().Be(timeProvider.GetUtcNow());
    }

    [Fact]
    public async Task PublishAsync_ShortfallFlagsSkip()
    {
        var bus = new InMemoryEventBus();
        var publisher = new PlanterMonitorPublisher(bus, CreateOptions(), new FakeTimeProvider());

        PlanterRowStatus? captured = null;
        using var subscription = bus.Subscribe<PlanterRowStatus>((message, _) =>
        {
            captured = message;
            return ValueTask.CompletedTask;
        });

        await publisher.PublishAsync(new RowPopulationMeasurement(0, 12.0, 6.0));

        captured.Should().NotBeNull();
        captured!.Quality.Should().Be(PlanterRowQuality.Skip);
        captured.SkipRate.Should().Be(1);
        captured.DoubleRate.Should().Be(0);
    }

    [Fact]
    public async Task PublishAsync_ExcessFlagsDouble()
    {
        var bus = new InMemoryEventBus();
        var publisher = new PlanterMonitorPublisher(bus, CreateOptions(), new FakeTimeProvider());

        PlanterRowStatus? captured = null;
        using var subscription = bus.Subscribe<PlanterRowStatus>((message, _) =>
        {
            captured = message;
            return ValueTask.CompletedTask;
        });

        await publisher.PublishAsync(new RowPopulationMeasurement(1, 8.0, 10.4));

        captured.Should().NotBeNull();
        captured!.Quality.Should().Be(PlanterRowQuality.Double);
        captured.DoubleRate.Should().BeApproximately(1.0, 1e-6);
        captured.SkipRate.Should().Be(0);
    }

    [Fact]
    public async Task PublishAsync_RowIndexBeyondRange_Throws()
    {
        var bus = new InMemoryEventBus();
        var publisher = new PlanterMonitorPublisher(bus, CreateOptions(), new FakeTimeProvider());

        Func<Task> act = () => publisher.PublishAsync(new RowPopulationMeasurement(32, 10.0, 10.0)).AsTask();

        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task PublishAsync_ZeroTargetMarksUnknown()
    {
        var bus = new InMemoryEventBus();
        var publisher = new PlanterMonitorPublisher(bus, CreateOptions(), new FakeTimeProvider());

        PlanterRowStatus? captured = null;
        using var subscription = bus.Subscribe<PlanterRowStatus>((message, _) =>
        {
            captured = message;
            return ValueTask.CompletedTask;
        });

        await publisher.PublishAsync(new RowPopulationMeasurement(2, 0.0, 0.0));

        captured.Should().NotBeNull();
        captured!.Quality.Should().Be(PlanterRowQuality.Unknown);
        captured.SkipRate.Should().Be(0);
        captured.DoubleRate.Should().Be(0);
    }
}
