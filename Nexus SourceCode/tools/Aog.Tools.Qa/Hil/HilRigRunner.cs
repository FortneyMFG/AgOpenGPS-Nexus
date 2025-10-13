using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Aog.Tools.Qa.Hil;

public sealed class HilRigRunner
{
    public HilRigRunResult Run(string configurationPath)
    {
        if (string.IsNullOrWhiteSpace(configurationPath))
        {
            throw new ArgumentException("Configuration path is required.", nameof(configurationPath));
        }

        if (!File.Exists(configurationPath))
        {
            throw new FileNotFoundException("Configuration file not found.", configurationPath);
        }

        using var stream = File.OpenRead(configurationPath);
        var configuration = JsonSerializer.Deserialize<HilRigConfiguration>(stream, Serialization.Options)
            ?? throw new InvalidOperationException("Failed to deserialize HIL configuration.");

        var errors = ValidateConfiguration(configuration);
        if (errors.Count > 0)
        {
            throw new InvalidOperationException(string.Join(Environment.NewLine, errors));
        }

        var scenarioResults = new List<HilScenarioResult>();
        var configDirectory = Path.GetDirectoryName(Path.GetFullPath(configurationPath)) ?? Environment.CurrentDirectory;

        foreach (var scenario in configuration.Scenarios)
        {
            var samplePath = Path.Combine(configDirectory, scenario.SampleData);
            if (!File.Exists(samplePath))
            {
                throw new FileNotFoundException($"Sample data for scenario '{scenario.Name}' not found.", samplePath);
            }

            using var sampleStream = File.OpenRead(samplePath);
            var sample = JsonSerializer.Deserialize<HilSampleData>(sampleStream, Serialization.Options)
                ?? new HilSampleData();

            var metricLookup = sample.Metrics.ToDictionary(m => m.Name, StringComparer.OrdinalIgnoreCase);
            var assertionResults = new List<HilAssertionResult>();

            foreach (var assertion in scenario.Assertions)
            {
                if (!metricLookup.TryGetValue(assertion.Metric, out var metric))
                {
                    assertionResults.Add(new HilAssertionResult(assertion.Metric, null, false, "Metric missing from sample data."));
                    continue;
                }

                var passed = true;
                string? failure = null;

                if (assertion.Min is not null && metric.Value < assertion.Min)
                {
                    passed = false;
                    failure = $"Value {metric.Value} is below minimum {assertion.Min}.";
                }

                if (assertion.Max is not null && metric.Value > assertion.Max)
                {
                    passed = false;
                    var suffix = failure is null ? string.Empty : " ";
                    failure = $"{failure}{suffix}Value {metric.Value} exceeds maximum {assertion.Max}.".Trim();
                }

                assertionResults.Add(new HilAssertionResult(assertion.Metric, metric.Value, passed, failure));
            }

            scenarioResults.Add(new HilScenarioResult(
                scenario.Name,
                TimeSpan.FromSeconds(Math.Max(scenario.DurationSeconds, 0)),
                assertionResults));
        }

        return new HilRigRunResult(configuration.RigName, configuration.ControllerEndpoint, configuration.StreamBindings, scenarioResults);
    }

    private static List<string> ValidateConfiguration(HilRigConfiguration configuration)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(configuration.RigName))
        {
            errors.Add("Rig name is required.");
        }

        if (string.IsNullOrWhiteSpace(configuration.ControllerEndpoint))
        {
            errors.Add("Controller endpoint is required.");
        }

        if (configuration.StreamBindings.Count == 0)
        {
            errors.Add("At least one stream binding is required.");
        }

        if (configuration.Scenarios.Count == 0)
        {
            errors.Add("At least one scenario must be defined.");
        }

        foreach (var scenario in configuration.Scenarios)
        {
            if (string.IsNullOrWhiteSpace(scenario.Name))
            {
                errors.Add("Scenario name is required.");
            }

            if (scenario.Assertions.Count == 0)
            {
                errors.Add($"Scenario '{scenario.Name}' must define assertions.");
            }

            if (string.IsNullOrWhiteSpace(scenario.SampleData))
            {
                errors.Add($"Scenario '{scenario.Name}' must reference sample data.");
            }
        }

        return errors;
    }

    private sealed class HilSampleData
    {
        public IReadOnlyList<HilSampleMetric> Metrics { get; init; } = Array.Empty<HilSampleMetric>();
    }

    private sealed class HilSampleMetric
    {
        public string Name { get; init; } = string.Empty;
        public double Value { get; init; }

    }
}

public sealed record HilRigRunResult(
    string RigName,
    string ControllerEndpoint,
    IReadOnlyList<string> StreamBindings,
    IReadOnlyList<HilScenarioResult> Scenarios)
{
    public bool Passed => Scenarios.All(s => s.Passed);
}

public sealed record HilScenarioResult(string Name, TimeSpan Duration, IReadOnlyList<HilAssertionResult> Assertions)
{
    public bool Passed => Assertions.All(a => a.Passed);
}

public sealed record HilAssertionResult(string Metric, double? Actual, bool Passed, string? FailureReason);
