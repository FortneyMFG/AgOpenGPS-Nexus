using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aog.Core.Eventing;

namespace Aog.Core.Mesh;

/// <summary>
/// Background worker that subscribes to the mesh service and records publications for offline sync.
/// </summary>
public sealed class MeshRetentionWorker : IAsyncDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    private readonly ILiveTelemetryMeshService _meshService;
    private readonly MeshRetentionStore _store;
    private readonly MeshRetentionWorkerOptions _options;
    private readonly IEventBus? _eventBus;
    private readonly TimeProvider _timeProvider;
    private readonly CancellationTokenSource _shutdown = new();
    private IAsyncEnumerator<MeshPublication>? _enumerator;
    private Task? _pumpTask;
    private bool _isStarted;
    private long _sequence;

    /// <summary>
    /// Initializes a new instance of the <see cref="MeshRetentionWorker"/> class.
    /// </summary>
    public MeshRetentionWorker(
        ILiveTelemetryMeshService meshService,
        MeshRetentionStore retentionStore,
        MeshRetentionWorkerOptions? options = null,
        IEventBus? eventBus = null,
        TimeProvider? timeProvider = null)
    {
        _meshService = meshService ?? throw new ArgumentNullException(nameof(meshService));
        _store = retentionStore ?? throw new ArgumentNullException(nameof(retentionStore));
        _options = (options ?? new MeshRetentionWorkerOptions()).Clone();
        _eventBus = eventBus;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>
    /// Starts the retention worker.
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_isStarted)
        {
            throw new InvalidOperationException("Retention worker has already been started.");
        }

        _isStarted = true;

        var subscribeProfile = new MeshSubscribeProfile(new[]
        {
            new MeshSubscribeGrant("*", "*", MeshDataTier.All)
        });

        var registration = new MeshDeviceRegistration(
            _options.DeviceId,
            _options.DeviceLabel,
            shareProfile: MeshShareProfile.Empty,
            subscribeProfile: subscribeProfile)
        {
            Capabilities = _options.Capabilities is { Count: > 0 } capabilities ? capabilities : null
        };

        await _meshService.RegisterOrUpdateDeviceAsync(registration, cancellationToken).ConfigureAwait(false);

        _enumerator = _meshService
            .SubscribeAsync(new MeshSubscriptionRequest(_options.DeviceId, null, null, null, MeshDataTier.All), _shutdown.Token)
            .GetAsyncEnumerator(_shutdown.Token);

        _pumpTask = Task.Run(() => PumpAsync(_enumerator, _shutdown.Token), CancellationToken.None);
    }

    /// <summary>
    /// Stops the worker and disposes underlying subscriptions.
    /// </summary>
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (!_isStarted)
        {
            return;
        }

        _shutdown.Cancel();

        if (_pumpTask is not null)
        {
            await Task.WhenAny(_pumpTask, Task.Delay(-1, cancellationToken)).ConfigureAwait(false);
        }

        if (_enumerator is not null)
        {
            await _enumerator.DisposeAsync().ConfigureAwait(false);
            _enumerator = null;
        }

        _isStarted = false;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await StopAsync().ConfigureAwait(false);
        _shutdown.Dispose();
        GC.SuppressFinalize(this);
    }

    private async Task PumpAsync(IAsyncEnumerator<MeshPublication> enumerator, CancellationToken cancellationToken)
    {
        try
        {
            while (await enumerator.MoveNextAsync().ConfigureAwait(false))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var publication = enumerator.Current;
                _store.Record(publication);

                if (_eventBus is not null)
                {
                    var telemetryEvent = CreateTelemetryEvent(publication);
                    await _eventBus.PublishAsync(telemetryEvent, cancellationToken).ConfigureAwait(false);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Swallow cancellation on shutdown.
        }
    }

    private MeshTelemetryEvent CreateTelemetryEvent(MeshPublication publication)
    {
        var payload = publication.Payload.ToArray();
        var metadataJson = SerializeMetadata(publication.Metadata);
        var presenceJson = SerializePresence(publication.State as MeshPresenceSnapshot);

        var sequence = Interlocked.Increment(ref _sequence);
        return new MeshTelemetryEvent(
            sequence,
            publication.PublisherDeviceId,
            publication.Topic,
            publication.SeasonId,
            publication.JobId,
            publication.LayerNamespace,
            publication.Tier,
            publication.PublishedAt,
            payload,
            metadataJson,
            presenceJson);
    }

    private static string SerializeMetadata(IReadOnlyDictionary<string, string>? metadata)
    {
        if (metadata is null || metadata.Count == 0)
        {
            return "{}";
        }

        var ordered = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in metadata)
        {
            ordered[pair.Key] = pair.Value;
        }
        return JsonSerializer.Serialize(ordered, JsonOptions);
    }

    private static string? SerializePresence(MeshPresenceSnapshot? snapshot)
    {
        if (snapshot is null)
        {
            return null;
        }

        var payload = new
        {
            snapshot.DeviceId,
            State = snapshot.State.ToString(),
            snapshot.Session.SeasonId,
            snapshot.Session.JobId,
            snapshot.Session.SessionId,
            snapshot.Session.LayerNamespace,
            snapshot.Pose.Latitude,
            snapshot.Pose.Longitude,
            snapshot.Pose.AltitudeMeters,
            snapshot.Pose.HeadingDegrees,
            snapshot.Pose.SpeedMetersPerSecond,
            snapshot.UpdatedAt,
            snapshot.Metadata,
        };

        return JsonSerializer.Serialize(payload, JsonOptions);
    }
}

/// <summary>
/// Configuration for the <see cref="MeshRetentionWorker"/>.
/// </summary>
public sealed class MeshRetentionWorkerOptions
{
    /// <summary>Gets or sets the device identifier used to subscribe to the mesh.</summary>
    public string DeviceId { get; set; } = "mesh.retention";

    /// <summary>Gets or sets the human readable label for the retention worker.</summary>
    public string DeviceLabel { get; set; } = "Mesh retention";

    /// <summary>Gets or sets the capability strings advertised for diagnostics.</summary>
    public IReadOnlyList<string>? Capabilities { get; set; } = new[] { "retention", "offline-sync" };

    internal MeshRetentionWorkerOptions Clone()
    {
        return new MeshRetentionWorkerOptions
        {
            DeviceId = DeviceId,
            DeviceLabel = DeviceLabel,
            Capabilities = Capabilities is null ? null : new List<string>(Capabilities),
        };
    }
}
