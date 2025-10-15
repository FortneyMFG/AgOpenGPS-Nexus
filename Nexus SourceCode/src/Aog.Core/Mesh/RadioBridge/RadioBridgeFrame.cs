using System;
using System.Buffers.Binary;

namespace Aog.Core.Mesh.RadioBridge;

/// <summary>
/// Flags that describe radio bridge frame behaviour.
/// </summary>
[Flags]
public enum RadioBridgeFrameFlags : byte
{
    /// <summary>
    /// No special behaviour.
    /// </summary>
    None = 0,

    /// <summary>
    /// Indicates that the payload is compressed using Brotli.
    /// </summary>
    Compressed = 1 << 0,

    /// <summary>
    /// Indicates that the frame is a retransmission of a previously sent payload.
    /// </summary>
    Retransmission = 1 << 1,

    /// <summary>
    /// Indicates that forward error correction blocks are attached. The current
    /// implementation does not emit FEC but preserves the bit for future use.
    /// </summary>
    ForwardErrorCorrection = 1 << 2,
}

/// <summary>
/// Supported radio bridge payload types.
/// </summary>
public enum RadioBridgePayloadType : byte
{
    /// <summary>
    /// Encoded <see cref="RadioBridgePublicationMessage"/> payload.
    /// </summary>
    Data = 0,

    /// <summary>
    /// Acknowledgement frame without payload.
    /// </summary>
    Ack = 1,

    /// <summary>
    /// Control message reserved for future negotiation flows.
    /// </summary>
    Control = 2,
}

/// <summary>
/// Represents a decoded radio bridge frame.
/// </summary>
public readonly record struct RadioBridgeFrame(
    byte Version,
    RadioBridgePayloadType PayloadType,
    RadioBridgeFrameFlags Flags,
    ushort Sequence,
    ushort Ack,
    uint TopicHash,
    ReadOnlyMemory<byte> Payload);

/// <summary>
/// Provides helpers to encode and decode radio bridge frames.
/// </summary>
public static class RadioBridgeFrameCodec
{
    private const int HeaderLength = 14;
    private const int FooterLength = 2;

    /// <summary>
    /// Encodes the specified frame into a new byte array containing the binary frame.
    /// </summary>
    /// <param name="frame">Frame to encode.</param>
    /// <returns>Binary representation of the frame including CRC16.</returns>
    public static byte[] Encode(RadioBridgeFrame frame)
    {
        var payloadLength = frame.Payload.Length;
        var buffer = new byte[HeaderLength + payloadLength + FooterLength];
        var span = buffer.AsSpan();

        span[0] = frame.Version;
        span[1] = (byte)frame.PayloadType;
        span[2] = (byte)frame.Flags;
        span[3] = 0; // reserved
        BinaryPrimitives.WriteUInt16BigEndian(span[4..6], frame.Sequence);
        BinaryPrimitives.WriteUInt16BigEndian(span[6..8], frame.Ack);
        BinaryPrimitives.WriteUInt32BigEndian(span[8..12], frame.TopicHash);
        BinaryPrimitives.WriteUInt16BigEndian(span[12..14], (ushort)payloadLength);
        frame.Payload.Span.CopyTo(span[HeaderLength..(HeaderLength + payloadLength)]);

        var crc = Crc16Ccitt(span[..(HeaderLength + payloadLength)]);
        BinaryPrimitives.WriteUInt16BigEndian(span[(HeaderLength + payloadLength)..], crc);
        return buffer;
    }

    /// <summary>
    /// Decodes a frame from the provided binary span.
    /// </summary>
    /// <param name="buffer">Binary frame payload.</param>
    /// <returns>The decoded <see cref="RadioBridgeFrame"/>.</returns>
    public static RadioBridgeFrame Decode(ReadOnlySpan<byte> buffer)
    {
        if (buffer.Length < HeaderLength + FooterLength)
        {
            throw new ArgumentException("Frame is too short to contain a valid header.", nameof(buffer));
        }

        var header = buffer[..HeaderLength];
        var payloadLength = BinaryPrimitives.ReadUInt16BigEndian(header[12..14]);
        if (HeaderLength + payloadLength + FooterLength != buffer.Length)
        {
            throw new ArgumentException("Frame length does not match payload length field.", nameof(buffer));
        }

        var expectedCrc = BinaryPrimitives.ReadUInt16BigEndian(buffer[(HeaderLength + payloadLength)..]);
        var computedCrc = Crc16Ccitt(buffer[..(HeaderLength + payloadLength)]);
        if (computedCrc != expectedCrc)
        {
            throw new InvalidOperationException("CRC16 validation failed for radio frame.");
        }

        var payload = payloadLength == 0
            ? ReadOnlyMemory<byte>.Empty
            : buffer.Slice(HeaderLength, payloadLength).ToArray();

        return new RadioBridgeFrame(
            Version: buffer[0],
            PayloadType: (RadioBridgePayloadType)buffer[1],
            Flags: (RadioBridgeFrameFlags)buffer[2],
            Sequence: BinaryPrimitives.ReadUInt16BigEndian(buffer[4..6]),
            Ack: BinaryPrimitives.ReadUInt16BigEndian(buffer[6..8]),
            TopicHash: BinaryPrimitives.ReadUInt32BigEndian(buffer[8..12]),
            Payload: payload);
    }

    /// <summary>
    /// Computes the CCITT 16-bit CRC for the given buffer.
    /// </summary>
    private static ushort Crc16Ccitt(ReadOnlySpan<byte> buffer)
    {
        const ushort polynomial = 0x1021;
        ushort crc = 0xFFFF;

        foreach (var b in buffer)
        {
            crc ^= (ushort)(b << 8);
            for (var i = 0; i < 8; i++)
            {
                if ((crc & 0x8000) != 0)
                {
                    crc = (ushort)((crc << 1) ^ polynomial);
                }
                else
                {
                    crc <<= 1;
                }
            }
        }

        return crc;
    }
}
