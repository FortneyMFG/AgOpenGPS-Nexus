using System;
using Aog.Core.V1;

namespace Aog.Agio.Legacy;

/// <summary>
/// Encodes legacy auto-steer PGNs that carry steering and section commands.
/// </summary>
public sealed class LegacyAutoSteerCodec
{
    /// <summary>
    /// Source address used by the legacy PC host when emitting auto-steer PGNs.
    /// </summary>
    public const byte AutoSteerSourceAddress = 0x7F;

    /// <summary>
    /// PGN emitted by the PC host to command steering and section relays.
    /// </summary>
    public const byte AutoSteerDataPgn = 0xFE;

    /// <summary>
    /// Length of the legacy auto-steer payload (excluding header and checksum).
    /// </summary>
    public const int AutoSteerPayloadLength = 8;

    /// <summary>
    /// Total frame length including sync bytes and checksum.
    /// </summary>
    public const int AutoSteerFrameLength = 2 + 1 + 1 + 1 + AutoSteerPayloadLength + 1;

    /// <summary>
    /// Encodes the supplied steer command and section mask into the legacy PGN format.
    /// </summary>
    public byte[] EncodeAutoSteerFrame(SteerCmd steer, SectionMask? sections = null)
    {
        ArgumentNullException.ThrowIfNull(steer);

        sections ??= new SectionMask();

        var buffer = new byte[AutoSteerFrameLength];
        buffer[0] = LegacyPoseCodec.Sync0;
        buffer[1] = LegacyPoseCodec.Sync1;
        buffer[2] = AutoSteerSourceAddress;
        buffer[3] = AutoSteerDataPgn;
        buffer[4] = AutoSteerPayloadLength;

        var payload = buffer.AsSpan(5, AutoSteerPayloadLength);

        // Speed and light-bar distance are not exposed via the current contracts; leave zeroed.
        payload[0] = 0;
        payload[1] = 0;
        payload[2] = steer.Enable ? (byte)1 : (byte)0;

        var steerAngle = EncodeSteerAngle(steer.TargetWheelAngleDeg, steer.Enable);
        payload[3] = (byte)(steerAngle & 0xFF);
        payload[4] = (byte)((steerAngle >> 8) & 0xFF);

        payload[5] = 0;

        var sectionCount = Math.Clamp(sections.SectionCount, 0, 16);
        uint mask = 0u;

        if (sectionCount > 0)
        {
            var allowedMask = BuildAllowedMask(sectionCount);
            mask = sections.Mask & allowedMask & 0xFFFFu;
        }

        payload[6] = (byte)(mask & 0xFF);
        payload[7] = (byte)((mask >> 8) & 0xFF);

        LegacyChecksum.Write(buffer);
        return buffer;
    }

    private static short EncodeSteerAngle(double targetAngleDeg, bool enabled)
    {
        if (!enabled || double.IsNaN(targetAngleDeg) || double.IsInfinity(targetAngleDeg))
        {
            return 0;
        }

        var scaled = Math.Round(targetAngleDeg * 100d, MidpointRounding.AwayFromZero);
        if (scaled >= short.MaxValue)
        {
            return short.MaxValue;
        }

        if (scaled <= short.MinValue)
        {
            return short.MinValue;
        }

        return (short)scaled;
    }

    private static uint BuildAllowedMask(int sectionCount)
    {
        if (sectionCount <= 0)
        {
            return 0u;
        }

        if (sectionCount >= 32)
        {
            return uint.MaxValue;
        }

        // Equivalent to SectionMask.SectionMaskForCount(sectionCount) when available.
        return (1u << sectionCount) - 1u;
    }
}
