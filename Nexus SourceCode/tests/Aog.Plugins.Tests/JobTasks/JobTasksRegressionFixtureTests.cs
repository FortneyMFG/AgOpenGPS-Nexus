using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Aog.Plugins.JobTasks;
using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using Xunit;

using PluginJobSessionState = Aog.Plugins.JobTasks.JobSessionState;

namespace Aog.Plugins.Tests.JobTasks;

public sealed class JobTasksRegressionFixtureTests
{
    [Fact]
    public async Task CreateSampleSnapshot_ShouldRoundTripThroughPersistence()
    {
        var snapshot = JobTasksFixtureCatalog.CreateSampleSnapshot();
        using var temp = new TemporaryDirectory();

        var jobRoot = Path.Combine(temp.DirectoryPath, "Jobs", "North40");
        var layout = snapshot.Layout with
        {
            JobRoot = jobRoot,
            DataDirectory = Path.Combine(jobRoot, "data"),
            ResumeFile = Path.Combine(jobRoot, "Resume.txt"),
            AttachmentsDirectory = Path.Combine(jobRoot, "attachments"),
        };

        var adjusted = snapshot with { Layout = layout };
        var persistence = new JobTasksPersistence(new FakeTimeProvider(snapshot.Metadata.UpdatedAt.AddMinutes(15)));

        await persistence.SaveAsync(adjusted).ConfigureAwait(false);
        var loaded = await persistence.LoadAsync(jobRoot).ConfigureAwait(false);

        loaded.Metadata.Should().Be(snapshot.Metadata);
        loaded.Sessions.Should().BeEquivalentTo(snapshot.Sessions);
        loaded.Layout.JobRoot.Should().Be(layout.JobRoot);
        loaded.Layout.DataDirectory.Should().Be(layout.DataDirectory);
        loaded.Layout.ResumeFile.Should().Be(layout.ResumeFile);
        loaded.Equipment.Should().Be(snapshot.Equipment);
        loaded.Spatial.Should().BeEquivalentTo(snapshot.Spatial);
        loaded.Assets.Should().BeEquivalentTo(snapshot.Assets);
        loaded.Stats.Should().BeEquivalentTo(snapshot.Stats);
    }

    [Fact]
    public async Task CreateLifecycleScenarioAsync_ShouldEmitDeterministicEvents()
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2025, 5, 5, 7, 30, 0, TimeSpan.Zero));
        var fixture = await JobTasksFixtureCatalog.CreateLifecycleScenarioAsync(
            clock,
            advanceTime: span => clock.Advance(span)).ConfigureAwait(false);

        using var orchestrator = fixture.Orchestrator;

        fixture.Events.Select(evt => evt.EventType)
            .Should().ContainInOrder(
                JobSessionEventType.Started,
                JobSessionEventType.Paused,
                JobSessionEventType.Resumed,
                JobSessionEventType.Completed);

        fixture.Events.Should().AllSatisfy(evt =>
        {
            evt.JobId.Should().Be(fixture.Job.JobId);
            evt.Session.SessionId.Should().Be(fixture.SessionId);
        });

        var completed = fixture.Sessions.Single();
        completed.State.Should().Be(PluginJobSessionState.Completed);
        completed.LastModifiedAt.Should().Be(clock.GetUtcNow());
        fixture.Job.ActiveSessionId.Should().Be("session:fixture-1");
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            DirectoryPath = Path.Combine(Path.GetTempPath(), "aog-jobtasks-fixtures", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(DirectoryPath);
        }

        public string DirectoryPath { get; }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(DirectoryPath))
                {
                    Directory.Delete(DirectoryPath, recursive: true);
                }
            }
            catch
            {
                // Ignore cleanup failures in tests.
            }
        }
    }
}
