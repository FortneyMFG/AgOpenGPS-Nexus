using System;
using Avalonia.Media;

namespace Aog.UI.Avalonia.ViewModels;

/// <summary>
/// Represents a single entry inside the layer legend, exposing the colour ramp
/// and range metadata sourced from the layer registry.
/// </summary>
public sealed class LayerLegendEntryViewModel
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LayerLegendEntryViewModel"/> class.
    /// </summary>
    /// <param name="layerId">Stable identifier for the layer.</param>
    /// <param name="displayName">Human readable label displayed in the legend.</param>
    /// <param name="minimumValue">Minimum numeric value represented by the ramp.</param>
    /// <param name="maximumValue">Maximum numeric value represented by the ramp.</param>
    /// <param name="units">Engineering units associated with the values.</param>
    /// <param name="gradientStart">Colour rendered at the minimum value.</param>
    /// <param name="gradientEnd">Colour rendered at the maximum value.</param>
    /// <param name="isPlanned">Whether the entry represents planned metadata.</param>
    /// <param name="description">Optional description surfaced below the ramp.</param>
    public LayerLegendEntryViewModel(
        string layerId,
        string displayName,
        double minimumValue,
        double maximumValue,
        string? units,
        Color gradientStart,
        Color gradientEnd,
        bool isPlanned,
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(layerId))
        {
            throw new ArgumentException("Layer identifier is required.", nameof(layerId));
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Layer display name is required.", nameof(displayName));
        }

        if (double.IsNaN(minimumValue) || double.IsInfinity(minimumValue))
        {
            throw new ArgumentOutOfRangeException(nameof(minimumValue));
        }

        if (double.IsNaN(maximumValue) || double.IsInfinity(maximumValue))
        {
            throw new ArgumentOutOfRangeException(nameof(maximumValue));
        }

        if (maximumValue < minimumValue)
        {
            throw new ArgumentException("Maximum value must be greater than or equal to the minimum value.", nameof(maximumValue));
        }

        LayerId = layerId;
        DisplayName = displayName.Trim();
        MinimumValue = minimumValue;
        MaximumValue = maximumValue;
        Units = string.IsNullOrWhiteSpace(units) ? null : units.Trim();
        GradientStart = gradientStart;
        GradientEnd = gradientEnd;
        IsPlanned = isPlanned;
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

        GradientBrush = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0.5, RelativeUnit.Relative),
            EndPoint = new RelativePoint(1, 0.5, RelativeUnit.Relative),
            GradientStops = new GradientStops
            {
                new GradientStop(gradientStart, 0),
                new GradientStop(gradientEnd, 1),
            },
        };

        RangeDisplay = BuildRangeDisplay(minimumValue, maximumValue, Units);
        ModeDisplay = isPlanned ? "Planned layer" : "Measured layer";
    }

    /// <summary>Gets the layer identifier sourced from the registry.</summary>
    public string LayerId { get; }

    /// <summary>Gets the label rendered inside the legend.</summary>
    public string DisplayName { get; }

    /// <summary>Gets the minimum numeric value represented by the ramp.</summary>
    public double MinimumValue { get; }

    /// <summary>Gets the maximum numeric value represented by the ramp.</summary>
    public double MaximumValue { get; }

    /// <summary>Gets the engineering units associated with the values.</summary>
    public string? Units { get; }

    /// <summary>Gets the colour rendered at the minimum value of the ramp.</summary>
    public Color GradientStart { get; }

    /// <summary>Gets the colour rendered at the maximum value of the ramp.</summary>
    public Color GradientEnd { get; }

    /// <summary>Gets a value indicating whether the entry represents planned metadata.</summary>
    public bool IsPlanned { get; }

    /// <summary>Gets an optional description surfaced below the ramp.</summary>
    public string? Description { get; }

    /// <summary>Gets the formatted range display surfaced in the UI.</summary>
    public string RangeDisplay { get; }

    /// <summary>Gets a short label describing whether the layer is planned or measured.</summary>
    public string ModeDisplay { get; }

    /// <summary>Gets a brush representing the colour ramp for the legend swatch.</summary>
    public IBrush GradientBrush { get; }

    /// <summary>
    /// Builds a user-friendly representation of the numeric range, including units.
    /// </summary>
    private static string BuildRangeDisplay(double minimum, double maximum, string? units)
    {
        if (!string.IsNullOrWhiteSpace(units) &&
            string.Equals(units, "fraction", StringComparison.OrdinalIgnoreCase) &&
            minimum >= 0 && maximum <= 1)
        {
            return FormattableString.Invariant($"{minimum:P0} – {maximum:P0}");
        }

        var baseDisplay = FormattableString.Invariant($"{minimum:0.##} – {maximum:0.##}");
        if (string.IsNullOrWhiteSpace(units))
        {
            return baseDisplay;
        }

        return string.Concat(baseDisplay, " ", units);
    }
}
