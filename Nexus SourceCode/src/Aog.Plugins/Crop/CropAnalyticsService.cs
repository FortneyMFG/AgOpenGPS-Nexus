using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json.Nodes;

namespace Aog.Plugins.Crop;

/// <summary>
/// Provides analytics helpers for crop layer snapshots including rotation lookups.
/// </summary>
public sealed class CropAnalyticsService
{
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="CropAnalyticsService"/> class.
    /// </summary>
    /// <param name="timeProvider">Optional time provider used when stamping analytics snapshots.</param>
    public CropAnalyticsService(TimeProvider? timeProvider = null)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>
    /// Creates an analytics snapshot based on the supplied layer snapshots.
    /// </summary>
    /// <param name="plannedLayers">Snapshots for planned crop layers.</param>
    /// <param name="actualLayers">Snapshots for actual crop layers.</param>
    /// <param name="historicalLayers">Snapshots for historical crop layers.</param>
    public CropAnalyticsSnapshot CreateSnapshot(
        IEnumerable<CropLayerSnapshot>? plannedLayers,
        IEnumerable<CropLayerSnapshot>? actualLayers,
        IEnumerable<CropLayerSnapshot>? historicalLayers)
    {
        var planned = (plannedLayers ?? Array.Empty<CropLayerSnapshot>()).ToArray();
        var actual = (actualLayers ?? Array.Empty<CropLayerSnapshot>()).ToArray();
        var historical = (historicalLayers ?? Array.Empty<CropLayerSnapshot>()).ToArray();

        var plannedSummaries = Aggregate(planned);
        var actualSummaries = Aggregate(actual);
        var historicalSummaries = Aggregate(historical);

        var historyRecords = BuildHistoryRecords(historical);
        var latestByField = BuildLatestByField(historyRecords);
        var rotationSummaries = BuildRotationSummaries(historyRecords);
        var orderedHistory = historyRecords
            .OrderByDescending(record => record.UpdatedAt)
            .ThenBy(record => record.LayerId, StringComparer.OrdinalIgnoreCase)
            .ThenBy(record => record.FeatureId, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new CropAnalyticsSnapshot(
            _timeProvider.GetUtcNow(),
            plannedSummaries,
            actualSummaries,
            historicalSummaries,
            rotationSummaries,
            latestByField,
            orderedHistory);
    }

    /// <summary>
    /// Finds the most recent historical crop for the specified field identifier.
    /// </summary>
    /// <param name="snapshot">Analytics snapshot produced by <see cref="CreateSnapshot"/>.</param>
    /// <param name="fieldId">Field identifier to evaluate.</param>
    public CropHistoryRecord? GetPreviousCropForField(CropAnalyticsSnapshot snapshot, string fieldId)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldId);

        if (!snapshot.LatestByField.TryGetValue(fieldId, out var record))
        {
            return null;
        }

        return record.Clone();
    }

    /// <summary>
    /// Finds the most recent historical crop record matching the supplied geometry.
    /// </summary>
    /// <param name="snapshot">Analytics snapshot produced by <see cref="CreateSnapshot"/>.</param>
    /// <param name="geometry">Geometry to compare against historical records.</param>
    public CropHistoryRecord? GetPreviousCropForGeometry(CropAnalyticsSnapshot snapshot, JsonNode geometry)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(geometry);

        foreach (var record in snapshot.HistoryByRecency)
        {
            if (JsonNode.DeepEquals(record.GeometryNode, geometry))
            {
                return record.Clone();
            }
        }

        return null;
    }

    private static IReadOnlyList<CropAcreageSummary> Aggregate(IEnumerable<CropLayerSnapshot> snapshots)
    {
        var totals = new Dictionary<(string Crop, string Status, int Year), (double AreaSqMeters, int Count)>(
            new CropSummaryKeyComparer());

        foreach (var snapshot in snapshots)
        {
            if (snapshot is null)
            {
                continue;
            }

            foreach (var feature in snapshot.Features)
            {
                if (feature is null)
                {
                    continue;
                }

                var key = (feature.Crop, feature.Status, feature.Year);
                var area = feature.AreaSqMeters ?? 0d;

                if (totals.TryGetValue(key, out var existing))
                {
                    totals[key] = (existing.AreaSqMeters + area, existing.Count + 1);
                }
                else
                {
                    totals[key] = (area, 1);
                }
            }
        }

        return totals
            .Select(pair => new CropAcreageSummary(
                pair.Key.Crop,
                pair.Key.Status,
                pair.Key.Year,
                ConvertToHectares(pair.Value.AreaSqMeters),
                pair.Value.Count))
            .OrderBy(summary => summary.Crop, StringComparer.OrdinalIgnoreCase)
            .ThenByDescending(summary => summary.Year)
            .ThenBy(summary => summary.Status, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static CropHistoryRecord[] BuildHistoryRecords(IEnumerable<CropLayerSnapshot> snapshots)
    {
        var records = new List<CropHistoryRecord>();
        foreach (var snapshot in snapshots)
        {
            if (snapshot is null)
            {
                continue;
            }

            foreach (var feature in snapshot.Features)
            {
                if (feature is null)
                {
                    continue;
                }

                var context = snapshot.Context;
                var farmId = context?.FarmId;
                var fieldIds = context?.FieldIds ?? Array.Empty<string>();
                var seasonId = context?.SeasonId;

                var record = new CropHistoryRecord(
                    snapshot.LayerId,
                    feature.FeatureId,
                    feature.Crop,
                    feature.Status,
                    feature.Year,
                    feature.Variety,
                    feature.Source,
                    feature.Notes,
                    feature.AreaSqMeters.HasValue ? ConvertToHectares(feature.AreaSqMeters.Value) : null,
                    snapshot.JobId,
                    snapshot.SessionId,
                    farmId,
                    fieldIds,
                    seasonId,
                    feature.UpdatedAt,
                    feature.UpdatedBy,
                    feature.Geometry);

                records.Add(record);
            }
        }

        return records.ToArray();
    }

    private static IReadOnlyDictionary<string, CropHistoryRecord> BuildLatestByField(IEnumerable<CropHistoryRecord> records)
    {
        var lookup = new Dictionary<string, CropHistoryRecord>(StringComparer.OrdinalIgnoreCase);
        foreach (var record in records)
        {
            foreach (var fieldId in record.FieldIds)
            {
                if (lookup.TryGetValue(fieldId, out var existing))
                {
                    if (record.UpdatedAt <= existing.UpdatedAt)
                    {
                        continue;
                    }
                }

                lookup[fieldId] = record;
            }
        }

        return new ReadOnlyDictionary<string, CropHistoryRecord>(lookup);
    }

    private static IReadOnlyList<CropRotationSummary> BuildRotationSummaries(IEnumerable<CropHistoryRecord> records)
    {
        var grouped = records
            .GroupBy(record => record.Crop, StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var distinctFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                double totalArea = 0d;
                CropHistoryRecord? latest = null;

                foreach (var record in group)
                {
                    foreach (var field in record.FieldIds)
                    {
                        distinctFields.Add(field);
                    }

                    if (record.AreaHectares.HasValue)
                    {
                        totalArea += record.AreaHectares.Value;
                    }

                    if (latest is null || record.UpdatedAt > latest.UpdatedAt)
                    {
                        latest = record;
                    }
                }

                return new CropRotationSummary(
                    Crop: group.Key,
                    Occurrences: group.Count(),
                    DistinctFieldCount: distinctFields.Count,
                    LatestYear: latest?.Year,
                    LatestStatus: latest?.Status,
                    LatestUpdatedAt: latest?.UpdatedAt,
                    TotalAreaHectares: Math.Round(totalArea, 4, MidpointRounding.AwayFromZero));
            })
            .OrderBy(summary => summary.Crop, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new ReadOnlyCollection<CropRotationSummary>(grouped);
    }

    private static double ConvertToHectares(double areaSqMeters)
    {
        return Math.Round(areaSqMeters / 10_000d, 4, MidpointRounding.AwayFromZero);
    }

    private sealed class CropSummaryKeyComparer : IEqualityComparer<(string Crop, string Status, int Year)>
    {
        public bool Equals((string Crop, string Status, int Year) x, (string Crop, string Status, int Year) y)
        {
            return string.Equals(x.Crop, y.Crop, StringComparison.OrdinalIgnoreCase)
                && string.Equals(x.Status, y.Status, StringComparison.OrdinalIgnoreCase)
                && x.Year == y.Year;
        }

        public int GetHashCode((string Crop, string Status, int Year) obj)
        {
            var cropHash = obj.Crop is null ? 0 : StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Crop);
            var statusHash = obj.Status is null ? 0 : StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Status);
            return HashCode.Combine(cropHash, statusHash, obj.Year);
        }
    }
}
