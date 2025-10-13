using Aog.Agio.Linux;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aog.Agio.Linux.Tests;

public sealed class LinuxSerialPortEnumeratorTests
{
    [Fact]
    public void GetPortNames_ReturnsSortedDevicePaths()
    {
        var tempDir = Directory.CreateTempSubdirectory();
        try
        {
            File.WriteAllText(Path.Combine(tempDir.FullName, "ttyUSB1"), string.Empty);
            File.WriteAllText(Path.Combine(tempDir.FullName, "ttyACM0"), string.Empty);
            File.WriteAllText(Path.Combine(tempDir.FullName, "not-a-port"), string.Empty);

            var enumerator = new LinuxSerialPortEnumerator(NullLogger<LinuxSerialPortEnumerator>.Instance, tempDir.FullName);

            var ports = enumerator.GetPortNames().ToArray();

            Assert.Equal(new[]
            {
                Path.Combine(tempDir.FullName, "ttyACM0"),
                Path.Combine(tempDir.FullName, "ttyUSB1"),
            },
            ports);
        }
        finally
        {
            tempDir.Delete(recursive: true);
        }
    }

    [Fact]
    public void GetPortNames_UnknownDirectory_ReturnsEmpty()
    {
        var enumerator = new LinuxSerialPortEnumerator(NullLogger<LinuxSerialPortEnumerator>.Instance, Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")));

        var ports = enumerator.GetPortNames();

        Assert.Empty(ports);
    }
}
