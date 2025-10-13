using System.Linq;
using Aog.Core.Simulation.Configuration;
using Aog.UI.Avalonia.ViewModels;
using FluentAssertions;
using Xunit;

namespace Aog.UI.Avalonia.Tests;

public sealed class SimulationBarViewModelTests
{
    [Fact]
    public void TogglePlaybackCommand_TogglesState()
    {
        var viewModel = CreateViewModel();

        viewModel.StatusText.Should().Be("Paused");
        viewModel.PlayPauseLabel.Should().Be("Play");

        viewModel.TogglePlaybackCommand.Execute(null);

        viewModel.StatusText.Should().Be("Playing");
        viewModel.PlayPauseLabel.Should().Be("Pause");
    }

    [Fact]
    public void SeekFraction_UpdatesPosition()
    {
        var viewModel = CreateViewModel();

        viewModel.SeekFraction = 0.5;

        viewModel.SeekFraction.Should().BeApproximately(0.5, 1e-6);
        viewModel.Position.TotalSeconds.Should().BeGreaterThan(0);
        viewModel.PositionDisplay.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void SelectingPlaybackRate_UpdatesSelection()
    {
        var viewModel = CreateViewModel();

        var doubleRate = viewModel.PlaybackRates.Single(rate => rate.Label == "2×");
        doubleRate.SelectCommand.Execute(null);

        viewModel.SelectedPlaybackRate.Should().Be(2.0);
        doubleRate.IsSelected.Should().BeTrue();
    }

    private static SimulationBarViewModel CreateViewModel()
    {
        const string json = """
{
  "schemaVersion": "1.0.0",
  "providers": [
    { "providerId": "sim.clock.fixed", "outputs": ["time"] },
    { "providerId": "sim.vehicle.bicycle", "inputs": ["time"], "outputs": ["pose"] },
    { "providerId": "sim.imu.synthetic", "inputs": ["time", "pose"], "outputs": ["imu"] }
  ],
  "routes": [
    { "stream": "pose", "source": "sim.vehicle.bicycle", "mode": "simulation" },
    { "stream": "imu", "source": "sim.imu.synthetic", "mode": "simulation" }
  ],
  "options": { "seed": 2024, "timeScale": 1.0 },
  "scenarios": []
}
""";

        var configuration = SimulationConfigurationLoader.Load(json);
        return new SimulationBarViewModel(configuration);
    }
}
