using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Aog.Core.Jobs;

/// <summary>
/// Configuration applied to <see cref="SessionAutosavePipeline"/>. Defaults
/// align with SRS requirements (60 second cadence, 5 MB journal threshold).
/// </summary>
public sealed record SessionAutosavePipelineOptions
{
    public static readonly TimeSpan DefaultAutosaveInterval = TimeSpan.FromMinutes(1);
    public const int DefaultJournalBatchSize = 10;
    public const int DefaultJournalSizeThresholdBytes = 5 * 1024 * 1024;

    public TimeSpan AutosaveInterval { get; init; } = DefaultAutosaveInterval;
    public int JournalBatchSize { get; init; } = DefaultJournalBatchSize;
    public int JournalSizeThresholdBytes { get; init; } = DefaultJournalSizeThresholdBytes;
}

/// <summary>
/// Persists session autosave checkpoints and journal batches to durable
/// storage. Implementations typically write to disk or forward to a sync
/// service.
/// </summary>
public interface ISessionAutosaveSink
{
    Task PersistAsync(SessionDocument session, IReadOnlyList<SessionJournalEntry> journalEntries, CancellationToken cancellationToken);
}

/// <summary>
/// Coordinates in-memory session metadata and journal buffers, flushing them to
/// an <see cref="ISessionAutosaveSink"/> based on the autosave cadence or when
/// batch/size thresholds are exceeded.
/// </summary>
public sealed class SessionAutosavePipeline : IAsyncDisposable
{
    private readonly ISessionAutosaveSink _sink;
    private readonly TimeProvider _timeProvider;
    private readonly TimeSpan _autosaveInterval;
    private readonly int _journalBatchSize;
    private readonly int _journalSizeThresholdBytes;
    private readonly object _mutex = new();

    private SessionDocument _session;
    private bool _sessionDirty;
    private readonly List<SessionJournalEntry> _journalBuffer = new();
    private int _journalBytes;
    private DateTimeOffset _nextAutosaveAt;
    private bool _disposed;

    public SessionAutosavePipeline(
        SessionDocument initialSession,
        ISessionAutosaveSink sink,
        SessionAutosavePipelineOptions? options = null,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(initialSession);
        ArgumentNullException.ThrowIfNull(sink);

        options ??= new SessionAutosavePipelineOptions();

        if (options.AutosaveInterval <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(options.AutosaveInterval), "Autosave interval must be positive.");

        if (options.JournalBatchSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(options.JournalBatchSize), "Batch size must be positive.");

        if (options.JournalSizeThresholdBytes <= 0)
            throw new ArgumentOutOfRangeException(nameof(options.JournalSizeThresholdBytes), "Size threshold must be positive.");

        _sink = sink;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _autosaveInterval = options.AutosaveInterval;
        _journalBatchSize = options.JournalBatchSize;
        _journalSizeThresholdBytes = options.JournalSizeThresholdBytes;
        _session = initialSession;
        _sessionDirty = true;
        _nextAutosaveAt = _timeProvider.GetUtcNow() + _autosaveInterval;
    }

    /// <summary>
    /// Gets the current snapshot tracked by the pipeline.
    /// </summary>
    public SessionDocument Snapshot
    {
        get
        {
            lock (_mutex)
            {
                ThrowIfDisposed();
                return _session;
            }
        }
    }

    /// <summary>
    /// Updates the session snapshot and marks it dirty so the next flush writes
    /// metadata to durable storage.
    /// </summary>
    public void UpdateSession(SessionDocument session)
    {
        ArgumentNullException.ThrowIfNull(session);

        lock (_mutex)
        {
            ThrowIfDisposed();
            _session = session;
            _sessionDirty = true;
        }
    }

    /// <summary>
    /// Enqueues a journal entry. If batch or size thresholds are exceeded the
    /// pipeline flushes immediately.
    /// </summary>
    public ValueTask RecordJournalEntryAsync(SessionJournalEntry entry, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        SessionDocument? snapshot = null;
        List<SessionJournalEntry>? batch = null;

        lock (_mutex)
        {
            ThrowIfDisposed();
            _journalBuffer.Add(entry);
            _journalBytes += entry.ApproximateSizeBytes;

            if (ShouldFlushDueToJournalLimits())
            {
                snapshot = _session;
                batch = DrainJournalBuffer();
                _sessionDirty = false;
                _nextAutosaveAt = _timeProvider.GetUtcNow() + _autosaveInterval;
            }
        }

        if (snapshot is null || batch is null)
        {
            return ValueTask.CompletedTask;
        }

        return new ValueTask(_sink.PersistAsync(snapshot, batch, cancellationToken));
    }

    /// <summary>
    /// Flushes pending changes when the autosave interval has elapsed.
    /// </summary>
    public Task FlushIfDueAsync(CancellationToken cancellationToken = default)
    {
        SessionDocument? snapshot = null;
        List<SessionJournalEntry>? batch = null;

        lock (_mutex)
        {
            ThrowIfDisposed();

            if (!HasPendingChanges())
            {
                return Task.CompletedTask;
            }

            var now = _timeProvider.GetUtcNow();
            if (!ShouldFlush(now))
            {
                return Task.CompletedTask;
            }

            snapshot = _session;
            batch = DrainJournalBuffer();
            _sessionDirty = false;
            _nextAutosaveAt = now + _autosaveInterval;
        }

        return _sink.PersistAsync(snapshot!, batch!, cancellationToken);
    }

    /// <summary>
    /// Forces a flush regardless of cadence or thresholds. Used for session
    /// transitions (pause, resume, complete) to guarantee durability.
    /// </summary>
    public Task FlushAsync(CancellationToken cancellationToken = default)
    {
        SessionDocument? snapshot = null;
        List<SessionJournalEntry>? batch = null;

        lock (_mutex)
        {
            ThrowIfDisposed();

            if (!HasPendingChanges())
            {
                return Task.CompletedTask;
            }

            snapshot = _session;
            batch = DrainJournalBuffer();
            _sessionDirty = false;
            _nextAutosaveAt = _timeProvider.GetUtcNow() + _autosaveInterval;
        }

        return _sink.PersistAsync(snapshot!, batch!, cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        SessionDocument? snapshot = null;
        List<SessionJournalEntry>? batch = null;

        lock (_mutex)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            if (HasPendingChanges())
            {
                snapshot = _session;
                batch = DrainJournalBuffer();
                _sessionDirty = false;
            }
        }

        if (snapshot is not null && batch is not null)
        {
            await _sink.PersistAsync(snapshot, batch, CancellationToken.None).ConfigureAwait(false);
        }
    }

    private bool ShouldFlush(DateTimeOffset now)
    {
        if (_journalBuffer.Count >= _journalBatchSize || _journalBytes >= _journalSizeThresholdBytes)
        {
            return true;
        }

        if (!_sessionDirty && _journalBuffer.Count == 0)
        {
            return false;
        }

        return now >= _nextAutosaveAt;
    }

    private bool ShouldFlushDueToJournalLimits()
    {
        return _journalBuffer.Count >= _journalBatchSize || _journalBytes >= _journalSizeThresholdBytes;
    }

    private bool HasPendingChanges()
    {
        return _sessionDirty || _journalBuffer.Count > 0;
    }

    private List<SessionJournalEntry> DrainJournalBuffer()
    {
        var batch = new List<SessionJournalEntry>(_journalBuffer);
        _journalBuffer.Clear();
        _journalBytes = 0;
        return batch;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(SessionAutosavePipeline));
        }
    }
}
