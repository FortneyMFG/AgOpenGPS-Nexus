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

public sealed class RadioBridgeLoraAdapterTests
{
    [Fact]
    public async Task Adapter_ForwardsMeshPublicationWithFecFlag()
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2025, 3, 22, 10, 0, 0, TimeSpan.Zero));
        var mesh = new LiveTelemetryMeshService(clock);
        var options = Options.Create(new RadioBridgeLoraAdapterOptions
        {
            Enabled = true,
            DeviceId = "bridge.lora.alpha",
            DeviceLabel = "Bridge",
            Endpoint = "sim://lora-loopback",
            DiagnosticsInterval = TimeSpan.Zero,
        });

        var simulator = new SimulatedRadioBridgeLink();
        var adapter = new RadioBridgeLoraAdapter(mesh, options, new StubLinkFactory(simulator), NullLogger<RadioBridgeLoraAdapter>.Instance, clock);

        await adapter.StartAsync(CancellationToken.None);

        try
        {
            await mesh.RegisterOrUpdateDeviceAsync(new MeshDeviceRegistration(
                "publisher",
                "Publisher",
                shareProfile: new MeshShareProfile(new[] { new MeshShareGrant("*", "*", MeshDataTier.All) }),
                subscribeProfile: new MeshSubscribeProfile(new[] { new MeshSubscribeGrant("*", "*", MeshDataTier.All) }))
            {
                Capabilities = Array.Empty<string>()
            }, CancellationToken.None);

            await mesh.PublishAsync(new MeshPublishRequest(
                "publisher",
                "aog/live/season/job/coverage",
                MeshDataTier.Coverage,
                new byte[] { 0x01, 0x02 },
                clock.GetUtcNow(),
                new Dictionary<string, string>()), CancellationToken.None);

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
            Assert.True(decoded.Flags.HasFlag(RadioBridgeFrameFlags.ForwardErrorCorrection));
        }
        finally
        {
            await adapter.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task Adapter_EmitsDiagnosticsWithFecState()
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2025, 3, 22, 11, 0, 0, TimeSpan.Zero));
        var mesh = new LiveTelemetryMeshService(clock);
        var options = Options.Create(new RadioBridgeLoraAdapterOptions
        {
            Enabled = true,
            DeviceId = "bridge.lora.alpha",
            DeviceLabel = "Bridge",
            Endpoint = "sim://lora-loopback",
            DiagnosticsInterval = TimeSpan.FromSeconds(1),
        });

        var simulator = new SimulatedRadioBridgeLink();
        var adapter = new RadioBridgeLoraAdapter(mesh, options, new StubLinkFactory(simulator), NullLogger<RadioBridgeLoraAdapter>.Instance, clock);

        await adapter.StartAsync(CancellationToken.None);

        try
        {
            await mesh.RegisterOrUpdateDeviceAsync(new MeshDeviceRegistration(
                "observer",
                "Observer",
                shareProfile: new MeshShareProfile(new[] { new MeshShareGrant("system", "radio-lora", MeshDataTier.All) }),
                subscribeProfile: new MeshSubscribeProfile(new[] { new MeshSubscribeGrant("system", "radio-lora", MeshDataTier.All) }))
            {
                Capabilities = Array.Empty<string>()
            }, CancellationToken.None);

            var diagnostics = mesh.SubscribeAsync(new MeshSubscriptionRequest("observer", "system", "radio-lora"));
            clock.Advance(TimeSpan.FromSeconds(1));

            using (var diagnosticsCts = new CancellationTokenSource(TimeSpan.FromSeconds(1)))
            await using (var enumerator = diagnostics.GetAsyncEnumerator(diagnosticsCts.Token))
            {
                while (await enumerator.MoveNextAsync())
                {
                    var publication = enumerator.Current;
                    if (publication.Topic.Contains("bridge.lora.alpha.radio", StringComparison.Ordinal))
                    {
                        var doc = JsonDocument.Parse(publication.Payload);
                        Assert.True(doc.RootElement.GetProperty("forwardErrorCorrection").GetBoolean());
                        Assert.True(doc.RootElement.GetProperty("sendIntervalMs").GetDouble() >= 200);
                        Assert.Equal("lora", doc.RootElement.GetProperty("kind").GetString());
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
