using System;
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

        if (!LegacyChecksum.Validate(datagram))
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
        var imuPitchHundredthsRaw = payload.Slice(47, 2);
        var imuPitchHundredthsValue = BinaryPrimitives.ReadUInt16LittleEndian(imuPitchHundredthsRaw);
        var imuPitchHundredths = (short)imuPitchHundredthsValue;

        var imuYawRateHundredthsRaw = payload.Slice(49, 2);
        var imuYawRateHundredthsValue = BinaryPrimitives.ReadUInt16LittleEndian(imuYawRateHundredthsRaw);
        var imuYawRateHundredths = (short)imuYawRateHundredthsValue;

        var headingRadians = double.IsFinite(headingDualDeg) && !float.IsNaN(headingDualDeg)
            ? DegreesToRadians(headingDualDeg)
            : DegreesToRadians(headingDeg);

        var sanitizedSpeedMps = 0d;
        var rawSpeed = (double)speedKph;
        if (double.IsFinite(rawSpeed))
        {
            var candidate = rawSpeed / 3.6d;
            if (double.IsFinite(candidate))
            {
                sanitizedSpeedMps = candidate;
            }
        }

        var sanitizedRollRad = 0d;
        var rawRoll = (double)rollDeg;
        if (double.IsFinite(rawRoll))
        {
            var candidate = DegreesToRadians(rawRoll);
            if (double.IsFinite(candidate))
            {
                sanitizedRollRad = candidate;
            }
        }

        var sanitizedAltitude = (double)altitudeMeters;
        if (!double.IsFinite(sanitizedAltitude))
        {
            sanitizedAltitude = 0d;
        }

        var sanitizedPitchRad = DegreesHundredthsToRadians(imuPitchHundredths);
        if (!double.IsFinite(sanitizedPitchRad) || !Half.IsFinite(BitConverter.UInt16BitsToHalf(imuPitchHundredthsValue)))
        {
            sanitizedPitchRad = 0d;
        }

        var sanitizedYawRateRadps = DegreesHundredthsToRadians(imuYawRateHundredths);
        if (!double.IsFinite(sanitizedYawRateRadps) || !Half.IsFinite(BitConverter.UInt16BitsToHalf(imuYawRateHundredthsValue)))
        {
            sanitizedYawRateRadps = 0d;
        }

        pose = new Pose
        {
            LongitudeDeg = longitude,
            LatitudeDeg = latitude,
            AltitudeM = sanitizedAltitude,
            HeadingRad = headingRadians,
            SpeedMps = sanitizedSpeedMps,
            RollRad = sanitizedRollRad,
            PitchRad = sanitizedPitchRad,
            YawRateRadps = sanitizedYawRateRadps,
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
    /// <param name="metadata">Legacy metadata describing additional fields. When omitted or when
    /// <see cref="LegacyPoseMetadata.SourceAddress"/> is zero, the header defaults to
    /// <see cref="MainAntennaSourceAddress"/>.</param>
    /// <returns>Byte array ready to send over UDP.</returns>
    /// <remarks>
    /// Non-finite pose values (for example <see cref="double.NaN"/> or infinities) are coerced to zero to
    /// maintain compatibility with legacy decoders that expect numeric fields to be finite.
    /// </remarks>
    public byte[] EncodePose(Pose pose, LegacyPoseMetadata? metadata = null)
    {
        if (pose is null)
        {
            throw new ArgumentNullException(nameof(pose));
        }

        var buffer = new byte[MainAntennaFrameLength];
        buffer[0] = Sync0;
        buffer[1] = Sync1;
        metadata ??= new LegacyPoseMetadata();
        var sourceAddress = metadata.SourceAddress != 0
            ? metadata.SourceAddress
            : MainAntennaSourceAddress;
        buffer[2] = sourceAddress;
        buffer[3] = MainAntennaPosePgn;
        buffer[4] = MainAntennaPayloadLength;

        var payload = buffer.AsSpan(5, MainAntennaPayloadLength);

        var longitude = SanitizeLongitude(pose.LongitudeDeg);
        var latitude = SanitizeLatitude(pose.LatitudeDeg);

        BinaryPrimitives.WriteDoubleLittleEndian(payload.Slice(0, 8), longitude);
        BinaryPrimitives.WriteDoubleLittleEndian(payload.Slice(8, 8), latitude);

        var headingRad = double.IsFinite(pose.HeadingRad) ? pose.HeadingRad : 0d;
        var headingDeg = (float)RadiansToDegrees(headingRad);
        if (!float.IsFinite(headingDeg))
        {
            headingDeg = 0f;
        }

        BinaryPrimitives.WriteSingleLittleEndian(payload.Slice(16, 4), headingDeg);
        BinaryPrimitives.WriteSingleLittleEndian(payload.Slice(20, 4), headingDeg);

        var speedMps = double.IsFinite(pose.SpeedMps) ? pose.SpeedMps : 0d;
        var speedKph = (float)(speedMps * 3.6);
        if (!float.IsFinite(speedKph))
        {
            speedKph = 0f;
        }
        BinaryPrimitives.WriteSingleLittleEndian(payload.Slice(24, 4), speedKph);

        var rollRad = double.IsFinite(pose.RollRad) ? pose.RollRad : 0d;
        var rollDeg = (float)RadiansToDegrees(rollRad);
        if (!float.IsFinite(rollDeg))
        {
            rollDeg = 0f;
        }
        BinaryPrimitives.WriteSingleLittleEndian(payload.Slice(28, 4), rollDeg);

        var altitudeM = double.IsFinite(pose.AltitudeM) ? pose.AltitudeM : 0d;
        var altitudeFloat = (float)altitudeM;
        if (!float.IsFinite(altitudeFloat))
        {
            altitudeFloat = 0f;
        }
        BinaryPrimitives.WriteSingleLittleEndian(payload.Slice(32, 4), altitudeFloat);

        BinaryPrimitives.WriteUInt16LittleEndian(payload.Slice(36, 2), metadata.SatellitesTracked);
        payload[38] = metadata.FixQuality;
        BinaryPrimitives.WriteUInt16LittleEndian(payload.Slice(39, 2), metadata.HdopTimes100);
        BinaryPrimitives.WriteUInt16LittleEndian(payload.Slice(41, 2), metadata.AgeOfCorrectionsTimes100);

        var imuHeading = metadata.ImuHeadingHundredths;
        if (imuHeading == 0 && headingRad is not 0d)
        {
            imuHeading = EncodeUnsignedAngleHundredths(headingRad);
        }

        var imuRoll = metadata.ImuRollHundredths;
        if (imuRoll == 0 && rollRad is not 0d)
        {
            imuRoll = EncodeSignedAngleHundredths(rollRad);
        }

        var pitchRad = double.IsFinite(pose.PitchRad) ? pose.PitchRad : 0d;
        var imuPitch = metadata.ImuPitchHundredths;
        if (imuPitch == 0 && pitchRad is not 0d)
        {
            imuPitch = EncodeSignedAngleHundredths(pitchRad);
        }

        var yawRateRadps = double.IsFinite(pose.YawRateRadps) ? pose.YawRateRadps : 0d;
        var imuYawRate = metadata.ImuYawRateHundredths;
        if (imuYawRate == 0 && yawRateRadps is not 0d)
        {
            imuYawRate = EncodeSignedAngleHundredths(yawRateRadps);
        }

        BinaryPrimitives.WriteUInt16LittleEndian(payload.Slice(43, 2), imuHeading);
        BinaryPrimitives.WriteInt16LittleEndian(payload.Slice(45, 2), imuRoll);
        BinaryPrimitives.WriteInt16LittleEndian(payload.Slice(47, 2), imuPitch);
        BinaryPrimitives.WriteInt16LittleEndian(payload.Slice(49, 2), imuYawRate);

        LegacyChecksum.Write(buffer);
        return buffer;
    }

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180d;

    private static double RadiansToDegrees(double radians) => radians * 180d / Math.PI;

    private static double DegreesHundredthsToRadians(short hundredths) => DegreesToRadians(hundredths / 100d);

    private static double SanitizeLongitude(double longitude)
    {
        if (!double.IsFinite(longitude))
        {
            return 0d;
        }

        if (longitude >= 180d)
        {
            return 180d;
        }

        if (longitude <= -180d)
        {
            return -180d;
        }

        return longitude;
    }

    private static double SanitizeLatitude(double latitude)
    {
        if (!double.IsFinite(latitude))
        {
            return 0d;
        }

        if (latitude >= 90d)
        {
            return 90d;
        }

        if (latitude <= -90d)
        {
            return -90d;
        }

        return latitude;
    }

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

}
