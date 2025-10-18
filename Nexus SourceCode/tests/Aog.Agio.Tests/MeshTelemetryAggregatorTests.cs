using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aog.Agio.Legacy;
using Aog.Agio.Telemetry;
using Aog.Core.Mesh;
using Aog.Core.V1;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Aog.Agio.Tests;

public sealed class MeshTelemetryAggregatorTests
{
    [Fact]
    public async Task OnPoseAsync_PublishesPresenceAndTrail()
    {
        var start = new DateTimeOffset(2025, 3, 19, 12, 0, 0, TimeSpan.Zero);
        var clock = new FakeTimeProvider(start);
        var mesh = new LiveTelemetryMeshService(clock);

        await mesh.RegisterOrUpdateDeviceAsync(new MeshDeviceRegistration(
            "subscriber",
            "Test Subscriber",
            subscribeProfile: new MeshSubscribeProfile(new[]
            {
                new MeshSubscribeGrant("season-2025", "job-42", MeshDataTier.Presence | MeshDataTier.Trails, new[] { "presence", "trail" })
            })), CancellationToken.None);

        var aggregator = new MeshTelemetryAggregator(
            mesh,
            Options.Create(new MeshTelemetryAggregatorOptions
            {
                DeviceId = "tractor.alpha",
                DeviceLabel = "Tractor Alpha",
                TrailCapacity = 16,
                TrailPublishInterval = TimeSpan.Zero,
            }),
            NullLogger<MeshTelemetryAggregator>.Instance,
            clock);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        await using var subscription = mesh
            .SubscribeAsync(new MeshSubscriptionRequest(
                "subscriber",
                SeasonId: "season-2025",
                JobId: "job-42",
                TierMask: MeshDataTier.Presence | MeshDataTier.Trails), cts.Token)
            .GetAsyncEnumerator(cts.Token);

        var pose = new Pose
        {
            Header = new Header
            {
                Sequence = 42,
                Source = " gps/main ",
                Frame = " earth ",
                JobId = " job-42 ",
                SeasonId = " season-2025 ",
                SessionId = " session-alpha ",
                Timestamp = Timestamp.FromDateTimeOffset(clock.GetUtcNow()),
            },
            LatitudeDeg = 45.1234,
            LongitudeDeg = -96.3219,
            AltitudeM = 349.8,
            HeadingRad = Math.PI / 2,
            SpeedMps = 4.25,
        };

        var metadata = new LegacyPoseMetadata
        {
            SourceAddress = 0x7C,
            FixQuality = 2,
            SatellitesTracked = 15,
            HdopTimes100 = 120,
            AgeOfCorrectionsTimes100 = 30,
            ImuHeadingHundredths = 1234,
            ImuRollHundredths = -56,
            ImuPitchHundredths = 78,
            ImuYawRateHundredths = -90,
        };

        await aggregator.OnPoseAsync(pose, metadata, cts.Token);

        Assert.True(await subscription.MoveNextAsync());
        var presence = subscription.Current;
        Assert.Equal(MeshDataTier.Presence, presence.Tier);
        var snapshot = Assert.IsType<MeshPresenceSnapshot>(presence.State);
        Assert.Equal("tractor.alpha", snapshot.DeviceId);
        Assert.Equal("season-2025", snapshot.Session.SeasonId);
        Assert.Equal("job-42", snapshot.Session.JobId);
        Assert.Equal("session-alpha", snapshot.Session.SessionId);
        Assert.Equal(pose.LatitudeDeg, snapshot.Pose.Latitude, 6);
        Assert.Equal(pose.LongitudeDeg, snapshot.Pose.Longitude, 6);
        Assert.True(presence.Metadata!.ContainsKey("fixQuality"));
        Assert.Equal("2", presence.Metadata["fixQuality"]);

        Assert.True(await subscription.MoveNextAsync());
        var trailPublication = subscription.Current;
        Assert.Equal(MeshDataTier.Trails, trailPublication.Tier);
        Assert.Equal("1", trailPublication.Metadata!["pointCount"]);

        var serializerOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var trail = JsonSerializer.Deserialize<MeshTelemetryAggregator.MeshTrailSnapshot>(trailPublication.Payload.Span, serializerOptions);
        Assert.NotNull(trail);
        Assert.Equal("tractor.alpha", trail!.DeviceId);
        Assert.Equal("season-2025", trail.SeasonId);
        Assert.Equal("job-42", trail.JobId);
        Assert.Single(trail.Points);
        var point = trail.Points[0];
        Assert.Equal(pose.LatitudeDeg, point.Latitude, 6);
        Assert.Equal(pose.LongitudeDeg, point.Longitude, 6);
    }

    [Fact]
    public async Task OnPoseAsync_RespectsTrailPublishInterval()
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2025, 4, 10, 9, 0, 0, TimeSpan.Zero));
        var mesh = new LiveTelemetryMeshService(clock);

        await mesh.RegisterOrUpdateDeviceAsync(new MeshDeviceRegistration(
            "watcher",
            "Watcher",
            subscribeProfile: new MeshSubscribeProfile(new[]
            {
                new MeshSubscribeGrant("season", "job", MeshDataTier.Presence | MeshDataTier.Trails, new[] { "presence", "trail" })
            })), CancellationToken.None);

        var aggregator = new MeshTelemetryAggregator(
            mesh,
            Options.Create(new MeshTelemetryAggregatorOptions
            {
                DeviceId = "tractor.beta",
                DeviceLabel = "Tractor Beta",
                TrailCapacity = 8,
                TrailPublishInterval = TimeSpan.FromSeconds(5),
            }),
            NullLogger<MeshTelemetryAggregator>.Instance,
            clock);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        await using var subscription = mesh
            .SubscribeAsync(new MeshSubscriptionRequest(
                "watcher",
                SeasonId: "season",
                JobId: "job",
                TierMask: MeshDataTier.Presence | MeshDataTier.Trails), cts.Token)
            .GetAsyncEnumerator(cts.Token);

        await aggregator.OnPoseAsync(CreatePose(clock, "season", "job", "session"), new LegacyPoseMetadata(), cts.Token);

        // Presence
        Assert.True(await subscription.MoveNextAsync());
        // Trail
        Assert.True(await subscription.MoveNextAsync());
        var firstTrail = subscription.Current;
        Assert.Equal(MeshDataTier.Trails, firstTrail.Tier);

        clock.Advance(TimeSpan.FromSeconds(2));
        await aggregator.OnPoseAsync(CreatePose(clock, "season", "job", "session"), new LegacyPoseMetadata(), cts.Token);

        // Presence update for the second pose
        Assert.True(await subscription.MoveNextAsync());
        Assert.Equal(MeshDataTier.Presence, subscription.Current.Tier);

        var nextTask = subscription.MoveNextAsync().AsTask();
        await Task.Delay(50, CancellationToken.None);
        Assert.False(nextTask.IsCompleted);
        cts.Cancel();
    }

    [Fact]
    public async Task OnPoseAsync_IgnoresMissingContext()
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2025, 5, 1, 8, 0, 0, TimeSpan.Zero));
        var mesh = new LiveTelemetryMeshService(clock);
        var aggregator = new MeshTelemetryAggregator(
            mesh,
            Options.Create(new MeshTelemetryAggregatorOptions()),
            NullLogger<MeshTelemetryAggregator>.Instance,
            clock);

        var pose = new Pose
        {
            Header = new Header
            {
                SeasonId = string.Empty,
                JobId = string.Empty,
            },
            LatitudeDeg = 0,
            LongitudeDeg = 0,
        };

        await aggregator.OnPoseAsync(pose, new LegacyPoseMetadata(), CancellationToken.None);

        Assert.Empty(mesh.ListPresence());
    }

    private static Pose CreatePose(FakeTimeProvider clock, string season, string job, string session)
    {
        return new Pose
        {
            Header = new Header
            {
                SeasonId = season,
                JobId = job,
                SessionId = session,
                Timestamp = Timestamp.FromDateTimeOffset(clock.GetUtcNow()),
            },
            LatitudeDeg = 44.0,
            LongitudeDeg = -95.0,
            HeadingRad = 0.5,
            SpeedMps = 3.0,
        };
    }
}
