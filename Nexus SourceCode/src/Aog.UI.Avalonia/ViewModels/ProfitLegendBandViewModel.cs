using System;
using Avalonia.Media;

namespace Aog.UI.Avalonia.ViewModels;

/// <summary>
/// Represents a discrete colour band surfaced in the profit heatmap legend.
/// </summary>
public sealed class ProfitLegendBandViewModel
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProfitLegendBandViewModel"/> class.
    /// </summary>
    /// <param name="label">Short label describing the band.</param>
    /// <param name="rangeDisplay">Human-friendly numeric range for the band.</param>
    /// <param name="color">Colour swatch used in the UI.</param>
    public ProfitLegendBandViewModel(string label, string rangeDisplay, Color color)
    {
        if (string.IsNullOrWhiteSpace(label))
        {
            throw new ArgumentException("Legend label is required.", nameof(label));
        }

        if (string.IsNullOrWhiteSpace(rangeDisplay))
        {
            throw new ArgumentException("Range display is required.", nameof(rangeDisplay));
        }

        Label = label.Trim();
        RangeDisplay = rangeDisplay.Trim();
        SwatchBrush = new SolidColorBrush(color);
    }

    /// <summary>Gets the short label describing the legend band.</summary>
    public string Label { get; }

    /// <summary>Gets the formatted numeric range.</summary>
    public string RangeDisplay { get; }

    /// <summary>Gets the colour swatch rendered in the UI.</summary>
    public IBrush SwatchBrush { get; }
}
