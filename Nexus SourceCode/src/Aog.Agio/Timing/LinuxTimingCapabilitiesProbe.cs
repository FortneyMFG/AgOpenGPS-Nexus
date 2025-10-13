using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aog.Core.V1;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;

namespace Aog.Agio.Timing;

/// <summary>
/// Linux implementation of <see cref="ITimingCapabilitiesProbe"/>.
/// </summary>
public sealed class LinuxTimingCapabilitiesProbe : ITimingCapabilitiesProbe
{
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<LinuxTimingCapabilitiesProbe> _logger;
    private readonly LinuxTimingProbeOptions _options;

    public LinuxTimingCapabilitiesProbe(
        TimeProvider timeProvider,
        ILogger<LinuxTimingCapabilitiesProbe> logger,
        LinuxTimingProbeOptions? options = null)
    {
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? new LinuxTimingProbeOptions();
    }

    /// <inheritdoc />
    public Task<TimingCaps> ProbeAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var now = _timeProvider.GetUtcNow();
        var header = new Header
        {
            Timestamp = Timestamp.FromDateTimeOffset(now),
            Source = "agio.timing",
            Frame = "clock",
        };

        var caps = new TimingCaps { Header = header };

        caps.HasPps = TryDetectPps();
        caps.HasPtp = TryDetectPtp();
        caps.HasGnssTime = caps.HasPps;
        caps.EstimatedSkewPpm = double.NaN;
        caps.ClockUncertaintyNs = double.NaN;

        _logger.LogInformation(
            "Detected timing capabilities on Linux: PPS={HasPps}, PTP={HasPtp}.",
            caps.HasPps,
            caps.HasPtp);

        return Task.FromResult(caps);
    }

    private bool TryDetectPps()
    {
        try
        {
            if (!Directory.Exists(_options.DevDirectory))
            {
                return false;
            }

            return Directory.EnumerateFileSystemEntries(_options.DevDirectory, "pps*", SearchOption.TopDirectoryOnly)
                .Any(entry =>
                {
                    try
                    {
                        return File.Exists(entry) || Directory.Exists(entry);
                    }
                    catch (IOException)
                    {
                        return false;
                    }
                    catch (UnauthorizedAccessException)
                    {
                        return false;
                    }
                });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to detect PPS devices in {Directory}.", _options.DevDirectory);
            return false;
        }
    }

    private bool TryDetectPtp()
    {
        try
        {
            if (!Directory.Exists(_options.SysClassPtpDirectory))
            {
                return false;
            }

            return Directory.EnumerateDirectories(_options.SysClassPtpDirectory, "ptp*", SearchOption.TopDirectoryOnly).Any();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to detect PTP clocks in {Directory}.", _options.SysClassPtpDirectory);
            return false;
        }
    }
}
