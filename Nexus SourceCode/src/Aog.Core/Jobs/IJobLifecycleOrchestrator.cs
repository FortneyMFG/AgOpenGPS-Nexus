using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Aog.Core.Jobs;

/// <summary>
/// Coordinates job lifecycle transitions for the Core host. The orchestrator maintains an
/// authoritative in-memory roster of jobs while storage and journaling layers evolve in
/// follow-on tasks.
/// </summary>
public interface IJobLifecycleOrchestrator
{
    /// <summary>
    /// Creates a new job and optionally mounts it as the active job.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when the request is invalid.</exception>
    /// <exception cref="InvalidOperationException">Thrown when another job is already active and the request asks to mount immediately.</exception>
    Task<JobMetadata> CreateJobAsync(JobCreationRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Mounts an existing job as the active job.
    /// </summary>
    Task<JobMetadata> MountJobAsync(string jobId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the currently active job, if any.
    /// </summary>
    Task<JobMetadata?> GetActiveJobAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a snapshot of all known jobs in creation order.
    /// </summary>
    Task<IReadOnlyList<JobMetadata>> ListJobsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Closes the currently active job.
    /// </summary>
    Task<JobMetadata> CloseActiveJobAsync(string? reason = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Streams lifecycle events for subscribers interested in job transitions.
    /// </summary>
    IAsyncEnumerable<JobLifecycleEvent> WatchAsync(CancellationToken cancellationToken = default);
}
