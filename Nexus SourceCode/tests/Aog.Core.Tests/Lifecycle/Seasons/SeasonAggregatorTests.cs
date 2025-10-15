using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
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

}

public sealed class SeasonDiscoveryWatcherTests
{
    [Fact]
    public async Task StartAsync_PublishesSnapshotsFromSources()
    {
        var aggregator = new SeasonAggregator();
        var target = new FakeSeasonSyncTarget();
        var orchestrator = new SeasonSyncOrchestrator(aggregator, target, NullLogger<SeasonSyncOrchestrator>.Instance);
        var source = new TestSeasonDiscoverySource("local");
        await using var watcher = new SeasonDiscoveryWatcher(orchestrator, new[] { source }, NullLogger<SeasonDiscoveryWatcher>.Instance);

        await watcher.StartAsync();
        try
        {
            source.Publish(new[]
            {
                new SeasonDocument(
                    "season:2026",
                    "2026",
                    new DateOnly(2026, 1, 1),
                    new DateOnly(2026, 12, 31),
                    new[] { "job:delta" },
                    null,
                    "user:test",
                    DateTimeOffset.UtcNow,
                    DateTimeOffset.UtcNow)
            });

            (await WaitForAsync(() => target.Published.Count == 1, TimeSpan.FromSeconds(1))).Should().BeTrue();
        }
        finally
        {
            await watcher.StopAsync();
        }

        target.Published.Should().ContainSingle(aggregate => aggregate.Document.SeasonId == "season:2026");
    }

    [Fact]
    public async Task StartAsync_IgnoresNullSnapshots()
    {
        var aggregator = new SeasonAggregator();
        var target = new FakeSeasonSyncTarget();
        var orchestrator = new SeasonSyncOrchestrator(aggregator, target, NullLogger<SeasonSyncOrchestrator>.Instance);
        var source = new TestSeasonDiscoverySource("local");
        await using var watcher = new SeasonDiscoveryWatcher(orchestrator, new[] { source }, NullLogger<SeasonDiscoveryWatcher>.Instance);

        await watcher.StartAsync();
        try
        {
            source.Publish(null);
            await Task.Delay(100);
        }
        finally
        {
            await watcher.StopAsync();
        }

        target.Published.Should().BeEmpty();
    }

    [Fact]
    public async Task StartAsync_ContinuesAfterPublishFailures()
    {
        var aggregator = new SeasonAggregator();
        var target = new FlakySeasonSyncTarget(failuresBeforeSuccess: 1);
        var orchestrator = new SeasonSyncOrchestrator(aggregator, target, NullLogger<SeasonSyncOrchestrator>.Instance);
        var source = new TestSeasonDiscoverySource("local");
        await using var watcher = new SeasonDiscoveryWatcher(orchestrator, new[] { source }, NullLogger<SeasonDiscoveryWatcher>.Instance);

        await watcher.StartAsync();
        try
        {
            var snapshot = new[]
            {
                new SeasonDocument(
                    "season:2027",
                    "2027",
                    new DateOnly(2027, 1, 1),
                    new DateOnly(2027, 12, 31),
                    new[] { "job:epsilon" },
                    null,
                    "user:test",
                    DateTimeOffset.UtcNow,
                    DateTimeOffset.UtcNow)
            };

            source.Publish(snapshot);
            source.Publish(snapshot);

            (await WaitForAsync(() => target.SuccessfulPublishes == 1, TimeSpan.FromSeconds(1))).Should().BeTrue();
        }
        finally
        {
            await watcher.StopAsync();
        }

        target.Attempts.Should().BeGreaterThan(1);
    }

    [Fact]
    public void Constructor_ThrowsForDuplicateProviders()
    {
        var aggregator = new SeasonAggregator();
        var target = new FakeSeasonSyncTarget();
        var orchestrator = new SeasonSyncOrchestrator(aggregator, target, NullLogger<SeasonSyncOrchestrator>.Instance);

        var source = new TestSeasonDiscoverySource("dup");

        Action act = () => new SeasonDiscoveryWatcher(orchestrator, new[] { source, source }, NullLogger<SeasonDiscoveryWatcher>.Instance);

        act.Should().Throw<ArgumentException>();
    }

    private static async Task<bool> WaitForAsync(Func<bool> condition, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;

        while (DateTime.UtcNow <= deadline)
        {
            if (condition())
            {
                return true;
            }

            await Task.Delay(25);
        }

        return condition();
    }

    private sealed class TestSeasonDiscoverySource : ISeasonDiscoverySource
    {
        private readonly Channel<IReadOnlyList<SeasonDocument>?> _channel = Channel.CreateUnbounded<IReadOnlyList<SeasonDocument>?>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false,
        });

        public TestSeasonDiscoverySource(string providerId)
        {
            ProviderId = providerId;
        }

        public string ProviderId { get; }

        public IAsyncEnumerable<IReadOnlyList<SeasonDocument>?> WatchAsync(CancellationToken cancellationToken)
        {
            return _channel.Reader.ReadAllAsync(cancellationToken);
        }

        public void Publish(IReadOnlyList<SeasonDocument>? snapshot)
        {
            _channel.Writer.TryWrite(snapshot);
        }
    }

    private sealed class FlakySeasonSyncTarget : ISeasonSyncTarget
    {
        private readonly int _failuresBeforeSuccess;

        public FlakySeasonSyncTarget(int failuresBeforeSuccess)
        {
            _failuresBeforeSuccess = failuresBeforeSuccess;
        }

        public int Attempts { get; private set; }

        public int SuccessfulPublishes { get; private set; }

        public Task PublishAsync(SeasonAggregate aggregate, CancellationToken cancellationToken)
        {
            Attempts++;

            if (Attempts <= _failuresBeforeSuccess)
            {
                throw new InvalidOperationException("Simulated publish failure.");
            }

            SuccessfulPublishes++;
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string seasonId, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}

internal sealed class FakeSeasonSyncTarget : ISeasonSyncTarget
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
