using System;
using Aog.Agio.Legacy;

namespace Aog.Bridge.Host.AogLink.Serial;

/// <summary>
/// Encodes and decodes AOG-Link serial frames. Frames are wrapped using COBS framing
/// with a trailing <c>0x00</c> delimiter and include a CRC-16/CCITT checksum.
/// </summary>
public static class AogLinkSerialCodec
{
    private const ushort CrcPolynomial = 0x1021; // CRC-16/CCITT polynomial
    private const ushort CrcSeed = 0xFFFF;

    /// <summary>
    /// Encodes the specified payload into a COBS framed buffer with CRC-16 trailer.
    /// </summary>
    public static byte[] Encode(ReadOnlySpan<byte> payload)
    {
        var frameLength = payload.Length + sizeof(ushort);
        Span<byte> frame = frameLength <= 256 ? stackalloc byte[frameLength] : new byte[frameLength];

        payload.CopyTo(frame);
        var crc = ComputeCrc(frame[..payload.Length]);
        frame[payload.Length] = (byte)(crc >> 8);
        frame[payload.Length + 1] = (byte)(crc & 0xFF);

        var encodedLength = LegacySerialFrameCodec.GetMaxEncodedLength(frameLength);
        var encoded = new byte[encodedLength];
        var written = LegacySerialFrameCodec.Encode(frame, encoded);
        Array.Resize(ref encoded, written);
        return encoded;
    }

    /// <summary>
    /// Attempts to decode a frame produced by <see cref="Encode"/>.
    /// </summary>
    public static bool TryDecode(ReadOnlySpan<byte> encoded, out byte[] payload)
    {
        payload = Array.Empty<byte>();

        Span<byte> buffer = encoded.Length <= 512 ? stackalloc byte[512] : new byte[encoded.Length];

        if (!LegacySerialFrameCodec.TryDecode(encoded, buffer, out var bytesWritten))
        {
            return false;
        }

        if (bytesWritten < sizeof(ushort))
        {
            return false;
        }

        var payloadSpan = buffer[..(bytesWritten - 2)];
        var crcSpan = buffer[(bytesWritten - 2)..bytesWritten];

        var expected = ComputeCrc(payloadSpan);
        var actual = (ushort)((crcSpan[0] << 8) | crcSpan[1]);

        if (expected != actual)
        {
            return false;
        }

        payload = payloadSpan.ToArray();
        return true;
    }

    private static ushort ComputeCrc(ReadOnlySpan<byte> buffer)
    {
        ushort crc = CrcSeed;

        foreach (var b in buffer)
        {
            crc ^= (ushort)(b << 8);
            for (var i = 0; i < 8; i++)
            {
                crc = (crc & 0x8000) != 0
                    ? (ushort)((crc << 1) ^ CrcPolynomial)
                    : (ushort)(crc << 1);
            }
        }

        return crc;
    }
}
