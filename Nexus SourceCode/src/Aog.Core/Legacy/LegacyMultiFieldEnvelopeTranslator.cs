using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Aog.Core.Jobs.Envelopes;
using Aog.Core.Paths;

namespace Aog.Core.Legacy;

/// <summary>
/// Translates legacy V6 field boundaries into multi-field job envelopes (ADR-043).
/// </summary>
public sealed class LegacyMultiFieldEnvelopeTranslator
{
    private const double EarthRadiusMeters = 6_378_137.0;
    private const double MaxWebMercatorLatitude = 85.05112878;

    /// <summary>
    /// Converts the supplied legacy field definitions into a job envelope aggregate expressed in WGS84.
    /// </summary>
    /// <param name="fields">Collection of legacy field envelope sources to include in the multi-field job.</param>
    /// <returns>Aggregated job envelope expressed in EPSG:4326.</returns>
    public JobEnvelopeAggregate Translate(IReadOnlyList<LegacyFieldEnvelopeSource> fields)
    {
        if (fields is null)
        {
            throw new ArgumentNullException(nameof(fields));
        }

        if (fields.Count == 0)
        {
            throw new ArgumentException("At least one field is required to translate an envelope.", nameof(fields));
        }

        var contributions = new List<JobEnvelopeFieldContribution>(fields.Count);

        foreach (var field in fields)
        {
            if (field is null)
            {
                throw new ArgumentException("Field collection cannot contain null entries.", nameof(fields));
            }

            if (string.IsNullOrWhiteSpace(field.FieldId))
            {
                throw new ArgumentException("Field identifier must be provided.", nameof(fields));
            }

            if (field.Boundaries is null || field.Boundaries.Count == 0)
            {
                throw new InvalidOperationException($"Field '{field.FieldId}' does not contain any boundaries.");
            }

            var polygons = new List<JobEnvelopePolygon>();
            foreach (var boundary in field.Boundaries)
            {
                if (boundary is null)
                {
                    throw new InvalidOperationException($"Field '{field.FieldId}' contains a null boundary definition.");
                }

                var exterior = ToMercatorRing(boundary.Perimeter, field.Origin);
                var holes = boundary.Headlands
                    .Select(headland => ToMercatorRing(headland.Vertices, field.Origin))
                    .Where(ring => ring.Count >= 4)
                    .ToList();

                polygons.Add(new JobEnvelopePolygon(exterior, holes));
            }

            if (polygons.Count == 0)
            {
                throw new InvalidOperationException($"Field '{field.FieldId}' did not produce any polygon geometry.");
            }

            IReadOnlyDictionary<string, object?>? extensions = null;
            if (field.Extensions is not null)
            {
                extensions = new ReadOnlyDictionary<string, object?>(new Dictionary<string, object?>(field.Extensions, StringComparer.OrdinalIgnoreCase));
            }

            contributions.Add(new JobEnvelopeFieldContribution(field.FieldId.Trim(), polygons, extensions));
        }

        var aggregator = new JobEnvelopeAggregator();
        var aggregateMercator = aggregator.Aggregate(3857, contributions);

        return ConvertAggregateToGeographic(aggregateMercator);
    }

    private static IReadOnlyList<JobEnvelopeCoordinate> ToMercatorRing(
        IReadOnlyList<BoundaryVertex> vertices,
        GeographicCoordinate origin)
    {
        if (vertices is null)
        {
            throw new ArgumentNullException(nameof(vertices));
        }

        if (vertices.Count < 3)
        {
            throw new InvalidOperationException("Boundary rings must contain at least three vertices.");
        }

        var coordinates = new List<JobEnvelopeCoordinate>(vertices.Count + 1);
        foreach (var vertex in vertices)
        {
            var geographic = ToGeographic(vertex.Point, origin);
            var mercator = ToWebMercator(geographic);
            coordinates.Add(new JobEnvelopeCoordinate(mercator.X, mercator.Y));
        }

        if (!coordinates[0].Equals(coordinates[^1]))
        {
            coordinates.Add(coordinates[0]);
        }

        if (coordinates.Count < 4)
        {
            throw new InvalidOperationException("Boundary rings must contain at least four coordinates after closure.");
        }

        return new ReadOnlyCollection<JobEnvelopeCoordinate>(coordinates);
    }

    private static GeographicCoordinate ToGeographic(PlanarPoint planar, GeographicCoordinate origin)
    {
        var originLatRad = DegreesToRadians(origin.LatitudeDeg);
        var originLonRad = DegreesToRadians(origin.LongitudeDeg);

        var latRad = (planar.Northing / EarthRadiusMeters) + originLatRad;
        var cosFactor = Math.Cos((latRad + originLatRad) / 2.0);
        if (Math.Abs(cosFactor) < 1e-12)
        {
            throw new InvalidOperationException("Unable to convert planar coordinates near the poles.");
        }

        var lonRad = (planar.Easting / (cosFactor * EarthRadiusMeters)) + originLonRad;

        return new GeographicCoordinate(RadiansToDegrees(latRad), RadiansToDegrees(lonRad));
    }

    private static (double X, double Y) ToWebMercator(GeographicCoordinate coordinate)
    {
        var clampedLat = Math.Clamp(coordinate.LatitudeDeg, -MaxWebMercatorLatitude, MaxWebMercatorLatitude);
        var latRad = DegreesToRadians(clampedLat);
        var lonRad = DegreesToRadians(coordinate.LongitudeDeg);

        var x = EarthRadiusMeters * lonRad;
        var y = EarthRadiusMeters * Math.Log(Math.Tan((Math.PI / 4.0) + (latRad / 2.0)));
        return (x, y);
    }

    private static JobEnvelopeAggregate ConvertAggregateToGeographic(JobEnvelopeAggregate aggregate)
    {
        var polygons = aggregate.Polygons
            .Select(ConvertPolygonToGeographic)
            .ToList();

        var members = aggregate.Members
            .Select(member => new JobEnvelopeMember(
                member.FieldId,
                member.Polygons.Select(ConvertPolygonToGeographic).ToList(),
                member.AreaHectares,
                member.Extensions))
            .ToList();

        var boundingBox = ConvertBoundingBox(aggregate.BoundingBox);
        var centroidGeo = FromWebMercator(aggregate.Centroid);
        var centroid = new JobEnvelopeCoordinate(centroidGeo.LongitudeDeg, centroidGeo.LatitudeDeg);

        return new JobEnvelopeAggregate(
            polygons,
            members,
            boundingBox,
            centroid,
            aggregate.AreaHectares,
            4326);
    }

    private static JobEnvelopePolygon ConvertPolygonToGeographic(JobEnvelopePolygon polygon)
    {
        var exterior = ConvertRingToGeographic(polygon.Exterior);
        var holes = polygon.Holes
            .Select(ConvertRingToGeographic)
            .ToList();

        return new JobEnvelopePolygon(exterior, holes);
    }

    private static IReadOnlyList<JobEnvelopeCoordinate> ConvertRingToGeographic(
        IReadOnlyList<JobEnvelopeCoordinate> ring)
    {
        var converted = new List<JobEnvelopeCoordinate>(ring.Count);
        foreach (var coordinate in ring)
        {
            var geographic = FromWebMercator(coordinate);
            converted.Add(new JobEnvelopeCoordinate(geographic.LongitudeDeg, geographic.LatitudeDeg));
        }

        return new ReadOnlyCollection<JobEnvelopeCoordinate>(converted);
    }

    private static JobEnvelopeBoundingBox ConvertBoundingBox(JobEnvelopeBoundingBox boundingBox)
    {
        var minGeo = FromWebMercator(new JobEnvelopeCoordinate(boundingBox.MinLongitude, boundingBox.MinLatitude));
        var maxGeo = FromWebMercator(new JobEnvelopeCoordinate(boundingBox.MaxLongitude, boundingBox.MaxLatitude));

        return new JobEnvelopeBoundingBox(
            minGeo.LongitudeDeg,
            minGeo.LatitudeDeg,
            maxGeo.LongitudeDeg,
            maxGeo.LatitudeDeg);
    }

    private static GeographicCoordinate FromWebMercator(JobEnvelopeCoordinate coordinate)
    {
        var lonRad = coordinate.Longitude / EarthRadiusMeters;
        var latRad = (Math.PI / 2.0) - (2.0 * Math.Atan(Math.Exp(-coordinate.Latitude / EarthRadiusMeters)));

        var latitude = RadiansToDegrees(latRad);
        var longitude = RadiansToDegrees(lonRad);

        return new GeographicCoordinate(latitude, longitude);
    }

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180.0;

    private static double RadiansToDegrees(double radians) => radians * 180.0 / Math.PI;
}

/// <summary>
/// Captures the legacy data required to build a multi-field job envelope.
/// </summary>
/// <param name="FieldId">Identifier of the field (field:*).</param>
/// <param name="Origin">Geodetic origin used by the legacy planar coordinates.</param>
/// <param name="Boundaries">Legacy boundary polygons imported from V6 field assets.</param>
/// <param name="Extensions">Optional per-field metadata to surface on the job envelope.</param>
public sealed record LegacyFieldEnvelopeSource(
    string FieldId,
    GeographicCoordinate Origin,
    IReadOnlyList<FieldBoundary> Boundaries,
    IReadOnlyDictionary<string, object?>? Extensions = null);
