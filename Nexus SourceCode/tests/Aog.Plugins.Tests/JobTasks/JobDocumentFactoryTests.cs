using System;
using System.Collections.Generic;
using System.Text.Json;
using Aog.Core.Jobs;
using Aog.Plugins.JobTasks;
using FluentAssertions;
using Xunit;

namespace Aog.Plugins.Tests.JobTasks;

public sealed class JobDocumentFactoryTests
{
    [Fact]
    public void Create_ShouldProduceDeterministicDocument()
    {
        var snapshot = CreateSampleSnapshot();

        var document = JobDocumentFactory.Create(snapshot);

        document.SchemaVersion.Should().Be("1.0.0");
        document.JobId.Should().Be(snapshot.Metadata.JobId);
        document.DisplayName.Should().Be(snapshot.Metadata.DisplayName);
        document.Slug.Should().Be(snapshot.Metadata.Slug);
        document.State.Should().Be("mounted");
        document.ActiveSessionId.Should().Be(snapshot.Metadata.ActiveSessionId);
        document.Context.FarmId.Should().Be(snapshot.Metadata.Context.FarmId);
        document.Context.FieldIds.Should().Equal(snapshot.Metadata.Context.FieldIds);
        document.Paths.JobRoot.Should().Be(snapshot.Layout.JobRoot);
        document.Paths.ResumeFile.Should().Be(snapshot.Layout.ResumeFile);
        document.Tags.Should().Equal(snapshot.Metadata.Tags);
        document.Sessions.Should().HaveCount(snapshot.Sessions.Count);
        document.Sessions![0].Stats.Should().NotBeNull();
        document.Sessions![0].Stats!.AreaHectares.Should().BeApproximately(12.5, 1e-6);
        document.Equipment.Should().NotBeNull();
        document.Equipment!.VehicleId.Should().Be("equipment:tractor.alpha");
        document.Spatial.Should().NotBeNull();
        document.Spatial!.Home!.Latitude.Should().BeApproximately(40.5, 1e-6);
        document.Assets.Should().NotBeNull();
        document.Assets!.BoundaryLayers.Should().Contain("layer:boundary");
        document.Extensions.Should().NotBeNull();
        document.Extensions!["job.tasks"].GetProperty("priority").GetString().Should().Be("High");
    }

    [Fact]
    public void ToSnapshot_ShouldRoundTripDocument()
    {
        var snapshot = CreateSampleSnapshot();
        var document = JobDocumentFactory.Create(snapshot);

        var roundTrip = JobDocumentFactory.ToSnapshot(document);

        roundTrip.Should().BeEquivalentTo(
            snapshot,
            options => options
                .Using<JsonElement>(ctx => ctx.Subject.GetRawText().Should().Be(ctx.Expectation.GetRawText()))
                    .WhenTypeIs<JsonElement>()
                .WithStrictOrdering());
    }

    private static JobSnapshot CreateSampleSnapshot()
    {
        var metadata = new JobMetadata(
            "job:alpha",
            "alpha",
            "Alpha Planting",
            JobLifecycleState.Mounted,
            DateTimeOffset.Parse("2025-05-05T07:00:00Z"),
            DateTimeOffset.Parse("2025-05-05T08:30:00Z"),
            "session:1",
            new JobContext(
                "farm:alpha",
                new[] { "field:north-40", "field:east-40" },
                "season:2025",
                "work:order-123",
                "Morning planting"),
            new[] { "corn", "planter" });

        var layout = new JobStoreLayout(
            "/jobs/alpha",
            "/jobs/alpha/data",
            "/jobs/alpha/Resume.txt",
            "/jobs/alpha/attachments");

        using var sessionExtensionDocument = JsonDocument.Parse("{\"value\":\"Seed check\"}");
        var sessionExtensions = new Dictionary<string, JsonElement>
        {
            ["notes"] = sessionExtensionDocument.RootElement.Clone()
        };

        var sessions = new[]
        {
            new JobSessionSnapshot(
                "session:1",
                JobSessionState.Active,
                DateTimeOffset.Parse("2025-05-05T07:05:00Z"),
                DateTimeOffset.Parse("2025-05-05T08:00:00Z"),
                "Morning pass",
                null,
                new[] { "user:operator.maya" },
                new JobSessionStatisticsSnapshot(AreaHectares: 12.5, DistanceKilometers: 8.1, DurationSeconds: 3600, CoveragePercent: 62.4),
                sessionExtensions)
        };

        var spatial = new JobSpatialSnapshot(
            new JobLatLonSnapshot(40.5, -96.5, 320),
            new JobEnvelopeSnapshot(40.45, -96.55, 40.55, -96.45),
            "boundary:home",
            new[] { "boundary:headland" },
            "EPSG:4326");

        var assets = new JobAssetSnapshot(
            BoundaryLayers: new[] { "layer:boundary" },
            CoverageLayers: new[] { "layer:coverage" },
            GuidanceSets: new[] { "guidance:ab-line" },
            Prescriptions: new[] { "layer:vrx" },
            Attachments: new[] { new JobAttachmentSnapshot("attach:1", "attachments/notes.txt", "text/plain", 128) });

        var stats = new JobStatisticsSnapshot(
            TotalAreaHectares: 12.5,
            TotalDistanceKilometers: 8.1,
            ActiveDurationSeconds: 5400,
            CompletedSessionCount: 3);

        using var jobExtensionDocument = JsonDocument.Parse("{\"priority\":\"High\",\"tasksCompleted\":2}");
        var extensions = new Dictionary<string, JsonElement>
        {
            ["job.tasks"] = jobExtensionDocument.RootElement.Clone()
        };

        var equipment = new JobEquipmentSnapshot(
            VehicleId: "equipment:tractor.alpha",
            ImplementId: "equipment:planter",
            PresetId: "preset:planter-16r",
            LayoutId: "layout:planter-dashboard");

        return new JobSnapshot(metadata, layout, sessions, spatial, assets, stats, extensions, equipment);
    }
}
