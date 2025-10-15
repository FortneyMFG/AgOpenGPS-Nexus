using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using Aog.Tools.Qa.Dashboard;
using FluentAssertions;
using Xunit;

namespace Aog.Tools.Qa.Tests;

public static class DashboardHarnessTests
{
    [Fact]
    public static void Harness_generates_metrics_and_findings()
    {
        var specPath = Resolve("dashboard/spec.json");
        var observationsPath = Resolve("dashboard/observations.json");
        var harness = new DashboardAutomationHarness();

        var result = harness.Run(new DashboardHarnessRequest
        {
            SpecPath = specPath,
            ObservationPath = observationsPath
        });

        result.MetricSet.Scenario.Should().Be("Dashboard smoke");
        result.MetricSet.Metrics.Should().HaveCount(4);
        result.Passed.Should().BeFalse();

        var steeringLatency = result.MetricSet.Metrics.Single(m => m.Name.EndsWith("steering.engage_latency_ms"));
        steeringLatency.Status.Should().Be("warn");
        steeringLatency.Value.Should().Be(1);

        var optionalMetric = result.MetricSet.Metrics.Single(m => m.Name.EndsWith("telemetry.optional_metric"));
        optionalMetric.Status.Should().Be("warn");
        optionalMetric.Value.Should().Be(0);

        var dropout = result.MetricSet.Metrics.Single(m => m.Name.EndsWith("sections.sections.dropout_count"));
        dropout.Status.Should().Be("fail");
        dropout.Value.Should().Be(0);

        result.Findings.Should().ContainSingle(f => f.Severity == DashboardHarnessFindingSeverity.Error && f.Message.Contains("sections", StringComparison.OrdinalIgnoreCase));
        result.Findings.Should().Contain(f => f.Severity == DashboardHarnessFindingSeverity.Warning && f.Message.Contains("optional", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public static void Harness_writes_metric_file_when_output_directory_provided()
    {
        var specPath = Resolve("dashboard/spec.json");
        var observationsPath = Resolve("dashboard/observations.json");
        var harness = new DashboardAutomationHarness();
        using var temp = new TempDirectory();

        var result = harness.Run(new DashboardHarnessRequest
        {
            SpecPath = specPath,
            ObservationPath = observationsPath,
            OutputDirectory = temp.Path,
            OutputFileName = "custom-metrics",
            ScenarioOverride = "Override Scenario"
        });

        result.MetricSet.Scenario.Should().Be("Override Scenario");

        var expectedPath = Path.Combine(temp.Path, "custom-metrics.json");
        File.Exists(expectedPath).Should().BeTrue("the harness writes a JSON metric set");

        using var stream = File.OpenRead(expectedPath);
        var written = JsonSerializer.Deserialize<QaMetricSet>(stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        written.Should().NotBeNull();
        written!.Scenario.Should().Be("Override Scenario");
        written.Source.Should().Be("dashboard-harness");
    }

    private static string Resolve(string relativePath)
    {
        return Path.Combine(AppContext.BaseDirectory, "Data", relativePath.Replace('/', Path.DirectorySeparatorChar));
    }

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "qa-harness-tests", Guid.NewGuid().ToString("n"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                try
                {
                    Directory.Delete(Path, recursive: true);
                }
                catch
                {
                    // Ignored.
                }
            }
        }
    }
}
