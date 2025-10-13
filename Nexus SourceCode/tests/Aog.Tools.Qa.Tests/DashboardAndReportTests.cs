using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Aog.Tools.Qa.Dashboard;
using Aog.Tools.Qa.Faults;
using Aog.Tools.Qa.Reports;
using FluentAssertions;
using Xunit;

namespace Aog.Tools.Qa.Tests;

public static class DashboardAndReportTests
{
    [Fact]
    public static void Aggregator_combines_metric_sets()
    {
        var metricsDir = Resolve("metrics");
        var aggregator = new QaDashboardAggregator();
        var dashboard = aggregator.Aggregate(metricsDir);

        dashboard.AllPassed.Should().BeFalse();
        dashboard.Scenarios.Should().HaveCount(2);
        dashboard.Metrics.Should().HaveCount(4);

        var dropout = dashboard.Metrics.Single(m => m.Name == "sections.dropout_count");
        dropout.FailCount.Should().Be(1);
        dropout.PassCount.Should().Be(1);
    }

    [Fact]
    public static void Report_generator_writes_markdown_summary()
    {
        var metricsDir = Resolve("metrics");
        var checklistPath = Resolve("checklists/completed.json");
        var faultScenario = Resolve("faults/power-network.json");
        var schedulePath = Path.GetTempFileName();
        var options = CreateOptions();

        try
        {
            var schedule = new FaultInjectionRunner().BuildSchedule(faultScenario);
            File.WriteAllText(schedulePath, JsonSerializer.Serialize(schedule, options));

            var generator = new PostRunReportGenerator();
            var markdown = generator.Generate(new PostRunReportRequest
            {
                MetricsDirectory = metricsDir,
                ChecklistPath = checklistPath,
                FaultSchedulePath = schedulePath
            });

            markdown.Should().Contain("Warm start");
            markdown.Should().Contain("Network dropout");
            markdown.Should().Contain("Overall status: ATTENTION REQUIRED");
            markdown.Should().Contain("Field safety checklist");
            markdown.Should().Contain("Fault injection timeline");
        }
        finally
        {
            File.Delete(schedulePath);
        }
    }

    private static string Resolve(string relativePath)
    {
        return Path.Combine(AppContext.BaseDirectory, "Data", relativePath.Replace('/', Path.DirectorySeparatorChar));
    }

    private static JsonSerializerOptions CreateOptions()
    {
        return new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        };
    }
}
