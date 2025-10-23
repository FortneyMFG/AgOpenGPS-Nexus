using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aog.Core.Eventing;
using Aog.Core.Host.Zones;
using Aog.Core.V1;
using Aog.Core.Zones;
using FluentAssertions;
using Grpc.Core;
using Grpc.Core.Testing;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aog.Core.Host.Tests.Zones;

public sealed class ZoneGrpcServiceTests
{
    [Fact]
    public async Task ListZones_RespectsIncludeDisabled()
    {
        var store = CreateStoreWithZones(out var enabled, out var disabled);
        var service = CreateService(store);

        var enabledOnly = await service.ListZones(new ListZonesRequest { IncludeDisabled = false }, CreateContext(method: "ListZones"));

        enabledOnly.Zones.Should().ContainSingle().Which.ZoneId.Should().Be(enabled.ZoneId);

        var allZones = await service.ListZones(new ListZonesRequest { IncludeDisabled = true }, CreateContext(method: "ListZones"));

        allZones.Zones.Select(z => z.ZoneId).Should().BeEquivalentTo(new[] { enabled.ZoneId, disabled.ZoneId });
    }

    [Fact]
    public async Task GetZonesInBounds_ValidatesRequest()
    {
        var store = CreateStoreWithZones(out _, out _);
        var service = CreateService(store);

        var invalidLon = new GetZonesInBoundsRequest
        {
            MinLongitudeDeg = 5,
            MaxLongitudeDeg = 1,
            MinLatitudeDeg = 0,
            MaxLatitudeDeg = 1,
        };

        await Assert.ThrowsAsync<RpcException>(() => service.GetZonesInBounds(invalidLon, CreateContext()));

        var invalidLat = new GetZonesInBoundsRequest
        {
            MinLongitudeDeg = 0,
            MaxLongitudeDeg = 1,
            MinLatitudeDeg = 3,
            MaxLatitudeDeg = 2,
        };

        await Assert.ThrowsAsync<RpcException>(() => service.GetZonesInBounds(invalidLat, CreateContext()));
    }

    [Fact]
    public async Task WatchZones_StreamsSnapshotAndDeltas()
    {
        var store = CreateStoreWithZones(out var enabled, out var disabled);
        var eventBus = new InMemoryEventBus();
        var service = new ZoneGrpcService(store, eventBus, NullLogger<ZoneGrpcService>.Instance);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var writer = new RecordingStreamWriter<WatchZonesResponse>();
        var context = CreateContext(cts.Token, "WatchZones");

        var watchTask = service.WatchZones(new WatchZonesRequest { IncludeDisabled = false }, writer, context);

        var snapshot = await writer.ReadAsync(cts.Token);
        snapshot.Snapshot.Select(z => z.ZoneId).Should().Equal(enabled.ZoneId);

        var newZone = CreateZone("zone-new", originX: 20, originY: 20, size: 5, enabled: true, priority: 50);
        var upsertDelta = store.Upsert(newZone);
        await eventBus.PublishAsync(new ZoneRegistryDeltaEvent(upsertDelta), CancellationToken.None);

        var upsertUpdate = await writer.ReadAsync(cts.Token);
        upsertUpdate.Deltas.Should().ContainSingle();
        upsertUpdate.Deltas[0].Upserts.Should().ContainSingle().Which.ZoneId.Should().Be(newZone.ZoneId);

        var (enabledOriginX, enabledOriginY, enabledSize) = GetZoneDimensions(enabled);
        var disableExisting = CreateZone(enabled.ZoneId, enabledOriginX, enabledOriginY, enabledSize, enabled: false, priority: enabled.Priority);
        var disableDelta = store.Upsert(disableExisting);
        await eventBus.PublishAsync(new ZoneRegistryDeltaEvent(disableDelta), CancellationToken.None);

        var disableUpdate = await writer.ReadAsync(cts.Token);
        disableUpdate.Deltas.Should().ContainSingle();
        disableUpdate.Deltas[0].DeletedZoneIds.Should().Contain(enabled.ZoneId);

        cts.Cancel();
        await watchTask;
    }

    [Fact]
    public async Task WatchZones_IncludingDisabledStreamsDisabledUpserts()
    {
        var store = CreateStoreWithZones(out var enabled, out var disabled);
        var eventBus = new InMemoryEventBus();
        var service = new ZoneGrpcService(store, eventBus, NullLogger<ZoneGrpcService>.Instance);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var writer = new RecordingStreamWriter<WatchZonesResponse>();
        var context = CreateContext(cts.Token, "WatchZones");

        var watchTask = service.WatchZones(new WatchZonesRequest { IncludeDisabled = true }, writer, context);

        var snapshot = await writer.ReadAsync(cts.Token);
        snapshot.Snapshot.Select(z => z.ZoneId).Should().BeEquivalentTo(new[] { enabled.ZoneId, disabled.ZoneId });

        var (disabledOriginX, disabledOriginY, disabledSize) = GetZoneDimensions(disabled);
        var enableDisabled = CreateZone(disabled.ZoneId, disabledOriginX, disabledOriginY, disabledSize, enabled: true, priority: disabled.Priority);
        var delta = store.Upsert(enableDisabled);
        await eventBus.PublishAsync(new ZoneRegistryDeltaEvent(delta), CancellationToken.None);

        var update = await writer.ReadAsync(cts.Token);
        update.Deltas.Should().ContainSingle();
        update.Deltas[0].Upserts.Should().ContainSingle().Which.ZoneId.Should().Be(disabled.ZoneId);

        cts.Cancel();
        await watchTask;
    }

    private static ZoneGrpcService CreateService(ZoneStore store)
    {
        return new ZoneGrpcService(store, new InMemoryEventBus(), NullLogger<ZoneGrpcService>.Instance);
    }

    private static ZoneStore CreateStoreWithZones(out ZoneDefinition enabled, out ZoneDefinition disabled)
    {
        var store = new ZoneStore(4326);
        enabled = CreateZone("zone-enabled", originX: 0, originY: 0, size: 4, enabled: true, priority: 100);
        disabled = CreateZone("zone-disabled", originX: 50, originY: 50, size: 4, enabled: false, priority: 10);
        store.MountZones(new[] { enabled, disabled });
        return store;
    }

    private static ZoneDefinition CreateZone(
        string zoneId,
        double originX,
        double originY,
        double size,
        bool enabled,
        uint priority)
    {
        var ring = new[]
        {
            new Aog.Core.Zones.ZoneCoordinate(originX, originY),
            new Aog.Core.Zones.ZoneCoordinate(originX + size, originY),
            new Aog.Core.Zones.ZoneCoordinate(originX + size, originY + size),
            new Aog.Core.Zones.ZoneCoordinate(originX, originY + size),
            new Aog.Core.Zones.ZoneCoordinate(originX, originY),
        };

        var polygon = new Aog.Core.Zones.ZonePolygon(new Aog.Core.Zones.ZoneLinearRing(ring));
        var buffers = new Aog.Core.Zones.ZoneBuffers(1, 2);
        return new ZoneDefinition(zoneId, Aog.Core.Zones.ZoneType.Headland, zoneId, priority, enabled, polygon, buffers);
    }

    private static (double OriginX, double OriginY, double Size) GetZoneDimensions(ZoneDefinition zone)
    {
        var exterior = zone.Geometry.Exterior.Vertices;
        var origin = exterior[0];
        var next = exterior[1];
        var size = Math.Abs(next.Longitude - origin.Longitude);
        return (origin.Longitude, origin.Latitude, size);
    }

    private static ServerCallContext CreateContext(CancellationToken token = default, string method = "GetZonesInBounds")
    {
        return TestServerCallContext.Create(
            method,
            null,
            DateTime.UtcNow.AddMinutes(1),
            new Metadata(),
            token,
            "ipv4:127.0.0.1",
            null,
            null,
            _ => Task.CompletedTask,
            () => new WriteOptions(),
            _ => { });
    }

    private sealed class RecordingStreamWriter<T> : IServerStreamWriter<T>
    {
        private readonly System.Threading.Channels.Channel<T> _channel = System.Threading.Channels.Channel.CreateUnbounded<T>(new System.Threading.Channels.UnboundedChannelOptions
        {
            SingleReader = false,
            SingleWriter = false,
        });

        public WriteOptions? WriteOptions { get; set; }

        public Task WriteAsync(T message)
        {
            if (message is null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            if (!_channel.Writer.TryWrite(message))
            {
                throw new InvalidOperationException("Unable to record message.");
            }

            return Task.CompletedTask;
        }

        public async Task<T> ReadAsync(CancellationToken cancellationToken)
        {
            return await _channel.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
