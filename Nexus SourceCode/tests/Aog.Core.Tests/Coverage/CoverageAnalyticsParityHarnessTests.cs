using System;
using System.IO;
using Aog.Core.Coverage;
using FluentAssertions;
using Xunit;

namespace Aog.Core.Tests.Coverage;

public sealed class CoverageAnalyticsParityHarnessTests
{
    private static readonly string DataRoot = Path.Combine("Coverage", "Data");

    [Fact]
    public void Compare_ReturnsWithinTolerance()
    {
        var harness = new CoverageAnalyticsParityHarness(tolerance: 0.1);
        var legacy = Path.Combine(AppContext.BaseDirectory, DataRoot, "CoverageParityLegacy.csv");
        var nexus = Path.Combine(AppContext.BaseDirectory, DataRoot, "CoverageParityNexus.csv");

        var report = harness.Compare(legacy, nexus);

        report.IsWithinTolerance.Should().BeTrue();
        report.Metrics.Should().ContainSingle(m => m.Metric == "TotalAreaSquareMeters" && m.AbsoluteDifference > 0);
    }

    [Fact]
    public void Compare_DetectsOutOfTolerance()
    {
        var harness = new CoverageAnalyticsParityHarness(tolerance: 0.01);
        var legacy = Path.Combine(AppContext.BaseDirectory, DataRoot, "CoverageParityLegacy.csv");
        var nexus = Path.Combine(AppContext.BaseDirectory, DataRoot, "CoverageParityNexus.csv");

        var report = harness.Compare(legacy, nexus);

        report.IsWithinTolerance.Should().BeFalse();
        report.Metrics.Should().Contain(m => !m.IsWithinTolerance);
    }

    [Fact]
    public void Compare_Throws_WhenMetricsMismatch()
    {
        var harness = new CoverageAnalyticsParityHarness(0.1);
        using var temp = new TempCsv();
        File.WriteAllText(temp.Path, "Metric,Value\nDifferentMetric,1");

        var legacy = Path.Combine(AppContext.BaseDirectory, DataRoot, "CoverageParityLegacy.csv");
        Action act = () => harness.Compare(legacy, temp.Path);

        act.Should().Throw<InvalidDataException>();
    }

    private sealed class TempCsv : IDisposable
    {
        public TempCsv()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".csv");
        }

        public string Path { get; }

        public void Dispose()
        {
            if (File.Exists(Path))
            {
                File.Delete(Path);
            }
        }
    }
}
