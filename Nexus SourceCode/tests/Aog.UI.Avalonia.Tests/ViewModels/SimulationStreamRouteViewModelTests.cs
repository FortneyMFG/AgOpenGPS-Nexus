using System.Collections.Generic;
using Aog.UI.Avalonia.ViewModels;
using FluentAssertions;
using Xunit;

namespace Aog.UI.Avalonia.Tests.ViewModels;

public sealed class SimulationStreamRouteViewModelTests
{
    [Fact]
    public void SelectedMode_WithDifferentCasing_UpdatesToCanonicalValue()
    {
        var availableSources = new List<string> { "sim.vehicle.bicycle" };
        var availableModes = new List<string> { "simulation", "replay" };

        var viewModel = new SimulationStreamRouteViewModel(
            stream: "pose",
            selectedSource: "sim.vehicle.bicycle",
            selectedMode: "simulation",
            availableSources: availableSources,
            availableModes: availableModes);

        viewModel.SelectedMode = "REPLAY";

        viewModel.SelectedMode.Should().Be("replay");
    }

    [Fact]
    public void SelectedSource_NormalizesMixedCaseAssignments()
    {
        var viewModel = new SimulationStreamRouteViewModel(
            stream: "nmea",
            selectedSource: "MockProvider",
            selectedMode: "Live",
            availableSources: new List<string> { "MockProvider", "ReplayProvider" },
            availableModes: new List<string> { "Live" });

        viewModel.SelectedSource = "mockprovider";

        viewModel.SelectedSource.Should().Be("MockProvider");
    }

    [Fact]
    public void SettingUnknownModeOrSource_LeavesValueUnchanged()
    {
        var vm = new SimulationStreamRouteViewModel(
            stream: "pose",
            selectedSource: "sim.vehicle.bicycle",
            selectedMode: "simulation",
            availableSources: new List<string> { "sim.vehicle.bicycle" },
            availableModes: new List<string> { "simulation", "replay" });

        vm.SelectedMode = "not-a-mode";
        vm.SelectedMode.Should().Be("simulation");

        vm.SelectedSource = "not-a-source";
        vm.SelectedSource.Should().Be("sim.vehicle.bicycle");
    }
}
