using System;
using System.Threading;
using System.Threading.Tasks;
using Aog.Core.V1;
using Google.Protobuf.WellKnownTypes;

namespace Aog.Agio.Timing;

/// <summary>
/// Fallback probe used on platforms where timing capabilities cannot be detected.
/// </summary>
public sealed class NullTimingCapabilitiesProbe : ITimingCapabilitiesProbe
{
    private readonly TimeProvider _timeProvider;

    public NullTimingCapabilitiesProbe(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    /// <inheritdoc />
    public Task<TimingCaps> ProbeAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var now = _timeProvider.GetUtcNow();
        var caps = new TimingCaps
        {
            Header = new Header
            {
                Timestamp = Timestamp.FromDateTimeOffset(now),
                Source = "agio.timing",
                Frame = "clock",
            },
            EstimatedSkewPpm = double.NaN,
            ClockUncertaintyNs = double.NaN,
        };

        return Task.FromResult(caps);
    }
}
