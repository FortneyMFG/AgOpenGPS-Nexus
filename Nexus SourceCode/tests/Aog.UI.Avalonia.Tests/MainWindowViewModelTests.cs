using System.Linq;
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

    [Fact]
    public void SimulationBar_ExposesRoutesFromConfiguration()
    {
        var viewModel = new MainWindowViewModel();

        viewModel.SimulationBar.Should().NotBeNull();
        viewModel.SimulationBar.Routes.Should().NotBeEmpty();
        viewModel.SimulationBar.Routes.Select(route => route.Stream)
            .Should().Contain(new[] { "pose", "imu" });
    }
}
