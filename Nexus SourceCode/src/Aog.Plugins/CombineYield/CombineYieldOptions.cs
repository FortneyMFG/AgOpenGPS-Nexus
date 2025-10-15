using System;
using System.Collections.Generic;
using System.Linq;

namespace Aog.Plugins.CombineYield;

/// <summary>
/// Options controlling how combine yield telemetry is aggregated and published.
/// </summary>
public sealed class CombineYieldOptions
{
    /// <summary>
    /// Gets or sets the grid cell size used for aggregating samples (meters).
    /// </summary>
    public double CellSizeMeters { get; set; } = 10.0;

    /// <summary>
    /// Gets or sets the time between automatic layer publications.
    /// </summary>
    public TimeSpan PublishInterval { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Gets or sets the telemetry source identifier applied to published layers.
    /// </summary>
    public string Source { get; set; } = "sim";

    /// <summary>
    /// Gets or sets the transform metadata captured in provenance records.
    /// </summary>
    public string Transform { get; set; } = "aggregate:combine-yield";

    /// <summary>
    /// Gets or sets the actor recorded in provenance entries.
    /// </summary>
    public string Actor { get; set; } = "plugin:combine-yield";

    /// <summary>
    /// Gets or sets the coordinate frame identifier for published layers.
    /// </summary>
    public string Frame { get; set; } = "vehicle";

    /// <summary>
    /// Gets or sets the crop name associated with the layer. Used for UI labeling and exports.
    /// </summary>
    public string Crop { get; set; } = "Unknown";

    /// <summary>
    /// Gets or sets the spatial reference identifier used for published tiles.
    /// </summary>
    public string Projection { get; set; } = "EPSG:4978";

    /// <summary>
    /// Gets or sets the identifier of the calibration profile applied to measurements.
    /// </summary>
    public string CalibrationProfileId { get; set; } = "calibration:default";

    /// <summary>
    /// Gets or sets the timestamp when the calibration profile was applied.
    /// </summary>
    public DateTimeOffset? CalibrationAppliedAt { get; set; }

    /// <summary>
    /// Gets or sets the origin of the calibration profile (e.g., manual, OEM import).
    /// </summary>
    public string? CalibrationSource { get; set; }

    /// <summary>
    /// Gets or sets the sensor or monitor model this calibration targets.
    /// </summary>
    public string? CalibrationSensorModel { get; set; }

    /// <summary>
    /// Gets or sets free-form notes describing calibration context.
    /// </summary>
    public string? CalibrationNotes { get; set; }

    /// <summary>
    /// Gets calibration factors applied during normalization keyed by sensor channel.
    /// </summary>
    public IDictionary<string, double> CalibrationFactors { get; } = new Dictionary<string, double>();

    /// <summary>
    /// Gets or sets the smoothing method recorded in metadata.
    /// </summary>
    public string SmoothingMethod { get; set; } = "movingAverage";

    /// <summary>
    /// Gets or sets the smoothing kernel size (odd integer) applied to the yield grid.
    /// </summary>
    public int SmoothingKernelSize { get; set; } = 3;

    /// <summary>
    /// Gets or sets the smoothing window length in seconds for metadata purposes.
    /// </summary>
    public double? SmoothingWindowSeconds { get; set; }

    /// <summary>
    /// Gets or sets the lag compensation applied during smoothing in seconds for metadata purposes.
    /// </summary>
    public double? SmoothingLagCompensationSeconds { get; set; }

    /// <summary>
    /// Gets or sets the number of smoothing passes executed.
    /// </summary>
    public int SmoothingPasses { get; set; } = 1;

    /// <summary>
    /// Gets or sets the fraction of the raw range used to clamp outliers after smoothing.
    /// </summary>
    public double OutlierClampFraction { get; set; } = 0.10;

    /// <summary>
    /// Gets or sets the aggregation basis recorded in metadata (e.g., area, time).
    /// </summary>
    public string AggregationBasis { get; set; } = "area";

    /// <summary>
    /// Gets or sets the scopes that receive aggregation rollups (job, field, season, etc.).
    /// </summary>
    public string[] AggregationScopes { get; set; } = new[] { "job", "field" };

    /// <summary>
    /// Gets or sets the binning scheme used to generate legend breaks.
    /// </summary>
    public YieldBinningScheme BinningScheme { get; set; } = YieldBinningScheme.Quantile;

    /// <summary>
    /// Gets or sets the number of bins when using quantile or equal-interval binning.
    /// </summary>
    public int BinningBinCount { get; set; } = 5;

    /// <summary>
    /// Gets or sets explicit break values when <see cref="BinningScheme"/> is <see cref="YieldBinningScheme.Custom"/>.
    /// </summary>
    public double[]? CustomBinBreaks { get; set; }

    /// <summary>
    /// Gets or sets optional custom labels corresponding to <see cref="CustomBinBreaks"/> intervals.
    /// </summary>
    public string[]? CustomBinLabels { get; set; }

    /// <summary>
    /// Validates the configured option values.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the configuration is invalid.</exception>
    public void Validate()
    {
        if (double.IsNaN(CellSizeMeters) || double.IsInfinity(CellSizeMeters) || CellSizeMeters <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(CellSizeMeters), CellSizeMeters, "Cell size must be a positive finite value.");
        }

        if (PublishInterval < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(PublishInterval), PublishInterval, "Publish interval cannot be negative.");
        }

        if (string.IsNullOrWhiteSpace(Source))
        {
            throw new ArgumentOutOfRangeException(nameof(Source), Source, "Source must be provided.");
        }

        if (string.IsNullOrWhiteSpace(Transform))
        {
            throw new ArgumentOutOfRangeException(nameof(Transform), Transform, "Transform must be provided.");
        }

        if (string.IsNullOrWhiteSpace(Actor))
        {
            throw new ArgumentOutOfRangeException(nameof(Actor), Actor, "Actor must be provided.");
        }

        if (string.IsNullOrWhiteSpace(Frame))
        {
            throw new ArgumentOutOfRangeException(nameof(Frame), Frame, "Frame must be provided.");
        }

        if (string.IsNullOrWhiteSpace(Crop))
        {
            throw new ArgumentOutOfRangeException(nameof(Crop), Crop, "Crop name must be provided.");
        }

        if (string.IsNullOrWhiteSpace(Projection))
        {
            throw new ArgumentOutOfRangeException(nameof(Projection), Projection, "Projection must be provided.");
        }

        if (string.IsNullOrWhiteSpace(CalibrationProfileId))
        {
            throw new ArgumentOutOfRangeException(nameof(CalibrationProfileId), CalibrationProfileId, "Calibration profile identifier must be provided.");
        }

        if (SmoothingKernelSize <= 0 || SmoothingKernelSize % 2 == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(SmoothingKernelSize), SmoothingKernelSize, "Smoothing kernel size must be a positive odd integer.");
        }

        if (SmoothingPasses <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(SmoothingPasses), SmoothingPasses, "Smoothing passes must be positive.");
        }

        if (OutlierClampFraction < 0 || double.IsNaN(OutlierClampFraction) || double.IsInfinity(OutlierClampFraction))
        {
            throw new ArgumentOutOfRangeException(nameof(OutlierClampFraction), OutlierClampFraction, "Outlier clamp fraction must be non-negative and finite.");
        }

        if (string.IsNullOrWhiteSpace(AggregationBasis))
        {
            throw new ArgumentOutOfRangeException(nameof(AggregationBasis), AggregationBasis, "Aggregation basis must be provided.");
        }

        if (AggregationScopes is null || AggregationScopes.Length == 0 || AggregationScopes.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentOutOfRangeException(nameof(AggregationScopes), AggregationScopes, "At least one aggregation scope must be provided.");
        }

        if (BinningScheme != YieldBinningScheme.Custom)
        {
            if (BinningBinCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(BinningBinCount), BinningBinCount, "Binning bin count must be positive when not using a custom scheme.");
            }
        }
        else
        {
            if (CustomBinBreaks is null || CustomBinBreaks.Length < 2)
            {
                throw new ArgumentOutOfRangeException(nameof(CustomBinBreaks), CustomBinBreaks, "Custom binning requires at least two break values.");
            }

            if (!IsStrictlyAscending(CustomBinBreaks))
            {
                throw new ArgumentOutOfRangeException(nameof(CustomBinBreaks), CustomBinBreaks, "Custom bin breaks must be in ascending order without duplicates.");
            }

            if (CustomBinLabels is not null && CustomBinLabels.Length != CustomBinBreaks.Length - 1)
            {
                throw new ArgumentOutOfRangeException(nameof(CustomBinLabels), CustomBinLabels, "Custom bin labels must align with the number of break intervals.");
            }
        }

        if (CalibrationFactors.Any(kvp => string.IsNullOrWhiteSpace(kvp.Key) || !double.IsFinite(kvp.Value)))
        {
            throw new ArgumentOutOfRangeException(nameof(CalibrationFactors), CalibrationFactors, "Calibration factors must contain finite values keyed by non-empty identifiers.");
        }
    }

    private static bool IsStrictlyAscending(double[] values)
    {
        if (values.Length < 2)
        {
            return false;
        }

        for (var i = 1; i < values.Length; i++)
        {
            if (!(values[i] > values[i - 1]))
            {
                return false;
            }
        }

        return true;
    }
}
