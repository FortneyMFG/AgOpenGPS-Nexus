using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aog.Core.Eventing;
using Aog.Core.Paths;
using Aog.Core.Zones;
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

    [Fact]
    public void UpdateZones_StoresSnapshotsOrderedByPriority()
    {
        var store = new FieldStateStore();
        store.MountFields(new[]
        {
            new FieldGeometry("field-1", CreateSquare(120))
        });

        var zones = new[]
        {
            CreateZoneDefinition(
                zoneId: "zone-keepout",
                type: ZoneType.KeepOut,
                priority: 40,
                originX: 10,
                originY: 10,
                size: 20),
            CreateZoneDefinition(
                zoneId: "zone-boundary",
                type: ZoneType.Boundary,
                priority: 100,
                originX: 0,
                originY: 0,
                size: 120),
            CreateZoneDefinition(
                zoneId: "zone-workdisabled",
                type: ZoneType.WorkDisabled,
                priority: 60,
                originX: 30,
                originY: 30,
                size: 15,
                enabled: false)
        };

        store.UpdateZones("field-1", zones);

        var snapshots = store.GetZoneSnapshots("field-1");
        Assert.Equal(3, snapshots.Count);

        Assert.Collection(
            snapshots,
            first =>
            {
                Assert.Equal("zone-boundary", first.ZoneId);
                Assert.Equal(ZoneType.Boundary, first.Type);
                Assert.Equal(14400, first.AreaSquareMeters, 3);
                Assert.True(first.Enabled);
            },
            second =>
            {
                Assert.Equal("zone-workdisabled", second.ZoneId);
                Assert.Equal(ZoneType.WorkDisabled, second.Type);
                Assert.False(second.Enabled);
            },
            third =>
            {
                Assert.Equal("zone-keepout", third.ZoneId);
                Assert.Equal(ZoneType.KeepOut, third.Type);
                Assert.Equal(400, third.AreaSquareMeters, 3);
            });
    }

    [Fact]
    public void UpdateZones_ComputesAreaWithHoles()
    {
        var store = new FieldStateStore();
        store.MountFields(new[]
        {
            new FieldGeometry("field-1", CreateSquare(200))
        });

        var exterior = new ZoneLinearRing(new[]
        {
            new ZoneCoordinate(0, 0),
            new ZoneCoordinate(0, 80),
            new ZoneCoordinate(80, 80),
            new ZoneCoordinate(80, 0),
            new ZoneCoordinate(0, 0)
        });

        var hole = new ZoneLinearRing(new[]
        {
            new ZoneCoordinate(20, 20),
            new ZoneCoordinate(20, 40),
            new ZoneCoordinate(40, 40),
            new ZoneCoordinate(40, 20),
            new ZoneCoordinate(20, 20)
        });

        var polygon = new ZonePolygon(exterior, new[] { hole });
        var definition = new ZoneDefinition(
            "zone-headland",
            ZoneType.Headland,
            "Headland",
            priority: 50,
            enabled: true,
            polygon,
            new ZoneBuffers(1, 1));

        store.UpdateZones("field-1", new[] { definition });

        var snapshot = Assert.Single(store.GetZoneSnapshots("field-1"));
        Assert.Equal(ZoneType.Headland, snapshot.Type);
        Assert.Equal(80 * 80 - 20 * 20, snapshot.AreaSquareMeters, 3);
        Assert.Single(snapshot.Geometry.Holes);
        Assert.Equal(5, snapshot.Geometry.OuterBoundary.Count);
    }

    [Fact]
    public void UpdateZones_ThrowsWhenDuplicateIdentifiers()
    {
        var store = new FieldStateStore();
        store.MountFields(new[]
        {
            new FieldGeometry("field-1", CreateSquare(100))
        });

        var zone = CreateZoneDefinition(
            zoneId: "zone-duplicate",
            type: ZoneType.KeepOut,
            priority: 10,
            originX: 0,
            originY: 0,
            size: 10);

        Assert.Throws<ArgumentException>(() => store.UpdateZones("field-1", new[] { zone, zone }));
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

    private static ZoneDefinition CreateZoneDefinition(
        string zoneId,
        ZoneType type,
        uint priority,
        double originX,
        double originY,
        double size,
        bool enabled = true)
    {
        var exterior = new ZoneLinearRing(new[]
        {
            new ZoneCoordinate(originX, originY),
            new ZoneCoordinate(originX, originY + size),
            new ZoneCoordinate(originX + size, originY + size),
            new ZoneCoordinate(originX + size, originY),
            new ZoneCoordinate(originX, originY)
        });

        var polygon = new ZonePolygon(exterior);
        return new ZoneDefinition(
            zoneId,
            type,
            $"{type}:{zoneId}",
            priority,
            enabled,
            polygon,
            new ZoneBuffers(1, 2));
    }
}
