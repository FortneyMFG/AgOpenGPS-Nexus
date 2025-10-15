using System;
using System.Linq;
using Aog.Core.Layers.Controllers;
using Aog.Core.Paths;
using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Aog.Core.Tests.Layers;

public class LayerControllerRuntimeTests
{
    private static LayerControllerDescriptor CreateDescriptor(
        string controllerId,
        string layerId,
        LayerAggregationStrategy strategy,
        TimeSpan? cadence = null)
    {
        return new LayerControllerDescriptor(controllerId, layerId, cadence ?? TimeSpan.FromMilliseconds(100), strategy);
    }

    [Fact]
    public void RecordSample_ShouldThrowForUnknownController()
    {
        var runtime = new LayerControllerRuntime(new[] { CreateDescriptor("c1", "layer-1", LayerAggregationStrategy.Average) });
        var sample = new LayerControllerSample(
            DateTimeOffset.UtcNow,
            new PlanarPoint(0, 0),
            0,
            0,
            0,
            false,
            0);

        var act = () => runtime.RecordSample("missing", sample);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*missing*");
    }

    [Fact]
    public void CollectDueSnapshots_ShouldReturnWeightedAverage()
    {
        var clock = new FakeTimeProvider();
        var runtime = new LayerControllerRuntime(
            new[] { CreateDescriptor("controller-1", "layer-1", LayerAggregationStrategy.Average) },
            clock);

        runtime.RecordSample(
            "controller-1",
            CreateSample(clock.GetUtcNow(), engineering: 18, normalized: 0.3, quality: 0.7, area: 1.2, position: new PlanarPoint(1, 0)));
        runtime.RecordSample(
            "controller-1",
            CreateSample(clock.GetUtcNow() + TimeSpan.FromMilliseconds(20), engineering: 22, normalized: 0.8, quality: 0.9, area: 3.6, position: new PlanarPoint(2, 0)));

        clock.Advance(TimeSpan.FromMilliseconds(150));

        var snapshots = runtime.CollectDueSnapshots();

        snapshots.Should().HaveCount(1);
        using var snapshot = snapshots.Single();
        snapshot.ControllerId.Should().Be("controller-1");
        snapshot.LayerId.Should().Be("layer-1");
        snapshot.ContainsFreshData.Should().BeTrue();
        snapshot.EngineeringValue.Should().BeApproximately(21.0, 1e-6);
        snapshot.NormalizedValue.Should().BeApproximately(0.68, 1e-6);
        snapshot.Quality.Should().BeApproximately(0.85, 1e-6);
        snapshot.AreaSquareMeters.Should().BeApproximately(4.8, 1e-6);
        snapshot.SampleCount.Should().Be(2);
        snapshot.Positions.Should().ContainInOrder(new PlanarPoint(1, 0), new PlanarPoint(2, 0));
    }

    [Fact]
    public void CollectDueSnapshots_ShouldRespectSumAggregation()
    {
        var clock = new FakeTimeProvider();
        var runtime = new LayerControllerRuntime(
            new[] { CreateDescriptor("controller-1", "layer-1", LayerAggregationStrategy.Sum) },
            clock);

        runtime.RecordSample(
            "controller-1",
            new LayerControllerSample(
                clock.GetUtcNow(),
                new PlanarPoint(0, 0),
                engineeringValue: 10,
                normalizedValue: 0.5,
                quality: 0.7,
                rateUnavailable: false,
                areaSquareMeters: 0.5,
                engineeringContribution: 1.25));

        runtime.RecordSample(
            "controller-1",
            new LayerControllerSample(
                clock.GetUtcNow() + TimeSpan.FromMilliseconds(30),
                new PlanarPoint(1, 0),
                engineeringValue: 11,
                normalizedValue: 0.6,
                quality: 0.6,
                rateUnavailable: true,
                areaSquareMeters: 0.75,
                engineeringContribution: 1.75));

        clock.Advance(TimeSpan.FromMilliseconds(200));

        using var snapshot = runtime.CollectDueSnapshots().Single();
        snapshot.ContainsFreshData.Should().BeTrue();
        snapshot.EngineeringValue.Should().BeApproximately(3.0, 1e-6);
        snapshot.RateUnavailable.Should().BeTrue();
        snapshot.Numerator.Should().BeApproximately(0.5 * 0.5 + 0.6 * 0.75, 1e-6);
        snapshot.Denominator.Should().BeApproximately(0.5 + 0.75, 1e-6);
        snapshot.MinimumValue.Should().BeApproximately(10, 1e-6);
        snapshot.MaximumValue.Should().BeApproximately(11, 1e-6);
    }

    [Fact]
    public void CollectDueSnapshots_ShouldEmitHoldFrameWhenRequested()
    {
        var clock = new FakeTimeProvider();
        var runtime = new LayerControllerRuntime(
            new[] { CreateDescriptor("controller-1", "layer-1", LayerAggregationStrategy.Hold) },
            clock);

        runtime.RecordSample(
            "controller-1",
            CreateSample(clock.GetUtcNow(), engineering: 15, normalized: 0.4, quality: 0.8, area: 0.3, rateUnavailable: false));

        clock.Advance(TimeSpan.FromMilliseconds(120));
        using (runtime.CollectDueSnapshots().Single())
        {
        }

        clock.Advance(TimeSpan.FromMilliseconds(120));
        using var holdSnapshot = runtime.CollectDueSnapshots(emitHoldFrames: true).Single();

        holdSnapshot.ContainsFreshData.Should().BeFalse();
        holdSnapshot.EngineeringValue.Should().BeApproximately(15, 1e-6);
        holdSnapshot.NormalizedValue.Should().BeApproximately(0.4, 1e-6);
        holdSnapshot.Quality.Should().BeApproximately(0.8, 1e-6);
        holdSnapshot.SampleCount.Should().Be(0);
        holdSnapshot.FirstSampleTimestamp.Should().BeNull();
        holdSnapshot.LastSampleTimestamp.Should().BeNull();
        holdSnapshot.Positions.Should().ContainSingle().Which.Should().Be(new PlanarPoint(0, 0));
    }

    [Fact]
    public void CollectDueSnapshots_ShouldSkipHoldFrameWhenNotRequested()
    {
        var clock = new FakeTimeProvider();
        var runtime = new LayerControllerRuntime(
            new[] { CreateDescriptor("controller-1", "layer-1", LayerAggregationStrategy.Average) },
            clock);

        runtime.RecordSample(
            "controller-1",
            CreateSample(clock.GetUtcNow(), engineering: 20, normalized: 0.5, quality: 0.8, area: 0.8));

        clock.Advance(TimeSpan.FromMilliseconds(120));
        using (runtime.CollectDueSnapshots().Single())
        {
        }

        clock.Advance(TimeSpan.FromMilliseconds(120));
        var snapshots = runtime.CollectDueSnapshots(emitHoldFrames: false);

        snapshots.Should().BeEmpty();
    }

    [Fact]
    public void CollectDueSnapshots_ShouldForceEmission()
    {
        var clock = new FakeTimeProvider();
        var runtime = new LayerControllerRuntime(
            new[] { CreateDescriptor("controller-1", "layer-1", LayerAggregationStrategy.Average, cadence: TimeSpan.FromSeconds(10)) },
            clock);

        runtime.RecordSample(
            "controller-1",
            CreateSample(clock.GetUtcNow(), engineering: 12, normalized: 0.6, quality: 0.9, area: 0.5));

        using var snapshot = runtime.CollectDueSnapshots(force: true).Single();
        snapshot.ContainsFreshData.Should().BeTrue();
    }

    [Fact]
    public void ComputeAreaSlice_ShouldRespectCoverageFactors()
    {
        var area = LayerControllerSample.ComputeAreaSlice(
            groundSpeedMetersPerSecond: 3.5,
            duration: TimeSpan.FromSeconds(0.2),
            sectionWidthMeters: 0.75,
            coverageFactor: 0.8);

        area.Should().BeApproximately(0.42, 1e-6);
    }

    private static LayerControllerSample CreateSample(
        DateTimeOffset timestamp,
        double engineering,
        double normalized,
        double quality,
        double area,
        bool rateUnavailable = false,
        PlanarPoint? position = null)
    {
        return new LayerControllerSample(
            timestamp,
            position ?? new PlanarPoint(0, 0),
            engineeringValue: engineering,
            normalizedValue: normalized,
            quality: quality,
            rateUnavailable: rateUnavailable,
            areaSquareMeters: area);
    }
}
