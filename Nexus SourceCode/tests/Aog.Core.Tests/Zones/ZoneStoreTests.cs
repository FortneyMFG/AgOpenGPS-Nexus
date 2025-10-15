using System;
using System.Linq;
using Aog.Core.Zones;
using Xunit;

namespace Aog.Core.Tests.Zones;

public sealed class ZoneStoreTests
{
    [Fact]
    public void MountZones_ReplacesExistingAndIndexes()
    {
        var store = new ZoneStore(crsEpsg: 4326);

        var first = CreateSquareZone("zone:boundary", 0, 0, 10, priority: 25);
        var second = CreateSquareZone("zone:keepout", 20, 20, 5, priority: 80);

        var delta = store.MountZones(new[] { first, second });

        Assert.Equal<ulong>(1, delta.Version);
        Assert.Equal(2, delta.Upserts.Count);
        Assert.Empty(delta.DeletedZoneIds);

        var listed = store.ListZones();
        Assert.Equal(2, listed.Count);
        Assert.Equal("zone:keepout", listed[0].ZoneId); // Highest priority first.
        Assert.Equal("zone:boundary", listed[1].ZoneId);

        var matches = store.GetZonesInBounds(new ZoneBoundingBox(19, 19, 26, 26));
        Assert.Single(matches);
        Assert.Equal("zone:keepout", matches[0].ZoneId);
    }

    [Fact]
    public void Upsert_AddsZoneAndUpdatesVersion()
    {
        var store = new ZoneStore(4326);
        store.MountZones(Array.Empty<ZoneDefinition>());

        var zone = CreateSquareZone("zone:new", 50, 50, 3, priority: 60);

        var delta = store.Upsert(zone);

        Assert.Equal<ulong>(2, delta.Version); // Mount increments once, upsert increments again.
        Assert.Single(delta.Upserts);
        Assert.Equal("zone:new", delta.Upserts[0].ZoneId);
        Assert.Empty(delta.DeletedZoneIds);

        Assert.True(store.TryGetZone("zone:new", out var fetched));
        Assert.Same(zone, fetched);
    }

    [Fact]
    public void TryRemove_RemovesZoneAndUpdatesVersion()
    {
        var store = new ZoneStore(4326);
        var zone = CreateSquareZone("zone:remove", -5, -5, 6, priority: 10);
        store.Upsert(zone);

        var removed = store.TryRemove("zone:remove", out var delta);

        Assert.True(removed);
        Assert.NotNull(delta);
        Assert.Equal(store.Version, delta!.Version);
        Assert.Single(delta.DeletedZoneIds);
        Assert.Equal("zone:remove", delta.DeletedZoneIds[0]);
        Assert.False(store.TryGetZone("zone:remove", out _));
    }

    [Fact]
    public void GetZonesInBounds_RespectsIncludeDisabled()
    {
        var store = new ZoneStore(4326);
        store.MountZones(new[]
        {
            CreateSquareZone("zone:active", 0, 0, 4, priority: 50, enabled: true),
            CreateSquareZone("zone:disabled", 0, 0, 4, priority: 10, enabled: false)
        });

        var bounds = new ZoneBoundingBox(-1, -1, 5, 5);
        var activeOnly = store.GetZonesInBounds(bounds, includeDisabled: false);
        Assert.Single(activeOnly);
        Assert.Equal("zone:active", activeOnly[0].ZoneId);

        var includingDisabled = store.GetZonesInBounds(bounds, includeDisabled: true);
        Assert.Equal(2, includingDisabled.Count);
        Assert.Equal(new[] { "zone:active", "zone:disabled" }, includingDisabled.Select(z => z.ZoneId).ToArray());
    }

    [Fact]
    public void GetZonesContaining_ReturnsOrderedMatches()
    {
        var store = new ZoneStore(4326);
        store.MountZones(new[]
        {
            CreateSquareZone("zone:boundary", 0, 0, 4, priority: 10, enabled: true),
            CreateSquareZone("zone:keepout", 0, 0, 4, priority: 80, enabled: true),
            CreateSquareZone("zone:disabled", 0, 0, 4, priority: 50, enabled: false)
        });

        var all = store.GetZonesContaining(1, 1, includeDisabled: true);
        Assert.Equal(3, all.Count);
        Assert.Equal(new[] { "zone:keepout", "zone:disabled", "zone:boundary" }, all.Select(z => z.ZoneId).ToArray());

        var activeOnly = store.GetZonesContaining(1, 1, includeDisabled: false);
        Assert.Equal(2, activeOnly.Count);
        Assert.Equal(new[] { "zone:keepout", "zone:boundary" }, activeOnly.Select(z => z.ZoneId).ToArray());
    }

    [Fact]
    public void MountZones_ThrowsWhenDuplicateIdentifiersDetected()
    {
        var store = new ZoneStore(4326);
        var zone = CreateSquareZone("zone:dup", 0, 0, 2, priority: 5);

        var exception = Assert.Throws<ArgumentException>(() => store.MountZones(new[] { zone, zone }));
        Assert.Contains("Duplicate zone identifier", exception.Message);
    }

    private static ZoneDefinition CreateSquareZone(
        string zoneId,
        double originX,
        double originY,
        double size,
        uint priority,
        bool enabled = true)
    {
        var ring = new[]
        {
            new ZoneCoordinate(originX, originY),
            new ZoneCoordinate(originX + size, originY),
            new ZoneCoordinate(originX + size, originY + size),
            new ZoneCoordinate(originX, originY + size),
            new ZoneCoordinate(originX, originY)
        };

        var polygon = new ZonePolygon(new ZoneLinearRing(ring));
        var buffers = new ZoneBuffers(1, 1);
        return new ZoneDefinition(zoneId, ZoneType.Boundary, zoneId, priority, enabled, polygon, buffers);
    }
}
