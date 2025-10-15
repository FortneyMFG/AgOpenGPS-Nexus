using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Aog.Core.Layers;
using Aog.Core.Paths;
using Aog.Plugins.FileIO;
using FluentAssertions;
using Xunit;

namespace Aog.Plugins.Tests.FileIO;

public sealed class FileIoSurfaceServiceTests
{
    private static readonly string SampleSurfacePath = Path.Combine("FileIO", "Data", "sample-surface.csv");

    [Fact]
    public void ImportSurfaceFromDelimitedFile_LoadsCellsAndProvenance()
    {
        var service = new FileIoSurfaceService();
        var path = Path.Combine(AppContext.BaseDirectory, SampleSurfacePath);

        var document = service.ImportSurfaceFromDelimitedFile(
            path,
            layerId: "layer:vr.sample",
            kind: "rate",
            units: "kg/ha",
            cellSizeMeters: 12,
            source: "import:sample.csv",
            transform: "normalize:demo",
            createdBy: "user:test");

        document.LayerId.Should().Be("layer:vr.sample");
        document.Kind.Should().Be("rate");
        document.Units.Should().Be("kg/ha");
        document.Cells.Should().HaveCount(4);
        document.Cells.Select(cell => cell.CellSizeMeters).Should().OnlyContain(size => Math.Abs(size - 12d) < 1e-6);
        document.Provenance.Source.Should().Be("import:sample.csv");
        document.Provenance.Transform.Should().Be("normalize:demo");
        document.Provenance.Hash.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ExportSurfaceToGeoJsonAsync_WritesFeatureCollection()
    {
        var timestamp = new DateTimeOffset(2025, 03, 19, 10, 15, 00, TimeSpan.Zero);
        var cells = new[]
        {
            new AgronomicLayerCell(new PlanarPoint(100, 200), 12, 42.5),
            new AgronomicLayerCell(new PlanarPoint(112, 200), 12, 41.25)
        };
        var provenance = new LayerProvenance(
            source: "import:sample.csv",
            transform: "normalize:demo",
            hash: "ABC123",
            createdAt: timestamp,
            actor: "user:test");
        var document = new AgronomicLayerDocument(
            layerId: "layer:vr.sample",
            kind: "rate",
            units: "kg/ha",
            createdAt: timestamp,
            createdBy: "user:test",
            cells: cells,
            provenance: provenance);

        var service = new FileIoSurfaceService();
        var directory = Path.Combine(Path.GetTempPath(), $"nexus-fileio-{Guid.NewGuid():N}");
        var path = Path.Combine(directory, "surface.geojson");

        try
        {
            await service.ExportSurfaceToGeoJsonAsync(document, path).ConfigureAwait(false);

            File.Exists(path).Should().BeTrue();

            using var json = JsonDocument.Parse(File.ReadAllText(path));
            var root = json.RootElement;
            root.GetProperty("type").GetString().Should().Be("FeatureCollection");
            root.GetProperty("name").GetString().Should().Be("layer:vr.sample");

            var properties = root.GetProperty("properties");
            properties.GetProperty("layerId").GetString().Should().Be("layer:vr.sample");
            properties.GetProperty("units").GetString().Should().Be("kg/ha");
            properties.GetProperty("createdAt").GetString().Should().Be(timestamp.ToString("O"));
            properties.GetProperty("provenanceHash").GetString().Should().Be("ABC123");

            var bbox = root.GetProperty("bbox").EnumerateArray().Select(element => element.GetDouble()).ToArray();
            bbox.Should().BeEquivalentTo(new[] { 94d, 194d, 118d, 206d });

            var features = root.GetProperty("features").EnumerateArray().ToList();
            features.Should().HaveCount(2);

            var firstFeature = features[0];
            firstFeature.GetProperty("type").GetString().Should().Be("Feature");
            var featureProperties = firstFeature.GetProperty("properties");
            featureProperties.GetProperty("value").GetDouble().Should().BeApproximately(42.5, 1e-6);
            featureProperties.GetProperty("cellSizeMeters").GetDouble().Should().BeApproximately(12d, 1e-6);
            featureProperties.GetProperty("centerEasting").GetDouble().Should().BeApproximately(100d, 1e-6);
            featureProperties.GetProperty("centerNorthing").GetDouble().Should().BeApproximately(200d, 1e-6);

            var coordinates = firstFeature
                .GetProperty("geometry")
                .GetProperty("coordinates")[0]
                .EnumerateArray()
                .Select(point => (X: point[0].GetDouble(), Y: point[1].GetDouble()))
                .ToArray();

            coordinates.Should().HaveCount(5);
            coordinates[0].Should().Be((94d, 194d));
            coordinates[1].Should().Be((106d, 194d));
            coordinates[2].Should().Be((106d, 206d));
            coordinates[3].Should().Be((94d, 206d));
            coordinates[4].Should().Be((94d, 194d));
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }
}
