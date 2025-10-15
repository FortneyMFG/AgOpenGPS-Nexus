using System;
using System.Collections.Generic;
using Aog.Plugins.CostProfit;
using FluentAssertions;
using Xunit;

namespace Aog.Plugins.Tests.CostProfit;

public sealed class ProfitAnalyticsRollupServiceTests
{
    private static CostScope CreateScope(string fieldId = "field-1") => new("farm-1", fieldId, "job-1", "session-1");

    [Fact]
    public void CreateRollup_AggregatesRevenueAndCostByCurrency()
    {
        var ledger = new CostLedger();
        var scope = CreateScope();
        ledger.RecordCost(new CostRecord("cost-1", scope, CostCategory.Seed, 200m, "USD", DateTimeOffset.UtcNow, "ops"));
        ledger.RecordCost(new CostRecord("cost-2", scope, CostCategory.Fuel, 50m, "USD", DateTimeOffset.UtcNow, "ops"));
        ledger.RecordCost(new CostRecord("cost-3", scope, CostCategory.Misc, 75m, "CAD", DateTimeOffset.UtcNow, "ops"));

        var revenue = new List<RevenueContribution>
        {
            new("rev-1", scope, 900m, "USD", DateTimeOffset.UtcNow, "yield", "ops"),
            new("rev-2", scope, 600m, "CAD", DateTimeOffset.UtcNow, "yield", "ops"),
            new("rev-3", CreateScope(fieldId: "field-2"), 500m, "USD", DateTimeOffset.UtcNow, "yield", "ops")
        };

        var service = new ProfitAnalyticsRollupService(ledger);
        var filter = new CostScopeFilter { FarmId = "farm-1", FieldId = "field-1" };

        var rollup = service.CreateRollup(filter, revenue);

        rollup.RevenueByCurrency.Should().ContainKey("USD").WhoseValue.Should().Be(900m);
        rollup.RevenueByCurrency.Should().ContainKey("CAD").WhoseValue.Should().Be(600m);
        rollup.CostByCurrency.Should().ContainKey("USD").WhoseValue.Should().Be(250m);
        rollup.CostByCurrency.Should().ContainKey("CAD").WhoseValue.Should().Be(75m);
        rollup.ProfitByCurrency.Should().Contain(new KeyValuePair<string, decimal>("USD", 650m));
        rollup.ProfitByCurrency.Should().Contain(new KeyValuePair<string, decimal>("CAD", 525m));
        rollup.MarginByCurrency.Should().Contain(new KeyValuePair<string, decimal>("USD", 0.7222m));
        rollup.MarginByCurrency.Should().Contain(new KeyValuePair<string, decimal>("CAD", 0.8750m));
    }

    [Fact]
    public void CreateRollup_WhenRevenueMissingProducesNegativeProfit()
    {
        var ledger = new CostLedger();
        var scope = CreateScope();
        ledger.RecordCost(new CostRecord("cost-1", scope, CostCategory.Misc, 100m, "USD", DateTimeOffset.UtcNow, "ops"));

        var service = new ProfitAnalyticsRollupService(ledger);
        var rollup = service.CreateRollup(new CostScopeFilter { FarmId = "farm-1" }, Array.Empty<RevenueContribution>());

        rollup.ProfitByCurrency.Should().Contain(new KeyValuePair<string, decimal>("USD", -100m));
        rollup.MarginByCurrency.Should().Contain(new KeyValuePair<string, decimal>("USD", 0m));
    }
}
