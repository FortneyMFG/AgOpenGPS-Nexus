using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using Aog.Core.Mesh;
using Aog.Core.Mesh.RadioBridge;
using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Aog.Core.Mesh.Tests;

public sealed class RadioBridgeTransportTests
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.General)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    [Fact]
    public void EnqueuePublication_ProducesCompressedFrameWithTopicHash()
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2025, 3, 20, 12, 0, 0, TimeSpan.Zero));
        var transport = new RadioBridgeTransport(new RadioBridgeOptions { DeviceId = "bridge:alpha" }, clock);

        var message = new RadioBridgePublicationMessage(
            "aog/live/season/job/coverage",
            MeshDataTier.Coverage,
            clock.GetUtcNow(),
            "device:beta",
            new Dictionary<string, string> { ["origin"] = "mesh" },
            new byte[] { 0x10, 0x20, 0x30 });

        transport.EnqueuePublication(message);

        transport.TryGetNextFrame(out var frameBytes).Should().BeTrue();
        var decoded = RadioBridgeFrameCodec.Decode(frameBytes.Span);

        decoded.Version.Should().Be(1);
        decoded.PayloadType.Should().Be(RadioBridgePayloadType.Data);
        decoded.Flags.Should().HaveFlag(RadioBridgeFrameFlags.Compressed);
        decoded.TopicHash.Should().Be(RadioBridgeTopicHasher.ComputeHash(message.Topic));
        transport.PendingOutboundCount.Should().Be(1);
    }

    [Fact]
    public void EnqueuePublication_WhenFecEnabled_SetsFlagOnFrame()
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2025, 3, 20, 13, 0, 0, TimeSpan.Zero));
        var transport = new RadioBridgeTransport(
            new RadioBridgeOptions { DeviceId = "bridge:alpha", EnableForwardErrorCorrection = true },
            clock);

        var message = new RadioBridgePublicationMessage(
            "aog/live/season/job/diagnostics",
            MeshDataTier.Coverage,
            clock.GetUtcNow(),
            "device:fec",
            null,
            new byte[] { 0x55 });

        transport.EnqueuePublication(message);

        transport.TryGetNextFrame(out var frameBytes).Should().BeTrue();
        var decoded = RadioBridgeFrameCodec.Decode(frameBytes.Span);
        decoded.Flags.Should().HaveFlag(RadioBridgeFrameFlags.ForwardErrorCorrection);
        decoded.Flags.Should().HaveFlag(RadioBridgeFrameFlags.Compressed);
    }

    [Fact]
    public void ProcessInboundAck_RemovesPendingFrame()
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2025, 3, 20, 12, 0, 0, TimeSpan.Zero));
        var transport = new RadioBridgeTransport(new RadioBridgeOptions { DeviceId = "bridge:alpha" }, clock);

        var message = new RadioBridgePublicationMessage(
            "aog/live/season/job/presence",
            MeshDataTier.Presence,
            clock.GetUtcNow(),
            "device:gamma",
            null,
            new byte[] { 0x01 });

        transport.EnqueuePublication(message);
        transport.TryGetNextFrame(out var outboundBytes).Should().BeTrue();
        var outbound = RadioBridgeFrameCodec.Decode(outboundBytes.Span);

        var ackFrame = new RadioBridgeFrame(
            Version: 1,
            PayloadType: RadioBridgePayloadType.Ack,
            Flags: RadioBridgeFrameFlags.None,
            Sequence: outbound.Sequence,
            Ack: outbound.Sequence,
            TopicHash: 0,
            Payload: ReadOnlyMemory<byte>.Empty);

        var result = transport.ProcessInboundFrame(RadioBridgeFrameCodec.Encode(ackFrame));
        result.Status.Should().Be(RadioBridgeProcessStatus.AcknowledgementProcessed);
        transport.PendingOutboundCount.Should().Be(0);
    }

    [Fact]
    public void ProcessInboundData_DeliversPublicationAndQueuesAck()
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2025, 3, 20, 9, 30, 0, TimeSpan.Zero));
        var transport = new RadioBridgeTransport(new RadioBridgeOptions { DeviceId = "bridge:alpha" }, clock);

        RadioBridgePublicationMessage? received = null;
        transport.PublicationReceived += m => received = m;

        var inboundMessage = new RadioBridgePublicationMessage(
            "aog/live/season-2025/job-123/presence",
            MeshDataTier.Presence,
            clock.GetUtcNow(),
            "device:remote",
            new Dictionary<string, string> { ["operator"] = "Ada" },
            new byte[] { 0x42 });

        var frameBytes = EncodeDataFrame(inboundMessage, sequence: 5);
        var result = transport.ProcessInboundFrame(frameBytes);

        result.Status.Should().Be(RadioBridgeProcessStatus.PublicationDelivered);
        result.Sequence.Should().Be(5);
        received.Should().NotBeNull();
        received!.Topic.Should().Be(inboundMessage.Topic);
        received.Payload.Should().Equal(inboundMessage.Payload);

        transport.TryGetNextFrame(out var ackBytes).Should().BeTrue();
        var ackFrame = RadioBridgeFrameCodec.Decode(ackBytes.Span);
        ackFrame.PayloadType.Should().Be(RadioBridgePayloadType.Ack);
        ackFrame.Ack.Should().Be(5);
    }

    [Fact]
    public void ProcessInboundFrame_WithFecPayload_DeliversPublication()
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2025, 3, 20, 9, 45, 0, TimeSpan.Zero));
        var transport = new RadioBridgeTransport(
            new RadioBridgeOptions { DeviceId = "bridge:alpha", EnableForwardErrorCorrection = true },
            clock);

        RadioBridgePublicationMessage? received = null;
        transport.PublicationReceived += m => received = m;

        var inboundMessage = new RadioBridgePublicationMessage(
            "aog/live/season/job/coverage",
            MeshDataTier.Coverage,
            clock.GetUtcNow(),
            "device:lora",
            new Dictionary<string, string> { ["origin"] = "radio" },
            new byte[] { 0x10, 0x20, 0x30 });

        var frameBytes = EncodeDataFrame(inboundMessage, sequence: 12, useForwardErrorCorrection: true);
        var result = transport.ProcessInboundFrame(frameBytes);

        result.Status.Should().Be(RadioBridgeProcessStatus.PublicationDelivered);
        result.Sequence.Should().Be(12);
        received.Should().NotBeNull();
        received!.Payload.Should().Equal(inboundMessage.Payload);

        transport.TryGetNextFrame(out var ackBytes).Should().BeTrue();
        var ackFrame = RadioBridgeFrameCodec.Decode(ackBytes.Span);
        ackFrame.Flags.Should().NotHaveFlag(RadioBridgeFrameFlags.ForwardErrorCorrection);
    }

    [Fact]
    public void TryGetNextFrame_RetransmitsAfterInterval()
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2025, 3, 20, 10, 0, 0, TimeSpan.Zero));
        var options = new RadioBridgeOptions { DeviceId = "bridge:alpha", BaseRetryInterval = TimeSpan.FromMilliseconds(200) };
        var transport = new RadioBridgeTransport(options, clock);

        var message = new RadioBridgePublicationMessage(
            "aog/live/season/job/coverage",
            MeshDataTier.Coverage,
            clock.GetUtcNow(),
            "device:delta",
            null,
            new byte[] { 0x99 });

        transport.EnqueuePublication(message);
        transport.TryGetNextFrame(out var firstBytes).Should().BeTrue();
        var firstFrame = RadioBridgeFrameCodec.Decode(firstBytes.Span);
        firstFrame.Flags.Should().NotHaveFlag(RadioBridgeFrameFlags.Retransmission);

        transport.TryGetNextFrame(out _).Should().BeFalse();

        clock.Advance(TimeSpan.FromMilliseconds(250));
        transport.TryGetNextFrame(out var retryBytes).Should().BeTrue();
        var retryFrame = RadioBridgeFrameCodec.Decode(retryBytes.Span);
        retryFrame.Sequence.Should().Be(firstFrame.Sequence);
        retryFrame.Flags.Should().HaveFlag(RadioBridgeFrameFlags.Retransmission);
    }

    [Fact]
    public void TryGetNextFrame_WhenMaxRetransmissionsExceeded_NotifiesFailure()
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2025, 3, 20, 14, 0, 0, TimeSpan.Zero));
        var options = new RadioBridgeOptions
        {
            DeviceId = "bridge:alpha",
            BaseRetryInterval = TimeSpan.FromMilliseconds(100),
            MaxRetryInterval = TimeSpan.FromMilliseconds(100),
            MaxRetransmissions = 3,
        };

        var transport = new RadioBridgeTransport(options, clock);
        RadioBridgeOutboundFailure? failure = null;
        transport.OutboundDeliveryFailed += f => failure = f;

        var message = new RadioBridgePublicationMessage(
            "aog/live/season/job/fec",
            MeshDataTier.Coverage,
            clock.GetUtcNow(),
            "device:gamma",
            null,
            new byte[] { 0x40 });

        transport.EnqueuePublication(message);
        transport.TryGetNextFrame(out _).Should().BeTrue();

        for (var attempt = 0; attempt < options.MaxRetransmissions; attempt++)
        {
            clock.Advance(options.BaseRetryInterval);
            var result = transport.TryGetNextFrame(out _);
            if (attempt < options.MaxRetransmissions - 1)
            {
                result.Should().BeTrue();
            }
            else
            {
                result.Should().BeFalse();
            }
        }

        failure.Should().NotBeNull();
        failure!.Attempts.Should().Be(options.MaxRetransmissions);
        transport.PendingOutboundCount.Should().Be(0);
    }

    [Fact]
    public void ProcessInboundFrame_WhenCrcInvalid_ReturnsError()
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2025, 3, 20, 8, 0, 0, TimeSpan.Zero));
        var transport = new RadioBridgeTransport(new RadioBridgeOptions { DeviceId = "bridge:alpha" }, clock);

        var message = new RadioBridgePublicationMessage(
            "aog/live/season/job/presence",
            MeshDataTier.Presence,
            clock.GetUtcNow(),
            "device:epsilon",
            null,
            new byte[] { 0x11, 0x22 });

        var frameBytes = EncodeDataFrame(message, sequence: 7);
        var corrupted = frameBytes.ToArray();
        corrupted[^1] ^= 0xFF;

        var result = transport.ProcessInboundFrame(corrupted);
        result.Status.Should().Be(RadioBridgeProcessStatus.CrcMismatch);
    }

    [Fact]
    public void ProcessInboundFrame_DuplicateIsIgnoredAndAcked()
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2025, 3, 20, 7, 0, 0, TimeSpan.Zero));
        var transport = new RadioBridgeTransport(new RadioBridgeOptions { DeviceId = "bridge:alpha" }, clock);

        var inboundMessage = new RadioBridgePublicationMessage(
            "aog/live/season/job/trails",
            MeshDataTier.Trails,
            clock.GetUtcNow(),
            "device:trail",
            null,
            new byte[] { 0xAA, 0xBB });

        var frameBytes = EncodeDataFrame(inboundMessage, sequence: 21);
        var first = transport.ProcessInboundFrame(frameBytes);
        first.Status.Should().Be(RadioBridgeProcessStatus.PublicationDelivered);

        transport.TryGetNextFrame(out _).Should().BeTrue(); // consume ack for first delivery

        RadioBridgePublicationMessage? duplicateReceived = null;
        transport.PublicationReceived += m => duplicateReceived = m;

        var duplicate = transport.ProcessInboundFrame(frameBytes);
        duplicate.Status.Should().Be(RadioBridgeProcessStatus.DuplicateFrame);
        duplicate.Sequence.Should().Be(21);
        duplicateReceived.Should().BeNull();

        transport.TryGetNextFrame(out var duplicateAck).Should().BeTrue();
        var ackFrame = RadioBridgeFrameCodec.Decode(duplicateAck.Span);
        ackFrame.PayloadType.Should().Be(RadioBridgePayloadType.Ack);
        ackFrame.Ack.Should().Be(21);
    }

    private static byte[] EncodeDataFrame(
        RadioBridgePublicationMessage message,
        ushort sequence,
        bool useForwardErrorCorrection = false)
    {
        var envelope = JsonSerializer.SerializeToUtf8Bytes(message, SerializerOptions);
        using var compressedStream = new MemoryStream();
        using (var brotli = new BrotliStream(compressedStream, CompressionLevel.Fastest, leaveOpen: true))
        {
            brotli.Write(envelope);
        }

        var payload = compressedStream.ToArray();
        var flags = RadioBridgeFrameFlags.Compressed;
        if (useForwardErrorCorrection)
        {
            payload = Hamming12_8.Encode(payload);
            flags |= RadioBridgeFrameFlags.ForwardErrorCorrection;
        }

        var frame = new RadioBridgeFrame(
            Version: 1,
            PayloadType: RadioBridgePayloadType.Data,
            Flags: flags,
            Sequence: sequence,
            Ack: 0,
            TopicHash: RadioBridgeTopicHasher.ComputeHash(message.Topic),
            Payload: payload);
        return RadioBridgeFrameCodec.Encode(frame);
    }
}
