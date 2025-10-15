using System;
using Aog.Core.Zones;
using FluentAssertions;
using Google.Protobuf.WellKnownTypes;
using Xunit;

namespace Aog.Core.Tests.Zones;

public sealed class ZoneProtoMapperTests
{
    [Fact]
    public void ToProto_MapsAllFields()
    {
        var exterior = new ZoneLinearRing(new[]
        {
            new ZoneCoordinate(0, 0, 5),
            new ZoneCoordinate(10, 0, 5),
            new ZoneCoordinate(10, 10, 5),
            new ZoneCoordinate(0, 10, 5),
            new ZoneCoordinate(0, 0, 5),
        });

        var hole = new ZoneLinearRing(new[]
        {
            new ZoneCoordinate(2, 2),
            new ZoneCoordinate(4, 2),
            new ZoneCoordinate(4, 4),
            new ZoneCoordinate(2, 4),
            new ZoneCoordinate(2, 2),
        });

        var polygon = new ZonePolygon(exterior, new[] { hole });
        var buffers = new ZoneBuffers(1.5, 3.25);
        var validWhen = new ZoneValidWhen("corn", "2026", new[] { "dry" });
        var timestamp = new DateTimeOffset(2024, 02, 15, 12, 30, 0, TimeSpan.Zero);
        var provenance = new ZoneProvenance("import", timestamp, "initial load");

        var definition = new ZoneDefinition(
            "zone-keepout",
            ZoneType.KeepOut,
            "Rock pile",
            priority: 90,
            enabled: true,
            polygon,
            buffers,
            validWhen,
            provenance);

        var proto = ZoneProtoMapper.ToProto(definition);

        proto.ZoneId.Should().Be(definition.ZoneId);
        proto.Type.Should().Be(Aog.Core.V1.ZoneType.ZoneTypeKeepOut);
        proto.Label.Should().Be(definition.Label);
        proto.Priority.Should().Be(definition.Priority);
        proto.Enabled.Should().BeTrue();

        proto.Geometry.Should().NotBeNull();
        proto.Geometry!.Exterior.Vertices.Should().HaveCount(exterior.Vertices.Count);
        proto.Geometry.Holes.Should().ContainSingle();
        proto.Geometry.Holes[0].Vertices.Should().HaveCount(hole.Vertices.Count);

        proto.Buffers.Should().NotBeNull();
        proto.Buffers!.DriveM.Should().BeApproximately(buffers.DriveMeters, 1e-6);
        proto.Buffers.WorkM.Should().BeApproximately(buffers.WorkMeters, 1e-6);

        proto.ValidWhen.Should().NotBeNull();
        proto.ValidWhen!.Crop.Should().Be(validWhen.Crop);
        proto.ValidWhen.Season.Should().Be(validWhen.Season);
        proto.ValidWhen.Conditions.Should().Equal(validWhen.Conditions);

        proto.Provenance.Should().NotBeNull();
        proto.Provenance!.Source.Should().Be(provenance.Source);
        proto.Provenance.Note.Should().Be(provenance.Note);
        proto.Provenance.Timestamp.Should().Be(Timestamp.FromDateTimeOffset(timestamp));
    }

    [Fact]
    public void ToProto_HandlesOptionalMetadata()
    {
        var exterior = new ZoneLinearRing(new[]
        {
            new ZoneCoordinate(0, 0),
            new ZoneCoordinate(5, 0),
            new ZoneCoordinate(5, 5),
            new ZoneCoordinate(0, 5),
            new ZoneCoordinate(0, 0),
        });

        var polygon = new ZonePolygon(exterior);
        var buffers = new ZoneBuffers(0.5, 1.0);

        var definition = new ZoneDefinition(
            "zone-boundary",
            ZoneType.Boundary,
            "North Boundary",
            priority: 10,
            enabled: false,
            polygon,
            buffers);

        var proto = ZoneProtoMapper.ToProto(definition);

        proto.ValidWhen.Should().BeNull();
        proto.Provenance.Should().BeNull();
        proto.Enabled.Should().BeFalse();
    }
}
