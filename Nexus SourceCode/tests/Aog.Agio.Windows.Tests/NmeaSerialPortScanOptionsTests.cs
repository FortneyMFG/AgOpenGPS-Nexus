using Aog.Agio.Windows;
using Xunit;

namespace Aog.Agio.Windows.Tests;

public sealed class NmeaSerialPortScanOptionsTests
{
    [Fact]
    public void BaudRates_DefaultIncludesHighSpeedOption()
    {
        var options = new NmeaSerialPortScanOptions();

        Assert.Contains(921600, options.BaudRates);
    }
}
