using Aog.Agio.Linux.Gpsd;
using Xunit;

namespace Aog.Agio.Linux.Tests;

public sealed class GpsdClientOptionsTests
{
    [Fact]
    public void SocketPath_DefaultsToGpsdSocket()
    {
        var options = new GpsdClientOptions();

        Assert.Equal("/var/run/gpsd.sock", options.SocketPath);
    }

    [Fact]
    public void SocketPath_Null_DisablesWorker()
    {
        var options = new GpsdClientOptions
        {
            SocketPath = null,
        };

        Assert.Null(options.SocketPath);
    }

    [Fact]
    public void SocketPath_EmptyString_DisablesWorker()
    {
        var options = new GpsdClientOptions
        {
            SocketPath = string.Empty,
        };

        Assert.Null(options.SocketPath);
    }

    [Fact]
    public void SocketPath_ValidPath_Updates()
    {
        var options = new GpsdClientOptions
        {
            SocketPath = "/tmp/gpsd.sock",
        };

        Assert.Equal("/tmp/gpsd.sock", options.SocketPath);
    }

    [Fact]
    public void SocketPath_Whitespace_DisablesWorker()
    {
        var options = new GpsdClientOptions
        {
            SocketPath = "  \t\n  ",
        };

        Assert.Null(options.SocketPath);
    }

    [Fact]
    public void SocketPath_TrimmedValue_Persists()
    {
        var options = new GpsdClientOptions
        {
            SocketPath = "  /tmp/gpsd.sock  ",
        };

        Assert.Equal("/tmp/gpsd.sock", options.SocketPath);
    }
}
