using Aog.Agio.AogLink;
using Microsoft.Extensions.Logging;

namespace Aog.Agio.Ntrip;

/// <summary>
/// Forwards RTCM correction payloads to enabled AOG-Link transports.
/// </summary>
public sealed class AogLinkNtripCorrectionSink : INtripCorrectionSink
{
    private readonly IEnumerable<IAogLinkTransportDriver> _drivers;
    private readonly ILogger<AogLinkNtripCorrectionSink> _logger;
    private int _sequence;

    /// <summary>
    /// Initializes a new instance of the <see cref="AogLinkNtripCorrectionSink"/> class.
    /// </summary>
    public AogLinkNtripCorrectionSink(IEnumerable<IAogLinkTransportDriver> drivers, ILogger<AogLinkNtripCorrectionSink> logger)
    {
        _drivers = drivers ?? throw new ArgumentNullException(nameof(drivers));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async ValueTask PublishAsync(ReadOnlyMemory<byte> payload, CancellationToken cancellationToken)
    {
        if (payload.IsEmpty)
        {
            return;
        }

        if (payload.Length > ushort.MaxValue)
        {
            throw new InvalidOperationException("RTCM payload exceeds maximum frame size supported by AOG-Link.");
        }

        List<IAogLinkTransportDriver>? enabled = null;
        foreach (var driver in _drivers)
        {
            if (!driver.IsEnabled)
            {
                continue;
            }

            enabled ??= new List<IAogLinkTransportDriver>();
            enabled.Add(driver);
        }

        if (enabled is null || enabled.Count == 0)
        {
            _logger.LogDebug("Dropping {Length} bytes of RTCM corrections because no AOG-Link transports are enabled.", payload.Length);
            return;
        }

        var sequence = (ushort)(Interlocked.Increment(ref _sequence) & 0xFFFF);
        var header = new AogLinkFrameHeader(
            Version: 1,
            MessageClass: AogLinkMessageCatalog.GnssClass,
            MessageType: AogLinkMessageCatalog.RtcmCorrectionsType,
            Sequence: sequence,
            Source: AogLinkMessageCatalog.NtripSourceAddress,
            Destination: AogLinkMessageCatalog.BroadcastDestinationAddress,
            PayloadLength: (ushort)payload.Length);

        header.Validate(payload.Length);

        var copy = payload.ToArray();
        var frame = new AogLinkFrame(header, copy);

        foreach (var driver in enabled)
        {
            await driver.SendAsync(frame, cancellationToken).ConfigureAwait(false);
        }
    }
}
