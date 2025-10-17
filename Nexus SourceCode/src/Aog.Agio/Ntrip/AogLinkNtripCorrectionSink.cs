namespace Aog.Agio.Ntrip;

/// <summary>
/// Forwards RTCM correction payloads to enabled AOG-Link transports.
/// </summary>
public sealed class AogLinkNtripCorrectionSink : INtripCorrectionSink
{
    private readonly IEnumerable<Aog.Agio.AogLink.IAogLinkTransportDriver> _drivers;
    private readonly Microsoft.Extensions.Logging.ILogger<AogLinkNtripCorrectionSink> _logger;
    private int _sequence;

    /// <summary>
    /// Initializes a new instance of the <see cref="AogLinkNtripCorrectionSink"/> class.
    /// </summary>
    public AogLinkNtripCorrectionSink(
        IEnumerable<Aog.Agio.AogLink.IAogLinkTransportDriver> drivers,
        Microsoft.Extensions.Logging.ILogger<AogLinkNtripCorrectionSink> logger)
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

        List<Aog.Agio.AogLink.IAogLinkTransportDriver>? enabled = null;
        foreach (var driver in _drivers)
        {
            if (!driver.IsEnabled)
            {
                continue;
            }

            enabled ??= new List<Aog.Agio.AogLink.IAogLinkTransportDriver>();
            enabled.Add(driver);
        }

        if (enabled is null || enabled.Count == 0)
        {
            _logger.LogDebug("Dropping {Length} bytes of RTCM corrections because no AOG-Link transports are enabled.", payload.Length);
            return;
        }

        var sequence = (ushort)(Interlocked.Increment(ref _sequence) & 0xFFFF);
        var header = new Aog.Agio.AogLink.AogLinkFrameHeader(
            Version: 1,
            MessageClass: Aog.Agio.AogLink.AogLinkMessageCatalog.GnssClass,
            MessageType: Aog.Agio.AogLink.AogLinkMessageCatalog.RtcmCorrectionsType,
            Sequence: sequence,
            Source: Aog.Agio.AogLink.AogLinkMessageCatalog.NtripSourceAddress,
            Destination: Aog.Agio.AogLink.AogLinkMessageCatalog.BroadcastDestinationAddress,
            PayloadLength: (ushort)payload.Length);

        header.Validate(payload.Length);

        var copy = payload.ToArray();
        var frame = new Aog.Agio.AogLink.AogLinkFrame(header, copy);

        foreach (var driver in enabled)
        {
            await driver.SendAsync(frame, cancellationToken).ConfigureAwait(false);
        }
    }
}
