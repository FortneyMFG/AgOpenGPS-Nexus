using System;
using System.Collections.Generic;
using System.Linq;

namespace Aog.Core.Mesh;

/// <summary>
/// Stores a bounded retention window of mesh publications for offline sync workers.
/// </summary>
public sealed class MeshRetentionStore
{
    private readonly TimeProvider _timeProvider;
    private readonly MeshRetentionOptions _options;
    private readonly object _gate = new();
    private readonly Dictionary<string, LinkedList<RetainedPublication>> _topics = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, MeshPresenceSnapshot> _presence = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Initializes a new instance of the <see cref="MeshRetentionStore"/> class.
    /// </summary>
    /// <param name="options">Retention configuration.</param>
    /// <param name="timeProvider">Optional time provider for deterministic tests.</param>
    public MeshRetentionStore(MeshRetentionOptions? options = null, TimeProvider? timeProvider = null)
    {
        _options = (options ?? new MeshRetentionOptions()).Clone();
        _timeProvider = timeProvider ?? TimeProvider.System;

        if (_options.RetentionWindow <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Retention window must be positive.");
        }

        if (_options.MaxEntriesPerTopic <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Max entries per topic must be positive.");
        }
    }

    /// <summary>
    /// Records a publication and retains it for the configured window.
    /// </summary>
    /// <param name="publication">Publication to store.</param>
    public void Record(MeshPublication publication)
    {
        ArgumentNullException.ThrowIfNull(publication);

        var capturedAt = _timeProvider.GetUtcNow();
        var clone = ClonePublication(publication);
        var entry = new RetainedPublication(clone, capturedAt);

        lock (_gate)
        {
            if (!_topics.TryGetValue(clone.Topic, out var list))
            {
                list = new LinkedList<RetainedPublication>();
                _topics[clone.Topic] = list;
            }

            list.AddLast(entry);
            TrimTopic(list, capturedAt);

            if (clone.Tier.HasFlag(MeshDataTier.Presence) && clone.State is MeshPresenceSnapshot presence)
            {
                _presence[clone.PublisherDeviceId] = presence;
            }
        }
    }

    /// <summary>
    /// Queries retained publications matching the supplied filter.
    /// </summary>
    /// <param name="query">Query describing desired filters.</param>
    /// <returns>Publications ordered by their published timestamp.</returns>
    public IReadOnlyList<MeshPublication> Query(MeshRetentionQuery? query = null)
    {
        var filter = query ?? MeshRetentionQuery.All;
        var results = new List<MeshPublication>();
        var now = _timeProvider.GetUtcNow();

        lock (_gate)
        {
            foreach (var list in _topics.Values)
            {
                TrimTopic(list, now);

                foreach (var entry in list)
                {
                    var publication = entry.Publication;
                    if (!MatchesFilter(publication, filter))
                    {
                        continue;
                    }

                    results.Add(ClonePublication(publication));
                }
            }
        }

        results.Sort((left, right) => left.PublishedAt.CompareTo(right.PublishedAt));

        if (filter.Limit is { } limit && results.Count > limit)
        {
            results = results.Skip(Math.Max(0, results.Count - limit)).ToList();
        }

        return results;
    }

    /// <summary>
    /// Returns the most recent presence snapshots retained by the store.
    /// </summary>
    /// <param name="seasonId">Optional season filter.</param>
    /// <param name="jobId">Optional job filter.</param>
    /// <returns>Presence snapshots filtered by the provided context.</returns>
    public IReadOnlyList<MeshPresenceSnapshot> ListPresence(string? seasonId = null, string? jobId = null)
    {
        List<MeshPresenceSnapshot>? results = null;
        lock (_gate)
        {
            foreach (var snapshot in _presence.Values)
            {
                if (!string.IsNullOrWhiteSpace(seasonId) &&
                    !string.Equals(snapshot.Session.SeasonId, seasonId, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(jobId) &&
                    !string.Equals(snapshot.Session.JobId, jobId, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                results ??= new List<MeshPresenceSnapshot>();
                results.Add(snapshot);
            }
        }

        return results is null ? Array.Empty<MeshPresenceSnapshot>() : results;
    }

    private static MeshPublication ClonePublication(MeshPublication publication)
    {
        var payload = publication.Payload.ToArray();
        return new MeshPublication(
            publication.PublisherDeviceId,
            publication.Topic,
            publication.SeasonId,
            publication.JobId,
            publication.LayerNamespace,
            publication.Tier,
            publication.PublishedAt,
            new ReadOnlyMemory<byte>(payload),
            publication.Metadata,
            publication.State);
    }

    private bool MatchesFilter(MeshPublication publication, MeshRetentionQuery filter)
    {
        if (filter.TierMask != MeshDataTier.None && (publication.Tier & filter.TierMask) == 0)
        {
            return false;
        }

        if (filter.Since is { } since && publication.PublishedAt < since)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(filter.SeasonId) &&
            !string.Equals(publication.SeasonId, filter.SeasonId, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(filter.JobId) &&
            !string.Equals(publication.JobId, filter.JobId, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(filter.LayerNamespace) &&
            !string.Equals(publication.LayerNamespace, filter.LayerNamespace, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }

    private void TrimTopic(LinkedList<RetainedPublication> list, DateTimeOffset now)
    {
        var cutoff = now - _options.RetentionWindow;

        while (list.First is not null)
        {
            var node = list.First;
            if (node!.Value.CapturedAt >= cutoff && list.Count <= _options.MaxEntriesPerTopic)
            {
                break;
            }

            list.RemoveFirst();
        }
    }

    private sealed record RetainedPublication(MeshPublication Publication, DateTimeOffset CapturedAt);
}

/// <summary>
/// Describes retention settings for the mesh retention store.
/// </summary>
public sealed class MeshRetentionOptions
{
    /// <summary>Gets or sets the amount of time to retain publications.</summary>
    public TimeSpan RetentionWindow { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>Gets or sets the maximum number of entries stored per topic.</summary>
    public int MaxEntriesPerTopic { get; set; } = 256;

    internal MeshRetentionOptions Clone()
    {
        return new MeshRetentionOptions
        {
            RetentionWindow = RetentionWindow,
            MaxEntriesPerTopic = MaxEntriesPerTopic,
        };
    }
}

/// <summary>
/// Filter describing which publications to retrieve from the retention store.
/// </summary>
public sealed record MeshRetentionQuery(
    string? SeasonId = null,
    string? JobId = null,
    string? LayerNamespace = null,
    MeshDataTier TierMask = MeshDataTier.All,
    DateTimeOffset? Since = null,
    int? Limit = null)
{
    /// <summary>Default query that matches all retained publications.</summary>
    public static MeshRetentionQuery All { get; } = new();
}
