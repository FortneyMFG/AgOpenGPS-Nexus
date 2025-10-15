using System;
using System.Linq;
using Aog.Core.V1;
using Google.Protobuf.WellKnownTypes;

namespace Aog.Core.Zones;

/// <summary>
/// Converts zone domain models to gRPC contract representations.
/// </summary>
internal static class ZoneProtoMapper
{
    public static Zone ToProto(ZoneDefinition definition)
    {
        if (definition is null)
        {
            throw new ArgumentNullException(nameof(definition));
        }

        var zone = new Zone
        {
            ZoneId = definition.ZoneId,
            Type = MapZoneType(definition.Type),
            Label = definition.Label,
            Priority = definition.Priority,
            Enabled = definition.Enabled,
            Geometry = ToProto(definition.Geometry),
            Buffers = ToProto(definition.Buffers),
        };

        if (definition.ValidWhen is not null)
        {
            zone.ValidWhen = ToProto(definition.ValidWhen);
        }

        if (definition.Provenance is not null)
        {
            zone.Provenance = ToProto(definition.Provenance);
        }

        return zone;
    }

    public static ZonePolygon ToProto(ZonePolygon polygon)
    {
        if (polygon is null)
        {
            throw new ArgumentNullException(nameof(polygon));
        }

        var proto = new ZonePolygon
        {
            Exterior = ToProto(polygon.Exterior),
        };

        proto.Holes.AddRange(polygon.Holes.Select(ToProto));
        return proto;
    }

    private static ZoneLinearRing ToProto(ZoneLinearRing ring)
    {
        if (ring is null)
        {
            throw new ArgumentNullException(nameof(ring));
        }

        var proto = new ZoneLinearRing();
        foreach (var vertex in ring.Vertices)
        {
            proto.Vertices.Add(ToProto(vertex));
        }

        return proto;
    }

    private static ZoneCoordinate ToProto(ZoneCoordinate coordinate)
    {
        var proto = new ZoneCoordinate
        {
            LongitudeDeg = coordinate.Longitude,
            LatitudeDeg = coordinate.Latitude,
        };

        if (coordinate.ElevationMeters.HasValue)
        {
            proto.ElevationM = coordinate.ElevationMeters.Value;
        }

        return proto;
    }

    private static ZoneBuffers ToProto(ZoneBuffers buffers)
    {
        if (buffers is null)
        {
            throw new ArgumentNullException(nameof(buffers));
        }

        return new ZoneBuffers
        {
            DriveM = buffers.DriveMeters,
            WorkM = buffers.WorkMeters,
        };
    }

    private static ZoneValidWhen ToProto(ZoneValidWhen validWhen)
    {
        if (validWhen is null)
        {
            throw new ArgumentNullException(nameof(validWhen));
        }

        var proto = new ZoneValidWhen();

        if (!string.IsNullOrWhiteSpace(validWhen.Crop))
        {
            proto.Crop = validWhen.Crop;
        }

        if (!string.IsNullOrWhiteSpace(validWhen.Season))
        {
            proto.Season = validWhen.Season;
        }

        proto.Conditions.Add(validWhen.Conditions);
        return proto;
    }

    private static ZoneProvenance ToProto(ZoneProvenance provenance)
    {
        if (provenance is null)
        {
            throw new ArgumentNullException(nameof(provenance));
        }

        var proto = new ZoneProvenance();

        if (!string.IsNullOrWhiteSpace(provenance.Source))
        {
            proto.Source = provenance.Source;
        }

        if (provenance.Timestamp.HasValue)
        {
            proto.Timestamp = Timestamp.FromDateTimeOffset(provenance.Timestamp.Value);
        }

        if (!string.IsNullOrWhiteSpace(provenance.Note))
        {
            proto.Note = provenance.Note;
        }

        return proto;
    }

    private static Aog.Core.V1.ZoneType MapZoneType(ZoneType zoneType) => zoneType switch
    {
        ZoneType.Unspecified => Aog.Core.V1.ZoneType.ZoneTypeUnspecified,
        ZoneType.Boundary => Aog.Core.V1.ZoneType.ZoneTypeBoundary,
        ZoneType.Headland => Aog.Core.V1.ZoneType.ZoneTypeHeadland,
        ZoneType.KeepOut => Aog.Core.V1.ZoneType.ZoneTypeKeepOut,
        ZoneType.WorkDisabled => Aog.Core.V1.ZoneType.ZoneTypeWorkDisabled,
        _ => throw new ArgumentOutOfRangeException(nameof(zoneType), zoneType, "Unknown zone type."),
    };
}
