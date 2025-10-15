using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aog.Core.Mesh;
using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Aog.Core.Mesh.Tests;

public sealed class LiveTelemetryMeshServiceTests
{
    [Fact]
    public async Task RegisterAndPresenceUpdate_NormalizesIdentifiers()
    {
        var start = new DateTimeOffset(2025, 3, 19, 12, 0, 0, TimeSpan.Zero);
        var clock = new FakeTimeProvider(start);
        var service = new LiveTelemetryMeshService(clock);

        await service.RegisterOrUpdateDeviceAsync(new MeshDeviceRegistration(
            " device:alpha ",
            "  Harvester  ",
            new[] { "presence", "Presence" },
            new MeshShareProfile(new[]
            {
                new MeshShareGrant(" season:2025 ", " job:123 ", MeshDataTier.Presence, new[] { "presence" })
            }),
            new MeshSubscribeProfile(new[]
            {
                new MeshSubscribeGrant("*", "*", MeshDataTier.Presence)
            })),
            CancellationToken.None);

        await service.UpdatePresenceAsync(new MeshPresenceUpdate(
            "device:alpha",
            new MeshSessionDescriptor(" season:2025 ", " job:123 ", " session:42 "),
            new MeshPose(45.123, -96.321, headingDegrees: 87.5),
            MeshPresenceState.Online,
            new Dictionary<string, string> { [" operator "] = "  Ada  " }),
            CancellationToken.None);

        var presence = service.ListPresence();
        presence.Should().HaveCount(1);

        var snapshot = presence[0];
        snapshot.DeviceId.Should().Be("device:alpha");
        snapshot.Session.SeasonId.Should().Be("season:2025");
        snapshot.Session.JobId.Should().Be("job:123");
        snapshot.Session.SessionId.Should().Be("session:42");
        snapshot.Metadata.Should().ContainKey("operator").WhoseValue.Should().Be("Ada");
        snapshot.UpdatedAt.Should().Be(clock.GetUtcNow());
    }

    [Fact]
    public async Task PublishAsync_WhenShareProfileMissingPermission_Throws()
    {
        var service = new LiveTelemetryMeshService(new FakeTimeProvider());

        await service.RegisterOrUpdateDeviceAsync(new MeshDeviceRegistration(
            "device:beta",
            "Sprayer",
            shareProfile: new MeshShareProfile(new[]
            {
                new MeshShareGrant("season:2025", "job:alpha", MeshDataTier.Presence, new[] { "presence" })
            })),
            CancellationToken.None);

        var request = new MeshPublishRequest(
            "device:beta",
            "aog/live/season:2025/job:alpha/coverage",
            MeshDataTier.Coverage,
            new byte[] { 0x01 });

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.PublishAsync(request));
    }

    [Fact]
    public async Task SubscribeAsync_DeliversAuthorizedPublications()
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2025, 3, 19, 7, 30, 0, TimeSpan.Zero));
        var service = new LiveTelemetryMeshService(clock);

        await service.RegisterOrUpdateDeviceAsync(new MeshDeviceRegistration(
            "publisher",
            "Combine",
            shareProfile: new MeshShareProfile(new[]
            {
                new MeshShareGrant("season:2025", "job:alpha", MeshDataTier.Presence | MeshDataTier.Coverage, new[] { "presence", "coverage" })
            })),
            CancellationToken.None);

        await service.RegisterOrUpdateDeviceAsync(new MeshDeviceRegistration(
            "subscriber",
            "Scout",
            subscribeProfile: new MeshSubscribeProfile(new[]
            {
                new MeshSubscribeGrant("season:2025", "job:alpha", MeshDataTier.Coverage, new[] { "coverage" })
            })),
            CancellationToken.None);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        await using var watcher = service
            .SubscribeAsync(new MeshSubscriptionRequest(
                "subscriber",
                seasonId: "season:2025",
                jobId: "job:alpha",
                tierMask: MeshDataTier.Coverage), cts.Token)
            .GetAsyncEnumerator(cts.Token);

        var payload = new byte[] { 0x10, 0x20, 0x30 };
        await service.PublishAsync(new MeshPublishRequest(
            "publisher",
            "aog/live/season:2025/job:alpha/coverage",
            MeshDataTier.Coverage,
            payload),
            cts.Token);

        Assert.True(await watcher.MoveNextAsync());

        var publication = watcher.Current;
        publication.Topic.Should().Be("aog/live/season:2025/job:alpha/coverage");
        publication.PublisherDeviceId.Should().Be("publisher");
        publication.Payload.ToArray().Should().Equal(payload);
        publication.Metadata.Should().BeEmpty();
        publication.PublishedAt.Should().Be(clock.GetUtcNow());
    }

    [Fact]
    public async Task ListPresence_PrunesSnapshotsAfterTtl()
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2025, 3, 19, 8, 0, 0, TimeSpan.Zero));
        var service = new LiveTelemetryMeshService(clock);

        await service.RegisterOrUpdateDeviceAsync(new MeshDeviceRegistration(
            "device:gamma",
            "Tractor",
            shareProfile: new MeshShareProfile(new[]
            {
                new MeshShareGrant("season:2025", "job:beta", MeshDataTier.Presence, new[] { "presence" })
            })),
            CancellationToken.None);

        await service.UpdatePresenceAsync(new MeshPresenceUpdate(
            "device:gamma",
            new MeshSessionDescriptor("season:2025", "job:beta"),
            new MeshPose(40.0, -90.0)),
            CancellationToken.None);

        service.ListPresence().Should().HaveCount(1);

        clock.Advance(TimeSpan.FromSeconds(4));
        service.ListPresence().Should().HaveCount(1);

        clock.Advance(TimeSpan.FromSeconds(2));
        service.ListPresence().Should().BeEmpty();
    }
}
