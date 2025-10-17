using Aog.Agio.Legacy;
using Aog.Core.V1;
using Xunit;

namespace Aog.Agio.Tests;

public sealed class LegacyAutoSteerCodecTests
{
    [Fact]
    public void EncodeAutoSteerFrame_ZeroesWhenDisabled()
    {
        var codec = new LegacyAutoSteerCodec();
        var frame = codec.EncodeAutoSteerFrame(new SteerCmd { Enable = false, TargetWheelAngleDeg = 15 });

        Assert.Equal(LegacyAutoSteerCodec.AutoSteerFrameLength, frame.Length);
        // payload[2] = enable flag, payload[3..4] = angle (LSB/MSB)
        Assert.Equal(0, frame[7]);
        Assert.Equal(0, frame[8]);
        Assert.Equal(0, frame[9]);
    }

    [Fact]
    public void EncodeAutoSteerFrame_SetsSectionMask()
    {
        var codec = new LegacyAutoSteerCodec();
        var sections = new SectionMask { SectionCount = 16, Mask = 0x01FF };

        var frame = codec.EncodeAutoSteerFrame(new SteerCmd { Enable = true }, sections);

        // payload[6..7] = section mask (LSB/MSB)
        Assert.Equal(0xFF, frame[11]);
        Assert.Equal(0x01, frame[12]);
    }

    [Fact]
    public void EncodeAutoSteerFrame_ClearsMaskedBits_WhenSectionCountIsLessThanSixteen()
    {
        var codec = new LegacyAutoSteerCodec();
        var sections = new SectionMask
        {
            SectionCount = 8,
            Mask = 0xFFFF,
        };

        var frame = codec.EncodeAutoSteerFrame(new SteerCmd { Enable = true }, sections);

        // Only lower 8 bits are allowed when SectionCount = 8
        Assert.Equal(0xFF, frame[11]);
        Assert.Equal(0x00, frame[12]);
    }

    [Fact]
    public void EncodeAutoSteerFrame_ClearsMask_WhenSectionCountIsZero()
    {
        var codec = new LegacyAutoSteerCodec();
        var sections = new SectionMask
        {
            SectionCount = 0,
            Mask = 0xFFFF,
        };

        var frame = codec.EncodeAutoSteerFrame(new SteerCmd { Enable = true }, sections);

        Assert.Equal(0x00, frame[11]);
        Assert.Equal(0x00, frame[12]);
    }

    [Fact]
    public void EncodeAutoSteerFrame_NonFiniteTargetAngleZeroesOutput()
    {
        var codec = new LegacyAutoSteerCodec();
        var frame = codec.EncodeAutoSteerFrame(new SteerCmd { Enable = true, TargetWheelAngleDeg = double.NaN });

        // Angle bytes should be zeroed when target is non-finite
        Assert.Equal(0, frame[8]);
        Assert.Equal(0, frame[9]);
    }
}
