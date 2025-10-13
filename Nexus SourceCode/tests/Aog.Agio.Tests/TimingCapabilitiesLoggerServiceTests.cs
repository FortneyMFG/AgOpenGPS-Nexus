using System.Threading;
using System.Threading.Tasks;
using Aog.Agio.Timing;
using Aog.Core.V1;
using Xunit;

namespace Aog.Agio.Tests;

public sealed class TimingCapabilitiesLoggerServiceTests
{
    [Fact]
    public async Task StartAsync_LogsCapabilities()
    {
        var logger = new TestLogger<TimingCapabilitiesLoggerService>();
        var probe = new FakeProbe();
        var service = new TimingCapabilitiesLoggerService(logger, probe);

        await service.StartAsync(CancellationToken.None);

        Assert.True(probe.Invoked);
        Assert.Contains(logger.Entries, entry => entry.Level == Microsoft.Extensions.Logging.LogLevel.Information && entry.Message.Contains("PPS=True"));
    }

    [Fact]
    public async Task StartAsync_LogsWarning_WhenProbeReturnsNull()
    {
        var logger = new TestLogger<TimingCapabilitiesLoggerService>();
        var probe = new NullProbe();
        var service = new TimingCapabilitiesLoggerService(logger, probe);

        await service.StartAsync(CancellationToken.None);

        Assert.Contains(logger.Entries, entry => entry.Level == Microsoft.Extensions.Logging.LogLevel.Warning);
    }

    private sealed class FakeProbe : ITimingCapabilitiesProbe
    {
        public bool Invoked { get; private set; }

        public Task<TimingCaps> ProbeAsync(CancellationToken cancellationToken = default)
        {
            Invoked = true;
            var caps = new TimingCaps
            {
                HasPps = true,
                HasPtp = false,
                HasGnssTime = true,
            };
            return Task.FromResult(caps);
        }
    }

    private sealed class NullProbe : ITimingCapabilitiesProbe
    {
        public Task<TimingCaps> ProbeAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<TimingCaps>(null!);
        }
    }
}
