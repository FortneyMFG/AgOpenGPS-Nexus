using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Aog.Plugins.CombineYield;

/// <summary>
/// Describes the metadata published alongside a combine yield layer.
/// </summary>
/// <param name="Grid">Grid definition used for tiling.</param>
/// <param name="Smoothing">Smoothing pipeline metadata.</param>
/// <param name="Calibration">Calibration profile details applied during normalization.</param>
/// <param name="Aggregation">Aggregation and binning metadata.</param>
/// <param name="Statistics">Summary statistics calculated from the grid.</param>
public sealed record YieldLayerMetadata(
    YieldGridMetadata Grid,
    YieldSmoothingMetadata Smoothing,
    YieldCalibrationMetadata Calibration,
    YieldAggregationMetadata Aggregation,
    YieldStatisticsMetadata Statistics);

/// <summary>
/// Grid definition for a yield layer.
/// </summary>
/// <param name="CellSizeMeters">Edge length of each cell in metres.</param>
/// <param name="Projection">Spatial reference identifier for the grid.</param>
public sealed record YieldGridMetadata(double CellSizeMeters, string Projection);

/// <summary>
/// Describes the smoothing pipeline applied to raw sensor data.
/// </summary>
/// <param name="Method">Algorithm identifier (e.g., movingAverage).</param>
/// <param name="WindowSeconds">Window length used for smoothing.</param>
/// <param name="LagCompensationSeconds">Lag compensation applied to align samples.</param>
/// <param name="Passes">Number of smoothing passes executed.</param>
public sealed record YieldSmoothingMetadata(string Method, double WindowSeconds, double LagCompensationSeconds, int Passes);

/// <summary>
/// Calibration profile metadata captured for the layer.
/// </summary>
/// <param name="ProfileId">Identifier of the calibration profile.</param>
/// <param name="AppliedAt">Timestamp when the calibration profile was applied.</param>
/// <param name="Source">Origin of the calibration profile.</param>
/// <param name="SensorModel">Sensor or monitor model associated with the calibration.</param>
/// <param name="Notes">Optional notes describing the calibration.</param>
/// <param name="Factors">Per-channel calibration factors applied during normalization.</param>
public sealed record YieldCalibrationMetadata(
    string ProfileId,
    DateTimeOffset? AppliedAt,
    string? Source,
    string? SensorModel,
    string? Notes,
    IReadOnlyDictionary<string, double> Factors);

/// <summary>
/// Aggregation metadata including scopes and legend binning.
/// </summary>
/// <param name="Basis">Basis used for aggregation (e.g., area).</param>
/// <param name="Scopes">Scopes that receive aggregation rollups.</param>
/// <param name="UpdatedAt">Timestamp when aggregations were generated.</param>
/// <param name="Bins">Binning metadata shared with the UI.</param>
public sealed record YieldAggregationMetadata(
    string Basis,
    IReadOnlyList<string> Scopes,
    DateTimeOffset UpdatedAt,
    YieldBinningMetadata Bins);

/// <summary>
/// Binning metadata used to render map legends.
/// </summary>
/// <param name="Scheme">Scheme identifier (quantile, equalInterval, custom).</param>
/// <param name="Count">Number of bins when applicable.</param>
/// <param name="Breaks">Breakpoints for the bins, if defined.</param>
/// <param name="Labels">Optional human-readable labels for each interval.</param>
public sealed record YieldBinningMetadata(
    string Scheme,
    int Count,
    IReadOnlyList<double>? Breaks,
    IReadOnlyList<string>? Labels)
{
    /// <summary>
    /// Creates a binning metadata instance with defensively copied collections.
    /// </summary>
    public static YieldBinningMetadata Create(string scheme, int count, IEnumerable<double>? breaks, IEnumerable<string>? labels)
    {
        IReadOnlyList<double>? breaksList = null;
        IReadOnlyList<string>? labelsList = null;

        if (breaks is not null)
        {
            breaksList = new ReadOnlyCollection<double>(new List<double>(breaks));
        }

        if (labels is not null)
        {
            labelsList = new ReadOnlyCollection<string>(new List<string>(labels));
        }

        return new YieldBinningMetadata(scheme, count, breaksList, labelsList);
    }
}

/// <summary>
/// Summary statistics calculated from the aggregated grid.
/// </summary>
/// <param name="SampleCount">Total number of samples contributing to the layer.</param>
/// <param name="Mean">Weighted mean yield.</param>
/// <param name="Median">Weighted median yield.</param>
/// <param name="StdDev">Weighted standard deviation of yield.</param>
/// <param name="Min">Minimum observed yield.</param>
/// <param name="Max">Maximum observed yield.</param>
/// <param name="TotalMassKg">Total harvested mass represented by the layer.</param>
public sealed record YieldStatisticsMetadata(
    int SampleCount,
    double Mean,
    double Median,
    double StdDev,
    double Min,
    double Max,
    double TotalMassKg);
