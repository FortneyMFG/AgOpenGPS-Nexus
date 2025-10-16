using System;
using System.Collections.Generic;
using System.Linq;
using Aog.Core.Layers.Controllers;
using Aog.Core.Layers.TileStore;
using Aog.Core.Paths;
using Aog.Core.Tests.Replay;
using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Aog.Core.Tests.Layers.TileStore;

public sealed class TileStoreTests
{
    [Fact]
    public void Append_ShouldPersistTilesAcrossReload()
    {
        using var directory = new TemporaryDirectory("tilestore-tests");
        var time = new FakeTimeProvider(new DateTimeOffset(2025, 5, 5, 6, 30, 0, TimeSpan.Zero));
        var audit = new RecordingAuditSink();
        using (var store = new TileStore(new TileStoreOptions
        {
            Path = directory.Path,
            TimeProvider = time,
            MaxSegmentSizeBytes = 2048,
            MaxRecordsPerSegment = 16,
        }, audit))
        {
            store.Append(CreateTile("tile:controller:alpha:0", time.GetUtcNow()));
        }

        audit.Entries.Should().Contain(e => e.EventType == "tile.append");

        using var reloaded = new TileStore(new TileStoreOptions { Path = directory.Path, TimeProvider = time });
        var tiles = reloaded.GetLiveTiles();
        tiles.Should().ContainSingle(tile => tile.TileId == "tile:controller:alpha:0");
    }

    [Fact]
    public void Compact_ShouldDropStaleRecordsAndCollapseSegments()
    {
        using var directory = new TemporaryDirectory("tilestore-tests");
        var time = new FakeTimeProvider(new DateTimeOffset(2025, 5, 6, 7, 0, 0, TimeSpan.Zero));
        using var store = new TileStore(new TileStoreOptions
        {
            Path = directory.Path,
            TimeProvider = time,
            MaxSegmentSizeBytes = 256,
            MaxRecordsPerSegment = 2,
        });

        var baseTimestamp = time.GetUtcNow();
        for (var i = 0; i < 4; i++)
        {
            var timestamp = baseTimestamp.AddMinutes(i);
            var tileId = "tile:controller:beta";
            store.Append(CreateTile(tileId, timestamp, engineeringValue: i));
        }

        var statusBefore = store.GetCompactionStatus();
        statusBefore.TotalRecords.Should().BeGreaterThan(statusBefore.LiveRecords);
        statusBefore.Segments.Should().HaveCountGreaterThan(1);

        var result = store.Compact();
        result.Compacted.Should().BeTrue();
        result.LiveRecords.Should().Be(result.TotalRecords);

        var statusAfter = store.GetCompactionStatus();
        statusAfter.TotalRecords.Should().Be(statusAfter.LiveRecords);
        statusAfter.Segments.Should().HaveCount(1);

        var tile = store.GetLiveTiles().Single();
        tile.EngineeringValue.Should().Be(3);
    }

    [Fact]
    public void MaintenanceWorker_ShouldTriggerCompactionWhenRatioLow()
    {
        using var directory = new TemporaryDirectory("tilestore-tests");
        var time = new FakeTimeProvider(new DateTimeOffset(2025, 5, 7, 8, 0, 0, TimeSpan.Zero));
        var audit = new RecordingAuditSink();
        using var store = new TileStore(new TileStoreOptions
        {
            Path = directory.Path,
            TimeProvider = time,
            MaxSegmentSizeBytes = 256,
            MaxRecordsPerSegment = 2,
        }, audit);

        for (var i = 0; i < 5; i++)
        {
            store.Append(CreateTile("tile:controller:gamma", time.GetUtcNow().AddMinutes(i), engineeringValue: i));
        }

        var worker = new TileStoreMaintenanceWorker(store, new TileStoreMaintenanceOptions
        {
            TargetLiveRatio = 0.75,
            MinimumRecordsBeforeCompaction = 1,
            TimeProvider = time,
            AuditSink = audit,
        });

        worker.RunOnce().Should().BeTrue();
        audit.Entries.Should().Contain(e => e.EventType == "maintenance.compaction");
    }

    private static LayerControllerTile CreateTile(string tileId, DateTimeOffset capturedAt, double engineeringValue = 1)
    {
        return new LayerControllerTile(
            tileId,
            controllerId: "controller",
            layerId: "layer",
            capturedAt: capturedAt,
            status: ControllerDiagnosticStatus.Nominal,
            statusReason: null,
            containsFreshData: true,
            rateUnavailable: false,
            normalizedValue: engineeringValue,
            engineeringValue: engineeringValue,
            quality: 0.95,
            areaSquareMeters: 1.5,
            minimumValue: engineeringValue,
            maximumValue: engineeringValue,
            sampleCount: 1,
            numerator: 1,
            denominator: 1,
            ratio: 1,
            timeSinceLastSample: TimeSpan.FromMilliseconds(50),
            positions: new[] { new PlanarPoint(1, 2) },
            metrics: new Dictionary<string, double> { ["normalized"] = engineeringValue });
    }

    private sealed class RecordingAuditSink : ITileStoreAuditSink
    {
        private readonly List<TileStoreAuditEntry> _entries = new();

        public IReadOnlyList<TileStoreAuditEntry> Entries => _entries;

        public void Record(TileStoreAuditEntry entry)
        {
            _entries.Add(entry);
        }
    }
}
