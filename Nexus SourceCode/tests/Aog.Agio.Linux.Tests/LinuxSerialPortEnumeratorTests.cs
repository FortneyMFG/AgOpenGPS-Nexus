using Aog.Agio.Linux.Serial;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Aog.Agio.Linux.Tests;

public sealed class LinuxSerialPortEnumeratorTests : IDisposable
{
    private readonly string _root;

    public LinuxSerialPortEnumeratorTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "nexus-linux-serial-test-" + Guid.NewGuid());
        Directory.CreateDirectory(_root);
    }

    [Fact]
    public void GetPortNames_ReturnsDevicesMatchingPrefixes()
    {
        var devDir = CreateSubDirectory("dev");
        var usb0 = CreateDevice(devDir, "ttyUSB0");
        var usb1 = CreateDevice(devDir, "ttyUSB1");
        var acm0 = CreateDevice(devDir, "ttyACM0");
        _ = CreateDevice(devDir, "random0");

        var options = Options.Create(new LinuxSerialPortEnumeratorOptions
        {
            DevicePrefixes = new[]
            {
                Path.Combine(devDir, "ttyUSB"),
                Path.Combine(devDir, "ttyACM"),
            },
        });

        var enumerator = new LinuxSerialPortEnumerator(NullLogger<LinuxSerialPortEnumerator>.Instance, options);

        var ports = enumerator.GetPortNames().ToArray();

        Assert.Equal(3, ports.Length);
        Assert.Contains(usb0, ports);
        Assert.Contains(usb1, ports);
        Assert.Contains(acm0, ports);
    }

    [Fact]
    public void GetPortNames_EnumeratesDirectoryEntries()
    {
        var byIdDir = CreateSubDirectory("serial/by-id");
        var device = CreateDevice(byIdDir, "u-blox" + Guid.NewGuid().ToString("N"));

        var options = Options.Create(new LinuxSerialPortEnumeratorOptions
        {
            DevicePrefixes = new[]
            {
                Path.Combine(_root, "serial/by-id/")
            },
        });

        var enumerator = new LinuxSerialPortEnumerator(NullLogger<LinuxSerialPortEnumerator>.Instance, options);

        var ports = enumerator.GetPortNames().ToArray();

        Assert.Single(ports);
        Assert.Equal(device, ports[0]);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_root))
            {
                Directory.Delete(_root, recursive: true);
            }
        }
        catch
        {
            // Ignore cleanup failures in CI.
        }
    }

    private string CreateSubDirectory(string relative)
    {
        var path = Path.Combine(_root, relative);
        Directory.CreateDirectory(path);
        return path;
    }

    private static string CreateDevice(string directory, string name)
    {
        var path = Path.Combine(directory, name);
        File.WriteAllText(path, string.Empty);
        return Path.GetFullPath(path);
    }
}
