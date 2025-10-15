using System;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Aog.Core.Layers;
using Aog.Plugins.Crop;
using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Aog.Plugins.Tests.Crop;

public sealed class CropLayerIngestionPipelineTests
{
    private static readonly LayerEditEventContext DefaultContext = new(
        "farm:demo",
        new[] { "field:demo" },
        "season:2025");

    [Fact]
    public async Task ApplyAsync_Create_AddsFeature()
    {
        var time = new FakeTimeProvider(new DateTimeOffset(2025, 4, 1, 7, 0, 0, TimeSpan.Zero));
        var journal = new LayerEditEventJournalService(time);
        var pipeline = new CropLayerIngestionPipeline("layer:cropType.planned.2025");

        var entry = await AppendAsync(
            journal,
            time,
            layerId: "layer:cropType.planned.2025",
            jobId: "job:plan-2025",
            sessionId: "session:alpha",
            type: "create",
            featureId: "feature:zone-1",
            attributes: JsonNode.Parse("""{ "crop": "Corn", "year": 2025, "status": "planned", "variety": "pioneer-123" }""")!,
            geometry: CreatePolygonGeometry(),
            areaDelta: 1500);

        await pipeline.ApplyAsync(entry);

        var snapshot = pipeline.CreateSnapshot();
        snapshot.LayerId.Should().Be("layer:cropType.planned.2025");
        snapshot.JobId.Should().Be("job:plan-2025");
        snapshot.SessionId.Should().Be("session:alpha");
        snapshot.IsEmpty.Should().BeFalse();
        snapshot.Features.Should().ContainSingle();

        var feature = snapshot.Features.Single();
        feature.FeatureId.Should().Be("feature:zone-1");
        feature.Crop.Should().Be("Corn");
        feature.Status.Should().Be("planned");
        feature.Year.Should().Be(2025);
        feature.Variety.Should().Be("pioneer-123");
        feature.AreaSqMeters.Should().Be(1500);
        feature.Geometry.Should().NotBeNull();
        feature.UpdatedAt.Should().Be(entry.CreatedAt);
        feature.UpdatedBy.Should().Be(entry.Actor);
    }

    [Fact]
    public async Task ApplyAsync_Update_ModifiesAttributesAndArea()
    {
        var time = new FakeTimeProvider(new DateTimeOffset(2025, 4, 1, 7, 0, 0, TimeSpan.Zero));
        var journal = new LayerEditEventJournalService(time);
        var pipeline = new CropLayerIngestionPipeline("layer:cropType.actual.2025");

        var create = await AppendAsync(
            journal,
            time,
            layerId: "layer:cropType.actual.2025",
            jobId: "job:plant-2025",
            sessionId: "session:bravo",
            type: "create",
            featureId: "feature:zone-2",
            attributes: JsonNode.Parse("""{ "crop": "Corn", "year": 2025, "status": "actual", "source": "sensor" }""")!,
            geometry: CreatePolygonGeometry(),
            areaDelta: 1500);

        await pipeline.ApplyAsync(create);
        time.Advance(TimeSpan.FromMinutes(5));

        var update = await AppendAsync(
            journal,
            time,
            layerId: "layer:cropType.actual.2025",
            jobId: "job:plant-2025",
            sessionId: "session:bravo",
            type: "update",
            featureId: "feature:zone-2",
            attributes: JsonNode.Parse("""{ "crop": "Soybean", "notes": "Replanted" }""")!,
            geometry: CreatePolygonGeometry(1),
            areaDelta: -200);

        await pipeline.ApplyAsync(update);

        var snapshot = pipeline.CreateSnapshot();
        snapshot.Features.Should().ContainSingle();
        var feature = snapshot.Features.Single();
        feature.Crop.Should().Be("Soybean");
        feature.Status.Should().Be("actual");
        feature.Source.Should().Be("sensor");
        feature.Notes.Should().Be("Replanted");
        feature.AreaSqMeters.Should().Be(1300);
        feature.UpdatedAt.Should().Be(update.CreatedAt);
        feature.Geometry.Should().NotBeNull();
    }

    [Fact]
    public async Task ApplyAsync_Delete_RemovesFeature()
    {
        var time = new FakeTimeProvider(new DateTimeOffset(2025, 4, 1, 7, 0, 0, TimeSpan.Zero));
        var journal = new LayerEditEventJournalService(time);
        var pipeline = new CropLayerIngestionPipeline("layer:cropType.planned.2025");

        var create = await AppendAsync(
            journal,
            time,
            layerId: "layer:cropType.planned.2025",
            jobId: "job:plan-2025",
            sessionId: "session:charlie",
            type: "create",
            featureId: "feature:zone-3",
            attributes: JsonNode.Parse("""{ "crop": "Canola", "year": 2025, "status": "planned" }""")!,
            geometry: CreatePolygonGeometry(),
            areaDelta: 900);

        await pipeline.ApplyAsync(create);
        time.Advance(TimeSpan.FromMinutes(1));

        var delete = await AppendAsync(
            journal,
            time,
            layerId: "layer:cropType.planned.2025",
            jobId: "job:plan-2025",
            sessionId: "session:charlie",
            type: "delete",
            featureId: "feature:zone-3",
            attributes: null,
            geometry: null,
            areaDelta: null);

        await pipeline.ApplyAsync(delete);

        var snapshot = pipeline.CreateSnapshot();
        snapshot.IsEmpty.Should().BeTrue();
        snapshot.Features.Should().BeEmpty();
    }

    [Fact]
    public async Task ApplyAsync_ThrowsWhenStatusMismatchedWithLayer()
    {
        var time = new FakeTimeProvider(new DateTimeOffset(2025, 4, 1, 7, 0, 0, TimeSpan.Zero));
        var journal = new LayerEditEventJournalService(time);
        var pipeline = new CropLayerIngestionPipeline("layer:cropType.actual.2025");

        var entry = await AppendAsync(
            journal,
            time,
            layerId: "layer:cropType.actual.2025",
            jobId: "job:plant-2025",
            sessionId: "session:delta",
            type: "create",
            featureId: "feature:zone-4",
            attributes: JsonNode.Parse("""{ "crop": "Corn", "year": 2025, "status": "planned" }""")!,
            geometry: CreatePolygonGeometry(),
            areaDelta: 500);

        Func<Task> act = () => pipeline.ApplyAsync(entry).AsTask();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*expects status 'actual'*");
    }

    [Fact]
    public async Task ApplyAsync_ThrowsWhenJobChanges()
    {
        var time = new FakeTimeProvider(new DateTimeOffset(2025, 4, 1, 7, 0, 0, TimeSpan.Zero));
        var journal = new LayerEditEventJournalService(time);
        var pipeline = new CropLayerIngestionPipeline("layer:cropType.planned.2025");

        var first = await AppendAsync(
            journal,
            time,
            layerId: "layer:cropType.planned.2025",
            jobId: "job:plan-2025",
            sessionId: "session:echo",
            type: "create",
            featureId: "feature:zone-5",
            attributes: JsonNode.Parse("""{ "crop": "Corn", "year": 2025, "status": "planned" }""")!,
            geometry: CreatePolygonGeometry(),
            areaDelta: 400);

        await pipeline.ApplyAsync(first);
        time.Advance(TimeSpan.FromMinutes(1));

        var second = await AppendAsync(
            journal,
            time,
            layerId: "layer:cropType.planned.2025",
            jobId: "job:different",
            sessionId: "session:echo",
            type: "update",
            featureId: "feature:zone-5",
            attributes: JsonNode.Parse("""{ "notes": "should fail" }""")!,
            geometry: CreatePolygonGeometry(2),
            areaDelta: 0);

        Func<Task> act = () => pipeline.ApplyAsync(second).AsTask();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already bound to job 'job:plan-2025'*");
    }

    private static async Task<LayerEditEventEntry> AppendAsync(
        LayerEditEventJournalService journal,
        FakeTimeProvider time,
        string layerId,
        string jobId,
        string? sessionId,
        string type,
        string featureId,
        JsonNode? attributes,
        JsonNode? geometry,
        double? areaDelta)
    {
        var summary = areaDelta.HasValue ? new LayerEditEventOperationSummary(areaDelta, null, Array.Empty<string>()) : null;
        var operation = new LayerEditEventOperation(
            type,
            featureId,
            geometryAfter: geometry,
            attributesAfter: attributes,
            summary: summary);

        var request = new LayerEditEventAppendRequest(layerId, jobId, DefaultContext, new[] { operation }, "user:tester", "polygon")
        {
            SessionId = sessionId,
            CreatedAt = time.GetUtcNow()
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
