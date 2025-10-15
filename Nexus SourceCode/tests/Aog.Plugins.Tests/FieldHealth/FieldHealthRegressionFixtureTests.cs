using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Aog.Plugins.FieldHealth;
using FluentAssertions;
using Xunit;

namespace Aog.Plugins.Tests.FieldHealth;

public sealed class FieldHealthRegressionFixtureTests
{
    private static readonly string FixturePath = Path.Combine("FieldHealth", "Data", "FieldHealthRegressionFixture.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
        }
    };

    [Fact]
    public void FieldHealthPipeline_MatchesRegressionFixture()
    {
        var fullPath = Path.Combine(AppContext.BaseDirectory, FixturePath);
        File.Exists(fullPath).Should().BeTrue($"Fixture '{FixturePath}' should be copied to the test output directory.");

        var json = File.ReadAllText(fullPath);
        var document = JsonSerializer.Deserialize<FixtureDocument>(json, JsonOptions);
        document.Should().NotBeNull();
        document!.Scenarios.Should().NotBeNull();
        document.Scenarios.Should().NotBeEmpty();

        foreach (var scenario in document.Scenarios)
        {
            ValidateScenario(scenario);
        }
    }

    private static void ValidateScenario(FixtureScenario scenario)
    {
        scenario.Should().NotBeNull();
        scenario.Expected.Should().NotBeNull();
        scenario.Expected.Metadata.Should().NotBeNull();

        var pipeline = new FieldHealthIngestPipeline(scenario.Options.ToOptions());
        foreach (var operation in scenario.Operations)
        {
            operation.Apply(pipeline);
        }

        var metadata = pipeline.LatestMetadata;
        var expected = scenario.Expected.Metadata!;

        metadata.Kind.Should().Be(expected.Kind, scenario.Name);
        metadata.SchemaRef.Should().Be(expected.SchemaRef, scenario.Name);
        metadata.Notes.Should().Be(expected.Notes, scenario.Name);
        metadata.Tags.Should().Equal(expected.Tags ?? Array.Empty<string>(), scenario.Name);

        metadata.Observations.Should().HaveCount(expected.Observations.Length, scenario.Name);
        for (var i = 0; i < expected.Observations.Length; i++)
        {
            var actual = metadata.Observations[i];
            var expectedObservation = expected.Observations[i];

            actual.FeatureId.Should().Be(expectedObservation.FeatureId, scenario.Name);
            actual.ZoneId.Should().Be(expectedObservation.ZoneId, scenario.Name);
            actual.Label.Should().Be(expectedObservation.Label, scenario.Name);
            actual.Severity.Should().Be(expectedObservation.Severity, scenario.Name);
            actual.ObservedAt.Should().Be(expectedObservation.ObservedAt, scenario.Name);
            actual.Observer.Should().Be(expectedObservation.Observer, scenario.Name);
            actual.Notes.Should().Be(expectedObservation.Notes, scenario.Name);
            actual.Attachments.Should().Equal(expectedObservation.Attachments ?? Array.Empty<string>(), scenario.Name);
            actual.SessionId.Should().Be(expectedObservation.SessionId, scenario.Name);
            if (expectedObservation.AreaHa is null)
            {
                actual.AreaHa.Should().BeNull(scenario.Name);
            }
            else
            {
                actual.AreaHa.Should().NotBeNull(scenario.Name);
                actual.AreaHa!.Value.Should().BeApproximately(expectedObservation.AreaHa.Value, 1e-6, scenario.Name);
            }

            actual.GeometryHash.Should().Be(expectedObservation.GeometryHash, scenario.Name);
            actual.LastUpdatedAt.Should().Be(expectedObservation.LastUpdatedAt, scenario.Name);
            actual.Status.Should().Be(expectedObservation.Status, scenario.Name);
        }

        var statistics = metadata.Statistics;
        statistics.TotalAreaHa.Should().BeApproximately(expected.Statistics.TotalAreaHa, 1e-6, scenario.Name);
        statistics.SeverityCounts.Should().Be(new FieldHealthSeverityCounts(
            expected.Statistics.SeverityCounts.None,
            expected.Statistics.SeverityCounts.Low,
            expected.Statistics.SeverityCounts.Moderate,
            expected.Statistics.SeverityCounts.High,
            expected.Statistics.SeverityCounts.Critical), scenario.Name);
        statistics.LastSurveyedAt.Should().Be(expected.Statistics.LastSurveyedAt, scenario.Name);

        var history = metadata.History;
        history.Entries.Should().HaveCount(expected.History.Entries.Length, scenario.Name);
        for (var i = 0; i < expected.History.Entries.Length; i++)
        {
            var actualEntry = history.Entries[i];
            var expectedEntry = expected.History.Entries[i];

            actualEntry.FeatureId.Should().Be(expectedEntry.FeatureId, scenario.Name);
            actualEntry.Status.Should().Be(expectedEntry.Status, scenario.Name);
            actualEntry.Severity.Should().Be(expectedEntry.Severity, scenario.Name);
            actualEntry.ChangedAt.Should().Be(expectedEntry.ChangedAt, scenario.Name);
        }

        history.Toggles.ShowActive.Should().Be(expected.History.Toggles.ShowActive, scenario.Name);
        history.Toggles.ShowMonitor.Should().Be(expected.History.Toggles.ShowMonitor, scenario.Name);
        history.Toggles.ShowResolved.Should().Be(expected.History.Toggles.ShowResolved, scenario.Name);
    }

    private sealed record FixtureDocument(FixtureScenario[] Scenarios);

    private sealed record FixtureScenario(
        string Name,
        PipelineOptionsModel Options,
        OperationModel[] Operations,
        ExpectedModel Expected);

    private sealed record PipelineOptionsModel(
        string Kind,
        string? SchemaRef,
        string? Notes,
        string[]? Tags)
    {
        public FieldHealthIngestOptions ToOptions()
        {
            return new FieldHealthIngestOptions
            {
                Kind = Kind,
                SchemaRef = SchemaRef,
                Notes = Notes,
                Tags = Tags ?? Array.Empty<string>()
            };
        }
    }

    private sealed record OperationModel(
        FieldHealthObservationModel? Ingest,
        UpdateLayerContextModel? UpdateLayerContext,
        HistoryToggleModel? UpdateHistoryToggles)
    {
        public void Apply(FieldHealthIngestPipeline pipeline)
        {
            if (Ingest is not null)
            {
                pipeline.IngestAsync(Ingest.ToObservation()).GetAwaiter().GetResult();
                return;
            }

            if (UpdateLayerContext is not null)
            {
                pipeline.UpdateLayerContext(UpdateLayerContext.Notes, UpdateLayerContext.Tags, UpdateLayerContext.SchemaRef);
                return;
            }

            if (UpdateHistoryToggles is not null)
            {
                pipeline.UpdateHistoryToggles(UpdateHistoryToggles.ToToggles());
                return;
            }

            throw new InvalidOperationException("Fixture operation did not specify an action.");
        }
    }

    private sealed record FieldHealthObservationModel(
        string FeatureId,
        Guid? ZoneId,
        string? Label,
        FieldHealthSeverity Severity,
        DateTimeOffset ObservedAt,
        string Observer,
        string? Notes,
        string[]? Attachments,
        string? SessionId,
        double? AreaHa,
        string? GeometryHash,
        DateTimeOffset? LastUpdatedAt,
        FieldHealthObservationStatus? Status)
    {
        public FieldHealthObservation ToObservation()
        {
            return new FieldHealthObservation
            {
                FeatureId = FeatureId,
                ZoneId = ZoneId,
                Label = Label,
                Severity = Severity,
                ObservedAt = ObservedAt,
                Observer = Observer,
                Notes = Notes,
                Attachments = Attachments ?? Array.Empty<string>(),
                SessionId = SessionId,
                AreaHa = AreaHa,
                GeometryHash = GeometryHash,
                LastUpdatedAt = LastUpdatedAt,
                Status = Status
            };
        }
    }

    private sealed record UpdateLayerContextModel(string? Notes, string[]? Tags, string? SchemaRef);

    private sealed record HistoryToggleModel(bool ShowActive, bool ShowMonitor, bool ShowResolved)
    {
        public FieldHealthHistoryToggles ToToggles() => new(ShowActive, ShowMonitor, ShowResolved);
    }

    private sealed record ExpectedModel(ExpectedMetadataModel? Metadata);

    private sealed record ExpectedMetadataModel(
        string Kind,
        string? SchemaRef,
        string? Notes,
        string[]? Tags,
        ExpectedObservationModel[] Observations,
        ExpectedStatisticsModel Statistics,
        ExpectedHistoryModel History);

    private sealed record ExpectedObservationModel(
        string FeatureId,
        Guid? ZoneId,
        string? Label,
        FieldHealthSeverity Severity,
        DateTimeOffset ObservedAt,
        string Observer,
        string? Notes,
        string[]? Attachments,
        string? SessionId,
        double? AreaHa,
        string? GeometryHash,
        DateTimeOffset? LastUpdatedAt,
        FieldHealthObservationStatus? Status);

    private sealed record ExpectedStatisticsModel(
        double TotalAreaHa,
        ExpectedSeverityCountsModel SeverityCounts,
        DateTimeOffset? LastSurveyedAt);

    private sealed record ExpectedSeverityCountsModel(int None, int Low, int Moderate, int High, int Critical);

    private sealed record ExpectedHistoryModel(ExpectedHistoryEntryModel[] Entries, ExpectedTogglesModel Toggles);

    private sealed record ExpectedHistoryEntryModel(
        string FeatureId,
        FieldHealthObservationStatus Status,
        FieldHealthSeverity Severity,
        DateTimeOffset ChangedAt);

    private sealed record ExpectedTogglesModel(bool ShowActive, bool ShowMonitor, bool ShowResolved);
}
