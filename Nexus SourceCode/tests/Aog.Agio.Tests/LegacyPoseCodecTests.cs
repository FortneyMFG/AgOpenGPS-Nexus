using System;
using System.Buffers.Binary;
using Aog.Agio.Legacy;
using Aog.Core.V1;
using Xunit;

namespace Aog.Agio.Tests;

public sealed class LegacyPoseCodecTests
{
    [Fact]
    public void EncodePose_WritesExpectedHeaderAndChecksum()
    {
        var codec = new LegacyPoseCodec();
        var pose = new Pose
        {
            LatitudeDeg = 51.123456,
            LongitudeDeg = -114.456789,
            AltitudeM = 812.4,
            HeadingRad = 1.234,
            SpeedMps = 4.2,
            RollRad = -0.12,
            PitchRad = 0.01,
            YawRateRadps = 0.015,
        };

        var metadata = new LegacyPoseMetadata
        {
            SourceAddress = LegacyPoseCodec.MainAntennaSourceAddress,
            FixQuality = 5,
            SatellitesTracked = 18,
            HdopTimes100 = 145,
            AgeOfCorrectionsTimes100 = 32,
        };

        var frame = codec.EncodePose(pose, metadata);

        Assert.Equal(LegacyPoseCodec.MainAntennaFrameLength, frame.Length);
        Assert.Equal(LegacyPoseCodec.Sync0, frame[0]);
        Assert.Equal(LegacyPoseCodec.Sync1, frame[1]);
        Assert.Equal(metadata.SourceAddress, frame[2]);
        Assert.Equal(LegacyPoseCodec.MainAntennaPosePgn, frame[3]);
        Assert.Equal(LegacyPoseCodec.MainAntennaPayloadLength, frame[4]);

        var checksum = ComputeChecksum(frame);
        Assert.Equal(checksum, frame[^1]);
    }

    [Fact]
    public void EncodePose_DefaultsSourceAddressWhenMetadataMissing()
    {
        var codec = new LegacyPoseCodec();
        var pose = new Pose
        {
            LatitudeDeg = 40.0,
            LongitudeDeg = -86.0,
        };

        var frame = codec.EncodePose(pose);

        Assert.Equal(LegacyPoseCodec.MainAntennaSourceAddress, frame[2]);
    }

    [Fact]
    public void TryDecodePose_RoundTripsEncodedFrame()
    {
        var codec = new LegacyPoseCodec();
        var pose = new Pose
        {
            LatitudeDeg = -35.9987,
            LongitudeDeg = 149.1234,
            AltitudeM = 456.7,
            HeadingRad = 2.5,
            SpeedMps = 7.25,
            RollRad = 0.05,
            PitchRad = -0.02,
            YawRateRadps = 0.01,
        };

        var metadata = new LegacyPoseMetadata
        {
            SourceAddress = 0x80,
            FixQuality = 4,
            SatellitesTracked = 15,
            HdopTimes100 = 120,
            AgeOfCorrectionsTimes100 = 12,
            ImuHeadingHundredths = 12345,
            ImuRollHundredths = -250,
            ImuPitchHundredths = 180,
            ImuYawRateHundredths = 65,
        };

        var frame = codec.EncodePose(pose, metadata);
        Assert.True(codec.TryDecodePose(frame, out var decodedPose, out var decodedMetadata));

        Assert.Equal(pose.LatitudeDeg, decodedPose.LatitudeDeg, 6);
        Assert.Equal(pose.LongitudeDeg, decodedPose.LongitudeDeg, 6);
        Assert.Equal(pose.AltitudeM, decodedPose.AltitudeM, 3);
        Assert.Equal(pose.HeadingRad, decodedPose.HeadingRad, 6);
        Assert.Equal(pose.SpeedMps, decodedPose.SpeedMps, 6);
        Assert.Equal(pose.RollRad, decodedPose.RollRad, 6);
        Assert.Equal(pose.PitchRad, decodedPose.PitchRad, 6);
        Assert.Equal(pose.YawRateRadps, decodedPose.YawRateRadps, 6);

        Assert.Equal(metadata.SourceAddress, decodedMetadata.SourceAddress);
        Assert.Equal(metadata.FixQuality, decodedMetadata.FixQuality);
        Assert.Equal(metadata.SatellitesTracked, decodedMetadata.SatellitesTracked);
        Assert.Equal(metadata.HdopTimes100, decodedMetadata.HdopTimes100);
        Assert.Equal(metadata.AgeOfCorrectionsTimes100, decodedMetadata.AgeOfCorrectionsTimes100);
        Assert.Equal(metadata.ImuHeadingHundredths, decodedMetadata.ImuHeadingHundredths);
        Assert.Equal(metadata.ImuRollHundredths, decodedMetadata.ImuRollHundredths);
        Assert.Equal(metadata.ImuPitchHundredths, decodedMetadata.ImuPitchHundredths);
        Assert.Equal(metadata.ImuYawRateHundredths, decodedMetadata.ImuYawRateHundredths);
    }

    [Fact]
    public void TryDecodePose_ReturnsFalseForInvalidChecksum()
    {
        var codec = new LegacyPoseCodec();
        var pose = new Pose { LatitudeDeg = 1, LongitudeDeg = 2 };
        var frame = codec.EncodePose(pose);
        frame[^1] ^= 0xFF; // corrupt checksum

        Assert.False(codec.TryDecodePose(frame, out var decodedPose, out var metadata));
        Assert.NotNull(decodedPose);
        Assert.NotNull(metadata);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void EncodePose_NormalizesNonFiniteInputs(double invalidValue)
    {
        var codec = new LegacyPoseCodec();
        var pose = new Pose
        {
            LatitudeDeg = 0,
            LongitudeDeg = 0,
            HeadingRad = invalidValue,
            SpeedMps = invalidValue,
            RollRad = invalidValue,
            AltitudeM = invalidValue,
            PitchRad = invalidValue,
            YawRateRadps = invalidValue,
        };

        var frame = codec.EncodePose(pose);
        var payload = frame.AsSpan(5, LegacyPoseCodec.MainAntennaPayloadLength);

        Assert.Equal(0f, BinaryPrimitives.ReadSingleLittleEndian(payload.Slice(16, 4)));
        Assert.Equal(0f, BinaryPrimitives.ReadSingleLittleEndian(payload.Slice(20, 4)));
        Assert.Equal(0f, BinaryPrimitives.ReadSingleLittleEndian(payload.Slice(24, 4)));
        Assert.Equal(0f, BinaryPrimitives.ReadSingleLittleEndian(payload.Slice(28, 4)));
        Assert.Equal(0f, BinaryPrimitives.ReadSingleLittleEndian(payload.Slice(32, 4)));
        Assert.Equal(0, BinaryPrimitives.ReadUInt16LittleEndian(payload.Slice(43, 2)));
        Assert.Equal(0, BinaryPrimitives.ReadInt16LittleEndian(payload.Slice(45, 2)));
        Assert.Equal(0, BinaryPrimitives.ReadInt16LittleEndian(payload.Slice(47, 2)));
        Assert.Equal(0, BinaryPrimitives.ReadInt16LittleEndian(payload.Slice(49, 2)));
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
