using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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
        LegacyFieldOverview? overview,
        IReadOnlyList<LegacyFlag> flags,
        LegacyContourResume contour,
        IReadOnlyList<LegacyRecordedPath> recordedPaths,
        IReadOnlyList<LegacyTramTemplate> tramTemplates,
        LegacyWorkedAreaHistory workedArea,
        LegacyBackgroundImagery? backgroundImagery = null)
    {
        Tracks = tracks ?? throw new ArgumentNullException(nameof(tracks));
        Boundaries = boundaries ?? throw new ArgumentNullException(nameof(boundaries));

        Overview = overview;
        Flags = new ReadOnlyCollection<LegacyFlag>((flags ?? Array.Empty<LegacyFlag>()).ToList());
        Contour = contour ?? LegacyContourResume.Empty;
        RecordedPaths = new ReadOnlyCollection<LegacyRecordedPath>((recordedPaths ?? Array.Empty<LegacyRecordedPath>()).ToList());
        TramTemplates = new ReadOnlyCollection<LegacyTramTemplate>((tramTemplates ?? Array.Empty<LegacyTramTemplate>()).ToList());
        WorkedArea = workedArea ?? LegacyWorkedAreaHistory.Empty;

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
    /// Gets the optional field overview metadata imported from <c>Field.txt</c>.
    /// </summary>
    public LegacyFieldOverview? Overview { get; }

    /// <summary>
    /// Gets the collection of scouting flags imported from <c>Flags.txt</c>.
    /// </summary>
    public IReadOnlyList<LegacyFlag> Flags { get; }

    /// <summary>
    /// Gets the contour resume state imported from <c>Contour.txt</c>.
    /// </summary>
    public LegacyContourResume Contour { get; }

    /// <summary>
    /// Gets the recorded path logs imported from <c>RecPath.txt</c>.
    /// </summary>
    public IReadOnlyList<LegacyRecordedPath> RecordedPaths { get; }

    /// <summary>
    /// Gets the tram line templates imported from <c>Tram.txt</c>.
    /// </summary>
    public IReadOnlyList<LegacyTramTemplate> TramTemplates { get; }

    /// <summary>
    /// Gets the worked area history imported from <c>Sections.txt</c>.
    /// </summary>
    public LegacyWorkedAreaHistory WorkedArea { get; }

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
        CurvePoints = new ReadOnlyCollection<GuidanceCurvePoint>((curvePoints ?? Array.Empty<GuidanceCurvePoint>()).ToList());
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
        Perimeter = new ReadOnlyCollection<BoundaryVertex>((perimeter ?? Array.Empty<BoundaryVertex>()).ToList());
        Headlands = new ReadOnlyCollection<HeadlandRing>((headlands ?? Array.Empty<HeadlandRing>()).ToList());
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
        Vertices = new ReadOnlyCollection<BoundaryVertex>((vertices ?? Array.Empty<BoundaryVertex>()).ToList());
    }

    public IReadOnlyList<BoundaryVertex> Vertices { get; }
}

/// <summary>
/// Captures the metadata stored in the legacy <c>Field.txt</c> overview document.
/// </summary>
public sealed class LegacyFieldOverview
{
    public LegacyFieldOverview(
        string? fieldName,
        string? operatorName,
        DateTimeOffset? createdAtUtc,
        GeographicCoordinate origin,
        double? convergenceAngleDegrees,
        double? elevationMeters,
        string? notes)
    {
        FieldName = string.IsNullOrWhiteSpace(fieldName) ? null : fieldName;
        OperatorName = string.IsNullOrWhiteSpace(operatorName) ? null : operatorName;
        CreatedAtUtc = createdAtUtc;
        Origin = origin;
        ConvergenceAngleDegrees = convergenceAngleDegrees;
        ElevationMeters = elevationMeters;
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes;
    }

    /// <summary>Gets the friendly field name recorded in the overview file.</summary>
    public string? FieldName { get; }

    /// <summary>Gets the operator responsible for creating the field.</summary>
    public string? OperatorName { get; }

    /// <summary>Gets the timestamp when the field was created, if present.</summary>
    public DateTimeOffset? CreatedAtUtc { get; }

    /// <summary>Gets the WGS84 origin captured when the field was created.</summary>
    public GeographicCoordinate Origin { get; }

    /// <summary>Gets the convergence angle in degrees used when projecting the field.</summary>
    public double? ConvergenceAngleDegrees { get; }

    /// <summary>Gets the optional field elevation in metres.</summary>
    public double? ElevationMeters { get; }

    /// <summary>Gets any free-form notes persisted alongside the overview metadata.</summary>
    public string? Notes { get; }
}

/// <summary>
/// Represents a geo-referenced scouting flag imported from the legacy <c>Flags.txt</c> document.
/// </summary>
public sealed class LegacyFlag
{
    public LegacyFlag(int id, string? label, PlanarPoint location, double headingDegrees, string color, string? notes)
    {
        if (string.IsNullOrWhiteSpace(color))
        {
            throw new ArgumentException("Flag colour must be provided.", nameof(color));
        }

        Id = id;
        Label = string.IsNullOrWhiteSpace(label) ? null : label;
        Location = location;
        HeadingDegrees = headingDegrees;
        Color = color.Trim();
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes;
    }

    /// <summary>Gets the identifier originally assigned by V6.</summary>
    public int Id { get; }

    /// <summary>Gets the optional label captured with the flag.</summary>
    public string? Label { get; }

    /// <summary>Gets the planar location of the flag in metres.</summary>
    public PlanarPoint Location { get; }

    /// <summary>Gets the heading, in degrees, recorded when the flag was placed.</summary>
    public double HeadingDegrees { get; }

    /// <summary>Gets the colour identifier as stored in the legacy export.</summary>
    public string Color { get; }

    /// <summary>Gets any operator notes associated with the flag.</summary>
    public string? Notes { get; }
}

/// <summary>
/// Represents the saved contour strips and pending buffer captured in <c>Contour.txt</c>.
/// </summary>
public sealed class LegacyContourResume
{
    public static LegacyContourResume Empty { get; } = new(
        isRecording: false,
        Array.Empty<LegacyContourStrip>(),
        null);

    public LegacyContourResume(bool isRecording, IReadOnlyList<LegacyContourStrip> savedStrips, LegacyContourStrip? pendingStrip)
    {
        IsRecording = isRecording;
        SavedStrips = new ReadOnlyCollection<LegacyContourStrip>((savedStrips ?? Array.Empty<LegacyContourStrip>()).ToList());
        PendingStrip = pendingStrip;
    }

    /// <summary>Gets a value indicating whether contour recording was active.</summary>
    public bool IsRecording { get; }

    /// <summary>Gets the collection of persisted contour strips.</summary>
    public IReadOnlyList<LegacyContourStrip> SavedStrips { get; }

    /// <summary>Gets the pending strip that had not yet been committed when exported.</summary>
    public LegacyContourStrip? PendingStrip { get; }
}

/// <summary>
/// Represents a single contour strip comprised of planar vertices.
/// </summary>
public sealed class LegacyContourStrip
{
    public LegacyContourStrip(IReadOnlyList<PlanarPoint> vertices)
    {
        Vertices = new ReadOnlyCollection<PlanarPoint>((vertices ?? Array.Empty<PlanarPoint>()).ToList());
    }

    /// <summary>Gets the vertices defining the contour strip.</summary>
    public IReadOnlyList<PlanarPoint> Vertices { get; }
}

/// <summary>
/// Represents a recorded path replay log (<c>RecPath.txt</c>).
/// </summary>
public sealed class LegacyRecordedPath
{
    public LegacyRecordedPath(string name, IReadOnlyList<LegacyRecordedPathPoint> samples)
    {
        Name = string.IsNullOrWhiteSpace(name) ? "Recorded Path" : name.Trim();
        Samples = new ReadOnlyCollection<LegacyRecordedPathPoint>((samples ?? Array.Empty<LegacyRecordedPathPoint>()).ToList());
    }

    /// <summary>Gets the friendly path name.</summary>
    public string Name { get; }

    /// <summary>Gets the ordered samples captured in the legacy recording.</summary>
    public IReadOnlyList<LegacyRecordedPathPoint> Samples { get; }
}

/// <summary>
/// Represents a single sample in a recorded path log.
/// </summary>
public sealed record LegacyRecordedPathPoint(PlanarPoint Position, double HeadingDegrees, double SpeedMetersPerSecond, bool IsAutosteerEnabled);

/// <summary>
/// Represents a tram line template imported from <c>Tram.txt</c>.
/// </summary>
public sealed class LegacyTramTemplate
{
    public LegacyTramTemplate(
        string name,
        double spacingMeters,
        IReadOnlyList<PlanarPoint> outerBoundary,
        IReadOnlyList<IReadOnlyList<PlanarPoint>> passes)
    {
        Name = string.IsNullOrWhiteSpace(name) ? "Tram Template" : name.Trim();
        SpacingMeters = spacingMeters;
        OuterBoundary = new ReadOnlyCollection<PlanarPoint>((outerBoundary ?? Array.Empty<PlanarPoint>()).ToList());
        var normalizedPasses = (passes ?? Array.Empty<IReadOnlyList<PlanarPoint>>())
            .Select(pass => (IReadOnlyList<PlanarPoint>)new ReadOnlyCollection<PlanarPoint>((pass ?? Array.Empty<PlanarPoint>()).ToList()))
            .ToList();

        Passes = new ReadOnlyCollection<IReadOnlyList<PlanarPoint>>(normalizedPasses);
    }

    /// <summary>Gets the template name.</summary>
    public string Name { get; }

    /// <summary>Gets the configured tram spacing in metres.</summary>
    public double SpacingMeters { get; }

    /// <summary>Gets the exterior boundary associated with the template.</summary>
    public IReadOnlyList<PlanarPoint> OuterBoundary { get; }

    /// <summary>Gets the ordered tram passes contained in the template.</summary>
    public IReadOnlyList<IReadOnlyList<PlanarPoint>> Passes { get; }
}

/// <summary>
/// Represents the worked area history exported to <c>Sections.txt</c>.
/// </summary>
public sealed class LegacyWorkedAreaHistory
{
    public static LegacyWorkedAreaHistory Empty { get; } = new(0, Array.Empty<LegacyWorkedAreaCell>(), Array.Empty<LegacyWorkedAreaCell>(), null);

    public LegacyWorkedAreaHistory(
        double cellSizeMeters,
        IReadOnlyList<LegacyWorkedAreaCell> savedCells,
        IReadOnlyList<LegacyWorkedAreaCell> pendingCells,
        string? layerId)
    {
        CellSizeMeters = cellSizeMeters;
        SavedCells = new ReadOnlyCollection<LegacyWorkedAreaCell>((savedCells ?? Array.Empty<LegacyWorkedAreaCell>()).ToList());
        PendingCells = new ReadOnlyCollection<LegacyWorkedAreaCell>((pendingCells ?? Array.Empty<LegacyWorkedAreaCell>()).ToList());
        LayerId = string.IsNullOrWhiteSpace(layerId) ? null : layerId;
    }

    /// <summary>Gets the edge length for each worked-area cell.</summary>
    public double CellSizeMeters { get; }

    /// <summary>Gets the cells that have been persisted in the legacy export.</summary>
    public IReadOnlyList<LegacyWorkedAreaCell> SavedCells { get; }

    /// <summary>Gets the cells that were pending flush when the export was produced.</summary>
    public IReadOnlyList<LegacyWorkedAreaCell> PendingCells { get; }

    /// <summary>Gets the layer identifier associated with the coverage data.</summary>
    public string? LayerId { get; }
}

/// <summary>
/// Represents a single worked-area coverage cell imported from legacy exports.
/// </summary>
public sealed record LegacyWorkedAreaCell(PlanarPoint Center, double CoverageFraction);

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
