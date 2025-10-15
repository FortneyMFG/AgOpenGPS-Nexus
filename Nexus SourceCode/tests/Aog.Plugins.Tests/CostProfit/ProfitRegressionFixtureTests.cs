using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Aog.Core.Paths;
using Aog.Plugins.CostProfit;
using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Aog.Plugins.Tests.CostProfit;

public sealed class ProfitRegressionFixtureTests
{
    private static readonly string FixturePath = Path.Combine("CostProfit", "Data", "ProfitRegressionFixture.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
        }
    };

    [Fact]
    public void ProfitPipelines_MatchGoldenRegressionFixture()
    {
        var fullPath = Path.Combine(AppContext.BaseDirectory, FixturePath);
        File.Exists(fullPath).Should().BeTrue($"Fixture '{FixturePath}' should be copied to the test output directory.");

        var json = File.ReadAllText(fullPath);
        var document = JsonSerializer.Deserialize<FixtureDocument>(json, JsonOptions);
        document.Should().NotBeNull();
        document!.Scenarios.Should().NotBeNull();
        document.Scenarios.Should().NotBeEmpty();

        foreach (var scenario in document.Scenarios)
        {
            ValidateScenario(scenario);
        }
    }

    private static void ValidateScenario(FixtureScenario scenario)
    {
        var ledger = new CostLedger();
        foreach (var record in scenario.CostRecords)
        {
            ledger.RecordCost(record.ToCostRecord());
        }

        var filter = scenario.Filter.ToFilter();
        var revenue = scenario.Revenue.Select(r => r.ToContribution()).ToList();
        var rollup = new ProfitAnalyticsRollupService(ledger).CreateRollup(filter, revenue);

        var options = scenario.Options.ToOptions();
        var timeProvider = new FakeTimeProvider(scenario.Expected.Layer.CreatedAt);
        var pipeline = new ProfitExportPipeline(options, timeProvider);
        var cells = scenario.Cells.Select(cell => cell.ToProfitCell()).ToList();
        var layer = pipeline.CreateProfitLayer(cells);

        layer.LayerId.Should().Be(options.LayerId, scenario.Name);
        layer.Kind.Should().Be(options.LayerKind);
        layer.Units.Should().Be(options.Units);
        layer.CreatedAt.Should().Be(scenario.Expected.Layer.CreatedAt);
        layer.CreatedBy.Should().Be(options.CreatedBy);
        layer.Provenance.Source.Should().Be(options.Source);
        layer.Provenance.Transform.Should().Be(options.Transform);
        layer.Provenance.Actor.Should().Be(options.Actor);
        layer.Provenance.Hash.Should().Be(scenario.Expected.Layer.Hash);

        layer.Cells.Should().HaveCount(scenario.Expected.Layer.Cells.Length);
        for (var i = 0; i < scenario.Expected.Layer.Cells.Length; i++)
        {
            var expected = scenario.Expected.Layer.Cells[i];
            var actual = layer.Cells[i];

            actual.Position.Easting.Should().BeApproximately(expected.Easting, 1e-6, scenario.Name);
            actual.Position.Northing.Should().BeApproximately(expected.Northing, 1e-6, scenario.Name);
            actual.CellSizeMeters.Should().BeApproximately(expected.Size, 1e-6, scenario.Name);
            actual.Value.Should().BeApproximately((double)expected.Profit, 1e-6, scenario.Name);
        }

        var rows = pipeline.CreateSummaryRows(rollup);
        rows.Should().HaveSameCount(scenario.Expected.SummaryRows);
        for (var i = 0; i < scenario.Expected.SummaryRows.Length; i++)
        {
            var expected = scenario.Expected.SummaryRows[i];
            var actual = rows[i];

            actual.Currency.Should().Be(expected.Currency, scenario.Name);
            actual.Revenue.Should().Be(expected.Revenue);
            actual.Cost.Should().Be(expected.Cost);
            actual.Profit.Should().Be(expected.Profit);
            actual.Margin.Should().Be(expected.Margin);
        }
    }

    private sealed record FixtureDocument(FixtureScenario[] Scenarios);

    private sealed record FixtureScenario(
        string Name,
        ExportOptionsModel Options,
        ScopeFilterModel Filter,
        CostRecordModel[] CostRecords,
        RevenueContributionModel[] Revenue,
        ProfitCellModel[] Cells,
        ExpectedModel Expected);

    private sealed record ExportOptionsModel(
        string LayerId,
        string LayerKind,
        string Units,
        string Source,
        string Transform,
        string Actor,
        string CreatedBy)
    {
        public ProfitExportOptions ToOptions() => new()
        {
            LayerId = LayerId,
            LayerKind = LayerKind,
            Units = Units,
            Source = Source,
            Transform = Transform,
            Actor = Actor,
            CreatedBy = CreatedBy
        };
    }

    private sealed record ScopeFilterModel(string FarmId, string? FieldId, string? JobId, string? SessionId)
    {
        public CostScopeFilter ToFilter() => new()
        {
            FarmId = FarmId,
            FieldId = FieldId,
            JobId = JobId,
            SessionId = SessionId
        };
    }

    private sealed record CostRecordModel(
        string Id,
        CostCategory Category,
        decimal Amount,
        string Currency,
        DateTimeOffset Timestamp,
        string Actor,
        ScopeModel Scope)
    {
        public CostRecord ToCostRecord()
        {
            return new CostRecord(Id, Scope.ToScope(), Category, Amount, Currency, Timestamp, Actor);
        }
    }

    private sealed record RevenueContributionModel(
        string Id,
        decimal Amount,
        string Currency,
        DateTimeOffset Timestamp,
        string Source,
        string Actor,
        ScopeModel Scope)
    {
        public RevenueContribution ToContribution()
        {
            return new RevenueContribution(Id, Scope.ToScope(), Amount, Currency, Timestamp, Source, Actor);
        }
    }

    private sealed record ProfitCellModel(double Easting, double Northing, double Size, decimal Revenue, decimal Cost)
    {
        public ProfitCell ToProfitCell()
        {
            return new ProfitCell(new PlanarPoint(Easting, Northing), Size, Revenue, Cost);
        }
    }

    private sealed record ExpectedModel(ExpectedLayerModel Layer, ExpectedSummaryRow[] SummaryRows);

    private sealed record ExpectedLayerModel(DateTimeOffset CreatedAt, string Hash, ExpectedCell[] Cells);

    private sealed record ExpectedCell(double Easting, double Northing, double Size, decimal Profit);

    private sealed record ExpectedSummaryRow(string Currency, decimal Revenue, decimal Cost, decimal Profit, decimal Margin);

    private sealed record ScopeModel(string FarmId, string? FieldId, string? JobId, string? SessionId)
    {
        public CostScope ToScope() => new(FarmId, FieldId, JobId, SessionId);
    }
}
