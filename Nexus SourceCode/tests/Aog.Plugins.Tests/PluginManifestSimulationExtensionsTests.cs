using System;
using System.Text.Json;
using Aog.Core.Simulation;
using FluentAssertions;
using Xunit;

namespace Aog.Plugins.Tests;

public sealed class PluginManifestSimulationExtensionsTests
{
    [Fact]
    public void RegisterSimulationProviders_RegistersDescriptors()
    {
        var manifest = new PluginManifest
        {
            SchemaVersion = "1.0.0",
            Id = "org.agopengps.plugins.bundle",
            Name = "Simulation Bundle",
            Version = "1.0.0",
            RequiredApis = { ["core"] = ">=1.0.0" },
            SimulationProviders =
            {
                new PluginSimProvider
                {
                    ProviderId = "autosteer.vehicle",
                    Type = "Aog.Plugins.Autosteer.VehicleProvider",
                    Topics = { "pose", "steer" },
                    Settings =
                    {
                        ["wheelbase"] = Json("2.8"),
                        ["latencyMs"] = Json("40")
                    }
                },
                new PluginSimProvider
                {
                    ProviderId = "gnss.basic",
                    Type = "Aog.Plugins.Sim.GnssProvider",
                    Topics = { "pose" }
                }
            }
        };

        var catalog = new SimulationCatalog();

        var registrations = manifest.RegisterSimulationProviders(catalog);

        registrations.Should().HaveCount(2);
        registrations[0].PluginId.Should().Be(manifest.Id);
        registrations[0].ProviderId.Should().Be("autosteer.vehicle");
        registrations[0].Topics.Should().ContainInOrder("pose", "steer");
        registrations[0].Settings.Should().ContainKeys("wheelbase", "latencyMs");

        var descriptor = catalog.GetRequired("gnss.basic");
        descriptor.Outputs.Should().ContainSingle().Which.Should().Be("pose");
    }

    [Fact]
    public void RegisterSimulationProviders_MissingTopics_Throws()
    {
        var manifest = new PluginManifest
        {
            SchemaVersion = "1.0.0",
            Id = "org.agopengps.plugins.invalid",
            Name = "Invalid",
            Version = "1.0.0",
            RequiredApis = { ["core"] = ">=1.0.0" },
            SimulationProviders =
            {
                new PluginSimProvider
                {
                    ProviderId = "broken.provider",
                    Type = "Aog.Plugins.Broken.Provider"
                }
            }
        };

        var catalog = new SimulationCatalog();

        var act = () => manifest.RegisterSimulationProviders(catalog);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*must declare at least one topic*");
    }

    [Fact]
    public void RegisterSimulationProviders_NullCatalog_Throws()
    {
        var manifest = new PluginManifest
        {
            SchemaVersion = "1.0.0",
            Id = "org.agopengps.plugins.bundle",
            Name = "Simulation Bundle",
            Version = "1.0.0",
            RequiredApis = { ["core"] = ">=1.0.0" },
            SimulationProviders =
            {
                new PluginSimProvider
                {
                    ProviderId = "autosteer.vehicle",
                    Type = "Aog.Plugins.Autosteer.VehicleProvider",
                    Topics = { "pose" }
                }
            }
        };

        Action act = () => manifest.RegisterSimulationProviders(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("catalog");
    }

    [Fact]
    public void RegisterSimulationProviders_WhenConflictingProviderExists_DoesNotMutateCatalog()
    {
        var manifest = new PluginManifest
        {
            SchemaVersion = "1.0.0",
            Id = "org.agopengps.plugins.bundle",
            Name = "Simulation Bundle",
            Version = "1.0.0",
            RequiredApis = { ["core"] = ">=1.0.0" },
            SimulationProviders =
            {
                new PluginSimProvider
                {
                    ProviderId = "autosteer.vehicle",
                    Type = "Aog.Plugins.Autosteer.VehicleProvider",
                    Topics = { "pose" }
                },
                new PluginSimProvider
                {
                    ProviderId = "gnss.basic",
                    Type = "Aog.Plugins.Sim.GnssProvider",
                    Topics = { "pose" }
                }
            }
        };

        var catalog = new SimulationCatalog();
        catalog.Register(new SimulationProviderDescriptor("gnss.basic", new[] { "pose" }));

        Action act = () => manifest.RegisterSimulationProviders(catalog);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*conflicts with an existing provider*");

        catalog.Providers.Should().ContainSingle(descriptor => descriptor.ProviderId == "gnss.basic");
        catalog.Providers.Should().NotContain(descriptor => descriptor.ProviderId == "autosteer.vehicle");
    }

    private static JsonElement Json(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}
