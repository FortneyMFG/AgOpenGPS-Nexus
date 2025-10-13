using System;
using Aog.Core.V1;
using Aog.Plugins.Isobus;
using FluentAssertions;
using Google.Protobuf;
using Xunit;

namespace Aog.Plugins.Tests.Isobus;

public sealed class IsobusMessageTests
{
    [Fact]
    public void TryFromCanFrame_ParsesExtendedIdentifier()
    {
        var payload = ByteString.CopyFrom(new byte[] { 0xAA, 0xBB, 0xCC });
        var frame = new CanFrame
        {
            ArbitrationId = 0x18EEFF33, // priority 6, PGN 0x00EE00, source 0x33
            IsExtendedId = true,
            Payload = payload
        };

        var success = IsobusMessage.TryFromCanFrame(frame, out var message);
        success.Should().BeTrue();
        message.Pgn.Should().Be(IsobusPgns.AddressClaim);
        message.SourceAddress.Should().Be(0x33);
        message.DestinationAddress.Should().Be(0xFF);
        message.Priority.Should().Be(6);
        message.Data.ToArray().Should().Equal(payload.ToByteArray());
    }

    [Fact]
    public void ToCanFrame_RoundTripsFields()
    {
        var data = new byte[] { 0x01, 0x02, 0x03 };
        var message = new IsobusMessage(0x00EA00, 0x80, 0x05, priority: 3, data);

        var frame = message.ToCanFrame();
        frame.IsExtendedId.Should().BeTrue();
        IsobusMessage.TryFromCanFrame(frame, out var reparsed).Should().BeTrue();
        reparsed.Pgn.Should().Be(message.Pgn);
        reparsed.SourceAddress.Should().Be(message.SourceAddress);
        reparsed.DestinationAddress.Should().Be(message.DestinationAddress);
        reparsed.Priority.Should().Be(message.Priority);
        reparsed.Data.ToArray().Should().Equal(data);
    }

    [Fact]
    public void Datagram_RoundTrips()
    {
        var data = new byte[] { 0x10, 0x20, 0x30, 0x40 };
        var message = new IsobusMessage(0x00FECA, 0x90, 0xFF, priority: 4, data);
        var datagram = message.ToDatagram();
        IsobusMessage.TryFromDatagram(datagram, out var reparsed).Should().BeTrue();
        reparsed.Pgn.Should().Be(message.Pgn);
        reparsed.SourceAddress.Should().Be(message.SourceAddress);
        reparsed.DestinationAddress.Should().Be(message.DestinationAddress);
        reparsed.Priority.Should().Be(message.Priority);
        reparsed.Data.ToArray().Should().Equal(data);
    }

    [Fact]
    public void TryFromDatagram_InvalidLength_ReturnsFalse()
    {
        var datagram = new byte[] { 0x00, 0xEA, 0x00, 0x00, 0x12, 0x34, 0x02, 0x08, 0xAA };
        IsobusMessage.TryFromDatagram(datagram, out _).Should().BeFalse();
    }
}
