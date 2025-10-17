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
        WritePolygonShapefile(shapefilePath, new[] { boundaryPoints });

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

    [Fact]
    public void Import_WithMultiPartPolygon_PreservesAllExteriorVertices()
    {
        using var temp = new TemporaryDirectory();
        var shapefilePath = System.IO.Path.Combine(temp.Path, "multipart.shp");

        var outerOne = new List<(double Lon, double Lat)>
        {
            (-114.0005, 51.0000),
            (-114.0005, 51.0010),
            (-113.9995, 51.0010),
            (-113.9995, 51.0000),
            (-114.0005, 51.0000),
        };

        var innerHole = new List<(double Lon, double Lat)>
        {
            (-114.0003, 51.0002),
            (-113.9997, 51.0002),
            (-113.9997, 51.0008),
            (-114.0003, 51.0008),
            (-114.0003, 51.0002),
        };

        var outerTwo = new List<(double Lon, double Lat)>
        {
            (-114.0010, 51.0020),
            (-114.0010, 51.0030),
            (-114.0000, 51.0030),
            (-114.0000, 51.0020),
            (-114.0010, 51.0020),
        };

        WritePolygonShapefile(shapefilePath, new[] { outerOne, innerHole, outerTwo });

        using var csvStream = new MemoryStream();
        var service = new LegacyGuidanceImportService();

        var result = service.Import("MultiField", csvStream, shapefilePath);

        var expected = outerOne.Take(outerOne.Count - 1)
            .Concat(outerTwo.Take(outerTwo.Count - 1))
            .Select(p => new GeographicCoordinate(p.Lat, p.Lon))
            .ToList();

        Assert.Equal(expected.Count, result.Boundary.Count);

        for (var i = 0; i < expected.Count; i++)
        {
            Assert.Equal(expected[i].LatitudeDeg, result.Boundary[i].LatitudeDeg, 9);
            Assert.Equal(expected[i].LongitudeDeg, result.Boundary[i].LongitudeDeg, 9);
        }

        var holeVertex = innerHole[0];
        Assert.DoesNotContain(result.Boundary, coordinate =>
            Math.Abs(coordinate.LatitudeDeg - holeVertex.Lat) < 1e-9 &&
            Math.Abs(coordinate.LongitudeDeg - holeVertex.Lon) < 1e-9);
    }

    private static void WritePolygonShapefile(string path, IReadOnlyList<IReadOnlyList<(double Lon, double Lat)>> parts)
    {
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);

        using var stream = File.Create(path);
        using var writer = new BinaryWriter(stream);

        var allPoints = parts.SelectMany(p => p).ToArray();
        var xs = allPoints.Select(p => p.Lon).ToArray();
        var ys = allPoints.Select(p => p.Lat).ToArray();
        var numPoints = allPoints.Length;
        var numParts = parts.Count;

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

        var pointOffset = 0;
        foreach (var part in parts)
        {
            writer.Write(pointOffset);
            pointOffset += part.Count;
        }

        foreach (var part in parts)
        {
            foreach (var point in part)
            {
                writer.Write(point.Lon);
                writer.Write(point.Lat);
            }
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
