using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Aog.Core.Paths;
using Aog.Core.Simulation.Configuration;

namespace Aog.Core.Legacy;

/// <summary>
/// Imports legacy guidance exports (AB lines and field boundaries) into Core-friendly structures.
/// </summary>
public sealed class LegacyGuidanceImportService : ILegacyGuidanceImportService
{
    private const double EarthRadiusMeters = 6_378_137.0;
    private const double OrientationTolerance = 1e-12;

    public LegacyGuidanceImportResult Import(string fieldName, Stream abLineCsv, string boundaryShapefilePath)
    {
        if (abLineCsv is null)
        {
            throw new ArgumentNullException(nameof(abLineCsv));
        }

        if (string.IsNullOrWhiteSpace(boundaryShapefilePath))
        {
            throw new ArgumentException("Boundary shapefile path is required.", nameof(boundaryShapefilePath));
        }

        if (!File.Exists(boundaryShapefilePath))
        {
            throw new FileNotFoundException("Boundary shapefile was not found.", boundaryShapefilePath);
        }

        var abLines = ParseAbLines(abLineCsv);
        var boundary = ParseBoundary(boundaryShapefilePath);

        if (boundary.Count == 0 && abLines.Count == 0)
        {
            throw new InvalidOperationException("Import requires at least one boundary vertex or AB line.");
        }

        if (string.IsNullOrWhiteSpace(fieldName))
        {
            fieldName = DeriveFieldName(boundaryShapefilePath);
        }

        var origin = boundary.Count > 0 ? boundary[0] : abLines[0].PointA;
        var boundaryPlanar = boundary.Select(point => ToPlanar(point, origin)).ToList();
        var abLinePlanar = abLines.Select(line => ProjectLine(line, origin)).ToList();
        var scenario = BuildScenario(fieldName);

        return new LegacyGuidanceImportResult(fieldName, origin, abLinePlanar, boundaryPlanar, scenario);
    }

    private static List<LegacyAbLineDefinition> ParseAbLines(Stream csvStream)
    {
        var lines = new List<LegacyAbLineDefinition>();

        csvStream.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(csvStream, leaveOpen: true);
        string? raw;
        var firstRow = true;

        while ((raw = reader.ReadLine()) is not null)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                continue;
            }

            var columns = raw.Split(',', StringSplitOptions.TrimEntries);
            if (columns.Length < 5)
            {
                throw new InvalidDataException($"AB line CSV row '{raw}' does not contain the expected five columns.");
            }

            if (firstRow && !IsCoordinate(columns[1]) && !IsCoordinate(columns[2]))
            {
                firstRow = false;
                continue; // Skip header row.
            }

            var name = string.IsNullOrWhiteSpace(columns[0]) ? $"Line {lines.Count + 1}" : columns[0].Trim();
            var latA = ParseDouble(columns[1], raw);
            var lonA = ParseDouble(columns[2], raw);
            var latB = ParseDouble(columns[3], raw);
            var lonB = ParseDouble(columns[4], raw);

            lines.Add(new LegacyAbLineDefinition(name, new GeographicCoordinate(latA, lonA), new GeographicCoordinate(latB, lonB)));
            firstRow = false;
        }

        return lines;
    }

    private static bool IsCoordinate(string value)
    {
        return double.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out _);
    }

    private static double ParseDouble(string value, string rawRow)
    {
        if (!double.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var parsed))
        {
            throw new InvalidDataException($"Unable to parse value '{value}' from row '{rawRow}'.");
        }

        return parsed;
    }

    private static List<GeographicCoordinate> ParseBoundary(string shapefilePath)
    {
        using var stream = File.OpenRead(shapefilePath);
        using var reader = new BinaryReader(stream);

        if (ReadBigEndianInt32(reader) != 9994)
        {
            throw new InvalidDataException("Shapefile header is invalid (file code mismatch).");
        }

        // Skip unused header bytes.
        for (var i = 0; i < 5; i++)
        {
            reader.ReadInt32();
        }

        var fileLengthWords = ReadBigEndianInt32(reader);
        if (fileLengthWords <= 0)
        {
            throw new InvalidDataException("Shapefile reports an invalid length.");
        }

        var version = reader.ReadInt32();
        if (version != 1000)
        {
            throw new InvalidDataException($"Unsupported shapefile version '{version}'.");
        }

        var shapeType = reader.ReadInt32();
        if (!IsPolygonShapeType(shapeType))
        {
            throw new InvalidDataException("Shapefile does not contain polygon data.");
        }

        // Bounding boxes + Z/M range are not required for import.
        stream.Seek(100, SeekOrigin.Begin);

        var combinedBoundary = new List<GeographicCoordinate>();

        while (stream.Position < stream.Length)
        {
            if (stream.Length - stream.Position < 8)
            {
                break;
            }

            _ = ReadBigEndianInt32(reader); // Record number.
            var contentLengthWords = ReadBigEndianInt32(reader);
            var contentBytes = contentLengthWords * 2L;
            var recordStart = stream.Position;

            var recordShapeType = reader.ReadInt32();
            if (!IsPolygonShapeType(recordShapeType))
            {
                stream.Seek(recordStart + contentBytes, SeekOrigin.Begin);
                continue;
            }

            // Bounding box.
            reader.ReadDouble();
            reader.ReadDouble();
            reader.ReadDouble();
            reader.ReadDouble();

            var numParts = reader.ReadInt32();
            var numPoints = reader.ReadInt32();
            if (numParts <= 0 || numPoints <= 0)
            {
                stream.Seek(recordStart + contentBytes, SeekOrigin.Begin);
                continue;
            }

            var partIndices = new int[numParts];
            for (var i = 0; i < numParts; i++)
            {
                partIndices[i] = reader.ReadInt32();
            }

            var coordinates = new GeographicCoordinate[numPoints];
            for (var i = 0; i < numPoints; i++)
            {
                var longitude = reader.ReadDouble();
                var latitude = reader.ReadDouble();
                coordinates[i] = new GeographicCoordinate(latitude, longitude);
            }

            var exterior = new List<GeographicCoordinate>();
            double? exteriorOrientation = null;

            for (var part = 0; part < numParts; part++)
            {
                var startIndex = partIndices[part];
                var endIndex = part + 1 < numParts ? partIndices[part + 1] : numPoints;

                if (startIndex < 0 || startIndex >= coordinates.Length || endIndex > coordinates.Length || endIndex <= startIndex)
                {
                    continue;
                }

                var ring = new List<GeographicCoordinate>(endIndex - startIndex);
                for (var i = startIndex; i < endIndex; i++)
                {
                    ring.Add(coordinates[i]);
                }

                if (ring.Count > 1 && ring[0].EqualsApprox(ring[^1]))
                {
                    ring.RemoveAt(ring.Count - 1);
                }

                if (ring.Count == 0)
                {
                    continue;
                }

                var orientation = ComputeSignedArea(ring);

                if (exteriorOrientation is null && Math.Abs(orientation) > OrientationTolerance)
                {
                    exteriorOrientation = orientation;
                }

                var includeRing = exteriorOrientation is null || Math.Abs(orientation) <= OrientationTolerance || Math.Sign(orientation) == Math.Sign(exteriorOrientation.Value);

                if (includeRing)
                {
                    exterior.AddRange(ring);
                }
            }

            if (exterior.Count > 0)
            {
                combinedBoundary.AddRange(exterior);
            }

            stream.Seek(recordStart + contentBytes, SeekOrigin.Begin);
        }

        if (combinedBoundary.Count > 0)
        {
            return combinedBoundary;
        }

        throw new InvalidDataException("No polygon records were found in the shapefile.");
    }

    private static double ComputeSignedArea(IReadOnlyList<GeographicCoordinate> ring)
    {
        if (ring.Count < 3)
        {
            return 0.0;
        }

        double area = 0.0;
        for (var i = 0; i < ring.Count; i++)
        {
            var current = ring[i];
            var next = ring[(i + 1) % ring.Count];
            area += (current.LongitudeDeg * next.LatitudeDeg) - (next.LongitudeDeg * current.LatitudeDeg);
        }

        return area / 2.0;
    }

    private static bool IsPolygonShapeType(int shapeType)
    {
        return shapeType is 5 or 15 or 25; // Polygon, PolygonZ, PolygonM
    }

    private static int ReadBigEndianInt32(BinaryReader reader)
    {
        var bytes = reader.ReadBytes(sizeof(int));
        if (bytes.Length < sizeof(int))
        {
            return 0;
        }

        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(bytes);
        }

        return BitConverter.ToInt32(bytes, 0);
    }

    private static PlanarPoint ToPlanar(GeographicCoordinate coordinate, GeographicCoordinate origin)
    {
        var latRad = DegreesToRadians(coordinate.LatitudeDeg);
        var lonRad = DegreesToRadians(coordinate.LongitudeDeg);
        var originLatRad = DegreesToRadians(origin.LatitudeDeg);
        var originLonRad = DegreesToRadians(origin.LongitudeDeg);

        var x = (lonRad - originLonRad) * Math.Cos((latRad + originLatRad) / 2.0) * EarthRadiusMeters;
        var y = (latRad - originLatRad) * EarthRadiusMeters;
        return new PlanarPoint(x, y);
    }

    private static LegacyAbLinePlanar ProjectLine(LegacyAbLineDefinition line, GeographicCoordinate origin)
    {
        var pointAPlanar = ToPlanar(line.PointA, origin);
        var pointBPlanar = ToPlanar(line.PointB, origin);
        var vector = pointBPlanar - pointAPlanar;
        var headingRad = Math.Atan2(vector.X, vector.Y);
        var headingDeg = RadiansToDegrees(headingRad);
        if (headingDeg < 0)
        {
            headingDeg += 360.0;
        }

        var length = vector.Length;
        return new LegacyAbLinePlanar(line.Name, line.PointA, line.PointB, pointAPlanar, pointBPlanar, headingDeg, length);
    }

    private static SimulationScenarioConfiguration BuildScenario(string fieldName)
    {
        var routes = new[]
        {
            new SimulationRouteConfiguration("pose", "legacy/udp/main_gps", "hardware"),
            new SimulationRouteConfiguration("steer_state", "legacy/udp/steer_state", "hardware"),
            new SimulationRouteConfiguration("steer_cmd", "legacy/udp/steer_cmd", "hardware"),
            new SimulationRouteConfiguration("sections", "legacy/udp/sections", "hardware"),
        };

        var scenarioId = $"legacy:{SanitizeIdentifier(fieldName)}";
        var description = $"Routes legacy UDP telemetry for '{fieldName}'.";
        return new SimulationScenarioConfiguration(scenarioId, description, routes, options: null);
    }

    private static string SanitizeIdentifier(string value)
    {
        var filtered = new string(value
            .Select(ch => char.IsLetterOrDigit(ch) ? char.ToLowerInvariant(ch) : '-')
            .ToArray());

        filtered = filtered.Trim('-');
        return string.IsNullOrWhiteSpace(filtered) ? "import" : filtered;
    }

    private static string DeriveFieldName(string path)
    {
        var name = Path.GetFileNameWithoutExtension(path);
        return string.IsNullOrWhiteSpace(name) ? "Legacy Field" : name;
    }

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180.0;

    private static double RadiansToDegrees(double radians) => radians * 180.0 / Math.PI;

    private sealed record LegacyAbLineDefinition(string Name, GeographicCoordinate PointA, GeographicCoordinate PointB);
}
