using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Aog.Core.Replay;
using Aog.Core.Simulation.Configuration;

namespace Aog.UI.Avalonia.ViewModels;

/// <summary>
/// Presentation model for the simulation control bar rendered in the shell window.
/// </summary>
public sealed class SimulationBarViewModel : ObservableObject
{
    private static readonly string[] DefaultModeOptions = new[] { "simulation", "hardware", "replay" };

    private readonly ReadOnlyCollection<SimulationPlaybackRateOptionViewModel> _playbackRates;
    private readonly ReadOnlyCollection<SimulationStreamRouteViewModel> _routes;
    private readonly TimeSpan _defaultDuration = TimeSpan.FromMinutes(5);
    private readonly DelegateCommand _togglePlaybackCommand;
    private readonly IReplayController? _replayController;
    private ReplayState _state;
    private double _selectedPlaybackRate;
    private TimeSpan _position;
    private double _seekFraction;
    private bool _suppressSeekSync;

    public SimulationBarViewModel(SimulationConfiguration? configuration, IReplayController? replayController = null)
    {
        _togglePlaybackCommand = new DelegateCommand(_ => TogglePlayback());
        _state = new ReplayState(isPlaying: false, position: TimeSpan.Zero, _defaultDuration, playbackRate: 1.0);
        _selectedPlaybackRate = _state.PlaybackRate;

        _playbackRates = BuildPlaybackRateOptions();
        _routes = BuildRoutes(configuration);
        SyncPlaybackRateSelection(_selectedPlaybackRate);

        _replayController = replayController;
        if (_replayController is not null)
        {
            var controllerState = NormalizeState(_replayController.State);
            _state = controllerState;
            SyncPlaybackRateSelection(controllerState.PlaybackRate);
            Position = controllerState.Position;
            _replayController.StateChanged += OnReplayStateChanged;
        }
    }

    /// <summary>
    /// Gets the command that toggles between play and pause states.
    /// </summary>
    public DelegateCommand TogglePlaybackCommand => _togglePlaybackCommand;

    /// <summary>
    /// Gets the formatted label for the play/pause button.
    /// </summary>
    public string PlayPauseLabel => _state.IsPlaying ? "Pause" : "Play";

    /// <summary>
    /// Gets a short text summary of the current playback state.
    /// </summary>
    public string StatusText => _state.IsPlaying ? "Playing" : "Paused";

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
    public TimeSpan Duration => _state.Duration > TimeSpan.Zero ? _state.Duration : _defaultDuration;

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

            if (_replayController is null)
            {
                _state = _state with { Position = clamped };
            }

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
            var position = TimeSpan.FromSeconds(seconds);
            Position = position;

            if (_replayController is not null)
            {
                _ = _replayController.SeekAsync(position);
            }
            else
            {
                _state = _state with { Position = position };
            }
        }
    }

    /// <summary>
    /// Gets the collection of routed simulation streams.
    /// </summary>
    public IReadOnlyList<SimulationStreamRouteViewModel> Routes => _routes;

    private void TogglePlayback()
    {
        if (_replayController is not null)
        {
            if (_state.IsPlaying)
            {
                _ = _replayController.PauseAsync();
            }
            else
            {
                _ = _replayController.PlayAsync();
            }

            return;
        }

        _state = _state with { IsPlaying = !_state.IsPlaying };
        OnPropertyChanged(nameof(StatusText));
        OnPropertyChanged(nameof(PlayPauseLabel));
    }

    private ReadOnlyCollection<SimulationPlaybackRateOptionViewModel> BuildPlaybackRateOptions()
    {
        var options = new List<SimulationPlaybackRateOptionViewModel>();
        void SelectOption(SimulationPlaybackRateOptionViewModel option)
        {
            foreach (var candidate in options)
            {
                candidate.SetSelected(candidate == option, suppressCallback: true);
            }

            OnPlaybackRateSelected(option.Rate);
        }

        foreach (var rate in new[] { 0.5, 1.0, 2.0 })
        {
            options.Add(new SimulationPlaybackRateOptionViewModel(rate, SelectOption));
        }

        return new ReadOnlyCollection<SimulationPlaybackRateOptionViewModel>(options);
    }

    private ReadOnlyCollection<SimulationStreamRouteViewModel> BuildRoutes(SimulationConfiguration? configuration)
    {
        if (configuration is null)
        {
            return new ReadOnlyCollection<SimulationStreamRouteViewModel>(Array.Empty<SimulationStreamRouteViewModel>());
        }

        var providersByStream = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var provider in configuration.Providers)
        {
            foreach (var output in provider.Outputs)
            {
                if (!providersByStream.TryGetValue(output, out var list))
                {
                    list = new List<string>();
                    providersByStream[output] = list;
                }

                if (!list.Any(providerId => providerId.Equals(provider.ProviderId, StringComparison.OrdinalIgnoreCase)))
                {
                    list.Add(provider.ProviderId);
                }
            }
        }

        var modeOptions = new HashSet<string>(DefaultModeOptions, StringComparer.OrdinalIgnoreCase);
        foreach (var route in configuration.Routes)
        {
            modeOptions.Add(route.Mode);
        }

        var sortedModes = modeOptions
            .OrderBy(mode => mode, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var routes = configuration.Routes
            .Select(route =>
            {
                if (!providersByStream.TryGetValue(route.Stream, out var sources) || sources.Count == 0)
                {
                    sources = new List<string> { route.Source };
                }

                return new SimulationStreamRouteViewModel(
                    route.Stream,
                    route.Source,
                    route.Mode,
                    sources,
                    sortedModes);
            })
            .OrderBy(route => route.Stream, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new ReadOnlyCollection<SimulationStreamRouteViewModel>(routes);
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

    private void SyncPlaybackRateSelection(double rate)
    {
        foreach (var option in _playbackRates)
        {
            option.SetSelected(Math.Abs(option.Rate - rate) < 1e-6, suppressCallback: true);
        }

        SelectedPlaybackRate = rate;
    }

    private void OnPlaybackRateSelected(double rate)
    {
        SyncPlaybackRateSelection(rate);

        if (_replayController is not null)
        {
            _ = _replayController.SetPlaybackRateAsync(rate);
        }
        else
        {
            _state = _state with { PlaybackRate = rate };
        }
    }

    private void OnReplayStateChanged(object? sender, ReplayStateChangedEventArgs e)
    {
        var next = NormalizeState(e.State);
        var playbackChanged = _state.IsPlaying != next.IsPlaying;
        _state = next;

        if (playbackChanged)
        {
            OnPropertyChanged(nameof(StatusText));
            OnPropertyChanged(nameof(PlayPauseLabel));
        }

        SyncPlaybackRateSelection(_state.PlaybackRate);
        Position = _state.Position;
        OnPropertyChanged(nameof(Duration));
        OnPropertyChanged(nameof(DurationDisplay));
    }

    private ReplayState NormalizeState(ReplayState state)
    {
        if (state.Duration <= TimeSpan.Zero)
        {
            state = state with { Duration = _defaultDuration };
        }

        return state;
    }

    private static string FormatTimestamp(TimeSpan value)
    {
        if (value.TotalHours >= 1)
        {
            return $"{(int)value.TotalHours:00}:{value.Minutes:00}:{value.Seconds:00}";
        }

        return $"{value.Minutes:00}:{value.Seconds:00}";
    }
}
