using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json.Nodes;
using Aog.Core.Layers;

namespace Aog.Plugins.Crop;

/// <summary>
/// Immutable snapshot of the crop layer state maintained by <see cref="CropLayerIngestionPipeline"/>.
/// </summary>
public sealed class CropLayerSnapshot
{
    internal CropLayerSnapshot(
        string layerId,
        string? jobId,
        string? sessionId,
        LayerEditEventContext? context,
        DateTimeOffset lastUpdated,
        IReadOnlyList<CropZoneFeature> features)
    {
        LayerId = layerId ?? throw new ArgumentNullException(nameof(layerId));
        JobId = jobId;
        SessionId = sessionId;
        UpdatedAt = lastUpdated;
        Context = context is null
            ? null
            : new LayerEditEventContext(context.FarmId, context.FieldIds.ToArray(), context.SeasonId);
        Features = new ReadOnlyCollection<CropZoneFeature>((features ?? Array.Empty<CropZoneFeature>()).Select(feature => feature.Clone()).ToArray());
    }

    /// <summary>
    /// Gets the layer identifier.
    /// </summary>
    public string LayerId { get; }

    /// <summary>
    /// Gets the job identifier associated with this layer, when known.
    /// </summary>
    public string? JobId { get; }

    /// <summary>
    /// Gets the session identifier associated with the latest edit, when known.
    /// </summary>
    public string? SessionId { get; }

    /// <summary>
    /// Gets the farm/field context of the layer.
    /// </summary>
    public LayerEditEventContext? Context { get; }

    /// <summary>
    /// Gets the timestamp of the most recent journal entry applied to this snapshot.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; }

    /// <summary>
    /// Gets the ordered set of crop zone features.
    /// </summary>
    public IReadOnlyList<CropZoneFeature> Features { get; }

    /// <summary>
    /// Gets a value indicating whether the snapshot contains any features.
    /// </summary>
    public bool IsEmpty => Features.Count == 0;
}

/// <summary>
/// Describes a single crop zone feature with geometry and semantic attributes.
/// </summary>
public sealed class CropZoneFeature
{
    internal CropZoneFeature(
        string featureId,
        string crop,
        int year,
        string status,
        JsonNode geometry,
        double? areaSqMeters,
        string? variety,
        string? source,
        string? notes,
        DateTimeOffset updatedAt,
        string updatedBy)
    {
        if (string.IsNullOrWhiteSpace(featureId))
        {
            throw new ArgumentException("Feature identifier is required.", nameof(featureId));
        }

        if (string.IsNullOrWhiteSpace(crop))
        {
            throw new ArgumentException("Crop name is required.", nameof(crop));
        }

        if (year is < 1900 or > 2100)
        {
            throw new ArgumentOutOfRangeException(nameof(year), year, "Year must be between 1900 and 2100.");
        }

        if (string.IsNullOrWhiteSpace(status))
        {
            throw new ArgumentException("Status is required.", nameof(status));
        }

        FeatureId = featureId;
        Crop = crop;
        Year = year;
        Status = status;
        AreaSqMeters = areaSqMeters;
        Variety = variety;
        Source = source;
        Notes = notes;
        UpdatedAt = updatedAt;
        UpdatedBy = string.IsNullOrWhiteSpace(updatedBy) ? "system:nexus" : updatedBy;
        GeometryNode = geometry?.DeepClone() ?? throw new ArgumentNullException(nameof(geometry));
    }

    /// <summary>
    /// Gets the feature identifier.
    /// </summary>
    public string FeatureId { get; }

    /// <summary>
    /// Gets the crop name associated with the feature.
    /// </summary>
    public string Crop { get; }

    /// <summary>
    /// Gets the crop year.
    /// </summary>
    public int Year { get; }

    /// <summary>
    /// Gets the lifecycle status (planned, actual, historical).
    /// </summary>
    public string Status { get; }

    /// <summary>
    /// Gets the optional crop variety or hybrid.
    /// </summary>
    public string? Variety { get; }

    /// <summary>
    /// Gets the optional source metadata.
    /// </summary>
    public string? Source { get; }

    /// <summary>
    /// Gets free-form notes associated with the feature.
    /// </summary>
    public string? Notes { get; }

    /// <summary>
    /// Gets the area tracked for the feature when available.
    /// </summary>
    public double? AreaSqMeters { get; }

    /// <summary>
    /// Gets the timestamp of the most recent update.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; }

    /// <summary>
    /// Gets the actor that produced the latest change.
    /// </summary>
    public string UpdatedBy { get; }

    /// <summary>
    /// Gets the GeoJSON geometry associated with the feature.
    /// </summary>
    public JsonNode Geometry => GeometryNode.DeepClone();

    internal JsonNode GeometryNode { get; }

    internal CropZoneFeature With(JsonNode geometry, CropLayerIngestionPipeline.CropAttributes attributes, double? area, DateTimeOffset updatedAt, string updatedBy)
    {
        return new CropZoneFeature(
            FeatureId,
            attributes.Crop,
            attributes.Year,
            attributes.Status,
            geometry,
            area,
            attributes.Variety,
            attributes.Source,
            attributes.Notes,
            updatedAt,
            updatedBy);
    }

    internal CropLayerIngestionPipeline.CropAttributes ToAttributes()
    {
        return new CropLayerIngestionPipeline.CropAttributes(Crop, Year, Status, Variety, Source, Notes);
    }

    internal CropZoneFeature Clone()
    {
        return new CropZoneFeature(
            FeatureId,
            Crop,
            Year,
            Status,
            GeometryNode.DeepClone(),
            AreaSqMeters,
            Variety,
            Source,
            Notes,
            UpdatedAt,
            UpdatedBy);
    }
}
