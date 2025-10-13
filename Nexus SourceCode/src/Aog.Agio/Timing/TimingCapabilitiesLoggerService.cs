using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aog.Agio.Timing;

/// <summary>
/// Hosted service that probes and logs timing capabilities during startup.
/// </summary>
public sealed class TimingCapabilitiesLoggerService : IHostedService
{
    private readonly ILogger<TimingCapabilitiesLoggerService> _logger;
    private readonly ITimingCapabilitiesProbe _probe;

    public TimingCapabilitiesLoggerService(
        ILogger<TimingCapabilitiesLoggerService> logger,
        ITimingCapabilitiesProbe probe)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _probe = probe ?? throw new ArgumentNullException(nameof(probe));
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var caps = await _probe.ProbeAsync(cancellationToken).ConfigureAwait(false);

        if (caps is null)
        {
            _logger.LogWarning("Timing capabilities probe returned no data.");
            return;
        }

        _logger.LogInformation(
            "Timing capabilities: PPS={HasPps}, PTP={HasPtp}, GNSS Time={HasGnssTime}, Skew={SkewPpm} ppm, Uncertainty={UncertaintyNs} ns.",
            caps.HasPps,
            caps.HasPtp,
            caps.HasGnssTime,
            caps.EstimatedSkewPpm,
            caps.ClockUncertaintyNs);
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
