using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Aog.Core.Jobs.Envelopes;

/// <summary>
/// Represents a geographic coordinate expressed as longitude and latitude degrees.
/// </summary>
/// <param name="Longitude">Longitude component in degrees (or project CRS units).</param>
/// <param name="Latitude">Latitude component in degrees (or project CRS units).</param>
public readonly record struct JobEnvelopeCoordinate(double Longitude, double Latitude)
{
    /// <summary>
    /// Returns the coordinate as a tuple.
    /// </summary>
    public (double Longitude, double Latitude) ToTuple() => (Longitude, Latitude);
}

/// <summary>
/// Represents an axis-aligned bounding box that encloses the aggregated envelope.
/// </summary>
/// <param name="MinLongitude">Minimum longitude (or X) bound.</param>
/// <param name="MinLatitude">Minimum latitude (or Y) bound.</param>
/// <param name="MaxLongitude">Maximum longitude (or X) bound.</param>
/// <param name="MaxLatitude">Maximum latitude (or Y) bound.</param>
public sealed record JobEnvelopeBoundingBox(
    double MinLongitude,
    double MinLatitude,
    double MaxLongitude,
    double MaxLatitude);

/// <summary>
/// Immutable representation of a polygon used when constructing job envelopes.
/// </summary>
public sealed record JobEnvelopePolygon
{
    private readonly IReadOnlyList<JobEnvelopeCoordinate> _exterior;
    private readonly IReadOnlyList<IReadOnlyList<JobEnvelopeCoordinate>> _holes;

    public JobEnvelopePolygon(
        IReadOnlyList<JobEnvelopeCoordinate> exterior,
        IReadOnlyList<IReadOnlyList<JobEnvelopeCoordinate>>? holes = null)
    {
        if (exterior is null)
        {
            throw new ArgumentNullException(nameof(exterior));
        }

        if (exterior.Count < 4)
        {
            throw new ArgumentException("Exterior ring must contain at least four coordinates.", nameof(exterior));
        }

        _exterior = new ReadOnlyCollection<JobEnvelopeCoordinate>(exterior.ToArray());

        if (holes is null || holes.Count == 0)
        {
            _holes = Array.Empty<IReadOnlyList<JobEnvelopeCoordinate>>();
            return;
        }

        var projected = new List<IReadOnlyList<JobEnvelopeCoordinate>>(holes.Count);
        foreach (var ring in holes)
        {
            if (ring is null)
            {
                throw new ArgumentException("Hole ring cannot be null.", nameof(holes));
            }

            if (ring.Count < 4)
            {
                throw new ArgumentException("Hole ring must contain at least four coordinates.", nameof(holes));
            }

            projected.Add(new ReadOnlyCollection<JobEnvelopeCoordinate>(ring.ToArray()));
        }

        _holes = projected;
    }

    /// <summary>
    /// Gets the exterior ring of the polygon.
    /// </summary>
    public IReadOnlyList<JobEnvelopeCoordinate> Exterior => _exterior;

    /// <summary>
    /// Gets the optional hole rings of the polygon.
    /// </summary>
    public IReadOnlyList<IReadOnlyList<JobEnvelopeCoordinate>> Holes => _holes;
}

/// <summary>
/// Represents a per-field contribution included in an aggregated job envelope.
/// </summary>
public sealed record JobEnvelopeFieldContribution(
    string FieldId,
    IReadOnlyList<JobEnvelopePolygon> Polygons,
    IReadOnlyDictionary<string, object?>? Extensions = null);

/// <summary>
/// Aggregated job envelope produced from multiple field contributions.
/// </summary>
public sealed record JobEnvelopeAggregate(
    IReadOnlyList<JobEnvelopePolygon> Polygons,
    IReadOnlyList<JobEnvelopeMember> Members,
    JobEnvelopeBoundingBox BoundingBox,
    JobEnvelopeCoordinate Centroid,
    double AreaHectares,
    int CrsEpsg);

/// <summary>
/// Per-field metadata included in the aggregated job envelope.
/// </summary>
public sealed record JobEnvelopeMember(
    string FieldId,
    IReadOnlyList<JobEnvelopePolygon> Polygons,
    double AreaHectares,
    IReadOnlyDictionary<string, object?>? Extensions);

