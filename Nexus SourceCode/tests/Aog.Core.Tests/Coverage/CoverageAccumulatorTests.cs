using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Aog.Core.Coverage;
using FluentAssertions;
using Xunit;

namespace Aog.Core.Tests.Coverage;

public sealed class CoverageAccumulatorTests
{
    private static readonly string DataRoot = Path.Combine("Coverage", "Data");

    [Fact]
    public void AddSample_ReplaysLegacyTriangleStrips()
    {
        var accumulator = new CoverageAccumulator();
        var rows = LoadSampleRows("LegacyPatchSamples.csv");

        foreach (var row in rows)
        {
            switch (row.Stage)
            {
                case SampleStage.Start:
                    accumulator.BeginPatch(row.Left, row.Right);
                    break;
                case SampleStage.Continue:
                    accumulator.AddSample(row.Left, row.Right, row.IncludeInUserTotals);
                    break;
                case SampleStage.End:
                    accumulator.AddSample(row.Left, row.Right, row.IncludeInUserTotals);
                    accumulator.EndPatch();
                    break;
            }
        }

        accumulator.TotalAreaSquareMeters.Should().BeApproximately(248.0, 1e-9);
        accumulator.UserAreaSquareMeters.Should().BeApproximately(200.0, 1e-9);
        accumulator.PatchCount.Should().Be(3);
    }

    [Fact]
    public void CoveragePercent_UsesFieldArea()
    {
        var accumulator = new CoverageAccumulator
        {
            FieldAreaSquareMeters = 500,
        };

        accumulator.BeginPatch(new PlanarPoint(0, 0), new PlanarPoint(5, 0));
        accumulator.AddSample(new PlanarPoint(0, 10), new PlanarPoint(5, 10));
        accumulator.EndPatch();

        accumulator.TotalAreaSquareMeters.Should().Be(50);
        accumulator.RemainingAreaSquareMeters.Should().Be(450);
        accumulator.CoveragePercent.Should().BeApproximately(10.0, 1e-9);
    }

    [Fact]
    public void GuardClauses_AreEnforced()
    {
        var accumulator = new CoverageAccumulator();

        var begin = () => accumulator.BeginPatch(new PlanarPoint(double.NaN, 0), new PlanarPoint(0, 0));
        begin.Should().Throw<ArgumentOutOfRangeException>();

        accumulator.BeginPatch(new PlanarPoint(0, 0), new PlanarPoint(5, 0));
        accumulator.Invoking(a => a.BeginPatch(new PlanarPoint(0, 0), new PlanarPoint(5, 0)))
            .Should().Throw<InvalidOperationException>();

        accumulator.Invoking(a => a.AddSample(new PlanarPoint(double.PositiveInfinity, 0), new PlanarPoint(5, 10)))
            .Should().Throw<ArgumentOutOfRangeException>();

        accumulator.AddSample(new PlanarPoint(0, 10), new PlanarPoint(5, 10));
        accumulator.EndPatch();

        accumulator.Invoking(a => a.EndPatch()).Should().Throw<InvalidOperationException>();
    }

    private static IReadOnlyList<SampleRow> LoadSampleRows(string fileName)
    {
        var path = Path.Combine(DataRoot, fileName);
        var fullPath = Path.Combine(AppContext.BaseDirectory, path);
        var lines = File.ReadAllLines(fullPath);
        var rows = new List<SampleRow>(lines.Length - 1);

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#", StringComparison.Ordinal))
            {
                continue;
            }

            var parts = line.Split(',', StringSplitOptions.TrimEntries);
            if (parts.Length != 7)
            {
                throw new InvalidDataException($"Unexpected column count in row: {line}");
            }

            var stage = parts[1] switch
            {
                "start" => SampleStage.Start,
                "continue" => SampleStage.Continue,
                "end" => SampleStage.End,
                _ => throw new InvalidDataException($"Unknown stage '{parts[1]}' in row: {line}"),
            };

            var row = new SampleRow
            {
                PatchId = int.Parse(parts[0], CultureInfo.InvariantCulture),
                Stage = stage,
                Left = new PlanarPoint(
                    double.Parse(parts[2], CultureInfo.InvariantCulture),
                    double.Parse(parts[3], CultureInfo.InvariantCulture)),
                Right = new PlanarPoint(
                    double.Parse(parts[4], CultureInfo.InvariantCulture),
                    double.Parse(parts[5], CultureInfo.InvariantCulture)),
                IncludeInUserTotals = bool.Parse(parts[6]),
            };

            rows.Add(row);
        }

        return rows;
    }

    private sealed record SampleRow
    {
        public int PatchId { get; init; }
        public SampleStage Stage { get; init; }
        public PlanarPoint Left { get; init; }
        public PlanarPoint Right { get; init; }
        public bool IncludeInUserTotals { get; init; }
    }

    private enum SampleStage
    {
        Start,
        Continue,
        End,
    }
}
