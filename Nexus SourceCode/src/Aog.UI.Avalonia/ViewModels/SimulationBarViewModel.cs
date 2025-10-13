using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Aog.Core.Simulation.Configuration;

namespace Aog.UI.Avalonia.ViewModels;

/// <summary>
/// Presentation model for the simulation control bar rendered in the shell window.
/// </summary>
public sealed class SimulationBarViewModel : ObservableObject
{
    private readonly ReadOnlyCollection<SimulationPlaybackRateOptionViewModel> _playbackRates;
    private readonly ObservableCollection<SimulationStreamRouteViewModel> _routes;
    private readonly SimulationConfiguration? _configuration;
    private readonly TimeSpan _duration;
    private readonly DelegateCommand _togglePlaybackCommand;
    private bool _isPlaying;
    private double _selectedPlaybackRate;
    private TimeSpan _position;
    private double _seekFraction;
    private bool _suppressSeekSync;
    private string _activeScenarioTitle = "Scenario: configuration defaults";
    private string _activeScenarioDescription = "Using routes from the loaded configuration.";
    private string _activeScenarioOptions = "—";

    public SimulationBarViewModel(SimulationConfiguration? configuration)
    {
        _configuration = configuration;
        _togglePlaybackCommand = new DelegateCommand(_ => TogglePlayback());
        _duration = TimeSpan.FromMinutes(5);
        _selectedPlaybackRate = 1.0;

        _playbackRates = BuildPlaybackRateOptions();
        var initialRoutes = configuration?.Routes ?? Array.Empty<SimulationRouteConfiguration>();
        _routes = new ObservableCollection<SimulationStreamRouteViewModel>(
            SimulationRouteViewModelBuilder.BuildRoutes(configuration, initialRoutes));
    }

    /// <summary>
    /// Gets the command that toggles between play and pause states.
    /// </summary>
    public DelegateCommand TogglePlaybackCommand => _togglePlaybackCommand;

    /// <summary>
    /// Gets the formatted label for the play/pause button.
    /// </summary>
    public string PlayPauseLabel => _isPlaying ? "Pause" : "Play";

    /// <summary>
    /// Gets a short text summary of the current playback state.
    /// </summary>
    public string StatusText => _isPlaying ? "Playing" : "Paused";

    /// <summary>
    /// Gets the available playback rate options.
    /// </summary>
    public IReadOnlyList<SimulationPlaybackRateOptionViewModel> PlaybackRates => _playbackRates;

    /// <summary>
    /// Gets the active playback rate.
    /// </summary>
    public double SelectedPlaybackRate
    {
        get => _selectedPlaybackRate;
        private set
        {
            if (!SetProperty(ref _selectedPlaybackRate, value))
            {
                return;
            }

            OnPropertyChanged(nameof(SelectedPlaybackRateLabel));
        }
    }

    /// <summary>
    /// Gets the formatted label describing the selected playback rate.
    /// </summary>
    public string SelectedPlaybackRateLabel => $"{SelectedPlaybackRate:0.##}×";

    /// <summary>
    /// Gets the total duration represented on the scrubber.
    /// </summary>
    public TimeSpan Duration => _duration;

    /// <summary>
    /// Gets the formatted duration label.
    /// </summary>
    public string DurationDisplay => FormatTimestamp(Duration);

    /// <summary>
    /// Gets the current playback position.
    /// </summary>
    public TimeSpan Position
    {
        get => _position;
        private set
        {
            var clamped = Clamp(value, TimeSpan.Zero, Duration);
            if (!SetProperty(ref _position, clamped))
            {
                return;
            }

            OnPropertyChanged(nameof(PositionDisplay));

            var fraction = Duration.TotalSeconds <= 0
                ? 0
                : clamped.TotalSeconds / Duration.TotalSeconds;

            if (Math.Abs(fraction - _seekFraction) > 0.0001)
            {
                _suppressSeekSync = true;
                _seekFraction = fraction;
                OnPropertyChanged(nameof(SeekFraction));
                _suppressSeekSync = false;
            }
        }
    }

    /// <summary>
    /// Gets a formatted label for the current playback position.
    /// </summary>
    public string PositionDisplay => FormatTimestamp(Position);

    /// <summary>
    /// Gets or sets the scrubber position as a normalized value between 0 and 1.
    /// </summary>
    public double SeekFraction
    {
        get => _seekFraction;
        set
        {
            var clamped = Math.Clamp(value, 0, 1);
            if (!SetProperty(ref _seekFraction, clamped) || _suppressSeekSync)
            {
                return;
            }

            var seconds = Duration.TotalSeconds * clamped;
            Position = TimeSpan.FromSeconds(seconds);
        }
    }

    /// <summary>
    /// Gets the collection of routed simulation streams.
    /// </summary>
    public IReadOnlyList<SimulationStreamRouteViewModel> Routes => _routes;

    /// <summary>
    /// Gets a short title describing the active scenario selection.
    /// </summary>
    public string ActiveScenarioTitle
    {
        get => _activeScenarioTitle;
        private set => SetProperty(ref _activeScenarioTitle, value);
    }

    /// <summary>
    /// Gets the description of the active scenario selection.
    /// </summary>
    public string ActiveScenarioDescription
    {
        get => _activeScenarioDescription;
        private set => SetProperty(ref _activeScenarioDescription, value);
    }

    /// <summary>
    /// Gets a formatted summary of the options applied by the active scenario.
    /// </summary>
    public string ActiveScenarioOptions
    {
        get => _activeScenarioOptions;
        private set => SetProperty(ref _activeScenarioOptions, value);
    }

    /// <summary>
    /// Applies the provided scenario, updating the routed streams and descriptive metadata.
    /// </summary>
    /// <param name="scenario">Scenario definition to activate.</param>
    public void ApplyScenario(SimulationScenarioConfiguration scenario)
    {
        ArgumentNullException.ThrowIfNull(scenario);

        UpdateRoutes(scenario.Routes);
        ActiveScenarioTitle = $"Scenario: {scenario.ScenarioId}";
        ActiveScenarioDescription = string.IsNullOrWhiteSpace(scenario.Description)
            ? "No description provided."
            : scenario.Description!;
        ActiveScenarioOptions = FormatScenarioOptions(scenario.Options);
    }

    /// <summary>
    /// Reverts the routed streams to the configuration defaults.
    /// </summary>
    public void ResetToConfigurationRoutes()
    {
        var routes = _configuration?.Routes ?? Array.Empty<SimulationRouteConfiguration>();
        UpdateRoutes(routes);
        ActiveScenarioTitle = "Scenario: configuration defaults";
        ActiveScenarioDescription = "Using routes from the loaded configuration.";
        ActiveScenarioOptions = "—";
    }

    private void TogglePlayback()
    {
        _isPlaying = !_isPlaying;
        OnPropertyChanged(nameof(StatusText));
        OnPropertyChanged(nameof(PlayPauseLabel));
    }

    private ReadOnlyCollection<SimulationPlaybackRateOptionViewModel> BuildPlaybackRateOptions()
    {
        var options = new List<SimulationPlaybackRateOptionViewModel>();
        void SelectOption(SimulationPlaybackRateOptionViewModel option)
        {
            SelectedPlaybackRate = option.Rate;

            foreach (var candidate in options)
            {
                candidate.SetSelected(candidate == option, suppressCallback: true);
            }
        }

        foreach (var rate in new[] { 0.5, 1.0, 2.0 })
        {
            options.Add(new SimulationPlaybackRateOptionViewModel(rate, SelectOption));
        }

        // Ensure the default rate is selected when the view-model is constructed.
        SelectOption(options.First(o => Math.Abs(o.Rate - 1.0) < double.Epsilon));

        return new ReadOnlyCollection<SimulationPlaybackRateOptionViewModel>(options);
    }

    private void UpdateRoutes(IEnumerable<SimulationRouteConfiguration> routes)
    {
        _routes.Clear();

        if (routes is null)
        {
            return;
        }

        var built = SimulationRouteViewModelBuilder.BuildRoutes(_configuration, routes);
        foreach (var route in built)
        {
            _routes.Add(route);
        }
    }

    private static TimeSpan Clamp(TimeSpan value, TimeSpan minimum, TimeSpan maximum)
    {
        if (value < minimum)
        {
            return minimum;
        }

        if (value > maximum)
        {
            return maximum;
        }

        return value;
    }

    private static string FormatTimestamp(TimeSpan value)
    {
        if (value.TotalHours >= 1)
        {
            return $"{(int)value.TotalHours:00}:{value.Minutes:00}:{value.Seconds:00}";
        }

        return $"{value.Minutes:00}:{value.Seconds:00}";
    }

    private static string FormatScenarioOptions(SimulationOptionsConfiguration? options)
    {
        if (options is null)
        {
            return "—";
        }

        var parts = new List<string>();
        if (options.Seed.HasValue)
        {
            parts.Add($"seed={options.Seed.Value}");
        }

        if (options.TimeScale.HasValue)
        {
            parts.Add($"timeScale={options.TimeScale.Value:0.###}");
        }

        return parts.Count == 0 ? "—" : string.Join(", ", parts);
    }
}
