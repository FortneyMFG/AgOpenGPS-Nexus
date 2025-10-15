using System;
using System.Threading;
using System.Threading.Tasks;
using Aog.Core.Jobs;
using Aog.Plugins.JobTasks;
using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Aog.Plugins.Tests.JobTasks;

public sealed class JobSeasonSessionOrchestratorTests
{
    [Fact]
    public async Task StartSessionAsync_AssignsDeterministicIdentifiers()
    {
        var time = new FakeTimeProvider(new DateTimeOffset(2025, 5, 5, 7, 0, 0, TimeSpan.Zero));
        var orchestrator = new JobSeasonSessionOrchestrator(time);
        await orchestrator.TrackJobAsync(CreateJobMetadata());

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        await using var watcher = orchestrator.WatchAsync(cts.Token).GetAsyncEnumerator(cts.Token);

        var session = await orchestrator.StartSessionAsync(
            "job:alpha",
            new JobSessionStartRequest(
                Name: "  Morning Run  ",
                ActiveOperators: new[] { " user:operator.maya", "USER:OPERATOR.MAYA" },
                WorkOrderId: " work:123 ",
                Notes: "  prep passes  "),
            CancellationToken.None);

        session.SessionId.Should().Be("session:1");
        session.Name.Should().Be("Morning Run");
        session.State.Should().Be(JobSessionState.Active);
        session.StartedAt.Should().Be(time.GetUtcNow());
        session.LastModifiedAt.Should().Be(time.GetUtcNow());
        session.WorkOrderId.Should().Be("work:123");
        session.Notes.Should().Be("prep passes");
        session.ActiveOperators.Should().Equal("user:operator.maya");

        var jobsForSeason = await orchestrator.ListJobsForSeasonAsync("season:2025");
        jobsForSeason.Should().ContainSingle().Which.Should().Be("job:alpha");

        Assert.True(await watcher.MoveNextAsync());
        watcher.Current.EventType.Should().Be(JobSessionEventType.Started);
        watcher.Current.Session.SessionId.Should().Be("session:1");
        watcher.Current.JobId.Should().Be("job:alpha");
        watcher.Current.SeasonId.Should().Be("season:2025");
    }

    [Fact]
    public async Task PauseResumeCompleteAsync_TransitionsInOrder()
    {
        var time = new FakeTimeProvider(new DateTimeOffset(2025, 6, 1, 12, 0, 0, TimeSpan.Zero));
        var orchestrator = new JobSeasonSessionOrchestrator(time);
        await orchestrator.TrackJobAsync(CreateJobMetadata());

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        await using var watcher = orchestrator.WatchAsync(cts.Token).GetAsyncEnumerator(cts.Token);

        var session = await orchestrator.StartSessionAsync("job:alpha", new JobSessionStartRequest(), CancellationToken.None);

        time.Advance(TimeSpan.FromMinutes(10));
        var paused = await orchestrator.PauseSessionAsync("job:alpha", session.SessionId, "Rain delay", CancellationToken.None);

        time.Advance(TimeSpan.FromMinutes(5));
        var resumed = await orchestrator.ResumeSessionAsync("job:alpha", session.SessionId, "Cleared", CancellationToken.None);

        time.Advance(TimeSpan.FromMinutes(30));
        var completed = await orchestrator.CompleteSessionAsync("job:alpha", session.SessionId, "Done", CancellationToken.None);

        paused.State.Should().Be(JobSessionState.Paused);
        resumed.State.Should().Be(JobSessionState.Active);
        completed.State.Should().Be(JobSessionState.Completed);
        completed.EndedAt.Should().Be(time.GetUtcNow());

        var sessions = await orchestrator.ListSessionsAsync("job:alpha");
        sessions.Should().ContainSingle();
        sessions[0].State.Should().Be(JobSessionState.Completed);

        Assert.True(await watcher.MoveNextAsync());
        watcher.Current.EventType.Should().Be(JobSessionEventType.Started);
        Assert.True(await watcher.MoveNextAsync());
        watcher.Current.EventType.Should().Be(JobSessionEventType.Paused);
        watcher.Current.Reason.Should().Be("Rain delay");
        Assert.True(await watcher.MoveNextAsync());
        watcher.Current.EventType.Should().Be(JobSessionEventType.Resumed);
        watcher.Current.Reason.Should().Be("Cleared");
        Assert.True(await watcher.MoveNextAsync());
        watcher.Current.EventType.Should().Be(JobSessionEventType.Completed);
        watcher.Current.Reason.Should().Be("Done");
    }

    [Fact]
    public async Task UpdateSessionMetadataAsync_ReplacesEditableFields()
    {
        var orchestrator = new JobSeasonSessionOrchestrator(new FakeTimeProvider());
        await orchestrator.TrackJobAsync(CreateJobMetadata());

        var session = await orchestrator.StartSessionAsync(
            "job:alpha",
            new JobSessionStartRequest(ActiveOperators: new[] { "user:alpha" }),
            CancellationToken.None);

        var updated = await orchestrator.UpdateSessionMetadataAsync(
            "job:alpha",
            session.SessionId,
            new JobSessionMetadata(
                Name: "  Evening pass  ",
                ActiveOperators: new[] { "user:beta", "USER:ALPHA" },
                WorkOrderId: "  ",
                Notes: null),
            CancellationToken.None);

        updated.Name.Should().Be("Evening pass");
        updated.WorkOrderId.Should().BeNull();
        updated.Notes.Should().BeNull();
        updated.ActiveOperators.Should().Equal("user:alpha", "user:beta");
    }

    [Fact]
    public async Task TrackJobAsync_ReassignsSeason()
    {
        var orchestrator = new JobSeasonSessionOrchestrator(new FakeTimeProvider());
        await orchestrator.TrackJobAsync(CreateJobMetadata());

        var initial = await orchestrator.ListJobsForSeasonAsync("season:2025");
        initial.Should().ContainSingle().Which.Should().Be("job:alpha");

        var updatedMetadata = CreateJobMetadata(seasonId: "season:2026");
        await orchestrator.TrackJobAsync(updatedMetadata);

        (await orchestrator.ListJobsForSeasonAsync("season:2025")).Should().BeEmpty();
        var reassigned = await orchestrator.ListJobsForSeasonAsync("season:2026");
        reassigned.Should().ContainSingle().Which.Should().Be("job:alpha");

        var session = await orchestrator.StartSessionAsync("job:alpha", new JobSessionStartRequest(), CancellationToken.None);
        session.SessionId.Should().Be("session:1");
    }

    [Fact]
    public async Task StartSessionAsync_WhenActiveSessionExists_Throws()
    {
        var orchestrator = new JobSeasonSessionOrchestrator(new FakeTimeProvider());
        await orchestrator.TrackJobAsync(CreateJobMetadata());
        await orchestrator.StartSessionAsync("job:alpha", new JobSessionStartRequest(), CancellationToken.None);

        var action = () => orchestrator.StartSessionAsync("job:alpha", new JobSessionStartRequest(), CancellationToken.None);
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already has an active session*");
    }

    [Fact]
    public async Task ResumeSessionAsync_WhenNotPaused_Throws()
    {
        var orchestrator = new JobSeasonSessionOrchestrator(new FakeTimeProvider());
        await orchestrator.TrackJobAsync(CreateJobMetadata());
        var session = await orchestrator.StartSessionAsync("job:alpha", new JobSessionStartRequest(), CancellationToken.None);

        var action = () => orchestrator.ResumeSessionAsync("job:alpha", session.SessionId, CancellationToken.None);
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*cannot be resumed*");
    }

    [Fact]
    public async Task RemoveJobAsync_ClearsSeasonIndex()
    {
        var orchestrator = new JobSeasonSessionOrchestrator(new FakeTimeProvider());
        await orchestrator.TrackJobAsync(CreateJobMetadata());

        await orchestrator.RemoveJobAsync("job:alpha");

        (await orchestrator.ListJobsForSeasonAsync("season:2025")).Should().BeEmpty();
        var listAction = () => orchestrator.ListSessionsAsync("job:alpha");
        await listAction.Should().ThrowAsync<KeyNotFoundException>();
    }

    private static JobMetadata CreateJobMetadata(string jobId = "job:alpha", string? seasonId = "season:2025")
    {
        return new JobMetadata(
            jobId,
            "job-alpha",
            "Alpha",
            JobLifecycleState.Mounted,
            DateTimeOffset.Parse("2025-05-05T07:00:00Z"),
            DateTimeOffset.Parse("2025-05-05T07:00:00Z"),
            activeSessionId: null,
            new JobContext(
                "farm:alpha",
                new[] { "field:north" },
                seasonId,
                workOrderId: null,
                notes: null),
            Array.Empty<string>());
    }
}
