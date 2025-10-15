using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aog.Plugins.FieldHealth;
using FluentAssertions;
using Xunit;

namespace Aog.Plugins.Tests.FieldHealth;

public class FieldHealthIngestPipelineTests
{
    [Fact]
    public async Task IngestAsync_AddsObservationAndUpdatesStats()
    {
        var options = new FieldHealthIngestOptions { Kind = "risk.weeds" };
        var pipeline = new FieldHealthIngestPipeline(options);
        var observedAt = new DateTimeOffset(2025, 4, 2, 15, 45, 0, TimeSpan.Zero);
        var observation = CreateObservation("feature:risk-zone-01", FieldHealthSeverity.High, observedAt, areaHa: 1.2);

        var metadata = await pipeline.IngestAsync(observation);

        metadata.Observations.Should().ContainSingle().Which.Should().Be(observation);
        metadata.Statistics.TotalAreaHa.Should().BeApproximately(1.2, 1e-6);
        metadata.Statistics.SeverityCounts.High.Should().Be(1);
        metadata.Statistics.LastSurveyedAt.Should().Be(observedAt);
        metadata.Kind.Should().Be("risk.weeds");
    }

    [Fact]
    public async Task IngestAsync_ReplacesObservationWhenNewer()
    {
        var options = new FieldHealthIngestOptions { Kind = "risk.flood" };
        var pipeline = new FieldHealthIngestPipeline(options);
        var featureId = "feature:risk-zone-02";
        var initial = CreateObservation(featureId, FieldHealthSeverity.Moderate, new DateTimeOffset(2025, 4, 2, 15, 55, 0, TimeSpan.Zero));
        var updated = initial with
        {
            Severity = FieldHealthSeverity.Critical,
            Notes = "Escalated after follow-up visit",
            LastUpdatedAt = initial.ObservedAt.AddMinutes(30)
        };

        await pipeline.IngestAsync(initial);
        var metadata = await pipeline.IngestAsync(updated);

        metadata.Observations.Should().ContainSingle().Which.Should().Be(updated);
        metadata.Statistics.SeverityCounts.Critical.Should().Be(1);
        metadata.Statistics.SeverityCounts.Moderate.Should().Be(0);
        metadata.Statistics.LastSurveyedAt.Should().Be(initial.ObservedAt);
    }

    [Fact]
    public async Task Remove_RecomputesStatistics()
    {
        var options = new FieldHealthIngestOptions { Kind = "risk.compaction" };
        var pipeline = new FieldHealthIngestPipeline(options);
        var first = CreateObservation("feature:one", FieldHealthSeverity.Low, new DateTimeOffset(2025, 4, 2, 15, 0, 0, TimeSpan.Zero), areaHa: 0.5);
        var second = CreateObservation("feature:two", FieldHealthSeverity.Moderate, new DateTimeOffset(2025, 4, 2, 16, 0, 0, TimeSpan.Zero), areaHa: 1.1);

        await pipeline.IngestAsync(first);
        await pipeline.IngestAsync(second);
        pipeline.Remove(first.FeatureId).Should().BeTrue();

        pipeline.LatestMetadata.Observations.Should().ContainSingle().Which.Should().Be(second);
        pipeline.LatestMetadata.Statistics.TotalAreaHa.Should().BeApproximately(1.1, 1e-6);
        pipeline.LatestMetadata.Statistics.SeverityCounts.Moderate.Should().Be(1);
        pipeline.LatestMetadata.Statistics.LastSurveyedAt.Should().Be(second.ObservedAt);
    }

    [Fact]
    public void UpdateLayerContext_NormalizesTags()
    {
        var options = new FieldHealthIngestOptions
        {
            Kind = "risk.other",
            Tags = new[] { "priority", "SPRING" }
        };
        var pipeline = new FieldHealthIngestPipeline(options);

        var metadata = pipeline.UpdateLayerContext("Follow-up after rain", new[] { "spring", "priority", "drainage" }, "https://example.com/schema");

        metadata.Notes.Should().Be("Follow-up after rain");
        metadata.SchemaRef.Should().Be("https://example.com/schema");
        metadata.Tags.Should().Equal("spring", "priority", "drainage");
    }

    [Fact]
    public void CreateMetadataSnapshot_OrdersObservationsByTimestamp()
    {
        var options = new FieldHealthIngestOptions { Kind = "risk.other" };
        var pipeline = new FieldHealthIngestPipeline(options);
        var older = CreateObservation("feature:old", FieldHealthSeverity.Low, new DateTimeOffset(2025, 4, 1, 12, 0, 0, TimeSpan.Zero));
        var newer = CreateObservation("feature:new", FieldHealthSeverity.Moderate, new DateTimeOffset(2025, 4, 2, 12, 0, 0, TimeSpan.Zero));

        pipeline.IngestAsync(older).GetAwaiter().GetResult();
        pipeline.IngestAsync(newer).GetAwaiter().GetResult();

        pipeline.LatestMetadata.Observations.Select(o => o.FeatureId).Should().Equal("feature:new", "feature:old");
    }

    [Fact]
    public async Task IngestAsync_RecordsHistoryEntriesWhenStatusChanges()
    {
        var options = new FieldHealthIngestOptions { Kind = "risk.weeds" };
        var pipeline = new FieldHealthIngestPipeline(options);
        var observedAt = new DateTimeOffset(2025, 4, 2, 14, 0, 0, TimeSpan.Zero);
        var initial = CreateObservation("feature:history", FieldHealthSeverity.Moderate, observedAt);

        await pipeline.IngestAsync(initial);
        pipeline.LatestMetadata.History.Entries.Should().ContainSingle();
        pipeline.LatestMetadata.History.Entries[0].Status.Should().Be(FieldHealthObservationStatus.Active);

        var updated = initial with
        {
            Status = FieldHealthObservationStatus.Resolved,
            LastUpdatedAt = observedAt.AddHours(2)
        };

        await pipeline.IngestAsync(updated);

        pipeline.LatestMetadata.History.Entries.Should().HaveCount(2);
        pipeline.LatestMetadata.History.Entries[^1].Status.Should().Be(FieldHealthObservationStatus.Resolved);
        pipeline.LatestMetadata.History.Entries[^1].ChangedAt.Should().Be(updated.LastUpdatedAt);
    }

    [Fact]
    public void UpdateHistoryToggles_PersistsState()
    {
        var options = new FieldHealthIngestOptions { Kind = "risk.weeds" };
        var pipeline = new FieldHealthIngestPipeline(options);
        var toggles = new FieldHealthHistoryToggles(showActive: true, showMonitor: false, showResolved: true);

        var metadata = pipeline.UpdateHistoryToggles(toggles);

        metadata.History.Toggles.Should().Be(toggles);
        pipeline.LatestMetadata.History.Toggles.Should().Be(toggles);
    }

    [Fact]
    public async Task RegisterAnalyticsCallback_NotifiesOnMetadataChanges()
    {
        var options = new FieldHealthIngestOptions { Kind = "risk.weeds" };
        var pipeline = new FieldHealthIngestPipeline(options);
        var events = new List<(FieldHealthLayerMetadata Current, FieldHealthLayerMetadata? Previous)>();

        using (pipeline.RegisterAnalyticsCallback((current, previous) => events.Add((current, previous)), replayLatest: true))
        {
            events.Should().HaveCount(1);
            events[0].Previous.Should().BeNull();

            var observation = CreateObservation(
                "feature:callback",
                FieldHealthSeverity.Low,
                new DateTimeOffset(2025, 4, 3, 10, 0, 0, TimeSpan.Zero));

            await pipeline.IngestAsync(observation);

            events.Should().HaveCount(2);
            events[1].Previous.Should().NotBeNull();
            events[1].Current.Observations.Should().ContainSingle(o => o.FeatureId == "feature:callback");
        }
    }

    private static FieldHealthObservation CreateObservation(string featureId, FieldHealthSeverity severity, DateTimeOffset observedAt, double? areaHa = null)
    {
        return new FieldHealthObservation
        {
            FeatureId = featureId,
            Severity = severity,
            ObservedAt = observedAt,
            Observer = "user:operator.maya",
            SessionId = "session:scouting-2025-04-02",
            AreaHa = areaHa,
            GeometryHash = "abcdef12",
            LastUpdatedAt = observedAt,
            Status = FieldHealthObservationStatus.Active,
            Attachments = Array.Empty<string>()
        };
    }
}
