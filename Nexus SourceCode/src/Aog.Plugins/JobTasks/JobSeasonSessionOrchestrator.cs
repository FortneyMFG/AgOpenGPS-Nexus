using System.Threading.Channels;

namespace Aog.Plugins.JobTasks;

/// <summary>
/// In-memory season/session orchestrator used by the Job Tasks plugin until the gRPC
/// surface lands. It groups jobs by season, enforces a single active session per job,
/// and emits lifecycle events for downstream consumers (telemetry logging, UI shells,
/// etc.).
/// </summary>
public sealed class JobSeasonSessionOrchestrator : IJobSeasonSessionOrchestrator, IDisposable
{
    private readonly TimeProvider _timeProvider;
    private readonly SemaphoreSlim _mutex = new(1, 1);
    private readonly Dictionary<string, JobState> _jobs = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, HashSet<string>> _seasonIndex = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<System.Threading.Channels.Channel<JobSessionEvent>> _watchers = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="JobSeasonSessionOrchestrator"/> class.
    /// </summary>
    /// <param name="timeProvider">Time provider used for deterministic testing.</param>
    public JobSeasonSessionOrchestrator(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    /// <inheritdoc />
    public async Task TrackJobAsync(Aog.Core.Jobs.JobMetadata job, CancellationToken cancellationToken = default)
    {
        if (job is null)
        {
            throw new ArgumentNullException(nameof(job));
        }

        await _mutex.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!_jobs.TryGetValue(job.JobId, out var state))
            {
                state = new JobState(job.JobId);
                _jobs[job.JobId] = state;
            }

            UpdateSeasonAssociation(state, job.Context.SeasonId);
            state.DisplayName = job.DisplayName;
        }
        finally
        {
            _mutex.Release();
        }
    }

    /// <inheritdoc />
    public async Task RemoveJobAsync(string jobId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(jobId))
        {
            throw new ArgumentException("Job identifier is required.", nameof(jobId));
        }

        await _mutex.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_jobs.Remove(jobId, out var state))
            {
                RemoveFromSeasonIndex(state.SeasonId, jobId);
            }
        }
        finally
        {
            _mutex.Release();
        }
    }

    /// <inheritdoc />
    public Task<JobSession> StartSessionAsync(
        string jobId,
        JobSessionStartRequest request,
        CancellationToken cancellationToken = default)
        => StartSessionInternalAsync(jobId, request, cancellationToken);

    /// <inheritdoc />
    public Task<JobSession> StartNextSessionAsync(
        string jobId,
        JobSessionStartRequest request,
        CancellationToken cancellationToken = default)
        => StartSessionInternalAsync(jobId, request, cancellationToken);

    /// <inheritdoc />
    public async Task<JobSession?> GetActiveSessionAsync(string jobId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(jobId))
        {
            throw new ArgumentException("Job identifier is required.", nameof(jobId));
        }

        await _mutex.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var state = GetJobState(jobId);
            if (state.ActiveSessionId is null)
            {
                return null;
            }

            var session = state.GetSession(state.ActiveSessionId);
            return CloneSession(session);
        }
        finally
        {
            _mutex.Release();
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<JobSession>> ListSessionsAsync(string jobId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(jobId))
        {
            throw new ArgumentException("Job identifier is required.", nameof(jobId));
        }

        await _mutex.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var state = GetJobState(jobId);
            return state.Sessions.Select(CloneSession).ToArray();
        }
        finally
        {
            _mutex.Release();
        }
    }

    /// <inheritdoc />
    public async Task<JobSession> PauseSessionAsync(
        string jobId,
        string sessionId,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        var events = new List<JobSessionEvent>(capacity: 1);

        await _mutex.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var state = GetJobState(jobId);
            var (session, index) = state.GetSessionWithIndex(sessionId);
            if (session.State != JobSessionState.Active)
            {
                throw new InvalidOperationException($"Session '{sessionId}' is not active and cannot be paused.");
            }

            var now = _timeProvider.GetUtcNow();
            var updated = session with
            {
                State = JobSessionState.Paused,
                LastModifiedAt = now,
            };

            state.Sessions[index] = updated;
            state.ActiveSessionId = updated.SessionId;
            events.Add(CreateEvent(JobSessionEventType.Paused, state, updated, reason));
            return CloneSession(updated);
        }
        finally
        {
            _mutex.Release();
            Broadcast(events);
        }
    }

    /// <inheritdoc />
    public async Task<JobSession> ResumeSessionAsync(
        string jobId,
        string sessionId,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        var events = new List<JobSessionEvent>(capacity: 1);

        await _mutex.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var state = GetJobState(jobId);
            var (session, index) = state.GetSessionWithIndex(sessionId);
            if (session.State != JobSessionState.Paused)
            {
                throw new InvalidOperationException($"Session '{sessionId}' is not paused and cannot be resumed.");
            }

            var now = _timeProvider.GetUtcNow();
            var updated = session with
            {
                State = JobSessionState.Active,
                LastModifiedAt = now,
            };

            state.Sessions[index] = updated;
            state.ActiveSessionId = updated.SessionId;
            events.Add(CreateEvent(JobSessionEventType.Resumed, state, updated, reason));
            return CloneSession(updated);
        }
        finally
        {
            _mutex.Release();
            Broadcast(events);
        }
    }

    /// <inheritdoc />
    public async Task<JobSession> CompleteSessionAsync(
        string jobId,
        string sessionId,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        var events = new List<JobSessionEvent>(capacity: 1);

        await _mutex.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var state = GetJobState(jobId);
            var (session, index) = state.GetSessionWithIndex(sessionId);
            if (session.State == JobSessionState.Completed)
            {
                return CloneSession(session);
            }

            var now = _timeProvider.GetUtcNow();
            var updated = session with
            {
                State = JobSessionState.Completed,
                LastModifiedAt = now,
                EndedAt = now,
            };

            state.Sessions[index] = updated;
            state.ActiveSessionId = null;
            events.Add(CreateEvent(JobSessionEventType.Completed, state, updated, reason));
            return CloneSession(updated);
        }
        finally
        {
            _mutex.Release();
            Broadcast(events);
        }
    }

    /// <inheritdoc />
    public async Task<JobSession> UpdateSessionMetadataAsync(
        string jobId,
        string sessionId,
        JobSessionMetadata metadata,
        CancellationToken cancellationToken = default)
    {
        if (metadata is null)
        {
            throw new ArgumentNullException(nameof(metadata));
        }

        var sanitized = SanitizeMetadata(metadata);
        var events = new List<JobSessionEvent>(capacity: 1);

        await _mutex.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var state = GetJobState(jobId);
            var (session, index) = state.GetSessionWithIndex(sessionId);
            var now = _timeProvider.GetUtcNow();

            var updated = session with
            {
                Name = sanitized.Name,
                WorkOrderId = sanitized.WorkOrderId,
                ActiveOperators = sanitized.ActiveOperators,
                Notes = sanitized.Notes,
                LastModifiedAt = now,
            };

            state.Sessions[index] = updated;
            events.Add(CreateEvent(JobSessionEventType.MetadataChanged, state, updated, reason: null));
            return CloneSession(updated);
        }
        finally
        {
            _mutex.Release();
            Broadcast(events);
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> ListJobsForSeasonAsync(
        string seasonId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(seasonId))
        {
            throw new ArgumentException("Season identifier is required.", nameof(seasonId));
        }

        await _mutex.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!_seasonIndex.TryGetValue(seasonId.Trim(), out var jobs) || jobs.Count == 0)
            {
                return Array.Empty<string>();
            }

            return jobs.OrderBy(j => j, StringComparer.OrdinalIgnoreCase).ToArray();
        }
        finally
        {
            _mutex.Release();
        }
    }

    /// <inheritdoc />
    public IAsyncEnumerable<JobSessionEvent> WatchAsync(CancellationToken cancellationToken = default)
    {
        var channel = Channel.CreateUnbounded<JobSessionEvent>(new UnboundedChannelOptions
        {
            AllowSynchronousContinuations = false,
            SingleReader = false,
            SingleWriter = false,
        });

        lock (_watchers)
        {
            _watchers.Add(channel);
        }

        return ReadEventsAsync(channel, cancellationToken);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _mutex.Dispose();

        lock (_watchers)
        {
            foreach (var watcher in _watchers)
            {
                watcher.Writer.TryComplete();
            }

            _watchers.Clear();
        }
    }

    private async Task<JobSession> StartSessionInternalAsync(
        string jobId,
        JobSessionStartRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(jobId))
        {
            throw new ArgumentException("Job identifier is required.", nameof(jobId));
        }

        request ??= new JobSessionStartRequest();
        var sanitized = SanitizeStartRequest(request);
        var events = new List<JobSessionEvent>(capacity: 1);

        await _mutex.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var state = GetJobState(jobId);
            if (state.ActiveSessionId is string activeSessionId)
            {
                var active = state.GetSession(activeSessionId);
                if (active.State is JobSessionState.Active or JobSessionState.Paused)
                {
                    throw new InvalidOperationException($"Job '{jobId}' already has an active session.");
                }
            }

            var now = _timeProvider.GetUtcNow();
            var sequence = state.NextSequence++;
            var sessionId = $"session:{sequence}";
            var name = sanitized.Name ?? $"Session {sequence}";

            var session = new JobSession(
                sessionId,
                name,
                JobSessionState.Active,
                now,
                now,

                sanitized.WorkOrderId,
                sanitized.ActiveOperators,
                sanitized.Notes);

            state.Sessions.Add(session);
            state.ActiveSessionId = session.SessionId;
            events.Add(CreateEvent(JobSessionEventType.Started, state, session, reason: null));
            return CloneSession(session);
        }
        finally
        {
            _mutex.Release();
            Broadcast(events);
        }
    }

    private void UpdateSeasonAssociation(JobState state, string? seasonId)
    {
        var normalized = NormalizeSeasonId(seasonId);
        if (string.Equals(state.SeasonId, normalized, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (state.SeasonId is not null)
        {
            RemoveFromSeasonIndex(state.SeasonId, state.JobId);
        }

        if (normalized is not null)
        {
            if (!_seasonIndex.TryGetValue(normalized, out var jobs))
            {
                jobs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                _seasonIndex[normalized] = jobs;
            }

            jobs.Add(state.JobId);
        }

        state.SeasonId = normalized;
    }

    private void RemoveFromSeasonIndex(string? seasonId, string jobId)
    {
        if (seasonId is null)
        {
            return;
        }

        if (_seasonIndex.TryGetValue(seasonId, out var jobs))
        {
            jobs.Remove(jobId);
            if (jobs.Count == 0)
            {
                _seasonIndex.Remove(seasonId);
            }
        }
    }

    private JobState GetJobState(string jobId)
    {
        if (!_jobs.TryGetValue(jobId, out var state))
        {
            throw new KeyNotFoundException($"Job '{jobId}' is not tracked by the orchestrator.");
        }

        return state;
    }

    private static JobSessionEvent CreateEvent(JobSessionEventType type, JobState state, JobSession session, string? reason)
    {
        var snapshot = CloneSession(session);
        return new JobSessionEvent(type, state.JobId, state.SeasonId, snapshot, reason);
    }

    private static JobSession CloneSession(JobSession session)
    {
        var operators = session.ActiveOperators.Count == 0
            ? Array.Empty<string>()
            : session.ActiveOperators.ToArray();

        return session with { ActiveOperators = operators };
    }

    private static JobSessionStartRequest SanitizeStartRequest(JobSessionStartRequest request)
    {
        var operators = SanitizeOperators(request.ActiveOperators);
        var workOrderId = SanitizeOptionalString(request.WorkOrderId);
        var notes = SanitizeOptionalString(request.Notes);
        var name = string.IsNullOrWhiteSpace(request.Name) ? null : request.Name.Trim();

        return request with
        {
            ActiveOperators = operators,
            WorkOrderId = workOrderId,
            Notes = notes,
            Name = name,
        };
    }

    private static JobSessionMetadata SanitizeMetadata(JobSessionMetadata metadata)
    {
        var operators = SanitizeOperators(metadata.ActiveOperators);
        var workOrderId = SanitizeOptionalString(metadata.WorkOrderId);
        var notes = SanitizeOptionalString(metadata.Notes);
        var name = string.IsNullOrWhiteSpace(metadata.Name)
            ? throw new ArgumentException("Session name must be provided.", nameof(metadata))
            : metadata.Name.Trim();

        return metadata with
        {
            ActiveOperators = operators,
            WorkOrderId = workOrderId,
            Notes = notes,
            Name = name,
        };
    }

    private static IReadOnlyList<string> SanitizeOperators(IReadOnlyList<string>? operators)
    {
        if (operators is null)
        {
            return Array.Empty<string>();
        }

        return operators
            .Select(o => o?.Trim())
            .Where(o => !string.IsNullOrEmpty(o))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(o => o, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string? SanitizeOptionalString(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length == 0 ? null : trimmed;
    }

    private static string? NormalizeSeasonId(string? seasonId)
    {
        return string.IsNullOrWhiteSpace(seasonId) ? null : seasonId.Trim();
    }

    private async IAsyncEnumerable<JobSessionEvent> ReadEventsAsync(
        System.Threading.Channels.Channel<JobSessionEvent> channel,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using var registration = cancellationToken.Register(() => channel.Writer.TryComplete());

        try
        {
            await foreach (var evt in channel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
            {
                yield return evt;
            }
        }
        finally
        {
            lock (_watchers)
            {
                _watchers.Remove(channel);
            }

            channel.Writer.TryComplete();
        }
    }

    private void Broadcast(IEnumerable<JobSessionEvent> events)
    {
        if (events is null)
        {
            return;
        }

        if (events is not IList<JobSessionEvent> list)
        {
            list = events.ToList();
        }

        if (list.Count == 0)
        {
            return;
        }

        System.Threading.Channels.Channel<JobSessionEvent>[] watchers;
        lock (_watchers)
        {
            watchers = _watchers.ToArray();
        }

        foreach (var watcher in watchers)
        {
            var writer = watcher.Writer;
            var failed = false;

            foreach (var evt in list)
            {
                if (!writer.TryWrite(evt))
                {
                    failed = true;
                    break;
                }
            }

            if (failed)
            {
                lock (_watchers)
                {
                    _watchers.Remove(watcher);
                }
            }
        }
    }

    private sealed class JobState
    {
        public JobState(string jobId)
        {
            JobId = jobId;
        }

        public string JobId { get; }

        public string? SeasonId { get; set; }

        public string? DisplayName { get; set; }

        public int NextSequence { get; set; } = 1;

        public string? ActiveSessionId { get; set; }

        public List<JobSession> Sessions { get; } = new();

        public JobSession GetSession(string sessionId)
        {
            foreach (var session in Sessions)
            {
                if (string.Equals(session.SessionId, sessionId, StringComparison.OrdinalIgnoreCase))
                {
                    return session;
                }
            }

            throw new KeyNotFoundException($"Session '{sessionId}' is not tracked for job '{JobId}'.");
        }

        public (JobSession Session, int Index) GetSessionWithIndex(string sessionId)
        {
            for (var i = 0; i < Sessions.Count; i++)
            {
                if (string.Equals(Sessions[i].SessionId, sessionId, StringComparison.OrdinalIgnoreCase))
                {
                    return (Sessions[i], i);
                }
            }

            throw new KeyNotFoundException($"Session '{sessionId}' is not tracked for job '{JobId}'.");
        }
    }
}
