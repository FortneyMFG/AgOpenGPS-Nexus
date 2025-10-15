using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aog.Core.Eventing;
using Aog.Core.Paths;
using Xunit;

namespace Aog.Plugins.Mapping.Tests;

public sealed class FieldStateStoreTests
{
    [Fact]
    public void CoverageAccumulator_ComputesAreaAndPercent()
    {
        var store = new FieldStateStore();
        store.MountFields(new[]
        {
            new FieldGeometry("field-1", CreateSquare(100))
        });

        store.BeginCoveragePatch("field-1", new PlanarPoint(0, 0), new PlanarPoint(0, 10));
        store.AddCoverageSample("field-1", new PlanarPoint(10, 0), new PlanarPoint(10, 10));
        store.EndCoveragePatch("field-1");

        var snapshot = store.GetCoverageSnapshot("field-1");
        Assert.Equal(100, snapshot.TotalAreaSquareMeters, 3);
        Assert.Equal(100, snapshot.UserAreaSquareMeters, 3);
        Assert.Equal(1, snapshot.CoveragePercent, 3);
        Assert.Equal(1, snapshot.PatchCount);
    }

    [Fact]
    public void GetAbLinePasses_ReturnsNeighbourhood()
    {
        var store = new FieldStateStore();
        store.MountFields(new[]
        {
            new FieldGeometry("field-1", CreateSquare(120))
        });

        store.UpdateAbLines("field-1", new[]
        {
            new AbLineDefinition(
                id: "main",
                pointA: new PlanarPoint(0, 0),
                pointB: new PlanarPoint(0, 100),
                toolWidthMeters: 12,
                overlapMeters: 0.5)
        });

        var passes = store.GetAbLinePasses(
            fieldId: "field-1",
            abLineId: "main",
            pivot: new PlanarPoint(1, 10),
            toolOffset: 0,
            headingSameWay: true,
            neighborCount: 1);

        Assert.Collection(
            passes,
            first =>
            {
                Assert.Equal(-1, first.PassIndex);
                Assert.True(first.SignedDistanceMeters > 12);
            },
            centre =>
            {
                Assert.Equal(0, centre.PassIndex);
                Assert.Equal(1, centre.SignedDistanceMeters, 3);
                Assert.Equal(0, centre.HeadingRadians, 3);
                Assert.Equal(11.5, centre.LaneSpacingMeters, 3);
            },
            last =>
            {
                Assert.Equal(1, last.PassIndex);
                Assert.True(last.SignedDistanceMeters < -10);
            });
    }

    [Fact]
    public async Task CoverageFeedPublisher_PublishesWhenThresholdExceeded()
    {
        var store = new FieldStateStore();
        store.MountFields(new[]
        {
            new FieldGeometry("field-1", CreateSquare(100))
        });

        var bus = new InMemoryEventBus();
        var published = new List<FieldCoverageSnapshot>();
        using var subscription = bus.Subscribe<FieldCoverageSnapshot>((snapshot, _) =>
        {
            published.Add(snapshot);
            return ValueTask.CompletedTask;
        });

        var publisher = new CoverageFeedPublisher(store, bus, minimumAreaDelta: 50);

        // Initial coverage patch (10 m x 10 m).
        store.BeginCoveragePatch("field-1", new PlanarPoint(0, 0), new PlanarPoint(0, 10));
        store.AddCoverageSample("field-1", new PlanarPoint(10, 0), new PlanarPoint(10, 10));
        store.EndCoveragePatch("field-1");
        await publisher.PublishAsync("field-1");

        // Additional patch adds 20 m^2 (below threshold).
        store.BeginCoveragePatch("field-1", new PlanarPoint(10, 0), new PlanarPoint(10, 10));
        store.AddCoverageSample("field-1", new PlanarPoint(12, 0), new PlanarPoint(12, 10));
        store.EndCoveragePatch("field-1");
        await publisher.PublishAsync("field-1");

        // Next patch brings the cumulative delta to 60 m^2 (above threshold).
        store.BeginCoveragePatch("field-1", new PlanarPoint(12, 0), new PlanarPoint(12, 10));
        store.AddCoverageSample("field-1", new PlanarPoint(16, 0), new PlanarPoint(16, 10));
        store.EndCoveragePatch("field-1");
        await publisher.PublishAsync("field-1");

        Assert.Equal(2, published.Count);
        Assert.Equal(100, published[0].TotalAreaSquareMeters, 3);
        Assert.Equal(160, published[1].TotalAreaSquareMeters, 3);
    }

    private static IReadOnlyList<PlanarPoint> CreateSquare(double edgeLength)
    {
        return new[]
        {
            new PlanarPoint(0, 0),
            new PlanarPoint(edgeLength, 0),
            new PlanarPoint(edgeLength, edgeLength),
            new PlanarPoint(0, edgeLength)
        };
    }
}
