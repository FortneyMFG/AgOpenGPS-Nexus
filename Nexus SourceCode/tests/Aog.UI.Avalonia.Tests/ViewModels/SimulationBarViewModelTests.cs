using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Aog.Core.Legacy;
using Aog.Core.Paths;
using Aog.Core.Replay;
using Aog.Core.Simulation.Configuration;
using Aog.UI.Avalonia.ViewModels;
using Avalonia.Threading;
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
    public void PlaybackRates_DisallowExternalMutation()
    {
        using var viewModel = CreateViewModel();

        var readOnlyRates = viewModel.PlaybackRates;
        var attempt = () => ((IList<SimulationPlaybackRateOptionViewModel>)readOnlyRates)
            .Add(new SimulationPlaybackRateOptionViewModel(3.0, _ => { }));

        attempt.Should().Throw<NotSupportedException>();
        readOnlyRates.Should().HaveCount(3);
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
    public void PlaybackRateLabel_UsesCurrentUICulture()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUiCulture = CultureInfo.CurrentUICulture;

        try
        {
            var culture = CultureInfo.GetCultureInfo("fr-FR");
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;

            using var viewModel = CreateViewModel();
            var scenario = new SimulationScenarioConfiguration(
                "fractional-rate",
                "Scenario requesting 1.5x speed",
                new[]
                {
                    new SimulationRouteConfiguration("pose", "sim.vehicle.bicycle", "simulation")
                },
                new SimulationOptionsConfiguration(2024, 1.5));

            viewModel.ApplyScenario(scenario);

            viewModel.SelectedPlaybackRateLabel.Should().Be("1,5×");
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
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

        using var viewModel = CreateViewModel(configuration);

        viewModel.SelectedPlaybackRate.Should().Be(0.75);
        viewModel.SelectedPlaybackRateLabel.Should().Be("0.75×");
        viewModel.PlaybackRates.Select(option => option.Rate)
            .Should()
            .Equal(0.5, 0.75, 1.0, 2.0);

        var customRate = viewModel.PlaybackRates.Single(option => Math.Abs(option.Rate - 0.75) < 1e-6);
        customRate.IsSelected.Should().BeTrue();
        customRate.Label.Should().Be($"{0.75:0.#}×");
        viewModel.PlaybackRates.Count(option => option.IsSelected).Should().Be(1);
    }

    [Fact]
    public void ApplyScenario_UpdatesMetadataAndRoutes()
    {
        var configuration = CreateConfigurationWithScenario();
        using var viewModel = CreateViewModel(configuration);
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
        using var viewModel = CreateViewModel(configuration);
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
        viewModel.PlaybackRates.Count(option => option.IsSelected).Should().Be(1);
    }

    [Fact]
    public void ApplyScenario_RaisesPlaybackRateNotificationsEvenWhenRateUnchanged()
    {
        var configuration = CreateConfigurationWithScenario();
        using var viewModel = new SimulationBarViewModel(configuration);
        var notifications = new List<string>();
        viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName is not null)
            {
                notifications.Add(args.PropertyName);
            }
        };

        var scenario = new SimulationScenarioConfiguration(
            "same-rate",
            "Scenario that keeps the default playback rate",
            new[]
            {
                new SimulationRouteConfiguration("pose", "sim.vehicle.bicycle", "simulation")
            },
            new SimulationOptionsConfiguration(2024, 1.0));

        viewModel.ApplyScenario(scenario);

        notifications.Count(name => name == nameof(SimulationBarViewModel.SelectedPlaybackRate))
            .Should()
            .BeGreaterThan(0);
        notifications.Count(name => name == nameof(SimulationBarViewModel.SelectedPlaybackRateLabel))
            .Should()
            .BeGreaterThan(0);
    }

    [Fact]
    public void SelectedPlaybackRateLabel_WhenOptionHasNoLabel_FallsBackToMultiplier()
    {
        using var viewModel = CreateViewModel();

        var playbackRatesField = typeof(SimulationBarViewModel)
            .GetField("_playbackRates", BindingFlags.Instance | BindingFlags.NonPublic);
        playbackRatesField.Should().NotBeNull();

        var playbackRates = (IList<SimulationPlaybackRateOptionViewModel>)playbackRatesField!
            .GetValue(viewModel)!;

        var unlabeledOption = CreatePlaybackRateOptionWithoutLabel(3.25);
        playbackRates.Add(unlabeledOption);

        viewModel.SetSelectedPlaybackRate(3.25);

        viewModel.SelectedPlaybackRate.Should().Be(3.25);
        viewModel.SelectedPlaybackRateLabel.Should().Be("3.25×");
    }

    [Fact]
    public void ResetToConfigurationRoutes_WhenConfigurationOmitsTimeScale_RevertsToNormalRate()
    {
        var configuration = CreateConfigurationWithoutTimeScaleOption();
        using var viewModel = CreateViewModel(configuration);
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
        using var viewModel = CreateViewModel(configuration);

        var scenario = new SimulationScenarioConfiguration(
            "legacy-import",
            "Legacy import scenario",
            new[]
            {
                new SimulationRouteConfiguration("pose", "sim.vehicle.bicycle", "simulation")
            },
            new SimulationOptionsConfiguration(null, 0.75));

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

        viewModel.SelectedPlaybackRate.Should().Be(0.75);
        viewModel.PlaybackRates.Select(option => option.Rate)
            .Should()
            .Equal(0.5, 0.75, 1.0, 2.0);
        viewModel.ActiveScenarioOptions.Should().Contain("timeScale=0.75");
    }

    [Fact]
    public void ResetToConfigurationRoutes_RestoresConfigurationPlaybackRate()
    {
        var configuration = CreateConfigurationWithTimeScale(1.2);
        using var viewModel = CreateViewModel(configuration);

        var doubleRate = viewModel.PlaybackRates.Single(rate => Math.Abs(rate.Rate - 2.0) < 1e-6);
        doubleRate.SelectCommand.Execute(null);

        viewModel.SelectedPlaybackRate.Should().Be(2.0);

        viewModel.ResetToConfigurationRoutes();

        viewModel.SelectedPlaybackRate.Should().BeApproximately(1.2, 1e-6);
    }

    [Fact]
        [Fact]
        public void SetSelectedPlaybackRate_WhenBelowMinimum_ClampsToLowerBound()
        {
            using var viewModel = CreateViewModel();

            viewModel.SetSelectedPlaybackRate(-2);

            viewModel.SelectedPlaybackRate.Should().BeApproximately(0.1, 1e-6);
            viewModel.SelectedPlaybackRateLabel.Should().Be("0.1×");
        }

        [Fact]
        public void SetSelectedPlaybackRate_WhenAboveMaximum_ClampsToUpperBound()
        {
            using var viewModel = CreateViewModel();

            viewModel.SetSelectedPlaybackRate(10);

            viewModel.SelectedPlaybackRate.Should().BeApproximately(4.0, 1e-6);
            viewModel.SelectedPlaybackRateLabel.Should().Be("4×");
        }

        [Fact]
        public void ResetToConfigurationRoutes_RaisesPlaybackRateNotificationsEvenWhenAlreadyAtConfigurationRate()
        {
            var configuration = CreateConfigurationWithScenario();
            using var viewModel = new SimulationBarViewModel(configuration);
            var notifications = new List<string>();
            viewModel.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName is not null)
                {
                    notifications.Add(args.PropertyName);
                }
            };

            viewModel.ResetToConfigurationRoutes();

            notifications.Count(name => name == nameof(SimulationBarViewModel.SelectedPlaybackRate))
                .Should().BeGreaterThan(0);
            notifications.Count(name => name == nameof(SimulationBarViewModel.SelectedPlaybackRateLabel))
                .Should().BeGreaterThan(0);
        }

    [Fact]
    public void DisposingAndRecreatingViewModel_DoesNotDuplicateReplayNotifications()
    {
        var configuration = CreateConfigurationWithScenario();
        var replayController = new ReplayControllerStub();

        var first = CreateViewModel(configuration, replayController);
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

        var second = CreateViewModel(configuration, replayController);
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

            using (var viewModel = CreateViewModel(configuration, replayController))
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

        using var viewModel = CreateViewModel(configuration, replayController, logger);

        viewModel.TogglePlaybackCommand.Execute(null);

        await WaitForLogAsync(logger);

        logger.Entries.Should().Contain(
            entry => entry.Level == LogLevel.Error && entry.Message.Contains("start playback", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task TogglePlaybackCommand_WhenReplayControllerCancels_LogsInformation()
    {
        var configuration = CreateConfigurationWithScenario();
        var logger = new TestLogger<SimulationBarViewModel>();
        var replayController = new ReplayControllerStub(
            playAsync: () => ValueTask.FromException(new OperationCanceledException()));

        using var viewModel = CreateViewModel(configuration, replayController, logger);

        viewModel.TogglePlaybackCommand.Execute(null);

        await WaitForLogAsync(logger);

        logger.Entries.Should().HaveCount(1);
        logger.Entries.Should().OnlyContain(entry => entry.Level == LogLevel.Information);
        logger.Entries.Should().ContainSingle(
            entry => entry.Message.Contains("start playback", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task SeekFraction_WhenReplayControllerFails_LogsError()
    {
        var configuration = CreateConfigurationWithScenario();
        var logger = new TestLogger<SimulationBarViewModel>();
        var replayController = new ReplayControllerStub(
            seekAsync: _ => new ValueTask(Task.Run(() => throw new InvalidOperationException("seek failed"))));

        using var viewModel = CreateViewModel(configuration, replayController, logger);

        viewModel.SeekFraction = 0.5;

        await WaitForLogAsync(logger);

        logger.Entries.Should().Contain(
            entry => entry.Level == LogLevel.Error && entry.Message.Contains("seek to", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task SeekFraction_WhenUpdatedRapidly_OnlySeeksToFinalPosition()
    {
        var configuration = CreateConfigurationWithScenario();
        var positions = new ConcurrentQueue<TimeSpan>();
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var replayController = new ReplayControllerStub(
            seekAsync: position =>
            {
                positions.Enqueue(position);
                completion.TrySetResult();
                return ValueTask.CompletedTask;
            });

        using var viewModel = new SimulationBarViewModel(configuration, replayController);

        viewModel.SeekFraction = 0.1;
        viewModel.SeekFraction = 0.2;
        viewModel.SeekFraction = 0.3;

        await completion.Task.WaitAsync(TimeSpan.FromSeconds(1));
        await Task.Delay(100);

        var recorded = positions.ToArray();
        recorded.Should().ContainSingle();

        var expectedPosition = TimeSpan.FromTicks((long)(viewModel.Duration.Ticks * 0.3));
        recorded[0].Should().Be(expectedPosition);
    }
// in your test class

[Fact]
public void ToggleAutoResumeCommand_WithNoSession_DoesNotChangeState()
{
    using var viewModel = CreateViewModel();

    Action exec = () => viewModel.ToggleAutoResumeCommand.Execute(null);
    exec.Should().NotThrow();
    viewModel.IsAutoResumeEnabled.Should().BeFalse();
}

private static SimulationPlaybackRateOptionViewModel CreatePlaybackRateOptionWithoutLabel(double rate)
{
    var option = (SimulationPlaybackRateOptionViewModel)FormatterServices.GetUninitializedObject(
        typeof(SimulationPlaybackRateOptionViewModel));

    SetField(option, "<Rate>k__BackingField", rate);
    SetField(option, "<Label>k__BackingField", null);
    SetField(option, "_onSelected", new Action<SimulationPlaybackRateOptionViewModel>(_ => { }));
    SetField(option, "<SelectCommand>k__BackingField", new DelegateCommand(_ => { }));

    return option;
}

private static void SetField(object target, string fieldName, object? value)
{
    var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
               ?? throw new InvalidOperationException($"Field '{fieldName}' not found.");
    field.SetValue(target, value);
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
        return CreateViewModel(configuration);
    }

    private static SimulationBarViewModel CreateViewModel(
        SimulationConfiguration configuration,
        IReplayController? replayController = null,
        ILogger<SimulationBarViewModel>? logger = null,
        IDispatcher? dispatcher = null)
    {
        return new SimulationBarViewModel(configuration, replayController, logger, dispatcher ?? new InlineDispatcher());
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

    private sealed class InlineDispatcher : IDispatcher
    {
        public bool CheckAccess() => true;

        public void VerifyAccess()
        {
        }

        public void Post(Action action, DispatcherPriority priority = default)
        {
            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            action();
        }
    }

    private sealed class RecordingDispatcher : IDispatcher, IDisposable
    {
        private readonly BlockingCollection<(Action action, DispatcherPriority priority)> _queue = new();
        private readonly List<int> _executedThreadIds = new();
        private readonly ManualResetEventSlim _started = new();
        private readonly Thread _thread;
        private readonly object _gate = new();
        private bool _disposed;

        public RecordingDispatcher()
        {
            _thread = new Thread(ProcessQueue)
            {
                IsBackground = true,
                Name = "RecordingDispatcher"
            };

            _thread.Start();
            _started.Wait();
        }

        public int DispatcherThreadId { get; private set; }

        public IReadOnlyList<int> ExecutedThreadIds
        {
            get
            {
                lock (_gate)
                {
                    return _executedThreadIds.ToArray();
                }
            }
        }

        public bool CheckAccess() => Thread.CurrentThread.ManagedThreadId == DispatcherThreadId;

        public void VerifyAccess()
        {
            if (!CheckAccess())
            {
                throw new InvalidOperationException("Access from non-dispatcher thread.");
            }
        }

        public void Post(Action action, DispatcherPriority priority = default)
        {
            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(RecordingDispatcher));
            }

            _queue.Add((action, priority));
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _queue.CompleteAdding();
            _thread.Join();
        }

        private void ProcessQueue()
        {
            DispatcherThreadId = Thread.CurrentThread.ManagedThreadId;
            _started.Set();

            foreach (var (action, _) in _queue.GetConsumingEnumerable())
            {
                action();

                lock (_gate)
                {
                    _executedThreadIds.Add(Thread.CurrentThread.ManagedThreadId);
                }
            }
        }
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
