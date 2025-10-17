using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Aog.Core.Legacy;
using Aog.Core.Replay;
using Aog.Core.Simulation.Configuration;
using Avalonia.Threading;
using Microsoft.Extensions.Logging;

namespace Aog.UI.Avalonia.ViewModels;

/// <summary>
/// View-model powering the simulation transport bar rendered in the shell.
/// </summary>
public class SimulationBarViewModel : ObservableObject, IDisposable
{
    private const double PlaybackRateComparisonTolerance = 1e-6;
    private const double MinPlaybackRate = 0.1;
    private const double MaxPlaybackRate = 4.0;
    private static readonly TimeSpan DefaultDuration = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan SeekDebounceDelay = TimeSpan.FromMilliseconds(50);

    private readonly SimulationConfiguration? _configuration;
    private readonly IReplayController? _replayController;
    private readonly ILogger<SimulationBarViewModel>? _logger;
    private readonly ObservableCollection<SimulationPlaybackRateOptionViewModel> _playbackRates = new();
    private readonly ReadOnlyObservableCollection<SimulationPlaybackRateOptionViewModel> _playbackRateView;
    private readonly EventHandler<ReplayStateChangedEventArgs>? _stateChangedHandler;
    private readonly IDispatcher _dispatcher;

    private SimulationPlaybackRateOptionViewModel? _selectedPlaybackRateOption;
    private bool _disposed;
    private bool _isPlaying;
    private bool _isUpdatingFromController;
    private double _seekFraction;
    private TimeSpan _position;
    private TimeSpan _duration = DefaultDuration;
    private string _statusText = "Paused";
    private string _playPauseLabel = "Play";
    private string _positionDisplay = FormatPosition(TimeSpan.Zero, DefaultDuration);
    private double _selectedPlaybackRateMultiplier = 1.0;
    private string _selectedPlaybackRateLabel = FormatPlaybackRateLabel(1.0);
    private string _activeScenarioTitle = "Scenario: configuration defaults";
    private string _activeScenarioDescription = "Routes sourced from configuration.";
    private string _activeScenarioOptions = DescribeOptions(null);
    private IReadOnlyList<SimulationStreamRouteViewModel> _routes =
        new ReadOnlyCollection<SimulationStreamRouteViewModel>(Array.Empty<SimulationStreamRouteViewModel>());
    private CancellationTokenSource? _seekCancellationSource;
    private SimulationBarState _state = SimulationBarState.CreateDefault();

    /// <summary>
    /// Initializes a new instance of the <see cref="SimulationBarViewModel"/> class.
    /// </summary>
    /// <param name="configuration">The configuration that seeds the transport routes.</param>
    /// <param name="replayController">Optional replay controller used to manipulate playback.</param>
    /// <param name="logger">Optional logger used to capture transport control failures.</param>
    public SimulationBarViewModel(
        SimulationConfiguration? configuration,
        IReplayController? replayController = null,
        ILogger<SimulationBarViewModel>? logger = null,
        IDispatcher? dispatcher = null)
    {
        _configuration = configuration;
        _replayController = replayController;
        _logger = logger;
        _dispatcher = dispatcher ?? Dispatcher.UIThread;

        TogglePlaybackCommand = new DelegateCommand(_ => TogglePlayback());
        ToggleAutoResumeCommand = new DelegateCommand(_ => ToggleAutoResume());

        _playbackRateView = new ReadOnlyObservableCollection<SimulationPlaybackRateOptionViewModel>(_playbackRates);

        InitializePlaybackRates(configuration?.Options?.TimeScale ?? 1.0);
        NotifyPlaybackRateProperties();
        Routes = SimulationRouteViewModelBuilder.BuildRoutes(
            configuration,
            configuration?.Routes ?? Array.Empty<SimulationRouteConfiguration>());
        ActiveScenarioOptions = DescribeOptions(configuration?.Options);

        if (replayController is not null)
        {
            _stateChangedHandler = (_, args) => OnReplayStateChanged(args.State);
            replayController.StateChanged += _stateChangedHandler;
            OnReplayStateChanged(replayController.State);
        }
    }

    /// <summary>Raised when the playback command should toggle between play/pause.</summary>
    public ICommand TogglePlaybackCommand { get; }

    /// <summary>Raised when the auto-resume command should toggle its enabled state.</summary>
    public ICommand ToggleAutoResumeCommand { get; }

    /// <summary>Gets the status text describing the current playback state.</summary>
    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    /// <summary>Gets the label shown on the play/pause toggle button.</summary>
    public string PlayPauseLabel
    {
        get => _playPauseLabel;
        private set => SetProperty(ref _playPauseLabel, value);
    }

    /// <summary>Gets or sets the fractional playback position.</summary>
    public double SeekFraction
    {
        get => _seekFraction;
        set => UpdateSeekFraction(value, triggerSeek: true);
    }

    /// <summary>Gets the current playback position.</summary>
    public TimeSpan Position
    {
        get => _position;
        private set
        {
            if (SetProperty(ref _position, value))
            {
                PositionDisplay = FormatPosition(_position, Duration);
            }
        }
    }

    /// <summary>Gets the total replay duration.</summary>
    public TimeSpan Duration
    {
        get => _duration;
        private set
        {
            var validated = value > TimeSpan.Zero ? value : DefaultDuration;
            if (SetProperty(ref _duration, validated))
            {
                PositionDisplay = FormatPosition(Position, validated);
            }
        }
    }

    /// <summary>Gets the formatted display describing the playback position.</summary>
    public string PositionDisplay
    {
        get => _positionDisplay;
        private set => SetProperty(ref _positionDisplay, value);
    }

    /// <summary>Gets the available playback rates.</summary>
    public ReadOnlyObservableCollection<SimulationPlaybackRateOptionViewModel> PlaybackRates => _playbackRateView;

    /// <summary>Gets the currently selected playback rate option when available.</summary>
    public SimulationPlaybackRateOptionViewModel? SelectedPlaybackRateOption => _selectedPlaybackRateOption;

    /// <summary>
    /// Gets the currently selected playback rate multiplier. Values are clamped between
    /// <see cref="MinPlaybackRate"/> and <see cref="MaxPlaybackRate"/>.
    /// </summary>
    public double SelectedPlaybackRate
    {
        get => _selectedPlaybackRateMultiplier;
        private set => SetProperty(ref _selectedPlaybackRateMultiplier, value);
    }

    /// <summary>Gets a formatted label describing the selected playback rate.</summary>
    public string SelectedPlaybackRateLabel
    {
        get
        {
            var option = _selectedPlaybackRateOption;
            if (option is not null)
            {
                if (!string.IsNullOrWhiteSpace(option.Label))
                {
                    return option.Label;
                }

                // Fallback to formatted multiplier if no label
                return FormatPlaybackRateLabel(ClampPlaybackRate(option.Multiplier));
            }

            // No option selected — use current multiplier (clamped)
            return FormatPlaybackRateLabel(ClampPlaybackRate(_selectedPlaybackRateMultiplier));
        }
        private set => SetProperty(ref _selectedPlaybackRateLabel, value);
    }

    /// <summary>Gets the currently selected playback rate option.</summary>
    public SimulationPlaybackRateOptionViewModel? SelectedPlaybackRateOption
    {
        get => _selectedPlaybackRateOption;
        private set => SetProperty(ref _selectedPlaybackRateOption, value);
    }


    /// <summary>Gets or sets the friendly title describing the active scenario.</summary>
    public string ActiveScenarioTitle
    {
        get => _activeScenarioTitle;
        private set => SetProperty(ref _activeScenarioTitle, value);
    }

    /// <summary>Gets or sets the description surfaced for the active scenario.</summary>
    public string ActiveScenarioDescription
    {
        get => _activeScenarioDescription;
        private set => SetProperty(ref _activeScenarioDescription, value);
    }

    /// <summary>Gets or sets the option summary displayed for the active scenario.</summary>
    public string ActiveScenarioOptions
    {
        get => _activeScenarioOptions;
        private set => SetProperty(ref _activeScenarioOptions, value);
    }

    /// <summary>Gets the routed simulation streams.</summary>
    public IReadOnlyList<SimulationStreamRouteViewModel> Routes
    {
        get => _routes;
        private set => SetProperty(ref _routes, value);
    }

    /// <summary>Gets a value indicating whether auto resume is enabled for the active session.</summary>
    public bool IsAutoResumeEnabled => _state.ReplaySession?.AutoResumeEnabled ?? false;

    /// <summary>Updates the replay session tracked by the simulation bar.</summary>
    /// <param name="session">Session snapshot published by the replay service.</param>
    public void UpdateReplaySession(ReplaySessionState? session)
    {
        UpdateState(_state with { ReplaySession = session });
    }

    /// <summary>
    /// PUBLIC API (added): safely set the selected playback rate from the outside.
    /// Validates input and updates controller + UI.
    /// </summary>
    public void SetSelectedPlaybackRate(double rate)
    {
        if (double.IsNaN(rate) || double.IsInfinity(rate))
        {
            throw new ArgumentOutOfRangeException(nameof(rate));
        }

        var clamped = ClampPlaybackRate(rate);
        SelectPlaybackRate(clamped, updateController: true);
    }

    /// <summary>
    /// Applies a scenario to the bar, updating playback rate and routing metadata.
    /// </summary>
    /// <param name="scenario">Scenario definition selected by the operator.</param>
    public void ApplyScenario(SimulationScenarioConfiguration scenario)
    {
        ArgumentNullException.ThrowIfNull(scenario);

        Routes = SimulationRouteViewModelBuilder.BuildRoutes(_configuration, scenario.Routes);
        ActiveScenarioTitle = $"Scenario: {scenario.ScenarioId}";
        ActiveScenarioDescription = string.IsNullOrWhiteSpace(scenario.Description)
            ? "No description provided."
            : scenario.Description;
        ActiveScenarioOptions = DescribeOptions(scenario.Options ?? _configuration?.Options);

        var targetRate = scenario.Options?.TimeScale ?? _configuration?.Options?.TimeScale ?? 1.0;
        SelectPlaybackRate(targetRate, updateController: true);
        NotifyPlaybackRateProperties();
    }

    /// <summary>
    /// Applies the result of a legacy import, updating routes and playback rate.
    /// </summary>
    /// <param name="result">Legacy import output containing a scenario.</param>
    public void ApplyLegacyImport(LegacyGuidanceImportResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        ApplyScenario(result.Scenario);

        SortPlaybackRates();
        OnPropertyChanged(nameof(PlaybackRates));
    }

    /// <summary>
    /// Restores the routes and playback rate declared by the base configuration.
    /// </summary>
    public void ResetToConfigurationRoutes()
    {
        Routes = SimulationRouteViewModelBuilder.BuildRoutes(
            _configuration,
            _configuration?.Routes ?? Array.Empty<SimulationRouteConfiguration>());
        ActiveScenarioTitle = "Scenario: configuration defaults";
        ActiveScenarioDescription = "Routes sourced from configuration.";
        ActiveScenarioOptions = DescribeOptions(_configuration?.Options);

        var targetRate = _configuration?.Options?.TimeScale ?? 1.0;
        SelectPlaybackRate(targetRate, updateController: true);
        NotifyPlaybackRateProperties();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        _seekCancellationSource?.Cancel();
        _seekCancellationSource?.Dispose();
        _seekCancellationSource = null;

        if (_replayController is not null && _stateChangedHandler is not null)
        {
            _replayController.StateChanged -= _stateChangedHandler;
        }
    }

    private void InitializePlaybackRates(double initialRate)
    {
        ExecuteOnDispatcher(() =>
        {
            _playbackRates.Clear();
            _playbackRates.Add(new SimulationPlaybackRateOptionViewModel(0.5, OnPlaybackRateOptionSelected));
            _playbackRates.Add(new SimulationPlaybackRateOptionViewModel(1.0, OnPlaybackRateOptionSelected));
            _playbackRates.Add(new SimulationPlaybackRateOptionViewModel(2.0, OnPlaybackRateOptionSelected));
            SortPlaybackRates();
        });

        SelectPlaybackRate(initialRate, updateController: false);
    }

    private void TogglePlayback()
    {
        if (_disposed)
        {
            return;
        }

        if (_replayController is null)
        {
            SetIsPlaying(!_isPlaying);
            return;
        }

        if (_isPlaying)
        {
            FireAndForget(() => _replayController.PauseAsync(), "Failed to pause playback.");
        }
        else
        {
            FireAndForget(() => _replayController.PlayAsync(), "Failed to start playback.");
        }
    }

    private void ToggleAutoResume()
    {
        if (_disposed || _state.ReplaySession is null)
        {
            return;
        }

        var current = _state.ReplaySession.Value;
        var updated = current with { AutoResumeEnabled = !current.AutoResumeEnabled };
        UpdateState(_state with { ReplaySession = updated });
    }

private void UpdateState(SimulationBarState newState)
{
    if (_state.Equals(newState))
    {
        return;
    }

    var previousSession = _state.ReplaySession;
    var previousAutoResumeEnabled = previousSession?.AutoResumeEnabled ?? false;
    var previousHasSession = previousSession.HasValue;

    _state = newState;

    var currentSession = _state.ReplaySession;
    var currentAutoResumeEnabled = currentSession?.AutoResumeEnabled ?? false;
    var currentHasSession = currentSession.HasValue;

    if (previousHasSession != currentHasSession || previousAutoResumeEnabled != currentAutoResumeEnabled)
    {
        OnPropertyChanged(nameof(IsAutoResumeEnabled));
    }
}

private void OnPlaybackRateOptionSelected(SimulationPlaybackRateOptionViewModel option)
{
    if (option is null)
    {
        return;
    }

    SelectPlaybackRate(option.Rate, updateController: true);
}

private void SelectPlaybackRate(double rate, bool updateController)
{
    var effectiveRate = ClampPlaybackRate(rate);
    var option = EnsurePlaybackRateOption(effectiveRate);

    // Toggle selection using the unified helper (updates selection state & notifies bindings)
    UpdateSelectedPlaybackRateOption(option);

    // Keep scalar + label properties in sync (handle null labels defensively)
    SelectedPlaybackRate = effectiveRate;
    SelectedPlaybackRateLabel = option?.Label ?? $"{effectiveRate:0.##}x";

    if (updateController && !_isUpdatingFromController && _replayController is not null)
    {
        FireAndForget(
            () => _replayController.SetPlaybackRateAsync(effectiveRate),
            "Failed to set playback rate.");
    }
}

private static double ClampPlaybackRate(double rate)
{
    if (double.IsNaN(rate) || double.IsInfinity(rate))
    {
        return 1.0;
    }

    return Math.Clamp(rate, MinPlaybackRate, MaxPlaybackRate);
}

private SimulationPlaybackRateOptionViewModel EnsurePlaybackRateOption(double rate)
{
    var normalized = rate <= 0 ? 1.0 : rate;

    var existing = _playbackRates.FirstOrDefault(
        o => Math.Abs(o.Rate - normalized) < PlaybackRateComparisonTolerance);

    if (existing is not null)
    {
        return existing;
    }

    var option = new SimulationPlaybackRateOptionViewModel(normalized, OnPlaybackRateOptionSelected);
    _playbackRates.Add(option);
    SortPlaybackRates();
    OnPropertyChanged(nameof(PlaybackRates));
    return option;
}

private void SortPlaybackRates()
{
    if (_playbackRates.Count < 2)
    {
        return;
    }

    var ordered = _playbackRates.OrderBy(o => o.Rate).ToList();
    for (var targetIndex = 0; targetIndex < ordered.Count; targetIndex++)
    {
        var item = ordered[targetIndex];
        var currentIndex = _playbackRates.IndexOf(item);
        if (currentIndex != targetIndex)
        {
            _playbackRates.Move(currentIndex, targetIndex);
        }
    }
}

// Call this when a playback rate option is chosen (e.g., from the UI).
    private void UpdateSelectedPlaybackRateOption(SimulationPlaybackRateOptionViewModel option)
    {
        if (option is null)
        {
            return;
        }

        if (ReferenceEquals(_selectedPlaybackRateOption, option))
        {
            // Ensure visual state is correct without re-firing callbacks.
            option.SetSelected(true, suppressCallback: true);
            // Still notify in case dependent bindings read through properties.
            NotifyPlaybackRateProperties();
            return;
        }

        _selectedPlaybackRateOption?.SetSelected(false, suppressCallback: true);
        option.SetSelected(true, suppressCallback: true);
        SelectedPlaybackRateOption = option;

        NotifyPlaybackRateProperties();
    }

    private void NotifyPlaybackRateProperties()
    {
        RaisePropertyChanged(nameof(SelectedPlaybackRate));
        RaisePropertyChanged(nameof(SelectedPlaybackRateLabel));
        RaisePropertyChanged(nameof(SelectedPlaybackRateOption));
    }


    private void UpdateSeekFraction(double value, bool triggerSeek)
    {
        var clamped = double.IsNaN(value) ? 0 : Math.Clamp(value, 0, 1);

        if (!SetProperty(ref _seekFraction, clamped, nameof(SeekFraction)))
        {
            return;
        }

        if (_isUpdatingFromController)
        {
            return;
        }

        var newPosition = TimeSpan.FromTicks((long)(Duration.Ticks * clamped));
        Position = newPosition;

        if (triggerSeek)
        {
            ScheduleSeek(newPosition);
        }
    }

    private void UpdateSeekFraction(TimeSpan position, bool triggerSeek)
    {
        if (Duration <= TimeSpan.Zero)
        {
            UpdateSeekFraction(0, triggerSeek);
            return;
        }

        var fraction = Math.Clamp(position.TotalSeconds / Duration.TotalSeconds, 0, 1);
        UpdateSeekFraction(fraction, triggerSeek);
    }

    private void OnReplayStateChanged(ReplayState state)
    {
        ExecuteOnDispatcher(() =>
        {
            _isUpdatingFromController = true;
            try
            {
                SetIsPlaying(state.IsPlaying);
                Duration = state.Duration;
                Position = state.Position;

                UpdateSeekFraction(state.Position, triggerSeek: false);

                // Normalize and clamp rate (0 or negative -> 1.0), then ensure option exists.
                var normalizedRate = ClampPlaybackRate(state.PlaybackRate <= 0 ? 1.0 : state.PlaybackRate);
                SelectedPlaybackRate = normalizedRate;

                var option = EnsurePlaybackRateOption(normalizedRate);
                UpdateSelectedPlaybackRateOption(option);

                // Keep label consistent with option (fallback to formatted multiplier).
                SelectedPlaybackRateLabel = option?.Label ?? FormatPlaybackRateLabel(normalizedRate);
            }
            finally
            {
                _isUpdatingFromController = false;
            }
        });
    }


    private void SetIsPlaying(bool isPlaying)
    {
        if (_isPlaying == isPlaying)
        {
            return;
        }

        _isPlaying = isPlaying;
        StatusText = isPlaying ? "Playing" : "Paused";
        PlayPauseLabel = isPlaying ? "Pause" : "Play";
    }

    private void FireAndForget(Func<ValueTask> operation, string failureMessage)
    {
        _ = Task.Run(
            async () =>
            {
                try
                {
                    await operation().ConfigureAwait(false);
                }
                catch (OperationCanceledException ex)
                {
                    _logger?.LogInformation(ex, failureMessage);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, failureMessage);
                }
            });
    }

    private void ScheduleSeek(TimeSpan position)
    {
        if (_disposed)
        {
            return;
        }

        var controller = _replayController;
        if (controller is null)
        {
            return;
        }

        var previous = _seekCancellationSource;
        previous?.Cancel();

        var current = new CancellationTokenSource();
        _seekCancellationSource = current;

        var token = current.Token;
        FireAndForget(
            () => DebouncedSeekAsync(controller, position, current, token),
            "Failed to seek to requested position.");
    }

    private async ValueTask DebouncedSeekAsync(
        IReplayController controller,
        TimeSpan position,
        CancellationTokenSource source,
        CancellationToken token)
    {
        try
        {
            await Task.Delay(SeekDebounceDelay, token).ConfigureAwait(false);

            if (_disposed || token.IsCancellationRequested)
            {
                return;
            }

            await controller.SeekAsync(position, token).ConfigureAwait(false);
        }
        finally
        {
            if (ReferenceEquals(_seekCancellationSource, source))
            {
                _seekCancellationSource = null;
            }

            source.Dispose();
        }
    }

    private void ExecuteOnDispatcher(Action action)
    {
        if (action is null)
        {
            return;
        }

        if (_dispatcher.CheckAccess())
        {
            action();
        }
        else
        {
            _dispatcher.Post(action);
        }
    }

    private static string FormatPosition(TimeSpan position, TimeSpan duration)
    {
        return $"{FormatTime(position)} / {FormatTime(duration)}";
    }

    private static string FormatTime(TimeSpan value)
    {
        return value.ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture);
    }

    private static string FormatPlaybackRateLabel(double rate)
    {
        return string.Format(CultureInfo.CurrentUICulture, "{0:0.##}×", rate);
    }

    private static string DescribeOptions(SimulationOptionsConfiguration? options)
    {
        if (options is null)
        {
            return "Options: (none)";
        }

        var parts = new List<string>();

        if (options.Seed.HasValue)
        {
            parts.Add(FormattableString.Invariant($"seed={options.Seed.Value}"));
        }

        if (options.TimeScale.HasValue)
        {
            parts.Add(FormattableString.Invariant($"timeScale={options.TimeScale.Value:0.###}"));
        }

        return parts.Count == 0
            ? "Options: (none)"
            : "Options: " + string.Join(", ", parts);
    }

    /// <summary>Snapshot of simulation bar state tracked for replay-aware interactions.</summary>
    /// <param name="ReplaySession">Replay session currently surfaced in the UI.</param>
    private readonly record struct SimulationBarState(ReplaySessionState? ReplaySession)
    {
        public static SimulationBarState CreateDefault() => new(null);
    }

    /// <summary>Represents replay session state surfaced by the simulation bar.</summary>
    /// <param name="AutoResumeEnabled">Indicates whether auto resume is currently enabled.</param>
    public readonly record struct ReplaySessionState(bool AutoResumeEnabled);
}
