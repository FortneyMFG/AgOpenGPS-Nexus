using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aog.Core.Eventing;
using Aog.Core.Layers.Controllers;
using Aog.Core.Paths;
using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Aog.Core.Tests.Layers;

public sealed class LayerControllerDiagnosticsPublisherTests
{
    [Fact]
    public async Task PublishSnapshotsAsync_ShouldEmitDiagnosticsAndTileWrites()
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2025, 5, 5, 7, 0, 0, TimeSpan.Zero));
        var descriptors = new[]
        {
            new LayerControllerDescriptor("controller-1", "layer-rate", TimeSpan.FromMilliseconds(100), LayerAggregationStrategy.Average)
        };

        var runtime = new LayerControllerRuntime(descriptors, clock);
        var eventBus = new InMemoryEventBus();
        var tileStore = new RecordingTileStore();
        var tileWriter = new LayerControllerTileWriter(tileStore);
        var publisher = new LayerControllerDiagnosticsPublisher(runtime, eventBus, tileWriter);

        var diagnostics = new List<LayerControllerDiagnosticEvent>();
        using var subscription = eventBus.Subscribe<LayerControllerDiagnosticEvent>((evt, token) =>
        {
            diagnostics.Add(evt);
            return ValueTask.CompletedTask;
        });

        runtime.RecordSample(
            "controller-1",
            new LayerControllerSample(
                clock.GetUtcNow(),
                new PlanarPoint(0, 0),
                engineeringValue: 18,
                normalizedValue: 0.6,
                quality: 0.8,
                rateUnavailable: false,
                areaSquareMeters: 1.2));

        runtime.RecordSample(
            "controller-1",
            new LayerControllerSample(
                clock.GetUtcNow() + TimeSpan.FromMilliseconds(30),
                new PlanarPoint(1, 0),
                engineeringValue: 22,
                normalizedValue: 0.9,
                quality: 0.9,
                rateUnavailable: true,
                areaSquareMeters: 3.6));

        clock.Advance(TimeSpan.FromMilliseconds(150));

        await publisher.PublishSnapshotsAsync();

        diagnostics.Should().ContainSingle();
        var diagnostic = diagnostics.Single();
        diagnostic.ControllerId.Should().Be("controller-1");
        diagnostic.LayerId.Should().Be("layer-rate");
        diagnostic.ContainsFreshData.Should().BeTrue();
        diagnostic.EngineeringValue.Should().BeApproximately(21.0, 1e-6);
        diagnostic.NormalizedValue.Should().BeApproximately(0.84, 1e-6);
        diagnostic.RateUnavailable.Should().BeTrue();
        diagnostic.AreaSquareMeters.Should().BeApproximately(4.8, 1e-6);
        diagnostic.SampleCount.Should().Be(2);
        diagnostic.MinimumValue.Should().BeApproximately(18, 1e-6);
        diagnostic.MaximumValue.Should().BeApproximately(22, 1e-6);

        tileStore.Requests.Should().ContainSingle();
        var request = tileStore.Requests.Single();
        request.ControllerId.Should().Be("controller-1");
        request.LayerId.Should().Be("layer-rate");
        request.EngineeringValue.Should().BeApproximately(21.0, 1e-6);
        request.NormalizedValue.Should().BeApproximately(0.84, 1e-6);
        request.Weight.Should().BeApproximately(4.8, 1e-6);
        request.RateUnavailable.Should().BeTrue();
        request.ContainsFreshData.Should().BeTrue();
    }

    [Fact]
    public async Task PublishSnapshotsAsync_ShouldRespectCancellationTokens()
    {
        var clock = new FakeTimeProvider();
        var descriptors = new[]
        {
            new LayerControllerDescriptor("controller-1", "layer-quality", TimeSpan.FromMilliseconds(50), LayerAggregationStrategy.Average)
        };

        var runtime = new LayerControllerRuntime(descriptors, clock);
        var eventBus = new RecordingEventBus();
        var tileStore = new RecordingTileStore();
        var tileWriter = new LayerControllerTileWriter(tileStore);
        var publisher = new LayerControllerDiagnosticsPublisher(runtime, eventBus, tileWriter);

        runtime.RecordSample(
            "controller-1",
            new LayerControllerSample(
                clock.GetUtcNow(),
                new PlanarPoint(0, 0),
                engineeringValue: 12,
                normalizedValue: 0.5,
                quality: 0.7,
                rateUnavailable: false,
                areaSquareMeters: 1.0));

        clock.Advance(TimeSpan.FromMilliseconds(60));

        using var cts = new CancellationTokenSource();
        await publisher.PublishSnapshotsAsync(cancellationToken: cts.Token);

        eventBus.TokenCaptured.Should().BeTrue();
        eventBus.LastToken.Should().Be(cts.Token);
        tileStore.LastToken.Should().Be(cts.Token);
    }

    private sealed class RecordingTileStore : ILayerTileStore
    {
        public List<LayerTileWriteRequest> Requests { get; } = new();

        public CancellationToken LastToken { get; private set; }

        public ValueTask WriteAsync(LayerTileWriteRequest request, CancellationToken cancellationToken = default)
        {
            Requests.Add(request);
            LastToken = cancellationToken;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class RecordingEventBus : IEventBus
    {
        public CancellationToken LastToken { get; private set; }
        public bool TokenCaptured { get; private set; }

        public IDisposable Subscribe<TEvent>(Func<TEvent, CancellationToken, ValueTask> handler)
        {
            return new DummySubscription();
        }

        public ValueTask PublishAsync<TEvent>(TEvent message, CancellationToken cancellationToken = default)
        {
            TokenCaptured = true;
            LastToken = cancellationToken;
            return ValueTask.CompletedTask;
        }

        private sealed class DummySubscription : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }
}
