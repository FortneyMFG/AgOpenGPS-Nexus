using System;

namespace Aog.Plugins.CombineYield;

/// <summary>
/// Represents a raw sensor sample prior to normalization.
/// </summary>
/// <param name="Timestamp">Timestamp when the sample was captured (UTC).</param>
/// <param name="EastingMeters">Local east offset from the field origin in meters.</param>
/// <param name="NorthingMeters">Local north offset from the field origin in meters.</param>
/// <param name="MassFlowKgPerSecond">Instantaneous mass flow reported by the sensor in kilograms per second.</param>
/// <param name="GroundSpeedMetersPerSecond">Ground speed of the combine in meters per second.</param>
/// <param name="SwathWidthMeters">Effective swath width of the header in meters.</param>
/// <param name="MoisturePercent">Optional grain moisture percentage reported by the sensor.</param>
public readonly record struct YieldSensorSample(
    DateTimeOffset Timestamp,
    double EastingMeters,
    double NorthingMeters,
    double MassFlowKgPerSecond,
    double GroundSpeedMetersPerSecond,
    double SwathWidthMeters,
    double? MoisturePercent = null)
{
    /// <summary>
    /// Validates that the sample contains finite values.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when a value is not finite.</exception>
    public void Validate()
    {
        if (!double.IsFinite(EastingMeters) || EastingMeters < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(EastingMeters), EastingMeters, "Easting must be finite and non-negative.");
        }

        if (!double.IsFinite(NorthingMeters) || NorthingMeters < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(NorthingMeters), NorthingMeters, "Northing must be finite and non-negative.");
        }

        if (!double.IsFinite(MassFlowKgPerSecond) || MassFlowKgPerSecond < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(MassFlowKgPerSecond), MassFlowKgPerSecond, "Mass flow must be finite and non-negative.");
        }

        if (!double.IsFinite(GroundSpeedMetersPerSecond) || GroundSpeedMetersPerSecond < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(GroundSpeedMetersPerSecond), GroundSpeedMetersPerSecond, "Ground speed must be finite and non-negative.");
        }

        if (!double.IsFinite(SwathWidthMeters) || SwathWidthMeters < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(SwathWidthMeters), SwathWidthMeters, "Swath width must be finite and non-negative.");
        }

        if (MoisturePercent.HasValue && !double.IsFinite(MoisturePercent.Value))
        {
            throw new ArgumentOutOfRangeException(nameof(MoisturePercent), MoisturePercent, "Moisture must be finite when supplied.");
        }
    }
}
