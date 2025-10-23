using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Aog.Core.Eventing;
using Aog.Core.Layers;
using Aog.Core.V1;
using Aog.Plugins.CombineYield;
using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Aog.Plugins.Tests.CombineYield;

public sealed class YieldRegressionFixtureTests
{
    private static readonly string FixturePath = Path.Combine("CombineYield", "Data", "YieldRegressionFixture.json");

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
    public void CombineYieldAggregator_MatchesRegressionFixture()
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
        scenario.Should().NotBeNull();

        var options = scenario.Options.ToOptions();
        var timeProvider = new FakeTimeProvider(scenario.Timestamp);
        var aggregator = new CombineYieldLayerAggregator(new InMemoryEventBus(), options, timeProvider);

        foreach (var measurement in scenario.Measurements)
        {
            aggregator.IngestAsync(measurement.ToMeasurement()).GetAwaiter().GetResult();
        }

        var snapshot = aggregator.CreateLayerSnapshot();
        var expected = scenario.Expected;

        snapshot.Should().NotBeNull();
        expected.Should().NotBeNull();

        ValidateLayer(snapshot.Layer, expected.Layer, scenario.Name);
        ValidateProvenance(snapshot.Provenance, expected.Provenance, scenario.Name);
        ValidateMetadata(snapshot.Metadata, expected.Metadata, scenario.Name);
    }

    private static void ValidateLayer(CombineYieldLayer layer, ExpectedLayerModel expected, string scenarioName)
    {
        layer.Should().NotBeNull();
        expected.Should().NotBeNull();

        layer.Header.Sequence.Should().Be(expected.Header.Sequence, scenarioName);
        layer.Header.Source.Should().Be(expected.Header.Source, scenarioName);
        layer.Header.Frame.Should().Be(expected.Header.Frame, scenarioName);
        layer.Header.Timestamp.ToDateTimeOffset().Should().Be(expected.Header.Timestamp, scenarioName);

        layer.Crop.Should().Be(expected.Crop, scenarioName);
        layer.CellSizeMeters.Should().BeApproximately(expected.CellSizeMeters, 1e-6, scenarioName);

        layer.Cells.Should().HaveCount(expected.Cells.Length, scenarioName);
        for (var i = 0; i < expected.Cells.Length; i++)
        {
            var actual = layer.Cells[i];
            var expectedCell = expected.Cells[i];

            actual.Column.Should().Be((uint)expectedCell.Column, scenarioName);
            actual.Row.Should().Be((uint)expectedCell.Row, scenarioName);
            actual.AverageYieldKgPerHectare.Should().BeApproximately(expectedCell.AverageYieldKgPerHectare, 1e-6, scenarioName);
            actual.AverageMoisturePercent.Should().BeApproximately(expectedCell.AverageMoisturePercent, 1e-6, scenarioName);
            actual.SampleCount.Should().Be((uint)expectedCell.SampleCount, scenarioName);
        }
    }

    private static void ValidateProvenance(LayerProvenance provenance, ExpectedProvenanceModel expected, string scenarioName)
    {
        provenance.Should().NotBeNull();
        expected.Should().NotBeNull();

        provenance.Source.Should().Be(expected.Source, scenarioName);
        provenance.Transform.Should().Be(expected.Transform, scenarioName);
        provenance.Actor.Should().Be(expected.Actor, scenarioName);
        provenance.Hash.Should().Be(expected.Hash, scenarioName);
        provenance.CreatedAt.Should().Be(expected.CreatedAt, scenarioName);
    }

    private static void ValidateMetadata(YieldLayerMetadata metadata, ExpectedMetadataModel expected, string scenarioName)
    {
        metadata.Should().NotBeNull();
        expected.Should().NotBeNull();

        metadata.Grid.CellSizeMeters.Should().BeApproximately(expected.Grid.CellSizeMeters, 1e-6, scenarioName);
        metadata.Grid.Projection.Should().Be(expected.Grid.Projection, scenarioName);

        metadata.Smoothing.Method.Should().Be(expected.Smoothing.Method, scenarioName);
        metadata.Smoothing.WindowSeconds.Should().BeApproximately(expected.Smoothing.WindowSeconds, 1e-6, scenarioName);
        metadata.Smoothing.LagCompensationSeconds.Should().BeApproximately(expected.Smoothing.LagCompensationSeconds, 1e-6, scenarioName);
        metadata.Smoothing.Passes.Should().Be(expected.Smoothing.Passes, scenarioName);

        metadata.Calibration.ProfileId.Should().Be(expected.Calibration.ProfileId, scenarioName);
        metadata.Calibration.AppliedAt.Should().Be(expected.Calibration.AppliedAt, scenarioName);
        metadata.Calibration.Source.Should().Be(expected.Calibration.Source, scenarioName);
        metadata.Calibration.SensorModel.Should().Be(expected.Calibration.SensorModel, scenarioName);
        metadata.Calibration.Notes.Should().Be(expected.Calibration.Notes, scenarioName);
        metadata.Calibration.Factors.Should().BeEquivalentTo(expected.Calibration.Factors, scenarioName);

        metadata.Aggregation.Basis.Should().Be(expected.Aggregation.Basis, scenarioName);
        metadata.Aggregation.Scopes.Should().Equal(expected.Aggregation.Scopes, scenarioName);
        metadata.Aggregation.UpdatedAt.Should().Be(expected.Aggregation.Timestamp, scenarioName);

        metadata.Aggregation.Bins.Should().NotBeNull(scenarioName);
        metadata.Aggregation.Bins.Scheme.Should().Be(expected.Aggregation.Bins.Scheme, scenarioName);
        metadata.Aggregation.Bins.Count.Should().Be(expected.Aggregation.Bins.Count, scenarioName);

        var actualBreaks = metadata.Aggregation.Bins.Breaks ?? Array.Empty<double>();
        actualBreaks.Should().HaveCount(expected.Aggregation.Bins.Breaks.Length, scenarioName);
        for (var i = 0; i < expected.Aggregation.Bins.Breaks.Length; i++)
        {
            actualBreaks[i].Should().BeApproximately(expected.Aggregation.Bins.Breaks[i], 1e-6, scenarioName);
        }

        if (expected.Aggregation.Bins.Labels is null)
        {
            metadata.Aggregation.Bins.Labels.Should().BeNull(scenarioName);
        }
        else
        {
            metadata.Aggregation.Bins.Labels.Should().Equal(expected.Aggregation.Bins.Labels, scenarioName);
        }

        metadata.Statistics.SampleCount.Should().Be(expected.Statistics.SampleCount, scenarioName);
        metadata.Statistics.Mean.Should().BeApproximately(expected.Statistics.Mean, 1e-6, scenarioName);
        metadata.Statistics.Median.Should().BeApproximately(expected.Statistics.Median, 1e-6, scenarioName);
        metadata.Statistics.StdDev.Should().BeApproximately(expected.Statistics.StdDev, 1e-6, scenarioName);
        metadata.Statistics.Min.Should().BeApproximately(expected.Statistics.Min, 1e-6, scenarioName);
        metadata.Statistics.Max.Should().BeApproximately(expected.Statistics.Max, 1e-6, scenarioName);
        metadata.Statistics.TotalMassKg.Should().BeApproximately(expected.Statistics.TotalMassKg, 1e-6, scenarioName);
    }

    private sealed record FixtureDocument(FixtureScenario[] Scenarios);

    private sealed record FixtureScenario(
        string Name,
        DateTimeOffset Timestamp,
        OptionsModel Options,
        MeasurementModel[] Measurements,
        ExpectedModel Expected);

    private sealed record OptionsModel(
        double CellSizeMeters,
        double PublishIntervalSeconds,
        string Source,
        string Transform,
        string Actor,
        string Frame,
        string Crop,
        string Projection,
        string CalibrationProfileId,
        DateTimeOffset? CalibrationAppliedAt,
        string? CalibrationSource,
        string? CalibrationSensorModel,
        string? CalibrationNotes,
        Dictionary<string, double> CalibrationFactors,
        string SmoothingMethod,
        int SmoothingKernelSize,
        double? SmoothingWindowSeconds,
        double? SmoothingLagCompensationSeconds,
        int SmoothingPasses,
        double OutlierClampFraction,
        string AggregationBasis,
        string[] AggregationScopes,
        string BinningScheme,
        int BinningBinCount,
        double[]? CustomBinBreaks,
        string[]? CustomBinLabels)
    {
        public CombineYieldOptions ToOptions()
        {
            var options = new CombineYieldOptions
            {
                CellSizeMeters = CellSizeMeters,
                PublishInterval = TimeSpan.FromSeconds(PublishIntervalSeconds),
                Source = Source,
                Transform = Transform,
                Actor = Actor,
                Frame = Frame,
                Crop = Crop,
                Projection = Projection,
                CalibrationProfileId = CalibrationProfileId,
                CalibrationAppliedAt = CalibrationAppliedAt,
                CalibrationSource = CalibrationSource,
                CalibrationSensorModel = CalibrationSensorModel,
                CalibrationNotes = CalibrationNotes,
                SmoothingMethod = SmoothingMethod,
                SmoothingKernelSize = SmoothingKernelSize,
                SmoothingWindowSeconds = SmoothingWindowSeconds,
                SmoothingLagCompensationSeconds = SmoothingLagCompensationSeconds,
                SmoothingPasses = SmoothingPasses,
                OutlierClampFraction = OutlierClampFraction,
                AggregationBasis = AggregationBasis,
                AggregationScopes = AggregationScopes,
                BinningScheme = Enum.Parse<YieldBinningScheme>(BinningScheme, ignoreCase: true),
                BinningBinCount = BinningBinCount,
                CustomBinBreaks = CustomBinBreaks,
                CustomBinLabels = CustomBinLabels
            };

            options.CalibrationFactors.Clear();
            foreach (var entry in CalibrationFactors)
            {
                options.CalibrationFactors[entry.Key] = entry.Value;
            }

            return options;
        }
    }

    private sealed record MeasurementModel(
        double EastingMeters,
        double NorthingMeters,
        double YieldKgPerHectare,
        double? MoisturePercent)
    {
        public CombineYieldMeasurement ToMeasurement()
        {
            return new CombineYieldMeasurement(EastingMeters, NorthingMeters, YieldKgPerHectare, MoisturePercent);
        }
    }

    private sealed record ExpectedModel(
        ExpectedLayerModel Layer,
        ExpectedProvenanceModel Provenance,
        ExpectedMetadataModel Metadata);

    private sealed record ExpectedLayerModel(
        ExpectedHeaderModel Header,
        string Crop,
        double CellSizeMeters,
        ExpectedCellModel[] Cells);

    private sealed record ExpectedHeaderModel(
        ulong Sequence,
        string Source,
        string Frame,
        DateTimeOffset Timestamp);

    private sealed record ExpectedCellModel(
        int Column,
        int Row,
        double AverageYieldKgPerHectare,
        double AverageMoisturePercent,
        uint SampleCount);

    private sealed record ExpectedProvenanceModel(
        string Source,
        string Transform,
        string Actor,
        string Hash,
        DateTimeOffset CreatedAt);

    private sealed record ExpectedMetadataModel(
        ExpectedGridModel Grid,
        ExpectedSmoothingModel Smoothing,
        ExpectedCalibrationModel Calibration,
        ExpectedAggregationModel Aggregation,
        ExpectedStatisticsModel Statistics);

    private sealed record ExpectedGridModel(double CellSizeMeters, string Projection);

    private sealed record ExpectedSmoothingModel(
        string Method,
        double WindowSeconds,
        double LagCompensationSeconds,
        int Passes);

    private sealed record ExpectedCalibrationModel(
        string ProfileId,
        DateTimeOffset? AppliedAt,
        string? Source,
        string? SensorModel,
        string? Notes,
        Dictionary<string, double> Factors);

    private sealed record ExpectedAggregationModel(
        string Basis,
        string[] Scopes,
        DateTimeOffset Timestamp,
        ExpectedBinsModel Bins);

    private sealed record ExpectedBinsModel(
        string Scheme,
        int Count,
        double[] Breaks,
        string[]? Labels);

    private sealed record ExpectedStatisticsModel(
        int SampleCount,
        double Mean,
        double Median,
        double StdDev,
        double Min,
        double Max,
        double TotalMassKg);
}
