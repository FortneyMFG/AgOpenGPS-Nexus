using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Aog.Core.Layers;
using Aog.Core.Reporting;
using Aog.Plugins.Crop;
using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Aog.Plugins.Tests.Crop;

public sealed class CropReportSectionContributorTests
{
    [Fact]
    public async Task PrepareAndRenderAsync_ReturnsPayloadWhenSnapshotAvailable()
    {
        var snapshot = await CreateSnapshotAsync();
        var provider = new TestProvider(snapshot);
        var contributor = new CropReportSectionContributor(provider);
        var context = CreateContext();
        var section = context.Template.Sections.Single();

        var prepare = await contributor.PrepareAsync(context, CancellationToken.None);
        prepare.IsReady.Should().BeTrue();

        var result = await contributor.RenderAsync(new ReportSectionContext(context, section), CancellationToken.None);
        result.Status.Should().Be(ReportSectionStatus.Success);
        result.Payload.Should().BeOfType<CropReportSectionPayload>();

        var payload = (CropReportSectionPayload)result.Payload!;
        payload.GeneratedAt.Should().Be(snapshot.GeneratedAt);
        payload.Planned.Should().NotBeEmpty();
        payload.History.Should().HaveCount(1);
        payload.LatestByField.Should().ContainKey("field:alpha");
    }

    [Fact]
    public async Task PrepareAsync_ReturnsMissingWhenProviderNull()
    {
        var provider = new TestProvider(null);
        var contributor = new CropReportSectionContributor(provider);
        var context = CreateContext();
        var section = context.Template.Sections.Single();

        var prepare = await contributor.PrepareAsync(context, CancellationToken.None);
        prepare.IsReady.Should().BeFalse();

        var result = await contributor.RenderAsync(new ReportSectionContext(context, section), CancellationToken.None);
        result.Status.Should().Be(ReportSectionStatus.MissingData);
    }

    private static async Task<CropAnalyticsSnapshot> CreateSnapshotAsync()
    {
        var time = new FakeTimeProvider(new DateTimeOffset(2025, 4, 10, 7, 0, 0, TimeSpan.Zero));
        var journal = new LayerEditEventJournalService(time);
        var planned = new CropLayerIngestionPipeline("layer:cropType.planned.2025");
        var history = new CropLayerIngestionPipeline("layer:cropType.history.2024");

        await planned.ApplyAsync(await AppendAsync(
            journal,
            time,
            layerId: "layer:cropType.planned.2025",
            jobId: "job:plan",
            fieldIds: new[] { "field:alpha" },
            type: "create",
            featureId: "feature:planned-1",
            attributes: JsonNode.Parse("""{ "crop": "Corn", "status": "planned", "year": 2025 }""")!,
            geometry: CreatePolygonGeometry(),
            areaDelta: 1000));

        await history.ApplyAsync(await AppendAsync(
            journal,
            time,
            layerId: "layer:cropType.history.2024",
            jobId: "job:archive",
            fieldIds: new[] { "field:alpha" },
            type: "create",
            featureId: "feature:history-1",
            attributes: JsonNode.Parse("""{ "crop": "Soybean", "status": "historical", "year": 2024 }""")!,
            geometry: CreatePolygonGeometry(),
            areaDelta: 950));

        var service = new CropAnalyticsService(time);
        return service.CreateSnapshot(
            plannedLayers: new[] { planned.CreateSnapshot() },
            actualLayers: Array.Empty<CropLayerSnapshot>(),
            historicalLayers: new[] { history.CreateSnapshot() });
    }

    private static ReportGenerationContext CreateContext()
    {
        var template = new ReportTemplate(
            "template:crop",
            "Crop Summary",
            "1.0.0",
            ReportScopeKind.Job,
            new[] { new ReportTemplateSection("crop.analytics.summary") },
            new[] { new ReportTemplateOutput("json", "JSON") });

        var scope = new ReportScope(ReportScopeKind.Job, "job:demo", new Dictionary<string, string>
        {
            ["seasonId"] = "season:2025",
            ["jobId"] = "job:demo"
        });

        var options = new ReportGenerationOptions();
        return new ReportGenerationContext(template, scope, options);
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
        var context = new LayerEditEventContext("farm:demo", fieldIds.ToArray(), "season:2025");
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

    private sealed class TestProvider : ICropAnalyticsProvider
    {
        private readonly CropAnalyticsSnapshot? _snapshot;

        public TestProvider(CropAnalyticsSnapshot? snapshot)
        {
            _snapshot = snapshot;
        }

        public ValueTask<CropAnalyticsSnapshot?> GetSnapshotAsync(ReportGenerationContext context, CancellationToken cancellationToken)
        {
            return new ValueTask<CropAnalyticsSnapshot?>(_snapshot);
        }
    }
}
