using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Aog.Core.Mesh;

/// <summary>
/// In-memory implementation of the live telemetry mesh core service described in ADR-047.
/// </summary>
public sealed class LiveTelemetryMeshService : ILiveTelemetryMeshService, IDisposable
{
    private static readonly TimeSpan PresenceTtl = TimeSpan.FromSeconds(5);

    private readonly TimeProvider _timeProvider;
    private readonly object _gate = new();
    private readonly Dictionary<string, MeshDeviceRecord> _devices = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, MeshPresenceEntry> _presence = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<Guid, MeshSubscriptionRecord> _subscriptions = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="LiveTelemetryMeshService"/> class.
    /// </summary>
    /// <param name="timeProvider">Time abstraction used for deterministic expiry tests.</param>
    public LiveTelemetryMeshService(TimeProvider? timeProvider = null)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc />
    public ValueTask RegisterOrUpdateDeviceAsync(MeshDeviceRegistration registration, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(registration);
        cancellationToken.ThrowIfCancellationRequested();

        var deviceId = NormalizeDeviceId(registration.DeviceId);
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            throw new ArgumentException("Device identifier is required.", nameof(registration));
        }

        var label = NormalizeLabel(registration.Label);
        if (string.IsNullOrWhiteSpace(label))
        {
            throw new ArgumentException("Device label is required.", nameof(registration));
        }

        var capabilities = NormalizeCapabilities(registration.Capabilities);
        var shareProfile = NormalizeShareProfile(registration.ShareProfile);
        var subscribeProfile = NormalizeSubscribeProfile(registration.SubscribeProfile);

        lock (_gate)
        {
            if (_devices.TryGetValue(deviceId, out var existing))
            {
                existing.Update(label, capabilities, shareProfile, subscribeProfile);
            }
            else
            {
                _devices[deviceId] = new MeshDeviceRecord(deviceId, label, capabilities, shareProfile, subscribeProfile);
            }
        }

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask PublishAsync(MeshPublishRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var deviceId = NormalizeDeviceId(request.DeviceId);
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            throw new ArgumentException("Device identifier is required.", nameof(request));
        }

        var topicParts = ParseTopic(request.Topic);
        EnsureTierMatchesTopic(request.Tier, topicParts.LayerNamespace);

        MeshDeviceRecord device;
        MeshPublication publication;
        MeshSubscriptionRecord[] subscriptions;

        lock (_gate)
        {
            if (!_devices.TryGetValue(deviceId, out device!))
            {
                throw new KeyNotFoundException($"Device '{deviceId}' is not registered with the mesh.");
            }

            if (!device.ShareProfile.Allows(topicParts.SeasonId, topicParts.JobId, topicParts.LayerNamespace, request.Tier))
            {
                throw new InvalidOperationException($"Device '{deviceId}' is not permitted to publish to topic '{topicParts.Topic}'.");
            }

            var publishedAt = request.PublishedAt ?? _timeProvider.GetUtcNow();
            var payload = request.Payload;
            var metadata = SanitizeMetadata(request.Metadata);
            publication = new MeshPublication(
                device.DeviceId,
                topicParts.Topic,
                topicParts.SeasonId,
                topicParts.JobId,
                topicParts.LayerNamespace,
                request.Tier,
                publishedAt,
                payload,
                metadata);

            subscriptions = _subscriptions.Values.ToArray();
        }

        Broadcast(publication, subscriptions);
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public IAsyncEnumerable<MeshPublication> SubscribeAsync(MeshSubscriptionRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var deviceId = NormalizeDeviceId(request.DeviceId);
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            throw new ArgumentException("Device identifier is required.", nameof(request));
        }

        var normalizedSeason = NormalizeOptionalIdentifier(request.SeasonId);
        var normalizedJob = NormalizeOptionalIdentifier(request.JobId);
        var normalizedLayers = NormalizeLayerSet(request.LayerNamespaces);
        var tierMask = request.TierMask == MeshDataTier.None ? MeshDataTier.None : request.TierMask;

        MeshSubscriptionRecord subscription;
        Guid subscriptionId;
        Channel<MeshPublication> channel;

        lock (_gate)
        {
            if (!_devices.TryGetValue(deviceId, out var device))
            {
                throw new KeyNotFoundException($"Device '{deviceId}' is not registered with the mesh.");
            }

            var filters = device.SubscribeProfile.CreateFilters(normalizedSeason, normalizedJob, normalizedLayers, tierMask);
            if (filters.Length == 0)
            {
                throw new InvalidOperationException($"Device '{deviceId}' does not have subscription grants for the requested filters.");
            }

            channel = Channel.CreateUnbounded<MeshPublication>(new UnboundedChannelOptions
            {
                AllowSynchronousContinuations = false,
                SingleReader = false,
                SingleWriter = false,
            });

            subscriptionId = Guid.NewGuid();
            subscription = new MeshSubscriptionRecord(subscriptionId, device.DeviceId, channel, filters);
            _subscriptions[subscriptionId] = subscription;
        }

        return ReadAsync(channel, subscriptionId, cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask UpdatePresenceAsync(MeshPresenceUpdate update, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(update);
        cancellationToken.ThrowIfCancellationRequested();

        var deviceId = NormalizeDeviceId(update.DeviceId);
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            throw new ArgumentException("Device identifier is required.", nameof(update));
        }

        var session = SanitizeSession(update.Session);
        var pose = update.Pose ?? throw new ArgumentNullException(nameof(update.Pose));
        var metadata = SanitizeMetadata(update.Metadata);
        var timestamp = update.Timestamp ?? _timeProvider.GetUtcNow();

        var topicParts = new MeshTopicParts(
            $"aog/live/{session.SeasonId}/{session.JobId}/presence",
            session.SeasonId,
            session.JobId,
            session.LayerNamespace ?? "presence");

        MeshPresenceSnapshot snapshot;
        MeshSubscriptionRecord[] subscriptions;
        MeshPublication publication;

        lock (_gate)
        {
            if (!_devices.TryGetValue(deviceId, out var device))
            {
                throw new KeyNotFoundException($"Device '{deviceId}' is not registered with the mesh.");
            }

            if (!device.ShareProfile.Allows(topicParts.SeasonId, topicParts.JobId, "presence", MeshDataTier.Presence))
            {
                throw new InvalidOperationException($"Device '{deviceId}' is not permitted to broadcast presence for job '{topicParts.JobId}'.");
            }

            snapshot = new MeshPresenceSnapshot(device.DeviceId, update.State, session, pose, timestamp, metadata);
            var expiresAt = update.State == MeshPresenceState.Online ? timestamp + PresenceTtl : timestamp;
            _presence[device.DeviceId] = new MeshPresenceEntry(snapshot, expiresAt);

            publication = new MeshPublication(
                device.DeviceId,
                topicParts.Topic,
                topicParts.SeasonId,
                topicParts.JobId,
                "presence",
                MeshDataTier.Presence,
                timestamp,
                ReadOnlyMemory<byte>.Empty,
                metadata,
                snapshot);

            subscriptions = _subscriptions.Values.ToArray();
        }

        Broadcast(publication, subscriptions);
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public IReadOnlyList<MeshPresenceSnapshot> ListPresence(string? seasonId = null, string? jobId = null)
    {
        var normalizedSeason = NormalizeOptionalIdentifier(seasonId);
        var normalizedJob = NormalizeOptionalIdentifier(jobId);
        var now = _timeProvider.GetUtcNow();

        lock (_gate)
        {
            PrunePresence(now);

            var results = new List<MeshPresenceSnapshot>();
            foreach (var entry in _presence.Values)
            {
                var snapshot = entry.Snapshot;
                if (normalizedSeason is not null && !string.Equals(snapshot.Session.SeasonId, normalizedSeason, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (normalizedJob is not null && !string.Equals(snapshot.Session.JobId, normalizedJob, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                results.Add(snapshot);
            }

            return results;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        lock (_gate)
        {
            foreach (var subscription in _subscriptions.Values)
            {
                subscription.Channel.Writer.TryComplete();
            }

            _subscriptions.Clear();
            _devices.Clear();
            _presence.Clear();
        }
    }

    private static string NormalizeDeviceId(string value)
    {
        return value?.Trim() ?? string.Empty;
    }

    private static string NormalizeLabel(string value)
    {
        return value?.Trim() ?? string.Empty;
    }

    private static string? NormalizeOptionalIdentifier(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return string.Equals(trimmed, "*", StringComparison.Ordinal) ? null : trimmed;
    }

    private static string NormalizeIdentifier(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Identifier value is required.");
        }

        var trimmed = value.Trim();
        if (string.Equals(trimmed, "*", StringComparison.Ordinal))
        {
            throw new ArgumentException("Wildcard identifiers are not allowed in this context.");
        }

        return trimmed;
    }

    private static string[] NormalizeCapabilities(IReadOnlyCollection<string>? capabilities)
    {
        if (capabilities is null || capabilities.Count == 0)
        {
            return Array.Empty<string>();
        }

        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var capability in capabilities)
        {
            if (string.IsNullOrWhiteSpace(capability))
            {
                continue;
            }

            var normalized = capability.Trim();
            set.Add(normalized);
        }

        if (set.Count == 0)
        {
            return Array.Empty<string>();
        }

        return set.OrderBy(item => item, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private static MeshShareProfileRecord NormalizeShareProfile(MeshShareProfile? profile)
    {
        if (profile is null || profile.Grants.Count == 0)
        {
            return new MeshShareProfileRecord(Array.Empty<MeshProfileGrantRecord>());
        }

        var grants = profile.Grants
            .Where(g => g is not null)
            .Select(g => NormalizeGrant(g.SeasonId, g.JobId, g.Tiers, g.LayerNamespaces))
            .Where(record => record is not null)
            .Cast<MeshProfileGrantRecord>()
            .ToArray();

        return new MeshShareProfileRecord(grants);
    }

    private static MeshSubscribeProfileRecord NormalizeSubscribeProfile(MeshSubscribeProfile? profile)
    {
        if (profile is null || profile.Grants.Count == 0)
        {
            return new MeshSubscribeProfileRecord(Array.Empty<MeshProfileGrantRecord>());
        }

        var grants = profile.Grants
            .Where(g => g is not null)
            .Select(g => NormalizeGrant(g.SeasonId, g.JobId, g.Tiers, g.LayerNamespaces))
            .Where(record => record is not null)
            .Cast<MeshProfileGrantRecord>()
            .ToArray();

        return new MeshSubscribeProfileRecord(grants);
    }

    private static MeshProfileGrantRecord? NormalizeGrant(string seasonId, string jobId, MeshDataTier tier, IReadOnlyCollection<string>? layers)
    {
        if (tier == MeshDataTier.None)
        {
            return null;
        }

        var (seasonValue, anySeason) = NormalizeGrantIdentifier(seasonId);
        var (jobValue, anyJob) = NormalizeGrantIdentifier(jobId);
        var (layerSet, anyLayer) = NormalizeGrantLayers(layers);

        return new MeshProfileGrantRecord(seasonValue, anySeason, jobValue, anyJob, tier, layerSet, anyLayer);
    }

    private static (string? Value, bool AllowAny) NormalizeGrantIdentifier(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Grant identifier must be provided.");
        }

        var trimmed = value.Trim();
        if (string.Equals(trimmed, "*", StringComparison.Ordinal))
        {
            return (null, true);
        }

        return (trimmed, false);
    }

    private static (HashSet<string>? Layers, bool AllowAny) NormalizeGrantLayers(IReadOnlyCollection<string>? values)
    {
        if (values is null || values.Count == 0)
        {
            return (null, true);
        }

        var set = NormalizeLayerSet(values);
        return set is null || set.Count == 0 ? (null, true) : (set, false);
    }

    private static HashSet<string>? NormalizeLayerSet(IReadOnlyCollection<string>? layers)
    {
        if (layers is null)
        {
            return null;
        }

        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var layer in layers)
        {
            if (string.IsNullOrWhiteSpace(layer))
            {
                continue;
            }

            set.Add(layer.Trim());
        }

        return set.Count == 0 ? null : set;
    }

    private static MeshSessionDescriptor SanitizeSession(MeshSessionDescriptor session)
    {
        ArgumentNullException.ThrowIfNull(session);

        var seasonId = NormalizeIdentifier(session.SeasonId);
        var jobId = NormalizeIdentifier(session.JobId);
        var sessionId = string.IsNullOrWhiteSpace(session.SessionId) ? null : session.SessionId.Trim();
        var layerNamespace = string.IsNullOrWhiteSpace(session.LayerNamespace) ? null : session.LayerNamespace.Trim();
        return new MeshSessionDescriptor(seasonId, jobId, sessionId, layerNamespace);
    }

    private static IReadOnlyDictionary<string, string> SanitizeMetadata(IReadOnlyDictionary<string, string>? metadata)
    {
        if (metadata is null || metadata.Count == 0)
        {
            return new ReadOnlyDictionary<string, string>(new Dictionary<string, string>());
        }

        var dictionary = new Dictionary<string, string>(metadata.Count, StringComparer.OrdinalIgnoreCase);
        foreach (var kvp in metadata)
        {
            if (string.IsNullOrWhiteSpace(kvp.Key))
            {
                continue;
            }

            dictionary[kvp.Key.Trim()] = kvp.Value?.Trim() ?? string.Empty;
        }

        return new ReadOnlyDictionary<string, string>(dictionary);
    }

    private static MeshTopicParts ParseTopic(string topic)
    {
        if (string.IsNullOrWhiteSpace(topic))
        {
            throw new ArgumentException("Topic is required.", nameof(topic));
        }

        var trimmed = topic.Trim();
        var segments = trimmed.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length != 5)
        {
            throw new ArgumentException("Topic must follow 'aog/live/{season}/{job}/{layer}'.", nameof(topic));
        }

        if (!string.Equals(segments[0], "aog", StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(segments[1], "live", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Topic must begin with 'aog/live'.", nameof(topic));
        }

        var season = NormalizeIdentifier(segments[2]);
        var job = NormalizeIdentifier(segments[3]);
        var layer = segments[4].Trim();
        if (layer.Length == 0)
        {
            throw new ArgumentException("Layer namespace must be provided.", nameof(topic));
        }

        var normalizedTopic = $"aog/live/{season}/{job}/{layer}";
        return new MeshTopicParts(normalizedTopic, season, job, layer);
    }

    private static void EnsureTierMatchesTopic(MeshDataTier tier, string layerNamespace)
    {
        if (tier == MeshDataTier.None)
        {
            throw new ArgumentException("Tier must be specified.");
        }

        var expected = layerNamespace.Equals("presence", StringComparison.OrdinalIgnoreCase)
            ? MeshDataTier.Presence
            : layerNamespace.Equals("coverage", StringComparison.OrdinalIgnoreCase)
                ? MeshDataTier.Coverage
                : layerNamespace.Equals("trail", StringComparison.OrdinalIgnoreCase) || layerNamespace.Equals("trails", StringComparison.OrdinalIgnoreCase)
                    ? MeshDataTier.Trails
                    : MeshDataTier.Layers;

        if ((tier & expected) == 0)
        {
            throw new ArgumentException($"Tier '{tier}' does not match the layer namespace '{layerNamespace}'.", nameof(tier));
        }
    }

    private void Broadcast(MeshPublication publication, MeshSubscriptionRecord[] subscriptions)
    {
        if (subscriptions.Length == 0)
        {
            return;
        }

        foreach (var subscription in subscriptions)
        {
            if (!subscription.Filters.Any(filter => filter.Matches(publication)))
            {
                continue;
            }

            var writer = subscription.Channel.Writer;
            if (!writer.TryWrite(publication))
            {
                lock (_gate)
                {
                    _subscriptions.Remove(subscription.Id);
                }
            }
        }
    }

    private void PrunePresence(DateTimeOffset now)
    {
        var expired = _presence
            .Where(pair => pair.Value.ExpiresAt <= now)
            .Select(pair => pair.Key)
            .ToArray();

        foreach (var key in expired)
        {
            _presence.Remove(key);
        }
    }

    private async IAsyncEnumerable<MeshPublication> ReadAsync(Channel<MeshPublication> channel, Guid subscriptionId, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using var registration = cancellationToken.Register(() => channel.Writer.TryComplete());

        try
        {
            await foreach (var publication in channel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
            {
                yield return publication;
            }
        }
        finally
        {
            lock (_gate)
            {
                if (_subscriptions.Remove(subscriptionId, out var subscription))
                {
                    subscription.Channel.Writer.TryComplete();
                }
            }
        }
    }

    private sealed record MeshTopicParts(string Topic, string SeasonId, string JobId, string LayerNamespace);

    private sealed class MeshDeviceRecord
    {
        public MeshDeviceRecord(string deviceId, string label, string[] capabilities, MeshShareProfileRecord shareProfile, MeshSubscribeProfileRecord subscribeProfile)
        {
            DeviceId = deviceId;
            Label = label;
            Capabilities = capabilities;
            ShareProfile = shareProfile;
            SubscribeProfile = subscribeProfile;
        }

        public string DeviceId { get; }

        public string Label { get; private set; }

        public string[] Capabilities { get; private set; }

        public MeshShareProfileRecord ShareProfile { get; private set; }

        public MeshSubscribeProfileRecord SubscribeProfile { get; private set; }

        public void Update(string label, string[] capabilities, MeshShareProfileRecord shareProfile, MeshSubscribeProfileRecord subscribeProfile)
        {
            Label = label;
            Capabilities = capabilities;
            ShareProfile = shareProfile;
            SubscribeProfile = subscribeProfile;
        }
    }

    private sealed class MeshPresenceEntry
    {
        public MeshPresenceEntry(MeshPresenceSnapshot snapshot, DateTimeOffset expiresAt)
        {
            Snapshot = snapshot;
            ExpiresAt = expiresAt;
        }

        public MeshPresenceSnapshot Snapshot { get; }

        public DateTimeOffset ExpiresAt { get; }
    }

    private sealed class MeshShareProfileRecord
    {
        public MeshShareProfileRecord(IReadOnlyList<MeshProfileGrantRecord> grants)
        {
            Grants = grants;
        }

        public IReadOnlyList<MeshProfileGrantRecord> Grants { get; }

        public bool Allows(string seasonId, string jobId, string layerNamespace, MeshDataTier tier)
        {
            foreach (var grant in Grants)
            {
                if (!grant.Allows(seasonId, jobId, layerNamespace, tier))
                {
                    continue;
                }

                return true;
            }

            return false;
        }
    }

    private sealed class MeshSubscribeProfileRecord
    {
        public MeshSubscribeProfileRecord(IReadOnlyList<MeshProfileGrantRecord> grants)
        {
            Grants = grants;
        }

        public IReadOnlyList<MeshProfileGrantRecord> Grants { get; }

        public MeshSubscriptionFilter[] CreateFilters(string? seasonFilter, string? jobFilter, HashSet<string>? layerFilter, MeshDataTier tierMask)
        {
            if (Grants.Count == 0 || tierMask == MeshDataTier.None)
            {
                return Array.Empty<MeshSubscriptionFilter>();
            }

            var filters = new List<MeshSubscriptionFilter>();
            foreach (var grant in Grants)
            {
                var tiers = grant.Tiers & tierMask;
                if (tiers == MeshDataTier.None)
                {
                    continue;
                }

                var season = MergeFilter(grant.SeasonId, grant.AllowsAnySeason, seasonFilter);
                if (season == MeshSubscriptionFilter.FilterRejected)
                {
                    continue;
                }

                var job = MergeFilter(grant.JobId, grant.AllowsAnyJob, jobFilter);
                if (job == MeshSubscriptionFilter.FilterRejected)
                {
                    continue;
                }

                var layers = MergeLayerFilter(grant.LayerNamespaces, grant.AllowsAnyLayer, layerFilter);
                if (layers == MeshSubscriptionFilter.LayerFilterRejected)
                {
                    continue;
                }

                filters.Add(new MeshSubscriptionFilter(
                    season == MeshSubscriptionFilter.FilterWildcard ? null : season,
                    job == MeshSubscriptionFilter.FilterWildcard ? null : job,
                    tiers,
                    layers));
            }

            return filters.ToArray();
        }

        private static string MergeFilter(string? grantValue, bool allowAnyGrant, string? requested)
        {
            if (requested is not null)
            {
                if (allowAnyGrant)
                {
                    return requested;
                }

                return string.Equals(grantValue, requested, StringComparison.OrdinalIgnoreCase)
                    ? requested
                    : MeshSubscriptionFilter.FilterRejected;
            }

            if (allowAnyGrant)
            {
                return MeshSubscriptionFilter.FilterWildcard;
            }

            return grantValue ?? MeshSubscriptionFilter.FilterRejected;
        }

        private static HashSet<string>? MergeLayerFilter(HashSet<string>? grantLayers, bool allowAny, HashSet<string>? requested)
        {
            if (requested is not null)
            {
                if (requested.Count == 0)
                {
                    return MeshSubscriptionFilter.LayerFilterRejected;
                }

                if (allowAny)
                {
                    return new HashSet<string>(requested, StringComparer.OrdinalIgnoreCase);
                }

                if (grantLayers is null || grantLayers.Count == 0)
                {
                    return MeshSubscriptionFilter.LayerFilterRejected;
                }

                var intersection = requested
                    .Where(layer => grantLayers.Contains(layer))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                return intersection.Count == 0 ? MeshSubscriptionFilter.LayerFilterRejected : intersection;
            }

            if (allowAny)
            {
                return null;
            }

            return grantLayers is null || grantLayers.Count == 0
                ? MeshSubscriptionFilter.LayerFilterRejected
                : new HashSet<string>(grantLayers, StringComparer.OrdinalIgnoreCase);
        }
    }

    private sealed class MeshProfileGrantRecord
    {
        public MeshProfileGrantRecord(string? seasonId, bool allowsAnySeason, string? jobId, bool allowsAnyJob, MeshDataTier tiers, HashSet<string>? layerNamespaces, bool allowsAnyLayer)
        {
            SeasonId = seasonId;
            AllowsAnySeason = allowsAnySeason;
            JobId = jobId;
            AllowsAnyJob = allowsAnyJob;
            Tiers = tiers;
            LayerNamespaces = layerNamespaces;
            AllowsAnyLayer = allowsAnyLayer;
        }

        public string? SeasonId { get; }

        public bool AllowsAnySeason { get; }

        public string? JobId { get; }

        public bool AllowsAnyJob { get; }

        public MeshDataTier Tiers { get; }

        public HashSet<string>? LayerNamespaces { get; }

        public bool AllowsAnyLayer { get; }

        public bool Allows(string seasonId, string jobId, string layerNamespace, MeshDataTier tier)
        {
            if ((Tiers & tier) == 0)
            {
                return false;
            }

            if (!AllowsAnySeason && !string.Equals(SeasonId, seasonId, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!AllowsAnyJob && !string.Equals(JobId, jobId, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (AllowsAnyLayer)
            {
                return true;
            }

            return LayerNamespaces is not null && LayerNamespaces.Contains(layerNamespace);
        }
    }

    private sealed class MeshSubscriptionFilter
    {
        public const string FilterRejected = "__rejected__";
        public const string FilterWildcard = "__wildcard__";
        public static readonly HashSet<string> LayerFilterRejected = new();

        public MeshSubscriptionFilter(string? seasonId, string? jobId, MeshDataTier tierMask, HashSet<string>? layers)
        {
            SeasonId = seasonId;
            JobId = jobId;
            TierMask = tierMask;
            LayerNamespaces = layers;
        }

        public string? SeasonId { get; }

        public string? JobId { get; }

        public MeshDataTier TierMask { get; }

        public HashSet<string>? LayerNamespaces { get; }

        public bool Matches(MeshPublication publication)
        {
            if ((TierMask & publication.Tier) == 0)
            {
                return false;
            }

            if (SeasonId is not null && !string.Equals(SeasonId, publication.SeasonId, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (JobId is not null && !string.Equals(JobId, publication.JobId, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (LayerNamespaces is null)
            {
                return true;
            }

            return LayerNamespaces.Contains(publication.LayerNamespace);
        }
    }

    private sealed class MeshSubscriptionRecord
    {
        public MeshSubscriptionRecord(Guid id, string deviceId, Channel<MeshPublication> channel, MeshSubscriptionFilter[] filters)
        {
            Id = id;
            DeviceId = deviceId;
            Channel = channel;
            Filters = filters;
        }

        public Guid Id { get; }

        public string DeviceId { get; }

        public Channel<MeshPublication> Channel { get; }

        public MeshSubscriptionFilter[] Filters { get; }
    }
}
