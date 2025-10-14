using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aog.Core.Eventing;
using Aog.Core.V1;
using Aog.Plugins.Isobus;
using FluentAssertions;
using Google.Protobuf;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Aog.Plugins.Tests.Isobus;

public sealed class IsobusRouterTests
{
    private static readonly byte[] IsoName = { 0x10, 0x20, 0x30, 0x40, 0x50, 0x60, 0x70, 0x80 };

    [Fact]
    public async Task RouteCanFrameAsync_PublishesDatagramAndHandshake()
    {
        var bus = new InMemoryEventBus();
        var handshake = new IsobusHandshakeManager(0x90, IsoName, "bridge", "vehicle", new FakeTimeProvider());
        var router = new IsobusRouter(bus, handshake, "isobus/udp", new FakeTimeProvider());

        var publishedFrames = new List<CanFrame>();
        var publishedDatagrams = new List<IsobusDatagram>();

        using var frameSubscription = bus.Subscribe<CanFrame>((message, _) =>
        {
            publishedFrames.Add(message);
            return ValueTask.CompletedTask;
        });

        using var datagramSubscription = bus.Subscribe<IsobusDatagram>((datagram, _) =>
        {
            publishedDatagrams.Add(datagram);
            return ValueTask.CompletedTask;
        });

        var request = new IsobusMessage(IsobusPgns.Request, 0x50, 0x90, priority: 6, new byte[] { 0x00, 0xEE, 0x00 });
        await router.RouteCanFrameAsync(request.ToCanFrame());

        publishedDatagrams.Should().HaveCount(1);
        publishedFrames.Should().HaveCount(1);

        var handshakeFrame = publishedFrames[0];
        IsobusMessage.TryFromCanFrame(handshakeFrame, out var parsedHandshake).Should().BeTrue();
        parsedHandshake.Pgn.Should().Be(IsobusPgns.AddressClaim);
        parsedHandshake.DestinationAddress.Should().Be(0x50);
        parsedHandshake.SourceAddress.Should().Be(0x90);

        var datagram = publishedDatagrams[0];
        datagram.Topic.Should().Be("isobus/udp");
        IsobusMessage.TryFromDatagram(datagram.Payload, out var parsedDatagram).Should().BeTrue();
        parsedDatagram.Pgn.Should().Be(IsobusPgns.Request);

        var snapshot = router.GetDiagnosticsSnapshot();
        snapshot.HasObservedAddressClaim.Should().BeTrue();
        snapshot.LastRequestedPgn.Should().Be(IsobusPgns.AddressClaim);
    }

    [Fact]
    public async Task RouteDatagramAsync_PublishesCanFrame()
    {
        var bus = new InMemoryEventBus();
        var handshake = new IsobusHandshakeManager(0x40, IsoName, "bridge", "vehicle", new FakeTimeProvider());
        var router = new IsobusRouter(bus, handshake, "isobus/udp", new FakeTimeProvider());

        var captured = new List<CanFrame>();
        using var subscription = bus.Subscribe<CanFrame>((frame, _) =>
        {
            captured.Add(frame);
            return ValueTask.CompletedTask;
        });

        var message = new IsobusMessage(IsobusPgns.DiagnosticMessage1, 0xA0, 0xFF, priority: 4, new byte[] { 0x01, 0x02 });
        await router.RouteDatagramAsync(message.ToDatagram());

        captured.Should().HaveCount(1);
        IsobusMessage.TryFromCanFrame(captured[0], out var parsed).Should().BeTrue();
        parsed.Pgn.Should().Be(IsobusPgns.DiagnosticMessage1);
        parsed.SourceAddress.Should().Be(0xA0);

        var snapshot = router.GetDiagnosticsSnapshot();
        snapshot.LastDiagnosticPgn.Should().Be(IsobusPgns.DiagnosticMessage1);
        snapshot.LastDiagnosticTimestamp.Should().NotBeNull();
    }
}
