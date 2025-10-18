using System;
using System.Collections.Generic;
using Aog.Core.Paths;
using Aog.Plugins.CostProfit;
using Microsoft.Extensions.Time.Testing;
using FluentAssertions;
using Xunit;

namespace Aog.Plugins.Tests.CostProfit;

public sealed class ProfitExportPipelineTests
{
    private static ProfitExportOptions CreateOptions() => new()
    {
        LayerId = "profit-field-1",
        LayerKind = "ProfitLayer.v1",
        Units = "USD/ha",
        Source = "plugin:cost-profit",
        Transform = "profit/export/v1",
        Actor = "plugin:cost-profit",
        CreatedBy = "plugin:cost-profit"
    };

    [Fact]
    public void CreateProfitLayer_ProducesSortedLayerWithProvenance()
    {
        var options = CreateOptions();
        var time = new FakeTimeProvider(new DateTimeOffset(2025, 3, 22, 6, 30, 0, TimeSpan.Zero));
        var pipeline = new ProfitExportPipeline(options, time);

        var cells = new List<ProfitCell>
        {
            new(new PlanarPoint(10, 20), 5, revenue: 500m, cost: 300m),
            new(new PlanarPoint(0, 10), 5, revenue: 400m, cost: 150m)
        };

        var layer = pipeline.CreateProfitLayer(cells);

        layer.LayerId.Should().Be(options.LayerId);
        layer.Kind.Should().Be("ProfitLayer.v1");
        layer.Units.Should().Be("USD/ha");
        layer.CreatedAt.Should().Be(time.GetUtcNow());
        layer.Provenance.Source.Should().Be("plugin:cost-profit");
        layer.Provenance.Transform.Should().Be("profit/export/v1");
        layer.Provenance.Hash.Should().NotBeNullOrWhiteSpace();
        layer.Cells.Should().HaveCount(2);
        layer.Cells[0].Position.Should().Be(new PlanarPoint(0, 10));
        layer.Cells[0].Value.Should().Be(250d);
        layer.Cells[1].Position.Should().Be(new PlanarPoint(10, 20));
        layer.Cells[1].Value.Should().Be(200d);
    }

    [Fact]
    public void CreateSummaryRows_ReturnsPerCurrencyBreakdown()
    {
        var ledger = new CostLedger();
        var scope = new CostScope("farm-1", "field-1", "job-1", "session-1");
        ledger.RecordCost(new CostRecord("cost-1", scope, CostCategory.Seed, 100m, "USD", DateTimeOffset.UtcNow, "ops"));
        ledger.RecordCost(new CostRecord("cost-2", scope, CostCategory.Fuel, 50m, "USD", DateTimeOffset.UtcNow, "ops"));

        var revenue = new[]
        {
            new RevenueContribution("rev-1", scope, 400m, "USD", DateTimeOffset.UtcNow, "yield", "ops")
        };

        var rollup = new ProfitAnalyticsRollupService(ledger)
            .CreateRollup(new CostScopeFilter { FarmId = "farm-1", FieldId = "field-1" }, revenue);

        var pipeline = new ProfitExportPipeline(CreateOptions());
        var rows = pipeline.CreateSummaryRows(rollup);

        rows.Should().ContainSingle();
        var row = rows[0];
        row.Currency.Should().Be("USD");
        row.Revenue.Should().Be(400m);
        row.Cost.Should().Be(150m);
        row.Profit.Should().Be(250m);
        row.Margin.Should().Be(0.6250m);
    }
}
