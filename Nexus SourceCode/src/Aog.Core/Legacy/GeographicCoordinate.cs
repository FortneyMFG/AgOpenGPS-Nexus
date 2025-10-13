using System;

namespace Aog.Core.Legacy;

/// <summary>
/// Represents a WGS84 geographic coordinate expressed in degrees.
/// </summary>
public readonly record struct GeographicCoordinate(double LatitudeDeg, double LongitudeDeg)
{
    /// <summary>
    /// Determines whether the coordinate is effectively equal to another using a small tolerance.
    /// </summary>
    public bool EqualsApprox(GeographicCoordinate other, double tolerance = 1e-9)
    {
        return Math.Abs(LatitudeDeg - other.LatitudeDeg) <= tolerance
            && Math.Abs(LongitudeDeg - other.LongitudeDeg) <= tolerance;
    }
}
