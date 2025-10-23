using System;
using System.Collections.Generic;
using System.Linq;

namespace Aog.Core.Lifecycle.Seasons;

/// <summary>
/// Aggregates season documents from multiple providers and produces a canonical snapshot
/// that downstream services can consume.
/// </summary>
public sealed class SeasonAggregator
{
    private readonly Dictionary<string, SeasonAggregateState> _seasons = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _gate = new();

    /// <summary>
    /// Applies a season document reported by a provider and returns the resulting aggregate.
    /// </summary>
    public SeasonAggregationChange Upsert(string providerId, SeasonDocument document)
    {
        if (string.IsNullOrWhiteSpace(providerId))
        {
            throw new ArgumentException("Provider identifier is required.", nameof(providerId));
        }

        if (document is null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        if (string.IsNullOrWhiteSpace(document.SeasonId))
        {
            throw new ArgumentException("Season identifier is required.", nameof(document));
        }

        var sanitized = SanitizeDocument(document);

        lock (_gate)
        {
            if (!_seasons.TryGetValue(sanitized.SeasonId, out var state))
            {
                state = new SeasonAggregateState(sanitized.SeasonId);
                _seasons[sanitized.SeasonId] = state;
            }

            state.Contributions[providerId] = sanitized;
            var aggregate = BuildAggregate(state);
            return SeasonAggregationChange.Updated(aggregate);
        }
    }

    /// <summary>
    /// Removes provider contributions for seasons that are no longer present in the provider snapshot.
    /// </summary>
    public IReadOnlyList<SeasonAggregationChange> TrimMissing(string providerId, ISet<string> seasonIdsToRetain)
    {
        if (string.IsNullOrWhiteSpace(providerId))
        {
            throw new ArgumentException("Provider identifier is required.", nameof(providerId));
        }

        if (seasonIdsToRetain is null)
        {
            throw new ArgumentNullException(nameof(seasonIdsToRetain));
        }

        var changes = new List<SeasonAggregationChange>();

        lock (_gate)
        {
            foreach (var key in _seasons.Keys.ToList())
            {
                var state = _seasons[key];
                if (!state.Contributions.ContainsKey(providerId) || seasonIdsToRetain.Contains(state.SeasonId))
                {
                    continue;
                }

                state.Contributions.Remove(providerId);
                if (state.Contributions.Count == 0)
                {
                    _seasons.Remove(key);
                    changes.Add(SeasonAggregationChange.Removed(state.SeasonId));
                    continue;
                }

                var aggregate = BuildAggregate(state);
                changes.Add(SeasonAggregationChange.Updated(aggregate));
            }
        }

        return changes;
    }

    private static SeasonDocument SanitizeDocument(SeasonDocument document)
    {
        var jobs = (document.JobIds ?? Array.Empty<string>())
            .Select(id => id?.Trim())
            .Where(id => !string.IsNullOrEmpty(id))
            .Select(id => id!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return document with { JobIds = jobs };
    }

    private static SeasonAggregate BuildAggregate(SeasonAggregateState state)
    {
        var contributions = state.Contributions
            .Select(kvp => new SeasonContributionSnapshot(kvp.Key, kvp.Value))
            .ToList();

        var canonical = contributions
            .OrderByDescending(c => c.Document.LastModifiedAt ?? DateTimeOffset.MinValue)
            .ThenBy(c => c.ProviderId, StringComparer.OrdinalIgnoreCase)
            .First();

        var jobs = contributions
            .SelectMany(c => c.Document.JobIds ?? Array.Empty<string>())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var canonicalDocument = canonical.Document with { JobIds = jobs };

        var sources = contributions
            .OrderBy(c => c.ProviderId, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new SeasonAggregate(canonicalDocument, canonical.ProviderId, sources);
    }

    private sealed class SeasonAggregateState
    {
        public SeasonAggregateState(string seasonId)
        {
            SeasonId = seasonId;
        }

        public string SeasonId { get; }

        public Dictionary<string, SeasonDocument> Contributions { get; } = new(StringComparer.OrdinalIgnoreCase);
    }
}
