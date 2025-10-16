using System;
using System.Threading;

namespace Aog.Core.Layers.TileStore;

/// <summary>
/// Background maintenance worker responsible for triggering TileStore compaction.
/// </summary>
public sealed class TileStoreMaintenanceWorker
{
    private readonly TileStore _store;
    private readonly TileStoreMaintenanceOptions _options;
    private readonly ITileStoreAuditSink _auditSink;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="TileStoreMaintenanceWorker"/> class.
    /// </summary>
    /// <param name="store">TileStore instance that will be compacted.</param>
    /// <param name="options">Maintenance thresholds controlling compaction cadence.</param>
    /// <param name="auditSink">Optional audit sink for maintenance events.</param>
    public TileStoreMaintenanceWorker(TileStore store, TileStoreMaintenanceOptions? options = null, ITileStoreAuditSink? auditSink = null)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _options = options ?? TileStoreMaintenanceOptions.Default;
        if (_options.TargetLiveRatio <= 0 || _options.TargetLiveRatio > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(options), _options.TargetLiveRatio, "Target live ratio must be within (0, 1].");
        }

        if (_options.MinimumRecordsBeforeCompaction < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), _options.MinimumRecordsBeforeCompaction, "Minimum records must be non-negative.");
        }

        _auditSink = auditSink ?? _options.AuditSink ?? MaintenanceNullAuditSink.Instance;
        _timeProvider = _options.TimeProvider ?? TimeProvider.System;
    }

    /// <summary>
    /// Runs a single maintenance pass, compacting when thresholds are exceeded.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token used to abort maintenance.</param>
    /// <returns><c>true</c> when compaction executed.</returns>
    public bool RunOnce(CancellationToken cancellationToken = default)
    {
        var status = _store.GetCompactionStatus();
        if (status.TotalRecords < _options.MinimumRecordsBeforeCompaction)
        {
            return false;
        }

        if (status.LiveRatio >= _options.TargetLiveRatio)
        {
            return false;
        }

        _auditSink.Record(TileStoreAuditEntry.MaintenanceTriggered(_timeProvider.GetUtcNow(), status.LiveRatio, status.TotalRecords));
        var result = _store.Compact(cancellationToken);
        return result.Compacted;
    }

    private sealed class MaintenanceNullAuditSink : ITileStoreAuditSink
    {
        public static MaintenanceNullAuditSink Instance { get; } = new();

        public void Record(TileStoreAuditEntry entry)
        {
        }
    }
}

/// <summary>Options governing <see cref="TileStoreMaintenanceWorker"/> behaviour.</summary>
public sealed class TileStoreMaintenanceOptions
{
    /// <summary>Gets the default maintenance options.</summary>
    public static TileStoreMaintenanceOptions Default { get; } = new();

    /// <summary>Target live ratio; compaction runs when the store falls below this threshold.</summary>
    public double TargetLiveRatio { get; init; } = 0.85;

    /// <summary>Minimum records required before attempting compaction.</summary>
    public int MinimumRecordsBeforeCompaction { get; init; } = 64;

    /// <summary>Optional time provider used for deterministic audit timestamps.</summary>
    public TimeProvider? TimeProvider { get; init; }

    /// <summary>Optional audit sink that receives maintenance trigger events.</summary>
    public ITileStoreAuditSink? AuditSink { get; init; }
}
