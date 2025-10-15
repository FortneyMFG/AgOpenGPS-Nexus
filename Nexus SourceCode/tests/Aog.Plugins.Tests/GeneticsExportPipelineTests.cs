using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Xml.Linq;
using Aog.Core.Paths;
using Aog.Plugins.Genetics;
using FluentAssertions;
using Xunit;

namespace Aog.Plugins.Tests;

public sealed class GeneticsExportPipelineTests
{
    private static List<PlanarPoint> CreateSquare(double size) => new()
    {
        new PlanarPoint(0, 0),
        new PlanarPoint(size, 0),
        new PlanarPoint(size, size),
        new PlanarPoint(0, size)
    };

    [Fact]
    public void Export_GeneratesCsvGeoJsonAndIsoXml()
    {
        var now = new DateTimeOffset(2025, 9, 1, 7, 0, 0, TimeSpan.Zero);
        var pipeline = new GeneticsLayerIngestPipeline(clock: () => now);

        pipeline.UpsertPlan(new GeneticsPlanIngestRequest
        {
            ZoneId = "Plan Zone",
            OuterBoundary = CreateSquare(20),
            Brand = "Brand",
            Product = "Product",
            Lot = "LOT-PLAN",
            TraitStack = "Stack",
            Source = "manual",
            Notes = "Plan notes",
            JobId = "job:plan"
        });

        var changeLog = new List<GeneticsChangeLogEntry>
        {
            new(now.AddMinutes(-10), "operator:1", "barcode", null, "BAR-123"),
            new(now.AddMinutes(-5), "operator:1", "lot", "LOT-PLAN", "LOT-APPLIED")
        };

        pipeline.UpsertVariety(new GeneticsVarietyIngestRequest
        {
            ZoneId = "Plan Zone",
            SessionId = "session:variety",
            JobId = "job:plan",
            OuterBoundary = CreateSquare(20),
            Brand = "Brand",
            Product = "Product",
            Lot = "LOT-APPLIED",
            Barcode = "BAR-123",
            ChangeLog = changeLog,
            Notes = "Variety notes"
        });

        var exportPipeline = new GeneticsExportPipeline();
        var artifacts = exportPipeline.Export(pipeline.GetPlanFeatures(), pipeline.GetVarietyFeatures());

        artifacts.Csv.Should().NotBeNullOrWhiteSpace();
        artifacts.GeoJson.Should().NotBeNullOrWhiteSpace();
        artifacts.IsoXml.Should().NotBeNullOrWhiteSpace();

        var csvLines = artifacts.Csv.Trim().Split('\n');
        csvLines.Should().HaveCount(3);
        csvLines[0].Should().Contain("recordType");
        csvLines[1].Should().Contain("plan");
        csvLines[1].Should().Contain("LOT-PLAN");
        csvLines[2].Should().Contain("variety");
        csvLines[2].Should().Contain("LOT-APPLIED");

        using (var document = JsonDocument.Parse(artifacts.GeoJson))
        {
            var root = document.RootElement;
            root.GetProperty("type").GetString().Should().Be("FeatureCollection");
            var features = root.GetProperty("features");
            features.GetArrayLength().Should().Be(2);

            var planFeature = features[0];
            planFeature.GetProperty("properties").GetProperty("recordType").GetString().Should().Be("plan");
            planFeature.GetProperty("properties").GetProperty("lot").GetString().Should().Be("LOT-PLAN");
            planFeature.GetProperty("geometry").GetProperty("type").GetString().Should().Be("Polygon");

            var varietyFeature = features[1];
            varietyFeature.GetProperty("properties").GetProperty("recordType").GetString().Should().Be("variety");
            varietyFeature.GetProperty("properties").GetProperty("barcode").GetString().Should().Be("BAR-123");
            var changeLogArray = varietyFeature.GetProperty("properties").GetProperty("changeLog");
            changeLogArray.GetArrayLength().Should().Be(2);
        }

        var isoDocument = XDocument.Parse(artifacts.IsoXml);
        isoDocument.Root!.Name.LocalName.Should().Be("ISO11783_TaskData");
        var planElement = isoDocument.Root.Element("GeneticsPlans")!.Elements("Plan").Single();
        planElement.Attribute("lot")!.Value.Should().Be("LOT-PLAN");
        planElement.Element("Geometry")!.Element("Boundary")!.Elements("Point").Should().HaveCount(5);

        var varietyElement = isoDocument.Root.Element("GeneticsVarieties")!.Elements("Variety").Single();
        varietyElement.Attribute("barcode")!.Value.Should().Be("BAR-123");
        varietyElement.Element("ChangeLog")!.Elements("Entry").Should().HaveCount(2);
    }
}
