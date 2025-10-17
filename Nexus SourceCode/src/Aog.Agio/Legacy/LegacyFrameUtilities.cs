using System;

namespace Aog.Agio.Legacy;

/// <summary>
/// Helper methods for working with legacy UDP PGN frames.
/// </summary>
internal static class LegacyFrameUtilities
{
    private const int SyncByteCount = 2;
    private const int HeaderLength = 3;
    private const int ChecksumLength = 1;
    private const int MinimumFrameLength = SyncByteCount + HeaderLength + ChecksumLength;

    /// <summary>
    /// Computes the legacy checksum by summing bytes 2..n-2.
    /// </summary>
    /// <param name="frame">Frame to process.</param>
    /// <returns>Computed checksum byte.</returns>
    public static byte ComputeChecksum(ReadOnlySpan<byte> frame)
    {
        var sum = 0;
        for (var i = SyncByteCount; i < frame.Length - ChecksumLength; i++)
        {
            sum += frame[i];
        }

        return unchecked((byte)sum);
    }

    /// <summary>
    /// Validates that the trailing checksum byte matches the computed checksum.
    /// </summary>
    /// <param name="frame">Frame to validate.</param>
    /// <returns><c>true</c> when the checksum is valid.</returns>
    public static bool ValidateChecksum(ReadOnlySpan<byte> frame)
    {
        if (frame.Length < MinimumFrameLength)
        {
            return false;
        }

        return ComputeChecksum(frame) == frame[^ChecksumLength];
    }
}
