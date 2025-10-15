using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aog.Bridge.Host.AogLink.Legacy;
using Aog.Bridge.Host.AogLink.Transports;
using Aog.Link.V1;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aog.Bridge.Host.AogLink;

/// <summary>
/// Coordinates the AOG-Link translators, transports, and compatibility bridges.
/// </summary>
public sealed class AogLinkGateway : IAogLinkGateway
{
    private readonly AogLinkTranslator _translator;
    private readonly LegacyCompatibilityBridge _legacyBridge;
    private readonly IReadOnlyList<IAogLinkTransport> _transports;
    private readonly AogLinkMeshBridge _meshBridge;
    private readonly BridgeHostOptions _options;
    private readonly ILogger<AogLinkGateway> _logger;
    private readonly CancellationTokenSource _cts = new();
    private readonly ConcurrentBag<Task> _backgroundTasks = new();

    public AogLinkGateway(
        AogLinkTranslator translator,
        LegacyCompatibilityBridge legacyBridge,
        IEnumerable<IAogLinkTransport> transports,
        AogLinkMeshBridge meshBridge,
        IOptions<BridgeHostOptions> options,
        ILogger<AogLinkGateway> logger)
    {
        _translator = translator ?? throw new ArgumentNullException(nameof(translator));
        _legacyBridge = legacyBridge ?? throw new ArgumentNullException(nameof(legacyBridge));
        _transports = transports?.ToArray() ?? throw new ArgumentNullException(nameof(transports));
        _meshBridge = meshBridge ?? throw new ArgumentNullException(nameof(meshBridge));
        if (_transports.Count == 0)
        {
            throw new ArgumentException("At least one transport must be registered.", nameof(transports));
        }

        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (var transport in _transports)
        {
            await transport.StartAsync(cancellationToken).ConfigureAwait(false);
            _backgroundTasks.Add(Task.Run(() => PumpTransportAsync(transport, _cts.Token), cancellationToken));
        }

        _backgroundTasks.Add(Task.Run(() => ResendLoopAsync(_cts.Token), cancellationToken));
        _logger.LogInformation("AOG-Link gateway started with {TransportCount} transport(s).", _transports.Count);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _cts.Cancel();

        while (_backgroundTasks.TryTake(out var task))
        {
            try
            {
                await task.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
        }

        foreach (var transport in _transports)
        {
            await transport.StopAsync(cancellationToken).ConfigureAwait(false);
            await transport.DisposeAsync().ConfigureAwait(false);
        }

        _logger.LogInformation("AOG-Link gateway stopped.");
    }

    /// <summary>
    /// Sends an envelope across the primary transport. Exposed for unit tests and future gRPC surfaces.
    /// </summary>
    public ValueTask SendAsync(LinkEnvelope envelope, CancellationToken cancellationToken = default)
    {
        if (envelope is null)
            throw new ArgumentNullException(nameof(envelope));

        return _transports[0].SendAsync(envelope, cancellationToken);
    }

    private async Task PumpTransportAsync(IAogLinkTransport transport, CancellationToken cancellationToken)
    {
        await foreach (var envelope in transport.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            try
            {
                await HandleIncomingEnvelopeAsync(envelope, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to handle incoming AOG-Link envelope.");
            }
        }
    }

    public async Task ProcessLegacyDatagramAsync(ReadOnlyMemory<byte> datagram, CancellationToken cancellationToken = default)
    {
        if (_legacyBridge.TryConvertLegacyFrame(datagram.Span, out var envelope))
        {
            await HandleIncomingEnvelopeAsync(envelope, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            _logger.LogDebug("Legacy datagram (length {Length}) did not match known PGNs.", datagram.Length);
        }
    }

    private async Task HandleIncomingEnvelopeAsync(LinkEnvelope envelope, CancellationToken cancellationToken)
    {
        if (!_translator.TryTranslateIncoming(envelope, out var message, out var kind))
        {
            _logger.LogDebug("Ignoring unknown envelope type {Type}.", envelope.Header?.MessageType);
            return;
        }

        if (_legacyBridge.TryConvertToLegacy(envelope, out var legacyDatagram))
        {
            _logger.LogDebug("Converted {Kind} to legacy PGN frame (length {Length}).", kind, legacyDatagram.Length);
        }

        switch (kind)
        {
            case AogLinkMessageKind.CommandAck when message is CommandAck ack:
                if (_translator.TryHandleAck(ack, out _))
                {
                    _logger.LogDebug("Acknowledged command sequence {Sequence}.", ack.AcknowledgedSequence);
                }
                break;
            case AogLinkMessageKind.DiscoveryAnnounce when message is DiscoveryAnnounce announce:
                await RespondToDiscoveryAsync(announce, cancellationToken).ConfigureAwait(false);
                break;
            default:
                _logger.LogDebug("Received {Kind} from node {Source}.", kind, envelope.Header?.Source);
                break;
        }

        try
        {
            await _meshBridge.HandleAsync(envelope, message, kind, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Mesh bridge failed to handle {Kind} from node {Source}.", kind, envelope.Header?.Source);
        }
    }

    private async Task RespondToDiscoveryAsync(DiscoveryAnnounce announce, CancellationToken cancellationToken)
    {
        var response = _translator.CreateDiscoveryResponse(announce, Array.Empty<nexus.capabilities.v1.CapabilityDescriptor>());
        await SendAsync(response, cancellationToken).ConfigureAwait(false);
    }

    private async Task ResendLoopAsync(CancellationToken cancellationToken)
    {
        var ackTimeout = TimeSpan.FromMilliseconds(_options.Udp.CommandAckTimeoutMs);
        var retryCount = _options.Udp.RetryCount;

        while (!cancellationToken.IsCancellationRequested)
        {
            if (_translator.TryCollectResends(ackTimeout, retryCount, out var resends))
            {
                foreach (var resend in resends)
                {
                    _logger.LogDebug("Retrying command sequence {Sequence}.", resend.Header?.Sequence);
                    await SendAsync(resend, cancellationToken).ConfigureAwait(false);
                }
            }

            try
            {
                await Task.Delay(ackTimeout, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
