using System;

namespace Aog.Plugins.CombineYield;

/// <summary>
/// Summary statistics describing a yield aggregation bucket.
/// </summary>
public sealed class YieldSummary
{
    /// <summary>
    /// Initializes a new instance of the <see cref="YieldSummary"/> class.
    /// </summary>
    public YieldSummary(int sampleCount, double mean, double median, double stdDev, double min, double max, double totalMassKg)
    {
        if (sampleCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sampleCount), sampleCount, "Sample count must be non-negative.");
        }

        if (!IsFinite(mean))
        {
            throw new ArgumentOutOfRangeException(nameof(mean), mean, "Mean must be a finite value.");
        }

        if (!IsFinite(median))
        {
            throw new ArgumentOutOfRangeException(nameof(median), median, "Median must be a finite value.");
        }

        if (!IsFinite(stdDev) || stdDev < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(stdDev), stdDev, "Standard deviation must be a non-negative finite value.");
        }

        if (!IsFinite(min))
        {
            throw new ArgumentOutOfRangeException(nameof(min), min, "Minimum must be a finite value.");
        }

        if (!IsFinite(max))
        {
            throw new ArgumentOutOfRangeException(nameof(max), max, "Maximum must be a finite value.");
        }

        if (!IsFinite(totalMassKg) || totalMassKg < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalMassKg), totalMassKg, "Total mass must be a non-negative finite value.");
        }

        SampleCount = sampleCount;
        Mean = mean;
        Median = median;
        StdDev = stdDev;
        Min = min;
        Max = max;
        TotalMassKg = totalMassKg;
    }

    /// <summary>
    /// Gets the total number of samples aggregated.
    /// </summary>
    public int SampleCount { get; }

    /// <summary>
    /// Gets the weighted mean yield in kilograms per hectare.
    /// </summary>
    public double Mean { get; }

    /// <summary>
    /// Gets the weighted median yield in kilograms per hectare.
    /// </summary>
    public double Median { get; }

    /// <summary>
    /// Gets the weighted standard deviation in kilograms per hectare.
    /// </summary>
    public double StdDev { get; }

    /// <summary>
    /// Gets the minimum observed yield in kilograms per hectare.
    /// </summary>
    public double Min { get; }

    /// <summary>
    /// Gets the maximum observed yield in kilograms per hectare.
    /// </summary>
    public double Max { get; }

    /// <summary>
    /// Gets the total harvested mass represented by the aggregation in kilograms.
    /// </summary>
    public double TotalMassKg { get; }

    private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
}
