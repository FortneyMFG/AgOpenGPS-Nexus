using System;
using System.Collections.Generic;
using Aog.Core.V1;

namespace Aog.Plugins.PlanterMonitor;

/// <summary>
/// Represents an immutable snapshot of planter row analytics aggregated from live measurements.
/// </summary>
public sealed class PlanterMonitorAnalyticsSnapshot
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PlanterMonitorAnalyticsSnapshot"/> class.
    /// </summary>
    /// <param name="timestamp">Timestamp associated with the analytics snapshot.</param>
    /// <param name="totalMeasurements">Total number of measurements processed since aggregator creation.</param>
    /// <param name="activeRowCount">Number of rows that have reported at least one measurement.</param>
    /// <param name="skipRowCount">Number of rows currently classified as skips.</param>
    /// <param name="doubleRowCount">Number of rows currently classified as doubles.</param>
    /// <param name="unknownRowCount">Number of rows currently classified as unknown.</param>
    /// <param name="averagePopulationErrorPerMeter">Average absolute population error across all samples.</param>
    /// <param name="averageSkipRate">Average skip rate across all samples.</param>
    /// <param name="averageDoubleRate">Average double rate across all samples.</param>
    /// <param name="maxSkipRate">Maximum latest skip rate among all rows.</param>
    /// <param name="maxDoubleRate">Maximum latest double rate among all rows.</param>
    /// <param name="overallQuality">Most severe quality classification observed among all rows.</param>
    /// <param name="rows">Per-row analytics summaries.</param>
    public PlanterMonitorAnalyticsSnapshot(
        DateTimeOffset timestamp,
        long totalMeasurements,
        int activeRowCount,
        int skipRowCount,
        int doubleRowCount,
        int unknownRowCount,
        double averagePopulationErrorPerMeter,
        double averageSkipRate,
        double averageDoubleRate,
        double maxSkipRate,
        double maxDoubleRate,
        PlanterRowQuality overallQuality,
        IReadOnlyList<PlanterRowAnalytics> rows)
    {
        Timestamp = timestamp;
        TotalMeasurements = totalMeasurements;
        ActiveRowCount = activeRowCount;
        SkipRowCount = skipRowCount;
        DoubleRowCount = doubleRowCount;
        UnknownRowCount = unknownRowCount;
        AveragePopulationErrorPerMeter = averagePopulationErrorPerMeter;
        AverageSkipRate = averageSkipRate;
        AverageDoubleRate = averageDoubleRate;
        MaxSkipRate = maxSkipRate;
        MaxDoubleRate = maxDoubleRate;
        OverallQuality = overallQuality;
        Rows = rows ?? throw new ArgumentNullException(nameof(rows));
    }

    /// <summary>Gets the timestamp associated with the analytics snapshot.</summary>
    public DateTimeOffset Timestamp { get; }

    /// <summary>Gets the total number of measurements processed by the aggregator.</summary>
    public long TotalMeasurements { get; }

    /// <summary>Gets the number of rows that have reported at least one measurement.</summary>
    public int ActiveRowCount { get; }

    /// <summary>Gets the number of rows currently classified as skips.</summary>
    public int SkipRowCount { get; }

    /// <summary>Gets the number of rows currently classified as doubles.</summary>
    public int DoubleRowCount { get; }

    /// <summary>Gets the number of rows currently classified as unknown.</summary>
    public int UnknownRowCount { get; }

    /// <summary>Gets the average absolute population error in seeds per meter.</summary>
    public double AveragePopulationErrorPerMeter { get; }

    /// <summary>Gets the average skip rate across all samples.</summary>
    public double AverageSkipRate { get; }

    /// <summary>Gets the average double rate across all samples.</summary>
    public double AverageDoubleRate { get; }

    /// <summary>Gets the worst observed skip rate among all rows.</summary>
    public double MaxSkipRate { get; }

    /// <summary>Gets the worst observed double rate among all rows.</summary>
    public double MaxDoubleRate { get; }

    /// <summary>Gets the most severe row quality classification observed.</summary>
    public PlanterRowQuality OverallQuality { get; }

    /// <summary>Gets the per-row analytics summaries ordered by row index.</summary>
    public IReadOnlyList<PlanterRowAnalytics> Rows { get; }
}

/// <summary>
/// Analytics summary for a single planter row.
/// </summary>
public sealed record PlanterRowAnalytics(
    int RowIndex,
    int SampleCount,
    double AverageTargetPopulationPerMeter,
    double AverageActualPopulationPerMeter,
    double AveragePopulationErrorPerMeter,
    double AverageSkipRate,
    double AverageDoubleRate,
    int SkipCount,
    int DoubleCount,
    int UnknownCount,
    PlanterRowQuality LatestQuality,
    double LatestSkipRate,
    double LatestDoubleRate,
    double LatestTargetPopulationPerMeter,
    double LatestActualPopulationPerMeter);
