using System;
using System.Collections.Generic;
using System.Linq;

namespace Aog.Plugins.CombineYield;

/// <summary>
/// Provides helper methods for composing yield analytics snapshots and evaluating queries.
/// </summary>
public sealed class YieldAnalyticsService
{
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="YieldAnalyticsService"/> class.
    /// </summary>
    /// <param name="timeProvider">Optional time provider for deterministic testing.</param>
    public YieldAnalyticsService(TimeProvider? timeProvider = null)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>
    /// Creates an immutable analytics snapshot from the supplied aggregation entries.
    /// </summary>
    /// <param name="cropAggregations">Aggregations summarised by crop.</param>
    /// <param name="varietyAggregations">Aggregations summarised by crop and variety.</param>
    public YieldAnalyticsSnapshot CreateSnapshot(
        IEnumerable<YieldCropAggregation>? cropAggregations,
        IEnumerable<YieldVarietyAggregation>? varietyAggregations)
    {
        var crops = Normalize(cropAggregations, nameof(cropAggregations));
        var varieties = Normalize(varietyAggregations, nameof(varietyAggregations));
        return new YieldAnalyticsSnapshot(_timeProvider.GetUtcNow(), crops, varieties);
    }

    /// <summary>
    /// Returns yield aggregations grouped by crop that match the supplied scope filter.
    /// </summary>
    /// <param name="snapshot">Snapshot produced by <see cref="CreateSnapshot"/>.</param>
    /// <param name="scope">Scope filter to evaluate.</param>
    public IReadOnlyList<YieldCropAggregation> GetYieldByCrop(YieldAnalyticsSnapshot snapshot, YieldScopeFilter scope)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(scope);

        var normalized = Clone(scope);
        normalized.Normalize();

        return snapshot.Crops
            .Where(entry => entry.Scope.Matches(normalized))
            .OrderBy(entry => entry.Crop, StringComparer.OrdinalIgnoreCase)
            .ThenBy(entry => entry.Scope.FarmId, StringComparer.Ordinal)
            .ThenBy(entry => entry.Scope.SeasonId ?? string.Empty, StringComparer.Ordinal)
            .ThenBy(entry => entry.Scope.FieldId ?? string.Empty, StringComparer.Ordinal)
            .ThenBy(entry => entry.Scope.JobId ?? string.Empty, StringComparer.Ordinal)
            .ThenBy(entry => entry.Scope.SessionId ?? string.Empty, StringComparer.Ordinal)
            .ToArray();
    }

    /// <summary>
    /// Returns yield aggregations grouped by variety that match the supplied scope filter.
    /// </summary>
    /// <param name="snapshot">Snapshot produced by <see cref="CreateSnapshot"/>.</param>
    /// <param name="scope">Scope filter to evaluate.</param>
    public IReadOnlyList<YieldVarietyAggregation> GetYieldByVariety(YieldAnalyticsSnapshot snapshot, YieldScopeFilter scope)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(scope);

        var normalized = Clone(scope);
        normalized.Normalize();

        return snapshot.Varieties
            .Where(entry => entry.Scope.Matches(normalized))
            .OrderBy(entry => entry.Crop, StringComparer.OrdinalIgnoreCase)
            .ThenBy(entry => entry.Variety, StringComparer.OrdinalIgnoreCase)
            .ThenBy(entry => entry.Scope.FarmId, StringComparer.Ordinal)
            .ThenBy(entry => entry.Scope.SeasonId ?? string.Empty, StringComparer.Ordinal)
            .ThenBy(entry => entry.Scope.FieldId ?? string.Empty, StringComparer.Ordinal)
            .ThenBy(entry => entry.Scope.JobId ?? string.Empty, StringComparer.Ordinal)
            .ThenBy(entry => entry.Scope.SessionId ?? string.Empty, StringComparer.Ordinal)
            .ToArray();
    }

    private static List<T> Normalize<T>(IEnumerable<T>? source, string parameterName)
    {
        if (source is null)
        {
            return new List<T>();
        }

        if (source is List<T> list)
        {
            EnsureNoNullEntries(list, parameterName);
            return new List<T>(list);
        }

        var materialized = new List<T>();
        foreach (var item in source)
        {
            if (item is null)
            {
                throw new ArgumentException("Aggregation collections cannot contain null entries.", parameterName);
            }

            materialized.Add(item);
        }

        return materialized;
    }

    private static YieldScopeFilter Clone(YieldScopeFilter scope)
    {
        return new YieldScopeFilter
        {
            FarmId = scope.FarmId,
            SeasonId = scope.SeasonId,
            FieldId = scope.FieldId,
            JobId = scope.JobId,
            SessionId = scope.SessionId
        };
    }

    private static void EnsureNoNullEntries<T>(IReadOnlyList<T> items, string parameterName)
    {
        for (var i = 0; i < items.Count; i++)
        {
            if (items[i] is null)
            {
                throw new ArgumentException("Aggregation collections cannot contain null entries.", parameterName);
            }
        }
    }
}
