using System;
using System.Collections.Generic;

namespace Aog.Core.Paths;

/// <summary>
/// Generates inset headland polygons from a recorded field boundary.
/// </summary>
public static class HeadlandGenerator
{
    /// <summary>
    /// Generates a series of headland polygons inset from the provided boundary.
    /// </summary>
    /// <param name="boundary">Outer boundary points (must define a simple polygon).</param>
    /// <param name="headlandWidth">Width of a single headland pass.</param>
    /// <param name="passes">Number of headland passes to generate.</param>
    /// <returns>List of headland polygons ordered from outermost to innermost.</returns>
    public static IReadOnlyList<IReadOnlyList<PlanarPoint>> GenerateHeadlands(
        IReadOnlyList<PlanarPoint> boundary,
        double headlandWidth,
        int passes)
    {
        if (boundary is null)
        {
            throw new ArgumentNullException(nameof(boundary));
        }

        if (headlandWidth <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(headlandWidth), "Headland width must be positive.");
        }

        if (passes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(passes), "At least one headland pass is required.");
        }

        var basePolygon = NormalizeBoundary(boundary);
        if (basePolygon.Count < 3)
        {
            throw new ArgumentException("Boundary must contain at least three distinct points.", nameof(boundary));
        }

        var rings = new List<IReadOnlyList<PlanarPoint>>(passes);
        var current = basePolygon;

        for (var i = 0; i < passes; i++)
        {
            current = InsetPolygon(current, headlandWidth);
            if (current.Count < 3)
            {
                throw new InvalidOperationException("Headland inset collapsed the polygon. Reduce pass count or width.");
            }

            rings.Add(current);
        }

        return rings;
    }

    private static IReadOnlyList<PlanarPoint> NormalizeBoundary(IReadOnlyList<PlanarPoint> boundary)
    {
        var normalized = new List<PlanarPoint>();
        foreach (var point in boundary)
        {
            if (normalized.Count == 0 || normalized[^1] != point)
            {
                normalized.Add(point);
            }
        }

        if (normalized.Count > 1 && normalized[0] == normalized[^1])
        {
            normalized.RemoveAt(normalized.Count - 1);
        }

        return normalized;
    }

    private static IReadOnlyList<PlanarPoint> InsetPolygon(IReadOnlyList<PlanarPoint> polygon, double insetDistance)
    {
        var area = PlanarGeometryExtensions.ComputePolygonArea(polygon);
        var isCcw = area >= 0;
        var inset = new PlanarPoint[polygon.Count];

        for (var i = 0; i < polygon.Count; i++)
        {
            var current = polygon[i];
            var prev = polygon[(i - 1 + polygon.Count) % polygon.Count];
            var next = polygon[(i + 1) % polygon.Count];

            var dirPrev = (current - prev).Normalize();
            var dirNext = (next - current).Normalize();

            var interiorPrev = isCcw ? dirPrev.PerpendicularLeft() : dirPrev.PerpendicularRight();
            var interiorNext = isCcw ? dirNext.PerpendicularLeft() : dirNext.PerpendicularRight();

            var originPrev = current + (interiorPrev * insetDistance);
            var originNext = current + (interiorNext * insetDistance);

            var intersection = PlanarGeometryExtensions.IntersectLines(originPrev, dirPrev, originNext, dirNext);
            if (intersection is null)
            {
                var fallbackNormal = (interiorPrev + interiorNext).LengthSquared < PlanarGeometry.Epsilon
                    ? interiorPrev
                    : (interiorPrev + interiorNext).Normalize();
                inset[i] = current + (fallbackNormal * insetDistance);
            }
            else
            {
                inset[i] = intersection.Value;
            }
        }

        return inset;
    }
}
