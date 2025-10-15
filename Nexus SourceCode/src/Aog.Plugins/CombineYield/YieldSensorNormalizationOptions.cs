using System;

namespace Aog.Plugins.CombineYield;

/// <summary>
/// Configuration for the <see cref="YieldSensorNormalizer"/>.
/// </summary>
public sealed class YieldSensorNormalizationOptions
{
    /// <summary>
    /// Gets or sets the multiplicative gain applied to raw mass flow measurements.
    /// </summary>
    public double MassFlowGain { get; set; } = 1.0;

    /// <summary>
    /// Gets or sets the additive offset applied after the mass flow gain.
    /// </summary>
    public double MassFlowOffset { get; set; }

    /// <summary>
    /// Gets or sets the multiplicative gain applied to moisture readings when present.
    /// </summary>
    public double MoistureGain { get; set; } = 1.0;

    /// <summary>
    /// Gets or sets the additive offset applied after the moisture gain.
    /// </summary>
    public double MoistureOffset { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether moisture values are clamped to the valid range of 0-100 percent.
    /// </summary>
    public bool ClampMoistureToValidRange { get; set; } = true;

    /// <summary>
    /// Gets or sets the minimum acceptable ground speed in meters per second. Samples below the threshold are ignored.
    /// </summary>
    public double MinimumGroundSpeedMps { get; set; } = 0.1;

    /// <summary>
    /// Gets or sets the minimum acceptable swath width in meters. Samples below the threshold are ignored.
    /// </summary>
    public double MinimumSwathWidthMeters { get; set; } = 0.5;

    /// <summary>
    /// Gets or sets the minimum acceptable calibrated mass flow (kg/s). Samples below the threshold are ignored.
    /// </summary>
    public double MinimumFlowKgPerSecond { get; set; } = 0.01;

    /// <summary>
    /// Gets or sets an optional smoothing factor (0-1] for mass flow. When <c>null</c> or &lt;= 0 no smoothing is applied.
    /// </summary>
    public double? FlowSmoothingFactor { get; set; } = 0.2;

    /// <summary>
    /// Gets or sets an optional smoothing factor (0-1] for moisture. When <c>null</c> or &lt;= 0 no smoothing is applied.
    /// </summary>
    public double? MoistureSmoothingFactor { get; set; } = 0.2;

    /// <summary>
    /// Gets or sets an optional lower clamp for computed yield in kilograms per hectare.
    /// </summary>
    public double? MinimumYieldKgPerHectare { get; set; }

    /// <summary>
    /// Gets or sets an optional upper clamp for computed yield in kilograms per hectare.
    /// </summary>
    public double? MaximumYieldKgPerHectare { get; set; }

    /// <summary>
    /// Gets or sets the lag applied to normalized samples. Samples are emitted once their age exceeds this duration.
    /// </summary>
    public TimeSpan Lag { get; set; } = TimeSpan.Zero;

    /// <summary>
    /// Validates the configured values.
    /// </summary>
    public void Validate()
    {
        if (!double.IsFinite(MassFlowGain))
        {
            throw new ArgumentOutOfRangeException(nameof(MassFlowGain), MassFlowGain, "Mass flow gain must be finite.");
        }

        if (!double.IsFinite(MassFlowOffset))
        {
            throw new ArgumentOutOfRangeException(nameof(MassFlowOffset), MassFlowOffset, "Mass flow offset must be finite.");
        }

        if (!double.IsFinite(MoistureGain))
        {
            throw new ArgumentOutOfRangeException(nameof(MoistureGain), MoistureGain, "Moisture gain must be finite.");
        }

        if (!double.IsFinite(MoistureOffset))
        {
            throw new ArgumentOutOfRangeException(nameof(MoistureOffset), MoistureOffset, "Moisture offset must be finite.");
        }

        if (MinimumGroundSpeedMps < 0 || double.IsNaN(MinimumGroundSpeedMps) || double.IsInfinity(MinimumGroundSpeedMps))
        {
            throw new ArgumentOutOfRangeException(nameof(MinimumGroundSpeedMps), MinimumGroundSpeedMps, "Minimum ground speed must be finite and non-negative.");
        }

        if (MinimumSwathWidthMeters < 0 || double.IsNaN(MinimumSwathWidthMeters) || double.IsInfinity(MinimumSwathWidthMeters))
        {
            throw new ArgumentOutOfRangeException(nameof(MinimumSwathWidthMeters), MinimumSwathWidthMeters, "Minimum swath width must be finite and non-negative.");
        }

        if (MinimumFlowKgPerSecond < 0 || double.IsNaN(MinimumFlowKgPerSecond) || double.IsInfinity(MinimumFlowKgPerSecond))
        {
            throw new ArgumentOutOfRangeException(nameof(MinimumFlowKgPerSecond), MinimumFlowKgPerSecond, "Minimum flow must be finite and non-negative.");
        }

        ValidateSmoothingFactor(FlowSmoothingFactor, nameof(FlowSmoothingFactor));
        ValidateSmoothingFactor(MoistureSmoothingFactor, nameof(MoistureSmoothingFactor));

        if (MinimumYieldKgPerHectare.HasValue && !double.IsFinite(MinimumYieldKgPerHectare.Value))
        {
            throw new ArgumentOutOfRangeException(nameof(MinimumYieldKgPerHectare), MinimumYieldKgPerHectare, "Minimum yield must be finite when specified.");
        }

        if (MaximumYieldKgPerHectare.HasValue && !double.IsFinite(MaximumYieldKgPerHectare.Value))
        {
            throw new ArgumentOutOfRangeException(nameof(MaximumYieldKgPerHectare), MaximumYieldKgPerHectare, "Maximum yield must be finite when specified.");
        }

        if (MinimumYieldKgPerHectare.HasValue && MaximumYieldKgPerHectare.HasValue && MinimumYieldKgPerHectare > MaximumYieldKgPerHectare)
        {
            throw new ArgumentOutOfRangeException(nameof(MinimumYieldKgPerHectare), MinimumYieldKgPerHectare, "Minimum yield cannot exceed maximum yield.");
        }

        if (Lag < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(Lag), Lag, "Lag cannot be negative.");
        }
    }

    private static void ValidateSmoothingFactor(double? factor, string name)
    {
        if (!factor.HasValue)
        {
            return;
        }

        if (factor.Value < 0 || factor.Value > 1 || double.IsNaN(factor.Value) || double.IsInfinity(factor.Value))
        {
            throw new ArgumentOutOfRangeException(name, factor, "Smoothing factor must be within [0, 1].");
        }
    }
}
