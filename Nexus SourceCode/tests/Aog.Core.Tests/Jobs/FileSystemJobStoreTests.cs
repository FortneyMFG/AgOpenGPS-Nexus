using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Aog.Core.Jobs.Tests;

public sealed class FileSystemJobStoreTests
{
    [Fact]
    public async Task ResumeAsync_WhenDifferentJobIsActive_DeactivatesPreviousJob()
    {
        var start = new DateTimeOffset(2025, 5, 1, 12, 0, 0, TimeSpan.Zero);
        var time = new FakeTimeProvider(start);
        var store = new FileSystemJobStore(time);

        var jobA = new JobDocument(
            "job:a",
            "Job Alpha",
            JobLifecycleState.Active,
            start,
            start,
            new[]
            {
                new JobSessionDocument(
                    "session:a",
                    "Session Alpha",
                    JobSessionState.Active,
                    start,
                    endedAt: null),
            });

        var jobB = new JobDocument(
            "job:b",
            "Job Beta",
            JobLifecycleState.Paused,
            start,
            start,
            new[]
            {
                new JobSessionDocument(
                    "session:b",
                    "Session Beta",
                    JobSessionState.Paused,
                    start,
                    endedAt: start),
            });

        store.Seed(jobA, isActive: true);
        store.Seed(jobB);

        time.Advance(TimeSpan.FromMinutes(5));
        var resumed = await store.ResumeAsync("job:b", CancellationToken.None);

        resumed.JobId.Should().Be("job:b");
        resumed.SessionId.Should().NotBeNullOrWhiteSpace();
        resumed.ResumedAt.Should().Be(time.GetUtcNow());

        store.TryGetJob("job:a", out var storedJobA).Should().BeTrue();
        storedJobA!.State.Should().Be(JobLifecycleState.Paused);
        storedJobA.UpdatedAt.Should().Be(time.GetUtcNow());
        storedJobA.Sessions.Single().State.Should().Be(JobSessionState.Paused);
        storedJobA.Sessions.Single().EndedAt.Should().Be(time.GetUtcNow());

        store.TryGetJob("job:b", out var storedJobB).Should().BeTrue();
        storedJobB!.State.Should().Be(JobLifecycleState.Active);
        storedJobB.UpdatedAt.Should().Be(time.GetUtcNow());
        storedJobB.Sessions.Last().State.Should().Be(JobSessionState.Active);
        storedJobB.Sessions.Last().EndedAt.Should().BeNull();
    }
}
