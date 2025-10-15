using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aog.Agio.RadioBridge;
using Aog.Agio.RadioBridge.Simulation;
using Aog.Core.Mesh;
using Aog.Core.Mesh.RadioBridge;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Aog.Agio.Tests;

public sealed class RadioBridgeElrsAdapterTests
{
    [Fact]
    public async Task Adapter_ForwardsMeshPublicationToRadio()
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2025, 3, 21, 12, 0, 0, TimeSpan.Zero));
        var mesh = new LiveTelemetryMeshService(clock);
        var options = Options.Create(new RadioBridgeElrsAdapterOptions
        {
            Enabled = true,
            DeviceId = "bridge.elrs.alpha",
            DeviceLabel = "Bridge",
            Endpoint = "sim://loopback",
            DiagnosticsInterval = TimeSpan.Zero,
        });

        var simulator = new SimulatedRadioBridgeLink();
        var adapter = new RadioBridgeElrsAdapter(mesh, options, new StubLinkFactory(simulator), NullLogger<RadioBridgeElrsAdapter>.Instance, clock);

        await adapter.StartAsync(CancellationToken.None);

        try
        {
            await mesh.RegisterOrUpdateDeviceAsync(new MeshDeviceRegistration(
                "publisher",
                "Publisher",
                new[] { "test" },
                new MeshShareProfile(new[] { new MeshShareGrant("*", "*", MeshDataTier.All) }),
                new MeshSubscribeProfile(new[] { new MeshSubscribeGrant("*", "*", MeshDataTier.All) })), CancellationToken.None);

            var payload = new byte[] { 0x10, 0x20 };
            await mesh.PublishAsync(new MeshPublishRequest(
                "publisher",
                "aog/live/season/job/coverage",
                MeshDataTier.Coverage,
                payload,
                clock.GetUtcNow(),
                new Dictionary<string, string> { ["source"] = "mesh" }), CancellationToken.None);

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            ReadOnlyMemory<byte>? frame = null;
            await using (var enumerator = simulator.OutboundFrames.GetAsyncEnumerator(cts.Token))
            {
                if (await enumerator.MoveNextAsync())
                {
                    frame = enumerator.Current;
                }
            }

            Assert.True(frame.HasValue);
            var decoded = RadioBridgeFrameCodec.Decode(frame.Value.Span);
            Assert.Equal(RadioBridgePayloadType.Data, decoded.PayloadType);
            Assert.Equal(RadioBridgeTopicHasher.ComputeHash("aog/live/season/job/coverage"), decoded.TopicHash);
        }
        finally
        {
            await adapter.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task Adapter_PublishesInboundFrameToMesh()
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2025, 3, 21, 12, 0, 0, TimeSpan.Zero));
        var mesh = new LiveTelemetryMeshService(clock);
        var options = Options.Create(new RadioBridgeElrsAdapterOptions
        {
            Enabled = true,
            DeviceId = "bridge.elrs.alpha",
            DeviceLabel = "Bridge",
            Endpoint = "sim://loopback",
            DiagnosticsInterval = TimeSpan.Zero,
        });

        var simulator = new SimulatedRadioBridgeLink();
        var adapter = new RadioBridgeElrsAdapter(mesh, options, new StubLinkFactory(simulator), NullLogger<RadioBridgeElrsAdapter>.Instance, clock);

        await adapter.StartAsync(CancellationToken.None);

        try
        {
            await mesh.RegisterOrUpdateDeviceAsync(new MeshDeviceRegistration(
                "inspector",
                "Inspector",
                Array.Empty<string>(),
                new MeshShareProfile(new[] { new MeshShareGrant("*", "*", MeshDataTier.All) }),
                new MeshSubscribeProfile(new[] { new MeshSubscribeGrant("*", "*", MeshDataTier.All) })), CancellationToken.None);

            var subscription = mesh.SubscribeAsync(new MeshSubscriptionRequest("inspector"));
            var firmware = new RadioBridgeFirmwareStub("firmware.elrs");
            var payload = JsonSerializer.SerializeToUtf8Bytes(new { Value = 42 });
            var frame = firmware.CreatePublication(
                "aog/live/season/job/coverage",
                MeshDataTier.Coverage,
                clock.GetUtcNow(),
                "remote.device",
                new Dictionary<string, string> { ["origin"] = "radio" },
                payload);

            await simulator.EnqueueInboundAsync(frame);

            using (var publicationCts = new CancellationTokenSource(TimeSpan.FromSeconds(1)))
            await using (var enumerator = subscription.GetAsyncEnumerator(publicationCts.Token))
            {
                while (await enumerator.MoveNextAsync())
                {
                    var publication = enumerator.Current;
                    if (publication.Topic == "aog/live/season/job/coverage")
                    {
                        Assert.Equal(options.Value.DeviceId, publication.PublisherDeviceId);
                        Assert.True(publication.Metadata!.ContainsKey("radio.publisher"));
                        Assert.Equal("remote.device", publication.Metadata["radio.publisher"]);
                        Assert.Equal(payload, publication.Payload.ToArray());
                        break;
                    }
                }
            }
        }
        finally
        {
            await adapter.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task Adapter_EmitsDiagnostics()
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2025, 3, 21, 12, 0, 0, TimeSpan.Zero));
        var mesh = new LiveTelemetryMeshService(clock);
        var options = Options.Create(new RadioBridgeElrsAdapterOptions
        {
            Enabled = true,
            DeviceId = "bridge.elrs.alpha",
            DeviceLabel = "Bridge",
            Endpoint = "sim://loopback",
            DiagnosticsInterval = TimeSpan.FromSeconds(1),
        });

        var simulator = new SimulatedRadioBridgeLink();
        simulator.SetLinkMetrics(new RadioBridgeLinkMetrics(-65, 0.05));

        var adapter = new RadioBridgeElrsAdapter(mesh, options, new StubLinkFactory(simulator), NullLogger<RadioBridgeElrsAdapter>.Instance, clock);
        await adapter.StartAsync(CancellationToken.None);

        try
        {
            await mesh.RegisterOrUpdateDeviceAsync(new MeshDeviceRegistration(
                "observer",
                "Observer",
                Array.Empty<string>(),
                new MeshShareProfile(new[] { new MeshShareGrant("system", "radio", MeshDataTier.All) }),
                new MeshSubscribeProfile(new[] { new MeshSubscribeGrant("system", "radio", MeshDataTier.All) })), CancellationToken.None);

            var diagnostics = mesh.SubscribeAsync(new MeshSubscriptionRequest("observer", "system", "radio"));

            clock.Advance(TimeSpan.FromSeconds(1));

            using (var diagnosticsCts = new CancellationTokenSource(TimeSpan.FromSeconds(1)))
            await using (var enumerator = diagnostics.GetAsyncEnumerator(diagnosticsCts.Token))
            {
                while (await enumerator.MoveNextAsync())
                {
                    var publication = enumerator.Current;
                    if (publication.Topic.Contains($"{options.Value.DeviceId}.radio", StringComparison.Ordinal))
                    {
                        var doc = JsonDocument.Parse(publication.Payload);
                        Assert.Equal(options.Value.DeviceId, doc.RootElement.GetProperty("deviceId").GetString());
                        Assert.Equal(-65, doc.RootElement.GetProperty("link").GetProperty("rssi").GetDouble());
                        break;
                    }
                }
            }
        }
        finally
        {
            await adapter.StopAsync(CancellationToken.None);
        }
    }

    private sealed class StubLinkFactory : IRadioBridgeLinkFactory
    {
        private readonly IRadioBridgeLink _link;

        public StubLinkFactory(IRadioBridgeLink link)
        {
            _link = link;
        }

        public IRadioBridgeLink Create(RadioBridgeAdapterOptions options) => _link;
    }
}
