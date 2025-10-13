using System;

namespace Aog.Plugins.CombineYield;

/// <summary>
/// Represents a single instantaneous yield measurement emitted by a combine harvester.
/// </summary>
/// <param name="EastingMeters">Local east offset from the field origin in meters.</param>
/// <param name="NorthingMeters">Local north offset from the field origin in meters.</param>
/// <param name="YieldKgPerHectare">Instantaneous yield rate scaled to kilograms per hectare.</param>
/// <param name="MoisturePercent">Optional crop moisture percentage (0-100).</param>
public readonly record struct CombineYieldMeasurement(
    double EastingMeters,
    double NorthingMeters,
    double YieldKgPerHectare,
    double? MoisturePercent = null)
{
    /// <summary>
    /// Validates the measurement values.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when a value is outside the acceptable range.</exception>
    public void Validate()
    {
        if (double.IsNaN(EastingMeters) || double.IsInfinity(EastingMeters) || EastingMeters < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(EastingMeters), EastingMeters, "Easting must be a finite, non-negative value in meters.");
        }

        if (double.IsNaN(NorthingMeters) || double.IsInfinity(NorthingMeters) || NorthingMeters < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(NorthingMeters), NorthingMeters, "Northing must be a finite, non-negative value in meters.");
        }

        if (double.IsNaN(YieldKgPerHectare) || double.IsInfinity(YieldKgPerHectare) || YieldKgPerHectare < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(YieldKgPerHectare), YieldKgPerHectare, "Yield must be a finite, non-negative value.");
        }

        if (MoisturePercent.HasValue)
        {
            var moisture = MoisturePercent.Value;
            if (double.IsNaN(moisture) || double.IsInfinity(moisture) || moisture < 0 || moisture > 100)
            {
                throw new ArgumentOutOfRangeException(nameof(MoisturePercent), moisture, "Moisture must be between 0 and 100 percent when supplied.");
            }
        }
    }
}
