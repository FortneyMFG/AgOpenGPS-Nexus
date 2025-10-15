using System;
using System.IO;
using System.Text.Json;
using Aog.Tools.RadioBridge;
using Xunit;

namespace Aog.Tools.RadioBridge.Tests;

public sealed class RadioBridgeProvisioningCliTests
{
    [Fact]
    public void Provision_WritesJsonToStdout_WhenOutputMissing()
    {
        var originalOut = Console.Out;
        using var writer = new StringWriter();
        Console.SetOut(writer);

        var exitCode = Program.Main(new[] { "provision", "--device-id", "bridge.test", "--key", "AA" });

        Assert.Equal(0, exitCode);
        var json = writer.ToString().Trim();
        var profile = JsonSerializer.Deserialize<RadioBridgeProvisioningProfile>(json, RadioBridgeProvisioningProfile.SerializerOptions);
        Assert.NotNull(profile);
        Assert.Equal("bridge.test", profile!.DeviceId);
        Assert.Equal("AA", profile.PreSharedKey);

        Console.SetOut(originalOut);
    }

    [Fact]
    public void Provision_WritesFile_WhenOutputSpecified()
    {
        using var temp = new TempDirectory();
        var path = Path.Combine(temp.Path, "profile.json");

        var exitCode = Program.Main(new[] { "provision", "--output", path, "--key-bytes", "4" });

        Assert.Equal(0, exitCode);
        Assert.True(File.Exists(path));

        var profile = JsonSerializer.Deserialize<RadioBridgeProvisioningProfile>(File.ReadAllText(path), RadioBridgeProvisioningProfile.SerializerOptions);
        Assert.NotNull(profile);
        Assert.Equal(4 * 2, profile!.PreSharedKey.Length);
    }

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(Path))
                {
                    Directory.Delete(Path, recursive: true);
                }
            }
            catch
            {
            }
        }
    }
}
