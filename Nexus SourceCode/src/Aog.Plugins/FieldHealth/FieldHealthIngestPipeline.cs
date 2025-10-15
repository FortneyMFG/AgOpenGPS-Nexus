using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Aog.Plugins.FieldHealth;

/// <summary>
/// Aggregates field health risk observations into layer metadata snapshots.
/// </summary>
public sealed class FieldHealthIngestPipeline
{
    private readonly FieldHealthIngestOptions _options;
    private readonly Dictionary<string, FieldHealthObservation> _observations = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, FieldHealthHistoryEntry> _historyCursor = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<FieldHealthHistoryEntry> _historyEntries = new();
    private readonly Dictionary<Guid, FieldHealthAnalyticsCallback> _analyticsCallbacks = new();
    private string? _notes;
    private string? _schemaRef;
    private string[] _tags;
    private FieldHealthHistoryToggles _historyToggles = FieldHealthHistoryToggles.Default;

    /// <summary>
    /// Initializes a new instance of the <see cref="FieldHealthIngestPipeline"/> class.
    /// </summary>
    public FieldHealthIngestPipeline(FieldHealthIngestOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _options.Validate();
        _notes = options.Notes;
        _schemaRef = options.SchemaRef;
        _tags = NormalizeTags(options.Tags);
        LatestMetadata = CreateMetadataSnapshot();
    }

    /// <summary>
    /// Latest metadata snapshot produced by the pipeline.
    /// </summary>
    public FieldHealthLayerMetadata LatestMetadata { get; private set; }

    /// <summary>
    /// Number of observations currently tracked by the pipeline.
    /// </summary>
    public int ObservationCount => _observations.Count;

    /// <summary>
    /// Adds or updates an observation and recomputes metadata when changes occur.
    /// </summary>
    public ValueTask<FieldHealthLayerMetadata> IngestAsync(FieldHealthObservation observation, CancellationToken cancellationToken = default)
    {
        if (observation is null)
        {
            throw new ArgumentNullException(nameof(observation));
        }

        cancellationToken.ThrowIfCancellationRequested();
        observation.Validate();

        var key = observation.FeatureId;
        if (_observations.TryGetValue(key, out var existing) && !ShouldReplace(existing, observation))
        {
            return ValueTask.FromResult(LatestMetadata);
        }

        RecordHistory(observation);

        var previous = LatestMetadata;
        _observations[key] = observation;
        LatestMetadata = CreateMetadataSnapshot();
        NotifyAnalyticsCallbacks(previous);
        return ValueTask.FromResult(LatestMetadata);
    }

    /// <summary>
    /// Removes an observation by feature identifier.
    /// </summary>
    public bool Remove(string featureId)
    {
        if (string.IsNullOrWhiteSpace(featureId))
        {
            throw new ArgumentException("FeatureId must be provided.", nameof(featureId));
        }

        if (_observations.Remove(featureId, out _))
        {
            _historyCursor.Remove(featureId);
            var previous = LatestMetadata;
            LatestMetadata = CreateMetadataSnapshot();
            NotifyAnalyticsCallbacks(previous);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Clears all observations from the pipeline.
    /// </summary>
    public void Clear()
    {
        if (_observations.Count == 0)
        {
            return;
        }

        _observations.Clear();
        _historyCursor.Clear();
        var previous = LatestMetadata;
        LatestMetadata = CreateMetadataSnapshot();
        NotifyAnalyticsCallbacks(previous);
    }

    /// <summary>
    /// Updates layer level context (notes, tags, schema reference) and returns the recomputed metadata.
    /// </summary>
    public FieldHealthLayerMetadata UpdateLayerContext(string? notes, IEnumerable<string>? tags = null, string? schemaRef = null)
    {
        _notes = notes;

        if (tags is not null)
        {
            var tagList = tags.ToArray();
            foreach (var tag in tagList)
            {
                FieldHealthIngestOptions.ValidateTag(tag);
            }

            _tags = NormalizeTags(tagList);
        }

        if (schemaRef is not null)
        {
            if (string.IsNullOrWhiteSpace(schemaRef))
            {
                _schemaRef = null;
            }
            else if (!Uri.IsWellFormedUriString(schemaRef, UriKind.Absolute))
            {
                throw new ArgumentException("SchemaRef must be an absolute URI when provided.", nameof(schemaRef));
            }
            else
            {
                _schemaRef = schemaRef;
            }
        }

        var previous = LatestMetadata;
        LatestMetadata = CreateMetadataSnapshot();
        NotifyAnalyticsCallbacks(previous);
        return LatestMetadata;
    }

    /// <summary>
    /// Updates persisted history toggle selections and returns the recomputed metadata.
    /// </summary>
    public FieldHealthLayerMetadata UpdateHistoryToggles(FieldHealthHistoryToggles toggles)
    {
        if (toggles is null)
        {
            throw new ArgumentNullException(nameof(toggles));
        }

        _historyToggles = toggles;
        var previous = LatestMetadata;
        LatestMetadata = CreateMetadataSnapshot();
        NotifyAnalyticsCallbacks(previous);
        return LatestMetadata;
    }

    /// <summary>
    /// Registers an analytics callback invoked whenever metadata changes.
    /// </summary>
    /// <param name="callback">Callback invoked with the latest and previous metadata snapshots.</param>
    /// <param name="replayLatest">When <c>true</c>, immediately invokes the callback with the current metadata.</param>
    public IDisposable RegisterAnalyticsCallback(FieldHealthAnalyticsCallback callback, bool replayLatest = true)
    {
        if (callback is null)
        {
            throw new ArgumentNullException(nameof(callback));
        }

        var id = Guid.NewGuid();

        lock (_analyticsCallbacks)
        {
            _analyticsCallbacks[id] = callback;
        }

        if (replayLatest)
        {
            callback(LatestMetadata, previous: null);
        }

        return new Subscription(this, id);
    }

    private static bool ShouldReplace(FieldHealthObservation existing, FieldHealthObservation candidate)
    {
        var existingStamp = existing.LastUpdatedAt ?? existing.ObservedAt;
        var candidateStamp = candidate.LastUpdatedAt ?? candidate.ObservedAt;

        if (candidateStamp > existingStamp)
        {
            return true;
        }

        if (candidateStamp < existingStamp)
        {
            return false;
        }

        // If timestamps match, prefer the candidate to ensure deterministic updates when severity or notes change.
        return true;
    }

    private FieldHealthLayerMetadata CreateMetadataSnapshot()
    {
        var observations = _observations.Values
            .OrderByDescending(o => o.ObservedAt)
            .ThenBy(o => o.FeatureId, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var statistics = ComputeStatistics(observations);
        var history = new FieldHealthLayerHistory(_historyEntries, _historyToggles);
        return new FieldHealthLayerMetadata(_options.Kind, _schemaRef, _notes, _tags, observations, statistics, history);
    }

    private static FieldHealthLayerStatistics ComputeStatistics(IReadOnlyList<FieldHealthObservation> observations)
    {
        if (observations.Count == 0)
        {
            return new FieldHealthLayerStatistics(0, new FieldHealthSeverityCounts(0, 0, 0, 0, 0), null);
        }

        double totalArea = 0;
        var counts = new FieldHealthSeverityCounts(0, 0, 0, 0, 0);
        DateTimeOffset? lastSurveyed = null;

        foreach (var observation in observations)
        {
            if (observation.AreaHa is { } area)
            {
                totalArea += area;
            }

            counts = counts.Add(observation.Severity);

            if (lastSurveyed is null || observation.ObservedAt > lastSurveyed)
            {
                lastSurveyed = observation.ObservedAt;
            }
        }

        return new FieldHealthLayerStatistics(totalArea, counts, lastSurveyed);
    }

    private static string[] NormalizeTags(IEnumerable<string> tags)
    {
        return tags
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Select(tag => tag.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private void RecordHistory(FieldHealthObservation observation)
    {
        if (observation.Status is not FieldHealthObservationStatus status)
        {
            return;
        }

        var stamp = observation.LastUpdatedAt ?? observation.ObservedAt;

        if (_historyCursor.TryGetValue(observation.FeatureId, out var last)
            && last.Status == status
            && last.Severity == observation.Severity
            && last.ChangedAt == stamp)
        {
            return;
        }

        var entry = new FieldHealthHistoryEntry(observation.FeatureId, status, observation.Severity, stamp);
        _historyCursor[observation.FeatureId] = entry;
        _historyEntries.Add(entry);
    }

    private void NotifyAnalyticsCallbacks(FieldHealthLayerMetadata previous)
    {
        if (_analyticsCallbacks.Count == 0)
        {
            return;
        }

        FieldHealthAnalyticsCallback[] callbacks;

        lock (_analyticsCallbacks)
        {
            callbacks = _analyticsCallbacks.Values.ToArray();
        }

        foreach (var callback in callbacks)
        {
            callback(LatestMetadata, previous);
        }
    }

    private void UnregisterAnalyticsCallback(Guid id)
    {
        lock (_analyticsCallbacks)
        {
            _analyticsCallbacks.Remove(id);
        }
    }

    private sealed class Subscription : IDisposable
    {
        private FieldHealthIngestPipeline? _pipeline;
        private readonly Guid _id;

        public Subscription(FieldHealthIngestPipeline pipeline, Guid id)
        {
            _pipeline = pipeline;
            _id = id;
        }

        public void Dispose()
        {
            var pipeline = Interlocked.Exchange(ref _pipeline, null);
            pipeline?.UnregisterAnalyticsCallback(_id);
        }
    }
}

/// <summary>
/// Delegate invoked when field health metadata changes.
/// </summary>
/// <param name="current">Latest metadata snapshot.</param>
/// <param name="previous">Previous metadata snapshot, or <c>null</c> for the initial replay.</param>
public delegate void FieldHealthAnalyticsCallback(FieldHealthLayerMetadata current, FieldHealthLayerMetadata? previous);
