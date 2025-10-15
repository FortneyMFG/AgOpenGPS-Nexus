using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Aog.Core.Zones;

/// <summary>
/// Zone classification aligned with ADR-027 spatial constraints.
/// </summary>
public enum ZoneType
{
    /// <summary>
    /// Zone type has not been assigned.
    /// </summary>
    Unspecified = 0,

    /// <summary>
    /// Field or project boundary.
    /// </summary>
    Boundary = 1,

    /// <summary>
    /// Headland region derived from boundary buffers.
    /// </summary>
    Headland = 2,

    /// <summary>
    /// Hard keep-out / no-go area.
    /// </summary>
    KeepOut = 3,

    /// <summary>
    /// Driveable region where work is disabled (product off, logging only).
    /// </summary>
    WorkDisabled = 4
}

/// <summary>
/// Represents a coordinate expressed in the project CRS.
/// </summary>
/// <param name="Longitude">Longitude (or X) component.</param>
/// <param name="Latitude">Latitude (or Y) component.</param>
/// <param name="ElevationMeters">Optional elevation component.</param>
public readonly record struct ZoneCoordinate(double Longitude, double Latitude, double? ElevationMeters = null)
{
    /// <summary>
    /// Returns the coordinate as a tuple.
    /// </summary>
    public (double Longitude, double Latitude, double? ElevationMeters) ToTuple() => (Longitude, Latitude, ElevationMeters);
}

/// <summary>
/// Closed linear ring that composes a polygon exterior or hole.
/// </summary>
public sealed class ZoneLinearRing
{
    private readonly IReadOnlyList<ZoneCoordinate> _vertices;

    /// <summary>
    /// Initializes a new instance of the <see cref="ZoneLinearRing"/> class.
    /// </summary>
    /// <param name="vertices">Ordered vertices that compose the ring.</param>
    public ZoneLinearRing(IReadOnlyList<ZoneCoordinate> vertices)
    {
        if (vertices is null)
        {
            throw new ArgumentNullException(nameof(vertices));
        }

        if (vertices.Count < 4)
        {
            throw new ArgumentException("Linear ring must contain at least four coordinates.", nameof(vertices));
        }

        _vertices = new ReadOnlyCollection<ZoneCoordinate>(vertices.ToArray());
    }

    /// <summary>
    /// Gets the ordered vertices that compose the ring.
    /// </summary>
    public IReadOnlyList<ZoneCoordinate> Vertices => _vertices;
}

/// <summary>
/// Polygon geometry assigned to a zone.
/// </summary>
public sealed class ZonePolygon
{
    private readonly IReadOnlyList<ZoneLinearRing> _holes;

    /// <summary>
    /// Initializes a new instance of the <see cref="ZonePolygon"/> class.
    /// </summary>
    /// <param name="exterior">Exterior ring of the polygon.</param>
    /// <param name="holes">Optional interior holes to exclude.</param>
    public ZonePolygon(ZoneLinearRing exterior, IReadOnlyList<ZoneLinearRing>? holes = null)
    {
        Exterior = exterior ?? throw new ArgumentNullException(nameof(exterior));

        if (holes is null || holes.Count == 0)
        {
            _holes = Array.Empty<ZoneLinearRing>();
            return;
        }

        var projected = new List<ZoneLinearRing>(holes.Count);
        foreach (var ring in holes)
        {
            if (ring is null)
            {
                throw new ArgumentException("Hole ring cannot be null.", nameof(holes));
            }

            projected.Add(ring);
        }

        _holes = new ReadOnlyCollection<ZoneLinearRing>(projected);
    }

    /// <summary>
    /// Gets the exterior ring.
    /// </summary>
    public ZoneLinearRing Exterior { get; }

    /// <summary>
    /// Gets the optional hole rings.
    /// </summary>
    public IReadOnlyList<ZoneLinearRing> Holes => _holes;
}

/// <summary>
/// Drive vs. work clearance buffers for a zone (meters).
/// </summary>
public sealed class ZoneBuffers
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ZoneBuffers"/> class.
    /// </summary>
    /// <param name="driveMeters">Drive clearance in meters.</param>
    /// <param name="workMeters">Work clearance in meters.</param>
    public ZoneBuffers(double driveMeters, double workMeters)
    {
        if (driveMeters < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(driveMeters), driveMeters, "Drive buffer must be non-negative.");
        }

        if (workMeters < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(workMeters), workMeters, "Work buffer must be non-negative.");
        }

        DriveMeters = driveMeters;
        WorkMeters = workMeters;
    }

    /// <summary>
    /// Gets the drive clearance buffer (meters).
    /// </summary>
    public double DriveMeters { get; }

    /// <summary>
    /// Gets the work clearance buffer (meters).
    /// </summary>
    public double WorkMeters { get; }
}

/// <summary>
/// Optional metadata describing when a zone applies.
/// </summary>
public sealed class ZoneValidWhen
{
    private readonly IReadOnlyList<string> _conditions;

    /// <summary>
    /// Initializes a new instance of the <see cref="ZoneValidWhen"/> class.
    /// </summary>
    /// <param name="crop">Crop identifier when the zone is active.</param>
    /// <param name="season">Season identifier when the zone is active.</param>
    /// <param name="conditions">Additional arbitrary conditions.</param>
    public ZoneValidWhen(string? crop = null, string? season = null, IReadOnlyList<string>? conditions = null)
    {
        Crop = crop?.Trim();
        Season = season?.Trim();

        if (conditions is null || conditions.Count == 0)
        {
            _conditions = Array.Empty<string>();
            return;
        }

        var projected = new List<string>(conditions.Count);
        foreach (var condition in conditions)
        {
            if (string.IsNullOrWhiteSpace(condition))
            {
                throw new ArgumentException("Conditions cannot contain null or whitespace values.", nameof(conditions));
            }

            projected.Add(condition.Trim());
        }

        _conditions = new ReadOnlyCollection<string>(projected);
    }

    /// <summary>
    /// Gets the crop identifier when the zone applies.
    /// </summary>
    public string? Crop { get; }

    /// <summary>
    /// Gets the season identifier when the zone applies.
    /// </summary>
    public string? Season { get; }

    /// <summary>
    /// Gets the optional arbitrary conditions.
    /// </summary>
    public IReadOnlyList<string> Conditions => _conditions;
}

/// <summary>
/// Provenance metadata capturing the author and rationale for a zone.
/// </summary>
public sealed class ZoneProvenance
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ZoneProvenance"/> class.
    /// </summary>
    /// <param name="source">Source that authored the zone.</param>
    /// <param name="timestamp">Timestamp when the zone was created or updated.</param>
    /// <param name="note">Optional note describing the change rationale.</param>
    public ZoneProvenance(string? source, DateTimeOffset? timestamp, string? note = null)
    {
        Source = string.IsNullOrWhiteSpace(source) ? null : source.Trim();
        Timestamp = timestamp;
        Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
    }

    /// <summary>
    /// Gets the provenance source identifier.
    /// </summary>
    public string? Source { get; }

    /// <summary>
    /// Gets the timestamp describing when the zone was created or updated.
    /// </summary>
    public DateTimeOffset? Timestamp { get; }

    /// <summary>
    /// Gets the optional human-readable note.
    /// </summary>
    public string? Note { get; }
}

/// <summary>
/// Immutable zone definition persisted in the store.
/// </summary>
public sealed class ZoneDefinition
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ZoneDefinition"/> class.
    /// </summary>
    /// <param name="zoneId">Stable identifier for the zone.</param>
    /// <param name="type">Zone classification.</param>
    /// <param name="label">Human readable label.</param>
    /// <param name="priority">Priority ordering when zones overlap.</param>
    /// <param name="enabled">Whether the zone is enabled.</param>
    /// <param name="geometry">Buffered polygon geometry.</param>
    /// <param name="buffers">Drive/work clearance buffers.</param>
    /// <param name="validWhen">Optional applicability metadata.</param>
    /// <param name="provenance">Optional provenance metadata.</param>
    public ZoneDefinition(
        string zoneId,
        ZoneType type,
        string label,
        uint priority,
        bool enabled,
        ZonePolygon geometry,
        ZoneBuffers buffers,
        ZoneValidWhen? validWhen = null,
        ZoneProvenance? provenance = null)
    {
        if (string.IsNullOrWhiteSpace(zoneId))
        {
            throw new ArgumentException("Zone identifier is required.", nameof(zoneId));
        }

        if (string.IsNullOrWhiteSpace(label))
        {
            throw new ArgumentException("Zone label is required.", nameof(label));
        }

        ZoneId = zoneId.Trim();
        Type = type;
        Label = label.Trim();
        Priority = priority;
        Enabled = enabled;
        Geometry = geometry ?? throw new ArgumentNullException(nameof(geometry));
        Buffers = buffers ?? throw new ArgumentNullException(nameof(buffers));
        ValidWhen = validWhen;
        Provenance = provenance;
    }

    /// <summary>
    /// Gets the stable identifier for the zone.
    /// </summary>
    public string ZoneId { get; }

    /// <summary>
    /// Gets the zone classification.
    /// </summary>
    public ZoneType Type { get; }

    /// <summary>
    /// Gets the human readable label for the zone.
    /// </summary>
    public string Label { get; }

    /// <summary>
    /// Gets the priority used when overlapping zones are evaluated.
    /// </summary>
    public uint Priority { get; }

    /// <summary>
    /// Gets a value indicating whether the zone is enabled.
    /// </summary>
    public bool Enabled { get; }

    /// <summary>
    /// Gets the buffered polygon geometry assigned to the zone.
    /// </summary>
    public ZonePolygon Geometry { get; }

    /// <summary>
    /// Gets the drive/work clearance buffers.
    /// </summary>
    public ZoneBuffers Buffers { get; }

    /// <summary>
    /// Gets the optional applicability metadata.
    /// </summary>
    public ZoneValidWhen? ValidWhen { get; }

    /// <summary>
    /// Gets the optional provenance metadata.
    /// </summary>
    public ZoneProvenance? Provenance { get; }
}

/// <summary>
/// Axis-aligned bounding box used when filtering zones.
/// </summary>
public readonly struct ZoneBoundingBox
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ZoneBoundingBox"/> struct.
    /// </summary>
    /// <param name="minLongitude">Minimum longitude (or X) bound.</param>
    /// <param name="minLatitude">Minimum latitude (or Y) bound.</param>
    /// <param name="maxLongitude">Maximum longitude (or X) bound.</param>
    /// <param name="maxLatitude">Maximum latitude (or Y) bound.</param>
    public ZoneBoundingBox(double minLongitude, double minLatitude, double maxLongitude, double maxLatitude)
    {
        if (maxLongitude < minLongitude)
        {
            throw new ArgumentException("Maximum longitude must be greater than or equal to the minimum longitude.", nameof(maxLongitude));
        }

        if (maxLatitude < minLatitude)
        {
            throw new ArgumentException("Maximum latitude must be greater than or equal to the minimum latitude.", nameof(maxLatitude));
        }

        MinLongitude = minLongitude;
        MinLatitude = minLatitude;
        MaxLongitude = maxLongitude;
        MaxLatitude = maxLatitude;
    }

    /// <summary>
    /// Gets the minimum longitude (or X) bound.
    /// </summary>
    public double MinLongitude { get; }

    /// <summary>
    /// Gets the minimum latitude (or Y) bound.
    /// </summary>
    public double MinLatitude { get; }

    /// <summary>
    /// Gets the maximum longitude (or X) bound.
    /// </summary>
    public double MaxLongitude { get; }

    /// <summary>
    /// Gets the maximum latitude (or Y) bound.
    /// </summary>
    public double MaxLatitude { get; }
}
