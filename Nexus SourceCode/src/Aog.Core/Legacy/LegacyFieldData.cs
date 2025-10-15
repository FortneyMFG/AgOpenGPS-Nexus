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
        IReadOnlyList<FieldBoundary> boundaries,
        LegacyBackgroundImagery? backgroundImagery = null)
    {
        Tracks = tracks ?? throw new ArgumentNullException(nameof(tracks));
        Boundaries = boundaries ?? throw new ArgumentNullException(nameof(boundaries));
        BackgroundImagery = backgroundImagery;
    }

    /// <summary>
    /// Gets the imported AB and curve tracks.
    /// </summary>
    public IReadOnlyList<GuidanceTrackDefinition> Tracks { get; }

    /// <summary>
    /// Gets the imported field boundaries and headlands.
    /// </summary>
    public IReadOnlyList<FieldBoundary> Boundaries { get; }

    /// <summary>
    /// Gets the optional legacy background imagery bounding box and PNG payload.
    /// </summary>
    public LegacyBackgroundImagery? BackgroundImagery { get; }
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

/// <summary>
/// Represents the legacy background imagery payload associated with a field.
/// </summary>
public sealed class LegacyBackgroundImagery
{
    public LegacyBackgroundImagery(LegacyGeoBoundingBox boundingBox, ReadOnlyMemory<byte> imagePng)
    {
        BoundingBox = boundingBox;
        ImagePng = imagePng;
    }

    /// <summary>
    /// Gets the bounding box encompassing the imagery in legacy Geo coordinates.
    /// </summary>
    public LegacyGeoBoundingBox BoundingBox { get; }

    /// <summary>
    /// Gets the PNG payload backing the background imagery.
    /// </summary>
    public ReadOnlyMemory<byte> ImagePng { get; }
}

/// <summary>
/// Represents a geographic bounding box persisted by legacy BackPic.txt files.
/// </summary>
public readonly struct LegacyGeoBoundingBox
{
    public LegacyGeoBoundingBox(double minNorthing, double maxNorthing, double minEasting, double maxEasting)
    {
        if (double.IsNaN(minNorthing))
        {
            throw new ArgumentException("Minimum northing must be a valid number.", nameof(minNorthing));
        }

        if (double.IsNaN(maxNorthing))
        {
            throw new ArgumentException("Maximum northing must be a valid number.", nameof(maxNorthing));
        }

        if (double.IsNaN(minEasting))
        {
            throw new ArgumentException("Minimum easting must be a valid number.", nameof(minEasting));
        }

        if (double.IsNaN(maxEasting))
        {
            throw new ArgumentException("Maximum easting must be a valid number.", nameof(maxEasting));
        }

        if (maxNorthing < minNorthing)
        {
            throw new ArgumentException("Maximum northing must be greater than or equal to minimum northing.", nameof(maxNorthing));
        }

        if (maxEasting < minEasting)
        {
            throw new ArgumentException("Maximum easting must be greater than or equal to minimum easting.", nameof(maxEasting));
        }

        MinNorthing = minNorthing;
        MaxNorthing = maxNorthing;
        MinEasting = minEasting;
        MaxEasting = maxEasting;
    }

    public double MinNorthing { get; }

    public double MaxNorthing { get; }

    public double MinEasting { get; }

    public double MaxEasting { get; }
}
