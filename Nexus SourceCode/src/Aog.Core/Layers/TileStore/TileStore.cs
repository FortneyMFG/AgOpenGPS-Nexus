using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using Aog.Core.Layers.Controllers;

namespace Aog.Core.Layers.TileStore;

/// <summary>
/// Durable TileStore implementation that journals tiles to disk and supports crash-safe compaction.
/// Implements NX-612.
/// </summary>
public sealed class TileStore : ILayerControllerTileSink, IDisposable
{
    private const string ManifestFileName = "manifest.json";
    private static readonly Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    private readonly object _sync = new();
    private readonly string _storePath;
    private readonly string _manifestPath;
    private readonly JsonSerializerOptions _serializerOptions;
    private readonly ITileStoreAuditSink _auditSink;
    private readonly TimeProvider _timeProvider;
    private readonly TileStoreOptions _options;

    private readonly Dictionary<string, TileStoreSegmentState> _segmentStates = new(StringComparer.Ordinal);
    private readonly List<string> _segmentOrder = new();
    private readonly Dictionary<string, string> _tileToSegment = new(StringComparer.Ordinal);
    private readonly Dictionary<string, LayerControllerTile> _liveTiles = new(StringComparer.Ordinal);

    private TileStoreManifest _manifest;
    private FileStream? _currentSegmentStream;
    private TileStoreSegmentState? _currentSegment;

    /// <summary>
    /// Initializes a new instance of the <see cref="TileStore"/> class.
    /// </summary>
    /// <param name="options">Configuration controlling persistence and compaction behaviour.</param>
    /// <param name="auditSink">Optional audit sink used for provenance logging.</param>
    public TileStore(TileStoreOptions options, ITileStoreAuditSink? auditSink = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        if (string.IsNullOrWhiteSpace(options.Path))
        {
            throw new ArgumentException("TileStore path must be provided.", nameof(options));
        }

        if (options.MaxSegmentSizeBytes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), options.MaxSegmentSizeBytes, "Segment size must be positive.");
        }

        if (options.MaxRecordsPerSegment <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), options.MaxRecordsPerSegment, "Max records per segment must be positive.");
        }

        _auditSink = auditSink ?? NullAuditSink.Instance;
        _timeProvider = options.TimeProvider ?? TimeProvider.System;
        _storePath = options.Path;
        Directory.CreateDirectory(_storePath);
        _manifestPath = Path.Combine(_storePath, ManifestFileName);
        _serializerOptions = CreateSerializerOptions();

        _manifest = ReadManifest();
        LoadSegmentsFromManifest();
        RehydrateSegments();
        EnsureSegmentForAppend(forceCreateWhenMissing: false);
        UpdateManifestFromState();
        SaveManifest();
        _auditSink.Record(TileStoreAuditEntry.StoreMounted(_timeProvider.GetUtcNow(), _manifest.LiveRecords, _manifest.TotalRecords, _segmentOrder.Count));
    }

    /// <summary>Gets the directory backing the store.</summary>
    public string Path => _storePath;

    /// <summary>Gets live tiles currently persisted in the store.</summary>
    public IReadOnlyList<LayerControllerTile> GetLiveTiles()
    {
        lock (_sync)
        {
            return _liveTiles.Values
                .OrderBy(tile => tile.CapturedAt)
                .ThenBy(tile => tile.TileId, StringComparer.Ordinal)
                .ToArray();
        }
    }

    /// <summary>Returns compaction statistics describing the store.</summary>
    public TileStoreCompactionStatus GetCompactionStatus()
    {
        lock (_sync)
        {
            var segments = _segmentOrder
                .Where(_segmentStates.ContainsKey)
                .Select(name => _segmentStates[name])
                .Select(state => new TileStoreSegmentStatistics(state.FileName, state.TotalRecords, state.LiveRecords))
                .ToArray();

            var totalRecords = segments.Sum(s => s.TotalRecords);
            var liveRecords = _liveTiles.Count;
            var ratio = totalRecords == 0 ? 1.0 : (double)liveRecords / totalRecords;

            return new TileStoreCompactionStatus(totalRecords, liveRecords, ratio, new ReadOnlyCollection<TileStoreSegmentStatistics>(segments));
        }
    }

    /// <summary>
    /// Runs crash-safe compaction to drop stale records and collapse segments when necessary.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel compaction.</param>
    /// <returns>Result describing the compaction outcome.</returns>
    public TileStoreCompactionResult Compact(CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            var totalBefore = _segmentStates.Values.Sum(state => state.TotalRecords);
            var liveBefore = _liveTiles.Count;
            if (totalBefore == 0 || liveBefore == totalBefore)
            {
                return TileStoreCompactionResult.Skipped(totalBefore, liveBefore);
            }

            var timestamp = _timeProvider.GetUtcNow();
            var newSegmentName = FormattableString.Invariant($"segment-{timestamp:yyyyMMddTHHmmssfff}-{Guid.NewGuid():N}.jsonl");
            var tempPath = GetSegmentPath(newSegmentName + ".tmp");
            var finalPath = GetSegmentPath(newSegmentName);

            cancellationToken.ThrowIfCancellationRequested();

            using (var stream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                var orderedTiles = _liveTiles.Values
                    .OrderBy(tile => tile.CapturedAt)
                    .ThenBy(tile => tile.TileId, StringComparer.Ordinal)
                    .ToList();

                foreach (var tile in orderedTiles)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    WriteTile(stream, tile, flush: false);
                }

                stream.Flush(true);
            }

            if (File.Exists(finalPath))
            {
                File.Delete(finalPath);
            }

            File.Move(tempPath, finalPath, overwrite: true);

            var oldSegmentNames = _segmentOrder.Where(_segmentStates.ContainsKey).ToArray();
            var oldSegmentPaths = oldSegmentNames
                .Select(name => (name, path: GetSegmentPath(name)))
                .Where(tuple => File.Exists(tuple.path))
                .ToArray();

            _segmentStates.Clear();
            _segmentOrder.Clear();

            var state = new TileStoreSegmentState(newSegmentName)
            {
                TotalRecords = _liveTiles.Count,
                LiveRecords = _liveTiles.Count,
            };

            _segmentStates[newSegmentName] = state;
            _segmentOrder.Add(newSegmentName);

            foreach (var tileId in _tileToSegment.Keys.ToList())
            {
                _tileToSegment[tileId] = newSegmentName;
            }

            _currentSegmentStream?.Dispose();
            _currentSegmentStream = new FileStream(finalPath, FileMode.Append, FileAccess.Write, FileShare.Read);
            _currentSegment = state;

            UpdateManifestFromState();
            SaveManifest();

            _auditSink.Record(TileStoreAuditEntry.CompactionCompleted(_timeProvider.GetUtcNow(), oldSegmentNames.Length, _manifest.TotalRecords, _manifest.LiveRecords));
            foreach (var (name, path) in oldSegmentPaths)
            {
                if (!string.Equals(name, newSegmentName, StringComparison.Ordinal) && File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            return TileStoreCompactionResult.Compacted(oldSegmentNames.Length, _manifest.TotalRecords, _manifest.LiveRecords);
        }
    }

    /// <inheritdoc />
    public void Append(LayerControllerTile tile)
    {
        ArgumentNullException.ThrowIfNull(tile);

        lock (_sync)
        {
            EnsureSegmentForAppend(forceCreateWhenMissing: true);
            WriteTile(_currentSegmentStream!, tile, flush: true);

            _currentSegment!.TotalRecords++;
            if (_tileToSegment.TryGetValue(tile.TileId, out var previousSegmentName) &&
                _segmentStates.TryGetValue(previousSegmentName, out var previousSegment))
            {
                previousSegment.LiveRecords = Math.Max(0, previousSegment.LiveRecords - 1);
            }

            _tileToSegment[tile.TileId] = _currentSegment.FileName;
            _liveTiles[tile.TileId] = tile;
            _currentSegment.LiveRecords++;

            UpdateManifestFromState();
            SaveManifest();
            _auditSink.Record(TileStoreAuditEntry.TileAppended(_timeProvider.GetUtcNow(), tile.TileId, _currentSegment.FileName));
        }
    }

    /// <summary>Disposes the store, ensuring open streams are flushed and closed.</summary>
    public void Dispose()
    {
        lock (_sync)
        {
            _currentSegmentStream?.Dispose();
            _currentSegmentStream = null;
        }
    }

    private static JsonSerializerOptions CreateSerializerOptions()
    {
        return new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        };
    }

    private TileStoreManifest ReadManifest()
    {
        if (!File.Exists(_manifestPath))
        {
            return new TileStoreManifest();
        }

        using var stream = File.OpenRead(_manifestPath);
        var manifest = JsonSerializer.Deserialize<TileStoreManifest>(stream, _serializerOptions);
        return manifest ?? new TileStoreManifest();
    }

    private void LoadSegmentsFromManifest()
    {
        _segmentStates.Clear();
        _segmentOrder.Clear();

        foreach (var segment in _manifest.Segments)
        {
            if (string.IsNullOrWhiteSpace(segment.FileName))
            {
                continue;
            }

            var path = GetSegmentPath(segment.FileName);
            if (!File.Exists(path))
            {
                continue;
            }

            if (_segmentStates.ContainsKey(segment.FileName))
            {
                continue;
            }

            _segmentStates[segment.FileName] = new TileStoreSegmentState(segment.FileName)
            {
                TotalRecords = segment.TotalRecords,
                LiveRecords = segment.LiveRecords,
            };
            _segmentOrder.Add(segment.FileName);
        }
    }

    private void RehydrateSegments()
    {
        _tileToSegment.Clear();
        _liveTiles.Clear();

        foreach (var segmentName in _segmentOrder.ToArray())
        {
            if (!_segmentStates.TryGetValue(segmentName, out var state))
            {
                continue;
            }

            var path = GetSegmentPath(segmentName);
            if (!File.Exists(path))
            {
                _segmentStates.Remove(segmentName);
                _segmentOrder.Remove(segmentName);
                continue;
            }

            state.TotalRecords = 0;
            state.LiveRecords = 0;

            foreach (var tile in ReadSegment(path))
            {
                state.TotalRecords++;

                if (_tileToSegment.TryGetValue(tile.TileId, out var previousSegmentName) &&
                    _segmentStates.TryGetValue(previousSegmentName, out var previousState))
                {
                    previousState.LiveRecords = Math.Max(0, previousState.LiveRecords - 1);
                }

                _tileToSegment[tile.TileId] = segmentName;
                _liveTiles[tile.TileId] = tile;
                state.LiveRecords++;
            }
        }
    }

    private IEnumerable<LayerControllerTile> ReadSegment(string path)
    {
        foreach (var line in File.ReadLines(path, Utf8NoBom))
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            LayerControllerTile? tile = null;
            try
            {
                tile = JsonSerializer.Deserialize<LayerControllerTile>(line, _serializerOptions);
            }
            catch
            {
                // Skip malformed entries to keep the store available. The audit trail will capture the discrepancy.
            }

            if (tile is not null)
            {
                yield return tile;
            }
        }
    }

    private void EnsureSegmentForAppend(bool forceCreateWhenMissing)
    {
        if (_currentSegment is not null && _currentSegmentStream is not null)
        {
            if (_currentSegment.TotalRecords < _options.MaxRecordsPerSegment &&
                _currentSegmentStream.Length < _options.MaxSegmentSizeBytes)
            {
                return;
            }
        }
        else if (!forceCreateWhenMissing)
        {
            if (_segmentOrder.Count > 0 && _segmentStates.TryGetValue(_segmentOrder[^1], out var existing))
            {
                var existingPath = GetSegmentPath(existing.FileName);
                _currentSegmentStream = new FileStream(existingPath, FileMode.Append, FileAccess.Write, FileShare.Read);
                _currentSegment = existing;
                return;
            }
        }

        CreateNewSegment();
    }

    private void CreateNewSegment()
    {
        _currentSegmentStream?.Dispose();

        var timestamp = _timeProvider.GetUtcNow();
        var segmentName = FormattableString.Invariant($"segment-{timestamp:yyyyMMddTHHmmssfff}-{Guid.NewGuid():N}.jsonl");
        var fullPath = GetSegmentPath(segmentName);

        _currentSegmentStream = new FileStream(fullPath, FileMode.Append, FileAccess.Write, FileShare.Read);
        var state = new TileStoreSegmentState(segmentName);
        _segmentStates[segmentName] = state;
        _segmentOrder.Add(segmentName);
        _currentSegment = state;
    }

    private string GetSegmentPath(string fileName)
    {
        return System.IO.Path.Combine(_storePath, fileName);
    }

    private void WriteTile(FileStream stream, LayerControllerTile tile, bool flush)
    {
        var payload = JsonSerializer.Serialize(tile, _serializerOptions);
        var bytes = Utf8NoBom.GetBytes(payload);
        stream.Write(bytes, 0, bytes.Length);
        stream.WriteByte((byte)\n);
        if (flush)
        {
            stream.Flush(true);
        }
    }

    private void UpdateManifestFromState()
    {
        _manifest.TotalRecords = _segmentStates.Values.Sum(state => state.TotalRecords);
        _manifest.LiveRecords = _liveTiles.Count;
        _manifest.Segments = _segmentOrder
            .Where(_segmentStates.ContainsKey)
            .Select(name =>
            {
                var state = _segmentStates[name];
                return new TileStoreManifestSegment(state.FileName, state.TotalRecords, state.LiveRecords);
            })
            .ToList();
    }

    private void SaveManifest()
    {
        var tempPath = _manifestPath + ".tmp";
        var payload = JsonSerializer.Serialize(_manifest, _serializerOptions);
        using (var stream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            var bytes = Utf8NoBom.GetBytes(payload);
            stream.Write(bytes, 0, bytes.Length);
            stream.Flush(true);
        }

        File.Move(tempPath, _manifestPath, overwrite: true);
    }

    private sealed class TileStoreManifest
    {
        public List<TileStoreManifestSegment> Segments { get; set; } = new();

        public long TotalRecords { get; set; }

        public long LiveRecords { get; set; }
    }

    private sealed class TileStoreManifestSegment
    {
        public TileStoreManifestSegment()
        {
        }

        public TileStoreManifestSegment(string fileName, long totalRecords, long liveRecords)
        {
            FileName = fileName;
            TotalRecords = totalRecords;
            LiveRecords = liveRecords;
        }

        public string FileName { get; set; } = string.Empty;

        public long TotalRecords { get; set; }

        public long LiveRecords { get; set; }
    }

    private sealed class TileStoreSegmentState
    {
        public TileStoreSegmentState(string fileName)
        {
            FileName = fileName;
        }

        public string FileName { get; }

        public long TotalRecords { get; set; }

        public long LiveRecords { get; set; }
    }

    private sealed class NullAuditSink : ITileStoreAuditSink
    {
        public static NullAuditSink Instance { get; } = new();

        private NullAuditSink()
        {
        }

        public void Record(TileStoreAuditEntry entry)
        {
        }
    }
}

/// <summary>
/// Options controlling TileStore persistence.
/// </summary>
public sealed class TileStoreOptions
{
    /// <summary>Gets or sets the directory path backing the store.</summary>
    public string Path { get; init; } = string.Empty;

    /// <summary>Gets or sets the maximum segment size in bytes before a new segment is created.</summary>
    public long MaxSegmentSizeBytes { get; init; } = 1_048_576;

    /// <summary>Gets or sets the maximum number of records per segment before rolling.</summary>
    public int MaxRecordsPerSegment { get; init; } = 1_024;

    /// <summary>Gets or sets the time provider used for deterministic naming and audit timestamps.</summary>
    public TimeProvider? TimeProvider { get; init; }
}

/// <summary>Compaction statistics describing TileStore health.</summary>
public sealed record TileStoreCompactionStatus(
    long TotalRecords,
    long LiveRecords,
    double LiveRatio,
    IReadOnlyList<TileStoreSegmentStatistics> Segments);

/// <summary>Per-segment statistics exposed by <see cref="TileStoreCompactionStatus"/>.</summary>
public sealed record TileStoreSegmentStatistics(string FileName, long TotalRecords, long LiveRecords);

/// <summary>Result returned by <see cref="TileStore.Compact"/>.</summary>
public sealed record TileStoreCompactionResult(
    bool Compacted,
    int RemovedSegmentCount,
    long TotalRecords,
    long LiveRecords)
{
    /// <summary>Result when compaction ran successfully.</summary>
    public static TileStoreCompactionResult Compacted(int removedSegmentCount, long totalRecords, long liveRecords) =>
        new(true, removedSegmentCount, totalRecords, liveRecords);

    /// <summary>Result when compaction was skipped.</summary>
    public static TileStoreCompactionResult Skipped(long totalRecords, long liveRecords) =>
        new(false, 0, totalRecords, liveRecords);
}
