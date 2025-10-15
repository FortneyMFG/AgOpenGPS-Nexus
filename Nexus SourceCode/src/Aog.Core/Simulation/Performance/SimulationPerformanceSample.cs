using System;

namespace Aog.Core.Simulation.Performance;

/// <summary>
/// Immutable performance snapshot captured from a synthetic simulation run. The sample
/// is consumed by budget evaluators and telemetry exporters to spot regressions.
/// </summary>
/// <param name="ScenarioId">Identifier of the scenario that was executed.</param>
/// <param name="Iterations">Number of iterations that completed.</param>
/// <param name="ProviderCount">Number of providers participating in the graph.</param>
/// <param name="TotalMessages">Total number of messages published during the run.</param>
/// <param name="TotalOutputs">Number of declared provider outputs in the graph.</param>
/// <param name="Elapsed">Wall-clock duration measured for the run.</param>
/// <param name="Checksum">Deterministic checksum derived from the synthetic payloads.</param>
public sealed record SimulationPerformanceSample(
    string ScenarioId,
    int Iterations,
    int ProviderCount,
    int TotalMessages,
    int TotalOutputs,
    TimeSpan Elapsed,
    double Checksum)
{
    /// <summary>
    /// Average number of messages produced per iteration.
    /// </summary>
    public double MessagesPerIteration => Iterations == 0 ? 0d : (double)TotalMessages / Iterations;

    /// <summary>
    /// Average duration of a single iteration.
    /// </summary>
    public TimeSpan AverageIterationDuration
        => Iterations == 0 ? TimeSpan.Zero : TimeSpan.FromTicks(Elapsed.Ticks / Iterations);
}
