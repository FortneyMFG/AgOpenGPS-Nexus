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
          "supportedCapabilities": ["guidance.control"],
          "requiredTransports": ["AOG-Link"],
          "minimumRuntimeVersion": "1.0.0",
          "simProviders": [
            {
              "providerId": "autosteer.vehicle",
              "type": "Aog.Plugins.Autosteer.VehicleProvider",
              "topics": ["pose", "steer"],
              "settings": {
                "wheelbase": 2.8
              }
            }
          ],
          "leases": [
            {
              "capability": "guidance.control",
              "mode": "Exclusive",
              "timeoutSeconds": 5,
              "recovery": "GracefulDegradation"
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
        manifest.SupportedCapabilities.Should().ContainSingle().Which.Should().Be("guidance.control");
        manifest.RequiredTransports.Should().ContainSingle().Which.Should().Be("AOG-Link");
        manifest.MinimumRuntimeVersion.Should().Be("1.0.0");
        manifest.CapabilityLeases.Should().ContainSingle();
        manifest.CapabilityLeases[0].Capability.Should().Be("guidance.control");
        manifest.CapabilityLeases[0].Mode.Should().Be(PluginLeaseMode.Exclusive);
        manifest.CapabilityLeases[0].TimeoutSeconds.Should().Be(5);

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
          "supportedCapabilities": [],
          "requiredTransports": [],
          "minimumRuntimeVersion": "1.0.0",
          "simProviders": []
        }
        """;

        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var act = async () => await _loader.LoadAsync(stream);

        await act.Should().ThrowAsync<InvalidDataException>();
    }

    [Theory]
    [InlineData("\"\"")]
    [InlineData("\"   \"")]
    [InlineData("null")]
    public async Task LoadAsync_BlankRequiredTransports_Throws(string transportLiteral)
    {
        var json = string.Format("""
        {
          "schemaVersion": "1.0.0",
          "id": "org.agopengps.plugins.blanktransport",
          "name": "Blank Transport Plugin",
          "version": "1.0.0",
          "requiredApis": { "core": ">=1.0.0" },
          "supportedCapabilities": ["blank.transport"],
          "requiredTransports": [{0}],
          "minimumRuntimeVersion": "1.0.0",
          "simProviders": [
            {
              "providerId": "blank.sim",
              "type": "Aog.Plugins.Blank.Provider",
              "topics": ["topic"]
            }
          ]
        }
        """, transportLiteral);

        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var act = async () => await _loader.LoadAsync(stream);

        await act.Should().ThrowAsync<InvalidDataException>()
            .WithMessage("*transport names must be non-empty*");
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
          "supportedCapabilities": ["sample.capability"],
          "requiredTransports": ["AOG-Link"],
          "minimumRuntimeVersion": "1.0.0",
          "simProviders": [
            {
              "providerId": "disk.sim",
              "type": "Aog.Plugins.Disk.Provider",
              "topics": ["disk.topic"]
            }
          ],
          "leases": [
            {
              "capability": "sample.capability",
              "mode": "Shared",
              "timeoutSeconds": 10,
              "recovery": "GracefulDegradation"
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
            manifest.SupportedCapabilities.Should().Contain("sample.capability");
            manifest.RequiredTransports.Should().Contain("AOG-Link");
            manifest.CapabilityLeases.Should().ContainSingle(lease => lease.Capability == "sample.capability");
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    [Fact]
    public async Task LoadAsync_ProviderMissingTopics_Throws()
    {
        const string json = """
        {
          "schemaVersion": "1.0.0",
          "id": "org.agopengps.plugins.invalid-topics",
          "name": "Invalid Topics Plugin",
          "version": "1.0.0",
          "requiredApis": { "core": ">=1.0.0" },
          "supportedCapabilities": ["invalid.capability"],
          "requiredTransports": ["AOG-Link"],
          "minimumRuntimeVersion": "1.0.0",
          "simProviders": [
            {
              "providerId": "invalid.provider",
              "type": "Aog.Plugins.Invalid.Provider"
            }
          ],
          "leases": [
            {
              "capability": "invalid.capability",
              "mode": "Exclusive",
              "timeoutSeconds": 5,
              "recovery": "FailSafe"
            }
          ]
        }
        """;

        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var act = async () => await _loader.LoadAsync(stream);

        await act.Should().ThrowAsync<InvalidDataException>()
            .WithMessage("*must declare at least one topic*");
    }
}
