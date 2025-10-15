using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aog.Core.Jobs;

/// <summary>
/// Hosted service that ensures the job store is initialised and logs lifecycle events.
/// </summary>
public sealed class JobsHostedService : IHostedService, IDisposable
{
    private readonly IJobStore _store;
    private readonly IJobLifecycleOrchestrator _orchestrator;
    private readonly ILogger<JobsHostedService> _logger;

    public JobsHostedService(IJobStore store, IJobLifecycleOrchestrator orchestrator, ILogger<JobsHostedService> logger)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _orchestrator.JobStateChanged += HandleJobStateChanged;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _store.EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);
        var active = await _orchestrator.GetActiveAsync(cancellationToken).ConfigureAwait(false);

        if (active is null)
        {
            _logger.LogInformation("JobsService initialised with no active job.");
        }
        else
        {
            _logger.LogInformation("JobsService restored active job {JobId} ({DisplayName}).", active.Id, active.DisplayName);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private void HandleJobStateChanged(object? sender, JobLifecycleEventArgs e)
    {
        switch (e.ChangeType)
        {
            case JobLifecycleChangeType.Activated:
                _logger.LogInformation("Job {JobId} activated ({DisplayName}).", e.Job.Id, e.Job.DisplayName);
                break;
            case JobLifecycleChangeType.Deactivated:
                _logger.LogInformation("Job {JobId} deactivated ({DisplayName}).", e.Job.Id, e.Job.DisplayName);
                break;
            default:
                _logger.LogInformation("Job {JobId} updated ({DisplayName}).", e.Job.Id, e.Job.DisplayName);
                break;
        }
    }

    public void Dispose()
    {
        _orchestrator.JobStateChanged -= HandleJobStateChanged;
    }
}
