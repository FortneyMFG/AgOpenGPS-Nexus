using System;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Aog.Core.Layers;
using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Aog.Core.Tests.Layers;

public sealed class LayerEditEventJournalServiceTests
{
    private static LayerEditEventAppendRequest CreateRequest(TimeProvider timeProvider)
    {
        var context = new LayerEditEventContext("farm:alpha", new[] { "field:north-40" }, "season:2025");
        var operation = new LayerEditEventOperation(
            type: "create",
            featureId: "feature:zone-1",
            geometryAfter: JsonNode.Parse("{\"type\":\"Polygon\",\"coordinates\":[[[0,0],[1,0],[1,1],[0,1],[0,0]]]}")!,
            attributesAfter: JsonNode.Parse("{\"name\":\"North Zone\"}")!,
            vertexEdits: new[]
            {
                new LayerEditEventVertexEdit(0, new[] { 0d, 0d }, new[] { 0d, 0d }),
                new LayerEditEventVertexEdit(1, new[] { 1d, 0d }, new[] { 1d, 0d }),
            },
            attributePatches: new[]
            {
                new LayerEditEventAttributePatch("/name", "replace", JsonValue.Create("North Zone"))
            },
            summary: new LayerEditEventOperationSummary(100.5, 45.1, new[] { "name" }));

        return new LayerEditEventAppendRequest(
            layerId: "layer:alpha",
            jobId: "job:seed-2025",
            context: context,
            operations: new[] { operation },
            actor: "user:demo",
            tool: "polygon")
        {
            SessionId = "session:morning",
            TileReferences = new[]
            {
                new LayerEditEventTileReference("tile:abc", "ABCDEF1234567890", "geometry"),
            },
            Metadata = JsonNode.Parse("{\"plugin\":\"mapping\"}")!,
            Notes = "Initial draw",
            CreatedAt = timeProvider.GetUtcNow(),
        };
    }

    [Fact]
    public async Task AppendAsync_ComputesDeterministicHashAndLinksEntries()
    {
        var time = new FakeTimeProvider(new DateTimeOffset(2025, 3, 19, 12, 30, 0, TimeSpan.Zero));
        var service = new LayerEditEventJournalService(time);

        var first = await service.AppendAsync(CreateRequest(time));
        time.Advance(TimeSpan.FromSeconds(15));
        var second = await service.AppendAsync(CreateRequest(time));

        first.SchemaVersion.Should().Be("1.0.0");
        first.Id.Should().Be("layerEdit:20250319T123000000:alpha:0001");
        first.PreviousHash.Should().BeNull();
        first.NextHash.Should().Be(second.Hash);
        first.Hash.Should().NotBeNullOrEmpty();

        second.Id.Should().Be("layerEdit:20250319T123015000:alpha:0002");
        second.PreviousHash.Should().Be(first.Hash);
        second.NextHash.Should().BeNull();
        second.TileReferences.Should().HaveCount(1);
        second.Operations.Should().HaveCount(1);
        second.Metadata.Should().NotBeNull();

        var snapshot = await service.ListAsync("layer:alpha");
        snapshot.Should().HaveCount(2);
        snapshot[0].Hash.Should().Be(first.Hash);
        snapshot[1].Hash.Should().Be(second.Hash);
    }

    [Fact]
    public async Task TruncateAsync_RemovesTailAndClearsNextHash()
    {
        var time = new FakeTimeProvider(new DateTimeOffset(2025, 3, 19, 12, 30, 0, TimeSpan.Zero));
        var service = new LayerEditEventJournalService(time);

        var first = await service.AppendAsync(CreateRequest(time));
        time.Advance(TimeSpan.FromSeconds(5));
        var second = await service.AppendAsync(CreateRequest(time));
        time.Advance(TimeSpan.FromSeconds(5));
        await service.AppendAsync(CreateRequest(time));

        await service.TruncateAsync("layer:alpha", second.Hash);

        var snapshot = await service.ListAsync("layer:alpha");
        snapshot.Should().HaveCount(2);
        snapshot.Last().Hash.Should().Be(second.Hash);
        snapshot.Last().NextHash.Should().BeNull();
        snapshot[0].NextHash.Should().Be(second.Hash);

        await service.TruncateAsync("layer:alpha", null);
        var empty = await service.ListAsync("layer:alpha");
        empty.Should().BeEmpty();
    }

    [Fact]
    public async Task AppendAsync_ValidatesInputs()
    {
        var service = new LayerEditEventJournalService(new FakeTimeProvider());
        var context = new LayerEditEventContext("farm:alpha", new[] { "field:north-40" }, null);
        var operation = new LayerEditEventOperation("create", "feature:zone-1");

        var invalidLayerRequest = new LayerEditEventAppendRequest("layer?invalid", "job:ok", context, new[] { operation }, "actor", "polygon");
        await Assert.ThrowsAsync<ArgumentException>(() => service.AppendAsync(invalidLayerRequest).AsTask());

        var invalidOperation = new LayerEditEventOperation("merge", "feature:zone-1");
        var request = new LayerEditEventAppendRequest("layer:alpha", "job:ok", context, new[] { invalidOperation }, "actor", "polygon");
        await Assert.ThrowsAsync<ArgumentException>(() => service.AppendAsync(request).AsTask());
    }

    [Fact]
    public async Task AppendAsync_RejectsUnsupportedTool()
    {
        var service = new LayerEditEventJournalService(new FakeTimeProvider());
        var context = new LayerEditEventContext("farm:alpha", new[] { "field:north-40" }, null);
        var operation = new LayerEditEventOperation("create", "feature:zone-1");

        var request = new LayerEditEventAppendRequest("layer:alpha", "job:ok", context, new[] { operation }, "actor", "unknown");
        await Assert.ThrowsAsync<ArgumentException>(() => service.AppendAsync(request).AsTask());
    }
}
