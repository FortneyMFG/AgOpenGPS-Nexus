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
        Assert.Equal(0, frame[7]);
        Assert.Equal(0, frame[8]);
        Assert.Equal(0, frame[9]);
    }

    [Fact]
    public void EncodeAutoSteerFrame_SetsSectionMask()
    {
        var codec = new LegacyAutoSteerCodec();
        var sections = new SectionMask { Mask = 0x01FF };

        var frame = codec.EncodeAutoSteerFrame(new SteerCmd { Enable = true }, sections);

        Assert.Equal(0xFF, frame[11]);
        Assert.Equal(0x01, frame[12]);
    }
}
