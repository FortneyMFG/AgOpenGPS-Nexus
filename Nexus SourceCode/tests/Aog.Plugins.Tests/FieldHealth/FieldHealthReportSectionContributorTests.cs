using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aog.Core.Reporting;
using Aog.Plugins.FieldHealth;
using FluentAssertions;
using Xunit;

namespace Aog.Plugins.Tests.FieldHealth;

public sealed class FieldHealthReportSectionContributorTests
{
    private static readonly ReportTemplate Template = new(
        "template:field-health",
        "Field Health",
        "1.0.0",
        ReportScopeKind.Job,
        new[] { new ReportTemplateSection("fieldHealth.summary") },
        new[] { new ReportTemplateOutput("pdf", "PDF") });

    private static readonly ReportScope Scope = new(ReportScopeKind.Job, "job-123");

    [Fact]
    public async Task PrepareAsync_WhenSnapshotMissing_ReturnsNotReady()
    {
        var dataSource = new FakeDataSource();
        var contributor = new FieldHealthReportSectionContributor(dataSource);
        var context = new ReportGenerationContext(Template, Scope, new ReportGenerationOptions());

        var result = await contributor.PrepareAsync(context, CancellationToken.None);

        result.IsReady.Should().BeFalse();
        result.Reason.IndexOf("Field health observations", StringComparison.OrdinalIgnoreCase).Should().BeGreaterThanOrEqualTo(0);
        result.Diagnostics.Should().ContainKey("reason").WhoseValue.Should().Be("no-data");
    }

    [Fact]
    public async Task RenderAsync_WhenDataAvailable_ReturnsPayload()
    {
        var snapshot = CreateSnapshot();
        var dataSource = new FakeDataSource { Snapshot = snapshot };
        var contributor = new FieldHealthReportSectionContributor(dataSource, new FieldHealthReportSectionOptions(highlightObservationCount: 1));
        var generation = new ReportGenerationContext(Template, Scope, new ReportGenerationOptions());
        var section = Template.Sections[0];
        var sectionContext = new ReportSectionContext(generation, section);

        var prepare = await contributor.PrepareAsync(generation, CancellationToken.None);
        prepare.IsReady.Should().BeTrue();

        var result = await contributor.RenderAsync(sectionContext, CancellationToken.None);

        result.Status.Should().Be(ReportSectionStatus.Success);
        result.Message.IndexOf("summary", StringComparison.OrdinalIgnoreCase).Should().BeGreaterThanOrEqualTo(0);
        result.Payload.Should().BeOfType<FieldHealthReportPayload>();

        var payload = (FieldHealthReportPayload)result.Payload!;
        payload.ScopeIdentifier.Should().Be(Scope.Identifier);
        payload.Layers.Should().HaveCount(2);
        payload.TotalAreaHectares.Should().BeApproximately(6.5, 1e-6);
        payload.SeverityTotals.Should().Be(new FieldHealthSeverityCounts(0, 2, 1, 1, 1));
        payload.LastSurveyedAt.Should().Be(snapshot.Layers[1].Statistics.LastSurveyedAt);

        var firstLayer = payload.Layers[0];
        firstLayer.Kind.Should().Be("risk.compaction");
        firstLayer.ObservationCount.Should().Be(2);
        firstLayer.Highlights.Should().HaveCount(1);
        firstLayer.Highlights[0].Severity.Should().Be(FieldHealthSeverity.High);
    }

    [Fact]
    public async Task RenderAsync_ReusesSnapshotFromPrepare()
    {
        var snapshot = CreateSnapshot();
        var dataSource = new FakeDataSource { Snapshot = snapshot };
        var contributor = new FieldHealthReportSectionContributor(dataSource);
        var generation = new ReportGenerationContext(Template, Scope, new ReportGenerationOptions());
        var section = Template.Sections[0];
        var sectionContext = new ReportSectionContext(generation, section);

        await contributor.PrepareAsync(generation, CancellationToken.None);
        dataSource.CallCount.Should().Be(1);

        await contributor.RenderAsync(sectionContext, CancellationToken.None);

        dataSource.CallCount.Should().Be(1);
    }

    private static FieldHealthReportSnapshot CreateSnapshot()
    {
        var firstLayer = CreateLayer(
            "risk.compaction",
            new[]
            {
                CreateObservation("feature:one", FieldHealthSeverity.High, new DateTimeOffset(2025, 4, 2, 10, 0, 0, TimeSpan.Zero), FieldHealthObservationStatus.Active, 2.5),
                CreateObservation("feature:two", FieldHealthSeverity.Low, new DateTimeOffset(2025, 4, 1, 9, 0, 0, TimeSpan.Zero), FieldHealthObservationStatus.Monitor, 1.0),
            },
            new FieldHealthLayerStatistics(3.5, new FieldHealthSeverityCounts(0, 1, 0, 1, 0), new DateTimeOffset(2025, 4, 2, 10, 0, 0, TimeSpan.Zero)));

        var secondLayer = CreateLayer(
            "risk.weeds",
            new[]
            {
                CreateObservation("feature:three", FieldHealthSeverity.Critical, new DateTimeOffset(2025, 4, 3, 12, 30, 0, TimeSpan.Zero), FieldHealthObservationStatus.Active, 1.5),
                CreateObservation("feature:four", FieldHealthSeverity.Low, new DateTimeOffset(2025, 4, 2, 12, 0, 0, TimeSpan.Zero), FieldHealthObservationStatus.Resolved, 0.5),
                CreateObservation("feature:five", FieldHealthSeverity.Moderate, new DateTimeOffset(2025, 4, 1, 8, 30, 0, TimeSpan.Zero), FieldHealthObservationStatus.Active, 1.0),
            },
            new FieldHealthLayerStatistics(3.0, new FieldHealthSeverityCounts(0, 1, 1, 0, 1), new DateTimeOffset(2025, 4, 3, 12, 30, 0, TimeSpan.Zero)));

        return new FieldHealthReportSnapshot(new[] { firstLayer, secondLayer }, scopeDisplayName: "North Field");
    }

    private static FieldHealthLayerMetadata CreateLayer(
        string kind,
        IReadOnlyList<FieldHealthObservation> observations,
        FieldHealthLayerStatistics statistics)
    {
        return new FieldHealthLayerMetadata(
            kind,
            schemaRef: "https://agopengps.org/schemas/FieldHealthRiskLayer.v1.json",
            notes: "Scouting notes",
            tags: new[] { "priority.high" },
            observations,
            statistics,
            new FieldHealthLayerHistory(Array.Empty<FieldHealthHistoryEntry>(), FieldHealthHistoryToggles.Default));
    }

    private static FieldHealthObservation CreateObservation(
        string featureId,
        FieldHealthSeverity severity,
        DateTimeOffset observedAt,
        FieldHealthObservationStatus status,
        double areaHa)
    {
        return new FieldHealthObservation
        {
            FeatureId = featureId,
            Severity = severity,
            ObservedAt = observedAt,
            Observer = "user:scout",
            Status = status,
            AreaHa = areaHa,
            SessionId = "session:test",
        };
    }

    private sealed class FakeDataSource : IFieldHealthReportDataSource
    {
        public FieldHealthReportSnapshot? Snapshot { get; set; }

        public int CallCount { get; private set; }

        public ValueTask<FieldHealthReportSnapshot?> GetSnapshotAsync(ReportScope scope, CancellationToken cancellationToken)
        {
            CallCount++;
            return ValueTask.FromResult(Snapshot);
        }
    }
}
