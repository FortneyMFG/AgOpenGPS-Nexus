using Aog.Core.Paths;

namespace Aog.Core.Legacy;

/// <summary>
/// Represents a legacy AB line converted into the Core planar frame.
/// </summary>
public sealed record LegacyAbLinePlanar(
    string Name,
    GeographicCoordinate PointA,
    GeographicCoordinate PointB,
    PlanarPoint PointAPlanar,
    PlanarPoint PointBPlanar,
    double HeadingDegrees,
    double LengthMeters);
