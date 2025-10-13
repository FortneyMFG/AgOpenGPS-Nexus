using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Aog.Tools.Qa.Faults;

public sealed class FaultInjectionRunner
{
    public FaultInjectionSchedule BuildSchedule(string scenarioPath)
    {
        if (string.IsNullOrWhiteSpace(scenarioPath))
        {
            throw new ArgumentException("Scenario path is required.", nameof(scenarioPath));
        }

        if (!File.Exists(scenarioPath))
        {
            throw new FileNotFoundException("Scenario file not found.", scenarioPath);
        }

        using var stream = File.OpenRead(scenarioPath);
        var scenario = JsonSerializer.Deserialize<FaultInjectionScenario>(stream, Serialization.Options)
            ?? throw new InvalidOperationException("Failed to deserialize fault injection scenario.");

        ValidateScenario(scenario);

        var rng = new Random(scenario.Seed ?? 0);
        var events = new List<ScheduledFaultInjectionEvent>();

        foreach (var fault in scenario.Events)
        {
            var jitter = Math.Max(fault.JitterSeconds ?? 0, 0);
            var offset = fault.OffsetSeconds + (jitter > 0 ? (rng.NextDouble() * 2 - 1) * jitter : 0);
            offset = Math.Max(offset, 0);

            var duration = Math.Max(fault.DurationSeconds, 0);
            var start = TimeSpan.FromSeconds(offset);
            var length = TimeSpan.FromSeconds(duration);

            events.Add(new ScheduledFaultInjectionEvent(
                fault.Type,
                fault.Target,
                start,
                length,
                Math.Clamp(fault.Severity, 0, 1),
                fault.Notes));
        }

        events.Sort((a, b) => a.StartOffset.CompareTo(b.StartOffset));
        return new FaultInjectionSchedule(scenario.Name, events);
    }

    private static void ValidateScenario(FaultInjectionScenario scenario)
    {
        if (string.IsNullOrWhiteSpace(scenario.Name))
        {
            throw new InvalidOperationException("Scenario name is required.");
        }

        if (scenario.Events.Count == 0)
        {
            throw new InvalidOperationException("Scenario must include at least one event.");
        }

        foreach (var fault in scenario.Events)
        {
            if (string.IsNullOrWhiteSpace(fault.Type))
            {
                throw new InvalidOperationException("Fault events require a type.");
            }

            if (string.IsNullOrWhiteSpace(fault.Target))
            {
                throw new InvalidOperationException($"Fault '{fault.Type}' requires a target.");
            }

            if (fault.DurationSeconds <= 0)
            {
                throw new InvalidOperationException($"Fault '{fault.Type}' must have a positive duration.");
            }
        }
    }
}

public sealed class FaultInjectionScenario
{
    public string Name { get; set; } = string.Empty;
    public int? Seed { get; set; }

    public IReadOnlyList<FaultInjectionEvent> Events { get; set; } = Array.Empty<FaultInjectionEvent>();
}

public sealed class FaultInjectionEvent
{
    public string Type { get; set; } = string.Empty;
    public string Target { get; set; } = string.Empty;
    public double OffsetSeconds { get; set; }

    public double DurationSeconds { get; set; }

    public double Severity { get; set; } = 1.0;
    public double? JitterSeconds { get; set; }

    public string? Notes { get; set; }

}

public sealed record ScheduledFaultInjectionEvent(
    string Type,
    string Target,
    TimeSpan StartOffset,
    TimeSpan Duration,
    double Severity,
    string? Notes);

public sealed record FaultInjectionSchedule(string ScenarioName, IReadOnlyList<ScheduledFaultInjectionEvent> Events)
{
    public TimeSpan TotalDuration => Events.Count == 0
        ? TimeSpan.Zero
        : Events.Max(e => e.StartOffset + e.Duration);
}