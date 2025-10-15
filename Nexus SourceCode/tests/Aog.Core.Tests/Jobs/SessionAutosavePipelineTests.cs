using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Aog.Core.Jobs.Tests;

public sealed class SessionAutosavePipelineTests
{
    private static SessionDocument CreateSession(FakeTimeProvider time)
    {
        var now = time.GetUtcNow();
        return new SessionDocument(
            "job:alpha",
            "session:1",
            SessionLifecycleState.Active,
            now,
            endedAt: null,
            createdAt: now,
            createdBy: "user:operator",
            lastModifiedAt: now,
            operators: new[] { "user:operator" },
            notes: Array.Empty<SessionNote>(),
            extensions: new Dictionary<string, object?>());
    }

    private sealed class TestSink : ISessionAutosaveSink
    {
        public List<(SessionDocument Session, IReadOnlyList<SessionJournalEntry> Journal)> Calls { get; } = new();

        public Task PersistAsync(SessionDocument session, IReadOnlyList<SessionJournalEntry> journalEntries, CancellationToken cancellationToken)
        {
            Calls.Add((session, journalEntries));
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task FlushIfDueAsync_WhenIntervalElapsed_PersistsSnapshotAndJournal()
    {
        var time = new FakeTimeProvider(new DateTimeOffset(2025, 5, 5, 7, 0, 0, TimeSpan.Zero));
        var sink = new TestSink();
        var session = CreateSession(time);

        await using var pipeline = new SessionAutosavePipeline(
            session,
            sink,
            new SessionAutosavePipelineOptions
            {
                AutosaveInterval = TimeSpan.FromMinutes(1),
                JournalBatchSize = 10,
                JournalSizeThresholdBytes = int.MaxValue,
            },
            time);

        await pipeline.RecordJournalEntryAsync(new SessionJournalEntry("coverage", time.GetUtcNow(), 128));

        time.Advance(TimeSpan.FromMinutes(1));

        await pipeline.FlushIfDueAsync();

        Assert.Single(sink.Calls);
        var call = sink.Calls[0];
        Assert.Equal(session, call.Session);
        Assert.Single(call.Journal);
        Assert.Equal("coverage", call.Journal[0].EntryType);
    }

    [Fact]
    public async Task RecordJournalEntryAsync_WhenBatchLimitReached_FlushesImmediately()
    {
        var time = new FakeTimeProvider();
        var sink = new TestSink();
        var session = CreateSession(time);

        await using var pipeline = new SessionAutosavePipeline(
            session,
            sink,
            new SessionAutosavePipelineOptions
            {
                AutosaveInterval = TimeSpan.FromMinutes(5),
                JournalBatchSize = 2,
                JournalSizeThresholdBytes = int.MaxValue,
            },
            time);

        await pipeline.RecordJournalEntryAsync(new SessionJournalEntry("coverage", time.GetUtcNow(), 64));
        Assert.Empty(sink.Calls);

        await pipeline.RecordJournalEntryAsync(new SessionJournalEntry("coverage", time.GetUtcNow(), 64));

        Assert.Single(sink.Calls);
        var call = sink.Calls[0];
        Assert.Equal(2, call.Journal.Count);
    }

    [Fact]
    public async Task DisposeAsync_FlushesPendingData()
    {
        var time = new FakeTimeProvider();
        var sink = new TestSink();
        var session = CreateSession(time);

        var pipeline = new SessionAutosavePipeline(
            session,
            sink,
            new SessionAutosavePipelineOptions
            {
                AutosaveInterval = TimeSpan.FromMinutes(5),
                JournalBatchSize = 10,
                JournalSizeThresholdBytes = int.MaxValue,
            },
            time);

        await pipeline.RecordJournalEntryAsync(new SessionJournalEntry("coverage", time.GetUtcNow(), 64));

        await pipeline.DisposeAsync();

        Assert.Single(sink.Calls);
        Assert.Single(sink.Calls[0].Journal);
    }

    [Fact]
    public async Task FlushIfDueAsync_WhenMetadataDirtyWithoutJournal_PersistsDocument()
    {
        var time = new FakeTimeProvider();
        var sink = new TestSink();
        var session = CreateSession(time);

        await using var pipeline = new SessionAutosavePipeline(
            session,
            sink,
            new SessionAutosavePipelineOptions
            {
                AutosaveInterval = TimeSpan.FromMinutes(1),
                JournalBatchSize = 10,
                JournalSizeThresholdBytes = int.MaxValue,
            },
            time);

        var updated = session with { LastModifiedAt = time.GetUtcNow() + TimeSpan.FromSeconds(10) };
        pipeline.UpdateSession(updated);

        time.Advance(TimeSpan.FromMinutes(1));
        await pipeline.FlushIfDueAsync();

        Assert.Single(sink.Calls);
        Assert.Equal(updated, sink.Calls[0].Session);
        Assert.Empty(sink.Calls[0].Journal);
    }
}
