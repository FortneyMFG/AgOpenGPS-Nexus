using System;
using System.Linq;
using Xunit;

namespace Aog.Core.Jobs.Tests;

public sealed class SessionTimebaseDriftMonitorTests
{
    [Fact]
    public void RecordSample_WithAlignedTimestamps_DoesNotFlagDrift()
    {
        var monitor = new SessionTimebaseDriftMonitor();
        var sessionId = "session:alpha";
        var start = new DateTimeOffset(2025, 5, 5, 7, 0, 0, TimeSpan.Zero);

        monitor.RecordSample(sessionId, start, start);
        monitor.RecordSample(sessionId, start.AddMinutes(2), start.AddMinutes(2));

        var report = monitor.TryGetReport(sessionId);
        Assert.NotNull(report);
        Assert.False(report!.IsDriftDetected);
        Assert.True(report.HasSufficientData);
        Assert.Equal(TimeSpan.Zero, report.Offset);
        Assert.Equal(0d, report.DriftRateMillisecondsPerMinute);
        Assert.Equal(2, report.SampleCount);
        Assert.Equal(TimeSpan.FromMinutes(2), report.ObservationWindow);
    }

    [Fact]
    public void RecordSample_WhenDriftExceedsThreshold_FlagsSession()
    {
        var monitor = new SessionTimebaseDriftMonitor();
        var sessionId = "session:bravo";
        var start = new DateTimeOffset(2025, 5, 5, 7, 15, 0, TimeSpan.Zero);

        monitor.RecordSample(sessionId, start, start);
        monitor.RecordSample(sessionId, start.AddMinutes(3), start.AddMinutes(3).AddMilliseconds(9));

        var report = monitor.TryGetReport(sessionId);
        Assert.NotNull(report);
        Assert.True(report!.IsDriftDetected);
        Assert.True(report.HasSufficientData);
        Assert.Equal(TimeSpan.FromMilliseconds(9), report.Offset);
        Assert.InRange(report.DriftRateMillisecondsPerMinute, 2.9, 3.1);
    }

    [Fact]
    public void RecordSample_WithShortObservation_DoesNotReportDrift()
    {
        var monitor = new SessionTimebaseDriftMonitor(minimumObservation: TimeSpan.FromMinutes(1));
        var sessionId = "session:charlie";
        var start = new DateTimeOffset(2025, 5, 5, 8, 0, 0, TimeSpan.Zero);

        monitor.RecordSample(sessionId, start, start);
        monitor.RecordSample(sessionId, start.AddSeconds(30), start.AddSeconds(30).AddMilliseconds(10));

        var report = monitor.TryGetReport(sessionId);
        Assert.NotNull(report);
        Assert.False(report!.HasSufficientData);
        Assert.False(report.IsDriftDetected);
        Assert.Equal(TimeSpan.FromMilliseconds(10), report.Offset);
        Assert.Equal(0d, report.DriftRateMillisecondsPerMinute);
    }

    [Fact]
    public void SnapshotReports_ReturnsActiveSessions()
    {
        var monitor = new SessionTimebaseDriftMonitor();
        var now = new DateTimeOffset(2025, 5, 5, 9, 0, 0, TimeSpan.Zero);

        monitor.RecordSample("session:one", now, now);
        monitor.RecordSample("session:two", now, now);

        var reports = monitor.SnapshotReports();
        Assert.Equal(2, reports.Count);
        Assert.True(reports.Any(r => r.SessionId == "session:one"));
        Assert.True(reports.Any(r => r.SessionId == "session:two"));
    }

    [Fact]
    public void ResetSession_RemovesSamples()
    {
        var monitor = new SessionTimebaseDriftMonitor();
        var sessionId = "session:delta";
        var start = new DateTimeOffset(2025, 5, 5, 10, 0, 0, TimeSpan.Zero);

        monitor.RecordSample(sessionId, start, start);
        Assert.NotNull(monitor.TryGetReport(sessionId));

        Assert.True(monitor.ResetSession(sessionId));
        Assert.Null(monitor.TryGetReport(sessionId));
    }
}
