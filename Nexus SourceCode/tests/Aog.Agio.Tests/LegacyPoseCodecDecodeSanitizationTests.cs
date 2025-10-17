using System;
using System.Buffers.Binary;
using Aog.Agio.Legacy;
using Aog.Core.V1;
using Xunit;

namespace Aog.Agio.Tests;

public sealed class LegacyPoseCodecDecodeSanitizationTests
{
    public static TheoryData<Action<Span<byte>>, Func<Pose, double>> NonFiniteFieldMutations => new()
    {
        { payload => BinaryPrimitives.WriteSingleLittleEndian(payload.Slice(24, 4), float.NaN), pose => pose.SpeedMps },
        { payload => BinaryPrimitives.WriteSingleLittleEndian(payload.Slice(24, 4), float.PositiveInfinity), pose => pose.SpeedMps },
        { payload => BinaryPrimitives.WriteSingleLittleEndian(payload.Slice(28, 4), float.NaN), pose => pose.RollRad },
        { payload => BinaryPrimitives.WriteSingleLittleEndian(payload.Slice(28, 4), float.NegativeInfinity), pose => pose.RollRad },
        { payload => BinaryPrimitives.WriteSingleLittleEndian(payload.Slice(32, 4), float.NaN), pose => pose.AltitudeM },
        { payload => BinaryPrimitives.WriteSingleLittleEndian(payload.Slice(32, 4), float.PositiveInfinity), pose => pose.AltitudeM },
        { payload => BinaryPrimitives.WriteUInt16LittleEndian(payload.Slice(47, 2), BitConverter.HalfToUInt16Bits(Half.NaN)), pose => pose.PitchRad },
        { payload => BinaryPrimitives.WriteUInt16LittleEndian(payload.Slice(47, 2), BitConverter.HalfToUInt16Bits(Half.PositiveInfinity)), pose => pose.PitchRad },
        { payload => BinaryPrimitives.WriteUInt16LittleEndian(payload.Slice(49, 2), BitConverter.HalfToUInt16Bits(Half.NaN)), pose => pose.YawRateRadps },
        { payload => BinaryPrimitives.WriteUInt16LittleEndian(payload.Slice(49, 2), BitConverter.HalfToUInt16Bits(Half.NegativeInfinity)), pose => pose.YawRateRadps },
    };

    [Theory]
    [MemberData(nameof(NonFiniteFieldMutations))]
    public void TryDecodePose_NormalizesNonFiniteField(Action<Span<byte>> mutatePayload, Func<Pose, double> selector)
    {
        var codec = new LegacyPoseCodec();
        var frame = codec.EncodePose(
            new Pose
            {
                LatitudeDeg = 1.0,
                LongitudeDeg = 2.0,
                AltitudeM = 100.0,
                HeadingRad = 1.0,
                SpeedMps = 5.0,
                RollRad = 0.1,
                PitchRad = -0.05,
                YawRateRadps = 0.02,
            },
            new LegacyPoseMetadata
            {
                SourceAddress = LegacyPoseCodec.MainAntennaSourceAddress,
                ImuHeadingHundredths = 1200,
                ImuRollHundredths = -250,
                ImuPitchHundredths = 150,
                ImuYawRateHundredths = -75,
            });

        var payload = frame.AsSpan(5, LegacyPoseCodec.MainAntennaPayloadLength);
        mutatePayload(payload);
        LegacyChecksum.Write(frame);

        Assert.True(codec.TryDecodePose(frame, out var pose, out _));
        Assert.Equal(0d, selector(pose));
    }
}
