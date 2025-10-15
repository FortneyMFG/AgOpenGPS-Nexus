using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aog.Core.Jobs;

namespace Aog.Plugins.JobTasks;

/// <summary>
/// Provides deterministic fixtures for regression tests and simulation harnesses exercising the Job Tasks plugin.
/// Implements NX-290 by seeding metadata snapshots and lifecycle scenarios referenced across tests.
/// </summary>
public static class JobTasksFixtureCatalog
{
    private static readonly DateTimeOffset FixtureCreatedAt = new(2025, 5, 5, 7, 15, 0, TimeSpan.Zero);

    /// <summary>
    /// Creates a representative job snapshot containing metadata, filesystem layout, sessions, and attachments.
    /// </summary>
    public static JobSnapshot CreateSampleSnapshot()
    {
        var metadata = CreateSampleMetadata();
        var layout = CreateSampleLayout();

        var session = new JobSessionSnapshot(
            SessionId: "session:fixture-1",
            State: JobSessionState.Active,
            StartedAt: FixtureCreatedAt.AddMinutes(5),
            LastModifiedAt: FixtureCreatedAt.AddMinutes(30),
            Name: "Fixture morning run",
            EndedAt: null,
            ActiveOperators: new[] { "user:operator.maya", "user:operator.liam" },
            Stats: new JobSessionStatisticsSnapshot(12.4, 8.1, 5400, 58.2),
            Extensions: new Dictionary<string, JsonElement>());

        var spatial = new JobSpatialSnapshot(
            Home: new JobLatLonSnapshot(42.1204, -93.1124, 320.5),
            Envelope: new JobEnvelopeSnapshot(42.1100, -93.1300, 42.1400, -93.0900),
            PrimaryBoundaryId: "boundary:north-40",
            BoundaryReferences: new[] { "boundary:headland" },
            CoordinateReferenceSystem: "EPSG:4326");

        var assets = new JobAssetSnapshot(
            BoundaryLayers: new[] { "layer:boundary.north-40" },
            CoverageLayers: new[] { "layer:coverage.north-40" },
            GuidanceSets: new[] { "guidance:ab-north" },
            Prescriptions: new[] { "prescription:2025-north-40" },
            Attachments: new[]
            {
                new JobAttachmentSnapshot("attachment:plan", Path.Combine("attachments", "plan.pdf"), "application/pdf", 1024)
            });

        var stats = new JobStatisticsSnapshot(
            TotalAreaHectares: 12.4,
            TotalDistanceKilometers: 18.6,
            ActiveDurationSeconds: 7200,
            CompletedSessionCount: 1);

        var equipment = new JobEquipmentSnapshot(
            VehicleId: "equipment:tractor.alpha",
            ImplementId: "equipment:planter.16r",
            PresetId: "preset:planter-fixture",
            LayoutId: "layout:planter-dashboard");

        return new JobSnapshot(
            metadata,
            layout,
            new[] { session },
            Spatial: spatial,
            Assets: assets,
            Stats: stats,
            Extensions: null,
            Equipment: equipment);
    }

    /// <summary>
    /// Creates a lifecycle scenario exercising start, pause, resume, and complete transitions.
    /// </summary>
    /// <param name="timeProvider">Clock used to produce deterministic timestamps.</param>
    /// <param name="advanceTime">Optional callback used to advance the supplied <paramref name="timeProvider"/>.</param>
    /// <param name="cancellationToken">Token used to cancel orchestration.</param>
    public static async Task<JobTasksLifecycleFixture> CreateLifecycleScenarioAsync(
        TimeProvider timeProvider,
        Action<TimeSpan>? advanceTime = null,
        CancellationToken cancellationToken = default)
    {
        if (timeProvider is null)
        {
            throw new ArgumentNullException(nameof(timeProvider));
        }

        var orchestrator = new JobSeasonSessionOrchestrator(timeProvider);
        var metadata = CreateSampleMetadata();
        await orchestrator.TrackJobAsync(metadata, cancellationToken).ConfigureAwait(false);

        var events = new List<JobSessionEvent>();
        await using var watcher = orchestrator.WatchAsync(cancellationToken).GetAsyncEnumerator(cancellationToken);

        async Task CaptureAsync()
        {
            if (!await watcher.MoveNextAsync().ConfigureAwait(false))
            {
                throw new InvalidOperationException("Watcher completed unexpectedly while seeding fixture.");
            }

            events.Add(watcher.Current);
        }

        var session = await orchestrator.StartSessionAsync(
            metadata.JobId,
            new JobSessionStartRequest(
                Name: "Fixture lifecycle",
                ActiveOperators: new[] { "user:operator.maya", "user:operator.liam" },
                WorkOrderId: "work:fixture-1",
                Notes: "Lifecycle validation"),
            cancellationToken).ConfigureAwait(false);
        await CaptureAsync().ConfigureAwait(false);

        advanceTime?.Invoke(TimeSpan.FromMinutes(10));
        await orchestrator.PauseSessionAsync(metadata.JobId, session.SessionId, "Fixture pause", cancellationToken)
            .ConfigureAwait(false);
        await CaptureAsync().ConfigureAwait(false);

        advanceTime?.Invoke(TimeSpan.FromMinutes(5));
        await orchestrator.ResumeSessionAsync(metadata.JobId, session.SessionId, "Fixture resume", cancellationToken)
            .ConfigureAwait(false);
        await CaptureAsync().ConfigureAwait(false);

        advanceTime?.Invoke(TimeSpan.FromMinutes(20));
        await orchestrator.CompleteSessionAsync(metadata.JobId, session.SessionId, "Fixture complete", cancellationToken)
            .ConfigureAwait(false);
        await CaptureAsync().ConfigureAwait(false);

        await watcher.DisposeAsync().ConfigureAwait(false);

        var sessions = await orchestrator.ListSessionsAsync(metadata.JobId, cancellationToken).ConfigureAwait(false);

        return new JobTasksLifecycleFixture(
            orchestrator,
            metadata,
            session.SessionId,
            sessions,
            events);
    }

    private static JobMetadata CreateSampleMetadata()
    {
        return new JobMetadata(
            JobId: "job:fixture.north-40",
            Slug: "north-40-fixture",
            DisplayName: "North 40 Fixture",
            State: JobLifecycleState.Active,
            CreatedAt: FixtureCreatedAt,
            UpdatedAt: FixtureCreatedAt.AddMinutes(30),
            ActiveSessionId: "session:fixture-1",
            Context: new JobContext(
                FarmId: "farm:alpha",
                FieldIds: new[] { "field:north-40" },
                SeasonId: "season:2025",
                WorkOrderId: "work:fixture-1",
                Notes: "Fixture job used for regression tests."),
            Tags: new[] { "planting", "regression" });
    }

    private static JobStoreLayout CreateSampleLayout()
    {
        var jobRoot = Path.Combine(Path.GetTempPath(), "aog-fixtures", "north-40");
        return new JobStoreLayout(
            JobRoot: jobRoot,
            DataDirectory: Path.Combine(jobRoot, "data"),
            ResumeFile: Path.Combine(jobRoot, "Resume.txt"),
            AttachmentsDirectory: Path.Combine(jobRoot, "attachments"));
    }
}

/// <summary>
/// Result returned by <see cref="JobTasksFixtureCatalog.CreateLifecycleScenarioAsync"/> describing the seeded orchestrator.
/// </summary>
/// <param name="Orchestrator">Orchestrator seeded by the fixture. Caller is responsible for disposal.</param>
/// <param name="Job">Metadata describing the job used in the scenario.</param>
/// <param name="SessionId">Identifier of the session operated during the scenario.</param>
/// <param name="Sessions">Sessions tracked by the orchestrator after the scenario completes.</param>
/// <param name="Events">Lifecycle events emitted during the scenario.</param>
public sealed record JobTasksLifecycleFixture(
    JobSeasonSessionOrchestrator Orchestrator,
    JobMetadata Job,
    string SessionId,
    IReadOnlyList<JobSession> Sessions,
    IReadOnlyList<JobSessionEvent> Events);
