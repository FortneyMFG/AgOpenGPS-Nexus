using System;
using System.Windows.Input;

namespace Aog.UI.Avalonia.ViewModels;

/// <summary>
/// Represents a selectable playback rate entry for the simulation bar.
/// </summary>
public sealed class SimulationPlaybackRateOptionViewModel : ObservableObject
{
    private readonly Action<SimulationPlaybackRateOptionViewModel> _onSelected;
    private bool _isSelected;

    public SimulationPlaybackRateOptionViewModel(
        double rate,
        Action<SimulationPlaybackRateOptionViewModel> onSelected,
        string? label = null)
    {
        if (rate <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(rate), "Playback rate must be positive.");
        }

        Rate = rate;
        Label = string.IsNullOrWhiteSpace(label)
            ? $"{rate:0.##}×"
            : label;
        _onSelected = onSelected ?? throw new ArgumentNullException(nameof(onSelected));
        SelectCommand = new DelegateCommand(_ => _onSelected(this));
    }

    /// <summary>
    /// Gets the numeric playback rate represented by the option.
    /// </summary>
    public double Rate { get; }

    /// <summary>
    /// Gets the numeric multiplier associated with the playback rate.
    /// </summary>
    public double Multiplier => Rate;

    /// <summary>
    /// Gets the formatted label displayed in the UI.
    /// </summary>
    public string Label { get; }

    /// <summary>
    /// Gets the command invoked when the option is selected via the UI.
    /// </summary>
    public ICommand SelectCommand { get; }

    /// <summary>
    /// Gets or sets whether the option is currently selected.
    /// </summary>
    public bool IsSelected
    {
        get => _isSelected;
        set => SetSelected(value, suppressCallback: false);
    }

    internal void SetSelected(bool value, bool suppressCallback)
    {
        if (!SetProperty(ref _isSelected, value))
        {
            return;
        }

        if (!suppressCallback && value)
        {
            _onSelected(this);
        }
    }
}
