using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Aog.UI.Avalonia.ViewModels.Support;

namespace Aog.UI.Avalonia.ViewModels;

/// <summary>
/// Provides the state and interaction logic for the simulation control bar.
/// </summary>
/// <remarks>
/// The bootstrap implementation exposes a deterministic set of playback rates, a
/// configurable seek position, and sample routing information so the shell can be
/// exercised end-to-end before the real simulation clock is available.
/// </remarks>
public class SimBarViewModel : INotifyPropertyChanged
{
    private bool _isPlaying = true;
    private TimeSpan _currentPosition = TimeSpan.Zero;
    private TimeSpan _duration = TimeSpan.FromMinutes(15);
    private PlaybackRateOptionViewModel _selectedPlaybackRate;

    /// <summary>
    /// Initializes a new instance of the <see cref="SimBarViewModel"/> class
    /// with sample data suitable for design-time tooling.
    /// </summary>
    public SimBarViewModel()
    {
        TogglePlayPauseCommand = new DelegateCommand(_ => TogglePlayPause());

        PlaybackRates = new ObservableCollection<PlaybackRateOptionViewModel>(
            new[]
            {
                new PlaybackRateOptionViewModel(this, 0.5, "0.5×"),
                new PlaybackRateOptionViewModel(this, 1.0, "1×"),
                new PlaybackRateOptionViewModel(this, 2.0, "2×"),
            });

        _selectedPlaybackRate = PlaybackRates[1];
        _selectedPlaybackRate.SetIsSelected(true);

        StreamRoutes = new ObservableCollection<SimulationStreamRouteViewModel>(
            new[]
            {
                new SimulationStreamRouteViewModel(
                    "Vehicle pose",
                    new[] { "Scenario simulator", "Replay file", "External AGiO" },
                    "Scenario simulator"),
                new SimulationStreamRouteViewModel(
                    "GNSS corrections",
                    new[] { "Scenario simulator", "Replay file", "External AGiO" },
                    "Scenario simulator"),
                new SimulationStreamRouteViewModel(
                    "Implement telemetry",
                    new[] { "Scenario simulator", "Replay file", "External AGiO" },
                    "Scenario simulator"),
            });
    }

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Gets a value indicating whether playback is currently running.
    /// </summary>
    public bool IsPlaying
    {
        get => _isPlaying;
        private set
        {
            if (_isPlaying == value)
            {
                return;
            }

            _isPlaying = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(PlayPauseLabel));
        }
    }

    /// <summary>
    /// Gets a user-friendly label for the play/pause button.
    /// </summary>
    public string PlayPauseLabel => IsPlaying ? "Pause" : "Play";

    /// <summary>
    /// Gets or sets the current playback position expressed in seconds.
    /// </summary>
    public double CurrentPositionSeconds
    {
        get => _currentPosition.TotalSeconds;
        set
        {
            var clamped = Clamp(value, 0, DurationSeconds);
            if (Math.Abs(clamped - _currentPosition.TotalSeconds) < 0.0001)
            {
                return;
            }

            _currentPosition = TimeSpan.FromSeconds(clamped);
            OnPropertyChanged();
            OnPropertyChanged(nameof(TimelineSummary));
        }
    }

    /// <summary>
    /// Gets the total duration of the scenario expressed in seconds.
    /// </summary>
    public double DurationSeconds
    {
        get => _duration.TotalSeconds;
        private set
        {
            var duration = TimeSpan.FromSeconds(Math.Max(0, value));
            if (_duration == duration)
            {
                return;
            }

            _duration = duration;
            OnPropertyChanged();
            OnPropertyChanged(nameof(TimelineSummary));

            if (_currentPosition > _duration)
            {
                CurrentPositionSeconds = _duration.TotalSeconds;
            }
        }
    }

    /// <summary>
    /// Gets the textual summary of the current timeline position.
    /// </summary>
    public string TimelineSummary =>
        FormattableString.Invariant($"{FormatTime(_currentPosition)} / {FormatTime(_duration)}");

    /// <summary>
    /// Gets the command that toggles between play and pause states.
    /// </summary>
    public ICommand TogglePlayPauseCommand { get; }

    /// <summary>
    /// Gets the available playback rate options.
    /// </summary>
    public ObservableCollection<PlaybackRateOptionViewModel> PlaybackRates { get; }

    /// <summary>
    /// Gets the configured stream routes displayed in the toolbar.
    /// </summary>
    public ObservableCollection<SimulationStreamRouteViewModel> StreamRoutes { get; }

    /// <summary>
    /// Gets the currently selected playback rate multiplier.
    /// </summary>
    public double SelectedPlaybackRate => _selectedPlaybackRate.Rate;

    /// <summary>
    /// Updates the total duration of the scenario timeline.
    /// </summary>
    /// <param name="duration">The new duration to apply.</param>
    public void SetDuration(TimeSpan duration)
    {
        if (duration < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(duration));
        }

        DurationSeconds = duration.TotalSeconds;
    }

    /// <summary>
    /// Updates the current playback position, clamping the value to the available range.
    /// </summary>
    /// <param name="position">The desired playback position.</param>
    public void Seek(TimeSpan position)
    {
        if (position < TimeSpan.Zero)
        {
            position = TimeSpan.Zero;
        }

        CurrentPositionSeconds = position.TotalSeconds;
    }

    /// <summary>
    /// Selects the specified playback rate option.
    /// </summary>
    /// <param name="rate">The playback multiplier to select.</param>
    public void SetSelectedPlaybackRate(double rate)
    {
        var option = PlaybackRates.FirstOrDefault(r => Math.Abs(r.Rate - rate) < 0.0001);
        if (option is not null)
        {
            SelectPlaybackRate(option);
        }
    }

    internal void SelectPlaybackRate(PlaybackRateOptionViewModel option)
    {
        if (_selectedPlaybackRate == option)
        {
            option.SetIsSelected(true);
            return;
        }

        foreach (var rate in PlaybackRates)
        {
            rate.SetIsSelected(rate == option);
        }

        _selectedPlaybackRate = option;
        OnPropertyChanged(nameof(SelectedPlaybackRate));
    }

    internal void ClearSelection(PlaybackRateOptionViewModel option)
    {
        if (_selectedPlaybackRate == option)
        {
            option.SetIsSelected(true);
        }
    }

    private void TogglePlayPause()
    {
        IsPlaying = !IsPlaying;
    }

    private static double Clamp(double value, double minimum, double maximum) =>
        Math.Min(Math.Max(value, minimum), maximum);

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private static string FormatTime(TimeSpan value)
    {
        if (value.TotalHours >= 1)
        {
            return value.ToString("hh\\:mm\\:ss", CultureInfo.InvariantCulture);
        }

        return value.ToString("mm\\:ss", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Represents a selectable playback rate option in the toolbar.
    /// </summary>
    public class PlaybackRateOptionViewModel : INotifyPropertyChanged
    {
        private readonly SimBarViewModel _owner;
        private bool _isSelected;

        internal PlaybackRateOptionViewModel(SimBarViewModel owner, double rate, string label)
        {
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            Rate = rate;
            Label = label ?? throw new ArgumentNullException(nameof(label));
        }

        /// <inheritdoc />
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Gets the playback multiplier represented by this option.
        /// </summary>
        public double Rate { get; }

        /// <summary>
        /// Gets the label shown in the UI for this option.
        /// </summary>
        public string Label { get; }

        /// <summary>
        /// Gets or sets a value indicating whether this option is the current selection.
        /// </summary>
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected == value)
                {
                    return;
                }

                if (value)
                {
                    _owner.SelectPlaybackRate(this);
                }
                else
                {
                    _owner.ClearSelection(this);
                }
            }
        }

        internal void SetIsSelected(bool value)
        {
            if (_isSelected == value)
            {
                return;
            }

            _isSelected = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
        }
    }

    /// <summary>
    /// Represents routing information for a simulation stream (e.g. GNSS, telemetry).
    /// </summary>
    public class SimulationStreamRouteViewModel : INotifyPropertyChanged
    {
        private string _selectedSource;

        /// <summary>
        /// Initializes a new instance of the <see cref="SimulationStreamRouteViewModel"/> class.
        /// </summary>
        /// <param name="streamName">Name of the stream presented to the user.</param>
        /// <param name="availableSources">The available sources that can feed the stream.</param>
        /// <param name="selectedSource">The default selected source.</param>
        public SimulationStreamRouteViewModel(
            string streamName,
            IReadOnlyList<string> availableSources,
            string selectedSource)
        {
            StreamName = streamName ?? throw new ArgumentNullException(nameof(streamName));
            AvailableSources = availableSources ?? throw new ArgumentNullException(nameof(availableSources));

            if (AvailableSources.Count == 0)
            {
                throw new ArgumentException("At least one source must be provided.", nameof(availableSources));
            }

            _selectedSource = AvailableSources.Contains(selectedSource) ? selectedSource : AvailableSources[0];
        }

        /// <inheritdoc />
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Gets the display name of the stream.
        /// </summary>
        public string StreamName { get; }

        /// <summary>
        /// Gets the available routing sources for this stream.
        /// </summary>
        public IReadOnlyList<string> AvailableSources { get; }

        /// <summary>
        /// Gets or sets the currently selected source.
        /// </summary>
        public string SelectedSource
        {
            get => _selectedSource;
            set
            {
                if (_selectedSource == value || string.IsNullOrWhiteSpace(value))
                {
                    return;
                }

                if (!AvailableSources.Contains(value))
                {
                    return;
                }

                _selectedSource = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedSource)));
            }
        }
    }
}
