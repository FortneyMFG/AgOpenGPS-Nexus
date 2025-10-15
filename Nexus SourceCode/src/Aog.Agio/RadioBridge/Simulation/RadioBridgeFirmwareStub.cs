using System;
using System.Collections.Generic;
using Aog.Core.Mesh;
using Aog.Core.Mesh.RadioBridge;

namespace Aog.Agio.RadioBridge.Simulation;

/// <summary>
/// Helper that simulates basic firmware behaviour for integration tests.
/// </summary>
public sealed class RadioBridgeFirmwareStub
{
    private readonly RadioBridgeTransport _transport;

    /// <summary>
    /// Initializes a new instance of the <see cref="RadioBridgeFirmwareStub"/> class.
    /// </summary>
    public RadioBridgeFirmwareStub(string deviceId)
    {
        _transport = new RadioBridgeTransport(new RadioBridgeOptions { DeviceId = deviceId });
    }

    /// <summary>
    /// Builds an acknowledgement frame for the specified outbound frame.
    /// </summary>
    public static ReadOnlyMemory<byte> CreateAck(ReadOnlyMemory<byte> outboundFrame)
    {
        var frame = RadioBridgeFrameCodec.Decode(outboundFrame.Span);
        var ack = new RadioBridgeFrame(
            Version: frame.Version,
            PayloadType: RadioBridgePayloadType.Ack,
            Flags: RadioBridgeFrameFlags.None,
            Sequence: frame.Sequence,
            Ack: frame.Sequence,
            TopicHash: 0,
            Payload: ReadOnlyMemory<byte>.Empty);
        return RadioBridgeFrameCodec.Encode(ack);
    }

    /// <summary>
    /// Creates an encoded publication frame that represents a mesh payload delivered by firmware.
    /// </summary>
    public ReadOnlyMemory<byte> CreatePublication(
        string topic,
        MeshDataTier tier,
        DateTimeOffset publishedAt,
        string publisherDeviceId,
        IReadOnlyDictionary<string, string>? metadata,
        byte[] payload)
    {
        var message = new RadioBridgePublicationMessage(topic, tier, publishedAt, publisherDeviceId, metadata, payload);
        _transport.EnqueuePublication(message);
        if (!_transport.TryGetNextFrame(out var frame))
        {
            throw new InvalidOperationException("Failed to build publication frame for firmware stub.");
        }

        return frame.ToArray();
    }
}
