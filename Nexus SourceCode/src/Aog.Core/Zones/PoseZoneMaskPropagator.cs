using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Aog.Core.Eventing;
using Aog.Core.V1;

namespace Aog.Core.Zones;

/// <summary>
/// Subscribes to PoseStream events and enriches pose samples with zone mask context.
/// </summary>
public sealed class PoseZoneMaskPropagator : IDisposable
{
    private readonly ZoneStore _zoneStore;
    private readonly IDisposable _poseSubscription;
    private readonly IDisposable _zoneSubscription;

    private string _registryHash;
    private bool _disposed;

    public PoseZoneMaskPropagator(ZoneStore zoneStore, IEventBus eventBus)
    {
        _zoneStore = zoneStore ?? throw new ArgumentNullException(nameof(zoneStore));
        ArgumentNullException.ThrowIfNull(eventBus);

        Volatile.Write(ref _registryHash, ComputeRegistryHash(_zoneStore.GetSnapshot(includeDisabled: true)));

        _poseSubscription = eventBus.Subscribe<Pose>(OnPoseAsync);
        _zoneSubscription = eventBus.Subscribe<ZoneRegistryDeltaEvent>(OnZoneDeltaAsync);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _poseSubscription.Dispose();
        _zoneSubscription.Dispose();

        _disposed = true;
        GC.SuppressFinalize(this);
    }

    private ValueTask OnPoseAsync(Pose pose, CancellationToken cancellationToken)
    {
        if (pose is null)
        {
            return ValueTask.CompletedTask;
        }

        var mask = BuildMask(pose.LongitudeDeg, pose.LatitudeDeg);
        pose.ZoneMask = mask;
        return ValueTask.CompletedTask;
    }

    private ValueTask OnZoneDeltaAsync(ZoneRegistryDeltaEvent @event, CancellationToken cancellationToken)
    {
        if (@event is null)
        {
            return ValueTask.CompletedTask;
        }

        Volatile.Write(ref _registryHash, ComputeRegistryHash(_zoneStore.GetSnapshot(includeDisabled: true)));
        return ValueTask.CompletedTask;
    }

    private PoseZoneMask BuildMask(double longitude, double latitude)
    {
        var mask = new PoseZoneMask
        {
            ZoneRegistryHash = Volatile.Read(ref _registryHash) ?? string.Empty,
        };

        IReadOnlyList<ZoneDefinition> zones;
        try
        {
            zones = _zoneStore.GetZonesContaining(longitude, latitude, includeDisabled: false);
        }
        catch (ArgumentOutOfRangeException)
        {
            return mask;
        }

        foreach (var zone in zones)
        {
            mask.ActiveZoneIds.Add(zone.ZoneId);

            switch (zone.Type)
            {
                case ZoneType.Boundary:
                    mask.InsideBoundary = true;
                    break;
                case ZoneType.Headland:
                    mask.InsideHeadland = true;
                    break;
                case ZoneType.KeepOut:
                    mask.InsideKeepOut = true;
                    break;
                case ZoneType.WorkDisabled:
                    mask.InsideWorkDisabled = true;
                    break;
            }
        }

        return mask;
    }

    private static string ComputeRegistryHash(ZoneStoreSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        using var sha256 = SHA256.Create();
        var builder = new StringBuilder();
        builder.Append(snapshot.Zones.Count).Append('|').Append(snapshot.Version).AppendLine();

        foreach (var zone in snapshot.Zones)
        {
            AppendZone(builder, zone);
        }

        var bytes = Encoding.UTF8.GetBytes(builder.ToString());
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static void AppendZone(StringBuilder builder, ZoneDefinition zone)
    {
        builder
            .Append(zone.ZoneId).Append('|')
            .Append(zone.Type).Append('|')
            .Append(zone.Label).Append('|')
            .Append(zone.Priority).Append('|')
            .Append(zone.Enabled ? '1' : '0').Append('|');

        AppendBuffers(builder, zone.Buffers);
        builder.Append('|');
        AppendPolygon(builder, zone.Geometry);
        builder.Append('|');
        AppendValidWhen(builder, zone.ValidWhen);
        builder.Append('|');
        AppendProvenance(builder, zone.Provenance);
        builder.AppendLine();
    }

    private static void AppendBuffers(StringBuilder builder, ZoneBuffers buffers)
    {
        builder
            .Append(FormatDouble(buffers.DriveMeters))
            .Append(',')
            .Append(FormatDouble(buffers.WorkMeters));
    }

    private static void AppendPolygon(StringBuilder builder, ZonePolygon polygon)
    {
        AppendRing(builder, polygon.Exterior);

        if (polygon.Holes.Count == 0)
        {
            return;
        }

        builder.Append('/');
        for (var i = 0; i < polygon.Holes.Count; i++)
        {
            if (i > 0)
            {
                builder.Append(';');
            }

            AppendRing(builder, polygon.Holes[i]);
        }
    }

    private static void AppendRing(StringBuilder builder, ZoneLinearRing ring)
    {
        for (var i = 0; i < ring.Vertices.Count; i++)
        {
            if (i > 0)
            {
                builder.Append(',');
            }

            var vertex = ring.Vertices[i];
            builder.Append(FormatDouble(vertex.Longitude))
                .Append(':')
                .Append(FormatDouble(vertex.Latitude));

            if (vertex.ElevationMeters.HasValue)
            {
                builder.Append(':').Append(FormatDouble(vertex.ElevationMeters.Value));
            }
        }
    }

    private static void AppendValidWhen(StringBuilder builder, ZoneValidWhen? validWhen)
    {
        if (validWhen is null)
        {
            builder.Append("null");
            return;
        }

        builder
            .Append(validWhen.Crop ?? string.Empty)
            .Append(';')
            .Append(validWhen.Season ?? string.Empty)
            .Append(';');

        for (var i = 0; i < validWhen.Conditions.Count; i++)
        {
            if (i > 0)
            {
                builder.Append(',');
            }

            builder.Append(validWhen.Conditions[i]);
        }
    }

    private static void AppendProvenance(StringBuilder builder, ZoneProvenance? provenance)
    {
        if (provenance is null)
        {
            builder.Append("null");
            return;
        }

        builder
            .Append(provenance.Source ?? string.Empty)
            .Append(';')
            .Append(provenance.Timestamp?.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture) ?? string.Empty)
            .Append(';')
            .Append(provenance.Note ?? string.Empty);
    }

    private static string FormatDouble(double value)
        => value.ToString("G17", CultureInfo.InvariantCulture);
}
