using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Aog.Tools.Qa.Dashboard;

public sealed class QaDashboardAggregator
{
    public QaDashboard Aggregate(string metricsDirectory)
    {
        if (string.IsNullOrWhiteSpace(metricsDirectory))
        {
            throw new ArgumentException("Metrics directory is required.", nameof(metricsDirectory));
        }

        if (!Directory.Exists(metricsDirectory))
        {
            throw new DirectoryNotFoundException($"Metrics directory '{metricsDirectory}' was not found.");
        }

        var scenarioSummaries = new List<QaScenarioSummary>();
        var aggregates = new Dictionary<string, MetricAccumulator>(StringComparer.OrdinalIgnoreCase);

        foreach (var file in Directory.EnumerateFiles(metricsDirectory, "*.json", SearchOption.TopDirectoryOnly))
        {
            using var stream = File.OpenRead(file);
            var set = JsonSerializer.Deserialize<QaMetricSet>(stream, Serialization.Options);
            if (set is null)
            {
                continue;
            }

            var metrics = set.Metrics ?? Array.Empty<QaMetric>();
            var status = DetermineScenarioStatus(metrics);
            scenarioSummaries.Add(new QaScenarioSummary(set.Scenario, set.Source, status, metrics));

            foreach (var metric in metrics)
            {
                var key = $"{metric.Category}|{metric.Name}|{metric.Unit}".ToLowerInvariant();
                if (!aggregates.TryGetValue(key, out var accumulator))
                {
                    accumulator = new MetricAccumulator(metric.Name, metric.Category, metric.Unit);
                    aggregates[key] = accumulator;
                }

                accumulator.Add(metric.Value, metric.Status);
            }
        }

        scenarioSummaries.Sort((a, b) => string.Compare(a.Scenario, b.Scenario, StringComparison.OrdinalIgnoreCase));

        var metricAggregates = aggregates.Values
            .Select(a => a.ToAggregate())
            .OrderBy(a => a.Category, StringComparer.OrdinalIgnoreCase)
            .ThenBy(a => a.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var allPassed = scenarioSummaries.All(s => string.Equals(s.Status, "pass", StringComparison.OrdinalIgnoreCase));

        return new QaDashboard(scenarioSummaries, metricAggregates, allPassed);
    }

    private static string DetermineScenarioStatus(IEnumerable<QaMetric> metrics)
    {
        var status = "pass";
        foreach (var metric in metrics)
        {
            if (string.Equals(metric.Status, "fail", StringComparison.OrdinalIgnoreCase))
            {
                return "fail";
            }

            if (string.Equals(metric.Status, "warn", StringComparison.OrdinalIgnoreCase))
            {
                status = "warn";
            }
        }

        return status;
    }

    private sealed class MetricAccumulator
    {
        private readonly List<double> _values = new();
        private int _passCount;
        private int _warnCount;
        private int _failCount;

        public MetricAccumulator(string name, string category, string unit)
        {
            Name = string.IsNullOrWhiteSpace(name) ? "unknown" : name;
            Category = string.IsNullOrWhiteSpace(category) ? "general" : category;
            Unit = unit ?? string.Empty;
        }

        public string Name { get; }
        public string Category { get; }
        public string Unit { get; }

        public void Add(double value, string? status)
        {
            _values.Add(value);

            if (string.Equals(status, "fail", StringComparison.OrdinalIgnoreCase))
            {
                _failCount++;
            }
            else if (string.Equals(status, "warn", StringComparison.OrdinalIgnoreCase))
            {
                _warnCount++;
            }
            else
            {
                _passCount++;
            }
        }

        public QaMetricAggregate ToAggregate()
        {
            var average = _values.Count > 0 ? _values.Average() : 0;
            var min = _values.Count > 0 ? _values.Min() : 0;
            var max = _values.Count > 0 ? _values.Max() : 0;
            return new QaMetricAggregate(Name, Category, Unit, average, min, max, _values.Count, _passCount, _warnCount, _failCount);
        }
    }
}

public sealed class QaMetricSet
{
    public string Scenario { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public IReadOnlyList<QaMetric> Metrics { get; set; } = Array.Empty<QaMetric>();
}

public sealed class QaMetric
{
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public double Value { get; set; }

    public string Unit { get; set; } = string.Empty;
    public string Status { get; set; } = "pass";
}

public sealed record QaDashboard(
    IReadOnlyList<QaScenarioSummary> Scenarios,
    IReadOnlyList<QaMetricAggregate> Metrics,
    bool AllPassed);

public sealed record QaScenarioSummary(string Scenario, string Source, string Status, IReadOnlyList<QaMetric> Metrics);

public sealed record QaMetricAggregate(
    string Name,
    string Category,
    string Unit,
    double Average,
    double Minimum,
    double Maximum,
    int Samples,
    int PassCount,
    int WarnCount,
    int FailCount);
