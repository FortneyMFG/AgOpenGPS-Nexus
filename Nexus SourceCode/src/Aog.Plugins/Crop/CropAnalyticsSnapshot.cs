using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json.Nodes;

namespace Aog.Plugins.Crop;

/// <summary>
/// Immutable analytics snapshot describing crop layer aggregates and history.
/// </summary>
public sealed class CropAnalyticsSnapshot
{
    internal CropAnalyticsSnapshot(
        DateTimeOffset generatedAt,
        IReadOnlyList<CropAcreageSummary> planned,
        IReadOnlyList<CropAcreageSummary> actual,
        IReadOnlyList<CropAcreageSummary> historical,
        IReadOnlyList<CropRotationSummary> rotations,
        IReadOnlyDictionary<string, CropHistoryRecord> latestByField,
        IReadOnlyList<CropHistoryRecord> historyByRecency)
    {
        GeneratedAt = generatedAt;
        Planned = new ReadOnlyCollection<CropAcreageSummary>((planned ?? Array.Empty<CropAcreageSummary>()).ToArray());
        Actual = new ReadOnlyCollection<CropAcreageSummary>((actual ?? Array.Empty<CropAcreageSummary>()).ToArray());
        Historical = new ReadOnlyCollection<CropAcreageSummary>((historical ?? Array.Empty<CropAcreageSummary>()).ToArray());
        Rotations = new ReadOnlyCollection<CropRotationSummary>((rotations ?? Array.Empty<CropRotationSummary>()).ToArray());

        var fieldLookup = new Dictionary<string, CropHistoryRecord>(StringComparer.OrdinalIgnoreCase);
        if (latestByField is not null)
        {
            foreach (var kvp in latestByField)
            {
                fieldLookup[kvp.Key] = kvp.Value.Clone();
            }
        }

        LatestByField = new ReadOnlyDictionary<string, CropHistoryRecord>(fieldLookup);
        HistoryByRecency = new ReadOnlyCollection<CropHistoryRecord>((historyByRecency ?? Array.Empty<CropHistoryRecord>())
            .Select(record => record.Clone())
            .ToArray());
    }

    /// <summary>
    /// Gets the timestamp when the snapshot was generated.
    /// </summary>
    public DateTimeOffset GeneratedAt { get; }

    /// <summary>
    /// Gets aggregated acreage summaries for planned crop layers.
    /// </summary>
    public IReadOnlyList<CropAcreageSummary> Planned { get; }

    /// <summary>
    /// Gets aggregated acreage summaries for actual crop layers.
    /// </summary>
    public IReadOnlyList<CropAcreageSummary> Actual { get; }

    /// <summary>
    /// Gets aggregated acreage summaries for historical crop layers.
    /// </summary>
    public IReadOnlyList<CropAcreageSummary> Historical { get; }

    /// <summary>
    /// Gets rotation statistics derived from historical crop layers.
    /// </summary>
    public IReadOnlyList<CropRotationSummary> Rotations { get; }

    /// <summary>
    /// Gets the most recent crop record for each field.
    /// </summary>
    public IReadOnlyDictionary<string, CropHistoryRecord> LatestByField { get; }

    /// <summary>
    /// Gets historical crop records ordered by recency.
    /// </summary>
    public IReadOnlyList<CropHistoryRecord> HistoryByRecency { get; }
}

/// <summary>
/// Aggregated acreage summary for a crop, status, and year combination.
/// </summary>
public sealed record CropAcreageSummary(
    string Crop,
    string Status,
    int Year,
    double AreaHectares,
    int FeatureCount);

/// <summary>
/// Describes a single historical crop record used for analytics and reporting.
/// </summary>
public sealed class CropHistoryRecord
{
    public CropHistoryRecord(
        string layerId,
        string featureId,
        string crop,
        string status,
        int year,
        string? variety,
        string? source,
        string? notes,
        double? areaHectares,
        string? jobId,
        string? sessionId,
        string? farmId,
        IReadOnlyList<string> fieldIds,
        string? seasonId,
        DateTimeOffset updatedAt,
        string updatedBy,
        JsonNode geometry)
    {
        LayerId = layerId ?? throw new ArgumentNullException(nameof(layerId));
        FeatureId = featureId ?? throw new ArgumentNullException(nameof(featureId));
        Crop = crop ?? throw new ArgumentNullException(nameof(crop));
        Status = status ?? throw new ArgumentNullException(nameof(status));
        Year = year;
        Variety = variety;
        Source = source;
        Notes = notes;
        AreaHectares = areaHectares;
        JobId = jobId;
        SessionId = sessionId;
        FarmId = farmId;
        FieldIds = new ReadOnlyCollection<string>((fieldIds ?? Array.Empty<string>()).ToArray());
        SeasonId = seasonId;
        UpdatedAt = updatedAt;
        UpdatedBy = string.IsNullOrWhiteSpace(updatedBy) ? "system:nexus" : updatedBy;
        GeometryNode = geometry?.DeepClone() ?? throw new ArgumentNullException(nameof(geometry));
    }

    /// <summary>
    /// Gets the layer identifier that produced the record.
    /// </summary>
    public string LayerId { get; }

    /// <summary>
    /// Gets the feature identifier associated with the record.
    /// </summary>
    public string FeatureId { get; }

    /// <summary>
    /// Gets the crop name.
    /// </summary>
    public string Crop { get; }

    /// <summary>
    /// Gets the crop lifecycle status.
    /// </summary>
    public string Status { get; }

    /// <summary>
    /// Gets the crop year.
    /// </summary>
    public int Year { get; }

    /// <summary>
    /// Gets the crop variety when provided.
    /// </summary>
    public string? Variety { get; }

    /// <summary>
    /// Gets the data source associated with the record.
    /// </summary>
    public string? Source { get; }

    /// <summary>
    /// Gets operator notes associated with the record.
    /// </summary>
    public string? Notes { get; }

    /// <summary>
    /// Gets the area tracked for the feature in hectares.
    /// </summary>
    public double? AreaHectares { get; }

    /// <summary>
    /// Gets the job identifier associated with the record.
    /// </summary>
    public string? JobId { get; }

    /// <summary>
    /// Gets the session identifier associated with the record.
    /// </summary>
    public string? SessionId { get; }

    /// <summary>
    /// Gets the farm identifier from the layer context.
    /// </summary>
    public string? FarmId { get; }

    /// <summary>
    /// Gets the field identifiers from the layer context.
    /// </summary>
    public IReadOnlyList<string> FieldIds { get; }

    /// <summary>
    /// Gets the season identifier from the layer context.
    /// </summary>
    public string? SeasonId { get; }

    /// <summary>
    /// Gets the timestamp of the most recent update.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; }

    /// <summary>
    /// Gets the actor that produced the latest change.
    /// </summary>
    public string UpdatedBy { get; }

    /// <summary>
    /// Gets the GeoJSON geometry associated with the record.
    /// </summary>
    public JsonNode Geometry => GeometryNode.DeepClone();

    internal JsonNode GeometryNode { get; }

    /// <summary>
    /// Creates a deep clone of the record.
    /// </summary>
    public CropHistoryRecord Clone()
    {
        return new CropHistoryRecord(
            LayerId,
            FeatureId,
            Crop,
            Status,
            Year,
            Variety,
            Source,
            Notes,
            AreaHectares,
            JobId,
            SessionId,
            FarmId,
            FieldIds,
            SeasonId,
            UpdatedAt,
            UpdatedBy,
            GeometryNode.DeepClone());
    }
}

/// <summary>
/// Summarises crop rotation history for analytics and reporting.
/// </summary>
public sealed record CropRotationSummary(
    string Crop,
    int Occurrences,
    int DistinctFieldCount,
    int? LatestYear,
    string? LatestStatus,
    DateTimeOffset? LatestUpdatedAt,
    double TotalAreaHectares);
