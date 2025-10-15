using System;
using System.Buffers.Binary;

namespace Aog.Agio.AogLink;

/// <summary>
/// Serialises and deserialises AOG-Link frames.
/// </summary>
public static class AogLinkFrameCodec
{
    private const int HeaderSize = 10;

    public static byte[] Encode(AogLinkFrame frame)
    {
        var buffer = new byte[HeaderSize + frame.Payload.Length];
        WriteHeader(buffer.AsSpan(0, HeaderSize), frame.Header);
        frame.Payload.Span.CopyTo(buffer.AsSpan(HeaderSize));
        return buffer;
    }

    public static AogLinkFrame Decode(ReadOnlySpan<byte> datagram)
    {
        if (datagram.Length < HeaderSize)
        {
            throw new InvalidOperationException("Datagram too short to contain an AOG-Link header.");
        }

        var header = ReadHeader(datagram.Slice(0, HeaderSize));
        var payload = datagram.Slice(HeaderSize).ToArray();
        header.Validate(payload.Length);
        return new AogLinkFrame(header, payload);
    }

    private static void WriteHeader(Span<byte> destination, AogLinkFrameHeader header)
    {
        destination[0] = header.Version;
        destination[1] = header.MessageClass;
        BinaryPrimitives.WriteUInt16BigEndian(destination.Slice(2, 2), header.MessageType);
        BinaryPrimitives.WriteUInt16BigEndian(destination.Slice(4, 2), header.Sequence);
        destination[6] = header.Source;
        destination[7] = header.Destination;
        BinaryPrimitives.WriteUInt16BigEndian(destination.Slice(8, 2), header.PayloadLength);
    }

    private static AogLinkFrameHeader ReadHeader(ReadOnlySpan<byte> source)
    {
        var version = source[0];
        var messageClass = source[1];
        var messageType = BinaryPrimitives.ReadUInt16BigEndian(source.Slice(2, 2));
        var sequence = BinaryPrimitives.ReadUInt16BigEndian(source.Slice(4, 2));
        var sourceAddress = source[6];
        var destination = source[7];
        var length = BinaryPrimitives.ReadUInt16BigEndian(source.Slice(8, 2));
        return new AogLinkFrameHeader(version, messageClass, messageType, sequence, sourceAddress, destination, length);
    }
}
