using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Aog.Plugins.CombineYield;

/// <summary>
/// Immutable snapshot describing the current yield analytics aggregates.
/// </summary>
public sealed class YieldAnalyticsSnapshot
{
    /// <summary>
    /// Initializes a new instance of the <see cref="YieldAnalyticsSnapshot"/> class.
    /// </summary>
    public YieldAnalyticsSnapshot(
        DateTimeOffset generatedAt,
        IReadOnlyList<YieldCropAggregation> crops,
        IReadOnlyList<YieldVarietyAggregation> varieties)
    {
        GeneratedAt = generatedAt;
        Crops = Wrap(crops);
        Varieties = Wrap(varieties);
    }

    /// <summary>
    /// Gets the timestamp when the snapshot was generated.
    /// </summary>
    public DateTimeOffset GeneratedAt { get; }

    /// <summary>
    /// Gets the aggregated crop analytics entries.
    /// </summary>
    public IReadOnlyList<YieldCropAggregation> Crops { get; }

    /// <summary>
    /// Gets the aggregated variety analytics entries.
    /// </summary>
    public IReadOnlyList<YieldVarietyAggregation> Varieties { get; }

    private static IReadOnlyList<T> Wrap<T>(IReadOnlyList<T> items)
    {
        if (items is null)
        {
            throw new ArgumentNullException(nameof(items));
        }

        if (items.Count == 0)
        {
            return Array.Empty<T>();
        }

        if (items is List<T> list)
        {
            return new ReadOnlyCollection<T>(list);
        }

        return new ReadOnlyCollection<T>(new List<T>(items));
    }
}
