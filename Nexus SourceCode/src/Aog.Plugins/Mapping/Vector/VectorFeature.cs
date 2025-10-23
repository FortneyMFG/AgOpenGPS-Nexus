using System.Collections.Generic;
using System.Numerics;
using Aog.Plugins.Mapping.Core;

namespace Aog.Plugins.Mapping.Vector;

public enum VectorFeatureType
{
    Polygon,
    Polyline,
    Point
}

public sealed class VectorFeature
{
    public VectorFeature(
        VectorFeatureType type,
        IReadOnlyList<Double3> vertices,
        Vector4? fillColor = null,
        Vector4? lineColor = null,
        float? lineWidthMeters = null)
    {
        Type = type;
        Vertices = vertices;
        FillColor = fillColor;
        LineColor = lineColor;
        LineWidthMeters = lineWidthMeters;
    }

    public VectorFeatureType Type { get; }

    public IReadOnlyList<Double3> Vertices { get; }

    public Vector4? FillColor { get; }

    public Vector4? LineColor { get; }

    public float? LineWidthMeters { get; }
}
