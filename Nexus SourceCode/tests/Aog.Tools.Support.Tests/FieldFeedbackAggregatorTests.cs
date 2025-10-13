using System;
using System.IO;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace Aog.Tools.Support.Tests;

public sealed class FieldFeedbackAggregatorTests
{
    [Fact]
    public void Aggregate_summarizes_feedback_events()
    {
        using var temp = new TempDirectory();
        var path = Path.Combine(temp.Path, "events.jsonl");
        File.WriteAllText(path, string.Join('\n',
            JsonSerializer.Serialize(new FieldFeedbackEvent
            {
                Timestamp = new DateTimeOffset(2024, 4, 12, 15, 30, 0, TimeSpan.Zero),
                TractorId = "tractor-001",
                Build = "1.0.5",
                Component = "Guidance",
                Event = "ab-line-drift",
                Severity = "warning",
                Notes = "Operators saw AB line drift on slope.",
            }, FieldFeedbackAggregator.SerializerOptions),
            JsonSerializer.Serialize(new FieldFeedbackEvent
            {
                Timestamp = new DateTimeOffset(2024, 4, 12, 16, 5, 0, TimeSpan.Zero),
                TractorId = "tractor-002",
                Build = "1.0.4",
                Component = "Sections",
                Event = "section-stutter",
                Severity = "critical",
                Notes = "Outer boom latched off twice.",
            }, FieldFeedbackAggregator.SerializerOptions),
            JsonSerializer.Serialize(new FieldFeedbackEvent
            {
                Timestamp = new DateTimeOffset(2024, 4, 13, 10, 15, 0, TimeSpan.Zero),
                TractorId = "tractor-001",
                Build = "1.0.5",
                Component = "Guidance",
                Event = "ab-line-drift",
                Severity = "info",
                Notes = "Recovered after recalibration.",
            }, FieldFeedbackAggregator.SerializerOptions)));

        var aggregator = new FieldFeedbackAggregator();
        var report = aggregator.Aggregate(temp.Path, windowDays: null, now: new DateTimeOffset(2024, 4, 13, 12, 0, 0, TimeSpan.Zero));

        report.TotalEvents.Should().Be(3);
        report.UniqueMachines.Should().Be(2);
        report.Builds.Should().ContainEquivalentOf(new BuildSummary("1.0.5", 2));
        report.Builds.Should().ContainEquivalentOf(new BuildSummary("1.0.4", 1));

        report.Components.Should().ContainEquivalentOf(new ComponentSummary("Guidance", 2, 0, 1, 1));
        report.Components.Should().ContainEquivalentOf(new ComponentSummary("Sections", 1, 1, 0, 0));

        report.TopIssues.Should().ContainSingle(i => i.Event == "ab-line-drift" && i.Count == 2);
        report.TopIssues.Should().ContainSingle(i => i.Event == "section-stutter" && i.Count == 1);
    }

    [Fact]
    public void Aggregate_respects_window_days()
    {
        using var temp = new TempDirectory();
        var file = Path.Combine(temp.Path, "feedback.jsonl");
        File.WriteAllText(file, string.Join('\n',
            JsonSerializer.Serialize(new FieldFeedbackEvent
            {
                Timestamp = new DateTimeOffset(2024, 3, 20, 9, 0, 0, TimeSpan.Zero),
                TractorId = "tractor-003",
                Build = "1.0.3",
                Component = "Telemetry",
                Event = "upload-failed",
                Severity = "warning",
            }, FieldFeedbackAggregator.SerializerOptions),
            JsonSerializer.Serialize(new FieldFeedbackEvent
            {
                Timestamp = new DateTimeOffset(2024, 4, 19, 9, 0, 0, TimeSpan.Zero),
                TractorId = "tractor-004",
                Build = "1.0.6",
                Component = "Telemetry",
                Event = "upload-failed",
                Severity = "warning",
            }, FieldFeedbackAggregator.SerializerOptions)));

        var aggregator = new FieldFeedbackAggregator();
        var report = aggregator.Aggregate(temp.Path, windowDays: 7, now: new DateTimeOffset(2024, 4, 20, 0, 0, 0, TimeSpan.Zero));

        report.TotalEvents.Should().Be(1);
        report.Builds.Should().ContainSingle(b => b.Build == "1.0.6" && b.Count == 1);
        report.Components.Should().ContainSingle(c => c.Component == "Telemetry" && c.Total == 1);
    }

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(Path))
                {
                    Directory.Delete(Path, recursive: true);
                }
            }
            catch
            {
                // Ignore cleanup exceptions.
            }
        }
    }
}
