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
            "pose",
            "sim.vehicle.bicycle",
            "simulation",
            availableSources,
            availableModes);

        viewModel.SelectedMode = "REPLAY";

        viewModel.SelectedMode.Should().Be("replay");
    }
}
