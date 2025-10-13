using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Aog.Agio.Timing;
using Xunit;

namespace Aog.Agio.Tests;

public sealed class LinuxTimingCapabilitiesProbeTests : IDisposable
{
    private readonly string _root;
    private readonly string _dev;
    private readonly string _ptp;

    public LinuxTimingCapabilitiesProbeTests()
    {
        _root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        _dev = Path.Combine(_root, "dev");
        _ptp = Path.Combine(_root, "sys", "class", "ptp");
        Directory.CreateDirectory(_dev);
        Directory.CreateDirectory(_ptp);
    }

    [Fact]
    public async Task ProbeAsync_ReportsFalse_WhenNoDevicesFound()
    {
        var probe = CreateProbe();

        var caps = await probe.ProbeAsync(CancellationToken.None);

        Assert.False(caps.HasPps);
        Assert.False(caps.HasPtp);
        Assert.False(caps.HasGnssTime);
        Assert.True(double.IsNaN(caps.EstimatedSkewPpm));
        Assert.True(double.IsNaN(caps.ClockUncertaintyNs));
        Assert.Equal("agio.timing", caps.Header.Source);
    }

    [Fact]
    public async Task ProbeAsync_DetectsPpsAndPtp()
    {
        File.WriteAllText(Path.Combine(_dev, "pps0"), string.Empty);
        Directory.CreateDirectory(Path.Combine(_ptp, "ptp0"));

        var probe = CreateProbe();

        var caps = await probe.ProbeAsync(CancellationToken.None);

        Assert.True(caps.HasPps);
        Assert.True(caps.HasGnssTime);
        Assert.True(caps.HasPtp);
    }

    [Fact]
    public async Task ProbeAsync_HonorsCancellation()
    {
        var probe = CreateProbe();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() => probe.ProbeAsync(cts.Token));
    }

    private LinuxTimingCapabilitiesProbe CreateProbe()
    {
        var options = new LinuxTimingProbeOptions
        {
            DevDirectory = _dev,
            SysClassPtpDirectory = _ptp,
        };

        var timeProvider = new FixedTimeProvider(new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.Zero));
        var logger = new TestLogger<LinuxTimingCapabilitiesProbe>();
        return new LinuxTimingCapabilitiesProbe(timeProvider, logger, options);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_root))
            {
                Directory.Delete(_root, recursive: true);
            }
        }
        catch
        {
            // Ignore cleanup errors in tests.
        }
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _now;

        public FixedTimeProvider(DateTimeOffset now)
        {
            _now = now;
        }

        public override DateTimeOffset GetUtcNow() => _now;
    }
}
