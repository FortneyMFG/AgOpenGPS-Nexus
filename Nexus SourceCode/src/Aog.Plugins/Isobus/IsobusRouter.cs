using System;
using System.Threading;
using System.Threading.Tasks;
using Aog.Core.Eventing;
using Aog.Core.V1;

namespace Aog.Plugins.Isobus;

/// <summary>
/// Bridges CAN bus and UDP transport envelopes for ISO 11783 PGNs and records diagnostics.
/// </summary>
public sealed class IsobusRouter
{
    private readonly IEventBus _eventBus;
    private readonly IsobusDiagnostics _diagnostics;
    private readonly IsobusHandshakeManager _handshake;
    private readonly TimeProvider _timeProvider;
    private readonly string _datagramTopic;

    /// <summary>
    /// Initializes a new instance of the <see cref="IsobusRouter"/> class.
    /// </summary>
    /// <param name="eventBus">Event bus used to publish routed messages.</param>
    /// <param name="handshake">Handshake manager responsible for address claim responses.</param>
    /// <param name="datagramTopic">Topic identifier for UDP datagram publications.</param>
    /// <param name="timeProvider">Optional time provider used for diagnostics.</param>
    public IsobusRouter(
        IEventBus eventBus,
        IsobusHandshakeManager handshake,
        string datagramTopic,
        TimeProvider? timeProvider = null)
    {
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _handshake = handshake ?? throw new ArgumentNullException(nameof(handshake));
        if (string.IsNullOrWhiteSpace(datagramTopic))
        {
            throw new ArgumentException("Datagram topic is required.", nameof(datagramTopic));
        }

        _datagramTopic = datagramTopic;
        _diagnostics = new IsobusDiagnostics();
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>
    /// Routes an incoming CAN frame, optionally responding to handshake requests, and publishes a UDP datagram event.
    /// </summary>
    /// <param name="frame">Incoming CAN frame.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async ValueTask RouteCanFrameAsync(CanFrame frame, CancellationToken cancellationToken = default)
    {
        if (!IsobusMessage.TryFromCanFrame(frame, out var message))
        {
            return;
        }

        var timestamp = _timeProvider.GetUtcNow();
        _diagnostics.Record(message, timestamp);

        if (_handshake.TryCreateHandshakeResponse(message, out var response))
        {
            await _eventBus.PublishAsync(response, cancellationToken).ConfigureAwait(false);
        }

        var datagram = new IsobusDatagram(_datagramTopic, message.ToDatagram());
        await _eventBus.PublishAsync(datagram, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Routes an incoming UDP datagram to the CAN bus event stream.
    /// </summary>
    /// <param name="datagram">UDP payload encoded with the Nexus ISOBUS envelope.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async ValueTask RouteDatagramAsync(ReadOnlyMemory<byte> datagram, CancellationToken cancellationToken = default)
    {
        if (!IsobusMessage.TryFromDatagram(datagram.Span, out var message))
        {
            return;
        }

        var timestamp = _timeProvider.GetUtcNow();
        _diagnostics.Record(message, timestamp);

        var frame = message.ToCanFrame();
        await _eventBus.PublishAsync(frame, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Generates an address-claim frame using the configured handshake manager.
    /// </summary>
    public CanFrame BuildAddressClaim() => _handshake.BuildAddressClaim();

    /// <summary>
    /// Creates a snapshot of the recorded diagnostics.
    /// </summary>
    public IsobusDiagnosticsSnapshot GetDiagnosticsSnapshot() => _diagnostics.CreateSnapshot();
}

/// <summary>
/// Event published whenever a UDP datagram is emitted by the router.
/// </summary>
/// <param name="Topic">Topic identifier used for downstream routing.</param>
/// <param name="Payload">Serialized datagram bytes.</param>
public sealed record IsobusDatagram(string Topic, byte[] Payload);
