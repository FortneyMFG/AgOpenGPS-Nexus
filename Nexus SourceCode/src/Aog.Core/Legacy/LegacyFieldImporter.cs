using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Aog.Core.Paths;

namespace Aog.Core.Legacy;

/// <summary>
/// Provides helpers for importing legacy AgOpenGPS V6 field assets.
/// </summary>
public sealed class LegacyFieldImporter
{
    /// <summary>
    /// Imports the V6 field geometry and tracks located in the supplied directory.
    /// </summary>
    /// <param name="fieldDirectory">Path containing the legacy text files.</param>
    public LegacyFieldData Import(string fieldDirectory)
    {
        if (string.IsNullOrWhiteSpace(fieldDirectory))
        {
            throw new ArgumentException("Field directory is required.", nameof(fieldDirectory));
        }

        var fullPath = Path.GetFullPath(fieldDirectory);
        var tracks = LoadTracks(fullPath);
        var boundaries = LoadBoundaries(fullPath);

        return new LegacyFieldData(tracks, boundaries);
    }

    private static IReadOnlyList<GuidanceTrackDefinition> LoadTracks(string directory)
    {
        var path = Path.Combine(directory, "TrackLines.txt");
        if (!File.Exists(path))
        {
            return Array.Empty<GuidanceTrackDefinition>();
        }

        var tracks = new List<GuidanceTrackDefinition>();
        using var reader = new StreamReader(path);

        var header = reader.ReadLine();
        if (header is null || !header.TrimStart().StartsWith("$", StringComparison.Ordinal))
        {
            throw new InvalidDataException("TrackLines.txt missing $ header.");
        }

        while (!reader.EndOfStream)
        {
            var name = ReadTrimmedLine(reader);
            if (name is null)
            {
                break;
            }

            var headingLine = ReadTrimmedLine(reader) ?? throw new InvalidDataException("Unexpected EOF reading heading.");
            var pointALine = ReadTrimmedLine(reader) ?? throw new InvalidDataException("Unexpected EOF reading point A.");
            var pointBLine = ReadTrimmedLine(reader) ?? throw new InvalidDataException("Unexpected EOF reading point B.");
            var nudgeLine = ReadTrimmedLine(reader) ?? throw new InvalidDataException("Unexpected EOF reading nudge distance.");
            var modeLine = ReadTrimmedLine(reader) ?? throw new InvalidDataException("Unexpected EOF reading track mode.");
            var visibleLine = ReadTrimmedLine(reader) ?? throw new InvalidDataException("Unexpected EOF reading visibility flag.");
            var countLine = ReadTrimmedLine(reader) ?? throw new InvalidDataException("Unexpected EOF reading curve count.");

            var heading = double.Parse(headingLine, CultureInfo.InvariantCulture);
            var pointA = ParsePoint(pointALine);
            var pointB = ParsePoint(pointBLine);
            var nudge = double.Parse(nudgeLine, CultureInfo.InvariantCulture);
            var mode = (LegacyTrackMode)int.Parse(modeLine, NumberStyles.Integer, CultureInfo.InvariantCulture);
            var isVisible = bool.Parse(visibleLine);
            var curveCount = int.Parse(countLine, NumberStyles.Integer, CultureInfo.InvariantCulture);

            var curvePoints = new List<GuidanceCurvePoint>(curveCount);
            for (var i = 0; i < curveCount; i++)
            {
                var curveLine = reader.ReadLine();
                if (curveLine is null)
                {
                    throw new InvalidDataException("Unexpected EOF reading curve points.");
                }

                curvePoints.Add(ParseCurvePoint(curveLine));
            }

            tracks.Add(new GuidanceTrackDefinition(name, heading, pointA, pointB, nudge, mode, isVisible, curvePoints));
        }

        return tracks;
    }

    private static IReadOnlyList<FieldBoundary> LoadBoundaries(string directory)
    {
        var boundaryPath = Path.Combine(directory, "Boundary.txt");
        if (!File.Exists(boundaryPath))
        {
            return Array.Empty<FieldBoundary>();
        }

        var boundaries = new List<FieldBoundary>();
        using (var reader = new StreamReader(boundaryPath))
        {
            string? line = reader.ReadLine();
            if (line is not null && !line.TrimStart().StartsWith("$", StringComparison.Ordinal))
            {
                reader.BaseStream.Seek(0, SeekOrigin.Begin);
                reader.DiscardBufferedData();
            }

            while ((line = ReadNextDataLine(reader)) is not null)
            {
                var driveThrough = false;
                var trimmed = line.Trim();

                if (bool.TryParse(trimmed, out var flag))
                {
                    driveThrough = flag;
                    line = ReadNextDataLine(reader);
                    if (line is null)
                    {
                        break;
                    }

                    trimmed = line.Trim();
                    if (bool.TryParse(trimmed, out flag))
                    {
                        driveThrough = flag;
                        line = ReadNextDataLine(reader);
                        if (line is null)
                        {
                            break;
                        }

                        trimmed = line.Trim();
                    }
                }

                if (!int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out var vertexCount))
                {
                    break;
                }

                var vertices = new List<BoundaryVertex>(vertexCount);
                for (var i = 0; i < vertexCount; i++)
                {
                    var vertexLine = reader.ReadLine();
                    if (vertexLine is null)
                    {
                        throw new InvalidDataException("Unexpected EOF reading boundary vertices.");
                    }

                    vertices.Add(ParseBoundaryVertex(vertexLine));
                }

                boundaries.Add(new FieldBoundary(driveThrough, vertices, Array.Empty<HeadlandRing>()));
            }
        }

        AttachHeadlands(directory, boundaries);
        return boundaries;
    }

    private static void AttachHeadlands(string directory, List<FieldBoundary> boundaries)
    {
        if (boundaries.Count == 0)
        {
            return;
        }

        var headlandPath = Path.Combine(directory, "Headland.txt");
        if (!File.Exists(headlandPath))
        {
            return;
        }

        using var reader = new StreamReader(headlandPath);
        string? line = reader.ReadLine();
        if (line is not null && line.TrimStart().StartsWith("$", StringComparison.Ordinal))
        {
            line = null;
        }

        for (var index = 0; index < boundaries.Count; index++)
        {
            if (line is null)
            {
                line = reader.ReadLine();
            }

            if (line is null)
            {
                break;
            }

            if (!int.TryParse(line.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var count))
            {
                break;
            }

            var vertices = new List<BoundaryVertex>(count);
            for (var i = 0; i < count; i++)
            {
                var vertexLine = reader.ReadLine();
                if (vertexLine is null)
                {
                    throw new InvalidDataException("Unexpected EOF reading headland vertices.");
                }

                vertices.Add(ParseBoundaryVertex(vertexLine));
            }

            if (vertices.Count > 0)
            {
                var headlands = boundaries[index].Headlands.ToList();
                headlands.Add(new HeadlandRing(vertices));
                boundaries[index] = new FieldBoundary(
                    boundaries[index].IsDriveThrough,
                    boundaries[index].Perimeter,
                    headlands);
            }

            line = null;
        }
    }

    private static string? ReadTrimmedLine(TextReader reader)
    {
        var line = reader.ReadLine();
        return line?.Trim();
    }

    private static string? ReadNextDataLine(StreamReader reader)
    {
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (!string.IsNullOrWhiteSpace(line))
            {
                return line;
            }
        }

        return null;
    }

    private static GuidanceCurvePoint ParseCurvePoint(string line)
    {
        var parts = line.Split(',', StringSplitOptions.TrimEntries);
        if (parts.Length < 3)
        {
            throw new InvalidDataException("Curve point row must contain easting,northing,heading.");
        }

        var point = new PlanarPoint(
            double.Parse(parts[0], CultureInfo.InvariantCulture),
            double.Parse(parts[1], CultureInfo.InvariantCulture));

        var heading = double.Parse(parts[2], CultureInfo.InvariantCulture);
        return new GuidanceCurvePoint(point, heading);
    }

    private static PlanarPoint ParsePoint(string line)
    {
        var parts = line.Split(',', StringSplitOptions.TrimEntries);
        if (parts.Length < 2)
        {
            throw new InvalidDataException("Track point row must contain easting,northing.");
        }

        return new PlanarPoint(
            double.Parse(parts[0], CultureInfo.InvariantCulture),
            double.Parse(parts[1], CultureInfo.InvariantCulture));
    }

    private static BoundaryVertex ParseBoundaryVertex(string line)
    {
        var parts = line.Split(',', StringSplitOptions.TrimEntries);
        if (parts.Length < 3)
        {
            throw new InvalidDataException("Boundary vertex row must contain easting,northing,heading.");
        }

        var point = new PlanarPoint(
            double.Parse(parts[0], CultureInfo.InvariantCulture),
            double.Parse(parts[1], CultureInfo.InvariantCulture));

        var heading = double.Parse(parts[2], CultureInfo.InvariantCulture);
        return new BoundaryVertex(point, heading);
    }
}
