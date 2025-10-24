using System.IO;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Nexus.Plugin;
using Xunit;

namespace Nexus.Plugin.Manifest.Tests;

public sealed class PluginManifestTests
{
    [Fact]
    public async Task LoadAsync_WhenManifestIsValid_ReturnsManifest()
    {
        const string json = """
        {
          "id": "fe.example",
          "name": "Example Plugin",
          "version": "1.2.3",
          "sdkVersion": "1.0.0",
          "description": "Demo plugin",
          "entrypoints": {
            "core": "Fe.Example.Core",
            "ui": "Fe.Example.Ui",
            "agio": null
          },
          "capabilities": ["window", "blocks"],
          "requires": [
            { "id": "fe.base", "range": ">=1.0.0" }
          ],
          "assets": {
            "icon": "assets/icon.png"
          },
          "permissions": {
            "network": true,
            "serial": false
          }
        }
        """;

        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var manifest = await PluginManifest.LoadAsync(stream);

        manifest.Id.Should().Be("fe.example");
        manifest.Name.Should().Be("Example Plugin");
        manifest.Version.Should().Be("1.2.3");
        manifest.SdkVersion.Should().Be("1.0.0");
        manifest.Capabilities.Should().Contain(new[] { "window", "blocks" });
        manifest.Requires.Should().ContainSingle(r => r.Id == "fe.base" && r.Range == ">=1.0.0");
        manifest.Permissions.Network.Should().BeTrue();
        manifest.Permissions.Serial.Should().BeFalse();
    }

    [Fact]
    public void Validate_WhenRequiredFieldsMissing_Throws()
    {
        var manifest = new PluginManifest();

        Action act = manifest.Validate;

        act.Should().Throw<InvalidDataException>()
            .WithMessage("*must be provided*");
    }

    [Fact]
    public void Validate_WhenVersionNotSemVer_Throws()
    {
        var manifest = new PluginManifest
        {
            Id = "fe.invalid",
            Name = "Invalid Plugin",
            Version = "2024.01-beta",
            SdkVersion = "invalid",
        };

        Action act = manifest.Validate;

        act.Should().Throw<InvalidDataException>()
            .WithMessage("*semantic version*");
    }
}
