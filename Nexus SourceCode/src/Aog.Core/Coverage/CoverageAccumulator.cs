using System;

namespace Aog.Core.Coverage;

/// <summary>
/// Aggregates coverage area totals by integrating triangle strips generated while a
/// section is active. The algorithm mirrors the V6 triangle-strip accumulator so that
/// regression data captured from the legacy renderer can be replayed losslessly.
/// </summary>
public sealed class CoverageAccumulator
{
    private PlanarPoint _previousLeft;
    private PlanarPoint _previousRight;
    private bool _isPatchActive;

    /// <summary>
    /// Gets the total mapped area in square meters accumulated across all patches.
    /// </summary>
    public double TotalAreaSquareMeters { get; private set; }

    /// <summary>
    /// Gets the user-toggled area tally in square meters. Nexus keeps the value
    /// separate so the UI can display both the absolute total and an operator
    /// controlled tally (matching the V6 "Worked Area User" counter).
    /// </summary>
    public double UserAreaSquareMeters { get; private set; }

    /// <summary>
    /// Gets or sets the inclusive field area (outer boundary minus inner holes) used
    /// to compute coverage percentages. Leave <c>null</c> when unknown.
    /// </summary>
    public double? FieldAreaSquareMeters { get; set; }

    /// <summary>
    /// Gets the number of completed coverage patches.
    /// </summary>
    public int PatchCount { get; private set; }

    /// <summary>
    /// Starts a new coverage patch anchored at the supplied left/right boom corners.
    /// Subsequent samples extend the triangle strip until <see cref="EndPatch"/> is
    /// invoked.
    /// </summary>
    /// <param name="left">Left boom corner in the local planar map.</param>
    /// <param name="right">Right boom corner in the local planar map.</param>
    /// <exception cref="InvalidOperationException">Thrown when a patch is already active.</exception>
    public void BeginPatch(PlanarPoint left, PlanarPoint right)
    {
        ValidatePoint(left, nameof(left));
        ValidatePoint(right, nameof(right));

        if (_isPatchActive)
        {
            throw new InvalidOperationException("A coverage patch is already active.");
        }

        _previousLeft = left;
        _previousRight = right;
        _isPatchActive = true;
    }

    /// <summary>
    /// Extends the active coverage patch with a new sample. The generated strip area is
    /// added to <see cref="TotalAreaSquareMeters"/> and, when <paramref name="includeInUserTotals"/>
    /// is <c>true</c>, to <see cref="UserAreaSquareMeters"/>.
    /// </summary>
    /// <param name="left">Left boom corner for the new sample.</param>
    /// <param name="right">Right boom corner for the new sample.</param>
    /// <param name="includeInUserTotals">True to contribute the strip to the user tally.</param>
    /// <returns>The strip area in square meters.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no patch is active.</exception>
    public double AddSample(PlanarPoint left, PlanarPoint right, bool includeInUserTotals = true)
    {
        ValidatePoint(left, nameof(left));
        ValidatePoint(right, nameof(right));

        if (!_isPatchActive)
        {
            throw new InvalidOperationException("BeginPatch must be called before adding samples.");
        }

        var area = ComputeStripArea(_previousLeft, _previousRight, left, right);

        TotalAreaSquareMeters += area;

        if (includeInUserTotals)
        {
            UserAreaSquareMeters += area;
        }

        _previousLeft = left;
        _previousRight = right;

        return area;
    }

    /// <summary>
    /// Ends the current coverage patch. The next call to <see cref="BeginPatch"/>
    /// starts a new strip.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when no patch is active.</exception>
    public void EndPatch()
    {
        if (!_isPatchActive)
        {
            throw new InvalidOperationException("No coverage patch is active.");
        }

        _isPatchActive = false;
        PatchCount++;
    }

    /// <summary>
    /// Computes the remaining uncovered area in square meters. Returns <c>null</c>
    /// when the field area is unknown.
    /// </summary>
    public double? RemainingAreaSquareMeters
    {
        get
        {
            if (FieldAreaSquareMeters is not { } fieldArea)
            {
                return null;
            }

            var remaining = fieldArea - TotalAreaSquareMeters;
            return remaining < 0 ? 0 : remaining;
        }
    }

    /// <summary>
    /// Computes the coverage percentage relative to the configured field area. Returns
    /// <c>null</c> when the field area is unknown or zero.
    /// </summary>
    public double? CoveragePercent
    {
        get
        {
            if (FieldAreaSquareMeters is not { } fieldArea || fieldArea <= 0)
            {
                return null;
            }

            return TotalAreaSquareMeters / fieldArea * 100.0;
        }
    }

    private static double ComputeStripArea(PlanarPoint previousLeft, PlanarPoint previousRight, PlanarPoint currentLeft, PlanarPoint currentRight)
    {
        // The V6 renderer emitted triangle strips in the order L0, R0, L1, R1… and accumulated
        // the area of the last four vertices (R1, L1, R0, L0). The shoelace formula applied to
        // the equivalent quadrilateral preserves those results exactly, including degenerate
        // strips when points overlap.
        var area = Shoelace(previousLeft, previousRight, currentRight, currentLeft);
        return Math.Abs(area) * 0.5;
    }

    private static double Shoelace(PlanarPoint a, PlanarPoint b, PlanarPoint c, PlanarPoint d)
    {
        return (a.Easting * b.Northing - b.Easting * a.Northing)
             + (b.Easting * c.Northing - c.Easting * b.Northing)
             + (c.Easting * d.Northing - d.Easting * c.Northing)
             + (d.Easting * a.Northing - a.Easting * d.Northing);
    }

    private static void ValidatePoint(PlanarPoint point, string parameterName)
    {
        if (!double.IsFinite(point.Easting) || !double.IsFinite(point.Northing))
        {
            throw new ArgumentOutOfRangeException(parameterName, point, "Coordinates must be finite values.");
        }
    }
}

/// <summary>
/// Represents a point expressed in the local planar map used by the coverage engine.
/// </summary>
/// <param name="Easting">X coordinate in meters.</param>
/// <param name="Northing">Y coordinate in meters.</param>
public readonly record struct PlanarPoint(double Easting, double Northing)
{
    /// <summary>
    /// Gets the string representation using invariant culture to aid debug logging.
    /// </summary>
    public override string ToString() => $"({Easting}, {Northing})";
}
