using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Aog.Core.Legacy;
using Aog.Core.Paths;
using Xunit;

namespace Aog.Core.Tests.Legacy;

public sealed class LegacyGuidanceImportServiceTests
{
    [Fact]
    public void Import_WithValidInputs_ProducesScenarioAndPlanarData()
    {
        using var temp = new TemporaryDirectory();
        var shapefilePath = System.IO.Path.Combine(temp.Path, "field.shp");
        var boundaryPoints = new List<(double Lon, double Lat)>
        {
            (-114.0005, 51.0000),
            (-113.9995, 51.0000),
            (-113.9995, 51.0010),
            (-114.0005, 51.0010),
            (-114.0005, 51.0000),
        };
        WritePolygonShapefile(shapefilePath, boundaryPoints);

        var csvPath = System.IO.Path.Combine(temp.Path, "ablines.csv");
        File.WriteAllText(csvPath, string.Join(Environment.NewLine, new[]
        {
            "Name,LatA,LonA,LatB,LonB",
            "Alpha,51.0002,-114.0004,51.0008,-114.0004",
            "Beta,51.0001,-114.0002,51.0006,-114.0000",
        }));

        using var csvStream = File.OpenRead(csvPath);
        var service = new LegacyGuidanceImportService();

        var result = service.Import("WestField", csvStream, shapefilePath);

        Assert.Equal("WestField", result.FieldName);
        Assert.Equal(2, result.AbLines.Count);
        Assert.Equal(4, result.Boundary.Count);

        var origin = result.Origin;
        Assert.Equal(51.0000, origin.LatitudeDeg, 6);
        Assert.Equal(-114.0005, origin.LongitudeDeg, 6);

        var firstLine = result.AbLines[0];
        Assert.Equal("Alpha", firstLine.Name);
        Assert.True(firstLine.LengthMeters > 0.0);
        Assert.InRange(firstLine.HeadingDegrees, 350, 10); // Near northbound

        var scenario = result.Scenario;
        Assert.Equal("legacy:westfield", scenario.ScenarioId);
        Assert.Contains(scenario.Routes, route => route.Stream == "pose" && route.Source == "legacy/udp/main_gps" && route.Mode == "hardware");
        Assert.Contains(scenario.Routes, route => route.Stream == "sections" && route.Source == "legacy/udp/sections");
    }

    private static void WritePolygonShapefile(string path, IReadOnlyList<(double Lon, double Lat)> points)
    {
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);

        using var stream = File.Create(path);
        using var writer = new BinaryWriter(stream);

        var xs = points.Select(p => p.Lon).ToArray();
        var ys = points.Select(p => p.Lat).ToArray();
        var numPoints = points.Count;
        const int numParts = 1;

        var recordContentBytes = 4 + 32 + 4 + 4 + (4 * numParts) + (16 * numPoints);
        var totalBytes = 100 + 8 + recordContentBytes;
        var fileLengthWords = totalBytes / 2;

        WriteBigEndian(writer, 9994);
        for (var i = 0; i < 5; i++)
        {
            writer.Write(0);
        }

        WriteBigEndian(writer, fileLengthWords);
        writer.Write(1000); // Version
        writer.Write(5);    // Polygon

        writer.Write(xs.Min());
        writer.Write(ys.Min());
        writer.Write(xs.Max());
        writer.Write(ys.Max());
        writer.Write(0.0); // Z min
        writer.Write(0.0); // Z max
        writer.Write(0.0); // M min
        writer.Write(0.0); // M max

        WriteBigEndian(writer, 1); // Record number
        WriteBigEndian(writer, recordContentBytes / 2);
        writer.Write(5); // Polygon type

        writer.Write(xs.Min());
        writer.Write(ys.Min());
        writer.Write(xs.Max());
        writer.Write(ys.Max());
        writer.Write(numParts);
        writer.Write(numPoints);
        writer.Write(0); // First part offset

        foreach (var point in points)
        {
            writer.Write(point.Lon);
            writer.Write(point.Lat);
        }
    }

    private static void WriteBigEndian(BinaryWriter writer, int value)
    {
        var bytes = BitConverter.GetBytes(value);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(bytes);
        }

        writer.Write(bytes);
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "nexus-guidance-" + Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(Path))
                {
                    Directory.Delete(Path, recursive: true);
                }
            }
            catch
            {
                // Best-effort cleanup.
            }
        }
    }
}
