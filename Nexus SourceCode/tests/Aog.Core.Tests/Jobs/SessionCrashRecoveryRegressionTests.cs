using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Aog.Core.Jobs.Tests;

public sealed class SessionCrashRecoveryRegressionTests
{
    [Fact]
    [Trait("Category", "Guardrail")]
    public async Task CrashRecovery_ReplaysLostJournalEntryAndPersistsResumeAsync()
    {
        var start = new DateTimeOffset(2025, 5, 5, 7, 0, 0, TimeSpan.Zero);
        var time = new FakeTimeProvider(start);
        var options = new SessionAutosavePipelineOptions
        {
            AutosaveInterval = TimeSpan.FromSeconds(30),
            JournalBatchSize = 2,
            JournalSizeThresholdBytes = int.MaxValue,
        };

        var initialSink = new RecordingSink();
        var session = CreateSession(time);
        var pipeline = new SessionAutosavePipeline(session, initialSink, options, time);

        var entry1 = new SessionJournalEntry("posestream", time.GetUtcNow(), 512);
        await pipeline.RecordJournalEntryAsync(entry1);

        time.Advance(TimeSpan.FromSeconds(4));
        var entry2 = new SessionJournalEntry("posestream", time.GetUtcNow(), 512);
        await pipeline.RecordJournalEntryAsync(entry2);

        time.Advance(TimeSpan.FromSeconds(4));
        var lostEntry = new SessionJournalEntry("posestream", time.GetUtcNow(), 512);
        await pipeline.RecordJournalEntryAsync(lostEntry);

        // Simulate an unexpected crash by abandoning the pipeline without flushing the
        // remaining journal buffer. The recording sink references keep the persisted calls
        // accessible for the recovery phase below.
        pipeline = null;

        var persistedBeforeCrash = initialSink.Calls;
        Assert.Single(persistedBeforeCrash);
        Assert.Equal(new[] { entry1, entry2 }, persistedBeforeCrash[0].Journal);

        var persistedSnapshot = persistedBeforeCrash[0].Session;

        time.Advance(TimeSpan.FromSeconds(2));
        var resumeTimestamp = time.GetUtcNow();
        var resumedSession = persistedSnapshot with { LastModifiedAt = resumeTimestamp };

        var recoverySink = new RecordingSink();
        await using var recoveryPipeline = new SessionAutosavePipeline(resumedSession, recoverySink, options, time);

        await recoveryPipeline.RecordJournalEntryAsync(lostEntry);

        time.Advance(TimeSpan.FromSeconds(3));
        var resumeEntry = new SessionJournalEntry("posestream", time.GetUtcNow(), 512);
        await recoveryPipeline.RecordJournalEntryAsync(resumeEntry);

        Assert.Single(recoverySink.Calls);
        var recoveryCall = recoverySink.Calls[0];
        Assert.Equal(new[] { lostEntry, resumeEntry }, recoveryCall.Journal);
        Assert.Equal(resumedSession, recoveryCall.Session);

        var combined = initialSink.Calls
            .SelectMany(call => call.Journal)
            .Concat(recoverySink.Calls.SelectMany(call => call.Journal))
            .ToList();

        Assert.Equal(new[] { entry1, entry2, lostEntry, resumeEntry }, combined);
    }

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

    private sealed class RecordingSink : ISessionAutosaveSink
    {
        public List<(SessionDocument Session, IReadOnlyList<SessionJournalEntry> Journal)> Calls { get; } = new();

        public Task PersistAsync(SessionDocument session, IReadOnlyList<SessionJournalEntry> journalEntries, CancellationToken cancellationToken)
        {
            Calls.Add((session, journalEntries));
            return Task.CompletedTask;
        }
    }
}
