using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aog.Core.Jobs;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aog.Core.Tests.Jobs;

public sealed class JobLifecycleOrchestratorTests
{
    [Fact]
    public async Task CreateAsync_RaisesActivatedEvent()
    {
        var store = new InMemoryJobStore();
        var orchestrator = new JobLifecycleOrchestrator(store, NullLogger<JobLifecycleOrchestrator>.Instance);

        var eventArgsTask = SubscribeAsync(orchestrator);
        var handle = await orchestrator.CreateAsync("Alpha", null, CancellationToken.None);
        var eventArgs = await eventArgsTask;

        Assert.Equal(JobLifecycleChangeType.Activated, eventArgs.ChangeType);
        Assert.Equal(handle.Id, eventArgs.Job.Id);
    }

    [Fact]
    public async Task CloseActiveAsync_RaisesDeactivatedEvent()
    {
        var store = new InMemoryJobStore();
        var orchestrator = new JobLifecycleOrchestrator(store, NullLogger<JobLifecycleOrchestrator>.Instance);

        await orchestrator.CreateAsync("Beta", null, CancellationToken.None);
        var eventArgsTask = SubscribeAsync(orchestrator);
        await orchestrator.CloseActiveAsync(CancellationToken.None);
        var eventArgs = await eventArgsTask;

        Assert.Equal(JobLifecycleChangeType.Deactivated, eventArgs.ChangeType);
    }

    [Fact]
    public async Task CloseActiveAsync_ReturnsNullWhenNoActiveJob()
    {
        var store = new InMemoryJobStore();
        var orchestrator = new JobLifecycleOrchestrator(store, NullLogger<JobLifecycleOrchestrator>.Instance);

        var result = await orchestrator.CloseActiveAsync(CancellationToken.None);
        Assert.Null(result);
    }

    private static Task<JobLifecycleEventArgs> SubscribeAsync(JobLifecycleOrchestrator orchestrator)
    {
        var tcs = new TaskCompletionSource<JobLifecycleEventArgs>(TaskCreationOptions.RunContinuationsAsynchronously);
        void Handler(object? sender, JobLifecycleEventArgs args)
        {
            orchestrator.JobStateChanged -= Handler;
            tcs.TrySetResult(args);
        }

        orchestrator.JobStateChanged += Handler;
        return tcs.Task.WaitAsync(TimeSpan.FromSeconds(2));
    }

    private sealed class InMemoryJobStore : IJobStore
    {
        private readonly List<JobHandle> _jobs = new();
        private JobHandle? _active;

        public Task<JobHandle> CreateAsync(JobCreationRequest request, CancellationToken cancellationToken)
        {
            var handle = new JobHandle(
                $"job:{Guid.NewGuid():n}",
                request.DisplayName,
                request.DisplayName.ToLowerInvariant(),
                JobLifecycleState.Active,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow,
                new List<JobSessionMetadata>
                {
                    new("session:1", request.InitialSessionName ?? "Session 1", JobSessionState.Active, DateTimeOffset.UtcNow, null)
                });

            _jobs.Add(handle);
            _active = handle;
            return Task.FromResult(handle);
        }

        public Task<JobHandle?> CloseActiveAsync(CancellationToken cancellationToken)
        {
            if (_active is null)
            {
                return Task.FromResult<JobHandle?>(null);
            }

            var closed = _active with { State = JobLifecycleState.Inactive };
            _jobs[_jobs.FindIndex(job => job.Id == closed.Id)] = closed;
            _active = null;
            return Task.FromResult<JobHandle?>(closed);
        }

        public Task<JobHandle?> GetActiveAsync(CancellationToken cancellationToken) => Task.FromResult(_active);

        public Task EnsureInitializedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<IReadOnlyList<JobHandle>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<JobHandle>>(_jobs);

        public Task<JobHandle> ResumeAsync(string jobId, CancellationToken cancellationToken)
        {
            var handle = _jobs.Find(job => job.Id == jobId) ??
                throw new InvalidOperationException($"Job '{jobId}' not found.");

            var resumed = handle with { State = JobLifecycleState.Active };
            _jobs[_jobs.FindIndex(job => job.Id == jobId)] = resumed;
            _active = resumed;
            return Task.FromResult(resumed);
        }
    }
}
