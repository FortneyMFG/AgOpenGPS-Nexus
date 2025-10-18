using System.Threading.Tasks;
using Aog.Core.Eventing;
using Aog.Core.V1;
using Aog.Core.Zones;
using FluentAssertions;
using Xunit;

namespace Aog.Core.Tests.Zones;

public sealed class PoseZoneMaskPropagatorTests
{
    [Fact]
    public async Task PublishPose_ShouldPopulateMaskForOverlappingZones()
    {
        var store = new ZoneStore(4326);
        store.MountZones(new[]
        {
            CreateZone("zone:boundary", global::Aog.Core.Zones.ZoneType.Boundary, priority: 10),
            CreateZone("zone:headland", global::Aog.Core.Zones.ZoneType.Headland, priority: 30),
            CreateZone("zone:work", global::Aog.Core.Zones.ZoneType.WorkDisabled, priority: 20),
            CreateZone("zone:keepout", global::Aog.Core.Zones.ZoneType.KeepOut, priority: 40),
        });

        var bus = new InMemoryEventBus();
        using var propagator = new PoseZoneMaskPropagator(store, bus);

        var pose = new Pose
        {
            LatitudeDeg = 1,
            LongitudeDeg = 1,
        };

        await bus.PublishAsync(pose);

        pose.ZoneMask.Should().NotBeNull();
        var mask = pose.ZoneMask;

        mask.ZoneRegistryHash.Should().NotBeNullOrEmpty();
        mask.ActiveZoneIds.Should().Equal(new[]
        {
            "zone:keepout",
            "zone:headland",
            "zone:work",
            "zone:boundary",
        });

        mask.InsideBoundary.Should().BeTrue();
        mask.InsideHeadland.Should().BeTrue();
        mask.InsideKeepOut.Should().BeTrue();
        mask.InsideWorkDisabled.Should().BeTrue();
    }

    [Fact]
    public async Task PublishPose_OutsideZonesShouldReturnEmptyMask()
    {
        var store = new ZoneStore(4326);
        store.MountZones(new[]
        {
            CreateZone("zone:boundary", global::Aog.Core.Zones.ZoneType.Boundary, priority: 10),
        });

        var bus = new InMemoryEventBus();
        using var propagator = new PoseZoneMaskPropagator(store, bus);

        var inside = new Pose { LatitudeDeg = 1, LongitudeDeg = 1 };
        await bus.PublishAsync(inside);
        var baselineHash = inside.ZoneMask.ZoneRegistryHash;

        var outside = new Pose { LatitudeDeg = 20, LongitudeDeg = 20 };
        await bus.PublishAsync(outside);

        outside.ZoneMask.Should().NotBeNull();
        var mask = outside.ZoneMask;
        mask.ZoneRegistryHash.Should().Be(baselineHash);
        mask.ActiveZoneIds.Should().BeEmpty();
        mask.InsideBoundary.Should().BeFalse();
        mask.InsideHeadland.Should().BeFalse();
        mask.InsideKeepOut.Should().BeFalse();
        mask.InsideWorkDisabled.Should().BeFalse();
    }

    [Fact]
    public async Task PublishPose_ShouldReflectUpdatedRegistryHash()
    {
        var store = new ZoneStore(4326);
        var original = CreateZone("zone:boundary", global::Aog.Core.Zones.ZoneType.Boundary, priority: 10);
        store.MountZones(new[] { original });

        var bus = new InMemoryEventBus();
        using var propagator = new PoseZoneMaskPropagator(store, bus);

        var pose = new Pose { LatitudeDeg = 1, LongitudeDeg = 1 };
        await bus.PublishAsync(pose);
        var initialHash = pose.ZoneMask.ZoneRegistryHash;

        var newZone = CreateZone("zone:keepout", global::Aog.Core.Zones.ZoneType.KeepOut, priority: 50);
        var delta = store.Upsert(newZone);
        await bus.PublishAsync(new ZoneRegistryDeltaEvent(delta));

        var updatedPose = new Pose { LatitudeDeg = 1, LongitudeDeg = 1 };
        await bus.PublishAsync(updatedPose);

        updatedPose.ZoneMask.ZoneRegistryHash.Should().NotBe(initialHash);
        updatedPose.ZoneMask.ActiveZoneIds.Should().Contain(newZone.ZoneId);
    }

    private static ZoneDefinition CreateZone(string id, global::Aog.Core.Zones.ZoneType type, uint priority)
    {
        var ring = new[]
        {
            new ZoneCoordinate(0, 0),
            new ZoneCoordinate(4, 0),
            new ZoneCoordinate(4, 4),
            new ZoneCoordinate(0, 4),
            new ZoneCoordinate(0, 0),
        };

        var polygon = new ZonePolygon(new ZoneLinearRing(ring));
        var buffers = new ZoneBuffers(1, 1);
        return new ZoneDefinition(id, type, id, priority, enabled: true, polygon, buffers);
    }
}
