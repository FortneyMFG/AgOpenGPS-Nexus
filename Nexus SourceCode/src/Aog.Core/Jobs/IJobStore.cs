using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Aog.Core.Jobs;

/// <summary>
/// Abstraction for persisting and retrieving job metadata.
/// </summary>
public interface IJobStore
{
    /// <summary>
    /// Ensures the underlying storage is ready for use.
    /// </summary>
    Task EnsureInitializedAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Lists known jobs from the store.
    /// </summary>
    Task<IReadOnlyList<JobHandle>> ListAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves the active job when one is tracked.
    /// </summary>
    Task<JobHandle?> GetActiveAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Creates a new job entry and marks it active.
    /// </summary>
    Task<JobHandle> CreateAsync(JobCreationRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Resumes work on an existing job and marks it active.
    /// </summary>
    Task<JobHandle> ResumeAsync(string jobId, CancellationToken cancellationToken);

    /// <summary>
    /// Closes the currently active job when one exists.
    /// </summary>
    Task<JobHandle?> CloseActiveAsync(CancellationToken cancellationToken);
}
