using System;
using System.Linq;
using Aog.Core.Layers.Controllers;
using Aog.Core.Paths;
using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Aog.Core.Tests.Layers;

public sealed class LayerControllerDiagnosticsFeedTests
{
    [Fact]
    public void CreateDiagnostic_ShouldReturnNominalForHealthySnapshot()
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2025, 5, 5, 7, 0, 0, TimeSpan.Zero));
        var runtime = new LayerControllerRuntime(
            new[] { new LayerControllerDescriptor("controller-1", "layer-coverage", TimeSpan.FromMilliseconds(50), LayerAggregationStrategy.Average) },
            clock);

        runtime.RecordSample(
            "controller-1",
            new LayerControllerSample(
                clock.GetUtcNow(),
                new PlanarPoint(1, 1),
                engineeringValue: 18,
                normalizedValue: 0.75,
                quality: 0.9,
                rateUnavailable: false,
                areaSquareMeters: 1.5));

        clock.Advance(TimeSpan.FromMilliseconds(60));
        using var snapshot = runtime.CollectDueSnapshots(force: true).Single();

        var feed = new LayerControllerDiagnosticsFeed();
        var diagnostic = feed.CreateDiagnostic(snapshot, clock);

        diagnostic.Status.Should().Be(ControllerDiagnosticStatus.Nominal);
        diagnostic.ContainsFreshData.Should().BeTrue();
        diagnostic.Quality.Should().BeApproximately(0.9, 1e-6);
        diagnostic.Ratio.Should().BeApproximately(snapshot.Numerator / snapshot.Denominator, 1e-6);
        diagnostic.Metrics.Should().ContainKey("normalized");
        diagnostic.TimeSinceLastSample.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void CreateDiagnostic_ShouldFlagRateUnavailableAsFault()
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2025, 5, 5, 7, 5, 0, TimeSpan.Zero));
        var runtime = new LayerControllerRuntime(
            new[] { new LayerControllerDescriptor("controller-2", "layer-sections", TimeSpan.FromMilliseconds(50), LayerAggregationStrategy.Sum) },
            clock);

        runtime.RecordSample(
            "controller-2",
            new LayerControllerSample(
                clock.GetUtcNow(),
                new PlanarPoint(2, 1),
                engineeringValue: 12,
                normalizedValue: 0.5,
                quality: 0.4,
                rateUnavailable: true,
                areaSquareMeters: 0.5));

        clock.Advance(TimeSpan.FromMilliseconds(60));
        using var snapshot = runtime.CollectDueSnapshots(force: true).Single();

        var feed = new LayerControllerDiagnosticsFeed();
        var diagnostic = feed.CreateDiagnostic(snapshot, clock);

        diagnostic.Status.Should().Be(ControllerDiagnosticStatus.Fault);
        diagnostic.StatusReason.Should().Contain("Rate unavailable");
        diagnostic.RateUnavailable.Should().BeTrue();
    }

    [Fact]
    public void CreateDiagnostic_ShouldWarnWhenHoldFrameIsStale()
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2025, 5, 5, 7, 10, 0, TimeSpan.Zero));
        var runtime = new LayerControllerRuntime(
            new[] { new LayerControllerDescriptor("controller-3", "layer-rate", TimeSpan.FromMilliseconds(50), LayerAggregationStrategy.Average) },
            clock);

        runtime.RecordSample(
            "controller-3",
            new LayerControllerSample(
                clock.GetUtcNow(),
                new PlanarPoint(3, 0),
                engineeringValue: 20,
                normalizedValue: 0.6,
                quality: 0.7,
                rateUnavailable: false,
                areaSquareMeters: 0.8));

        clock.Advance(TimeSpan.FromMilliseconds(60));
        using (runtime.CollectDueSnapshots(force: true).Single())
        {
        }

        clock.Advance(TimeSpan.FromSeconds(5));
        using var holdSnapshot = runtime.CollectDueSnapshots(force: true).Single();

        var options = new LayerControllerDiagnosticsFeedOptions(
            WarningQualityThreshold: 0.8,
            FaultQualityThreshold: 0.3,
            StaleAfter: TimeSpan.FromSeconds(2));
        var feed = new LayerControllerDiagnosticsFeed(options);

        var diagnostic = feed.CreateDiagnostic(holdSnapshot, clock);

        diagnostic.ContainsFreshData.Should().BeFalse();
        diagnostic.Status.Should().Be(ControllerDiagnosticStatus.Warning);
        diagnostic.StatusReason.Should().Contain("No fresh samples");
        diagnostic.TimeSinceLastSample.Should().Be(TimeSpan.FromSeconds(5));
    }
}
