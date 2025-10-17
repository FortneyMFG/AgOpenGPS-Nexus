using System;
using System.Linq;
using Google.Protobuf.WellKnownTypes;
using ProtoZone = Aog.Core.V1.Zone;
using ProtoZoneBuffers = Aog.Core.V1.ZoneBuffers;
using ProtoZoneCoordinate = Aog.Core.V1.ZoneCoordinate;
using ProtoZoneLinearRing = Aog.Core.V1.ZoneLinearRing;
using ProtoZonePolygon = Aog.Core.V1.ZonePolygon;
using ProtoZoneProvenance = Aog.Core.V1.ZoneProvenance;
using ProtoZoneType = Aog.Core.V1.ZoneType;
using ProtoZoneValidWhen = Aog.Core.V1.ZoneValidWhen;

namespace Aog.Core.Zones;

/// <summary>
/// Converts zone domain models to gRPC contract representations.
/// </summary>
public static class ZoneProtoMapper
{
    public static ProtoZone ToProto(ZoneDefinition definition)
    {
        if (definition is null)
        {
            throw new ArgumentNullException(nameof(definition));
        }

        var zone = new ProtoZone
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

    public static ProtoZonePolygon ToProto(ZonePolygon polygon)
    {
        if (polygon is null)
        {
            throw new ArgumentNullException(nameof(polygon));
        }

        var proto = new ProtoZonePolygon
        {
            Exterior = ToProto(polygon.Exterior),
        };
        proto.Holes.Add(polygon.Holes.Select(ToProto));
        return proto;
    }

    private static ProtoZoneLinearRing ToProto(ZoneLinearRing ring)
    {
        if (ring is null)
        {
            throw new ArgumentNullException(nameof(ring));
        }

        var proto = new ProtoZoneLinearRing();
        proto.Vertices.Add(ring.Vertices.Select(ToProto));
        return proto;
    }

    private static ProtoZoneCoordinate ToProto(ZoneCoordinate coordinate)
    {
        var proto = new ProtoZoneCoordinate
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

    private static ProtoZoneBuffers ToProto(ZoneBuffers buffers)
    {
        if (buffers is null)
        {
            throw new ArgumentNullException(nameof(buffers));
        }

        return new ProtoZoneBuffers
        {
            DriveM = buffers.DriveMeters,
            WorkM = buffers.WorkMeters,
        };
    }

    private static ProtoZoneValidWhen ToProto(ZoneValidWhen validWhen)
    {
        if (validWhen is null)
        {
            throw new ArgumentNullException(nameof(validWhen));
        }

        var proto = new ProtoZoneValidWhen();

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

    private static ProtoZoneProvenance ToProto(ZoneProvenance provenance)
    {
        if (provenance is null)
        {
            throw new ArgumentNullException(nameof(provenance));
        }

        var proto = new ProtoZoneProvenance();

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

    private static ProtoZoneType MapZoneType(ZoneType zoneType) => zoneType switch
    {
        ZoneType.Unspecified => ProtoZoneType.Unspecified,
        ZoneType.Boundary => ProtoZoneType.Boundary,
        ZoneType.Headland => ProtoZoneType.Headland,
        ZoneType.KeepOut => ProtoZoneType.KeepOut,
        ZoneType.WorkDisabled => ProtoZoneType.WorkDisabled,
        _ => throw new ArgumentOutOfRangeException(nameof(zoneType), zoneType, "Unknown zone type."),
    };
}
