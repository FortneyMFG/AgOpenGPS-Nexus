using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aog.Core.Eventing;

namespace Aog.Plugins.Mapping;

/// <summary>
/// Publishes coverage snapshots when a field accumulates new area.
/// </summary>
public sealed class CoverageFeedPublisher
{
    private readonly FieldStateStore _store;
    private readonly IEventBus _eventBus;
    private readonly double _minimumAreaDelta;
    private readonly TimeSpan _minimumInterval;
    private readonly TimeProvider _timeProvider;
    private readonly Dictionary<string, PublishedState> _published = new(StringComparer.Ordinal);

    /// <summary>
    /// Initializes a new instance of the <see cref="CoverageFeedPublisher"/> class.
    /// </summary>
    /// <param name="store">Field state store providing coverage snapshots.</param>
    /// <param name="eventBus">Event bus used to publish <see cref="FieldCoverageSnapshot"/> messages.</param>
    /// <param name="minimumAreaDelta">Minimum area change (square metres) required between publications.</param>
    /// <param name="minimumInterval">Optional minimum interval between publications.</param>
    /// <param name="timeProvider">Optional time provider for deterministic testing.</param>
    public CoverageFeedPublisher(
        FieldStateStore store,
        IEventBus eventBus,
        double minimumAreaDelta,
        TimeSpan? minimumInterval = null,
        TimeProvider? timeProvider = null)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));

        if (!double.IsFinite(minimumAreaDelta) || minimumAreaDelta < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(minimumAreaDelta), minimumAreaDelta, "Minimum area delta must be a non-negative finite value.");
        }

        _minimumAreaDelta = minimumAreaDelta;

        if (minimumInterval is { } interval && interval < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(minimumInterval), interval, "Minimum interval cannot be negative.");
        }

        _minimumInterval = minimumInterval ?? TimeSpan.Zero;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>
    /// Publishes a coverage snapshot for the specified field when thresholds are satisfied.
    /// </summary>
    public async ValueTask PublishAsync(string fieldId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fieldId))
        {
            throw new ArgumentException("Field identifier is required.", nameof(fieldId));
        }

        var snapshot = _store.GetCoverageSnapshot(fieldId);
        var now = _timeProvider.GetUtcNow();

        if (!_published.TryGetValue(fieldId, out var state))
        {
            await PublishAsync(fieldId, snapshot, now, cancellationToken).ConfigureAwait(false);
            return;
        }

        var areaDelta = Math.Abs(snapshot.TotalAreaSquareMeters - state.TotalAreaSquareMeters);
        var elapsed = now - state.PublishedAt;

        if (areaDelta < _minimumAreaDelta && elapsed < _minimumInterval)
        {
            return;
        }

        await PublishAsync(fieldId, snapshot, now, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Clears publication history allowing the next call to publish regardless of thresholds.
    /// </summary>
    public void Reset(string fieldId)
    {
        if (string.IsNullOrWhiteSpace(fieldId))
        {
            throw new ArgumentException("Field identifier is required.", nameof(fieldId));
        }

        _published.Remove(fieldId);
    }

    private async ValueTask PublishAsync(
        string fieldId,
        FieldCoverageSnapshot snapshot,
        DateTimeOffset timestamp,
        CancellationToken cancellationToken)
    {
        _published[fieldId] = new PublishedState(snapshot.TotalAreaSquareMeters, timestamp);
        await _eventBus.PublishAsync(snapshot, cancellationToken).ConfigureAwait(false);
    }

    private readonly record struct PublishedState(double TotalAreaSquareMeters, DateTimeOffset PublishedAt);
}
