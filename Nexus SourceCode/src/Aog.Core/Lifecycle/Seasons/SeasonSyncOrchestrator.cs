using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Aog.Core.Lifecycle.Seasons;

/// <summary>
/// Coordinates the season aggregator with downstream synchronization targets.
/// </summary>
public sealed class SeasonSyncOrchestrator
{
    private readonly SeasonAggregator _aggregator;
    private readonly ISeasonSyncTarget _target;
    private readonly ILogger<SeasonSyncOrchestrator>? _logger;

    public SeasonSyncOrchestrator(
        SeasonAggregator aggregator,
        ISeasonSyncTarget target,
        ILogger<SeasonSyncOrchestrator>? logger = null)
    {
        _aggregator = aggregator ?? throw new ArgumentNullException(nameof(aggregator));
        _target = target ?? throw new ArgumentNullException(nameof(target));
        _logger = logger;
    }

    /// <summary>
    /// Applies a full snapshot reported by a provider. Any season missing from the snapshot
    /// will be removed for that provider.
    /// </summary>
    public async Task ApplySnapshotAsync(
        string providerId,
        IEnumerable<SeasonDocument> seasons,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(providerId))
        {
            throw new ArgumentException("Provider identifier is required.", nameof(providerId));
        }

        if (seasons is null)
        {
            throw new ArgumentNullException(nameof(seasons));
        }

        var retained = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var season in seasons)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var change = _aggregator.Upsert(providerId, season);
            retained.Add(change.SeasonId);

            if (!change.IsRemoval && change.Aggregate is SeasonAggregate aggregate)
            {
                _logger?.LogDebug(
                    "Publishing season {SeasonId} from provider {ProviderId}.",
                    aggregate.Document.SeasonId,
                    aggregate.CanonicalProviderId);

                await _target.PublishAsync(aggregate, cancellationToken).ConfigureAwait(false);
            }
        }

        foreach (var change in _aggregator.TrimMissing(providerId, retained))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (change.IsRemoval)
            {
                _logger?.LogDebug(
                    "Removing season {SeasonId} after provider {ProviderId} snapshot.",
                    change.SeasonId,
                    providerId);

                await _target.RemoveAsync(change.SeasonId, cancellationToken).ConfigureAwait(false);
            }
            else if (change.Aggregate is SeasonAggregate aggregate)
            {
                _logger?.LogDebug(
                    "Publishing season {SeasonId} after provider {ProviderId} trimmed contributions.",
                    aggregate.Document.SeasonId,
                    providerId);

                await _target.PublishAsync(aggregate, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
