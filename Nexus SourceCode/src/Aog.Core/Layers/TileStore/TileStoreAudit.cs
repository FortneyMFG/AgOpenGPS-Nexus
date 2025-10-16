using System;

namespace Aog.Core.Layers.TileStore;

/// <summary>
/// Audit sink invoked by the TileStore for provenance logging.
/// </summary>
public interface ITileStoreAuditSink
{
    /// <summary>Records an audit entry emitted by the TileStore.</summary>
    /// <param name="entry">Entry describing the event.</param>
    void Record(TileStoreAuditEntry entry);
}

/// <summary>Audit entry emitted by the TileStore for durability and maintenance events.</summary>
public sealed record TileStoreAuditEntry(
    DateTimeOffset Timestamp,
    string EventType,
    string? TileId,
    string Details)
{
    /// <summary>Creates an audit entry representing TileStore mount.</summary>
    public static TileStoreAuditEntry StoreMounted(DateTimeOffset timestamp, long liveRecords, long totalRecords, int segmentCount) =>
        new(timestamp, "store.mounted", null, FormattableString.Invariant($"live={liveRecords};total={totalRecords};segments={segmentCount}"));

    /// <summary>Creates an audit entry for tile append operations.</summary>
    public static TileStoreAuditEntry TileAppended(DateTimeOffset timestamp, string tileId, string segmentName) =>
        new(timestamp, "tile.append", tileId, FormattableString.Invariant($"segment={segmentName}"));

    /// <summary>Creates an audit entry when compaction completes.</summary>
    public static TileStoreAuditEntry CompactionCompleted(DateTimeOffset timestamp, int removedSegments, long totalRecords, long liveRecords) =>
        new(timestamp, "maintenance.compaction", null, FormattableString.Invariant($"removedSegments={removedSegments};total={totalRecords};live={liveRecords}"));

    /// <summary>Creates an audit entry when maintenance triggers compaction.</summary>
    public static TileStoreAuditEntry MaintenanceTriggered(DateTimeOffset timestamp, double liveRatio, long totalRecords) =>
        new(timestamp, "maintenance.trigger", null, FormattableString.Invariant($"liveRatio={liveRatio:F3};total={totalRecords}"));
}
