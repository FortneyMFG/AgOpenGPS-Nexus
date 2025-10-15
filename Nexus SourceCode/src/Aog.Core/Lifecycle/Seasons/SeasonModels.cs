using System.Threading;
using System.Threading.Tasks;

namespace Aog.Core.Lifecycle.Seasons;

/// <summary>
/// Immutable representation of a season organizer document.
/// </summary>
/// <param name="SeasonId">Stable identifier for the season (e.g. <c>season:2025</c>).</param>
/// <param name="Name">Human-readable label for the season.</param>
/// <param name="StartDate">Inclusive start date of the season (if known).</param>
/// <param name="EndDate">Inclusive end date of the season (if known).</param>
/// <param name="JobIds">Jobs associated with the season.</param>
/// <param name="Notes">Freeform operator notes or context.</param>
/// <param name="CreatedBy">Actor that authored the season.</param>
/// <param name="CreatedAt">Timestamp when the season was created.</param>
/// <param name="LastModifiedAt">Timestamp when the season was last modified.</param>
public sealed record SeasonDocument(
    string SeasonId,
    string Name,
    DateOnly? StartDate,
    DateOnly? EndDate,
    IReadOnlyList<string> JobIds,
    string? Notes,
    string? CreatedBy,
    DateTimeOffset? CreatedAt,
    DateTimeOffset? LastModifiedAt);

/// <summary>
/// Snapshot of a season document reported by a specific provider.
/// </summary>
/// <param name="ProviderId">Identifier of the provider that supplied the document.</param>
/// <param name="Document">Season metadata published by the provider.</param>
public sealed record SeasonContributionSnapshot(string ProviderId, SeasonDocument Document);

/// <summary>
/// Aggregated view of a season across all contributing providers.
/// </summary>
/// <param name="Document">Canonical season document produced by the aggregator.</param>
/// <param name="CanonicalProviderId">Provider selected as the canonical source for descriptive metadata.</param>
/// <param name="Sources">Contributions from each provider that participates in the aggregate.</param>
public sealed record SeasonAggregate(
    SeasonDocument Document,
    string CanonicalProviderId,
    IReadOnlyList<SeasonContributionSnapshot> Sources);

/// <summary>
/// Represents a change detected by the season aggregator.
/// </summary>
public readonly struct SeasonAggregationChange
{
    private SeasonAggregationChange(string seasonId, SeasonAggregate? aggregate)
    {
        SeasonId = seasonId;
        Aggregate = aggregate;
    }

    /// <summary>
    /// Identifier of the season affected by the change.
    /// </summary>
    public string SeasonId { get; }

    /// <summary>
    /// Aggregated document when the season is available; <c>null</c> if the
    /// season should be removed from downstream targets.
    /// </summary>
    public SeasonAggregate? Aggregate { get; }

    /// <summary>
    /// Indicates whether the change represents a removal.
    /// </summary>
    public bool IsRemoval => Aggregate is null;

    /// <summary>
    /// Creates a change describing an updated aggregate.
    /// </summary>
    public static SeasonAggregationChange Updated(SeasonAggregate aggregate)
    {
        if (aggregate is null)
        {
            throw new ArgumentNullException(nameof(aggregate));
        }

        return new SeasonAggregationChange(aggregate.Document.SeasonId, aggregate);
    }

    /// <summary>
    /// Creates a change describing the removal of a season.
    /// </summary>
    public static SeasonAggregationChange Removed(string seasonId)
    {
        if (string.IsNullOrWhiteSpace(seasonId))
        {
            throw new ArgumentException("Season identifier is required.", nameof(seasonId));
        }

        return new SeasonAggregationChange(seasonId, null);
    }
}

/// <summary>
/// Downstream target that receives aggregated season updates.
/// </summary>
public interface ISeasonSyncTarget
{
    /// <summary>
    /// Publishes an updated aggregate to the target.
    /// </summary>
    Task PublishAsync(SeasonAggregate aggregate, CancellationToken cancellationToken);

    /// <summary>
    /// Removes a season from the target.
    /// </summary>
    Task RemoveAsync(string seasonId, CancellationToken cancellationToken);
}
