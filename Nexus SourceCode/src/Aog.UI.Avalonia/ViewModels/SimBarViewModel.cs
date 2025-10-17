using System;
using System.Collections.Generic;

namespace Aog.UI.Avalonia.ViewModels;

/// <summary>
/// Provides design-time sample data for the simulation bar control.
/// </summary>
public sealed class SimBarViewModel
{
    private readonly List<SimulationPlaybackRateOptionViewModel> _playbackRates;

    /// <summary>
    /// Initializes a new instance of the <see cref="SimBarViewModel"/> class.
    /// </summary>
    public SimBarViewModel()
    {
        _playbackRates = new List<SimulationPlaybackRateOptionViewModel>
        {
            CreatePlaybackRateOption(0.5),
            CreatePlaybackRateOption(1.0),
            CreatePlaybackRateOption(2.0),
        };

        // Default to 1× at design-time so bindings have a stable selected rate.
        _playbackRates[1].SetSelected(true, suppressCallback: true);
        SelectedPlaybackRate = _playbackRates[1].Rate;
    }

    /// <summary>
    /// Gets the sample playback rate options displayed in the design-time surface.
    /// </summary>
    public IReadOnlyList<SimulationPlaybackRateOptionViewModel> PlaybackRates => _playbackRates;

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
