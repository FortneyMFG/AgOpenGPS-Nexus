using Aog.Agio.Legacy;
using Aog.Core.V1;
using Xunit;

namespace Aog.Agio.Tests;

public sealed class LegacySerialFrameCodecTests
{
    [Fact]
    public void EncodeAndDecode_RoundTripsPoseFrame()
    {
        var poseCodec = new LegacyPoseCodec();
        var pose = new Pose { LatitudeDeg = 51.123456, LongitudeDeg = -114.987654, SpeedMps = 3.2 };
        var frame = poseCodec.EncodePose(pose);

        var encoded = new byte[LegacySerialFrameCodec.GetMaxEncodedLength(frame.Length)];
        var encodedLength = LegacySerialFrameCodec.Encode(frame, encoded);

        var decoded = new byte[frame.Length];
        var success = LegacySerialFrameCodec.TryDecode(encoded.AsSpan(0, encodedLength), decoded, out var bytesWritten);

        Assert.True(success);
        Assert.Equal(frame.Length, bytesWritten);
        Assert.True(frame.AsSpan().SequenceEqual(decoded.AsSpan(0, bytesWritten)));
        Assert.True(LegacyChecksum.Validate(decoded.AsSpan(0, bytesWritten)));
    }

    [Fact]
    public void EncodeAndDecode_PreservesZeroBytes()
    {
        var frame = new byte[] { 0x80, 0x81, 0x7F, 0xFE, 0x02, 0x00, 0xAA, 0x00 };
        LegacyChecksum.Write(frame);

        var encoded = new byte[LegacySerialFrameCodec.GetMaxEncodedLength(frame.Length)];
        var encodedLength = LegacySerialFrameCodec.Encode(frame, encoded);

        var decoded = new byte[frame.Length];
        var success = LegacySerialFrameCodec.TryDecode(encoded.AsSpan(0, encodedLength), decoded, out var bytesWritten);

        Assert.True(success);
        Assert.Equal(frame.Length, bytesWritten);
        Assert.True(frame.AsSpan().SequenceEqual(decoded.AsSpan(0, bytesWritten)));
    }

    [Fact]
    public void TryDecode_ReturnsFalseWithoutDelimiter()
    {
        var frame = new byte[] { 0x80, 0x81, 0x7F, 0xFE, 0x00, 0x00 };
        LegacyChecksum.Write(frame);

        var encoded = new byte[LegacySerialFrameCodec.GetMaxEncodedLength(frame.Length)];
        var encodedLength = LegacySerialFrameCodec.Encode(frame, encoded);

        var destination = new byte[frame.Length];
        var success = LegacySerialFrameCodec.TryDecode(encoded.AsSpan(0, encodedLength - 1), destination, out _);

        Assert.False(success);
    }
}
