using System;

namespace Aog.Agio.Legacy;

/// <summary>
/// Provides helpers for computing and validating legacy AgIO checksums.
/// </summary>
public static class LegacyChecksum
{
    /// <summary>
    /// Computes the legacy checksum for the supplied frame.
    /// </summary>
    /// <param name="frame">Frame containing sync bytes, header, payload, and checksum slot.</param>
    /// <returns>8-bit checksum of bytes 2 through <c>length - 2</c>.</returns>
    /// <exception cref="ArgumentException">Thrown when the frame is too short to contain a checksum.</exception>
    public static byte Compute(ReadOnlySpan<byte> frame)
    {
        if (frame.Length < 3)
        {
            throw new ArgumentException("Frame must include sync bytes and a checksum slot.", nameof(frame));
        }

        int sum = 0;
        for (var i = 2; i < frame.Length - 1; i++)
        {
            sum += frame[i];
        }

        return unchecked((byte)sum);
    }

    /// <summary>
    /// Writes the checksum into the final byte of the frame.
    /// </summary>
    /// <param name="frame">Frame to update.</param>
    public static void Write(Span<byte> frame)
    {
        frame[^1] = Compute(frame);
    }

    /// <summary>
    /// Validates that the checksum embedded in the frame matches the expected value.
    /// </summary>
    /// <param name="frame">Frame to validate.</param>
    /// <returns><c>true</c> when the checksum matches.</returns>
    public static bool Validate(ReadOnlySpan<byte> frame)
    {
        if (frame.Length < 3)
        {
            return false;
        }

        return Compute(frame) == frame[^1];
    }
}
