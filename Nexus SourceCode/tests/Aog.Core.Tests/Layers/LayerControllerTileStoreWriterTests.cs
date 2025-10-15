using System;
using System.Collections.Generic;
using System.Linq;
using Aog.Core.Layers.Controllers;
using Aog.Core.Paths;
using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Aog.Core.Tests.Layers;

public sealed class LayerControllerTileStoreWriterTests
{
    [Fact]
    public void WriteSnapshot_ShouldPersistTileWithDiagnostics()
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2025, 5, 5, 7, 20, 0, TimeSpan.Zero));
        var runtime = new LayerControllerRuntime(
            new[] { new LayerControllerDescriptor("controller-alpha", "layer-guidance", TimeSpan.FromMilliseconds(50), LayerAggregationStrategy.Average) },
            clock);

        runtime.RecordSample(
            "controller-alpha",
            new LayerControllerSample(
                clock.GetUtcNow(),
                new PlanarPoint(0, 0),
                engineeringValue: 24,
                normalizedValue: 0.8,
                quality: 0.95,
                rateUnavailable: false,
                areaSquareMeters: 1.0));

        clock.Advance(TimeSpan.FromMilliseconds(60));
        using var snapshot = runtime.CollectDueSnapshots(force: true).Single();

        var sink = new InMemoryTileSink();
        var writer = new LayerControllerTileStoreWriter(sink);

        var tile = writer.WriteSnapshot(snapshot, clock);

        sink.Tiles.Should().ContainSingle().Which.Should().BeSameAs(tile);
        tile.TileId.Should().StartWith("tile:controller-alpha:");
        tile.ControllerId.Should().Be("controller-alpha");
        tile.LayerId.Should().Be("layer-guidance");
        tile.Status.Should().Be(ControllerDiagnosticStatus.Nominal);
        tile.Metrics.Should().Contain(new KeyValuePair<string, double>("normalized", tile.NormalizedValue));

        snapshot.Dispose();
        tile.Positions.Should().ContainSingle().Which.Should().Be(new PlanarPoint(0, 0));
    }

    [Fact]
    public void WriteSnapshot_ShouldIncrementSequenceForDuplicateTimestamps()
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2025, 5, 5, 7, 25, 0, TimeSpan.Zero));
        var runtime = new LayerControllerRuntime(
            new[] { new LayerControllerDescriptor("controller-beta", "layer-sections", TimeSpan.Zero, LayerAggregationStrategy.Average) },
            clock);

        runtime.RecordSample(
            "controller-beta",
            new LayerControllerSample(
                clock.GetUtcNow(),
                new PlanarPoint(1, 1),
                engineeringValue: 10,
                normalizedValue: 0.4,
                quality: 0.7,
                rateUnavailable: false,
                areaSquareMeters: 0.5));

        using var firstSnapshot = runtime.CollectDueSnapshots(force: true).Single();
        using var secondSnapshot = runtime.CollectDueSnapshots(force: true).Single();

        var sink = new InMemoryTileSink();
        var writer = new LayerControllerTileStoreWriter(sink);

        var firstTile = writer.WriteSnapshot(firstSnapshot, clock);
        var secondTile = writer.WriteSnapshot(secondSnapshot, clock);

        firstTile.TileId.Should().NotBe(secondTile.TileId);
        firstTile.TileId.Should().EndWith("-00");
        secondTile.TileId.Should().EndWith("-01");
    }

    private sealed class InMemoryTileSink : ILayerControllerTileSink
    {
        private readonly List<LayerControllerTile> _tiles = new();

        public IReadOnlyList<LayerControllerTile> Tiles => _tiles;

        public void Append(LayerControllerTile tile)
        {
            _tiles.Add(tile);
        }
    }
}
