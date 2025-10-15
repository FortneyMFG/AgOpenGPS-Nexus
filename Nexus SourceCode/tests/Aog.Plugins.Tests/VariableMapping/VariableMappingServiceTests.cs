using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
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
