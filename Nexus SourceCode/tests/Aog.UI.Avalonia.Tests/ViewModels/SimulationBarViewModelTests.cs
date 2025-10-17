using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aog.Core.Replay;
using Aog.Core.Simulation.Configuration;
using Aog.UI.Avalonia.ViewModels;
using FluentAssertions;
using Xunit;

namespace Aog.UI.Avalonia.Tests.ViewModels;

public sealed class SimulationBarViewModelTests
{
    [Fact]
    public void TogglePlaybackCommand_TogglesState()
    {
        using var viewModel = CreateViewModel();

        viewModel.StatusText.Should().Be("Paused");
        viewModel.PlayPauseLabel.Should().Be("Play");

        viewModel.TogglePlaybackCommand.Execute(null);

        viewModel.StatusText.Should().Be("Playing");
        viewModel.PlayPauseLabel.Should().Be("Pause");
    }

    [Fact]
    public void SeekFraction_UpdatesPosition()
    {
        using var viewModel = CreateViewModel();

        viewModel.SeekFraction = 0.5;

        viewModel.SeekFraction.Should().BeApproximately(0.5, 1e-6);
        viewModel.Position.TotalSeconds.Should().BeGreaterThan(0);
        viewModel.PositionDisplay.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void SelectingPlaybackRate_UpdatesSelection()
    {
        using var viewModel = CreateViewModel();

        var doubleRate = viewModel.PlaybackRates.Single(rate => Math.Abs(rate.Rate - 2.0) < 1e-6);
        doubleRate.SelectCommand.Execute(null);

        viewModel.SelectedPlaybackRate.Should().Be(2.0);
        doubleRate.IsSelected.Should().BeTrue();
    }

    [Fact]
    public void PlaybackRates_ExposeFormattedLabels()
    {
        using var viewModel = CreateViewModel();

        viewModel.PlaybackRates.Select(option => option.Label)
            .Should()
            .ContainInOrder("50%", "100%", "200%");
    }

    [Fact]
    public void ApplyScenario_UpdatesMetadataAndRoutes()
    {
        var configuration = CreateConfigurationWithScenario();
        using var viewModel = new SimulationBarViewModel(configuration);
        var scenario = new SimulationScenarioConfiguration(
            "test",
            "Scenario for testing",
            new[]
            {
                new SimulationRouteConfiguration("pose", "sim.vehicle.bicycle", "simulation")
            },
            new SimulationOptionsConfiguration(1337, 0.75));

        viewModel.ApplyScenario(scenario);

        viewModel.ActiveScenarioTitle.Should().Be("Scenario: test");
        viewModel.ActiveScenarioDescription.Should().Contain("Scenario for testing");
        viewModel.ActiveScenarioOptions.Should().Contain("seed=1337");
        viewModel.Routes.Should().ContainSingle(route => route.Stream == "pose");

        viewModel.ResetToConfigurationRoutes();
        viewModel.ActiveScenarioTitle.Should().Be("Scenario: configuration defaults");
    }

    [Fact]
    public void DisposingAndRecreatingViewModel_DoesNotDuplicateReplayNotifications()
    {
        var configuration = CreateConfigurationWithScenario();
        var replayController = new ReplayControllerStub();

        var first = new SimulationBarViewModel(configuration, replayController);
        var firstStatusNotifications = 0;
        first.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(SimulationBarViewModel.StatusText))
            {
                firstStatusNotifications++;
            }
        };

        replayController.SubscriptionCount.Should().Be(1);

        first.Dispose();
        replayController.SubscriptionCount.Should().Be(0);

        var second = new SimulationBarViewModel(configuration, replayController);
        var secondStatusNotifications = 0;
        second.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(SimulationBarViewModel.StatusText))
            {
                secondStatusNotifications++;
            }
        };

        replayController.SubscriptionCount.Should().Be(1);

        replayController.RaiseStateChanged(new ReplayState(
            true,
            TimeSpan.FromSeconds(10),
            TimeSpan.FromMinutes(5),
            1.0));

        firstStatusNotifications.Should().Be(0);
        secondStatusNotifications.Should().BeGreaterThan(0);
        replayController.HandlerInvocationCount.Should().Be(1);

        second.Dispose();
    }

    [Fact]
    public void CreatingAndDisposingMultipleInstances_ReleasesReplayControllerSubscriptions()
    {
        var configuration = CreateConfigurationWithScenario();
        var replayController = new ReplayControllerStub();

        for (var iteration = 0; iteration < 3; iteration++)
        {
            replayController.SubscriptionCount.Should().Be(0);

            using (var viewModel = new SimulationBarViewModel(configuration, replayController))
            {
                replayController.SubscriptionCount.Should().Be(1);
                viewModel.StatusText.Should().Be("Paused");
            }

            replayController.SubscriptionCount.Should().Be(0);
        }
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

    private static SimulationConfiguration CreateConfigurationWithScenario()
    {
        const string json = """
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

        return SimulationConfigurationLoader.Load(json);
    }

    private sealed class ReplayControllerStub : IReplayController
    {
        private EventHandler<ReplayStateChangedEventArgs>? _stateChanged;

        public int SubscriptionCount { get; private set; }

        public int HandlerInvocationCount { get; private set; }

        public ReplayState State { get; private set; } = new(false, TimeSpan.Zero, TimeSpan.FromMinutes(5), 1.0);

        public event EventHandler<ReplayStateChangedEventArgs>? StateChanged
        {
            add
            {
                _stateChanged += value;
                SubscriptionCount++;
            }
            remove
            {
                _stateChanged -= value;
                SubscriptionCount--;
            }
        }

        public ValueTask PlayAsync(CancellationToken cancellationToken = default) => ValueTask.CompletedTask;

        public ValueTask PauseAsync(CancellationToken cancellationToken = default) => ValueTask.CompletedTask;

        public ValueTask SeekAsync(TimeSpan position, CancellationToken cancellationToken = default)
        {
            State = State with { Position = position };
            return ValueTask.CompletedTask;
        }

        public ValueTask SetPlaybackRateAsync(double playbackRate, CancellationToken cancellationToken = default)
        {
            State = State with { PlaybackRate = playbackRate };
            return ValueTask.CompletedTask;
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        public void RaiseStateChanged(ReplayState state)
        {
            State = state;
            var handlers = _stateChanged;
            if (handlers is null)
            {
                return;
            }

            HandlerInvocationCount += handlers.GetInvocationList().Length;
            handlers.Invoke(this, new ReplayStateChangedEventArgs(state));
        }
    }
}
