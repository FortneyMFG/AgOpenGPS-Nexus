using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aog.Core.Jobs;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace Aog.Core.Tests.Jobs;

public sealed class FileSystemJobStoreTests : IDisposable
{
    private readonly string _rootDirectory = Path.Combine(Path.GetTempPath(), $"aog-jobs-{Guid.NewGuid():n}");
    private readonly FakeTimeProvider _timeProvider = new(DateTimeOffset.Parse("2025-01-05T12:00:00Z"));

    [Fact]
    public async Task CreateJob_PersistsMetadataAndActivePointer()
    {
        using var store = CreateStore();
        await store.EnsureInitializedAsync(CancellationToken.None);

        var job = await store.CreateAsync(new JobCreationRequest("Alpha Field"), CancellationToken.None);

        Assert.Equal(JobLifecycleState.Active, job.State);
        Assert.Single(job.Sessions);
        Assert.Equal("Alpha Field", job.DisplayName);

        var jobDirectory = Path.Combine(_rootDirectory, job.DirectoryName);
        Assert.True(File.Exists(Path.Combine(jobDirectory, "job.json")));
        Assert.True(File.Exists(Path.Combine(_rootDirectory, "active.json")));

        var active = await store.GetActiveAsync(CancellationToken.None);
        Assert.NotNull(active);
        Assert.Equal(job.Id, active!.Id);
        Assert.Equal(job.ActiveSession?.Id, active.ActiveSession?.Id);
    }

    [Fact]
    public async Task CloseActiveJob_CompletesSession_AndClearsPointer()
    {
        using var store = CreateStore();
        await store.EnsureInitializedAsync(CancellationToken.None);
        var job = await store.CreateAsync(new JobCreationRequest("Beta Field"), CancellationToken.None);

        _timeProvider.Advance(TimeSpan.FromHours(2));
        var closed = await store.CloseActiveAsync(CancellationToken.None);

        Assert.NotNull(closed);
        Assert.Equal(JobLifecycleState.Inactive, closed!.State);
        var session = Assert.Single(closed.Sessions);
        Assert.Equal(JobSessionState.Completed, session.State);
        Assert.Equal(_timeProvider.GetUtcNow(), session.EndedAt);

        var active = await store.GetActiveAsync(CancellationToken.None);
        Assert.Null(active);
        Assert.False(File.Exists(Path.Combine(_rootDirectory, "active.json")));
    }

    [Fact]
    public async Task ResumeJob_WithCompletedSession_StartsNewSession()
    {
        using var store = CreateStore();
        await store.EnsureInitializedAsync(CancellationToken.None);
        var job = await store.CreateAsync(new JobCreationRequest("Gamma Field"), CancellationToken.None);
        await store.CloseActiveAsync(CancellationToken.None);

        _timeProvider.Advance(TimeSpan.FromMinutes(15));
        var resumed = await store.ResumeAsync(job.Id, CancellationToken.None);

        Assert.Equal(JobLifecycleState.Active, resumed.State);
        Assert.Equal(2, resumed.Sessions.Count);
        var latest = resumed.Sessions.Last();
        Assert.Equal(JobSessionState.Active, latest.State);
        Assert.Equal(_timeProvider.GetUtcNow(), latest.StartedAt);
        Assert.Null(latest.EndedAt);
    }

    [Fact]
    public async Task ListAsync_ReturnsJobsOrderedByUpdatedAt()
    {
        using var store = CreateStore();
        await store.EnsureInitializedAsync(CancellationToken.None);
        await store.CreateAsync(new JobCreationRequest("Delta Field"), CancellationToken.None);
        _timeProvider.Advance(TimeSpan.FromMinutes(1));
        await store.CloseActiveAsync(CancellationToken.None);
        _timeProvider.Advance(TimeSpan.FromMinutes(1));
        await store.CreateAsync(new JobCreationRequest("Epsilon Field"), CancellationToken.None);

        var jobs = await store.ListAsync(CancellationToken.None);

        Assert.Equal(2, jobs.Count);
        Assert.Equal("Epsilon Field", jobs[0].DisplayName);
        Assert.Equal("Delta Field", jobs[1].DisplayName);
    }

    private FileSystemJobStore CreateStore()
    {
        var options = Options.Create(new JobsServiceOptions
        {
            RootDirectory = _rootDirectory,
            ActiveStateFileName = "active.json",
            DefaultSessionName = "Session 1"
        });

        return new FileSystemJobStore(options, _timeProvider, NullLogger<FileSystemJobStore>.Instance);
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootDirectory))
        {
            try
            {
                Directory.Delete(_rootDirectory, recursive: true);
            }
            catch
            {
                // Ignore cleanup failures in test environment.
            }
        }
    }
}
