using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace Aog.Tools.LegacyConfigTranslator.Tests;

public sealed class LegacySoakRunnerTests
{
    [Fact]
    public async Task RunAsync_ProducesReportWithCounts()
    {
        var runner = new LegacySoakRunner();
        var options = new LegacySoakOptions(DurationSeconds: 1, UdpRatePerStreamHz: 10, SerialRateHz: 15);

        var report = await runner.RunAsync(options);

        report.Udp.PoseFrames.Should().BeGreaterThan(0);
        report.Udp.SteerCommandFrames.Should().Be(report.Udp.SectionFrames);
        report.Serial.FramesEncoded.Should().BeGreaterThan(0);
        report.Serial.FramesDecoded.Should().Be(report.Serial.FramesEncoded - report.Serial.DecodeFailures);
        report.Udp.PoseSpacingStats.Samples.Should().Be(report.Udp.PoseFrames);
        report.Udp.SteerStateSpacingStats.Samples.Should().Be(report.Udp.SteerStateFrames);
    }
}
