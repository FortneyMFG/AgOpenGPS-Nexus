using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;

namespace Aog.Core.Coverage;

/// <summary>
/// Verifies parity between legacy and Nexus coverage shapefile exports.
/// </summary>
public sealed class CoverageExportVerifier
{
    private readonly double _areaTolerance;
    private readonly double _centroidTolerance;
    private readonly double _symmetricAreaTolerance;

    /// <summary>
    /// Initializes a new instance of the <see cref="CoverageExportVerifier"/> class.
    /// </summary>
    /// <param name="areaTolerance">Maximum allowable area delta between paired polygons.</param>
    /// <param name="centroidTolerance">Maximum allowable centroid distance between paired polygons.</param>
    /// <param name="symmetricAreaTolerance">Maximum allowable symmetric difference area between paired polygons.</param>
    public CoverageExportVerifier(double areaTolerance, double centroidTolerance, double symmetricAreaTolerance)
    {
        if (areaTolerance < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(areaTolerance));
        }

        if (centroidTolerance < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(centroidTolerance));
        }

        if (symmetricAreaTolerance < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(symmetricAreaTolerance));
        }

        _areaTolerance = areaTolerance;
        _centroidTolerance = centroidTolerance;
        _symmetricAreaTolerance = symmetricAreaTolerance;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CoverageExportVerifier"/> class with a shared area tolerance.
    /// </summary>
    /// <param name="areaTolerance">Maximum allowable area delta between paired polygons.</param>
    /// <param name="centroidTolerance">Maximum allowable centroid distance between paired polygons.</param>
    public CoverageExportVerifier(double areaTolerance, double centroidTolerance)
        : this(areaTolerance, centroidTolerance, areaTolerance)
    {
    }

    /// <summary>
    /// Compares the contents of two zipped shapefile exports.
    /// </summary>
    /// <param name="legacyZipPath">Path to the legacy shapefile archive.</param>
    /// <param name="nexusZipPath">Path to the Nexus shapefile archive.</param>
    /// <returns>Parity report describing each polygon comparison.</returns>
    public CoverageShapefileReport Compare(string legacyZipPath, string nexusZipPath)
    {
        if (legacyZipPath is null)
        {
            throw new ArgumentNullException(nameof(legacyZipPath));
        }

        if (nexusZipPath is null)
        {
            throw new ArgumentNullException(nameof(nexusZipPath));
        }

        var legacyPolygons = LoadPolygonsFromZip(legacyZipPath);
        var nexusPolygons = LoadPolygonsFromZip(nexusZipPath);

        if (legacyPolygons.Count != nexusPolygons.Count)
        {
            throw new InvalidDataException(
                $"Polygon count mismatch. Legacy has {legacyPolygons.Count}, Nexus has {nexusPolygons.Count}.");
        }

        var orderedLegacy = legacyPolygons
            .OrderByDescending(p => p.Area)
            .ThenBy(p => p.Centroid.X)
            .ThenBy(p => p.Centroid.Y)
            .ToList();

        var orderedNexus = nexusPolygons
            .OrderByDescending(p => p.Area)
            .ThenBy(p => p.Centroid.X)
            .ThenBy(p => p.Centroid.Y)
            .ToList();

        var differences = new List<CoverageShapeDifference>(orderedLegacy.Count);
        for (var i = 0; i < orderedLegacy.Count; i++)
        {
            var legacy = orderedLegacy[i];
            var nexus = orderedNexus[i];

            var areaDifference = Math.Abs(legacy.Area - nexus.Area);
            var centroidDistance = legacy.Centroid.Distance(nexus.Centroid);
            var symmetricDifferenceArea = legacy.SymmetricDifference(nexus).Area;
            var withinTolerance = areaDifference <= _areaTolerance
                && centroidDistance <= _centroidTolerance
                && symmetricDifferenceArea <= _symmetricAreaTolerance;

            differences.Add(new CoverageShapeDifference(
                Index: i,
                LegacyArea: legacy.Area,
                NexusArea: nexus.Area,
                AreaDifference: areaDifference,
                CentroidDistance: centroidDistance,
                SymmetricDifferenceArea: symmetricDifferenceArea,
                IsWithinTolerance: withinTolerance));
        }

        return new CoverageShapefileReport(differences);
    }

    private static IReadOnlyList<Geometry> LoadPolygonsFromZip(string zipPath)
    {
        if (!File.Exists(zipPath))
        {
            throw new FileNotFoundException("Shapefile archive not found.", zipPath);
        }

        using var tempDirectory = TemporaryDirectory.Create();
        ZipFile.ExtractToDirectory(zipPath, tempDirectory.Path);

        var shapefilePath = Directory.EnumerateFiles(tempDirectory.Path, "*.shp", SearchOption.AllDirectories)
            .FirstOrDefault();
        if (shapefilePath is null)
        {
            throw new InvalidDataException($"No .shp file found in archive '{zipPath}'.");
        }

        var geometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory();
        var polygons = new List<Geometry>();
        using var reader = new ShapefileDataReader(shapefilePath, geometryFactory);
        while (reader.Read())
        {
            if (reader.Geometry is not Geometry geometry)
            {
                continue;
            }

            foreach (var polygon in FlattenGeometry(geometry))
            {
                polygons.Add(polygon);
            }
        }

        return polygons;
    }

    private static IEnumerable<Geometry> FlattenGeometry(Geometry geometry)
    {
        if (geometry is null)
        {
            yield break;
        }

        switch (geometry)
        {
            case GeometryCollection collection:
                for (var i = 0; i < collection.NumGeometries; i++)
                {
                    foreach (var child in FlattenGeometry(collection.GetGeometryN(i)))
                    {
                        yield return child;
                    }
                }

                break;
            default:
                var clone = (Geometry)geometry.Copy();
                clone.Normalize();
                yield return clone;
                break;
        }
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        private TemporaryDirectory(string path)
        {
            Path = path;
        }

        public string Path { get; }

        public static TemporaryDirectory Create()
        {
            var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(),
                "coverage-export-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);
            return new TemporaryDirectory(path);
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(Path))
                {
                    Directory.Delete(Path, recursive: true);
                }
            }
            catch (IOException)
            {
                // Ignore cleanup errors.
            }
            catch (UnauthorizedAccessException)
            {
                // Ignore cleanup errors.
            }
        }
    }
}

/// <summary>
/// Represents the parity report generated by <see cref="CoverageExportVerifier"/>.
/// </summary>
public sealed class CoverageShapefileReport
{
    public CoverageShapefileReport(IReadOnlyList<CoverageShapeDifference> differences)
    {
        Differences = differences ?? throw new ArgumentNullException(nameof(differences));
    }

    /// <summary>
    /// Gets the per-polygon differences.
    /// </summary>
    public IReadOnlyList<CoverageShapeDifference> Differences { get; }

    /// <summary>
    /// Gets a value indicating whether all polygons are within tolerance.
    /// </summary>
    public bool IsWithinTolerance => Differences.All(d => d.IsWithinTolerance);
}

/// <summary>
/// Captures the difference between legacy and Nexus coverage polygons.
/// </summary>
/// <param name="Index">Zero-based polygon index after sorting.</param>
/// <param name="LegacyArea">Legacy polygon area.</param>
/// <param name="NexusArea">Nexus polygon area.</param>
/// <param name="AreaDifference">Absolute difference in area.</param>
/// <param name="CentroidDistance">Distance between polygon centroids.</param>
/// <param name="SymmetricDifferenceArea">Area of the symmetric difference between polygons.</param>
/// <param name="IsWithinTolerance">True when all deltas are within tolerance.</param>
public sealed record CoverageShapeDifference(
    int Index,
    double LegacyArea,
    double NexusArea,
    double AreaDifference,
    double CentroidDistance,
    double SymmetricDifferenceArea,
    bool IsWithinTolerance);
