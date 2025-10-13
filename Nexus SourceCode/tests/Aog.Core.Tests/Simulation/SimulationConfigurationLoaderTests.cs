using System;
using Aog.Core.Simulation;
using Aog.Core.Simulation.Configuration;
using FluentAssertions;
using Xunit;

namespace Aog.Core.Tests.Simulation;

public sealed class SimulationConfigurationLoaderTests
{
    private const string SampleJson = """
    {
      "schemaVersion": "1.0.0",
      "providers": [
        {
          "providerId": "sim.clock",
          "outputs": ["time"]
        },
        {
          "providerId": "sim.vehicle",
          "inputs": ["time"],
          "outputs": ["pose"]
        }
      ],
      "routes": [
        {"stream": "pose", "source": "sim.vehicle", "mode": "simulation"}
      ],
      "options": {"seed": 1337, "timeScale": 1.0}
    }
    """;

    [Fact]
    public void Load_ReturnsConfigurationWithProviders()
    {
        var configuration = SimulationConfigurationLoader.Load(SampleJson);

        configuration.SchemaVersion.Should().Be("1.0.0");
        configuration.Providers.Should().HaveCount(2);
        configuration.Routes.Should().ContainSingle(route => route.Stream == "pose" && route.Source == "sim.vehicle");
        configuration.Options.Should().NotBeNull();

        var descriptors = configuration.CreateProviderDescriptors();
        var catalog = new SimulationCatalog();
        foreach (var descriptor in descriptors)
        {
            catalog.Register(descriptor);
        }

        var summary = catalog.BuildGraph().FormatSummary();
        summary.Should().Contain("sim.clock");
        summary.Should().Contain("sim.vehicle");
    }

    [Fact]
    public void Load_Throws_WhenSchemaVersionMissing()
    {
        const string json = "{ \"providers\": [{ \"providerId\": \"sim.a\", \"outputs\": [\"topic\"] }] }";

        var action = () => SimulationConfigurationLoader.Load(json);

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*schemaVersion*");
    }

    [Fact]
    public void Load_Throws_WhenProviderIdMissing()
    {
        const string json = "{ \"schemaVersion\": \"1.0.0\", \"providers\": [{ \"outputs\": [\"topic\"] }] }";

        var action = () => SimulationConfigurationLoader.Load(json);

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*providerId*");
    }
}
