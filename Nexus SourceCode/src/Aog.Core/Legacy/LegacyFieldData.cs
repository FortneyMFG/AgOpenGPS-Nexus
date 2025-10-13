using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Aog.Core.Paths;

namespace Aog.Core.Legacy;

/// <summary>
/// Represents the geometry and track data imported from a V6 field directory.
/// </summary>
public sealed class LegacyFieldData
{
    public LegacyFieldData(
        IReadOnlyList<GuidanceTrackDefinition> tracks,
        IReadOnlyList<FieldBoundary> boundaries)
    {
        Tracks = tracks ?? throw new ArgumentNullException(nameof(tracks));
        Boundaries = boundaries ?? throw new ArgumentNullException(nameof(boundaries));
    }

    /// <summary>
    /// Gets the imported AB and curve tracks.
    /// </summary>
    public IReadOnlyList<GuidanceTrackDefinition> Tracks { get; }

    /// <summary>
    /// Gets the imported field boundaries and headlands.
    /// </summary>
    public IReadOnlyList<FieldBoundary> Boundaries { get; }
}

/// <summary>
/// Describes a guidance track imported from V6.
/// </summary>
public sealed class GuidanceTrackDefinition
{
    public GuidanceTrackDefinition(
        string name,
        double headingRadians,
        PlanarPoint pointA,
        PlanarPoint pointB,
        double nudgeDistance,
        LegacyTrackMode mode,
        bool isVisible,
        IReadOnlyList<GuidanceCurvePoint> curvePoints)
    {
        Name = name ?? string.Empty;
        HeadingRadians = headingRadians;
        PointA = pointA;
        PointB = pointB;
        NudgeDistance = nudgeDistance;
        Mode = mode;
        IsVisible = isVisible;
        CurvePoints = new ReadOnlyCollection<GuidanceCurvePoint>(curvePoints ?? Array.Empty<GuidanceCurvePoint>());
    }

    public string Name { get; }

    public double HeadingRadians { get; }

    public PlanarPoint PointA { get; }

    public PlanarPoint PointB { get; }

    public double NudgeDistance { get; }

    public LegacyTrackMode Mode { get; }

    public bool IsVisible { get; }

    public IReadOnlyList<GuidanceCurvePoint> CurvePoints { get; }
}

/// <summary>
/// Represents a curve point captured in the legacy TrackLines.txt file.
/// </summary>
public sealed record GuidanceCurvePoint(PlanarPoint Point, double HeadingRadians);

/// <summary>
/// Enumerates the legacy track modes preserved for compatibility.
/// </summary>
public enum LegacyTrackMode
{
    None = 0,
    AbLine = 2,
    Curve = 4,
    BoundaryTrackOuter = 8,
    BoundaryTrackInner = 16,
    BoundaryCurve = 32,
    WaterPivot = 64,
}

/// <summary>
/// Represents a boundary polygon imported from the legacy Boundary.txt file.
/// </summary>
public sealed class FieldBoundary
{
    public FieldBoundary(bool isDriveThrough, IReadOnlyList<BoundaryVertex> perimeter, IReadOnlyList<HeadlandRing> headlands)
    {
        IsDriveThrough = isDriveThrough;
        Perimeter = new ReadOnlyCollection<BoundaryVertex>(perimeter ?? Array.Empty<BoundaryVertex>());
        Headlands = new ReadOnlyCollection<HeadlandRing>(headlands ?? Array.Empty<HeadlandRing>());
    }

    public bool IsDriveThrough { get; }

    public IReadOnlyList<BoundaryVertex> Perimeter { get; }

    public IReadOnlyList<HeadlandRing> Headlands { get; }
}

/// <summary>
/// Defines a vertex on a boundary or headland polyline.
/// </summary>
public sealed record BoundaryVertex(PlanarPoint Point, double HeadingRadians);

/// <summary>
/// Represents a single headland ring associated with a boundary.
/// </summary>
public sealed class HeadlandRing
{
    public HeadlandRing(IReadOnlyList<BoundaryVertex> vertices)
    {
        Vertices = new ReadOnlyCollection<BoundaryVertex>(vertices ?? Array.Empty<BoundaryVertex>());
    }

    public IReadOnlyList<BoundaryVertex> Vertices { get; }
}
