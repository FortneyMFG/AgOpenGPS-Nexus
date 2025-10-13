using System;

namespace Aog.Core.Paths;

/// <summary>
/// Generates lane offsets for straight AB guidance lines.
/// </summary>
public sealed class ABLinePlanner
{
    private readonly PlanarPoint _pointA;
    private readonly PlanarPoint _pointB;
    private readonly PlanarLine _referenceLine;
    private readonly double _laneSpacing;
    private readonly double _nudgeDistance;
    private readonly double _extensionLength;

    /// <summary>
    /// Creates a new planner for an AB line defined by two points.
    /// </summary>
    /// <param name="pointA">The first point of the reference line.</param>
    /// <param name="pointB">The second point of the reference line.</param>
    /// <param name="toolWidth">Total implement width in metres.</param>
    /// <param name="overlap">Configured overlap between adjacent passes in metres.</param>
    /// <param name="nudgeDistance">Additional lateral nudge applied to all passes in metres.</param>
    /// <param name="extensionLength">How far to extend generated line segments.</param>
    public ABLinePlanner(
        PlanarPoint pointA,
        PlanarPoint pointB,
        double toolWidth,
        double overlap,
        double nudgeDistance = 0,
        double extensionLength = 2000)
    {
        if (toolWidth <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(toolWidth), "Tool width must be positive.");
        }

        if (overlap < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(overlap), "Overlap cannot be negative.");
        }

        if (overlap >= toolWidth)
        {
            throw new ArgumentException("Overlap must be smaller than the tool width.", nameof(overlap));
        }

        if (extensionLength <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(extensionLength), "Extension length must be positive.");
        }

        _pointA = pointA;
        _pointB = pointB;
        _referenceLine = new PlanarLine(pointA, pointB).Extend(extensionLength);
        _laneSpacing = toolWidth - overlap;
        _nudgeDistance = nudgeDistance;
        _extensionLength = extensionLength;

        if (_referenceLine.Direction.LengthSquared < PlanarGeometry.Epsilon)
        {
            throw new ArgumentException("The AB line requires two distinct points.");
        }
    }

    /// <summary>
    /// Gets the heading of the AB line in radians (0 = north, increasing clockwise).
    /// </summary>
    public double Heading => Math.Atan2(_pointB.Easting - _pointA.Easting, _pointB.Northing - _pointA.Northing);

    /// <summary>
    /// Gets the spacing between adjacent lanes (tool width minus overlap).
    /// </summary>
    public double LaneSpacing => _laneSpacing;

    /// <summary>
    /// Returns the signed distance from the reference line to the specified pivot point.
    /// Positive distances are on the right-hand side of travel.
    /// </summary>
    public double GetSignedDistanceToReference(PlanarPoint pivot) => _referenceLine.SignedDistanceTo(pivot);

    /// <summary>
    /// Determines the closest pass index for the specified pivot point.
    /// </summary>
    /// <param name="pivot">Current vehicle pivot point.</param>
    /// <param name="toolOffset">Implement lateral offset (positive shifts implement to the right when travelling forward).</param>
    /// <param name="headingSameWay">True when travelling along the stored AB heading, false when reversing direction.</param>
    public int GetPassIndex(PlanarPoint pivot, double toolOffset, bool headingSameWay)
    {
        var distance = GetSignedDistanceToReference(pivot);
        var offset = headingSameWay ? -toolOffset : toolOffset;
        var adjusted = (distance - offset - _nudgeDistance) / _laneSpacing;

        return (int)Math.Round(adjusted, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Builds the lane line for the specified pass index.
    /// </summary>
    public PlanarLine GetPassLine(int passIndex, double toolOffset, bool headingSameWay)
    {
        var offset = (passIndex * _laneSpacing) + (headingSameWay ? -toolOffset : toolOffset) + _nudgeDistance;
        var heading = Heading;
        var offsetVector = new PlanarVector(Math.Cos(heading), -Math.Sin(heading)) * offset;

        var start = _pointA + offsetVector;
        var end = _pointB + offsetVector;
        return new PlanarLine(start, end).Extend(_extensionLength);
    }

    /// <summary>
    /// Computes the signed distance from the specified pass to the provided pivot point.
    /// Positive distances are on the right-hand side when travelling along the pass heading.
    /// </summary>
    public double GetSignedDistanceToPass(PlanarPoint pivot, int passIndex, double toolOffset, bool headingSameWay)
    {
        var line = GetPassLine(passIndex, toolOffset, headingSameWay);
        return line.SignedDistanceTo(pivot);
    }
}
