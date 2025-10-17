using System;
using System.Buffers.Binary;
using Aog.Core.V1;

namespace Aog.Agio.Legacy;

/// <summary>
/// Encodes and decodes legacy UDP PGNs that carry steering commands and feedback.
/// </summary>
public sealed class LegacySteerCodec
{
    /// <summary>
    /// Legacy PGN that transports steering commands and section bitmasks.
    /// </summary>
    public const byte SteerCommandPgn = 0xFE;

    /// <summary>
    /// Legacy PGN that transports steering feedback from hardware.
    /// </summary>
    public const byte SteerStatePgn = 0xFD;

    private const byte CommandSourceAddress = 0x7F;
    private const byte StateSourceAddress = 0x7E;
    private const byte EngagedBit = 0x01;
    private const int CommandPayloadLength = 8;
    private const int StatePayloadLength = 8;
    private const int CommandFrameLength = 2 /* sync */ + 1 /* src */ + 1 /* pgn */ + 1 /* len */ + CommandPayloadLength + 1 /* checksum */;
    private const int StateFrameLength = 2 + 1 + 1 + 1 + StatePayloadLength + 1;

    /// <summary>
    /// Attempts to decode a steering command PGN into a typed command and section mask.
    /// </summary>
    public bool TryDecodeSteerCommand(
        ReadOnlySpan<byte> datagram,
        out SteerCmd command,
        out LegacySteerCommandMetadata metadata,
        out SectionMask sectionMask)
    {
        command = new SteerCmd();
        metadata = new LegacySteerCommandMetadata();
        sectionMask = new SectionMask();

        if (datagram.Length != CommandFrameLength)
        {
            return false;
        }

        if (datagram[0] != LegacyPoseCodec.Sync0 || datagram[1] != LegacyPoseCodec.Sync1)
        {
            return false;
        }

        if (datagram[2] != CommandSourceAddress || datagram[3] != SteerCommandPgn)
        {
            return false;
        }

        if (datagram[4] != CommandPayloadLength)
        {
            return false;
        }

        if (!LegacyChecksum.Validate(datagram))
        {
            return false;
        }

        var speedHundredths = BinaryPrimitives.ReadUInt16LittleEndian(datagram.Slice(5, 2));
        var rawGuidanceStatus = datagram[7];
        var steerHundredths = BinaryPrimitives.ReadInt16LittleEndian(datagram.Slice(8, 2));
        var tramControl = datagram[10];
        var sectionsLow = datagram[11];
        var sectionsHigh = datagram[12];

        var speedKph = speedHundredths * 0.01;

        metadata = new LegacySteerCommandMetadata
        {
            SpeedKph = speedKph,
            CurrentSpeedMps = speedKph / 3.6,
            GuidanceStatus = rawGuidanceStatus,
            TramControl = tramControl,
        };

        const byte EngagedBit = 0x01;

        command.TargetWheelAngleDeg = steerHundredths / 100.0;
        command.Enable = (rawGuidanceStatus & EngagedBit) != 0;
        command.FeedForward = 0;
        command.ControllerOutput = 0;

        sectionMask.SectionCount = 16;
        sectionMask.Mask = (uint)(sectionsLow | (sectionsHigh << 8));

        return true;
    }

    /// <summary>
    /// Encodes a typed steering command (and optional section mask) into its legacy PGN representation.
    /// </summary>
    public byte[] EncodeSteerCommand(
        SteerCmd command,
        SectionMask? sectionMask = null,
        LegacySteerCommandMetadata? metadata = null)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        metadata ??= new LegacySteerCommandMetadata();

        var buffer = new byte[CommandFrameLength];
        buffer[0] = LegacyPoseCodec.Sync0;
        buffer[1] = LegacyPoseCodec.Sync1;
        buffer[2] = CommandSourceAddress;
        buffer[3] = SteerCommandPgn;
        buffer[4] = CommandPayloadLength;

        double speedKph;
        if (metadata.CurrentSpeedMps is double speedMps && double.IsFinite(speedMps))
        {
            speedKph = speedMps * 3.6;
        }
        else
        {
            speedKph = metadata.SpeedKph;
        }

        var speedHundredths = (ushort)Math.Clamp((int)Math.Round(speedKph * 100.0), 0, ushort.MaxValue);
        BinaryPrimitives.WriteUInt16LittleEndian(buffer.AsSpan(5, 2), speedHundredths);

        var status = metadata.GuidanceStatus;

        status = command.Enable
            ? (byte)(status | EngagedBit)
            : (byte)(status & ~EngagedBit);

        buffer[7] = status;

        var steerHundredths = command.Enable
            ? (short)Math.Clamp(Math.Round(command.TargetWheelAngleDeg * 100.0), short.MinValue, short.MaxValue)
            : (short)0;
        BinaryPrimitives.WriteInt16LittleEndian(buffer.AsSpan(8, 2), steerHundredths);

        buffer[10] = metadata.TramControl;

        uint mask = 0;

        if (sectionMask is not null)
        {
            var sectionCount = sectionMask.SectionCount;

            if (sectionCount > 32)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(sectionMask.SectionCount),
                    sectionCount,
                    "SectionCount must not exceed 32.");
            }

            // Legacy PGN supports only 16 bits — hard cap it here
            var cappedCount = Math.Min(sectionCount, 16);

            // If count is zero, mask must be zero
            if (cappedCount == 0)
            {
                mask = 0;
            }
            else
            {
                // Build a mask of allowed bits
                var allowedMask = (1u << cappedCount) - 1u;

                // Apply and clamp to 16 bits total
                mask = sectionMask.Mask & allowedMask & 0xFFFFu;
            }
        }

        buffer[11] = (byte)(mask & 0xFF);
        buffer[12] = (byte)((mask >> 8) & 0xFF);

        LegacyChecksum.Write(buffer);
        return buffer;
    }

    /// <summary>
    /// Attempts to decode a steering feedback PGN into a typed steer state.
    /// </summary>
    public bool TryDecodeSteerState(
        ReadOnlySpan<byte> datagram,
        out SteerState state,
        out LegacySteerStateMetadata metadata)
    {
        state = new SteerState();
        metadata = new LegacySteerStateMetadata();

        if (datagram.Length != StateFrameLength)
        {
            return false;
        }

        if (datagram[0] != LegacyPoseCodec.Sync0 || datagram[1] != LegacyPoseCodec.Sync1)
        {
            return false;
        }

        if (datagram[2] != StateSourceAddress || datagram[3] != SteerStatePgn)
        {
            return false;
        }

        if (datagram[4] != StatePayloadLength)
        {
            return false;
        }

        if (!LegacyChecksum.Validate(datagram))
        {
            return false;
        }

        var actualHundredths = BinaryPrimitives.ReadInt16LittleEndian(datagram.Slice(5, 2));
        var headingHundredths = BinaryPrimitives.ReadUInt16LittleEndian(datagram.Slice(7, 2));
        var rollHundredths = BinaryPrimitives.ReadInt16LittleEndian(datagram.Slice(9, 2));
        var switchByte = datagram[11];
        var pwm = datagram[12];

        var isWorkSwitchOn = (switchByte & 0x01) != 0;
        var isSteerSwitchOn = (switchByte & 0x02) != 0;
        var isRemoteSwitchOn = (switchByte & 0x04) != 0;

        metadata = new LegacySteerStateMetadata
        {
            HeadingDeg = headingHundredths / 100.0,
            RollDeg = rollHundredths / 100.0,
            IsWorkSwitchOn = isWorkSwitchOn,
            IsSteerSwitchOn = isSteerSwitchOn,
            IsRemoteSwitchOn = isRemoteSwitchOn,
            SwitchByte = switchByte,
            RawPwm = pwm,
        };

        state.MeasuredWheelAngleDeg = actualHundredths / 100.0;
        state.AppliedEffort = pwm / 255.0;
        state.LateralErrorM = 0;
        state.HeadingErrorRad = 0;
        state.Engaged = isSteerSwitchOn;

        return true;
    }

    /// <summary>
    /// Encodes a steering feedback snapshot into the legacy PGN representation.
    /// </summary>
    public byte[] EncodeSteerState(SteerState state, LegacySteerStateMetadata? metadata = null)
    {
        if (state is null)
        {
            throw new ArgumentNullException(nameof(state));
        }

        metadata ??= new LegacySteerStateMetadata();

        var buffer = new byte[StateFrameLength];
        buffer[0] = LegacyPoseCodec.Sync0;
        buffer[1] = LegacyPoseCodec.Sync1;
        buffer[2] = StateSourceAddress;
        buffer[3] = SteerStatePgn;
        buffer[4] = StatePayloadLength;

        var actualHundredths = (short)Math.Clamp(Math.Round(state.MeasuredWheelAngleDeg * 100.0), short.MinValue, short.MaxValue);
        BinaryPrimitives.WriteInt16LittleEndian(buffer.AsSpan(5, 2), actualHundredths);

        var headingHundredths = (ushort)Math.Clamp((int)Math.Round(metadata.HeadingDeg * 100.0), 0, ushort.MaxValue);
        BinaryPrimitives.WriteUInt16LittleEndian(buffer.AsSpan(7, 2), headingHundredths);

        var rollHundredths = (short)Math.Clamp(Math.Round(metadata.RollDeg * 100.0), short.MinValue, short.MaxValue);
        BinaryPrimitives.WriteInt16LittleEndian(buffer.AsSpan(9, 2), rollHundredths);

        var switchByte = metadata.SwitchByte;
        if (switchByte == 0)
        {
            if (metadata.IsWorkSwitchOn)
            {
                switchByte |= 0x01;
            }

            if (metadata.IsSteerSwitchOn || state.Engaged)
            {
                switchByte |= 0x02;
            }

            if (metadata.IsRemoteSwitchOn)
            {
                switchByte |= 0x04;
            }
        }
        else if (state.Engaged)
        {
            switchByte |= 0x02;
        }

        buffer[11] = switchByte;

        var pwm = metadata.RawPwm;
        if (pwm == 0)
        {
            pwm = (byte)Math.Clamp((int)Math.Round(state.AppliedEffort * 255.0), 0, 255);
        }

        buffer[12] = pwm;

        LegacyChecksum.Write(buffer);
        return buffer;
    }
}
