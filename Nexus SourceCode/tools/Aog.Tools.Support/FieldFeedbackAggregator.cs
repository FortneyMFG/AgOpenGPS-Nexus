using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Aog.Tools.Support;

public sealed class FieldFeedbackAggregator
{
    public static JsonSerializerOptions SerializerOptions { get; } = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
    };

    public FieldFeedbackReport Aggregate(string inputDirectory, int? windowDays, DateTimeOffset? now)
    {
        if (string.IsNullOrWhiteSpace(inputDirectory))
        {
            throw new ArgumentException("Input directory is required.", nameof(inputDirectory));
        }

        if (!Directory.Exists(inputDirectory))
        {
            throw new DirectoryNotFoundException($"Input directory '{inputDirectory}' was not found.");
        }

        var referenceTime = now ?? DateTimeOffset.UtcNow;
        var windowStart = windowDays is > 0 ? referenceTime - TimeSpan.FromDays(windowDays.Value) : (DateTimeOffset?)null;

        var events = Directory.EnumerateFiles(inputDirectory, "*.json*", SearchOption.TopDirectoryOnly)
            .SelectMany(ReadEvents)
            .Where(evt => windowStart is null || evt.Timestamp >= windowStart)
            .ToList();

        var builds = events
            .GroupBy(evt => Normalize(evt.Build, "unknown"), StringComparer.OrdinalIgnoreCase)
            .Select(group => new BuildSummary(group.Key, group.Count()))
            .OrderByDescending(summary => summary.Count)
            .ThenBy(summary => summary.Build, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var components = events
            .GroupBy(evt => Normalize(evt.Component, "general"), StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var counts = group.Aggregate((Total: 0, Critical: 0, Warning: 0), (state, evt) =>
                {
                    state.Total++;
                    switch (NormalizeSeverity(evt.Severity))
                    {
                        case SeverityLevel.Critical:
                            state.Critical++;
                            break;
                        case SeverityLevel.Warning:
                            state.Warning++;
                            break;
                    }

                    return state;
                });

                var informational = counts.Total - counts.Critical - counts.Warning;
                return new ComponentSummary(group.Key, counts.Total, counts.Critical, counts.Warning, informational);
            })
            .OrderByDescending(summary => summary.Total)
            .ThenBy(summary => summary.Component, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var issues = events
            .GroupBy(evt => Normalize(evt.Event, "unspecified"), StringComparer.OrdinalIgnoreCase)
            .Select(group => new IssueSummary(
                group.Key,
                group.Count(),
                group.Where(evt => !string.IsNullOrWhiteSpace(evt.Notes))
                     .Select(evt => evt.Notes.Trim())
                     .Distinct()
                     .Take(5)
                     .ToList()))
            .OrderByDescending(summary => summary.Count)
            .ThenBy(summary => summary.Event, StringComparer.OrdinalIgnoreCase)
            .Take(10)
            .ToList();

        var uniqueMachines = events
            .Select(evt => evt.TractorId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

        return new FieldFeedbackReport(events.Count, uniqueMachines, windowStart, builds, components, issues);
    }

    private static IEnumerable<FieldFeedbackEvent> ReadEvents(string path)
    {
        if (string.Equals(Path.GetExtension(path), ".jsonl", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var evt in ReadJsonLines(path))
            {
                yield return evt;
            }

            yield break;
        }

        using var stream = File.OpenRead(path);
        FieldFeedbackEvent[]? array = null;
        try
        {
            array = JsonSerializer.Deserialize<FieldFeedbackEvent[]>(stream, SerializerOptions);
        }
        catch (JsonException)
        {
            // Swallow and continue with other files.
        }

        if (array is null)
        {
            yield break;
        }

        foreach (var evt in array)
        {
            if (evt is not null)
            {
                yield return evt;
            }
        }
    }

    private static IReadOnlyList<FieldFeedbackEvent> ReadJsonLines(string path)
    {
        var bytes = File.ReadAllBytes(path);
        if (bytes.Length == 0)
        {
            return Array.Empty<FieldFeedbackEvent>();
        }

        var reader = new Utf8JsonReader(bytes, new JsonReaderOptions { AllowTrailingCommas = true });
        var events = new List<FieldFeedbackEvent>();
        while (reader.Read())
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                continue;
            }

            FieldFeedbackEvent? evt = null;
            try
            {
                evt = JsonSerializer.Deserialize<FieldFeedbackEvent>(ref reader, SerializerOptions);
            }
            catch (JsonException)
            {
                // Skip malformed payloads so a single bad upload does not break aggregation.
            }

            if (evt is not null)
            {
                events.Add(evt);
            }
        }

        return events;
    }

    private static SeverityLevel NormalizeSeverity(string? severity)
    {
        return severity?.Trim().ToLowerInvariant() switch
        {
            "critical" or "high" => SeverityLevel.Critical,
            "warn" or "warning" or "medium" => SeverityLevel.Warning,
            _ => SeverityLevel.Informational,
        };
    }

    private static string Normalize(string? value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }

    private enum SeverityLevel
    {
        Informational,
        Warning,
        Critical,
    }
}

public sealed class FieldFeedbackEvent
{
    public DateTimeOffset Timestamp { get; set; }
    public string TractorId { get; set; } = string.Empty;
    public string Build { get; set; } = string.Empty;
    public string Component { get; set; } = string.Empty;
    public string Event { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public Dictionary<string, string>? Tags { get; set; }
}

public sealed record FieldFeedbackReport(
    int TotalEvents,
    int UniqueMachines,
    DateTimeOffset? WindowStartUtc,
    IReadOnlyList<BuildSummary> Builds,
    IReadOnlyList<ComponentSummary> Components,
    IReadOnlyList<IssueSummary> TopIssues);

public sealed record BuildSummary(string Build, int Count);

public sealed record ComponentSummary(string Component, int Total, int Critical, int Warning, int Informational);

public sealed record IssueSummary(string Event, int Count, IReadOnlyList<string> SampleNotes);
