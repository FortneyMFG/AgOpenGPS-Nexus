using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Aog.Core.Layers;
using Aog.Core.Paths;
using FluentAssertions;
using Xunit;

namespace Aog.Core.Tests.Layers;

public sealed class ExternalAgronomicMapIngestorTests
{
    [Fact]
    public void LoadFromDelimitedFile_ProducesLayerDocument()
    {
        var timestamp = new DateTimeOffset(2024, 03, 18, 12, 30, 00, TimeSpan.Zero);
        var ingestor = new ExternalAgronomicMapIngestor(() => timestamp);
        var path = Path.Combine(AppContext.BaseDirectory, "Layers", "Data", "sample-rate.csv");

        var document = ingestor.LoadFromDelimitedFile(
            path,
            layerId: "layer:rate.sample",
            kind: "rate",
            units: "L/ha",
            cellSizeMeters: 12,
            source: "import:legacy.csv",
            transform: "resample:nearest",
            createdBy: "user:test");

        document.LayerId.Should().Be("layer:rate.sample");
        document.Kind.Should().Be("rate");
        document.Units.Should().Be("L/ha");
        document.CreatedAt.Should().Be(timestamp);
        document.CreatedBy.Should().Be("user:test");
        document.Cells.Should().HaveCount(4);
        document.Cells.Select(cell => cell.CellSizeMeters).Should().OnlyContain(size => Math.Abs(size - 12) < 1e-6);
        document.Cells.Select(cell => cell.Position).Should().Contain(new PlanarPoint(12, 12));

        document.Provenance.Source.Should().Be("import:legacy.csv");
        document.Provenance.Transform.Should().Be("resample:nearest");
        document.Provenance.CreatedAt.Should().Be(timestamp);
        document.Provenance.Actor.Should().Be("user:test");

        var expectedHash = ComputeExpectedHash("layer:rate.sample", document.Cells);
        document.Provenance.Hash.Should().Be(expectedHash);
    }

    [Theory]
    [InlineData('\t')]
    [InlineData(';')]
    public void LoadFromDelimitedFile_DetectsSeparators(char separator)
    {
        var timestamp = new DateTimeOffset(2024, 03, 18, 12, 30, 00, TimeSpan.Zero);
        var ingestor = new ExternalAgronomicMapIngestor(() => timestamp);
        var path = CreateTempDelimitedFile(separator, new[]
        {
            $"easting{separator}northing{separator}value",
            $"0{separator}0{separator}42",
            $"12{separator}12{separator}84",
        });

        try
        {
            var document = ingestor.LoadFromDelimitedFile(
                path,
                layerId: "layer:separator",
                kind: "rate",
                units: "L/ha",
                cellSizeMeters: 12,
                source: "import:legacy.csv",
                transform: "resample:nearest",
                createdBy: "user:test");

            document.Cells.Should().HaveCount(2);
            document.Cells.Select(cell => cell.Value).Should().Contain(new[] { 42d, 84d });
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void LoadFromDelimitedFile_ThrowsOnMalformedRow()
    {
        var timestamp = new DateTimeOffset(2024, 03, 18, 12, 30, 00, TimeSpan.Zero);
        var ingestor = new ExternalAgronomicMapIngestor(() => timestamp);
        var path = CreateTempDelimitedFile(',', new[]
        {
            "easting,northing,value",
            "0,0,42",
            "1,1",
        });

        try
        {
            var act = () => ingestor.LoadFromDelimitedFile(
                path,
                layerId: "layer:separator",
                kind: "rate",
                units: "L/ha",
                cellSizeMeters: 12,
                source: "import:legacy.csv",
                transform: "resample:nearest",
                createdBy: "user:test");

            act.Should().Throw<InvalidDataException>()
                .WithMessage("*Row '1,1' does not contain easting,northing,value.*");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void LoadFromDelimitedFile_FallsBackToSystemActorWhenMissing()
    {
        var timestamp = new DateTimeOffset(2024, 03, 18, 12, 30, 00, TimeSpan.Zero);
        var ingestor = new ExternalAgronomicMapIngestor(() => timestamp);
        var path = CreateTempDelimitedFile(',', new[]
        {
            "easting,northing,value",
            "0,0,42",
        });

        try
        {
            var document = ingestor.LoadFromDelimitedFile(
                path,
                layerId: "layer:separator",
                kind: "rate",
                units: "L/ha",
                cellSizeMeters: 12,
                source: "import:legacy.csv",
                transform: "resample:nearest",
                createdBy: string.Empty);

            document.CreatedBy.Should().Be("system:nexus");
            document.Provenance.Actor.Should().Be(string.Empty);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static string ComputeExpectedHash(string layerId, IReadOnlyList<AgronomicLayerCell> cells)
    {
        using var sha = SHA256.Create();
        var builder = new StringBuilder();
        builder.Append(layerId);
        builder.Append('|');
        foreach (var cell in cells.OrderBy(c => c.Position.Easting).ThenBy(c => c.Position.Northing))
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
        return Convert.ToHexString(sha.ComputeHash(bytes));
    }

    private static string CreateTempDelimitedFile(char separator, IEnumerable<string> lines)
    {
        var path = Path.Combine(Path.GetTempPath(), $"nexus-layer-{Guid.NewGuid():N}.txt");
        File.WriteAllLines(path, lines);
        return path;
    }
}
