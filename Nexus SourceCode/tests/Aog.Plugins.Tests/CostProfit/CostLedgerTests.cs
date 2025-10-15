using System;
using System.Linq;
using Aog.Plugins.CostProfit;
using FluentAssertions;
using Xunit;

namespace Aog.Plugins.Tests.CostProfit;

public sealed class CostLedgerTests
{
    private static InventoryLotDefinition CreateLotDefinition(string lotId = "lot-1") => new(
        lotId,
        sku: "SKU-001",
        displayName: "Hybrid Seed Lot",
        supplier: "AOG Co",
        quantityUnits: "bags",
        currency: "USD",
        storageLocation: "Warehouse A",
        receivedAt: new DateTimeOffset(2025, 3, 1, 8, 0, 0, TimeSpan.Zero),
        createdBy: "ops");

    private static CostScope CreateScope(string farmId = "farm-1", string? fieldId = "field-1") => new(
        farmId,
        fieldId,
        jobId: "job-1",
        sessionId: "session-1");

    [Fact]
    public void RegisterLot_SeedsLedgerWithInitialBalances()
    {
        var ledger = new CostLedger();
        var definition = CreateLotDefinition();

        var snapshot = ledger.RegisterLot(definition, initialQuantity: 20, initialCost: 400m, actor: "ops", timestamp: DateTimeOffset.UtcNow);

        snapshot.Definition.Should().Be(definition);
        snapshot.QuantityOnHand.Should().Be(20);
        snapshot.TotalCostBasis.Should().Be(400m);
        snapshot.WeightedCostPerUnit.Should().Be(20m);
    }

    [Fact]
    public void ReceiveInventory_UpdatesWeightedCostBasis()
    {
        var ledger = new CostLedger();
        var definition = CreateLotDefinition();
        ledger.RegisterLot(definition, initialQuantity: 10, initialCost: 200m, actor: "ops", timestamp: DateTimeOffset.UtcNow);

        var updated = ledger.ReceiveInventory(definition.LotId, quantity: 5, cost: 150m, actor: "ops", timestamp: DateTimeOffset.UtcNow);

        updated.QuantityOnHand.Should().Be(15);
        updated.TotalCostBasis.Should().Be(350m);
        updated.WeightedCostPerUnit.Should().Be(23.3333m);
    }

    [Fact]
    public void CommitAndReleaseInventory_AdjustsCommittedQuantity()
    {
        var ledger = new CostLedger();
        var definition = CreateLotDefinition();
        ledger.RegisterLot(definition, initialQuantity: 8, initialCost: 80m, actor: "ops", timestamp: DateTimeOffset.UtcNow);

        var committed = ledger.CommitInventory(definition.LotId, quantity: 3, actor: "planner", timestamp: DateTimeOffset.UtcNow);
        committed.CommittedQuantity.Should().Be(3);

        var released = ledger.ReleaseInventory(definition.LotId, quantity: 2, actor: "planner", timestamp: DateTimeOffset.UtcNow);
        released.CommittedQuantity.Should().Be(1);
    }

    [Fact]
    public void ConsumeInventory_GeneratesCostRecordAndUpdatesBalances()
    {
        var ledger = new CostLedger();
        var definition = CreateLotDefinition();
        ledger.RegisterLot(definition, initialQuantity: 50, initialCost: 500m, actor: "ops", timestamp: DateTimeOffset.UtcNow);
        ledger.CommitInventory(definition.LotId, quantity: 10, actor: "planner", timestamp: DateTimeOffset.UtcNow);

        var record = ledger.ConsumeInventory(
            lotId: definition.LotId,
            quantity: 8,
            recordId: "cost-001",
            scope: CreateScope(),
            category: CostCategory.Seed,
            timestamp: DateTimeOffset.UtcNow,
            actor: "machine",
            jobLayerId: "layer-123",
            notes: "Auto deducted during planting");

        record.Amount.Should().Be(80m);
        record.Currency.Should().Be("USD");
        record.InventoryLotId.Should().Be(definition.LotId);
        record.Quantity.Should().Be(8);
        record.QuantityUnits.Should().Be("bags");

        var lot = ledger.GetLot(definition.LotId);
        lot.Should().NotBeNull();
        lot!.QuantityOnHand.Should().Be(42);
        lot.CommittedQuantity.Should().Be(2);
        lot.TotalCostBasis.Should().Be(420m);
    }

    [Fact]
    public void RecordCost_DuplicateIdentifierThrows()
    {
        var ledger = new CostLedger();
        var scope = CreateScope();
        var record = new CostRecord(
            id: "cost-1",
            scope,
            CostCategory.Fuel,
            amount: 125.45m,
            currency: "USD",
            timestamp: DateTimeOffset.UtcNow,
            actor: "ops");

        ledger.RecordCost(record);
        Action act = () => ledger.RecordCost(record);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Summarize_FiltersByScopeAndCurrency()
    {
        var ledger = new CostLedger();
        var scopeA = CreateScope(farmId: "farm-A", fieldId: "field-1");
        var scopeB = CreateScope(farmId: "farm-A", fieldId: "field-2");
        var scopeC = CreateScope(farmId: "farm-B", fieldId: "field-1");

        ledger.RecordCost(new CostRecord("cost-1", scopeA, CostCategory.Labour, 100m, "USD", DateTimeOffset.UtcNow, "ops"));
        ledger.RecordCost(new CostRecord("cost-2", scopeB, CostCategory.Labour, 250m, "USD", DateTimeOffset.UtcNow, "ops"));
        ledger.RecordCost(new CostRecord("cost-3", scopeC, CostCategory.Fuel, 300m, "CAD", DateTimeOffset.UtcNow, "ops"));

        var filter = new CostScopeFilter { FarmId = "farm-A" };
        var summary = ledger.Summarize(filter);

        summary.TotalsByCurrency.Should().HaveCount(1);
        summary.TotalsByCurrency.Should().ContainKey("USD");
        summary.TotalsByCurrency["USD"].Should().Be(350m);

        filter.FieldId = "field-2";
        var fieldSummary = ledger.Summarize(filter);
        fieldSummary.TotalsByCurrency.Single().Value.Should().Be(250m);
    }

    [Fact]
    public void ConsumeInventory_WhenQuantityExceedsBalanceThrows()
    {
        var ledger = new CostLedger();
        var definition = CreateLotDefinition();
        ledger.RegisterLot(definition, initialQuantity: 5, initialCost: 100m, actor: "ops", timestamp: DateTimeOffset.UtcNow);

        Action act = () => ledger.ConsumeInventory(
            lotId: definition.LotId,
            quantity: 6,
            recordId: "cost-err",
            scope: CreateScope(),
            category: CostCategory.Seed,
            timestamp: DateTimeOffset.UtcNow,
            actor: "machine");

        act.Should().Throw<InvalidOperationException>();
    }
}
