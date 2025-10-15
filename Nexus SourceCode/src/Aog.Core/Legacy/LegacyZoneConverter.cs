using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Aog.Core.Zones;

namespace Aog.Core.Legacy;

/// <summary>
/// Converts legacy field geometry into <see cref="ZoneDefinition"/> instances.
/// </summary>
public sealed class LegacyZoneConverter
{
    /// <summary>
    /// Converts the supplied legacy field data into zone definitions using the provided options.
    /// </summary>
    public IReadOnlyList<ZoneDefinition> Convert(LegacyFieldData field, LegacyZoneImportOptions options)
    {
        if (field is null)
        {
            throw new ArgumentNullException(nameof(field));
        }

        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        options.Validate();

        var provenanceNote = options.Note ?? field.Overview?.Notes;
        var provenance = new ZoneProvenance(options.Source, field.Overview?.CreatedAtUtc, provenanceNote);
        var fieldLabel = !string.IsNullOrWhiteSpace(options.FieldName)
            ? options.FieldName!.Trim()
            : string.IsNullOrWhiteSpace(field.Overview?.FieldName)
                ? options.FieldId
                : field.Overview!.FieldName!;

        var zones = new List<ZoneDefinition>();
        for (var index = 0; index < field.Boundaries.Count; index++)
        {
            var boundary = field.Boundaries[index];
            if (boundary.Perimeter.Count < 3)
            {
                continue;
            }

            var perimeter = BuildRing(boundary.Perimeter);
            var polygon = new ZonePolygon(perimeter);
            var buffers = new ZoneBuffers(options.BoundaryDriveBufferMeters, options.BoundaryWorkBufferMeters);
            var zoneId = $"{options.FieldId}:boundary:{index + 1:D2}";
            var label = $"{fieldLabel} Boundary {index + 1}";

            zones.Add(new ZoneDefinition(
                zoneId,
                ZoneType.Boundary,
                label,
                priority: 10,
                enabled: true,
                polygon,
                buffers,
                provenance: provenance));

            for (var headlandIndex = 0; headlandIndex < boundary.Headlands.Count; headlandIndex++)
            {
                var headland = boundary.Headlands[headlandIndex];
                if (headland.Vertices.Count < 3)
                {
                    continue;
                }

                var headlandRing = BuildRing(headland.Vertices);
                var headlandPolygon = new ZonePolygon(headlandRing);
                var headlandBuffers = new ZoneBuffers(options.HeadlandDriveBufferMeters, options.HeadlandWorkBufferMeters);
                var headlandId = $"{options.FieldId}:headland:{index + 1:D2}:{headlandIndex + 1:D2}";
                var headlandLabel = $"{fieldLabel} Headland {index + 1}-{headlandIndex + 1}";

                zones.Add(new ZoneDefinition(
                    headlandId,
                    ZoneType.Headland,
                    headlandLabel,
                    priority: 20,
                    enabled: true,
                    headlandPolygon,
                    headlandBuffers,
                    provenance: provenance));
            }
        }

        return new ReadOnlyCollection<ZoneDefinition>(zones);
    }

    private static ZoneLinearRing BuildRing(IReadOnlyList<BoundaryVertex> vertices)
    {
        if (vertices.Count < 3)
        {
            throw new ArgumentException("Polygon requires at least three vertices.", nameof(vertices));
        }

        var coordinates = new List<ZoneCoordinate>(vertices.Count + 1);
        foreach (var vertex in vertices)
        {
            coordinates.Add(new ZoneCoordinate(vertex.Point.Easting, vertex.Point.Northing));
        }

        if (!IsClose(coordinates[0], coordinates[^1]))
        {
            coordinates.Add(coordinates[0]);
        }

        if (coordinates.Count < 4)
        {
            coordinates.Add(coordinates[^1]);
        }

        return new ZoneLinearRing(coordinates);
    }

    private static bool IsClose(ZoneCoordinate left, ZoneCoordinate right)
    {
        return Math.Abs(left.Longitude - right.Longitude) < 1e-6
            && Math.Abs(left.Latitude - right.Latitude) < 1e-6;
    }
}

/// <summary>
/// Options controlling how legacy geometry is mapped into <see cref="ZoneDefinition"/> instances.
/// </summary>
public sealed class LegacyZoneImportOptions
{
    /// <summary>
    /// Gets or sets the field identifier used to namespace zone identifiers.
    /// </summary>
    public string FieldId { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets an optional field name override for zone labels.
    /// </summary>
    public string? FieldName { get; init; }

    /// <summary>
    /// Gets or sets the provenance source recorded on imported zones.
    /// </summary>
    public string Source { get; init; } = "legacy.v6";

    /// <summary>
    /// Gets or sets an optional provenance note describing the import.
    /// </summary>
    public string? Note { get; init; }

    /// <summary>
    /// Gets or sets the drive buffer assigned to boundary zones.
    /// </summary>
    public double BoundaryDriveBufferMeters { get; init; }

    /// <summary>
    /// Gets or sets the work buffer assigned to boundary zones.
    /// </summary>
    public double BoundaryWorkBufferMeters { get; init; }

    /// <summary>
    /// Gets or sets the drive buffer assigned to headland zones.
    /// </summary>
    public double HeadlandDriveBufferMeters { get; init; }

    /// <summary>
    /// Gets or sets the work buffer assigned to headland zones.
    /// </summary>
    public double HeadlandWorkBufferMeters { get; init; }

    /// <summary>
    /// Validates the option values.
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(FieldId))
        {
            throw new ArgumentException("Field identifier is required.", nameof(FieldId));
        }

        if (BoundaryDriveBufferMeters < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(BoundaryDriveBufferMeters));
        }

        if (BoundaryWorkBufferMeters < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(BoundaryWorkBufferMeters));
        }

        if (HeadlandDriveBufferMeters < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(HeadlandDriveBufferMeters));
        }

        if (HeadlandWorkBufferMeters < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(HeadlandWorkBufferMeters));
        }

        if (Source is null)
        {
            throw new ArgumentNullException(nameof(Source));
        }
    }
}
