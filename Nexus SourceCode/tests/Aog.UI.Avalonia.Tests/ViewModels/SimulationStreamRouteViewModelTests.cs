using System.Collections.Generic;
using Aog.UI.Avalonia.ViewModels;
using FluentAssertions;
using Xunit;

namespace Aog.UI.Avalonia.Tests.ViewModels;

public sealed class SimulationStreamRouteViewModelTests
{
    [Fact]
    public void SelectedSource_NormalizesMixedCaseAssignments()
    {
        var viewModel = new SimulationStreamRouteViewModel(
            "nmea",
            "MockProvider",
            "Live",
            new List<string> { "MockProvider", "ReplayProvider" },
            new List<string> { "Live" });

        viewModel.SelectedSource = "mockprovider";

        viewModel.SelectedSource.Should().Be("MockProvider");
    }
}
