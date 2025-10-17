using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aog.Core.Legacy;
using Aog.Core.Paths;
using Aog.Core.Replay;
using Aog.Core.Simulation.Configuration;
using Aog.UI.Avalonia.ViewModels;
using FluentAssertions;
using Microsoft.Extensions.Logging;
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
            .ContainInOrder("0.5×", "1×", "2×");
    }

    [Fact]
    public void Constructor_WithConfiguredTimeScale_UsesConfiguredPlaybackRate()
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
  "options": { "seed": 2024, "timeScale": 0.75 },
  "scenarios": []
}
""";

        var configuration = SimulationConfigurationLoader.Load(json);

        using var viewModel = new SimulationBarViewModel(configuration);

        viewModel.SelectedPlaybackRate.Should().Be(0.75);
        viewModel.SelectedPlaybackRateLabel.Should().Be("0.75×");
        viewModel.PlaybackRates.Select(option => option.Rate)
            .Should()
            .Equal(0.5, 0.75, 1.0, 2.0);

        var customRate = viewModel.PlaybackRates.Single(option => Math.Abs(option.Rate - 0.75) < 1e-6);
        customRate.IsSelected.Should().BeTrue();
        customRate.Label.Should().Be($"{0.75:0.#}×");
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
            new SimulationOptionsConfiguration(1337, 1.5));

        viewModel.ApplyScenario(scenario);

        viewModel.ActiveScenarioTitle.Should().Be("Scenario: test");
        viewModel.ActiveScenarioDescription.Should().Contain("Scenario for testing");
        viewModel.ActiveScenarioOptions.Should().Contain("seed=1337");
        viewModel.SelectedPlaybackRate.Should().Be(1.5);
        viewModel.Routes.Should().ContainSingle(route => route.Stream == "pose");

        viewModel.ResetToConfigurationRoutes();
        viewModel.ActiveScenarioTitle.Should().Be("Scenario: configuration defaults");
        viewModel.SelectedPlaybackRate.Should().Be(1.0);
        viewModel.ActiveScenarioOptions.Should().Contain("seed=2024");
        viewModel.ActiveScenarioOptions.Should().Contain("timeScale=1");
    }

    [Fact]
    public void ApplyScenario_WithCustomPlaybackRate_AddsPlaybackRateOption()
    {
        var configuration = CreateConfigurationWithScenario();
        using var viewModel = new SimulationBarViewModel(configuration);
        var scenario = new SimulationScenarioConfiguration(
            "custom-rate",
            "Scenario requesting three-quarter speed",
            new[]
            {
                new SimulationRouteConfiguration("pose", "sim.vehicle.bicycle", "simulation")
            },
            new SimulationOptionsConfiguration(42, 0.75));

        viewModel.ApplyScenario(scenario);

        viewModel.SelectedPlaybackRate.Should().Be(0.75);
        viewModel.PlaybackRates.Select(option => option.Rate)
            .Should()
            .Equal(0.5, 0.75, 1.0, 2.0);
        viewModel.PlaybackRates.Single(option => Math.Abs(option.Rate - 0.75) < 1e-6).IsSelected.Should().BeTrue();
    }

    [Fact]
    public void ResetToConfigurationRoutes_WhenConfigurationOmitsTimeScale_RevertsToNormalRate()
    {
        var configuration = CreateConfigurationWithoutTimeScaleOption();
        using var viewModel = new SimulationBarViewModel(configuration);
        var scenario = new SimulationScenarioConfiguration(
            "half-speed",
            "Scenario that halves playback speed",
            new[]
            {
                new SimulationRouteConfiguration("pose", "sim.vehicle.bicycle", "simulation")
            },
            new SimulationOptionsConfiguration(null, 0.5));

        viewModel.ApplyScenario(scenario);
        viewModel.SelectedPlaybackRate.Should().Be(0.5);

        viewModel.ResetToConfigurationRoutes();

        viewModel.SelectedPlaybackRate.Should().Be(1.0);
        viewModel.ActiveScenarioOptions.Should().Contain("seed=2024");
        viewModel.ActiveScenarioOptions.Should().NotContain("timeScale");
    }

    [Fact]
    public void ApplyLegacyImport_WithTimeScale_AdjustsPlaybackRate()
    {
        var configuration = CreateConfigurationWithScenario();
        using var viewModel = new SimulationBarViewModel(configuration);

        var scenario = new SimulationScenarioConfiguration(
            "legacy-import",
            "Legacy import scenario",
            new[]
            {
                new SimulationRouteConfiguration("pose", "sim.vehicle.bicycle", "simulation")
            },
            new SimulationOptionsConfiguration(null, 0.5));

        var result = new LegacyGuidanceImportResult(
            "Field A",
            new GeographicCoordinate(40.0, -93.0),
            new[]
            {
                new LegacyAbLinePlanar(
                    "AB1",
                    new GeographicCoordinate(40.0, -93.0),
                    new GeographicCoordinate(40.0001, -93.0001),
                    new PlanarPoint(0, 0),
                    new PlanarPoint(10, 0),
                    0,
                    10)
            },
            new[]
            {
                new PlanarPoint(0, 0),
                new PlanarPoint(0, 10),
                new PlanarPoint(10, 10),
                new PlanarPoint(10, 0)
            },
            scenario);

        viewModel.ApplyLegacyImport(result);

        viewModel.SelectedPlaybackRate.Should().Be(0.5);
        viewModel.ActiveScenarioOptions.Should().Contain("timeScale=0.5");
    }

    [Fact]
    public void ResetToConfigurationRoutes_RestoresConfigurationPlaybackRate()
    {
        var configuration = CreateConfigurationWithTimeScale(1.2);
        using var viewModel = new SimulationBarViewModel(configuration);

        var doubleRate = viewModel.PlaybackRates.Single(rate => Math.Abs(rate.Rate - 2.0) < 1e-6);
        doubleRate.SelectCommand.Execute(null);

        viewModel.SelectedPlaybackRate.Should().Be(2.0);

        viewModel.ResetToConfigurationRoutes();

        viewModel.SelectedPlaybackRate.Should().BeApproximately(1.2, 1e-6);
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

    [Fact]
    public async Task TogglePlaybackCommand_WhenReplayControllerFails_LogsError()
    {
        var configuration = CreateConfigurationWithScenario();
        var logger = new TestLogger<SimulationBarViewModel>();
        var replayController = new ReplayControllerStub(
            playAsync: () => ValueTask.FromException(new InvalidOperationException("play failed")));

        using var viewModel = new SimulationBarViewModel(configuration, replayController, logger);

        viewModel.TogglePlaybackCommand.Execute(null);

        await WaitForLogAsync(logger);

        logger.Entries.Should().Contain(
            entry => entry.Level == LogLevel.Error && entry.Message.Contains("start playback", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task TogglePlaybackCommand_WhenReplayControllerCancels_DoesNotLogError()
    {
        var configuration = CreateConfigurationWithScenario();
        var logger = new TestLogger<SimulationBarViewModel>();
        var replayController = new ReplayControllerStub(
            playAsync: () => ValueTask.FromException(new OperationCanceledException()));

        using var viewModel = new SimulationBarViewModel(configuration, replayController, logger);

        viewModel.TogglePlaybackCommand.Execute(null);

        await WaitForLogAsync(logger);

        logger.Entries.Should().BeEmpty();
    }

    [Fact]
    public async Task SeekFraction_WhenReplayControllerFails_LogsError()
    {
        var configuration = CreateConfigurationWithScenario();
        var logger = new TestLogger<SimulationBarViewModel>();
        var replayController = new ReplayControllerStub(
            seekAsync: _ => new ValueTask(Task.Run(() => throw new InvalidOperationException("seek failed"))));

        using var viewModel = new SimulationBarViewModel(configuration, replayController, logger);

        viewModel.SeekFraction = 0.5;

        await WaitForLogAsync(logger);

        logger.Entries.Should().Contain(
            entry => entry.Level == LogLevel.Error && entry.Message.Contains("seek to", StringComparison.OrdinalIgnoreCase));
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

    private static SimulationConfiguration CreateConfigurationWithTimeScale(double timeScale)
    {
        var json = FormattableString.Invariant($"""
{
  "schemaVersion": "1.0.0",
  "providers": [
    { "providerId": "sim.clock.fixed", "outputs": ["time"] },
    { "providerId": "sim.vehicle.bicycle", "inputs": ["time"], "outputs": ["pose"] }
  ],
  "routes": [
    { "stream": "pose", "source": "sim.vehicle.bicycle", "mode": "simulation" }
  ],
  "options": { "seed": 2024, "timeScale": {timeScale:0.###} },
  "scenarios": []
}
""");

        return SimulationConfigurationLoader.Load(json);
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

    private static SimulationConfiguration CreateConfigurationWithoutTimeScaleOption()
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
  "options": { "seed": 2024 },
  "scenarios": []
}
""";

        return SimulationConfigurationLoader.Load(json);
    }

    private sealed class ReplayControllerStub : IReplayController
    {
        private EventHandler<ReplayStateChangedEventArgs>? _stateChanged;
        private readonly Func<ValueTask>? _playAsync;
        private readonly Func<ValueTask>? _pauseAsync;
        private readonly Func<TimeSpan, ValueTask>? _seekAsync;
        private readonly Func<double, ValueTask>? _setPlaybackRateAsync;

        public int SubscriptionCount { get; private set; }

        public int HandlerInvocationCount { get; private set; }

        public ReplayState State { get; private set; } = new(false, TimeSpan.Zero, TimeSpan.FromMinutes(5), 1.0);

        public ReplayControllerStub(
            Func<ValueTask>? playAsync = null,
            Func<ValueTask>? pauseAsync = null,
            Func<TimeSpan, ValueTask>? seekAsync = null,
            Func<double, ValueTask>? setPlaybackRateAsync = null)
        {
            _playAsync = playAsync;
            _pauseAsync = pauseAsync;
            _seekAsync = seekAsync;
            _setPlaybackRateAsync = setPlaybackRateAsync;
        }

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

        public ValueTask PlayAsync(CancellationToken cancellationToken = default)
        {
            if (_playAsync is not null)
            {
                return _playAsync();
            }

            State = State with { IsPlaying = true };
            return ValueTask.CompletedTask;
        }

        public ValueTask PauseAsync(CancellationToken cancellationToken = default)
        {
            if (_pauseAsync is not null)
            {
                return _pauseAsync();
            }

            State = State with { IsPlaying = false };
            return ValueTask.CompletedTask;
        }

        public ValueTask SeekAsync(TimeSpan position, CancellationToken cancellationToken = default)
        {
            if (_seekAsync is not null)
            {
                return _seekAsync(position);
            }

            State = State with { Position = position };
            return ValueTask.CompletedTask;
        }

        public ValueTask SetPlaybackRateAsync(double playbackRate, CancellationToken cancellationToken = default)
        {
            if (_setPlaybackRateAsync is not null)
            {
                return _setPlaybackRateAsync(playbackRate);
            }

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

    private static async Task WaitForLogAsync(TestLogger<SimulationBarViewModel> logger)
    {
        for (var attempt = 0; attempt < 10 && !logger.HasEntries; attempt++)
        {
            await Task.Delay(10);
        }
    }

    private sealed class TestLogger<T> : ILogger<T>
    {
        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();

            public void Dispose()
            {
            }
        }

        private readonly ConcurrentQueue<(LogLevel Level, string Message)> _entries = new();

        public IReadOnlyCollection<(LogLevel Level, string Message)> Entries => _entries.ToArray();

        public bool HasEntries => !_entries.IsEmpty;

        public IDisposable BeginScope<TState>(TState state) => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (formatter is null)
            {
                throw new ArgumentNullException(nameof(formatter));
            }

            var message = formatter(state, exception);
            _entries.Enqueue((logLevel, message));
        }
    }
}
