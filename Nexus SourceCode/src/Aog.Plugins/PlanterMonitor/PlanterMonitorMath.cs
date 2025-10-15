using System;
using Aog.Core.V1;

namespace Aog.Plugins.PlanterMonitor;

/// <summary>
/// Provides validation and evaluation helpers shared by the planter monitor components.
/// </summary>
internal static class PlanterMonitorMath
{
    /// <summary>
    /// Validates a measurement against the supplied options.
    /// </summary>
    /// <param name="options">Planter monitor options.</param>
    /// <param name="measurement">Measurement to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the row index or population values are outside supported bounds.</exception>
    public static void ValidateMeasurement(PlanterMonitorOptions options, RowPopulationMeasurement measurement)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (measurement.RowIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(measurement.RowIndex), measurement.RowIndex, "Row index cannot be negative.");
        }

        if (options.RowCount > 0 && measurement.RowIndex >= options.RowCount)
        {
            throw new ArgumentOutOfRangeException(nameof(measurement.RowIndex), measurement.RowIndex, "Row index exceeds configured row count.");
        }

        ValidatePopulation(measurement.TargetPopulationPerMeter, nameof(measurement.TargetPopulationPerMeter));
        ValidatePopulation(measurement.ActualPopulationPerMeter, nameof(measurement.ActualPopulationPerMeter));
    }

    /// <summary>
    /// Evaluates the supplied population values to determine skip/double rates and quality classification.
    /// </summary>
    /// <param name="options">Evaluation thresholds.</param>
    /// <param name="targetPopulationPerMeter">Target population in seeds per meter.</param>
    /// <param name="actualPopulationPerMeter">Measured population in seeds per meter.</param>
    /// <returns>A tuple describing skip rate, double rate, and quality.</returns>
    public static (double SkipRate, double DoubleRate, PlanterRowQuality Quality) Evaluate(
        PlanterMonitorOptions options,
        double targetPopulationPerMeter,
        double actualPopulationPerMeter)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (targetPopulationPerMeter <= 0 || double.IsNaN(targetPopulationPerMeter))
        {
            return (0d, 0d, PlanterRowQuality.Unknown);
        }

        if (double.IsNaN(actualPopulationPerMeter))
        {
            return (0d, 0d, PlanterRowQuality.Unknown);
        }

        var ratio = actualPopulationPerMeter / targetPopulationPerMeter;
        var deviation = ratio - 1d;

        if (double.IsNaN(deviation))
        {
            return (0d, 0d, PlanterRowQuality.Unknown);
        }

        if (deviation <= -options.SkipThreshold)
        {
            var relative = Math.Clamp((-deviation) / options.SkipThreshold, 0d, 1d);
            return (relative, 0d, PlanterRowQuality.Skip);
        }

        if (deviation >= options.DoubleThreshold)
        {
            var relative = Math.Clamp(deviation / options.DoubleThreshold, 0d, 1d);
            return (0d, relative, PlanterRowQuality.Double);
        }

        return (0d, 0d, PlanterRowQuality.Ok);
    }

    private static void ValidatePopulation(double value, string parameterName)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) || value < 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "Population values must be finite and non-negative.");
        }
    }
}
