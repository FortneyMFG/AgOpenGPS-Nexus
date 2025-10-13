using System.IO;
using System.Text;
using FluentAssertions;
using Xunit;

namespace Aog.Plugins.Tests;

public sealed class PluginManifestLoaderTests
{
    private readonly PluginManifestLoader _loader = new();

    [Fact]
    public async Task LoadAsync_Stream_ReturnsManifest()
    {
        const string json = """
        {
          "schemaVersion": "1.0.0",
          "id": "org.agopengps.plugins.sample",
          "name": "Sample Plugin",
          "version": "1.2.3",
          "description": "Demonstrates the manifest loader.",
          "requiredApis": {
            "core": ">=1.0.0",
            "sim": ">=1.0.0"
          },
          "settings": {
            "gain": 1.5,
            "enabled": true
          },
          "simProviders": [
            {
              "providerId": "autosteer.vehicle",
              "type": "Aog.Plugins.Autosteer.VehicleProvider",
              "topics": ["pose", "steer"],
              "settings": {
                "wheelbase": 2.8
              }
            }
          ]
        }
        """;

        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var manifest = await _loader.LoadAsync(stream);

        manifest.SchemaVersion.Should().Be("1.0.0");
        manifest.Id.Should().Be("org.agopengps.plugins.sample");
        manifest.Name.Should().Be("Sample Plugin");
        manifest.Version.Should().Be("1.2.3");
        manifest.RequiredApis.Should().ContainKey("core").WhoseValue.Should().Be(">=1.0.0");
        manifest.RequiredApis.Should().ContainKey("sim");
        manifest.Settings.Should().ContainKey("gain");
        manifest.Settings["gain"].GetDouble().Should().Be(1.5);
        manifest.Settings.Should().ContainKey("enabled");
        manifest.Settings["enabled"].GetBoolean().Should().BeTrue();

        manifest.SimulationProviders.Should().ContainSingle();
        var provider = manifest.SimulationProviders[0];
        provider.ProviderId.Should().Be("autosteer.vehicle");
        provider.Type.Should().Be("Aog.Plugins.Autosteer.VehicleProvider");
        provider.Topics.Should().Contain(new[] { "pose", "steer" });
        provider.Settings.Should().ContainKey("wheelbase");
        provider.Settings["wheelbase"].GetDouble().Should().Be(2.8);
    }

    [Fact]
    public async Task LoadAsync_InvalidManifest_Throws()
    {
        const string json = """
        {
          "schemaVersion": "1.0.0",
          "id": "org.agopengps.invalid",
          "name": "",
          "version": "1.0.0",
          "requiredApis": {},
          "simProviders": []
        }
        """;

        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var act = async () => await _loader.LoadAsync(stream);

        await act.Should().ThrowAsync<InvalidDataException>();
    }

    [Fact]
    public async Task LoadAsync_Path_ReadsManifestFromDisk()
    {
        const string json = """
        {
          "schemaVersion": "1.0.0",
          "id": "org.agopengps.plugins.disk",
          "name": "Disk Plugin",
          "version": "1.0.1",
          "requiredApis": { "core": ">=1.0.0" },
          "simProviders": [
            {
              "providerId": "disk.sim",
              "type": "Aog.Plugins.Disk.Provider"
            }
          ]
        }
        """;

        var tempPath = Path.Combine(Path.GetTempPath(), $"manifest-{Guid.NewGuid():N}.json");
        await File.WriteAllTextAsync(tempPath, json);

        try
        {
            var manifest = await _loader.LoadAsync(tempPath);
            manifest.Id.Should().Be("org.agopengps.plugins.disk");
            manifest.SimulationProviders.Should().ContainSingle();
            manifest.SimulationProviders[0].ProviderId.Should().Be("disk.sim");
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }
}
