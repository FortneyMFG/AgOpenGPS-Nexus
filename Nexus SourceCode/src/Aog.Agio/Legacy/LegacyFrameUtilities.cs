using System;

namespace Aog.Agio.Legacy;

/// <summary>
/// Helper methods for working with legacy UDP PGN frames.
/// </summary>
internal static class LegacyFrameUtilities
{
    /// <summary>
    /// Computes the legacy checksum by summing bytes 2..n-2.
    /// </summary>
    public static byte ComputeChecksum(ReadOnlySpan<byte> frame)
    {
        int sum = 0;
        for (var i = 2; i < frame.Length - 1; i++)
        {
            sum += frame[i];
        }

        return unchecked((byte)sum);
    }

    /// <summary>
    /// Validates that the trailing checksum byte matches the computed checksum.
    /// </summary>
    public static bool ValidateChecksum(ReadOnlySpan<byte> frame)
    {
        if (frame.Length == 0)
        {
            return false;
        }

        return ComputeChecksum(frame) == frame[^1];
    }
}
