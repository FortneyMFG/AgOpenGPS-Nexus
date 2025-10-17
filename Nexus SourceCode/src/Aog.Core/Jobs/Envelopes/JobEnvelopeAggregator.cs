using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using NetTopologySuite.Operation.Union;

namespace Aog.Core.Jobs.Envelopes;

/// <summary>
/// Aggregates field polygons into a multi-field job envelope as described in ADR-043.
/// </summary>
public sealed class JobEnvelopeAggregator
{
    /// <summary>
    /// Aggregates the supplied field polygons into a union envelope.
    /// </summary>
    /// <param name="crsEpsg">EPSG code describing the coordinate reference system of the polygons.</param>
    /// <param name="fields">Field contributions that participate in the job.</param>
    /// <returns>Aggregated envelope with per-field members.</returns>
    public JobEnvelopeAggregate Aggregate(int crsEpsg, IReadOnlyList<JobEnvelopeFieldContribution> fields)
    {
        if (fields is null)
        {
            throw new ArgumentNullException(nameof(fields));
        }

        if (fields.Count == 0)
        {
            throw new ArgumentException("At least one field contribution is required.", nameof(fields));
        }

        var geometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: crsEpsg);

        var memberSnapshots = new List<JobEnvelopeMember>(fields.Count);
        var fieldGeometries = new List<Geometry>(fields.Count);

        foreach (var field in fields)
        {
            if (field is null)
            {
                throw new ArgumentException("Field contribution cannot be null.", nameof(fields));
            }

            if (string.IsNullOrWhiteSpace(field.FieldId))
            {
                throw new ArgumentException("Field identifier must be provided.", nameof(fields));
            }

            if (field.Polygons is null || field.Polygons.Count == 0)
            {
                throw new ArgumentException(
                    $"Field '{field.FieldId}' must include at least one polygon.",
                    nameof(fields));
            }

            var geometries = new List<Geometry>(field.Polygons.Count);
            foreach (var polygon in field.Polygons)
            {
                if (polygon is null)
                {
                    throw new ArgumentException(
                        $"Field '{field.FieldId}' includes a null polygon contribution.",
                        nameof(fields));
                }

                geometries.Add(ToGeometry(polygon, geometryFactory));
            }

            var fieldGeometry = UnaryUnionOp.Union(geometries);
            if (fieldGeometry.IsEmpty)
            {
                throw new InvalidOperationException(
                    string.Format(CultureInfo.InvariantCulture,
                        "Field '{0}' produced an empty geometry after union.",
                        field.FieldId));
            }

            var normalized = NormalizeGeometry(fieldGeometry);
            fieldGeometries.Add(normalized);

            var memberPolygonList = FlattenPolygons(normalized)
                .Select(FromGeometry)
                .ToList();

            var memberPolygons = new ReadOnlyCollection<JobEnvelopePolygon>(memberPolygonList);

            var areaHectares = normalized.Area / 10_000d;

            var extensions = field.Extensions is null
                ? null
                : new ReadOnlyDictionary<string, object?>(
                    new Dictionary<string, object?>(field.Extensions, StringComparer.OrdinalIgnoreCase));

            memberSnapshots.Add(new JobEnvelopeMember(
                field.FieldId.Trim(),
                memberPolygons,
                areaHectares,
                extensions));
        }

        var aggregateGeometry = UnaryUnionOp.Union(fieldGeometries);
        if (aggregateGeometry.IsEmpty)
        {
            throw new InvalidOperationException("Aggregate geometry is empty after union.");
        }

        var normalizedAggregate = NormalizeGeometry(aggregateGeometry);

        var aggregatePolygonList = FlattenPolygons(normalizedAggregate)
            .Select(FromGeometry)
            .ToList();

        var aggregatePolygons = new ReadOnlyCollection<JobEnvelopePolygon>(aggregatePolygonList);

        var area = normalizedAggregate.Area / 10_000d;
        var envelope = normalizedAggregate.EnvelopeInternal;
        var centroid = normalizedAggregate.Centroid;

        var boundingBox = new JobEnvelopeBoundingBox(
            MinLongitude: envelope.MinX,
            MinLatitude: envelope.MinY,
            MaxLongitude: envelope.MaxX,
            MaxLatitude: envelope.MaxY);

        var centroidCoordinate = new JobEnvelopeCoordinate(centroid.X, centroid.Y);

        var orderedMembersList = memberSnapshots
            .OrderBy(m => m.FieldId, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var orderedMembers = new ReadOnlyCollection<JobEnvelopeMember>(orderedMembersList);

        return new JobEnvelopeAggregate(
            aggregatePolygons,
            orderedMembers,
            boundingBox,
            centroidCoordinate,
            area,
            crsEpsg);
    }

    private static Geometry NormalizeGeometry(Geometry geometry)
    {
        if (geometry is null)
        {
            throw new ArgumentNullException(nameof(geometry));
        }

        var clone = geometry.Copy();
        clone.Normalize();
        return clone;
    }

    private static Geometry ToGeometry(JobEnvelopePolygon polygon, GeometryFactory factory)
    {
        var shell = factory.CreateLinearRing(ToCoordinateArray(polygon.Exterior));

        var holes = polygon.Holes
            .Select(ring => factory.CreateLinearRing(ToCoordinateArray(ring)))
            .ToArray();

        Geometry geometry = factory.CreatePolygon(shell, holes);
        if (!geometry.IsValid)
        {
            geometry = geometry.Buffer(0);
        }

        return geometry;
    }

    private static Coordinate[] ToCoordinateArray(IReadOnlyList<JobEnvelopeCoordinate> ring)
    {
        if (ring is null)
        {
            throw new ArgumentNullException(nameof(ring));
        }

        if (ring.Count < 4)
        {
            throw new ArgumentException("Ring must contain at least four coordinates.", nameof(ring));
        }

        var needsClosure = !ring[0].Equals(ring[^1]);
        var coordinates = new Coordinate[ring.Count + (needsClosure ? 1 : 0)];

        for (var i = 0; i < ring.Count; i++)
        {
            var coordinate = ring[i];
            coordinates[i] = new Coordinate(coordinate.Longitude, coordinate.Latitude);
        }

        if (needsClosure)
        {
            coordinates[^1] = coordinates[0].Copy();
        }

        return coordinates;
    }

    private static IEnumerable<Polygon> FlattenPolygons(Geometry geometry)
    {
        switch (geometry)
        {
            case Polygon polygon:
                yield return polygon;
                yield break;
            case MultiPolygon multiPolygon:
                for (var i = 0; i < multiPolygon.NumGeometries; i++)
                {
                    if (multiPolygon.GetGeometryN(i) is Polygon child)
                    {
                        foreach (var nested in FlattenPolygons(child))
                        {
                            yield return nested;
                        }
                    }
                }

                yield break;
            case GeometryCollection collection:
                for (var i = 0; i < collection.NumGeometries; i++)
                {
                    foreach (var nested in FlattenPolygons(collection.GetGeometryN(i)))
                    {
                        yield return nested;
                    }
                }

                yield break;
            default:
                throw new InvalidOperationException(
                    $"Unsupported geometry type '{geometry.GeometryType}' in envelope aggregation.");
        }
    }

    private static JobEnvelopePolygon FromGeometry(Polygon polygon)
    {
        if (polygon is null)
        {
            throw new ArgumentNullException(nameof(polygon));
        }

        var shell = FromRing(polygon.ExteriorRing);

        var holes = new List<IReadOnlyList<JobEnvelopeCoordinate>>(polygon.NumInteriorRings);
        for (var i = 0; i < polygon.NumInteriorRings; i++)
        {
            holes.Add(FromRing(polygon.GetInteriorRingN(i)));
        }

        return new JobEnvelopePolygon(shell, holes);
    }

    private static IReadOnlyList<JobEnvelopeCoordinate> FromRing(LineString ring)
    {
        var sequence = ring.CoordinateSequence;
        var coordinates = new JobEnvelopeCoordinate[sequence.Count];
        for (var i = 0; i < sequence.Count; i++)
        {
            coordinates[i] = new JobEnvelopeCoordinate(sequence.GetX(i), sequence.GetY(i));
        }

        return new ReadOnlyCollection<JobEnvelopeCoordinate>(coordinates);
    }
}
