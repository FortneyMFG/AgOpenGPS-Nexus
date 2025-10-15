using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aog.Core.Layers.Controllers;
using Aog.Core.Paths;
using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Aog.Core.Tests.Layers;

public sealed class LayerControllerTileWriterTests
{
    [Fact]
    public async Task WriteAsync_ShouldTranslateSnapshotsToTileRequests()
    {
        var clock = new FakeTimeProvider();
        var descriptors = new[]
        {
            new LayerControllerDescriptor("controller-1", "layer-coverage", TimeSpan.FromMilliseconds(50), LayerAggregationStrategy.Average)
        };

        var runtime = new LayerControllerRuntime(descriptors, clock);
        var tileStore = new RecordingTileStore();
        var writer = new LayerControllerTileWriter(tileStore);

        runtime.RecordSample(
            "controller-1",
            new LayerControllerSample(
                clock.GetUtcNow(),
                new PlanarPoint(0, 0),
                engineeringValue: 15,
                normalizedValue: 0.5,
                quality: 0.75,
                rateUnavailable: false,
                areaSquareMeters: 1.5));

        clock.Advance(TimeSpan.FromMilliseconds(60));
        using (var snapshot = runtime.CollectDueSnapshots().Single())
        {
            await writer.WriteAsync(snapshot);
        }

        tileStore.Requests.Should().ContainSingle();
        var request = tileStore.Requests[0];
        request.EngineeringValue.Should().BeApproximately(15, 1e-6);
        request.NormalizedValue.Should().BeApproximately(0.5, 1e-6);
        request.Weight.Should().BeApproximately(1.5, 1e-6);
        request.Quality.Should().BeApproximately(0.75, 1e-6);
        request.ContainsFreshData.Should().BeTrue();

        clock.Advance(TimeSpan.FromMilliseconds(60));
        using (var holdSnapshot = runtime.CollectDueSnapshots(emitHoldFrames: true).Single())
        {
            await writer.WriteAsync(holdSnapshot);
        }

        tileStore.Requests.Should().HaveCount(2);
        var holdRequest = tileStore.Requests[1];
        holdRequest.ContainsFreshData.Should().BeFalse();
        holdRequest.Weight.Should().Be(0);
        holdRequest.EngineeringValue.Should().BeApproximately(15, 1e-6);
    }

    private sealed class RecordingTileStore : ILayerTileStore
    {
        public List<LayerTileWriteRequest> Requests { get; } = new();

        public ValueTask WriteAsync(LayerTileWriteRequest request, System.Threading.CancellationToken cancellationToken = default)
        {
            Requests.Add(request);
            return ValueTask.CompletedTask;
        }
    }
}
