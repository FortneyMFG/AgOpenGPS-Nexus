using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Aog.Core.Layers;
using Aog.Plugins.Crop;
using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Aog.Plugins.Tests.Crop;

public sealed class CropAnalyticsServiceTests
{
    private static readonly LayerEditEventContext DefaultContext = new(
        "farm:demo",
        new[] { "field:alpha" },
        "season:2025");

    [Fact]
    public async Task CreateSnapshot_AggregatesLayerSummaries()
    {
        var time = new FakeTimeProvider(new DateTimeOffset(2025, 4, 1, 7, 0, 0, TimeSpan.Zero));
        var journal = new LayerEditEventJournalService(time);
        var plannedPipeline = new CropLayerIngestionPipeline("layer:cropType.planned.2025");
        var actualPipeline = new CropLayerIngestionPipeline("layer:cropType.actual.2025");
        var historyPipeline = new CropLayerIngestionPipeline("layer:cropType.history.2024");

        await plannedPipeline.ApplyAsync(await AppendAsync(
            journal,
            time,
            layerId: "layer:cropType.planned.2025",
            jobId: "job:plan",
            fieldIds: new[] { "field:alpha" },
            type: "create",
            featureId: "feature:planned-1",
            attributes: JsonNode.Parse("""{ "crop": "Corn", "status": "planned", "year": 2025 }""")!,
            geometry: CreatePolygonGeometry(),
            areaDelta: 1200));

        await plannedPipeline.ApplyAsync(await AppendAsync(
            journal,
            time,
            layerId: "layer:cropType.planned.2025",
            jobId: "job:plan",
            fieldIds: new[] { "field:beta" },
            type: "create",
            featureId: "feature:planned-2",
            attributes: JsonNode.Parse("""{ "crop": "Soybean", "status": "planned", "year": 2025 }""")!,
            geometry: CreatePolygonGeometry(1),
            areaDelta: 800));

        await actualPipeline.ApplyAsync(await AppendAsync(
            journal,
            time,
            layerId: "layer:cropType.actual.2025",
            jobId: "job:plant",
            fieldIds: new[] { "field:alpha" },
            type: "create",
            featureId: "feature:actual-1",
            attributes: JsonNode.Parse("""{ "crop": "Corn", "status": "actual", "year": 2025, "source": "sensor" }""")!,
            geometry: CreatePolygonGeometry(),
            areaDelta: 1100));

        await historyPipeline.ApplyAsync(await AppendAsync(
            journal,
            time,
            layerId: "layer:cropType.history.2024",
            jobId: "job:archive",
            fieldIds: new[] { "field:alpha" },
            type: "create",
            featureId: "feature:history-1",
            attributes: JsonNode.Parse("""{ "crop": "Soybean", "status": "historical", "year": 2024 }""")!,
            geometry: CreatePolygonGeometry(),
            areaDelta: 1150));

        var service = new CropAnalyticsService(time);
        var snapshot = service.CreateSnapshot(
            plannedLayers: new[] { plannedPipeline.CreateSnapshot() },
            actualLayers: new[] { actualPipeline.CreateSnapshot() },
            historicalLayers: new[] { historyPipeline.CreateSnapshot() });

        snapshot.GeneratedAt.Should().Be(time.GetUtcNow());

        snapshot.Planned.Should().ContainEquivalentOf(new CropAcreageSummary("Corn", "planned", 2025, 0.12, 1));
        snapshot.Planned.Should().ContainEquivalentOf(new CropAcreageSummary("Soybean", "planned", 2025, 0.08, 1));

        snapshot.Actual.Should().ContainSingle(summary => summary.Crop == "Corn" && summary.Status == "actual");
        snapshot.Historical.Should().ContainSingle(summary => summary.Crop == "Soybean" && summary.Status == "historical");

        snapshot.Rotations.Should().ContainSingle(rotation =>
            rotation.Crop.Equals("Soybean", StringComparison.OrdinalIgnoreCase)
            && rotation.Occurrences == 1
            && rotation.DistinctFieldCount == 1
            && rotation.LatestYear == 2024);
    }

    [Fact]
    public async Task GetPreviousCropForField_ReturnsMostRecentRecord()
    {
        var time = new FakeTimeProvider(new DateTimeOffset(2025, 5, 1, 7, 0, 0, TimeSpan.Zero));
        var journal = new LayerEditEventJournalService(time);
        var historyPipeline = new CropLayerIngestionPipeline("layer:cropType.history.2024");

        await historyPipeline.ApplyAsync(await AppendAsync(
            journal,
            time,
            layerId: "layer:cropType.history.2024",
            jobId: "job:archive",
            fieldIds: new[] { "field:alpha" },
            type: "create",
            featureId: "feature:history-1",
            attributes: JsonNode.Parse("""{ "crop": "Corn", "status": "historical", "year": 2023 }""")!,
            geometry: CreatePolygonGeometry(),
            areaDelta: 1000));

        time.Advance(TimeSpan.FromHours(1));

        await historyPipeline.ApplyAsync(await AppendAsync(
            journal,
            time,
            layerId: "layer:cropType.history.2024",
            jobId: "job:archive",
            fieldIds: new[] { "field:alpha" },
            type: "create",
            featureId: "feature:history-2",
            attributes: JsonNode.Parse("""{ "crop": "Soybean", "status": "historical", "year": 2024 }""")!,
            geometry: CreatePolygonGeometry(1),
            areaDelta: 900));

        var service = new CropAnalyticsService(time);
        var snapshot = service.CreateSnapshot(
            plannedLayers: Array.Empty<CropLayerSnapshot>(),
            actualLayers: Array.Empty<CropLayerSnapshot>(),
            historicalLayers: new[] { historyPipeline.CreateSnapshot() });

        var record = service.GetPreviousCropForField(snapshot, "field:alpha");
        record.Should().NotBeNull();
        record!.Crop.Should().Be("Soybean");
        record.Year.Should().Be(2024);
        record.UpdatedAt.Should().Be(time.GetUtcNow());
    }

    [Fact]
    public async Task GetPreviousCropForGeometry_ReturnsMatchingRecord()
    {
        var time = new FakeTimeProvider(new DateTimeOffset(2025, 6, 1, 7, 0, 0, TimeSpan.Zero));
        var journal = new LayerEditEventJournalService(time);
        var historyPipeline = new CropLayerIngestionPipeline("layer:cropType.history.2024");

        var geometry = CreatePolygonGeometry();
        await historyPipeline.ApplyAsync(await AppendAsync(
            journal,
            time,
            layerId: "layer:cropType.history.2024",
            jobId: "job:archive",
            fieldIds: new[] { "field:alpha" },
            type: "create",
            featureId: "feature:history-1",
            attributes: JsonNode.Parse("""{ "crop": "Canola", "status": "historical", "year": 2022 }""")!,
            geometry: geometry,
            areaDelta: 700));

        var service = new CropAnalyticsService(time);
        var snapshot = service.CreateSnapshot(
            plannedLayers: null,
            actualLayers: null,
            historicalLayers: new[] { historyPipeline.CreateSnapshot() });

        var record = service.GetPreviousCropForGeometry(snapshot, geometry);
        record.Should().NotBeNull();
        record!.FeatureId.Should().Be("feature:history-1");
        record.Crop.Should().Be("Canola");

        var missing = service.GetPreviousCropForGeometry(snapshot, CreatePolygonGeometry(5));
        missing.Should().BeNull();
    }

    private static async Task<LayerEditEventEntry> AppendAsync(
        LayerEditEventJournalService journal,
        FakeTimeProvider time,
        string layerId,
        string jobId,
        IReadOnlyList<string> fieldIds,
        string type,
        string featureId,
        JsonNode? attributes,
        JsonNode? geometry,
        double? areaDelta)
    {
        var context = new LayerEditEventContext(DefaultContext.FarmId, fieldIds.ToArray(), DefaultContext.SeasonId);
        var summary = areaDelta.HasValue ? new LayerEditEventOperationSummary(areaDelta, null, Array.Empty<string>()) : null;
        var operation = new LayerEditEventOperation(
            type,
            featureId,
            geometryAfter: geometry,
            attributesAfter: attributes,
            summary: summary);

        var request = new LayerEditEventAppendRequest(layerId, jobId, context, new[] { operation }, "user:tester", "polygon")
        {
            CreatedAt = time.GetUtcNow(),
        };

        return await journal.AppendAsync(request);
    }

    private static JsonNode CreatePolygonGeometry(double offset = 0)
    {
        var ring = new JsonArray(
            new JsonArray(JsonValue.Create(offset), JsonValue.Create(0d)),
            new JsonArray(JsonValue.Create(1 + offset), JsonValue.Create(0d)),
            new JsonArray(JsonValue.Create(1 + offset), JsonValue.Create(1 + offset)),
            new JsonArray(JsonValue.Create(offset), JsonValue.Create(1 + offset)),
            new JsonArray(JsonValue.Create(offset), JsonValue.Create(0d)));

        var geometry = new JsonObject
        {
            ["type"] = "Polygon",
            ["coordinates"] = new JsonArray(ring)
        };

        return geometry;
    }
}
