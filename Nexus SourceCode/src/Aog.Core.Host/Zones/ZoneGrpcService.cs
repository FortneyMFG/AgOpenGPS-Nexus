using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Aog.Core.Eventing;
using Aog.Core.V1;
using Aog.Core.Zones;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace Aog.Core.Host.Zones;

/// <summary>
/// gRPC ZoneService implementation backed by the in-memory <see cref="ZoneStore"/>.
/// </summary>
public sealed class ZoneGrpcService : ZoneService.ZoneServiceBase
{
    private readonly ZoneStore _store;
    private readonly IEventBus _eventBus;
    private readonly ILogger<ZoneGrpcService>? _logger;

    public ZoneGrpcService(ZoneStore store, IEventBus eventBus, ILogger<ZoneGrpcService>? logger = null)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _logger = logger;
    }

    public override Task<ListZonesResponse> ListZones(ListZonesRequest request, ServerCallContext context)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var response = new ListZonesResponse();
        var zones = _store.ListZones(request.IncludeDisabled);

        foreach (var zone in zones)
        {
            response.Zones.Add(ZoneProtoMapper.ToProto(zone));
        }

        return Task.FromResult(response);
    }

    public override Task<GetZonesInBoundsResponse> GetZonesInBounds(GetZonesInBoundsRequest request, ServerCallContext context)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (request.MinLongitudeDeg > request.MaxLongitudeDeg)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Minimum longitude exceeds maximum longitude."));
        }

        if (request.MinLatitudeDeg > request.MaxLatitudeDeg)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Minimum latitude exceeds maximum latitude."));
        }

        var bounds = new ZoneBoundingBox(
            request.MinLongitudeDeg,
            request.MinLatitudeDeg,
            request.MaxLongitudeDeg,
            request.MaxLatitudeDeg);

        var response = new GetZonesInBoundsResponse();
        var zones = _store.GetZonesInBounds(bounds, request.IncludeDisabled);

        foreach (var zone in zones)
        {
            response.Zones.Add(ZoneProtoMapper.ToProto(zone));
        }

        return Task.FromResult(response);
    }

    public override async Task WatchZones(WatchZonesRequest request, IServerStreamWriter<WatchZonesResponse> responseStream, ServerCallContext context)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (responseStream is null)
        {
            throw new ArgumentNullException(nameof(responseStream));
        }

        var includeDisabled = request.IncludeDisabled;
        var channel = Channel.CreateUnbounded<ZoneStoreDelta>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false,
        });

        ValueTask Handler(ZoneRegistryDeltaEvent @event, CancellationToken token)
        {
            if (@event is null)
            {
                return ValueTask.CompletedTask;
            }

            if (!channel.Writer.TryWrite(@event.Delta))
            {
                _logger?.LogDebug("Zone delta dropped because watcher channel is closed (version {Version}).", @event.Delta.Version);
            }

            return ValueTask.CompletedTask;
        }

        using var subscription = _eventBus.Subscribe<ZoneRegistryDeltaEvent>(Handler);
        var knownZoneIds = new HashSet<string>(StringComparer.Ordinal);

        var snapshot = _store.GetSnapshot(includeDisabled: true);
        var currentVersion = snapshot.Version;

        var snapshotResponse = new WatchZonesResponse();
        foreach (var zone in snapshot.Zones)
        {
            if (!includeDisabled && !zone.Enabled)
            {
                continue;
            }

            snapshotResponse.Snapshot.Add(ZoneProtoMapper.ToProto(zone));
            knownZoneIds.Add(zone.ZoneId);
        }

        await responseStream.WriteAsync(snapshotResponse).ConfigureAwait(false);

        try
        {
            await foreach (var delta in channel.Reader.ReadAllAsync(context.CancellationToken).ConfigureAwait(false))
            {
                if (delta.Version <= currentVersion)
                {
                    continue;
                }

                currentVersion = delta.Version;
                var protoDelta = BuildProtoDelta(delta, includeDisabled, knownZoneIds);

                if (protoDelta is null)
                {
                    continue;
                }

                var update = new WatchZonesResponse();
                update.Deltas.Add(protoDelta);
                await responseStream.WriteAsync(update).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            _logger?.LogDebug("Zone watcher cancelled.");
        }
        finally
        {
            channel.Writer.TryComplete();
        }
    }

    private static ZoneDelta? BuildProtoDelta(ZoneStoreDelta delta, bool includeDisabled, ISet<string> knownZoneIds)
    {
        var protoDelta = new ZoneDelta
        {
            Version = delta.Version,
        };

        foreach (var definition in delta.Upserts)
        {
            if (includeDisabled || definition.Enabled)
            {
                protoDelta.Upserts.Add(ZoneProtoMapper.ToProto(definition));
                knownZoneIds.Add(definition.ZoneId);
            }
            else if (knownZoneIds.Remove(definition.ZoneId))
            {
                protoDelta.DeletedZoneIds.Add(definition.ZoneId);
            }
        }

        foreach (var deleted in delta.DeletedZoneIds)
        {
            if (knownZoneIds.Remove(deleted))
            {
                protoDelta.DeletedZoneIds.Add(deleted);
            }
        }

        if (protoDelta.Upserts.Count == 0 && protoDelta.DeletedZoneIds.Count == 0)
        {
            return null;
        }

        return protoDelta;
    }
}
