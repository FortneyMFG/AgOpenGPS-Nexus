using System.Buffers.Binary;
using Aog.Core.V1;

namespace Aog.Agio.Legacy;

/// <summary>
/// Encodes and decodes legacy UDP PGNs that carry the main GPS pose into typed
/// protobuf contracts.
/// </summary>
public sealed class LegacyPoseCodec
{
    /// <summary>
    /// Legacy sync byte 0.
    /// </summary>
    public const byte Sync0 = 0x80;

    /// <summary>
    /// Legacy sync byte 1.
    /// </summary>
    public const byte Sync1 = 0x81;

    /// <summary>
    /// Default source address for the main GPS antenna module.
    /// </summary>
    public const byte MainAntennaSourceAddress = 0x7C;

    /// <summary>
    /// PGN emitted by the main GPS antenna module.
    /// </summary>
    public const byte MainAntennaPosePgn = 0xD6;

    /// <summary>
    /// Length of the legacy payload (excluding header and checksum).
    /// </summary>
    public const int MainAntennaPayloadLength = 51;

    /// <summary>
    /// Total legacy frame length including sync bytes and checksum.
    /// </summary>
    public const int MainAntennaFrameLength = 2 + 1 + 1 + 1 + MainAntennaPayloadLength + 1;

    /// <summary>
    /// Attempts to decode a legacy GPS PGN into a typed pose.
    /// </summary>
    /// <param name="datagram">Incoming UDP datagram.</param>
    /// <param name="pose">Resulting typed pose.</param>
    /// <param name="metadata">Legacy metadata accompanying the pose.</param>
    /// <returns><c>true</c> when the datagram was recognised and decoded successfully.</returns>
    public bool TryDecodePose(ReadOnlySpan<byte> datagram, out Pose pose, out LegacyPoseMetadata metadata)
    {
        pose = new Pose();
        metadata = new LegacyPoseMetadata();

        if (datagram.Length != MainAntennaFrameLength)
        {
            return false;
        }

        if (datagram[0] != Sync0 || datagram[1] != Sync1)
        {
            return false;
        }

        if (datagram[3] != MainAntennaPosePgn || datagram[4] != MainAntennaPayloadLength)
        {
            return false;
        }

        if (!ValidateChecksum(datagram))
        {
            return false;
        }

        var payload = datagram.Slice(5, MainAntennaPayloadLength);

        var longitude = BinaryPrimitives.ReadDoubleLittleEndian(payload.Slice(0, 8));
        var latitude = BinaryPrimitives.ReadDoubleLittleEndian(payload.Slice(8, 8));
        var headingDualDeg = BinaryPrimitives.ReadSingleLittleEndian(payload.Slice(16, 4));
        var headingDeg = BinaryPrimitives.ReadSingleLittleEndian(payload.Slice(20, 4));
        var speedKph = BinaryPrimitives.ReadSingleLittleEndian(payload.Slice(24, 4));
        var rollDeg = BinaryPrimitives.ReadSingleLittleEndian(payload.Slice(28, 4));
        var altitudeMeters = BinaryPrimitives.ReadSingleLittleEndian(payload.Slice(32, 4));
        var satellites = BinaryPrimitives.ReadUInt16LittleEndian(payload.Slice(36, 2));
        var fixQuality = payload[38];
        var hdopTimes100 = BinaryPrimitives.ReadUInt16LittleEndian(payload.Slice(39, 2));
        var ageTimes100 = BinaryPrimitives.ReadUInt16LittleEndian(payload.Slice(41, 2));
        var imuHeadingHundredths = BinaryPrimitives.ReadUInt16LittleEndian(payload.Slice(43, 2));
        var imuRollHundredths = BinaryPrimitives.ReadInt16LittleEndian(payload.Slice(45, 2));
        var imuPitchHundredths = BinaryPrimitives.ReadInt16LittleEndian(payload.Slice(47, 2));
        var imuYawRateHundredths = BinaryPrimitives.ReadInt16LittleEndian(payload.Slice(49, 2));

        var headingRadians = double.IsFinite(headingDualDeg) && !float.IsNaN(headingDualDeg)
            ? DegreesToRadians(headingDualDeg)
            : DegreesToRadians(headingDeg);

        pose = new Pose
        {
            LongitudeDeg = longitude,
            LatitudeDeg = latitude,
            AltitudeM = altitudeMeters,
            HeadingRad = headingRadians,
            SpeedMps = speedKph / 3.6,
            RollRad = DegreesToRadians(rollDeg),
            PitchRad = DegreesHundredthsToRadians(imuPitchHundredths),
            YawRateRadps = DegreesHundredthsToRadians(imuYawRateHundredths),
        };

        metadata = new LegacyPoseMetadata
        {
            SourceAddress = datagram[2],
            FixQuality = fixQuality,
            SatellitesTracked = satellites,
            HdopTimes100 = hdopTimes100,
            AgeOfCorrectionsTimes100 = ageTimes100,
            ImuHeadingHundredths = imuHeadingHundredths,
            ImuRollHundredths = imuRollHundredths,
            ImuPitchHundredths = imuPitchHundredths,
            ImuYawRateHundredths = imuYawRateHundredths,
        };

        return true;
    }

    /// <summary>
    /// Encodes a typed pose into the legacy UDP PGN representation.
    /// </summary>
    /// <param name="pose">Pose to encode.</param>
    /// <param name="metadata">Legacy metadata describing additional fields.</param>
    /// <returns>Byte array ready to send over UDP.</returns>
    public byte[] EncodePose(Pose pose, LegacyPoseMetadata? metadata = null)
    {
        if (pose is null)
        {
            throw new ArgumentNullException(nameof(pose));
        }

        metadata ??= new LegacyPoseMetadata();

        var buffer = new byte[MainAntennaFrameLength];
        buffer[0] = Sync0;
        buffer[1] = Sync1;
        buffer[2] = metadata.SourceAddress;
        buffer[3] = MainAntennaPosePgn;
        buffer[4] = MainAntennaPayloadLength;

        var payload = buffer.AsSpan(5, MainAntennaPayloadLength);

        BinaryPrimitives.WriteDoubleLittleEndian(payload.Slice(0, 8), pose.LongitudeDeg);
        BinaryPrimitives.WriteDoubleLittleEndian(payload.Slice(8, 8), pose.LatitudeDeg);

        var headingDeg = (float)RadiansToDegrees(pose.HeadingRad);
        if (float.IsNaN(headingDeg) || float.IsInfinity(headingDeg))
        {
            headingDeg = 0f;
        }

        BinaryPrimitives.WriteSingleLittleEndian(payload.Slice(16, 4), headingDeg);
        BinaryPrimitives.WriteSingleLittleEndian(payload.Slice(20, 4), headingDeg);

        var speedKph = (float)(pose.SpeedMps * 3.6);
        BinaryPrimitives.WriteSingleLittleEndian(payload.Slice(24, 4), speedKph);

        var rollDeg = (float)RadiansToDegrees(pose.RollRad);
        BinaryPrimitives.WriteSingleLittleEndian(payload.Slice(28, 4), rollDeg);

        BinaryPrimitives.WriteSingleLittleEndian(payload.Slice(32, 4), (float)pose.AltitudeM);

        BinaryPrimitives.WriteUInt16LittleEndian(payload.Slice(36, 2), metadata.SatellitesTracked);
        payload[38] = metadata.FixQuality;
        BinaryPrimitives.WriteUInt16LittleEndian(payload.Slice(39, 2), metadata.HdopTimes100);
        BinaryPrimitives.WriteUInt16LittleEndian(payload.Slice(41, 2), metadata.AgeOfCorrectionsTimes100);

        var imuHeading = metadata.ImuHeadingHundredths;
        if (imuHeading == 0 && pose.HeadingRad is not 0d)
        {
            imuHeading = EncodeUnsignedAngleHundredths(pose.HeadingRad);
        }

        var imuRoll = metadata.ImuRollHundredths;
        if (imuRoll == 0 && pose.RollRad is not 0d)
        {
            imuRoll = EncodeSignedAngleHundredths(pose.RollRad);
        }

        var imuPitch = metadata.ImuPitchHundredths;
        if (imuPitch == 0 && pose.PitchRad is not 0d)
        {
            imuPitch = EncodeSignedAngleHundredths(pose.PitchRad);
        }

        var imuYawRate = metadata.ImuYawRateHundredths;
        if (imuYawRate == 0 && pose.YawRateRadps is not 0d)
        {
            imuYawRate = EncodeSignedAngleHundredths(pose.YawRateRadps);
        }

        BinaryPrimitives.WriteUInt16LittleEndian(payload.Slice(43, 2), imuHeading);
        BinaryPrimitives.WriteInt16LittleEndian(payload.Slice(45, 2), imuRoll);
        BinaryPrimitives.WriteInt16LittleEndian(payload.Slice(47, 2), imuPitch);
        BinaryPrimitives.WriteInt16LittleEndian(payload.Slice(49, 2), imuYawRate);

        buffer[^1] = ComputeChecksum(buffer);
        return buffer;
    }

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180d;

    private static double RadiansToDegrees(double radians) => radians * 180d / Math.PI;

    private static double DegreesHundredthsToRadians(short hundredths) => DegreesToRadians(hundredths / 100d);

    private static short EncodeSignedAngleHundredths(double radians)
    {
        if (double.IsNaN(radians) || double.IsInfinity(radians))
        {
            return 0;
        }

        var degrees = RadiansToDegrees(radians) * 100d;
        return ClampToInt16(degrees);
    }

    private static ushort EncodeUnsignedAngleHundredths(double radians)
    {
        if (double.IsNaN(radians) || double.IsInfinity(radians))
        {
            return 0;
        }

        var degrees = RadiansToDegrees(radians);
        var normalized = ((degrees % 360d) + 360d) % 360d;
        var scaled = Math.Round(normalized * 100d, MidpointRounding.AwayFromZero);
        if (scaled <= 0d)
        {
            return 0;
        }

        if (scaled >= ushort.MaxValue)
        {
            return ushort.MaxValue;
        }

        return (ushort)scaled;
    }

    private static short ClampToInt16(double value)
    {
        var rounded = Math.Round(value, MidpointRounding.AwayFromZero);
        if (rounded >= short.MaxValue)
        {
            return short.MaxValue;
        }

        if (rounded <= short.MinValue)
        {
            return short.MinValue;
        }

        return (short)rounded;
    }

    private static bool ValidateChecksum(ReadOnlySpan<byte> frame)
    {
        var checksum = ComputeChecksum(frame);
        return checksum == frame[^1];
    }

    private static byte ComputeChecksum(ReadOnlySpan<byte> frame)
    {
        int sum = 0;
        for (var i = 2; i < frame.Length - 1; i++)
        {
            sum += frame[i];
        }

        return unchecked((byte)sum);
    }
}
