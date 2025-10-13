using Aog.UI.Avalonia.ViewModels;
using FluentAssertions;
using Xunit;

namespace Aog.UI.Avalonia.Tests;

public sealed class MainWindowViewModelTests
{
    [Fact]
    public void SimulationGraphSummary_ExposesEmbeddedGraph()
    {
        var viewModel = new MainWindowViewModel();

        viewModel.SimulationGraphSummary.Should().Contain("Simulation Provider Graph");
        viewModel.SimulationGraphSummary.Should().Contain("sim.clock.fixed");
    }
}
