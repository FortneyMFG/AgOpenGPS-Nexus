using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Aog.Core.Coverage;
using FluentAssertions;
using NetTopologySuite;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using Xunit;

namespace Aog.Core.Tests.Coverage;

public sealed class CoverageExportVerifierTests
{
    private static readonly GeometryFactory GeometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory();

    [Fact]
    public void Compare_ReturnsWithinTolerance()
    {
        using var legacy = ShapefileZipFixture.Create(CreateBaselinePolygons());
        using var nexus = ShapefileZipFixture.Create(CreateBaselinePolygons());
        var verifier = new CoverageExportVerifier(areaTolerance: 1e-6, centroidTolerance: 1e-6);

        var report = verifier.Compare(legacy.ZipPath, nexus.ZipPath);

        report.IsWithinTolerance.Should().BeTrue();
        report.Differences.Should().HaveCount(2);
        report.Differences.Should().OnlyContain(d => d.IsWithinTolerance);
    }

    [Fact]
    public void Compare_DetectsShiftedPolygons()
    {
        using var legacy = ShapefileZipFixture.Create(CreateBaselinePolygons());
        using var shifted = ShapefileZipFixture.Create(CreateShiftedPolygons());
        var verifier = new CoverageExportVerifier(areaTolerance: 0.1, centroidTolerance: 0.01, symmetricAreaTolerance: 0.1);

        var report = verifier.Compare(legacy.ZipPath, shifted.ZipPath);

        report.IsWithinTolerance.Should().BeFalse();
        report.Differences.Should().ContainSingle(d => !d.IsWithinTolerance);
    }

    [Fact]
    public void Compare_ThrowsWhenArchiveMissing()
    {
        using var legacy = ShapefileZipFixture.Create(CreateBaselinePolygons());
        var verifier = new CoverageExportVerifier(areaTolerance: 0.1, centroidTolerance: 0.1);
        var missing = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".zip");

        Action act = () => verifier.Compare(missing, legacy.ZipPath);

        act.Should().Throw<FileNotFoundException>();
    }

    private static Geometry[] CreateBaselinePolygons()
    {
        return new Geometry[]
        {
            CreateRectangle(minX: 0, minY: 0, width: 10, height: 6),
            CreateRectangle(minX: 25, minY: 10, width: 4, height: 4),
        };
    }

    private static Geometry[] CreateShiftedPolygons()
    {
        return new Geometry[]
        {
            CreateRectangle(minX: 0, minY: 0, width: 10, height: 6),
            CreateRectangle(minX: 25.5, minY: 10, width: 4, height: 4),
        };
    }

    private static Polygon CreateRectangle(double minX, double minY, double width, double height)
    {
        var coordinates = new[]
        {
            new Coordinate(minX, minY),
            new Coordinate(minX + width, minY),
            new Coordinate(minX + width, minY + height),
            new Coordinate(minX, minY + height),
            new Coordinate(minX, minY),
        };

        return GeometryFactory.CreatePolygon(coordinates);
    }

    private sealed class ShapefileZipFixture : IDisposable
    {
        private readonly string _workingDirectory;

        private ShapefileZipFixture(string workingDirectory, string zipPath)
        {
            _workingDirectory = workingDirectory;
            ZipPath = zipPath;
        }

        public string ZipPath { get; }

        public static ShapefileZipFixture Create(params Geometry[] geometries)
        {
            if (geometries is null)
            {
                throw new ArgumentNullException(nameof(geometries));
            }

            if (geometries.Length == 0)
            {
                throw new ArgumentException("At least one geometry is required to create a shapefile.", nameof(geometries));
            }

            var workingDirectory = Path.Combine(Path.GetTempPath(), "coverage-test-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(workingDirectory);
            var shapefilePath = Path.Combine(workingDirectory, "coverage.shp");

            var features = geometries
                .Select((geometry, index) =>
                {
                    var attributes = new AttributesTable
                    {
                        { "Id", index },
                    };

                    return new Feature(geometry, attributes);
                })
                .ToList();

        var writer = new ShapefileDataWriter(shapefilePath, GeometryFactory)
        {
            Header = ShapefileDataWriter.GetHeader(features[0], features.Count),
        };
        writer.Write(features);

            var zipPath = Path.Combine(Path.GetTempPath(), "coverage-zip-" + Guid.NewGuid().ToString("N") + ".zip");
            if (File.Exists(zipPath))
            {
                File.Delete(zipPath);
            }

            ZipFile.CreateFromDirectory(workingDirectory, zipPath);

            return new ShapefileZipFixture(workingDirectory, zipPath);
        }

        public void Dispose()
        {
            try
            {
                if (File.Exists(ZipPath))
                {
                    File.Delete(ZipPath);
                }
            }
            catch (IOException)
            {
                // Ignore cleanup failures.
            }
            catch (UnauthorizedAccessException)
            {
                // Ignore cleanup failures.
            }

            try
            {
                if (Directory.Exists(_workingDirectory))
                {
                    Directory.Delete(_workingDirectory, recursive: true);
                }
            }
            catch (IOException)
            {
                // Ignore cleanup failures.
            }
            catch (UnauthorizedAccessException)
            {
                // Ignore cleanup failures.
            }
        }
    }
}
