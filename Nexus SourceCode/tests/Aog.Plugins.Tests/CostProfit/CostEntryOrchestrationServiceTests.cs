using System;
using System.Collections.Generic;
using Aog.Plugins.CostProfit;
using FluentAssertions;
using Xunit;

namespace Aog.Plugins.Tests.CostProfit;

public sealed class CostEntryOrchestrationServiceTests
{
    private static CostScope CreateScope() => new("farm-1", "field-1", "job-1", "session-1");

    [Fact]
    public void CaptureManualEntry_GeneratesIdentifierAndPersistsRecord()
    {
        var ledger = new CostLedger();
        var time = new FakeTimeProvider(new DateTimeOffset(2025, 3, 20, 12, 0, 0, TimeSpan.Zero));
        var service = new CostEntryOrchestrationService(ledger, time);
        var draft = new CostEntryDraft(
            CreateScope(),
            CostCategory.Seed,
            amount: 120.25m,
            currency: "USD",
            actor: "operator",
            quantity: 5,
            quantityUnits: "bags",
            notes: "Manual entry");

        var record = service.CaptureManualEntry(draft);

        record.Id.Should().StartWith("cost:");
        record.Amount.Should().Be(120.25m);
        record.Timestamp.Should().Be(time.GetUtcNow());
        record.Source.Should().Be("plugin:cost-profit/manual");
        record.Quantity.Should().Be(5);
        record.QuantityUnits.Should().Be("bags");

        ledger.GetRecords().Should().ContainSingle(r => r.Id == record.Id);
    }

    [Fact]
    public void CaptureManualEntry_RespectsProvidedIdentifier()
    {
        var ledger = new CostLedger();
        var service = new CostEntryOrchestrationService(ledger);
        var draft = new CostEntryDraft(
            CreateScope(),
            CostCategory.Fuel,
            amount: 75.50m,
            currency: "USD",
            actor: "ops",
            recordId: " cost-123 ");

        var record = service.CaptureManualEntry(draft);

        record.Id.Should().Be("cost-123");
        record.Source.Should().Be("plugin:cost-profit/manual");
    }

    [Fact]
    public void CaptureManualEntry_DeduplicatesUsingExternalReference()
    {
        var ledger = new CostLedger();
        var service = new CostEntryOrchestrationService(ledger);
        var scope = CreateScope();

        var first = service.CaptureManualEntry(new CostEntryDraft(
            scope,
            CostCategory.Labour,
            200m,
            "USD",
            actor: "ops",
            externalReference: "ERP-123"));

        var duplicate = service.CaptureManualEntry(new CostEntryDraft(
            scope,
            CostCategory.Labour,
            200m,
            "USD",
            actor: "ops",
            externalReference: "erp-123"));

        duplicate.Should().BeSameAs(first);
        ledger.GetRecords().Should().HaveCount(1);
    }

    [Fact]
    public void ImportBatch_ReturnsUniqueCapturedRecords()
    {
        var ledger = new CostLedger();
        var time = new FakeTimeProvider(new DateTimeOffset(2025, 3, 21, 8, 0, 0, TimeSpan.Zero));
        var service = new CostEntryOrchestrationService(ledger, time);
        var scope = CreateScope();

        var drafts = new List<CostEntryDraft>
        {
            new(scope, CostCategory.Seed, 300m, "USD", "ops", recordId: "cost-batch-1"),
            new(scope, CostCategory.Fertilizer, 180m, "USD", "ops", externalReference: "ERP-456"),
            new(scope, CostCategory.Fertilizer, 180m, "USD", "ops", externalReference: "ERP-456")
        };

        var records = service.ImportBatch(drafts);

        records.Should().HaveCount(2);
        records.Should().OnlyHaveUniqueItems(r => r.Id);
        ledger.GetRecords().Should().HaveCount(2);
    }
}
