using System;
using System.IO;
using System.Text;
using System.Text.Json;
using Aog.Tools.Qa.Checklist;
using Aog.Tools.Qa.Dashboard;
using Aog.Tools.Qa.Faults;

namespace Aog.Tools.Qa.Reports;

public sealed class PostRunReportGenerator
{
    private readonly QaDashboardAggregator _aggregator = new();

    public string Generate(PostRunReportRequest request)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.MetricsDirectory))
        {
            throw new ArgumentException("Metrics directory is required.", nameof(request));
        }

        var dashboard = _aggregator.Aggregate(request.MetricsDirectory);
        var checklist = LoadChecklist(request.ChecklistPath);
        var schedule = LoadSchedule(request.FaultSchedulePath);

        var builder = new StringBuilder();
        builder.AppendLine("# Nexus safety run summary");
        builder.AppendLine();
        builder.AppendLine($"Generated: {DateTimeOffset.UtcNow:u}");
        builder.AppendLine();

        builder.AppendLine("## Scenario status");
        builder.AppendLine();
        builder.AppendLine("| Scenario | Source | Status |");
        builder.AppendLine("| --- | --- | --- |");
        foreach (var scenario in dashboard.Scenarios)
        {
            builder.AppendLine($"| {scenario.Scenario} | {scenario.Source} | {scenario.Status} |");
        }
        builder.AppendLine();

        builder.AppendLine("## Metric aggregates");
        builder.AppendLine();
        builder.AppendLine("| Category | Metric | Avg | Min | Max | Samples | Pass | Warn | Fail |");
        builder.AppendLine("| --- | --- | --- | --- | --- | --- | --- | --- | --- |");
        foreach (var metric in dashboard.Metrics)
        {
            builder.AppendLine($"| {metric.Category} | {metric.Name} ({metric.Unit}) | {metric.Average:F3} | {metric.Minimum:F3} | {metric.Maximum:F3} | {metric.Samples} | {metric.PassCount} | {metric.WarnCount} | {metric.FailCount} |");
        }
        builder.AppendLine();

        if (checklist is not null)
        {
            builder.AppendLine("## Field safety checklist");
            builder.AppendLine();
            builder.AppendLine($"Site: {checklist.SiteName}  ");
            builder.AppendLine($"Operator: {checklist.Operator}  ");
            builder.AppendLine($"Approver: {checklist.Approver}  ");
            builder.AppendLine($"Date: {checklist.Date:yyyy-MM-dd}");
            builder.AppendLine();

            foreach (var section in checklist.Sections)
            {
                builder.AppendLine($"### {section.Name}");
                builder.AppendLine();
                builder.AppendLine("| Item | Status | Verified by | Notes |");
                builder.AppendLine("| --- | --- | --- | --- |");
                foreach (var item in section.Items)
                {
                    var verified = string.IsNullOrWhiteSpace(item.VerifiedBy)
                        ? string.Empty
                        : $"{item.VerifiedBy} ({item.VerifiedAtUtc:u})";
                    var notes = string.IsNullOrWhiteSpace(item.Notes) ? string.Empty : item.Notes.Replace("\n", "<br/>");
                    builder.AppendLine($"| {item.Description} | {item.Status} | {verified} | {notes} |");
                }

                builder.AppendLine();
            }
        }

        if (schedule is not null)
        {
            builder.AppendLine("## Fault injection timeline");
            builder.AppendLine();
            builder.AppendLine("| Type | Target | Start (s) | Duration (s) | Severity | Notes |");
            builder.AppendLine("| --- | --- | --- | --- | --- | --- |");
            foreach (var evt in schedule.Events)
            {
                var notes = string.IsNullOrWhiteSpace(evt.Notes) ? string.Empty : evt.Notes.Replace("\n", "<br/>");
                builder.AppendLine($"| {evt.Type} | {evt.Target} | {evt.StartOffset.TotalSeconds:F1} | {evt.Duration.TotalSeconds:F1} | {evt.Severity:F2} | {notes} |");
            }

            builder.AppendLine();
        }

        builder.AppendLine($"Overall status: {(dashboard.AllPassed ? "PASS" : "ATTENTION REQUIRED")}");
        return builder.ToString();
    }

    private static FieldSafetyChecklist? LoadChecklist(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return null;
        }

        using var stream = File.OpenRead(path);
        return JsonSerializer.Deserialize<FieldSafetyChecklist>(stream, Serialization.Options);
    }

    private static FaultInjectionSchedule? LoadSchedule(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return null;
        }

        using var stream = File.OpenRead(path);
        return JsonSerializer.Deserialize<FaultInjectionSchedule>(stream, Serialization.Options);
    }
}

public sealed class PostRunReportRequest
{
    public string MetricsDirectory { get; set; } = string.Empty;
    public string? ChecklistPath { get; set; }

    public string? FaultSchedulePath { get; set; }

}
