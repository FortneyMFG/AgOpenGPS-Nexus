using System;
using Aog.Agio.Ntrip;
using Xunit;

namespace Aog.Agio.Tests.Ntrip;

public sealed class NtripClientOptionsTests
{
    [Fact]
    public void Validate_AllowsBasicConfiguration()
    {
        var options = new NtripClientOptions
        {
            Host = "caster.example.com",
            Port = 2101,
            MountPoint = "MOUNT"
        };

        options.Validate();
        Assert.Equal("/MOUNT", options.GetRequestPath());
    }

    [Theory]
    [InlineData("", 2101, "MOUNT")]
    [InlineData("caster.example.com", 0, "MOUNT")]
    [InlineData("caster.example.com", 2101, "")] 
    [InlineData("caster.example.com", 2101, "mount with space")]
    public void Validate_ThrowsForInvalidConfiguration(string host, int port, string mountPoint)
    {
        var options = new NtripClientOptions
        {
            Host = host,
            Port = port,
            MountPoint = mountPoint
        };

        Assert.ThrowsAny<Exception>(() => options.Validate());
    }

    [Fact]
    public void Validate_RequiresMatchingCredentials()
    {
        var options = new NtripClientOptions
        {
            Host = "caster",
            Port = 2101,
            MountPoint = "M",
            Username = "user"
        };

        Assert.Throws<ArgumentException>(() => options.Validate());
    }

    [Fact]
    public void Validate_RejectsNewlinesInGga()
    {
        var options = new NtripClientOptions
        {
            Host = "caster",
            Port = 2101,
            MountPoint = "M",
            NmeaGgaSentence = "$GPGGA,123\n"
        };

        Assert.Throws<ArgumentException>(() => options.Validate());
    }
}
