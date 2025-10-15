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
/// Hosted service that bridges live mesh publications to an ELRS radio transport.
/// </summary>
public sealed class RadioBridgeElrsAdapter : IHostedService, IDisposable
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    private readonly ILiveTelemetryMeshService _meshService;
    private readonly IRadioBridgeLinkFactory _linkFactory;
    private readonly ILogger<RadioBridgeElrsAdapter> _logger;
    private readonly TimeProvider _timeProvider;
    private readonly RadioBridgeElrsAdapterOptions _options;
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
    /// Initializes a new instance of the <see cref="RadioBridgeElrsAdapter"/> class.
    /// </summary>
    public RadioBridgeElrsAdapter(
        ILiveTelemetryMeshService meshService,
        IOptions<RadioBridgeElrsAdapterOptions> options,
        IRadioBridgeLinkFactory linkFactory,
        ILogger<RadioBridgeElrsAdapter> logger,
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

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("RadioBridge ELRS adapter disabled by configuration.");
            return;
        }

        EnsureNotDisposed();

        _logger.LogInformation("Starting RadioBridge ELRS adapter for {Endpoint}.", _options.Endpoint);

        _transport = new RadioBridgeTransport(new RadioBridgeOptions { DeviceId = _options.DeviceId }, _timeProvider);
        _transport.OutboundDeliveryFailed += OnOutboundDeliveryFailed;

        _link = _linkFactory.Create(_options);
        _link.LinkMetricsChanged += OnLinkMetricsChanged;
        UpdateTransportLinkMetrics(_link.CurrentMetrics);

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
                    _logger.LogWarning("RadioBridge dropped frame due to {Status}.", result.Status);
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

        var payload = JsonSerializer.SerializeToUtf8Bytes(new
        {
            deviceId = _options.DeviceId,
            link = new { metrics.Rssi, metrics.PacketLoss },
            transmittedFrames = _counters.TransmittedFrames,
            outboundPublications = _counters.OutboundPublications,
            inboundPublications = _counters.InboundPublications,
            acknowledgements = _counters.Acknowledgements,
            faultedFrames = _counters.FaultedFrames,
        }, SerializerOptions);

        var topic = $"aog/live/{_options.DiagnosticsSeasonId}/{_options.DiagnosticsJobId}/{_options.DeviceId}.radio";
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["bridge.deviceId"] = _options.DeviceId,
            ["bridge.endpoint"] = _link?.Name ?? "unknown",
        };

        var request = new MeshPublishRequest(
            _options.DeviceId,
            topic,
            MeshDataTier.Coverage,
            payload,
            _timeProvider.GetUtcNow(),
            metadata);

        await _meshService.PublishAsync(request, cancellationToken).ConfigureAwait(false);
    }

    private async Task RegisterMeshDeviceAsync(CancellationToken cancellationToken)
    {
        var capabilities = _options.Capabilities ?? new[] { "radio", "bridge", "elrs" };
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
            "RadioBridge failed to deliver publication {Topic} after {Attempts} attempts.",
            failure.Message.Topic,
            failure.Attempts);
    }

    private void OnLinkMetricsChanged(RadioBridgeLinkMetrics metrics)
    {
        UpdateTransportLinkMetrics(metrics);
    }

    private void UpdateTransportLinkMetrics(RadioBridgeLinkMetrics metrics)
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
            throw new ObjectDisposedException(nameof(RadioBridgeElrsAdapter));
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
