using System;

namespace Aog.Core.Mesh.RadioBridge;

/// <summary>
/// Provides helpers for encoding and decoding payload bytes using a Hamming(12,8) forward error correction code.
/// </summary>
internal static class Hamming12_8
{
    /// <summary>
    /// Encodes the specified payload using the Hamming(12,8) code.
    /// </summary>
    /// <param name="payload">Payload to encode.</param>
    /// <returns>Encoded payload containing 12-bit code words stored in little-endian byte pairs.</returns>
    public static byte[] Encode(ReadOnlySpan<byte> payload)
    {
        if (payload.Length == 0)
        {
            return Array.Empty<byte>();
        }

        var output = new byte[payload.Length * 2];
        for (var i = 0; i < payload.Length; i++)
        {
            var encoded = EncodeByte(payload[i]);
            output[i * 2] = (byte)(encoded & 0xFF);
            output[(i * 2) + 1] = (byte)(encoded >> 8);
        }

        return output;
    }

    /// <summary>
    /// Decodes a payload previously encoded with <see cref="Encode"/>.
    /// </summary>
    /// <param name="encoded">Encoded payload.</param>
    /// <returns>Decoded payload bytes.</returns>
    public static byte[] Decode(ReadOnlySpan<byte> encoded)
    {
        if (encoded.Length == 0)
        {
            return Array.Empty<byte>();
        }

        if (encoded.Length % 2 != 0)
        {
            throw new ArgumentException("Encoded payload length must be even for Hamming(12,8).", nameof(encoded));
        }

        var output = new byte[encoded.Length / 2];
        for (var i = 0; i < output.Length; i++)
        {
            var codeWord = (ushort)(encoded[i * 2] | (encoded[(i * 2) + 1] << 8));
            output[i] = DecodeByte(codeWord);
        }

        return output;
    }

    private static ushort EncodeByte(byte value)
    {
        Span<bool> bits = stackalloc bool[13];
        bits[3] = GetBit(value, 0);
        bits[5] = GetBit(value, 1);
        bits[6] = GetBit(value, 2);
        bits[7] = GetBit(value, 3);
        bits[9] = GetBit(value, 4);
        bits[10] = GetBit(value, 5);
        bits[11] = GetBit(value, 6);
        bits[12] = GetBit(value, 7);

        bits[1] = ComputeParity(bits, stackalloc int[] { 3, 5, 7, 9, 11 });
        bits[2] = ComputeParity(bits, stackalloc int[] { 3, 6, 7, 10, 11 });
        bits[4] = ComputeParity(bits, stackalloc int[] { 5, 6, 7, 12 });
        bits[8] = ComputeParity(bits, stackalloc int[] { 9, 10, 11, 12 });

        ushort encoded = 0;
        for (var i = 1; i <= 12; i++)
        {
            if (bits[i])
            {
                encoded |= (ushort)(1 << (i - 1));
            }
        }

        return encoded;
    }

    private static byte DecodeByte(ushort codeWord)
    {
        Span<bool> bits = stackalloc bool[13];
        for (var i = 1; i <= 12; i++)
        {
            bits[i] = ((codeWord >> (i - 1)) & 0x1) != 0;
        }

        var s1 = ComputeParity(bits, stackalloc int[] { 1, 3, 5, 7, 9, 11 });
        var s2 = ComputeParity(bits, stackalloc int[] { 2, 3, 6, 7, 10, 11 });
        var s3 = ComputeParity(bits, stackalloc int[] { 4, 5, 6, 7, 12 });
        var s4 = ComputeParity(bits, stackalloc int[] { 8, 9, 10, 11, 12 });

        var errorPosition = (s1 ? 1 : 0) | (s2 ? 2 : 0) | (s3 ? 4 : 0) | (s4 ? 8 : 0);
        if (errorPosition > 12)
        {
            throw new InvalidOperationException("Detected unrecoverable forward error correction state.");
        }

        if (errorPosition is > 0 and <= 12)
        {
            bits[errorPosition] = !bits[errorPosition];
        }

        byte value = 0;
        if (bits[3])
        {
            value |= 1 << 0;
        }

        if (bits[5])
        {
            value |= 1 << 1;
        }

        if (bits[6])
        {
            value |= 1 << 2;
        }

        if (bits[7])
        {
            value |= 1 << 3;
        }

        if (bits[9])
        {
            value |= 1 << 4;
        }

        if (bits[10])
        {
            value |= 1 << 5;
        }

        if (bits[11])
        {
            value |= 1 << 6;
        }

        if (bits[12])
        {
            value |= 1 << 7;
        }

        return value;
    }

    private static bool ComputeParity(ReadOnlySpan<bool> bits, ReadOnlySpan<int> positions)
    {
        var parity = false;
        foreach (var position in positions)
        {
            parity ^= bits[position];
        }

        return parity;
    }

    private static bool GetBit(byte value, int position) => ((value >> position) & 0x1) != 0;
}
