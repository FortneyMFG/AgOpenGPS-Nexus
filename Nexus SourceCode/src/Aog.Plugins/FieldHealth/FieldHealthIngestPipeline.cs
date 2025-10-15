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
    private string? _notes;
    private string? _schemaRef;
    private string[] _tags;

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

        _observations[key] = observation;
        LatestMetadata = CreateMetadataSnapshot();
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

        var removed = _observations.Remove(featureId);
        if (removed)
        {
            LatestMetadata = CreateMetadataSnapshot();
        }

        return removed;
    }

    /// <summary>
    /// Clears all observations from the pipeline.
    /// </summary>
    public void Clear()
    {
        _observations.Clear();
        LatestMetadata = CreateMetadataSnapshot();
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

        LatestMetadata = CreateMetadataSnapshot();
        return LatestMetadata;
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
        return new FieldHealthLayerMetadata(_options.Kind, _schemaRef, _notes, _tags, observations, statistics);
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
}
