using System;
using System.Collections.Generic;

namespace Aog.Core.Paths;

/// <summary>
/// Builds parallel offsets of recorded curve paths.
/// </summary>
public static class CurvePathPlanner
{
    /// <summary>
    /// Computes a parallel offset polyline using right-hand positive distances.
    /// </summary>
    /// <param name="path">Reference curve points.</param>
    /// <param name="offset">Offset distance in metres (positive shifts right of travel).</param>
    /// <exception cref="ArgumentException">Thrown when the path contains fewer than two distinct points.</exception>
    public static IReadOnlyList<PlanarPoint> Offset(IReadOnlyList<PlanarPoint> path, double offset)
    {
        if (path is null)
        {
            throw new ArgumentNullException(nameof(path));
        }

        if (path.Count < 2)
        {
            throw new ArgumentException("The curve requires at least two points.", nameof(path));
        }

        var result = new PlanarPoint[path.Count];

        for (var i = 0; i < path.Count; i++)
        {
            var current = path[i];
            var prevIndex = i == 0 ? i : i - 1;
            var nextIndex = i == path.Count - 1 ? i : i + 1;

            var prevPoint = path[prevIndex];
            var nextPoint = path[nextIndex];

            if (prevPoint == current && nextPoint == current)
            {
                throw new ArgumentException("Curve contains duplicate points that collapse to a single location.", nameof(path));
            }

            PlanarVector directionPrev;
            PlanarVector directionNext;

            if (current == prevPoint)
            {
                directionPrev = (nextPoint - current).Normalize();
            }
            else
            {
                directionPrev = (current - prevPoint).Normalize();
            }

            if (current == nextPoint)
            {
                directionNext = (current - prevPoint).Normalize();
            }
            else
            {
                directionNext = (nextPoint - current).Normalize();
            }

            var normalPrev = directionPrev.PerpendicularRight();
            var normalNext = directionNext.PerpendicularRight();

            var linePrevOrigin = current + (normalPrev * offset);
            var lineNextOrigin = current + (normalNext * offset);

            var intersection = PlanarGeometryExtensions.IntersectLines(linePrevOrigin, directionPrev, lineNextOrigin, directionNext);

            if (intersection is null)
            {
                // Parallel segments - fall back to translating by the normal of either segment.
                var fallbackNormal = (normalPrev + normalNext).LengthSquared < PlanarGeometry.Epsilon
                    ? normalPrev
                    : (normalPrev + normalNext).Normalize();
                result[i] = current + (fallbackNormal * offset);
            }
            else
            {
                result[i] = intersection.Value;
            }
        }

        return result;
    }
}
