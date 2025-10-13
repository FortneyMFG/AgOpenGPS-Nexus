using System;
using Aog.Core.V1;
using Aog.Plugins.Isobus;
using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Aog.Plugins.Tests.Isobus;

public sealed class IsobusHandshakeManagerTests
{
    private static readonly byte[] IsoName = { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08 };

    [Fact]
    public void BuildAddressClaim_UsesConfiguredIdentity()
    {
        var manager = new IsobusHandshakeManager(0x90, IsoName, "bridge", "vehicle", new FakeTimeProvider());
        var frame = manager.BuildAddressClaim();
        frame.IsExtendedId.Should().BeTrue();
        IsobusMessage.TryFromCanFrame(frame, out var parsed).Should().BeTrue();
        parsed.Pgn.Should().Be(IsobusPgns.AddressClaim);
        parsed.SourceAddress.Should().Be(0x90);
    }

    [Fact]
    public void TryCreateHandshakeResponse_IgnoresUnrelatedMessages()
    {
        var manager = new IsobusHandshakeManager(0x20, IsoName, "bridge", "vehicle", new FakeTimeProvider());
        var message = new IsobusMessage(IsobusPgns.DiagnosticMessage1, 0x81, 0xFF, priority: 3, Array.Empty<byte>());
        manager.TryCreateHandshakeResponse(message, out _).Should().BeFalse();
    }

    [Fact]
    public void TryCreateHandshakeResponse_ReturnsAddressClaimWhenRequested()
    {
        var time = new FakeTimeProvider();
        var manager = new IsobusHandshakeManager(0x30, IsoName, "bridge", "vehicle", time);
        var requestPayload = new byte[] { 0x00, 0xEE, 0x00 };
        var request = new IsobusMessage(IsobusPgns.Request, 0xA1, 0x30, priority: 6, requestPayload);

        var responded = manager.TryCreateHandshakeResponse(request, out var frame);
        responded.Should().BeTrue();
        frame.IsExtendedId.Should().BeTrue();
        IsobusMessage.TryFromCanFrame(frame, out var parsed).Should().BeTrue();
        parsed.Pgn.Should().Be(IsobusPgns.AddressClaim);
        parsed.SourceAddress.Should().Be(0x30);
        parsed.DestinationAddress.Should().Be(0xA1);
    }
}
