using Aog.Agio.Linux.Serial;
using Xunit;

namespace Aog.Agio.Tests;

public sealed class LinuxSerialPortEnumeratorOptionsTests
{
    [Fact]
    public void DevicePrefixes_NormalizesWhitespaceAndRejectsNonRootedPaths()
    {
        var options = new LinuxSerialPortEnumeratorOptions
        {
            DevicePrefixes = new[] { "  /dev/ttyUSB  ", "ttyBAD" },
        };

        Assert.Equal(new[] { "/dev/ttyUSB" }, options.DevicePrefixes);
    }
}
