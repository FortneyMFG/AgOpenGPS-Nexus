using System;

namespace Aog.Plugins.CombineYield;

/// <summary>
/// Aggregated yield statistics keyed by crop.
/// </summary>
public sealed class YieldCropAggregation
{
    /// <summary>
    /// Initializes a new instance of the <see cref="YieldCropAggregation"/> class.
    /// </summary>
    public YieldCropAggregation(YieldAggregationScope scope, string crop, YieldSummary summary)
    {
        Scope = scope ?? throw new ArgumentNullException(nameof(scope));
        Crop = string.IsNullOrWhiteSpace(crop)
            ? throw new ArgumentException("Crop must be provided.", nameof(crop))
            : crop;
        Summary = summary ?? throw new ArgumentNullException(nameof(summary));
    }

    /// <summary>
    /// Gets the scope the aggregation applies to.
    /// </summary>
    public YieldAggregationScope Scope { get; }

    /// <summary>
    /// Gets the crop identifier.
    /// </summary>
    public string Crop { get; }

    /// <summary>
    /// Gets the aggregated statistics for the crop.
    /// </summary>
    public YieldSummary Summary { get; }
}

/// <summary>
/// Aggregated yield statistics keyed by crop and variety.
/// </summary>
public sealed class YieldVarietyAggregation
{
    /// <summary>
    /// Initializes a new instance of the <see cref="YieldVarietyAggregation"/> class.
    /// </summary>
    public YieldVarietyAggregation(YieldAggregationScope scope, string crop, string variety, YieldSummary summary)
    {
        Scope = scope ?? throw new ArgumentNullException(nameof(scope));
        Crop = string.IsNullOrWhiteSpace(crop)
            ? throw new ArgumentException("Crop must be provided.", nameof(crop))
            : crop;
        Variety = string.IsNullOrWhiteSpace(variety)
            ? throw new ArgumentException("Variety must be provided.", nameof(variety))
            : variety;
        Summary = summary ?? throw new ArgumentNullException(nameof(summary));
    }

    /// <summary>
    /// Gets the scope the aggregation applies to.
    /// </summary>
    public YieldAggregationScope Scope { get; }

    /// <summary>
    /// Gets the crop identifier.
    /// </summary>
    public string Crop { get; }

    /// <summary>
    /// Gets the variety identifier.
    /// </summary>
    public string Variety { get; }

    /// <summary>
    /// Gets the aggregated statistics for the variety.
    /// </summary>
    public YieldSummary Summary { get; }
}
