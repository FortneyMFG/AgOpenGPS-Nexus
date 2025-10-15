using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aog.Core.Jobs;

namespace Aog.Plugins.JobTasks;

/// <summary>
/// Coordinates job seasons and sessions for the Job Tasks plugin. The orchestrator keeps
/// an in-memory view that mirrors ADR-030/ADR-041 lifecycle semantics so plugins and
/// hosted services can react deterministically during early development iterations.
/// </summary>
public interface IJobSeasonSessionOrchestrator
{
    /// <summary>
    /// Registers or updates a job so the orchestrator can track its season association
    /// and session roster.
    /// </summary>
    Task TrackJobAsync(JobMetadata job, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes job state from the orchestrator.
    /// </summary>
    Task RemoveJobAsync(string jobId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts a new session for the specified job.
    /// </summary>
    Task<JobSession> StartSessionAsync(
        string jobId,
        JobSessionStartRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts the next session for a job using the supplied metadata.
    /// </summary>
    Task<JobSession> StartNextSessionAsync(
        string jobId,
        JobSessionStartRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the currently active session for a job, if one exists.
    /// </summary>
    Task<JobSession?> GetActiveSessionAsync(string jobId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all sessions recorded for the job.
    /// </summary>
    Task<IReadOnlyList<JobSession>> ListSessionsAsync(string jobId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pauses the specified session.
    /// </summary>
    Task<JobSession> PauseSessionAsync(
        string jobId,
        string sessionId,
        string? reason = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resumes a paused session.
    /// </summary>
    Task<JobSession> ResumeSessionAsync(
        string jobId,
        string sessionId,
        string? reason = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a session as completed.
    /// </summary>
    Task<JobSession> CompleteSessionAsync(
        string jobId,
        string sessionId,
        string? reason = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates editable session metadata (name, operators, notes, work order).
    /// </summary>
    Task<JobSession> UpdateSessionMetadataAsync(
        string jobId,
        string sessionId,
        JobSessionMetadata metadata,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists jobs currently associated with a season.
    /// </summary>
    Task<IReadOnlyList<string>> ListJobsForSeasonAsync(string seasonId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Subscribes to lifecycle events produced by the orchestrator.
    /// </summary>
    IAsyncEnumerable<JobSessionEvent> WatchAsync(CancellationToken cancellationToken = default);
}
