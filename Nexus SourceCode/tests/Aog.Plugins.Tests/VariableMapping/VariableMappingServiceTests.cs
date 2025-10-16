using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using Aog.Core.Layers;
using Aog.Core.Paths;
using Aog.Plugins.Sections;
using Aog.Plugins.VariableMapping;
using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Aog.Plugins.Tests.VariableMapping;

public class VariableMappingServiceTests
{
    [Fact]
    public void ImportPrescription_ShouldLoadLayerAndCache()
    {
        var clock = new FakeTimeProvider();
        var ingestor = new ExternalAgronomicMapIngestor(() => clock.GetUtcNow());
        var service = new VariableMappingService(ingestor);
        var samplePath = Path.Combine(AppContext.BaseDirectory, "FileIO", "Data", "sample-surface.csv");
        var timestamp = clock.GetUtcNow();

        var layer = service.ImportPrescription(
            "field-42",
            samplePath,
            cellSizeMeters: 12,
            units: "kg/ha",
            createdBy: "user:agronomist");

        layer.LayerId.Should().Be("vr.planned.field-42");
        layer.Kind.Should().Be("variable-rate");
        layer.CreatedAt.Should().Be(timestamp);
        layer.CreatedBy.Should().Be("user:agronomist");
        layer.Cells.Should().HaveCount(4);
        layer.Provenance.Source.Should().Be("plugin:variable-mapping");
        layer.Provenance.Transform.Should().Be("recipe:field-42;roi:none");

        service.TryGetPlannedLayer("vr.planned.field-42", out var cached).Should().BeTrue();
        ReferenceEquals(layer, cached).Should().BeTrue();
    }

    [Fact]
    public void ImportPrescription_ShouldApplyRegionOfInterest()
    {
        var clock = new FakeTimeProvider();
        var ingestor = new ExternalAgronomicMapIngestor(() => clock.GetUtcNow());
        var service = new VariableMappingService(ingestor);
        var samplePath = Path.Combine(AppContext.BaseDirectory, "FileIO", "Data", "sample-surface.csv");
        var timestamp = clock.GetUtcNow();

        var roiCells = new[]
        {
            new AgronomicLayerCell(new PlanarPoint(0, 0), 12, 1),
            new AgronomicLayerCell(new PlanarPoint(12, 0), 12, 1),
            new AgronomicLayerCell(new PlanarPoint(0, 12), 12, 0),
            new AgronomicLayerCell(new PlanarPoint(12, 12), 12, 0)
        };
        var roiLayer = new AgronomicLayerDocument(
            "roi.field-42",
            "region-of-interest",
            "dimensionless",
            timestamp,
            "user:editor",
            roiCells,
            new LayerProvenance(
                "layer-edit",
                "roi-selection",
                ComputeLayerHash("roi.field-42", roiCells),
                timestamp,
                "user:editor"));

        var layer = service.ImportPrescription(
            "field-42",
            samplePath,
            cellSizeMeters: 12,
            units: "kg/ha",
            createdBy: "user:agronomist",
            regionOfInterest: roiLayer);

        layer.Cells.Should().HaveCount(2);
        layer.Cells.Select(c => c.Position.Northing).Distinct().Should().ContainSingle().Which.Should().Be(0);
        layer.Provenance.Transform.Should().Be("recipe:field-42;roi:roi.field-42");
        layer.Provenance.Hash.Should().Be(ComputeLayerHash(layer.LayerId, layer.Cells));
    }

    [Fact]
    public void ComputeSetpoints_ShouldReturnSectionRates()
    {
        var clock = new FakeTimeProvider();
        var ingestor = new ExternalAgronomicMapIngestor(() => clock.GetUtcNow());
        var service = new VariableMappingService(ingestor);
        var samplePath = Path.Combine(AppContext.BaseDirectory, "FileIO", "Data", "sample-surface.csv");
        service.ImportPrescription("field-42", samplePath, 12, "kg/ha");

        var controller = new VariableRateController(sectionCount: 2, minimumRate: 0, maximumRate: 20, defaultRate: 10);
        var sections = new[]
        {
            new SectionPlacement(new PlanarPoint(0, 0)),
            new SectionPlacement(new PlanarPoint(12, 12))
        };

        var setpoints = service.ComputeSetpoints("vr.planned.field-42", controller, sections);

        setpoints.Should().HaveCount(2);
        setpoints[0].Should().BeApproximately(12.5, 1e-6);
        setpoints[1].Should().BeApproximately(14.1, 1e-6);
    }

    [Fact]
    public void ComputeSetpoints_ShouldThrowForMissingLayer()
    {
        var service = new VariableMappingService(new ExternalAgronomicMapIngestor());
        var controller = new VariableRateController(sectionCount: 1, minimumRate: 0, maximumRate: 20, defaultRate: 10);
        var sections = new[] { new SectionPlacement(new PlanarPoint(0, 0)) };

        var act = () => service.ComputeSetpoints("missing", controller, sections);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*missing*");
    }

    [Fact]
    public void ComputeSetpoints_ShouldUpdateTransportGuard()
    {
        var clock = new FakeTimeProvider();
        var ingestor = new ExternalAgronomicMapIngestor(() => clock.GetUtcNow());
        var service = new VariableMappingService(ingestor);
        var samplePath = Path.Combine(AppContext.BaseDirectory, "FileIO", "Data", "sample-surface.csv");
        var layer = service.ImportPrescription("field-42", samplePath, 12, "kg/ha");

        var controller = new VariableRateController(sectionCount: 2, minimumRate: 0, maximumRate: 20, defaultRate: 10);
        var sections = new[]
        {
            new SectionPlacement(new PlanarPoint(0, 0)),
            new SectionPlacement(new PlanarPoint(12, 12))
        };

        var guard = new VariableRateTransportGuard(timeProvider: clock);
        guard.Arm(layer.LayerId, layer.Provenance.Hash);

        var timestamp = clock.GetUtcNow();
        var setpoints = service.ComputeSetpoints(layer.LayerId, controller, sections, guard, timestamp);

        setpoints.Should().HaveCount(2);
        guard.EnsureHeartbeatFresh(timestamp + TimeSpan.FromMilliseconds(299));

        clock.Advance(TimeSpan.FromMilliseconds(400));
        Action act = () => guard.EnsureHeartbeatFresh(clock.GetUtcNow());
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ComputeSetpoints_ShouldThrowWhenTransportGuardMismatched()
    {
        var clock = new FakeTimeProvider();
        var ingestor = new ExternalAgronomicMapIngestor(() => clock.GetUtcNow());
        var service = new VariableMappingService(ingestor);
        var samplePath = Path.Combine(AppContext.BaseDirectory, "FileIO", "Data", "sample-surface.csv");
        var layer = service.ImportPrescription("field-42", samplePath, 12, "kg/ha");

        var controller = new VariableRateController(sectionCount: 1, minimumRate: 0, maximumRate: 20, defaultRate: 10);
        var sections = new[] { new SectionPlacement(new PlanarPoint(0, 0)) };

        var guard = new VariableRateTransportGuard(timeProvider: clock);
        guard.Arm("vr.planned.other", layer.Provenance.Hash);

        Action act = () => service.ComputeSetpoints(layer.LayerId, controller, sections, guard, clock.GetUtcNow());

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*mismatch*");
    }

    [Fact]
    public void ExportPrescription_ShouldProduceIsoXmlAndManifest()
    {
        var clock = new FakeTimeProvider();
        var ingestor = new ExternalAgronomicMapIngestor(() => clock.GetUtcNow());
        var pipeline = new PrescriptionExportPipeline(clock);
        var service = new VariableMappingService(ingestor, pipeline);
        var samplePath = Path.Combine(AppContext.BaseDirectory, "FileIO", "Data", "sample-surface.csv");
        var layer = service.ImportPrescription("field-42", samplePath, 12, "kg/ha", createdBy: "user:agronomist");

        var artifacts = service.ExportPrescription(layer.LayerId, "job-88", "session-22", "recipe-42", "user:operator");

        artifacts.IsoXml.Should().NotBeNullOrWhiteSpace();
        artifacts.ManifestJson.Should().NotBeNullOrWhiteSpace();

        var isoDocument = XDocument.Parse(artifacts.IsoXml);
        isoDocument.Root!.Name.LocalName.Should().Be("ISO11783_TaskData");
        var taskElement = isoDocument.Root!.Element("Task");
        taskElement.Should().NotBeNull();
        taskElement!.Attribute("id")!.Value.Should().Be("job-88");
        taskElement.Attribute("sessionId")!.Value.Should().Be("session-22");
        taskElement.Attribute("recipeId")!.Value.Should().Be("recipe-42");
        taskElement.Attribute("layerId")!.Value.Should().Be(layer.LayerId);

        var layerElement = isoDocument.Root!.Element("PrescriptionLayer");
        layerElement.Should().NotBeNull();
        layerElement!.Attribute("hash")!.Value.Should().Be(layer.Provenance.Hash);
        layerElement.Element("Cells")!.Elements("Cell").Should().HaveCount(layer.Cells.Count);

        using var manifest = JsonDocument.Parse(artifacts.ManifestJson);
        manifest.RootElement.GetProperty("jobId").GetString().Should().Be("job-88");
        manifest.RootElement.GetProperty("recipeId").GetString().Should().Be("recipe-42");
        manifest.RootElement.GetProperty("layerId").GetString().Should().Be(layer.LayerId);
        manifest.RootElement.GetProperty("operatorId").GetString().Should().Be("user:operator");
    }

    private static string ComputeLayerHash(string layerId, IReadOnlyList<AgronomicLayerCell> cells)
    {
        using var sha = SHA256.Create();
        var builder = new StringBuilder();
        builder.Append(layerId);
        builder.Append('|');

        foreach (var cell in cells
            .OrderBy(c => c.Position.Easting)
            .ThenBy(c => c.Position.Northing)
            .ThenBy(c => c.CellSizeMeters))
        {
            builder.AppendFormat(
                CultureInfo.InvariantCulture,
                "{0:F3},{1:F3},{2:G17},{3:G17};",
                cell.Position.Easting,
                cell.Position.Northing,
                cell.CellSizeMeters,
                cell.Value);
        }

        var bytes = Encoding.UTF8.GetBytes(builder.ToString());
        var hash = sha.ComputeHash(bytes);
        return Convert.ToHexString(hash);
    }
}
