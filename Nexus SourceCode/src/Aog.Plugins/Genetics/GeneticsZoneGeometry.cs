using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Aog.Core.Paths;

namespace Aog.Plugins.Genetics;

/// <summary>
/// Immutable polygon geometry describing a genetics layer zone.
/// </summary>
public sealed class GeneticsZoneGeometry
{
    private static readonly IReadOnlyList<IReadOnlyList<PlanarPoint>> EmptyHoles = Array.Empty<IReadOnlyList<PlanarPoint>>();

    /// <summary>
    /// Initializes a new instance of the <see cref="GeneticsZoneGeometry"/> class.
    /// </summary>
    /// <param name="outerBoundary">Outer boundary polygon expressed in planar metres.</param>
    /// <param name="holes">Optional inner holes/headlands expressed as polygons.</param>
    public GeneticsZoneGeometry(
        IReadOnlyList<PlanarPoint> outerBoundary,
        IReadOnlyList<IReadOnlyList<PlanarPoint>>? holes = null)
    {
        if (outerBoundary is null)
        {
            throw new ArgumentNullException(nameof(outerBoundary));
        }

        if (outerBoundary.Count < 3)
        {
            throw new ArgumentException("Outer boundary must contain at least three vertices.", nameof(outerBoundary));
        }

        var normalizedBoundary = new PlanarPoint[outerBoundary.Count];
        for (var i = 0; i < outerBoundary.Count; i++)
        {
            normalizedBoundary[i] = ValidatePoint(outerBoundary[i], nameof(outerBoundary));
        }

        OuterBoundary = new ReadOnlyCollection<PlanarPoint>(normalizedBoundary);

        if (holes is null || holes.Count == 0)
        {
            Holes = EmptyHoles;
        }
        else
        {
            var normalized = new List<IReadOnlyList<PlanarPoint>>(holes.Count);
            foreach (var hole in holes)
            {
                if (hole is null)
                {
                    throw new ArgumentException("Hole polygon cannot be null.", nameof(holes));
                }

                if (hole.Count == 0)
                {
                    normalized.Add(Array.Empty<PlanarPoint>());
                    continue;
                }

                if (hole.Count < 3)
                {
                    throw new ArgumentException("Hole polygons must contain at least three vertices.", nameof(holes));
                }

                var normalizedHole = new PlanarPoint[hole.Count];
                for (var i = 0; i < hole.Count; i++)
                {
                    normalizedHole[i] = ValidatePoint(hole[i], nameof(holes));
                }

                normalized.Add(new ReadOnlyCollection<PlanarPoint>(normalizedHole));
            }

            Holes = new ReadOnlyCollection<IReadOnlyList<PlanarPoint>>(normalized);
        }

        AreaSquareMeters = ComputeArea(OuterBoundary, Holes);
    }

    /// <summary>Gets the outer boundary polygon.</summary>
    public IReadOnlyList<PlanarPoint> OuterBoundary { get; }

    /// <summary>Gets the inner hole polygons (may be empty).</summary>
    public IReadOnlyList<IReadOnlyList<PlanarPoint>> Holes { get; }

    /// <summary>Gets the inclusive area of the polygon in square metres.</summary>
    public double AreaSquareMeters { get; }

    private static PlanarPoint ValidatePoint(PlanarPoint point, string parameterName)
    {
        if (!double.IsFinite(point.Easting) || !double.IsFinite(point.Northing))
        {
            throw new ArgumentOutOfRangeException(parameterName, point, "Coordinates must be finite values.");
        }

        return point;
    }

    private static double ComputeArea(
        IReadOnlyList<PlanarPoint> boundary,
        IReadOnlyList<IReadOnlyList<PlanarPoint>> holes)
    {
        var area = Math.Abs(PlanarGeometryExtensions.ComputePolygonArea(boundary));
        foreach (var hole in holes)
        {
            if (hole.Count >= 3)
            {
                area -= Math.Abs(PlanarGeometryExtensions.ComputePolygonArea(hole));
            }
        }

        return area < 0 ? 0 : area;
    }
}
