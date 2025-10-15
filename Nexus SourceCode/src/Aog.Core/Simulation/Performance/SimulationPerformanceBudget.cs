using System;
using System.Collections.Generic;

namespace Aog.Core.Simulation.Performance;

/// <summary>
/// Declarative thresholds that the synthetic performance harness must satisfy. Budgets are
/// expressed as maxima/minima so CI and telemetry pipelines can flag regressions without
/// hard-coding limits in multiple locations.
/// </summary>
public sealed class SimulationPerformanceBudget
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SimulationPerformanceBudget"/> class.
    /// </summary>
    /// <param name="maxTotalDuration">Maximum wall-clock duration allowed for the run.</param>
    /// <param name="maxAverageIterationDuration">Maximum average iteration duration.</param>
    /// <param name="minMessagesPerIteration">Minimum number of messages produced per iteration.</param>
    /// <param name="maxMessagesPerIteration">Maximum number of messages produced per iteration.</param>
    public SimulationPerformanceBudget(
        TimeSpan? maxTotalDuration = null,
        TimeSpan? maxAverageIterationDuration = null,
        double? minMessagesPerIteration = null,
        double? maxMessagesPerIteration = null)
    {
        if (maxTotalDuration is { } total && total <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(maxTotalDuration));
        }

        if (maxAverageIterationDuration is { } iteration && iteration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(maxAverageIterationDuration));
        }

        if (minMessagesPerIteration is { } min && min < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(minMessagesPerIteration));
        }

        if (maxMessagesPerIteration is { } max && max < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxMessagesPerIteration));
        }

        if (minMessagesPerIteration is { } minimum && maxMessagesPerIteration is { } maximum && minimum > maximum)
        {
            throw new ArgumentException("Minimum messages per iteration cannot exceed the maximum.");
        }

        MaxTotalDuration = maxTotalDuration;
        MaxAverageIterationDuration = maxAverageIterationDuration;
        MinMessagesPerIteration = minMessagesPerIteration;
        MaxMessagesPerIteration = maxMessagesPerIteration;
    }

    /// <summary>Gets the maximum wall-clock duration allowed for the run.</summary>
    public TimeSpan? MaxTotalDuration { get; }

    /// <summary>Gets the maximum average iteration duration.</summary>
    public TimeSpan? MaxAverageIterationDuration { get; }

    /// <summary>Gets the minimum number of messages that should be produced per iteration.</summary>
    public double? MinMessagesPerIteration { get; }

    /// <summary>Gets the maximum number of messages that should be produced per iteration.</summary>
    public double? MaxMessagesPerIteration { get; }

    /// <summary>
    /// Evaluates the provided sample and returns the violations encountered.
    /// </summary>
    /// <param name="sample">Performance snapshot to evaluate.</param>
    /// <returns>Result describing whether the sample satisfied the configured thresholds.</returns>
    public SimulationPerformanceBudgetResult Evaluate(SimulationPerformanceSample sample)
    {
        ArgumentNullException.ThrowIfNull(sample);

        var violations = new List<string>();

        if (MaxTotalDuration is { } maxDuration && sample.Elapsed > maxDuration)
        {
            violations.Add($"elapsed {sample.Elapsed.TotalMilliseconds:F2} ms exceeds {maxDuration.TotalMilliseconds:F2} ms budget");
        }

        if (MaxAverageIterationDuration is { } maxIteration && sample.AverageIterationDuration > maxIteration)
        {
            violations.Add($"average iteration {sample.AverageIterationDuration.TotalMilliseconds:F4} ms exceeds {maxIteration.TotalMilliseconds:F4} ms budget");
        }

        if (MinMessagesPerIteration is { } minMessages && sample.MessagesPerIteration < minMessages)
        {
            violations.Add($"messages/iteration {sample.MessagesPerIteration:F2} fell below minimum {minMessages:F2}");
        }

        if (MaxMessagesPerIteration is { } maxMessages && sample.MessagesPerIteration > maxMessages)
        {
            violations.Add($"messages/iteration {sample.MessagesPerIteration:F2} exceeded maximum {maxMessages:F2}");
        }

        return new SimulationPerformanceBudgetResult(violations.Count == 0, violations);
    }
}

/// <summary>
/// Result emitted when comparing a performance sample against a configured budget.
/// </summary>
/// <param name="IsWithinBudget">Indicates whether all thresholds were satisfied.</param>
/// <param name="Violations">Human-readable list of violations when the budget was exceeded.</param>
public sealed record SimulationPerformanceBudgetResult(bool IsWithinBudget, IReadOnlyList<string> Violations)
{
    /// <summary>Gets whether the evaluation produced at least one violation.</summary>
    public bool HasViolations => !IsWithinBudget;
}
