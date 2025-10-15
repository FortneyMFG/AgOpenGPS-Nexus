using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Aog.Core.Eventing;
using Aog.Core.Jobs;
using Aog.Core.V1;
using Aog.Plugins.TelemetryLogging;
using FluentAssertions;
using Google.Protobuf.WellKnownTypes;
using Xunit;

namespace Aog.Plugins.Tests;

public sealed class TelemetryLoggingCoordinatorTests
{
    [Fact]
    public async Task HandleEventAsync_MountedCreatesSessionWithManifest()
    {
        var time = new FakeTimeProvider(new DateTimeOffset(2025, 5, 1, 8, 0, 0, TimeSpan.Zero));
        var bus = new InMemoryEventBus();
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var options = new TelemetryLoggingOptions { RootDirectory = root };
        var coordinator = new TelemetryLoggingCoordinator(bus, options, time);
        var job = CreateJobMetadata(time.GetUtcNow());

        try
        {
            await coordinator.HandleEventAsync(new JobLifecycleEvent(JobLifecycleEventType.Mounted, job));

            var manifests = coordinator.ListSessions();
            manifests.Should().HaveCount(1);
            var manifest = manifests[0];

            manifest.SessionId.Should().StartWith("session-");
            manifest.JobId.Should().Be(job.JobId);
            manifest.JobSlug.Should().Be(job.Slug);
            manifest.JobDisplayName.Should().Be(job.DisplayName);
            manifest.StartedAt.Should().Be(time.GetUtcNow());
            manifest.EndedAt.Should().BeNull();
            manifest.FarmId.Should().Be(job.Context.FarmId);
            manifest.FieldIds.Should().Equal(job.Context.FieldIds);
            manifest.SeasonId.Should().Be(job.Context.SeasonId);
            manifest.WorkOrderId.Should().Be(job.Context.WorkOrderId);
            manifest.JobTags.Should().Equal(job.Tags);

            var sessionDirectory = ResolveSessionDirectory(root, manifest.Directory);
            Directory.Exists(sessionDirectory).Should().BeTrue();
            File.Exists(Path.Combine(sessionDirectory, options.MetadataFileName)).Should().BeTrue();
            File.Exists(Path.Combine(sessionDirectory, manifest.Files.Pose)).Should().BeTrue();
            File.Exists(Path.Combine(sessionDirectory, manifest.Files.Imu)).Should().BeTrue();
        }
        finally
        {
            await coordinator.DisposeAsync();
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task HandleEventAsync_ClosedFinalizesSessionAndStopsLogging()
    {
        var time = new FakeTimeProvider(new DateTimeOffset(2025, 6, 10, 14, 30, 0, TimeSpan.Zero));
        var bus = new InMemoryEventBus();
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var options = new TelemetryLoggingOptions { RootDirectory = root };
        var coordinator = new TelemetryLoggingCoordinator(bus, options, time);
        var job = CreateJobMetadata(time.GetUtcNow());

        try
        {
            await coordinator.HandleEventAsync(new JobLifecycleEvent(JobLifecycleEventType.Mounted, job));

            var manifest = coordinator.ListSessions().Single();
            var sessionDirectory = ResolveSessionDirectory(root, manifest.Directory);
            var posePath = Path.Combine(sessionDirectory, manifest.Files.Pose);

            await bus.PublishAsync(new Pose
            {
                Header = new Header
                {
                    Sequence = 1,
                    Timestamp = Timestamp.FromDateTimeOffset(time.GetUtcNow()),
                    Frame = "vehicle",
                    Source = "sim"
                },
                LatitudeDeg = 51.5,
                LongitudeDeg = -0.12,
                AltitudeM = 120.0,
                HeadingRad = 1.2,
                RollRad = 0.1,
                PitchRad = -0.05,
                SpeedMps = 5.5,
                YawRateRadps = 0.02
            });

            var lengthBeforeClose = new FileInfo(posePath).Length;
            lengthBeforeClose.Should().BeGreaterThan(0);

            time.Advance(TimeSpan.FromMinutes(7));

            var closedJob = job with
            {
                State = JobLifecycleState.Closed,
                UpdatedAt = time.GetUtcNow()
            };

            await coordinator.HandleEventAsync(new JobLifecycleEvent(JobLifecycleEventType.Closed, closedJob, "Session complete"));

            var manifests = coordinator.ListSessions();
            manifests.Should().HaveCount(1);
            var completed = manifests[0];
            completed.EndedAt.Should().Be(time.GetUtcNow());
            completed.CloseReason.Should().Be("Session complete");

            var lengthAfterClose = new FileInfo(posePath).Length;

            await bus.PublishAsync(new Pose
            {
                Header = new Header
                {
                    Sequence = 2,
                    Timestamp = Timestamp.FromDateTimeOffset(time.GetUtcNow()),
                    Frame = "vehicle",
                    Source = "sim"
                },
                LatitudeDeg = 51.6,
                LongitudeDeg = -0.11,
                AltitudeM = 121.0,
                HeadingRad = 1.1,
                RollRad = 0.12,
                PitchRad = -0.04,
                SpeedMps = 5.7,
                YawRateRadps = 0.03
            });

            new FileInfo(posePath).Length.Should().Be(lengthAfterClose);
        }
        finally
        {
            await coordinator.DisposeAsync();
            DeleteDirectory(root);
        }
    }

    private static JobMetadata CreateJobMetadata(DateTimeOffset timestamp)
    {
        var context = new JobContext(
            "farm:demo",
            new[] { "field:alpha", "field:beta" },
            "season:2025",
            "work:42",
            "Training session");

        return new JobMetadata(
            JobId: $"job:{timestamp:yyyyMMddTHHmmssfff}",
            Slug: "demo-job",
            DisplayName: "Demo Job",
            State: JobLifecycleState.Mounted,
            CreatedAt: timestamp,
            UpdatedAt: timestamp,
            ActiveSessionId: null,
            Context: context,
            Tags: new[] { "demo", "telemetry" });
    }

    private static string ResolveSessionDirectory(string root, string relative)
    {
        if (string.IsNullOrWhiteSpace(relative))
        {
            return root;
        }

        var segments = relative.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
        return Path.Combine(new[] { root }.Concat(segments).ToArray());
    }

    private static void DeleteDirectory(string root)
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
