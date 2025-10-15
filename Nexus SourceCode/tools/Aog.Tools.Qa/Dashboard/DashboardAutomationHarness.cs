using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace Aog.Tools.Qa.Dashboard;

/// <summary>
///     Evaluates dashboard automation logs against a declarative widget specification and
///     emits QA metrics that can be consumed by the aggregator and report generator.
/// </summary>
public sealed class DashboardAutomationHarness
{
    /// <summary>
    ///     Executes the harness using the supplied request. When an output directory is provided the
    ///     resulting metric set is written to disk so downstream tooling can pick it up automatically.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when required request properties are missing.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the specification or observations file is missing.</exception>
    public DashboardHarnessResult Run(DashboardHarnessRequest request)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.SpecPath))
        {
            throw new ArgumentException("Specification path is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.ObservationPath))
        {
            throw new ArgumentException("Observation path is required.", nameof(request));
        }

        if (!File.Exists(request.SpecPath))
        {
            throw new FileNotFoundException("Specification file not found.", request.SpecPath);
        }

        if (!File.Exists(request.ObservationPath))
        {
            throw new FileNotFoundException("Observation file not found.", request.ObservationPath);
        }

        using var specStream = File.OpenRead(request.SpecPath);
        var spec = JsonSerializer.Deserialize<DashboardHarnessSpec>(specStream, Serialization.Options)
            ?? throw new InvalidOperationException("Failed to deserialize dashboard specification.");

        using var observationStream = File.OpenRead(request.ObservationPath);
        var observations = JsonSerializer.Deserialize<DashboardObservationLog>(observationStream, Serialization.Options)
            ?? throw new InvalidOperationException("Failed to deserialize dashboard observations.");

        var findings = new List<DashboardHarnessFinding>();
        var metrics = new List<QaMetric>();

        var scenarioName = FirstNonEmpty(
            request.ScenarioOverride,
            spec.Scenario,
            observations.Scenario,
            Path.GetFileNameWithoutExtension(request.SpecPath),
            "Dashboard scenario");

        var sourceName = FirstNonEmpty(observations.Source, "dashboard-harness");

        if (!string.IsNullOrWhiteSpace(spec.Scenario) &&
            !string.IsNullOrWhiteSpace(observations.Scenario) &&
            !string.Equals(spec.Scenario, observations.Scenario, StringComparison.OrdinalIgnoreCase))
        {
            findings.Add(new DashboardHarnessFinding(
                DashboardHarnessFindingSeverity.Warning,
                $"Scenario mismatch between spec ('{spec.Scenario}') and observations ('{observations.Scenario}')."));
        }

        var observationLookup = observations.Samples
            .GroupBy(s => CreateLookupKey(s.WidgetId, s.Series), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        foreach (var widget in spec.Widgets ?? Array.Empty<DashboardHarnessWidget>())
        {
            var widgetId = FirstNonEmpty(widget.Id, widget.DisplayName, "widget");
            foreach (var series in widget.Series ?? Array.Empty<DashboardHarnessSeries>())
            {
                var seriesName = FirstNonEmpty(series.Name, "series");
                var lookupKey = CreateLookupKey(widget.Id, series.Name);
                observationLookup.TryGetValue(lookupKey, out var observedSamples);

                var sample = observedSamples?.OrderByDescending(s => s.SampleCount).FirstOrDefault();
                var status = DetermineStatus(sample?.Status);
                double value;

                if (sample is null)
                {
                    value = 0;
                    status = MergeStatuses(status, series.Required ? MetricStatus.Fail : MetricStatus.Warn);
                    var severity = series.Required
                        ? DashboardHarnessFindingSeverity.Error
                        : DashboardHarnessFindingSeverity.Warning;
                    findings.Add(new DashboardHarnessFinding(
                        severity,
                        $"Missing observation for widget '{widgetId}' series '{seriesName}'."));
                }
                else
                {
                    value = Math.Max(0, sample.SampleCount);

                    if (series.MinSampleCount is not null && value < series.MinSampleCount)
                    {
                        status = MergeStatuses(status, series.Required ? MetricStatus.Fail : MetricStatus.Warn);
                        findings.Add(new DashboardHarnessFinding(
                            series.Required ? DashboardHarnessFindingSeverity.Error : DashboardHarnessFindingSeverity.Warning,
                            $"Widget '{widgetId}' series '{seriesName}' produced {value} samples below the required {series.MinSampleCount}."));
                    }
                }

                metrics.Add(new QaMetric
                {
                    Name = $"dashboard.widget.{SanitizeToken(widgetId)}.{SanitizeToken(seriesName)}",
                    Category = "dashboard",
                    Value = value,
                    Unit = "samples",
                    Status = status.ToString().ToLowerInvariant()
                });
            }
        }

        var metricSet = new QaMetricSet
        {
            Scenario = scenarioName,
            Source = sourceName,
            Metrics = metrics
        };

        if (!string.IsNullOrWhiteSpace(request.OutputDirectory))
        {
            var outputDirectory = Path.GetFullPath(request.OutputDirectory);
            Directory.CreateDirectory(outputDirectory);

            var fileName = FirstNonEmpty(request.OutputFileName, SanitizeFileName($"{scenarioName}.json"), "dashboard-metrics.json");
            if (!fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                fileName += ".json";
            }

            var outputPath = Path.Combine(outputDirectory, fileName);
            File.WriteAllText(outputPath, JsonSerializer.Serialize(metricSet, Serialization.Options));
        }

        return new DashboardHarnessResult(metricSet, findings);
    }

    private static string FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return string.Empty;
    }

    private static string CreateLookupKey(string? widgetId, string? series)
    {
        return $"{widgetId ?? string.Empty}|{series ?? string.Empty}".ToLowerInvariant();
    }

    private static string SanitizeToken(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var character in value)
        {
            if (char.IsLetterOrDigit(character) || character is '.' or '-' or '_')
            {
                builder.Append(char.ToLowerInvariant(character));
            }
            else if (!char.IsWhiteSpace(character))
            {
                builder.Append('_');
            }
        }

        var sanitized = builder.ToString().Trim('_');
        return string.IsNullOrWhiteSpace(sanitized) ? "value" : sanitized;
    }

    private static string SanitizeFileName(string value)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var builder = new StringBuilder(value.Length);
        foreach (var character in value)
        {
            builder.Append(invalidChars.Contains(character) ? '_' : character);
        }

        return builder.ToString();
    }

    private static MetricStatus DetermineStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return MetricStatus.Pass;
        }

        return status.ToLowerInvariant() switch
        {
            "fail" => MetricStatus.Fail,
            "warn" or "warning" => MetricStatus.Warn,
            _ => MetricStatus.Pass
        };
    }

    private enum MetricStatus
    {
        Pass = 0,
        Warn = 1,
        Fail = 2
    }

    private static MetricStatus MergeStatuses(MetricStatus current, MetricStatus other)
    {
        return (MetricStatus)Math.Max((int)current, (int)other);
    }
}

public sealed class DashboardHarnessRequest
{
    public string SpecPath { get; init; } = string.Empty;
    public string ObservationPath { get; init; } = string.Empty;
    public string? ScenarioOverride { get; init; }
    public string? OutputDirectory { get; init; }
    public string? OutputFileName { get; init; }
}

public sealed record DashboardHarnessResult(QaMetricSet MetricSet, IReadOnlyList<DashboardHarnessFinding> Findings)
{
    public bool Passed => MetricSet.Metrics.All(m => !string.Equals(m.Status, "fail", StringComparison.OrdinalIgnoreCase)) &&
        Findings.All(f => f.Severity != DashboardHarnessFindingSeverity.Error);
}

public sealed record DashboardHarnessFinding(DashboardHarnessFindingSeverity Severity, string Message);

public enum DashboardHarnessFindingSeverity
{
    Info,
    Warning,
    Error
}

internal sealed class DashboardHarnessSpec
{
    public string Scenario { get; set; } = string.Empty;
    public IReadOnlyList<DashboardHarnessWidget> Widgets { get; set; } = Array.Empty<DashboardHarnessWidget>();
}

internal sealed class DashboardHarnessWidget
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public IReadOnlyList<DashboardHarnessSeries> Series { get; set; } = Array.Empty<DashboardHarnessSeries>();
}

internal sealed class DashboardHarnessSeries
{
    public string Name { get; set; } = string.Empty;
    public bool Required { get; set; } = true;
    public double? MinSampleCount { get; set; }
}

internal sealed class DashboardObservationLog
{
    public string Scenario { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public IReadOnlyList<DashboardObservationSample> Samples { get; set; } = Array.Empty<DashboardObservationSample>();
}

internal sealed class DashboardObservationSample
{
    public string WidgetId { get; set; } = string.Empty;
    public string Series { get; set; } = string.Empty;
    public double SampleCount { get; set; }
    public string Status { get; set; } = string.Empty;
}
