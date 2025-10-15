using System;
using System.Collections.Generic;
using System.Linq;
using Aog.Core.Layers;
using Aog.Core.Paths;
using Aog.Core.Safety;
using Aog.Core.V1;
using Aog.Plugins.Sections;
using FluentAssertions;
using Xunit;

namespace Aog.Plugins.Tests.Sections;

public sealed class VariableRateControllerTests
{
    [Fact]
    public void ComputeRates_UsesLayerCellsAndClamps()
    {
        var placements = new List<SectionPlacement>
        {
            new(new PlanarPoint(0, 0)),
            new(new PlanarPoint(10, 0)),
            new(new PlanarPoint(0, 10)),
            new(new PlanarPoint(10, 10)),
        };

        var cells = new List<AgronomicLayerCell>
        {
            new(new PlanarPoint(0, 0), 12, 80),
            new(new PlanarPoint(10, 0), 12, 140),
            new(new PlanarPoint(0, 10), 12, 90),
            new(new PlanarPoint(10, 10), 12, 60),
        };

        var layer = new AgronomicLayerDocument(
            layerId: "layer:rate",
            kind: "rate",
            units: "L/ha",
            createdAt: DateTimeOffset.UtcNow,
            createdBy: "user:test",
            cells: cells,
            provenance: new LayerProvenance("import:test", "resample:nearest", "hash", DateTimeOffset.UtcNow, "user:test"));

        var controller = new VariableRateController(sectionCount: 4, minimumRate: 70, maximumRate: 120, defaultRate: 95);

        var rates = controller.ComputeRates(placements, layer);

        rates.Should().ContainInOrder(80, 120, 90, 70);
    }

    [Fact]
    public void ComputeRates_UsesDefaultWhenLayerEmpty()
    {
        var placements = new List<SectionPlacement>
        {
            new(new PlanarPoint(0, 0)),
            new(new PlanarPoint(10, 0)),
        };

        var layer = new AgronomicLayerDocument(
            "layer:empty",
            "rate",
            "L/ha",
            DateTimeOffset.UtcNow,
            "user",
            Array.Empty<AgronomicLayerCell>(),
            new LayerProvenance("import", "none", "hash", DateTimeOffset.UtcNow, "user"));

        var controller = new VariableRateController(2, 50, 150, 90);

        var rates = controller.ComputeRates(placements, layer);
        rates.Should().AllBeEquivalentTo(90);
    }

    [Fact]
    public void ComputeRates_UsesDefaultWhenSectionOutsideLayer()
    {
        var placements = new List<SectionPlacement>
        {
            new(new PlanarPoint(100, 100)),
            new(new PlanarPoint(120, 120)),
        };

        var cells = new List<AgronomicLayerCell>
        {
            new(new PlanarPoint(0, 0), 12, 100),
        };

        var layer = new AgronomicLayerDocument(
            "layer:rate",
            "rate",
            "L/ha",
            DateTimeOffset.UtcNow,
            "user",
            cells,
            new LayerProvenance("import", "none", "hash", DateTimeOffset.UtcNow, "user"));

        var controller = new VariableRateController(2, 50, 150, 90);

        var rates = controller.ComputeRates(placements, layer);
        rates.Should().OnlyContain(rate => Math.Abs(rate - 90) < 1e-6);
    }

    [Fact]
    public void ComputeRates_TreatsBoundaryAsInsideCell()
    {
        var placements = new List<SectionPlacement>
        {
            new(new PlanarPoint(6, 0)),
        };

        var cells = new List<AgronomicLayerCell>
        {
            new(new PlanarPoint(0, 0), 12, 110),
        };

        var layer = new AgronomicLayerDocument(
            "layer:rate",
            "rate",
            "L/ha",
            DateTimeOffset.UtcNow,
            "user",
            cells,
            new LayerProvenance("import", "none", "hash", DateTimeOffset.UtcNow, "user"));

        var controller = new VariableRateController(1, 50, 150, 90);

        var rates = controller.ComputeRates(placements, layer);
        rates.Should().ContainSingle().Which.Should().Be(110);
    }

    [Fact]
    public void ComputeRates_ConstraintGateZerosOutputs()
    {
        var placements = new List<SectionPlacement>
        {
            new(new PlanarPoint(0, 0)),
            new(new PlanarPoint(10, 0)),
        };

        var cells = new List<AgronomicLayerCell>
        {
            new(new PlanarPoint(0, 0), 12, 100),
            new(new PlanarPoint(10, 0), 12, 110),
        };

        var layer = new AgronomicLayerDocument(
            "layer:rate",
            "rate",
            "L/ha",
            DateTimeOffset.UtcNow,
            "user",
            cells,
            new LayerProvenance("import", "none", "hash", DateTimeOffset.UtcNow, "user"));

        var controller = new VariableRateController(2, 50, 150, 90);
        var gate = ConstraintGateSnapshot.FromZoneMask(new PoseZoneMask { InsideKeepOut = true }, DateTimeOffset.UtcNow);

        var rates = controller.ComputeRates(placements, layer, gate);

        rates.Should().OnlyContain(rate => Math.Abs(rate) < 1e-6);
    }
}
