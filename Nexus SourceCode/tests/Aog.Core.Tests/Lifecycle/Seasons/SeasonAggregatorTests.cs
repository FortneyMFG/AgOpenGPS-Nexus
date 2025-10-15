using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aog.Core.Lifecycle.Seasons;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aog.Core.Tests.Lifecycle.Seasons;

public sealed class SeasonAggregatorTests
{
    [Fact]
    public void Upsert_UnifiesJobIdsAcrossProviders()
    {
        var aggregator = new SeasonAggregator();

        var localSeason = new SeasonDocument(
            "season:2025",
            "2025 Crop Year",
            new DateOnly(2025, 1, 1),
            new DateOnly(2025, 12, 31),
            new[] { "job:a", "JOB:A", "job:b" },
            null,
            "user:planner",
            DateTimeOffset.Parse("2024-11-01T00:00:00Z"),
            DateTimeOffset.Parse("2024-11-02T00:00:00Z"));

        var remoteSeason = new SeasonDocument(
            "season:2025",
            "2025 Crop Year",
            new DateOnly(2025, 1, 1),
            new DateOnly(2025, 12, 31),
            new[] { "job:c", "job:b" },
            null,
            "system:sync",
            DateTimeOffset.Parse("2024-11-05T00:00:00Z"),
            DateTimeOffset.Parse("2024-11-06T00:00:00Z"));

        var first = aggregator.Upsert("local", localSeason);
        var second = aggregator.Upsert("cloud", remoteSeason);

        first.IsRemoval.Should().BeFalse();
        second.IsRemoval.Should().BeFalse();

        second.Aggregate!.Document.JobIds.Should().Equal("job:a", "job:b", "job:c");
        second.Aggregate.CanonicalProviderId.Should().Be("cloud");
    }

    [Fact]
    public void TrimMissing_RemovesProviderContributionAndRebuildsAggregate()
    {
        var aggregator = new SeasonAggregator();

        var canonical = new SeasonDocument(
            "season:2024",
            "2024 Crop Year",
            new DateOnly(2024, 1, 1),
            new DateOnly(2024, 12, 31),
            new[] { "job:1" },
            null,
            "user:a",
            DateTimeOffset.Parse("2023-10-01T00:00:00Z"),
            DateTimeOffset.Parse("2023-10-05T00:00:00Z"));

        var fallback = canonical with
        {
            JobIds = new[] { "job:1", "job:2" },
            CreatedBy = "user:b",
            LastModifiedAt = DateTimeOffset.Parse("2023-10-03T00:00:00Z")
        };

        aggregator.Upsert("primary", canonical);
        aggregator.Upsert("secondary", fallback);

        var changes = aggregator.TrimMissing("primary", new HashSet<string>(StringComparer.OrdinalIgnoreCase));

        changes.Should().ContainSingle();
        var change = changes[0];
        change.IsRemoval.Should().BeFalse();
        change.Aggregate!.CanonicalProviderId.Should().Be("secondary");
        change.Aggregate.Document.JobIds.Should().Equal("job:1", "job:2");
    }
}

public sealed class SeasonSyncOrchestratorTests
{
    [Fact]
    public async Task ApplySnapshotAsync_PublishesUpdatesAndRemovals()
    {
        var aggregator = new SeasonAggregator();
        var target = new FakeSeasonSyncTarget();
        var orchestrator = new SeasonSyncOrchestrator(aggregator, target, NullLogger<SeasonSyncOrchestrator>.Instance);

        var initial = new[]
        {
            new SeasonDocument(
                "season:2024",
                "2024",
                new DateOnly(2024, 1, 1),
                new DateOnly(2024, 12, 31),
                new[] { "job:alpha" },
                null,
                "user:a",
                DateTimeOffset.Parse("2023-11-01T00:00:00Z"),
                DateTimeOffset.Parse("2023-11-02T00:00:00Z")),
            new SeasonDocument(
                "season:2025",
                "2025",
                new DateOnly(2025, 1, 1),
                new DateOnly(2025, 12, 31),
                new[] { "job:beta" },
                null,
                "user:b",
                DateTimeOffset.Parse("2024-01-01T00:00:00Z"),
                DateTimeOffset.Parse("2024-01-02T00:00:00Z"))
        };

        await orchestrator.ApplySnapshotAsync("local", initial, CancellationToken.None);

        target.Published.Should().HaveCount(2);
        target.Removed.Should().BeEmpty();

        target.Clear();

        var update = new[]
        {
            initial[0] with
            {
                JobIds = new[] { "job:alpha", "job:gamma" },
                LastModifiedAt = DateTimeOffset.Parse("2023-12-01T00:00:00Z")
            }
        };

        await orchestrator.ApplySnapshotAsync("local", update, CancellationToken.None);

        target.Published.Should().ContainSingle(aggregate => aggregate.Document.SeasonId == "season:2024");
        target.Published[0].Document.JobIds.Should().Equal("job:alpha", "job:gamma");
        target.Removed.Should().ContainSingle().Which.Should().Be("season:2025");
    }

    private sealed class FakeSeasonSyncTarget : ISeasonSyncTarget
    {
        public List<SeasonAggregate> Published { get; } = new();
        public List<string> Removed { get; } = new();

        public Task PublishAsync(SeasonAggregate aggregate, CancellationToken cancellationToken)
        {
            Published.Add(aggregate);
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string seasonId, CancellationToken cancellationToken)
        {
            Removed.Add(seasonId);
            return Task.CompletedTask;
        }

        public void Clear()
        {
            Published.Clear();
            Removed.Clear();
        }
    }
}
