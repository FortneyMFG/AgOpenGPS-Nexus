using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Aog.Core.Legacy;
using Aog.Core.Replay;
using Aog.Core.Simulation.Configuration;
using Microsoft.Extensions.Logging;

namespace Aog.UI.Avalonia.ViewModels;

/// <summary>
/// View-model powering the simulation transport bar rendered in the shell.
/// </summary>
public sealed class SimulationBarViewModel : ObservableObject, IDisposable
{
    private const double PlaybackRateComparisonTolerance = 1e-6;
    private static readonly TimeSpan DefaultDuration = TimeSpan.FromMinutes(5);

    private readonly SimulationConfiguration? _configuration;
    private readonly IReplayController? _replayController;
    private readonly ILogger<SimulationBarViewModel>? _logger;
    private readonly List<SimulationPlaybackRateOptionViewModel> _playbackRates = new();
    private readonly EventHandler<ReplayStateChangedEventArgs>? _stateChangedHandler;

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
    private double _selectedPlaybackRate = 1.0;
    private string _selectedPlaybackRateLabel = FormatPlaybackRateLabel(1.0);
    private string _activeScenarioTitle = "Scenario: configuration defaults";
    private string _activeScenarioDescription = "Routes sourced from configuration.";
    private string _activeScenarioOptions = DescribeOptions(null);
    private IReadOnlyList<SimulationStreamRouteViewModel> _routes =
        new ReadOnlyCollection<SimulationStreamRouteViewModel>(Array.Empty<SimulationStreamRouteViewModel>());

    /// <summary>
    /// Initializes a new instance of the <see cref="SimulationBarViewModel"/> class.
    /// </summary>
    /// <param name="configuration">The configuration that seeds the transport routes.</param>
    /// <param name="replayController">Optional replay controller used to manipulate playback.</param>
    /// <param name="logger">Optional logger used to capture transport control failures.</param>
    public SimulationBarViewModel(
        SimulationConfiguration? configuration,
        IReplayController? replayController = null,
        ILogger<SimulationBarViewModel>? logger = null)
    {
        _configuration = configuration;
        _replayController = replayController;
        _logger = logger;

        TogglePlaybackCommand = new DelegateCommand(_ => TogglePlayback());

        InitializePlaybackRates(configuration?.Options?.TimeScale ?? 1.0);
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
    public IReadOnlyList<SimulationPlaybackRateOptionViewModel> PlaybackRates =>
        new ReadOnlyCollection<SimulationPlaybackRateOptionViewModel>(_playbackRates);

    /// <summary>Gets the currently selected playback rate multiplier.</summary>
    public double SelectedPlaybackRate
    {
        get => _selectedPlaybackRate;
        private set
        {
            if (SetProperty(ref _selectedPlaybackRate, value))
            {
                SelectedPlaybackRateLabel = FormatPlaybackRateLabel(value);
            }
            else
            {
                SelectedPlaybackRateLabel = FormatPlaybackRateLabel(value);
            }
        }
    }

    /// <summary>Gets a formatted label describing the selected playback rate.</summary>
    public string SelectedPlaybackRateLabel
    {
        get => _selectedPlaybackRateLabel;
        private set => SetProperty(ref _selectedPlaybackRateLabel, value);
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

    /// <summary>
    /// PUBLIC API (added): safely set the selected playback rate from the outside.
    /// Validates input and updates controller + UI.
    /// </summary>
    public void SetSelectedPlaybackRate(double rate)
    {
        if (double.IsNaN(rate) || double.IsInfinity(rate) || rate <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(rate));
        }

        SelectPlaybackRate(rate, updateController: true);
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
    }

    /// <summary>
    /// Applies the result of a legacy import, updating routes and playback rate.
    /// </summary>
    /// <param name="result">Legacy import output containing a scenario.</param>
    public void ApplyLegacyImport(LegacyGuidanceImportResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        ApplyScenario(result.Scenario);
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
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (_replayController is not null && _stateChangedHandler is not null)
        {
            _replayController.StateChanged -= _stateChangedHandler;
        }
    }

    private void InitializePlaybackRates(double initialRate)
    {
        _playbackRates.Add(new SimulationPlaybackRateOptionViewModel(0.5, OnPlaybackRateOptionSelected));
        _playbackRates.Add(new SimulationPlaybackRateOptionViewModel(1.0, OnPlaybackRateOptionSelected));
        _playbackRates.Add(new SimulationPlaybackRateOptionViewModel(2.0, OnPlaybackRateOptionSelected));
        SortPlaybackRates();

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
        if (rate <= 0)
        {
            rate = 1.0;
        }

        var option = EnsurePlaybackRateOption(rate);
        UpdateSelectedPlaybackRateOption(option);

        SelectedPlaybackRate = rate;

        if (updateController && !_isUpdatingFromController && _replayController is not null)
        {
            FireAndForget(() => _replayController.SetPlaybackRateAsync(rate), "Failed to set playback rate.");
        }
    }

    private SimulationPlaybackRateOptionViewModel EnsurePlaybackRateOption(double rate)
    {
        var existing = _playbackRates.FirstOrDefault(
            option => Math.Abs(option.Rate - rate) < PlaybackRateComparisonTolerance);

        if (existing is not null)
        {
            return existing;
        }

        var option = new SimulationPlaybackRateOptionViewModel(rate, OnPlaybackRateOptionSelected);
        _playbackRates.Add(option);
        SortPlaybackRates();
        OnPropertyChanged(nameof(PlaybackRates));
        return option;
    }

    private void SortPlaybackRates()
    {
        _playbackRates.Sort((left, right) => left.Rate.CompareTo(right.Rate));
    }

    private void UpdateSelectedPlaybackRateOption(SimulationPlaybackRateOptionViewModel option)
    {
        if (_selectedPlaybackRateOption == option)
        {
            option.SetSelected(true, suppressCallback: true);
            return;
        }

        _selectedPlaybackRateOption?.SetSelected(false, suppressCallback: true);
        option.SetSelected(true, suppressCallback: true);
        _selectedPlaybackRateOption = option;
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

        if (triggerSeek && _replayController is not null)
        {
            FireAndForget(() => _replayController.SeekAsync(newPosition), "Failed to seek to requested position.");
        }
    }

    private void OnReplayStateChanged(ReplayState state)
    {
        _isUpdatingFromController = true;
        try
        {
            SetIsPlaying(state.IsPlaying);
            Duration = state.Duration;
            Position = state.Position;

            var fraction = Duration > TimeSpan.Zero
                ? Math.Clamp(state.Position.TotalSeconds / Duration.TotalSeconds, 0, 1)
                : 0;

            SetProperty(ref _seekFraction, fraction, nameof(SeekFraction));
            SelectedPlaybackRate = state.PlaybackRate;

            var option = EnsurePlaybackRateOption(state.PlaybackRate);
            UpdateSelectedPlaybackRateOption(option);
        }
        finally
        {
            _isUpdatingFromController = false;
        }
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
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, failureMessage);
                }
            });
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
        return FormattableString.Invariant($"{rate:0.##}×");
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
}
