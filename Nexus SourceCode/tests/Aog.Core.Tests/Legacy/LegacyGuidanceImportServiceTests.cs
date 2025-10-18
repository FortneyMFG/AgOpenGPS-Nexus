using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Aog.Core.Legacy;
using CoveragePlanarPoint = Aog.Core.Coverage.PlanarPoint;
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

        var expectedPlanar = boundaryPoints
            .Take(boundaryPoints.Count - 1)
            .Select(point => ToPlanar(point.Lat, point.Lon, origin))
            .ToList();

        for (var i = 0; i < expectedPlanar.Count; i++)
        {
            Assert.Equal(expectedPlanar[i].Easting, result.Boundary[i].Easting, 3);
            Assert.Equal(expectedPlanar[i].Northing, result.Boundary[i].Northing, 3);
        }

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
            .Select(p => ToPlanar(p.Lat, p.Lon, result.Origin))
            .ToList();

        Assert.Equal(expected.Count, result.Boundary.Count);

        for (var i = 0; i < expected.Count; i++)
        {
            Assert.Equal(expected[i].Easting, result.Boundary[i].Easting, 3);
            Assert.Equal(expected[i].Northing, result.Boundary[i].Northing, 3);
        }

        var holePlanar = ToPlanar(innerHole[0].Lat, innerHole[0].Lon, result.Origin);
        Assert.DoesNotContain(result.Boundary, coordinate => NearlyEquals(coordinate, holePlanar));
    }

    [Fact]
    public void Import_WithMultiplePolygonRecords_CombinesPerimeters()
    {
        using var temp = new TemporaryDirectory();
        var shapefilePath = System.IO.Path.Combine(temp.Path, "multi-record.shp");

        var firstRecordRing = new List<(double Lon, double Lat)>
        {
            (-114.0020, 51.0040),
            (-114.0020, 51.0050),
            (-114.0010, 51.0050),
            (-114.0010, 51.0040),
            (-114.0020, 51.0040),
        };

        var secondRecordRing = new List<(double Lon, double Lat)>
        {
            (-114.0000, 51.0000),
            (-114.0000, 51.0010),
            (-113.9990, 51.0010),
            (-113.9990, 51.0000),
            (-114.0000, 51.0000),
        };

        IReadOnlyList<IReadOnlyList<(double Lon, double Lat)>> firstRecord = new[] { firstRecordRing };
        IReadOnlyList<IReadOnlyList<(double Lon, double Lat)>> secondRecord = new[] { secondRecordRing };

        WritePolygonShapefile(shapefilePath, new[] { firstRecord, secondRecord });

        using var csvStream = new MemoryStream();
        var service = new LegacyGuidanceImportService();

        var result = service.Import("MultiRecord", csvStream, shapefilePath);

        var expected = firstRecordRing.Take(firstRecordRing.Count - 1)
            .Concat(secondRecordRing.Take(secondRecordRing.Count - 1))
            .Select(p => ToPlanar(p.Lat, p.Lon, result.Origin))
            .ToList();

        var boundary = result.Boundary.ToList();
        Assert.Equal(expected.Count, boundary.Count);

        for (var i = 0; i < expected.Count; i++)
        {
            Assert.Equal(expected[i].Easting, boundary[i].Easting, 3);
            Assert.Equal(expected[i].Northing, boundary[i].Northing, 3);
        }

        var secondRecordFirstVertex = secondRecordRing[0];
        var secondRecordPlanar = ToPlanar(secondRecordFirstVertex.Lat, secondRecordFirstVertex.Lon, result.Origin);
        var indexOfSecondRecord = boundary.FindIndex(point => NearlyEquals(point, secondRecordPlanar));

        Assert.InRange(indexOfSecondRecord, firstRecordRing.Count - 1, boundary.Count - secondRecordRing.Count + 1);
    }

    private static CoveragePlanarPoint ToPlanar(double latitudeDeg, double longitudeDeg, GeographicCoordinate origin)
    {
        const double EarthRadiusMeters = 6_378_137.0;
        var latRad = DegreesToRadians(latitudeDeg);
        var lonRad = DegreesToRadians(longitudeDeg);
        var originLatRad = DegreesToRadians(origin.LatitudeDeg);
        var originLonRad = DegreesToRadians(origin.LongitudeDeg);

        var easting = (lonRad - originLonRad) * Math.Cos((latRad + originLatRad) / 2.0) * EarthRadiusMeters;
        var northing = (latRad - originLatRad) * EarthRadiusMeters;
        return new CoveragePlanarPoint(easting, northing);
    }

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180.0;

    private static bool NearlyEquals(CoveragePlanarPoint left, CoveragePlanarPoint right, double toleranceMeters = 0.01)
    {
        return Math.Abs(left.Easting - right.Easting) <= toleranceMeters &&
               Math.Abs(left.Northing - right.Northing) <= toleranceMeters;
    }

    private static void WritePolygonShapefile(string path, IReadOnlyList<IReadOnlyList<(double Lon, double Lat)>> parts)
    {
        WritePolygonShapefile(path, new[] { parts });
    }

    private static void WritePolygonShapefile(string path, IReadOnlyList<IReadOnlyList<(double Lon, double Lat)>>[] records)
    {
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);

        using var stream = File.Create(path);
        using var writer = new BinaryWriter(stream);

        var allPoints = records.SelectMany(record => record.SelectMany(part => part)).ToArray();
        if (allPoints.Length == 0)
        {
            throw new InvalidOperationException("Shapefile must contain at least one point.");
        }

        var xs = allPoints.Select(p => p.Lon).ToArray();
        var ys = allPoints.Select(p => p.Lat).ToArray();

        var recordContentBytes = new int[records.Length];
        var totalRecordBytes = 0;

        for (var i = 0; i < records.Length; i++)
        {
            var parts = records[i];
            var numParts = parts.Count;
            var numPoints = parts.Sum(part => part.Count);
            recordContentBytes[i] = 4 + 32 + 4 + 4 + (4 * numParts) + (16 * numPoints);
            totalRecordBytes += 8 + recordContentBytes[i];
        }

        var fileLengthWords = (100 + totalRecordBytes) / 2;

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

        for (var recordIndex = 0; recordIndex < records.Length; recordIndex++)
        {
            var parts = records[recordIndex];
            var points = parts.SelectMany(part => part).ToArray();
            var recordXs = points.Select(p => p.Lon).ToArray();
            var recordYs = points.Select(p => p.Lat).ToArray();
            var numParts = parts.Count;
            var numPoints = points.Length;

            WriteBigEndian(writer, recordIndex + 1);
            WriteBigEndian(writer, recordContentBytes[recordIndex] / 2);
            writer.Write(5); // Polygon type

            writer.Write(recordXs.Min());
            writer.Write(recordYs.Min());
            writer.Write(recordXs.Max());
            writer.Write(recordYs.Max());
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
