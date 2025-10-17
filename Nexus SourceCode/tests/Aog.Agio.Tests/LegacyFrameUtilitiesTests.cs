using Aog.Agio.Legacy;
using Xunit;

namespace Aog.Agio.Tests.Legacy;

public class LegacyFrameUtilitiesTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(4)]
    public void ValidateChecksum_ReturnsFalse_WhenFrameIsShorterThanMinimum(int frameLength)
    {
        var frame = new byte[frameLength];

        var isValid = LegacyFrameUtilities.ValidateChecksum(frame);

        Assert.False(isValid);
    }
}
