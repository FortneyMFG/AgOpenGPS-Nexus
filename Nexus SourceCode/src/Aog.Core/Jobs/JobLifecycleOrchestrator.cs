using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Aog.Core.Jobs;

/// <summary>
/// Coordinates job lifecycle transitions and emits events for observers.
/// </summary>
public interface IJobLifecycleOrchestrator
{
    /// <summary>
    /// Raised whenever a job changes lifecycle state.
    /// </summary>
    event EventHandler<JobLifecycleEventArgs>? JobStateChanged;

    Task<JobHandle> CreateAsync(string displayName, string? initialSessionName, CancellationToken cancellationToken);

    Task<JobHandle> ResumeAsync(string jobId, CancellationToken cancellationToken);

    Task<JobHandle?> CloseActiveAsync(CancellationToken cancellationToken);

    Task<JobHandle?> GetActiveAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<JobHandle>> ListAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Default implementation that uses an <see cref="IJobStore"/> for persistence.
/// </summary>
public sealed class JobLifecycleOrchestrator : IJobLifecycleOrchestrator
{
    private readonly IJobStore _store;
    private readonly ILogger<JobLifecycleOrchestrator> _logger;

    public JobLifecycleOrchestrator(IJobStore store, ILogger<JobLifecycleOrchestrator> logger)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public event EventHandler<JobLifecycleEventArgs>? JobStateChanged;

    public async Task<JobHandle> CreateAsync(string displayName, string? initialSessionName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("A display name is required when creating a job.", nameof(displayName));
        }

        var request = new JobCreationRequest(displayName.Trim(), initialSessionName?.Trim());
        var handle = await _store.CreateAsync(request, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Created job {JobId} ({DisplayName}).", handle.Id, handle.DisplayName);
        OnJobStateChanged(JobLifecycleChangeType.Activated, handle);
        return handle;
    }

    public async Task<JobHandle> ResumeAsync(string jobId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(jobId))
        {
            throw new ArgumentException("A job identifier is required when resuming a job.", nameof(jobId));
        }

        var handle = await _store.ResumeAsync(jobId.Trim(), cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Resumed job {JobId} ({DisplayName}).", handle.Id, handle.DisplayName);
        OnJobStateChanged(JobLifecycleChangeType.Activated, handle);
        return handle;
    }

    public async Task<JobHandle?> CloseActiveAsync(CancellationToken cancellationToken)
    {
        var handle = await _store.CloseActiveAsync(cancellationToken).ConfigureAwait(false);
        if (handle is null)
        {
            return null;
        }

        _logger.LogInformation("Closed job {JobId} ({DisplayName}).", handle.Id, handle.DisplayName);
        OnJobStateChanged(JobLifecycleChangeType.Deactivated, handle);
        return handle;
    }

    public Task<JobHandle?> GetActiveAsync(CancellationToken cancellationToken) =>
        _store.GetActiveAsync(cancellationToken);

    public Task<IReadOnlyList<JobHandle>> ListAsync(CancellationToken cancellationToken) =>
        _store.ListAsync(cancellationToken);

    private void OnJobStateChanged(JobLifecycleChangeType changeType, JobHandle handle)
    {
        JobStateChanged?.Invoke(this, new JobLifecycleEventArgs(handle, changeType));
    }
}
