using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Media;

namespace Aog.UI.Avalonia.Models;

/// <summary>
/// Represents a logical map layer rendered within the <see cref="Controls.MapView"/>.
/// </summary>
public sealed class MapLayer
{
    public MapLayer(string layerId, string displayName, LayerVisualizationStyle style, IReadOnlyList<MapLayerCell> cells, bool isVisible = true, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(layerId))
        {
            throw new ArgumentException("Layer identifier is required.", nameof(layerId));
        }

        if (style is null)
        {
            throw new ArgumentNullException(nameof(style));
        }

        LayerId = layerId;
        DisplayName = string.IsNullOrWhiteSpace(displayName) ? layerId : displayName.Trim();
        Style = style;
        Cells = new ReadOnlyCollection<MapLayerCell>(cells ?? Array.Empty<MapLayerCell>());
        IsVisible = isVisible;
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    /// <summary>Gets the stable registry identifier for the layer.</summary>
    public string LayerId { get; }

    /// <summary>Gets the friendly label surfaced to users.</summary>
    public string DisplayName { get; }

    /// <summary>Gets the visualisation style applied when rendering the layer.</summary>
    public LayerVisualizationStyle Style { get; }

    /// <summary>Gets the coverage/value cells contained within the layer.</summary>
    public IReadOnlyList<MapLayerCell> Cells { get; }

    /// <summary>Gets a value indicating whether the layer is currently visible.</summary>
    public bool IsVisible { get; }

    /// <summary>Gets an optional description surfaced in legend entries.</summary>
    public string? Description { get; }
}

/// <summary>
/// Represents a single square cell rendered within a <see cref="MapLayer"/>.
/// </summary>
/// <param name="Center">Cell centre point in world coordinates (metres).</param>
/// <param name="SizeMeters">Cell edge length in metres.</param>
/// <param name="Value">Numeric value associated with the cell.</param>
public readonly record struct MapLayerCell(Point Center, double SizeMeters, double Value);

/// <summary>
/// Describes the visual appearance for a <see cref="MapLayer"/>.
/// </summary>
public sealed class LayerVisualizationStyle
{
    public LayerVisualizationStyle(Color gradientStart, Color gradientEnd, double minimumValue, double maximumValue, string? units = null, bool isPlanned = false, Color? outlineColor = null)
    {
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

        GradientStart = gradientStart;
        GradientEnd = gradientEnd;
        MinimumValue = minimumValue;
        MaximumValue = maximumValue;
        Units = string.IsNullOrWhiteSpace(units) ? null : units;
        IsPlanned = isPlanned;
        OutlineColor = outlineColor ?? Colors.Transparent;
    }

    /// <summary>Gets the colour at the minimum value of the ramp.</summary>
    public Color GradientStart { get; }

    /// <summary>Gets the colour at the maximum value of the ramp.</summary>
    public Color GradientEnd { get; }

    /// <summary>Gets the minimum numeric value represented by the ramp.</summary>
    public double MinimumValue { get; }

    /// <summary>Gets the maximum numeric value represented by the ramp.</summary>
    public double MaximumValue { get; }

    /// <summary>Gets the engineering units associated with values, when known.</summary>
    public string? Units { get; }

    /// <summary>Gets a value indicating whether the layer represents planned guidance.</summary>
    public bool IsPlanned { get; }

    /// <summary>Gets the outline colour used when rendering cell borders.</summary>
    public Color OutlineColor { get; }

    /// <summary>
    /// Computes the colour that should represent the supplied value.
    /// </summary>
    public Color Evaluate(double value)
    {
        if (MaximumValue <= MinimumValue)
        {
            return GradientEnd;
        }

        var t = (value - MinimumValue) / (MaximumValue - MinimumValue);
        if (double.IsNaN(t))
        {
            t = 0;
        }

        t = Math.Clamp(t, 0, 1);
        byte Lerp(byte a, byte b) => (byte)(a + ((b - a) * t));

        return Color.FromArgb(
            Lerp(GradientStart.A, GradientEnd.A),
            Lerp(GradientStart.R, GradientEnd.R),
            Lerp(GradientStart.G, GradientEnd.G),
            Lerp(GradientStart.B, GradientEnd.B));
    }
}
