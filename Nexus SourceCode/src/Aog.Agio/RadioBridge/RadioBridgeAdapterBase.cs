using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aog.Core.Mesh;
using Aog.Core.Mesh.RadioBridge;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aog.Agio.RadioBridge;

/// <summary>
/// Shared hosted service implementation for RadioBridge adapters.
/// </summary>
/// <typeparam name="TOptions">Options type controlling adapter behaviour.</typeparam>
public abstract class RadioBridgeAdapterBase<TOptions> : IHostedService, IDisposable
    where TOptions : RadioBridgeAdapterOptions
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    private readonly ILiveTelemetryMeshService _meshService;
    private readonly IRadioBridgeLinkFactory _linkFactory;
    private readonly ILogger _logger;
    private readonly TimeProvider _timeProvider;
    private readonly TOptions _options;
    private readonly SemaphoreSlim _sendSignal = new(0);
    private readonly object _transportGate = new();
    private readonly AdapterCounters _counters = new();
    private RadioBridgeTransport? _transport;
    private IRadioBridgeLink? _link;
    private CancellationTokenSource? _cancellation;
    private Task? _sendLoop;
    private Task? _receiveLoop;
    private Task? _subscriptionLoop;
    private Task? _diagnosticsLoop;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="RadioBridgeAdapterBase{TOptions}"/> class.
    /// </summary>
    protected RadioBridgeAdapterBase(
        ILiveTelemetryMeshService meshService,
        IOptions<TOptions> options,
        IRadioBridgeLinkFactory linkFactory,
        ILogger logger,
        TimeProvider? timeProvider = null)
    {
        _meshService = meshService ?? throw new ArgumentNullException(nameof(meshService));
        _linkFactory = linkFactory ?? throw new ArgumentNullException(nameof(linkFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _timeProvider = timeProvider ?? TimeProvider.System;

        var resolvedOptions = (options ?? throw new ArgumentNullException(nameof(options))).Value
            ?? throw new ArgumentException("Adapter options are required.", nameof(options));
        _options = resolvedOptions;
    }

    /// <summary>
    /// Gets the resolved options for the adapter.
    /// </summary>
    protected TOptions Options => _options;

    /// <summary>
    /// Gets the logger instance used by the adapter.
    /// </summary>
    protected ILogger Logger => _logger;

    /// <summary>
    /// Gets the user-facing name for the adapter used in logs.
    /// </summary>
    protected abstract string AdapterDisplayName { get; }

    /// <summary>
    /// Gets the identifier representing the radio technology (e.g., <c>elrs</c>, <c>lora</c>).
    /// </summary>
    protected abstract string AdapterKind { get; }

    /// <summary>
    /// Gets the capabilities advertised when the mesh device is registered.
    /// </summary>
    protected abstract IReadOnlyList<string> DefaultCapabilities { get; }

    /// <summary>
    /// Creates <see cref="RadioBridgeOptions"/> used by the transport when the adapter starts.
    /// </summary>
    /// <param name="options">Resolved adapter options.</param>
    /// <returns>Transport options.</returns>
    protected virtual RadioBridgeOptions CreateTransportOptions(TOptions options) => new()
    {
        DeviceId = options.DeviceId,
        EnableForwardErrorCorrection = options.EnableForwardErrorCorrection,
    };

    /// <summary>
    /// Allows derived adapters to enrich the metadata attached to mesh publications.
    /// </summary>
    protected virtual void EnrichPublicationMetadata(
        IDictionary<string, string> metadata,
        RadioBridgePublicationMessage message)
    {
    }

    /// <summary>
    /// Allows derived adapters to customize the diagnostics payload emitted to the mesh.
    /// </summary>
    protected virtual void EnrichDiagnosticsPayload(IDictionary<string, object> payload)
    {
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("{Adapter} adapter disabled by configuration.", AdapterDisplayName);
            return;
        }

        EnsureNotDisposed();

        _logger.LogInformation("Starting {Adapter} adapter for {Endpoint}.", AdapterDisplayName, _options.Endpoint);

        _transport = new RadioBridgeTransport(CreateTransportOptions(_options), _timeProvider);
        _transport.OutboundDeliveryFailed += OnOutboundDeliveryFailed;

        _link = _linkFactory.Create(_options);
        _link.LinkMetricsChanged += OnLinkMetricsChanged;
        OnLinkMetricsChanged(_link.CurrentMetrics);

        _cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        await _link.ConnectAsync(_cancellation.Token).ConfigureAwait(false);
        await RegisterMeshDeviceAsync(cancellationToken).ConfigureAwait(false);

        _sendLoop = Task.Run(() => RunSendLoopAsync(_cancellation.Token), CancellationToken.None);
        _receiveLoop = Task.Run(() => RunReceiveLoopAsync(_cancellation.Token), CancellationToken.None);
        _subscriptionLoop = Task.Run(() => RunSubscriptionLoopAsync(_cancellation.Token), CancellationToken.None);

        if (_options.DiagnosticsInterval > TimeSpan.Zero)
        {
            _diagnosticsLoop = Task.Run(() => RunDiagnosticsLoopAsync(_cancellation.Token), CancellationToken.None);
        }
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_cancellation is null)
        {
            return;
        }

        _cancellation.Cancel();

        await Task.WhenAll(
                new[] { _sendLoop, _receiveLoop, _subscriptionLoop, _diagnosticsLoop }
                    .Where(task => task is not null)!
                    .Select(task => CatchCancellationAsync(task!, cancellationToken)))
            .ConfigureAwait(false);

        if (_link is not null)
        {
            await _link.DisposeAsync().ConfigureAwait(false);
        }

        _cancellation.Dispose();
        _cancellation = null;
        _link = null;
        _transport = null;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _sendSignal.Dispose();
        _cancellation?.Dispose();
    }

    private async Task RunSendLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var waitTask = _sendSignal.WaitAsync(cancellationToken);
            var delayTask = _timeProvider.Delay(_options.SendInterval, cancellationToken).AsTask();
            var completed = await Task.WhenAny(waitTask, delayTask).ConfigureAwait(false);

            if (completed == waitTask)
            {
                await waitTask.ConfigureAwait(false);
                while (_sendSignal.CurrentCount > 0)
                {
                    await _sendSignal.WaitAsync(cancellationToken).ConfigureAwait(false);
                }
            }
            else
            {
                await delayTask.ConfigureAwait(false);
            }

            await FlushOutboundAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task RunReceiveLoopAsync(CancellationToken cancellationToken)
    {
        if (_link is null || _transport is null)
        {
            return;
        }

        await foreach (var frame in _link.ReadFramesAsync(cancellationToken).ConfigureAwait(false))
        {
            RadioBridgeProcessResult result;
            lock (_transportGate)
            {
                result = _transport.ProcessInboundFrame(frame.Span);
            }

            switch (result.Status)
            {
                case RadioBridgeProcessStatus.PublicationDelivered:
                    if (result.Publication is not null)
                    {
                        await PublishToMeshAsync(result.Publication, cancellationToken).ConfigureAwait(false);
                        Interlocked.Increment(ref _counters.InboundPublications);
                    }

                    break;
                case RadioBridgeProcessStatus.AcknowledgementProcessed:
                case RadioBridgeProcessStatus.StaleAcknowledgement:
                case RadioBridgeProcessStatus.DuplicateFrame:
                    Interlocked.Increment(ref _counters.Acknowledgements);
                    break;
                case RadioBridgeProcessStatus.PayloadDecodeFailure:
                case RadioBridgeProcessStatus.MalformedFrame:
                case RadioBridgeProcessStatus.UnsupportedPayloadType:
                case RadioBridgeProcessStatus.UnsupportedVersion:
                case RadioBridgeProcessStatus.CrcMismatch:
                    Interlocked.Increment(ref _counters.FaultedFrames);
                    _logger.LogWarning("{Adapter} dropped frame due to {Status}.", AdapterDisplayName, result.Status);
                    break;
            }

            SignalSend();
        }
    }

    private async Task RunSubscriptionLoopAsync(CancellationToken cancellationToken)
    {
        if (_transport is null)
        {
            return;
        }

        var request = new MeshSubscriptionRequest(_options.DeviceId);
        await foreach (var publication in _meshService.SubscribeAsync(request, cancellationToken).ConfigureAwait(false))
        {
            var payload = publication.Payload.IsEmpty ? Array.Empty<byte>() : publication.Payload.ToArray();
            var message = new RadioBridgePublicationMessage(
                publication.Topic,
                publication.Tier,
                publication.PublishedAt,
                publication.PublisherDeviceId,
                publication.Metadata,
                payload);

            lock (_transportGate)
            {
                _transport.EnqueuePublication(message);
            }

            Interlocked.Increment(ref _counters.OutboundPublications);
            SignalSend();
        }
    }

    private async Task RunDiagnosticsLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await _timeProvider.Delay(_options.DiagnosticsInterval, cancellationToken).ConfigureAwait(false);
            await PublishDiagnosticsAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private void SignalSend()
    {
        try
        {
            _sendSignal.Release();
        }
        catch (SemaphoreFullException)
        {
        }
    }

    private async Task FlushOutboundAsync(CancellationToken cancellationToken)
    {
        if (_link is null || _transport is null)
        {
            return;
        }

        while (true)
        {
            ReadOnlyMemory<byte> frame;
            lock (_transportGate)
            {
                if (!_transport.TryGetNextFrame(out frame))
                {
                    break;
                }
            }

            await _link.SendFrameAsync(frame, cancellationToken).ConfigureAwait(false);
            Interlocked.Increment(ref _counters.TransmittedFrames);
        }
    }

    private async Task PublishToMeshAsync(RadioBridgePublicationMessage message, CancellationToken cancellationToken)
    {
        var metadata = message.Metadata is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(message.Metadata, StringComparer.OrdinalIgnoreCase);

        metadata["radio.publisher"] = message.PublisherDeviceId;
        metadata["radio.bridge"] = _options.DeviceId;
        metadata["radio.kind"] = AdapterKind;
        metadata["radio.fec"] = _options.EnableForwardErrorCorrection ? "enabled" : "disabled";

        EnrichPublicationMetadata(metadata, message);

        var request = new MeshPublishRequest(
            _options.DeviceId,
            message.Topic,
            message.Tier,
            message.Payload,
            message.PublishedAt,
            metadata);

        await _meshService.PublishAsync(request, cancellationToken).ConfigureAwait(false);
    }

    private async Task PublishDiagnosticsAsync(CancellationToken cancellationToken)
    {
        if (_transport is null)
        {
            return;
        }

        RadioBridgeLinkMetrics metrics;
        lock (_transportGate)
        {
            metrics = _transport.LinkMetrics;
        }

        var payload = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
        {
            ["deviceId"] = _options.DeviceId,
            ["kind"] = AdapterKind,
            ["link"] = new { metrics.Rssi, metrics.PacketLoss },
            ["transmittedFrames"] = _counters.TransmittedFrames,
            ["outboundPublications"] = _counters.OutboundPublications,
            ["inboundPublications"] = _counters.InboundPublications,
            ["acknowledgements"] = _counters.Acknowledgements,
            ["faultedFrames"] = _counters.FaultedFrames,
            ["forwardErrorCorrection"] = _options.EnableForwardErrorCorrection,
        };

        EnrichDiagnosticsPayload(payload);

        var payloadBytes = JsonSerializer.SerializeToUtf8Bytes(payload, SerializerOptions);
        var topic = $"aog/live/{_options.DiagnosticsSeasonId}/{_options.DiagnosticsJobId}/{_options.DeviceId}.radio";
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["bridge.deviceId"] = _options.DeviceId,
            ["bridge.endpoint"] = _link?.Name ?? "unknown",
            ["radio.kind"] = AdapterKind,
        };

        var request = new MeshPublishRequest(
            _options.DeviceId,
            topic,
            MeshDataTier.Coverage,
            payloadBytes,
            _timeProvider.GetUtcNow(),
            metadata);

        await _meshService.PublishAsync(request, cancellationToken).ConfigureAwait(false);
    }

    private async Task RegisterMeshDeviceAsync(CancellationToken cancellationToken)
    {
        var capabilities = _options.Capabilities ?? DefaultCapabilities;
        var shareGrants = new[]
        {
            new MeshShareGrant("*", "*", MeshDataTier.All),
            new MeshShareGrant(_options.DiagnosticsSeasonId, _options.DiagnosticsJobId, MeshDataTier.Coverage),
        };

        var subscribeGrants = new[]
        {
            new MeshSubscribeGrant("*", "*", MeshDataTier.All),
        };

        var registration = new MeshDeviceRegistration(
            _options.DeviceId,
            _options.DeviceLabel,
            capabilities,
            new MeshShareProfile(shareGrants),
            new MeshSubscribeProfile(subscribeGrants));

        await _meshService.RegisterOrUpdateDeviceAsync(registration, cancellationToken).ConfigureAwait(false);
    }

    private void OnOutboundDeliveryFailed(RadioBridgeOutboundFailure failure)
    {
        Interlocked.Increment(ref _counters.FaultedFrames);
        _logger.LogWarning(
            "{Adapter} failed to deliver publication {Topic} after {Attempts} attempts.",
            AdapterDisplayName,
            failure.Message.Topic,
            failure.Attempts);
    }

    private void OnLinkMetricsChanged(RadioBridgeLinkMetrics metrics)
    {
        if (_transport is null)
        {
            return;
        }

        lock (_transportGate)
        {
            _transport.UpdateLinkMetrics(metrics);
        }
    }

    private static async Task CatchCancellationAsync(Task task, CancellationToken cancellationToken)
    {
        if (task is null)
        {
            return;
        }

        try
        {
            await task.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private void EnsureNotDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(GetType().Name);
        }
    }

    private sealed class AdapterCounters
    {
        public int TransmittedFrames;
        public int OutboundPublications;
        public int InboundPublications;
        public int Acknowledgements;
        public int FaultedFrames;
    }
}
