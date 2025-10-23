using System;
using System.Collections.Generic;
using System.Linq;
using Aog.Core.Mesh;
using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Aog.Core.Tests.Mesh;

public sealed class MeshRetentionStoreTests
{
    [Fact]
    [Trait("Category", "Guardrail")]
    public void Record_PrunesByRetentionWindowAndLimit()
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2025, 3, 21, 12, 0, 0, TimeSpan.Zero));
        var options = new MeshRetentionOptions
        {
            RetentionWindow = TimeSpan.FromMinutes(10),
            MaxEntriesPerTopic = 3,
        };

        var store = new MeshRetentionStore(options, clock);
        var topic = "aog/live/season:2025/job:alpha/coverage";

        for (var i = 0; i < 4; i++)
        {
            var publication = CreatePublication(
                publisher: "combine",
                topic,
                season: "season:2025",
                job: "job:alpha",
                layer: "coverage",
                MeshDataTier.Coverage,
                clock.GetUtcNow());

            store.Record(publication);
            clock.Advance(TimeSpan.FromMinutes(2));
        }

        // Advance beyond the retention window so the first item should be pruned by time.
        clock.Advance(TimeSpan.FromMinutes(7));

        var retained = store.Query();
        retained.Should().HaveCount(3);
        retained.Select(p => p.PublishedAt)
            .Should().BeInAscendingOrder()
            .And.Subject.Should().OnlyContain(p => p >= clock.GetUtcNow().AddMinutes(-10));
    }

    [Theory]
    [InlineData(30)]
    [InlineData(90)]
    [InlineData(365)]
    [Trait("Category", "Guardrail")]
    public void Record_RespectsRetentionWindows(int retentionDays)
    {
        var start = new DateTimeOffset(2025, 3, 24, 7, 0, 0, TimeSpan.Zero);
        var clock = new FakeTimeProvider(start);
        var options = new MeshRetentionOptions
        {
            RetentionWindow = TimeSpan.FromDays(retentionDays),
            MaxEntriesPerTopic = 8,
        };

        var store = new MeshRetentionStore(options, clock);
        var topic = "aog/live/season:alpha/job:bravo/coverage";
        var baseTime = clock.GetUtcNow();

        var expiredTimestamp = baseTime - TimeSpan.FromDays(retentionDays) - TimeSpan.FromMinutes(1);
        clock.SetUtcNow(expiredTimestamp);
        store.Record(CreatePublication(
            "combine",
            topic,
            "season:alpha",
            "job:bravo",
            "coverage",
            MeshDataTier.Coverage,
            expiredTimestamp));

        var retainedTimestamp = baseTime - TimeSpan.FromDays(retentionDays) + TimeSpan.FromMinutes(1);
        clock.SetUtcNow(retainedTimestamp);
        store.Record(CreatePublication(
            "combine",
            topic,
            "season:alpha",
            "job:bravo",
            "coverage",
            MeshDataTier.Coverage,
            retainedTimestamp));

        clock.SetUtcNow(baseTime);
        var retained = store.Query();

        retained.Should().ContainSingle();
        retained[0].PublishedAt.Should().Be(retainedTimestamp);
    }

    [Fact]
    public void Query_AppliesFilters()
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2025, 3, 22, 6, 0, 0, TimeSpan.Zero));
        var store = new MeshRetentionStore(timeProvider: clock);

        var publications = new[]
        {
            CreatePublication("combine", "aog/live/season:a/job:1/coverage", "season:a", "job:1", "coverage", MeshDataTier.Coverage, clock.GetUtcNow()),
            CreatePublication("sprayer", "aog/live/season:a/job:1/trail", "season:a", "job:1", "trail", MeshDataTier.Trails, clock.GetUtcNow()),
            CreatePublication("combine", "aog/live/season:b/job:2/coverage", "season:b", "job:2", "coverage", MeshDataTier.Coverage, clock.GetUtcNow()),
        };

        foreach (var publication in publications)
        {
            store.Record(publication);
        }

        var query = new MeshRetentionQuery(SeasonId: "season:a", TierMask: MeshDataTier.Coverage);
        var results = store.Query(query);

        results.Should().HaveCount(1);
        results[0].PublisherDeviceId.Should().Be("combine");
        results[0].LayerNamespace.Should().Be("coverage");
    }

    [Fact]
    public void ListPresence_ReturnsLatestSnapshots()
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2025, 3, 23, 9, 0, 0, TimeSpan.Zero));
        var store = new MeshRetentionStore(timeProvider: clock);

        var presenceA = CreatePresencePublication("tractor.a", "season:a", "job:1", clock.GetUtcNow(), MeshPresenceState.Online);
        store.Record(presenceA);

        clock.Advance(TimeSpan.FromMinutes(1));
        var presenceB = CreatePresencePublication("tractor.b", "season:b", "job:2", clock.GetUtcNow(), MeshPresenceState.Online);
        store.Record(presenceB);

        var allSnapshots = store.ListPresence();
        allSnapshots.Should().HaveCount(2);

        var seasonFiltered = store.ListPresence(seasonId: "season:a");
        seasonFiltered.Should().HaveCount(1);
        seasonFiltered[0].DeviceId.Should().Be("tractor.a");
    }

    private static MeshPublication CreatePublication(
        string publisher,
        string topic,
        string season,
        string job,
        string layer,
        MeshDataTier tier,
        DateTimeOffset publishedAt)
    {
        return new MeshPublication(
            publisher,
            topic,
            season,
            job,
            layer,
            tier,
            publishedAt,
            ReadOnlyMemory<byte>.Empty,
            new Dictionary<string, string>());
    }

    private static MeshPublication CreatePresencePublication(
        string publisher,
        string season,
        string job,
        DateTimeOffset timestamp,
        MeshPresenceState state)
    {
        var topic = $"aog/live/{season}/{job}/presence";
        var session = new MeshSessionDescriptor(season, job, "session:1");
        var pose = new MeshPose(45.0, -96.0);
        var snapshot = new MeshPresenceSnapshot(publisher, state, session, pose, timestamp, new Dictionary<string, string>());

        return new MeshPublication(
            publisher,
            topic,
            season,
            job,
            "presence",
            MeshDataTier.Presence,
            timestamp,
            ReadOnlyMemory<byte>.Empty,
            new Dictionary<string, string>(),
            snapshot);
    }
}
