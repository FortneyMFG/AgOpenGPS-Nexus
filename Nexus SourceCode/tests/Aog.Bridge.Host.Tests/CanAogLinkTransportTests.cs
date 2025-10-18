using System.Threading;
using System.Threading.Tasks;
using Aog.Bridge.Host.AogLink;
using Aog.Bridge.Host.AogLink.Can;
using Aog.Bridge.Host.Tests.Support;
using Aog.Core.V1;
using Aog.Link.V1;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aog.Bridge.Host.Tests;

public sealed class CanAogLinkTransportTests
{
    [Fact]
    public async Task SendAsync_FragmentationOccurs_WhenPayloadExceedsFrameSize()
    {
        var bus = new FakeCanBus();
        var transport = new CanAogLinkTransport(bus, NullLogger<CanAogLinkTransport>.Instance);

        var envelope = CreateLargeEnvelope();

        await transport.SendAsync(envelope, CancellationToken.None);

        Assert.True(bus.SentFrames.Count > 1);
        Assert.All(bus.SentFrames, frame => Assert.True(frame.Data.Length <= 64));
    }

    [Fact]
    public async Task ReadAsync_ReassemblesFragments()
    {
        var bus = new FakeCanBus();
        var transport = new CanAogLinkTransport(bus, NullLogger<CanAogLinkTransport>.Instance);
        await transport.StartAsync(CancellationToken.None);

        var envelope = CreateLargeEnvelope();

        var captureBus = new FakeCanBus();
        var captureTransport = new CanAogLinkTransport(captureBus, NullLogger<CanAogLinkTransport>.Instance);
        await captureTransport.SendAsync(envelope, CancellationToken.None);

        foreach (var frame in captureBus.SentFrames)
        {
            await bus.InjectAsync(frame);
        }

        var enumerator = transport.ReadAsync(CancellationToken.None).GetAsyncEnumerator();
        Assert.True(await enumerator.MoveNextAsync());
        var decoded = enumerator.Current;
        Assert.Equal(MessageType.LinkMessageTypeDiscoveryAnnounce, decoded.Header.MessageType);

        await transport.StopAsync(CancellationToken.None);
    }

    private static LinkEnvelope CreateLargeEnvelope()
    {
        var announce = new DiscoveryAnnounce
        {
            Identity = new NodeIdentity { NodeId = 10, FirmwareVersion = "1.0.0", HardwareModel = "test" },
            SessionId = 1,
        };

        for (var i = 0; i < 40; i++)
        {
            announce.CapabilityIds.Add($"cap-{i}");
        }

        return new LinkEnvelope
        {
            Header = new FrameHeader
            {
                Version = 1,
                MessageClass = LinkClass.System,
                MessageType = MessageType.LinkMessageTypeDiscoveryAnnounce,
                Sequence = 42,
                Source = 7,
                Destination = 0,
                PayloadLength = (uint)announce.CalculateSize(),
            },
            DiscoveryAnnounce = announce,
        };
    }
}
