using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;

namespace Aog.UI.Avalonia.Models;

/// <summary>
/// Represents a guidance path rendered on the map overlay.
/// </summary>
public sealed class GuidanceTrack
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GuidanceTrack"/> class.
    /// </summary>
    /// <param name="name">Human friendly label describing the track.</param>
    /// <param name="points">Ordered list of world-coordinate points in meters.</param>
    /// <param name="color">Stroke color used when drawing the track.</param>
    /// <param name="thickness">Stroke thickness in pixels.</param>
    public GuidanceTrack(string name, IReadOnlyList<Point> points, Color color, double thickness)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(points);

        Name = name;
        Points = points;
        Color = color;
        Thickness = thickness;
    }

    /// <summary>Gets the descriptive name of the track.</summary>
    public string Name { get; }

    /// <summary>Gets the ordered points describing the path.</summary>
    public IReadOnlyList<Point> Points { get; }

    /// <summary>Gets the color used to render the path.</summary>
    public Color Color { get; }

    /// <summary>Gets the stroke thickness used to draw the path.</summary>
    public double Thickness { get; }
}
