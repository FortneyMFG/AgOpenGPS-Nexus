using System;
using System.Collections.ObjectModel;

namespace Aog.UI.Avalonia.ViewModels;

/// <summary>
/// Provides design-time sample data for the simulation bar control.
/// </summary>
public sealed class SimBarViewModel
{
    private readonly ObservableCollection<SimulationPlaybackRateOptionViewModel> _playbackRates;
    private readonly ReadOnlyObservableCollection<SimulationPlaybackRateOptionViewModel> _playbackRateView;

    /// <summary>
    /// Initializes a new instance of the <see cref="SimBarViewModel"/> class.
    /// </summary>
    public SimBarViewModel()
    {
        _playbackRates = new ObservableCollection<SimulationPlaybackRateOptionViewModel>
        {
            CreatePlaybackRateOption(0.5),
            CreatePlaybackRateOption(1.0),
            CreatePlaybackRateOption(2.0),
        };
        _playbackRateView = new ReadOnlyObservableCollection<SimulationPlaybackRateOptionViewModel>(_playbackRates);

        // Default to 1× at design-time so bindings have a stable selected rate.
        _playbackRates[1].SetSelected(true, suppressCallback: true);
        SelectedPlaybackRate = _playbackRates[1].Rate;
    }

    /// <summary>
    /// Gets the sample playback rate options displayed in the design-time surface.
    /// </summary>
    public ReadOnlyObservableCollection<SimulationPlaybackRateOptionViewModel> PlaybackRates => _playbackRateView;

    /// <summary>
    /// Gets the playback rate associated with the currently selected option.
    /// </summary>
    public double SelectedPlaybackRate { get; private set; }

    private SimulationPlaybackRateOptionViewModel CreatePlaybackRateOption(double rate)
    {
        return new SimulationPlaybackRateOptionViewModel(rate, option =>
        {
            if (option is null)
            {
                return;
            }

            SelectedPlaybackRate = option.Rate;
        });
    }
}
