using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Aog.Core.Jobs.Tests;

public sealed class JobLifecycleOrchestratorTests
{
    private static JobCreationRequest CreateRequest(
        string displayName = "North 40 Planting",
        bool mountImmediately = true) =>
        new(
            displayName,
            new JobContext(
                "farm:alpha",
                new[] { "field:north-40" },
                "season:2025",
                "work:order-1",
                "  Morning pass  "),
            new[] { "Spring", "spring" },
            mountImmediately);

    [Fact]
    public async Task CreateJobAsync_AssignsDeterministicIdentifiers()
    {
        var time = new FakeTimeProvider(new DateTimeOffset(2025, 5, 5, 7, 15, 0, TimeSpan.Zero));
        using var orchestrator = new JobLifecycleOrchestrator(time);

        var metadata = await orchestrator.CreateJobAsync(CreateRequest());

        Assert.Equal("job:20250505T071500000", metadata.JobId);
        Assert.Equal("north-40-planting", metadata.Slug);
        Assert.Equal(JobLifecycleState.Mounted, metadata.State);
        Assert.Equal(time.GetUtcNow(), metadata.CreatedAt);
        Assert.Equal(metadata.CreatedAt, metadata.UpdatedAt);
        Assert.Equal("farm:alpha", metadata.Context.FarmId);
        Assert.Single(metadata.Context.FieldIds);
        Assert.Equal("field:north-40", metadata.Context.FieldIds[0]);
        Assert.Equal("season:2025", metadata.Context.SeasonId);
        Assert.Equal("work:order-1", metadata.Context.WorkOrderId);
        Assert.Equal("Morning pass", metadata.Context.Notes);
        Assert.Equal(new[] { "Spring" }, metadata.Tags.ToArray());

        var active = await orchestrator.GetActiveJobAsync();
        Assert.NotNull(active);
        Assert.Equal(metadata.JobId, active!.JobId);
    }

    [Fact]
    public async Task CreateJobAsync_WhenMountDisabled_DoesNotActivate()
    {
        using var orchestrator = new JobLifecycleOrchestrator(new FakeTimeProvider());

        var metadata = await orchestrator.CreateJobAsync(CreateRequest(mountImmediately: false));

        Assert.Equal(JobLifecycleState.Planned, metadata.State);
        Assert.Null(await orchestrator.GetActiveJobAsync());
    }

    [Fact]
    public async Task CreateJobAsync_WithActiveJob_Throws()
    {
        using var orchestrator = new JobLifecycleOrchestrator(new FakeTimeProvider());
        await orchestrator.CreateJobAsync(CreateRequest());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => orchestrator.CreateJobAsync(CreateRequest("Second")));
        Assert.Contains("Another job is already active", exception.Message);
    }

    [Fact]
    public async Task MountJobAsync_ActivatesPlannedJob()
    {
        using var orchestrator = new JobLifecycleOrchestrator(new FakeTimeProvider());
        var job = await orchestrator.CreateJobAsync(CreateRequest(mountImmediately: false));

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        await using var watcher = orchestrator.WatchAsync(cts.Token).GetAsyncEnumerator(cts.Token);

        var mounted = await orchestrator.MountJobAsync(job.JobId);

        Assert.Equal(JobLifecycleState.Mounted, mounted.State);
        Assert.Equal(job.JobId, (await orchestrator.GetActiveJobAsync())?.JobId);

        Assert.True(await watcher.MoveNextAsync());
        Assert.Equal(JobLifecycleEventType.Mounted, watcher.Current.EventType);
        Assert.Equal(job.JobId, watcher.Current.Job.JobId);

        cts.Cancel();
    }

    [Fact]
    public async Task CloseActiveJobAsync_UpdatesStateAndBroadcastsReason()
    {
        using var orchestrator = new JobLifecycleOrchestrator(new FakeTimeProvider());
        await orchestrator.CreateJobAsync(CreateRequest());

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        await using var watcher = orchestrator.WatchAsync(cts.Token).GetAsyncEnumerator(cts.Token);

        var closed = await orchestrator.CloseActiveJobAsync("Operator finished");

        Assert.Equal(JobLifecycleState.Closed, closed.State);
        Assert.Null(await orchestrator.GetActiveJobAsync());

        Assert.True(await watcher.MoveNextAsync());
        Assert.Equal(JobLifecycleEventType.Closed, watcher.Current.EventType);
        Assert.Equal("Operator finished", watcher.Current.Reason);
        Assert.Equal(closed.JobId, watcher.Current.Job.JobId);

        cts.Cancel();
    }

    [Fact]
    public async Task WatchAsync_StreamsLifecycleEventsInOrder()
    {
        var time = new FakeTimeProvider(new DateTimeOffset(2025, 5, 5, 7, 0, 0, TimeSpan.Zero));
        using var orchestrator = new JobLifecycleOrchestrator(time);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        await using var watcher = orchestrator.WatchAsync(cts.Token).GetAsyncEnumerator(cts.Token);

        var job = await orchestrator.CreateJobAsync(CreateRequest());

        Assert.True(await watcher.MoveNextAsync());
        Assert.Equal(JobLifecycleEventType.Created, watcher.Current.EventType);
        Assert.Equal(job.JobId, watcher.Current.Job.JobId);

        Assert.True(await watcher.MoveNextAsync());
        Assert.Equal(JobLifecycleEventType.Mounted, watcher.Current.EventType);
        Assert.Equal(job.JobId, watcher.Current.Job.JobId);

        cts.Cancel();
    }

    [Fact]
    public async Task CreateJobAsync_ValidatesInputs()
    {
        using var orchestrator = new JobLifecycleOrchestrator(new FakeTimeProvider());

        await Assert.ThrowsAsync<ArgumentException>(() => orchestrator.CreateJobAsync(new JobCreationRequest(
            " ",
            new JobContext("farm:alpha", new[] { "field:one" }, null, null, null))));

        await Assert.ThrowsAsync<ArgumentException>(() => orchestrator.CreateJobAsync(new JobCreationRequest(
            "Valid",
            new JobContext(" ", new[] { "field:one" }, null, null, null))));

        await Assert.ThrowsAsync<ArgumentException>(() => orchestrator.CreateJobAsync(new JobCreationRequest(
            "Valid",
            new JobContext("farm:alpha", Array.Empty<string>(), null, null, null))));
    }
}
