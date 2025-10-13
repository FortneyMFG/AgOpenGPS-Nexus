using System;

namespace Aog.Agio.Legacy;

/// <summary>
/// Encodes and decodes legacy serial frames using COBS framing and the legacy checksum.
/// </summary>
public static class LegacySerialFrameCodec
{
    /// <summary>
    /// Sentinel byte that terminates a COBS frame on the wire.
    /// </summary>
    public const byte FrameDelimiter = 0x00;

    /// <summary>
    /// Calculates the maximum encoded length for a frame with the specified payload length.
    /// </summary>
    /// <param name="frameLength">Length of the unencoded frame.</param>
    /// <returns>Maximum encoded length including delimiter.</returns>
    public static int GetMaxEncodedLength(int frameLength)
    {
        if (frameLength < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(frameLength));
        }

        var blockCount = frameLength / 254;
        return frameLength + blockCount + 2;
    }

    /// <summary>
    /// COBS-encodes the supplied frame into the destination span.
    /// </summary>
    /// <param name="frame">Frame to encode (including checksum byte).</param>
    /// <param name="destination">Destination span that receives the encoded frame.</param>
    /// <returns>Number of bytes written to <paramref name="destination"/>.</returns>
    public static int Encode(ReadOnlySpan<byte> frame, Span<byte> destination)
    {
        var required = GetMaxEncodedLength(frame.Length);
        if (destination.Length < required)
        {
            throw new ArgumentException("Destination span is too small for the encoded frame.", nameof(destination));
        }

        var writeIndex = 0;
        var codeIndex = 0;
        byte code = 1;

        destination[codeIndex] = 0; // Reserve space for the first code byte.
        writeIndex++;

        for (var i = 0; i < frame.Length; i++)
        {
            var value = frame[i];
            if (value == 0)
            {
                destination[codeIndex] = code;
                code = 1;
                codeIndex = writeIndex;
                destination[writeIndex++] = 0; // Placeholder for the next code byte.
                continue;
            }

            destination[writeIndex++] = value;
            code++;

            if (code == 0xFF)
            {
                destination[codeIndex] = code;
                code = 1;
                codeIndex = writeIndex;
                destination[writeIndex++] = 0; // Start a new block.
            }
        }

        destination[codeIndex] = code;
        destination[writeIndex++] = FrameDelimiter;
        return writeIndex;
    }

    /// <summary>
    /// Decodes a COBS-encoded frame.
    /// </summary>
    /// <param name="encodedFrame">Encoded frame including the trailing delimiter.</param>
    /// <param name="destination">Destination span to receive the decoded frame.</param>
    /// <param name="bytesWritten">Number of bytes written to <paramref name="destination"/>.</param>
    /// <returns><c>true</c> when decoding succeeds.</returns>
    public static bool TryDecode(ReadOnlySpan<byte> encodedFrame, Span<byte> destination, out int bytesWritten)
    {
        bytesWritten = 0;

        if (encodedFrame.Length < 2 || encodedFrame[^1] != FrameDelimiter)
        {
            return false;
        }

        var source = encodedFrame[..^1];
        var readIndex = 0;

        while (readIndex < source.Length)
        {
            var code = source[readIndex];
            if (code == 0)
            {
                return false;
            }

            readIndex++;
            var copyCount = code - 1;

            if (copyCount > source.Length - readIndex)
            {
                return false;
            }

            if (bytesWritten + copyCount > destination.Length)
            {
                return false;
            }

            for (var i = 0; i < copyCount; i++)
            {
                destination[bytesWritten++] = source[readIndex++];
            }

            if (code < 0xFF && readIndex < source.Length)
            {
                if (bytesWritten >= destination.Length)
                {
                    return false;
                }

                destination[bytesWritten++] = 0;
            }
        }

        return true;
    }
}
