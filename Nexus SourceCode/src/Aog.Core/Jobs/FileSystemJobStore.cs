using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Aog.Core.Jobs;

/// <summary>
/// Simplified file-backed job store that coordinates job and session metadata updates.
/// This implementation focuses on the lifecycle semantics required by the current
/// orchestrator workstreams. Persistence plumbing will arrive in follow-up tasks.
/// </summary>
public sealed class FileSystemJobStore
{
    private readonly TimeProvider _timeProvider;
    private readonly SemaphoreSlim _mutex = new(1, 1);
    private readonly Dictionary<string, JobDocument> _jobs = new(StringComparer.OrdinalIgnoreCase);
    private string? _activeJobId;

    public FileSystemJobStore(TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Seeds the store with job metadata. This helper exists so tests can inject
    /// specific scenarios without depending on persistence.
    /// </summary>
    /// <param name="job">Job document to add or replace.</param>
    /// <param name="isActive">Whether the job should be marked as the active job.</param>
    public void Seed(JobDocument job, bool isActive = false)
    {
        ArgumentNullException.ThrowIfNull(job);

        _jobs[job.Id] = job;
        if (isActive || job.State == JobLifecycleState.Active)
        {
            _activeJobId = job.Id;
        }
    }

    /// <summary>
    /// Attempts to retrieve the stored document for a job.
    /// </summary>
    public bool TryGetJob(string jobId, out JobDocument? job)
    {
        if (string.IsNullOrWhiteSpace(jobId))
        {
            job = null;
            return false;
        }

        return _jobs.TryGetValue(jobId, out job);
    }

    /// <summary>
    /// Marks the specified job as active and ensures any previously active job is
    /// transitioned back to a non-active state.
    /// </summary>
    public async Task<JobHandle> ResumeAsync(string jobId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(jobId))
        {
            throw new ArgumentException("A job identifier is required.", nameof(jobId));
        }

        await _mutex.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!_jobs.TryGetValue(jobId, out var job))
            {
                throw new InvalidOperationException($"Job '{jobId}' was not found in the store.");
            }

            var now = _timeProvider.GetUtcNow();

            DeactivatePreviousActiveJob(job.Id, now);

            job.State = JobLifecycleState.Active;
            job.UpdatedAt = now;

            var activeSession = job.Sessions.LastOrDefault(static session =>
                session.State is JobSessionState.Active or JobSessionState.Paused);

            if (activeSession is null)
            {
                activeSession = new JobSessionDocument(
                    $"session:{Guid.NewGuid():n}",
                    $"Session {job.Sessions.Count + 1}",
                    JobSessionState.Active,
                    now,
                    endedAt: null);
                job.Sessions.Add(activeSession);
            }
            else
            {
                activeSession.State = JobSessionState.Active;
                activeSession.EndedAt = null;
            }

            _activeJobId = job.Id;

            return new JobHandle(job.Id, activeSession.Id, now);
        }
        finally
        {
            _mutex.Release();
        }
    }

    private void DeactivatePreviousActiveJob(string resumedJobId, DateTimeOffset timestamp)
    {
        if (_activeJobId is null || string.Equals(_activeJobId, resumedJobId, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (!_jobs.TryGetValue(_activeJobId, out var previous))
        {
            _activeJobId = null;
            return;
        }

        previous.State = JobLifecycleState.Paused;
        previous.UpdatedAt = timestamp;

        var session = previous.Sessions.LastOrDefault(static s => s.State == JobSessionState.Active);
        if (session is not null)
        {
            session.State = JobSessionState.Paused;
            session.EndedAt = timestamp;
        }
    }
}

/// <summary>
/// Lightweight handle returned when a job becomes active.
/// </summary>
/// <param name="JobId">Identifier of the active job.</param>
/// <param name="SessionId">Identifier of the active session.</param>
/// <param name="ResumedAt">Timestamp when the job was resumed.</param>
public sealed record JobHandle(string JobId, string SessionId, DateTimeOffset ResumedAt);

/// <summary>
/// Simplified job document that mirrors the schema persisted by the real job store.
/// </summary>
public sealed class JobDocument
{
    public JobDocument(
        string id,
        string name,
        JobLifecycleState state,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt,
        IEnumerable<JobSessionDocument>? sessions = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("A job identifier is required.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("A job name is required.", nameof(name));
        }

        Id = id;
        Name = name;
        State = state;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
        Sessions = sessions?.Select(CloneSession).ToList() ?? new List<JobSessionDocument>();
    }

    public string Id { get; }

    public string Name { get; }

    public JobLifecycleState State { get; set; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset UpdatedAt { get; set; }

    public List<JobSessionDocument> Sessions { get; }

    private static JobSessionDocument CloneSession(JobSessionDocument session)
    {
        return new JobSessionDocument(
            session.Id,
            session.Name,
            session.State,
            session.StartedAt,
            session.EndedAt);
    }
}

/// <summary>
/// Session document tracked within a job document.
/// </summary>
public sealed class JobSessionDocument
{
    public JobSessionDocument(
        string id,
        string name,
        JobSessionState state,
        DateTimeOffset startedAt,
        DateTimeOffset? endedAt)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("A session identifier is required.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("A session name is required.", nameof(name));
        }

        Id = id;
        Name = name;
        State = state;
        StartedAt = startedAt;
        EndedAt = endedAt;
    }

    public string Id { get; }

    public string Name { get; }

    public JobSessionState State { get; set; }

    public DateTimeOffset StartedAt { get; }

    public DateTimeOffset? EndedAt { get; set; }
}

/// <summary>
/// Lifecycle states tracked for job sessions within the store.
/// </summary>
public enum JobSessionState
{
    Active = 0,
    Paused = 1,
    Completed = 2,
}
