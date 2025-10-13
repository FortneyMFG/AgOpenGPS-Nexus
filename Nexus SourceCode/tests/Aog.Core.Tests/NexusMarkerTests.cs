using Aog.Core;
using Xunit;

namespace Aog.Core.Tests;

public class NexusMarkerTests
{
    [Fact]
    public void ProductName_MatchesExpectedValue()
    {
        Assert.Equal("AOG Nexus Core", NexusMarker.ProductName);
    }
}
