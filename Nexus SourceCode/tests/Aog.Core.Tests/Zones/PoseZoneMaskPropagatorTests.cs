using System.Threading.Tasks;
using Aog.Core.Eventing;
using FluentAssertions;
using Xunit;
using Proto = Aog.Core.V1;
using CoreZones = Aog.Core.Zones;

namespace Aog.Core.Tests.Zones;

public sealed class PoseZoneMaskPropagatorTests
{
    [Fact]
    public async Task PublishPose_ShouldPopulateMaskForOverlappingZones()
    {
        var store = new CoreZones.ZoneStore(4326);
        store.MountZones(new[]
        {
            CreateZone("zone:boundary", CoreZones.ZoneType.Boundary, priority: 10),
            CreateZone("zone:headland", CoreZones.ZoneType.Headland, priority: 30),
            CreateZone("zone:work", CoreZones.ZoneType.WorkDisabled, priority: 20),
            CreateZone("zone:keepout", CoreZones.ZoneType.KeepOut, priority: 40),
        });

        var bus = new InMemoryEventBus();
        using var propagator = new CoreZones.PoseZoneMaskPropagator(store, bus);

        var pose = new Proto.Pose
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
        var store = new CoreZones.ZoneStore(4326);
        store.MountZones(new[]
        {
            CreateZone("zone:boundary", CoreZones.ZoneType.Boundary, priority: 10),
        });

        var bus = new InMemoryEventBus();
        using var propagator = new CoreZones.PoseZoneMaskPropagator(store, bus);

        var inside = new Proto.Pose { LatitudeDeg = 1, LongitudeDeg = 1 };
        await bus.PublishAsync(inside);
        var baselineHash = inside.ZoneMask.ZoneRegistryHash;

        var outside = new Proto.Pose { LatitudeDeg = 20, LongitudeDeg = 20 };
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
        var store = new CoreZones.ZoneStore(4326);
        var original = CreateZone("zone:boundary", CoreZones.ZoneType.Boundary, priority: 10);
        store.MountZones(new[] { original });

        var bus = new InMemoryEventBus();
        using var propagator = new CoreZones.PoseZoneMaskPropagator(store, bus);

        var pose = new Proto.Pose { LatitudeDeg = 1, LongitudeDeg = 1 };
        await bus.PublishAsync(pose);
        var initialHash = pose.ZoneMask.ZoneRegistryHash;

        var newZone = CreateZone("zone:keepout", CoreZones.ZoneType.KeepOut, priority: 50);
        var delta = store.Upsert(newZone);
        await bus.PublishAsync(new CoreZones.ZoneRegistryDeltaEvent(delta));

        var updatedPose = new Proto.Pose { LatitudeDeg = 1, LongitudeDeg = 1 };
        await bus.PublishAsync(updatedPose);

        updatedPose.ZoneMask.ZoneRegistryHash.Should().NotBe(initialHash);
        updatedPose.ZoneMask.ActiveZoneIds.Should().Contain(newZone.ZoneId);
    }

    private static CoreZones.ZoneDefinition CreateZone(string id, CoreZones.ZoneType type, uint priority)
    {
        var ring = new[]
        {
            new CoreZones.ZoneCoordinate(0, 0),
            new CoreZones.ZoneCoordinate(4, 0),
            new CoreZones.ZoneCoordinate(4, 4),
            new CoreZones.ZoneCoordinate(0, 4),
            new CoreZones.ZoneCoordinate(0, 0),
        };

        var polygon = new CoreZones.ZonePolygon(new CoreZones.ZoneLinearRing(ring));
        var buffers = new CoreZones.ZoneBuffers(1, 1);
        return new CoreZones.ZoneDefinition(id, type, id, priority, enabled: true, polygon, buffers);
    }
}
