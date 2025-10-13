using System.Linq;
using Aog.Core.V1;
using Aog.UI.Avalonia.Settings;
using Aog.UI.Avalonia.ViewModels;
using FluentAssertions;
using Xunit;

namespace Aog.UI.Avalonia.Tests;

public sealed class MainWindowViewModelTests
{
    [Fact]
    public void SimulationGraphSummary_ExposesEmbeddedGraph()
    {
        var viewModel = CreateViewModel();

        viewModel.SimulationGraphSummary.Should().Contain("Simulation Provider Graph");
        viewModel.SimulationGraphSummary.Should().Contain("sim.clock.fixed");
    }

    [Fact]
    public void SimulationBar_ExposesRoutesFromConfiguration()
    {
        var viewModel = CreateViewModel();

        viewModel.SimulationBar.Should().NotBeNull();
        viewModel.SimulationBar.Routes.Should().NotBeEmpty();
        viewModel.SimulationBar.Routes.Select(route => route.Stream)
            .Should().Contain(new[] { "pose", "imu" });
    }

    [Fact]
    public void PluginPanels_ExposeSampleState()
    {
        var viewModel = CreateViewModel();

        viewModel.SteerPanel.IsEnabled.Should().BeTrue();
        viewModel.SteerPanel.TargetWheelAngleDegrees.Should().BeApproximately(2.5, 1e-3);

        viewModel.SectionsPanel.Sections.Should().HaveCount(8);
        viewModel.SectionsPanel.CurrentMask.Should().Be(0b0011_1100u);

        viewModel.PlanterPanel.Rows.Should().HaveCountGreaterThan(0);
        viewModel.PlanterPanel.Rows.Single(row => row.RowIndex == 2).Quality.Should().Be(PlanterRowQuality.Double);
        viewModel.PlanterPanel.Summary.Should().Contain("Rows:");
    }

    [Fact]
    public void MapOverlays_ExposeCoverageAndGuidance()
    {
        var viewModel = CreateViewModel();

        viewModel.CoverageCells.Should().NotBeEmpty();
        viewModel.GuidanceTracks.Should().NotBeEmpty();
        viewModel.GuidanceTracks.Select(track => track.Points.Count).Max().Should().BeGreaterThan(1);
    }

    [Fact]
    public void Dashboards_SurfaceSampleHistory()
    {
        var viewModel = CreateViewModel();

        viewModel.SteerDashboard.CrossTrackErrorHistory.Should().NotBeEmpty();
        viewModel.ReplayTimeline.Bookmarks.Should().NotBeEmpty();
        viewModel.ReplayTimeline.SpeedSamples.Should().HaveCountGreaterThan(10);
    }

    private static MainWindowViewModel CreateViewModel()
    {
        var store = new InMemoryConnectionSettingsStore();
        var connection = new ConnectionSettingsViewModel(store);
        return new MainWindowViewModel(connection);
    }

    private sealed class InMemoryConnectionSettingsStore : IConnectionSettingsStore
    {
        private ConnectionSettings _settings = new();

        public ConnectionSettings Load() => _settings.Clone();

        public void Save(ConnectionSettings settings)
        {
            _settings = settings.Clone();
        }
    }
}
