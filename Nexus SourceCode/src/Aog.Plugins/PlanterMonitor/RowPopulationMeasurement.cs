namespace Aog.Plugins.PlanterMonitor;

/// <summary>
/// Represents a single measurement of planter population for a specific row.
/// </summary>
/// <param name="RowIndex">Zero-based row index.</param>
/// <param name="TargetPopulationPerMeter">Target seed population (seeds per meter).</param>
/// <param name="ActualPopulationPerMeter">Measured seed population (seeds per meter).</param>
public readonly record struct RowPopulationMeasurement(int RowIndex, double TargetPopulationPerMeter, double ActualPopulationPerMeter);
