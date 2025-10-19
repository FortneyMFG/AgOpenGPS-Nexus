using System.Linq;
using System.Text.Json.Nodes;
using FluentAssertions;
using Nexus.Plugin.Manifest;
using Xunit;

namespace Nexus.Plugin.Manifest.Tests;

public class PluginManifestTests
{
    [Fact]
    public void FromJson_WhenManifestIsValid_ReturnsManifest()
    {
        const string json = """
        {
          "id": "fe.example",
          "name": "Example Plugin",
          "version": "1.0.0",
          "sdkVersion": ">=1.0.0 <2.0.0",
          "requires": {
            "fe.base": ">=1.0.0"
          },
          "entrypoints": {
            "core": "Fe.Example.CoreEntrypoint",
            "ui": "Fe.Example.UiEntrypoint",
            "agio": null
          },
          "capabilities": ["window", "blocks"],
          "assets": {
            "icon": "assets/icon.png"
          },
          "update": {
            "feed": null
          },
          "permissions": {
            "network": true,
            "serial": false
          }
        }
        """;

        var manifest = PluginManifest.FromJson(json);

        manifest.Id.Should().Be("fe.example");
        manifest.Name.Should().Be("Example Plugin");
        manifest.Version.Should().Be("1.0.0");
        manifest.SdkVersion.Should().Be(">=1.0.0 <2.0.0");
        manifest.Capabilities.Should().Contain(new[] { "window", "blocks" });
        manifest.Permissions.Network.Should().BeTrue();
        manifest.Permissions.Serial.Should().BeFalse();
    }

    [Fact]
    public void GenerateJsonSchema_ContainsRequiredProperties()
    {
        var schema = PluginManifest.GenerateJsonSchema();
        schema.Should().NotBeNull();

        var required = schema["required"]!.AsArray().Select(node => node!.GetValue<string>()).ToList();
        required.Should().Contain(new[] { "id", "name", "version", "sdkVersion", "entrypoints", "capabilities" });

        var properties = schema["properties"]!.AsObject();
        properties.Should().ContainKey("entrypoints");
        properties["entrypoints"]!.AsObject()["required"]!.AsArray()
            .Select(node => node!.GetValue<string>())
            .Should().Contain(new[] { "core", "ui", "agio" });
    }
}
