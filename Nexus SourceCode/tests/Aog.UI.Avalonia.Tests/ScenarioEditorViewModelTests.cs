using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Aog.Core.Simulation.Configuration;
using Aog.UI.Avalonia.ViewModels;
using FluentAssertions;
using Xunit;

namespace Aog.UI.Avalonia.Tests;

public sealed class ScenarioEditorViewModelTests
{
    [Fact]
    public void ApplyScenarioCommand_InvokesCallback()
    {
        var configuration = LoadConfiguration();
        var scenarios = configuration.Scenarios.ToList();
        SimulationScenarioConfiguration? appliedScenario = null;

        var viewModel = new ScenarioEditorViewModel(
            configuration,
            scenarios,
            scenario => appliedScenario = scenario,
            () => { });

        viewModel.SelectedScenario.Should().NotBeNull();
        viewModel.ApplyScenarioCommand.CanExecute(null).Should().BeTrue();

        viewModel.ApplyScenarioCommand.Execute(null);

        appliedScenario.Should().NotBeNull();
        appliedScenario!.ScenarioId.Should().Be(configuration.Scenarios[0].ScenarioId);
    }

    [Fact]
    public void TryExportSelectedScenario_ProducesSchemaCompliantJson()
    {
        var configuration = LoadConfiguration();
        var scenarios = configuration.Scenarios.ToList();

        var viewModel = new ScenarioEditorViewModel(
            configuration,
            scenarios,
            _ => { },
            () => { });

        var result = viewModel.TryExportSelectedScenario(out var json, out var errorMessage);

        result.Should().BeTrue();
        errorMessage.Should().BeNull();

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        root.GetProperty("scenarioId").GetString().Should().NotBeNullOrWhiteSpace();
        root.GetProperty("routes").EnumerateArray().Should().NotBeEmpty();
    }

    [Fact]
    public void TryImportScenarioJson_AddsScenarioAndApplies()
    {
        var configuration = LoadConfiguration();
        var scenarios = configuration.Scenarios.ToList();
        var applied = new List<string>();

        var viewModel = new ScenarioEditorViewModel(
            configuration,
            scenarios,
            scenario => applied.Add(scenario.ScenarioId),
            () => { });

        const string json = """
{
  "scenarioId": "imported",
  "description": "Imported for testing",
  "routes": [
    { "stream": "pose", "source": "sim.vehicle.bicycle", "mode": "simulation" }
  ],
  "options": { "timeScale": 0.5 }
}
""";

        var success = viewModel.TryImportScenarioJson(json, out var errorMessage);

        success.Should().BeTrue();
        errorMessage.Should().BeNull();
        viewModel.Scenarios.Should().ContainSingle(vm => vm.ScenarioId == "imported");
        applied.Should().Contain("imported");
    }

    private static SimulationConfiguration LoadConfiguration()
    {
        const string sample = """
{
  "schemaVersion": "1.0.0",
  "providers": [
    { "providerId": "sim.clock.fixed", "outputs": ["time"] },
    { "providerId": "sim.vehicle.bicycle", "inputs": ["time"], "outputs": ["pose"] }
  ],
  "routes": [
    { "stream": "pose", "source": "sim.vehicle.bicycle", "mode": "simulation" }
  ],
  "options": { "seed": 2024, "timeScale": 1.0 },
  "scenarios": [
    {
      "scenarioId": "default",
      "description": "Default scenario",
      "routes": [
        { "stream": "pose", "source": "sim.vehicle.bicycle", "mode": "simulation" }
      ],
      "options": { "timeScale": 1.0 }
    }
  ]
}
""";

        return SimulationConfigurationLoader.Load(sample);
    }
}
