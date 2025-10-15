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
        var overview = LoadOverview(fullPath);
        var flags = LoadFlags(fullPath);
        var contour = LoadContour(fullPath);
        var recordedPaths = LoadRecordedPaths(fullPath);
        var tramTemplates = LoadTramTemplates(fullPath);
        var workedArea = LoadWorkedArea(fullPath);

        return new LegacyFieldData(tracks, boundaries, overview, flags, contour, recordedPaths, tramTemplates, workedArea);
    }

    private static LegacyFieldOverview? LoadOverview(string directory)
    {
        var path = Path.Combine(directory, "Field.txt");
        if (!File.Exists(path))
        {
            return null;
        }

        var map = ReadKeyValueFile(path);
        if (map.Count == 0)
        {
            return null;
        }

        var origin = new GeographicCoordinate(
            ParseDouble(map, new[] { "originlat", "latitude", "lat" }, 0d),
            ParseDouble(map, new[] { "originlon", "longitude", "lon" }, 0d));

        var fieldName = TryGetFirst(map, "field", "fieldname", "name");
        var operatorName = TryGetFirst(map, "operator", "creator", "createdby");
        var notes = TryGetFirst(map, "notes", "comment", "description");

        DateTimeOffset? createdAt = null;
        var createdRaw = TryGetFirst(map, "created", "createdutc", "createdat");
        if (createdRaw is not null && DateTimeOffset.TryParse(createdRaw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var created))
        {
            createdAt = created;
        }

        double? convergence = TryParseNullableDouble(map, "convergence", "convergenceangle", "convergencedeg");
        double? elevation = TryParseNullableDouble(map, "elevation", "elev", "altitude", "originalt");

        return new LegacyFieldOverview(fieldName, operatorName, createdAt, origin, convergence, elevation, notes);
    }

    private static IReadOnlyList<LegacyFlag> LoadFlags(string directory)
    {
        var path = Path.Combine(directory, "Flags.txt");
        if (!File.Exists(path))
        {
            return Array.Empty<LegacyFlag>();
        }

        var flags = new List<LegacyFlag>();
        using var reader = new StreamReader(path);
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            line = line.Trim();
            if (string.IsNullOrEmpty(line) || line.StartsWith("$", StringComparison.Ordinal) || line.StartsWith("#", StringComparison.Ordinal))
            {
                continue;
            }

            var parts = line.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
            {
                continue;
            }

            int id = 0;
            string? label = null;
            double? easting = null;
            double? northing = null;
            double heading = 0;
            string color = "Yellow";
            string? notes = null;

            if (parts.All(p => p.Contains('=', StringComparison.Ordinal)))
            {
                var kv = parts
                    .Select(p => p.Split('=', 2, StringSplitOptions.TrimEntries))
                    .Where(p => p.Length == 2)
                    .ToDictionary(p => p[0].ToLowerInvariant(), p => p[1]);

                if (kv.TryGetValue("id", out var idValue))
                {
                    _ = int.TryParse(idValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out id);
                }

                if (kv.TryGetValue("label", out var labelValue))
                {
                    label = labelValue;
                }

                if (kv.TryGetValue("x", out var xValue) || kv.TryGetValue("easting", out xValue))
                {
                    easting = double.Parse(xValue, CultureInfo.InvariantCulture);
                }

                if (kv.TryGetValue("y", out var yValue) || kv.TryGetValue("northing", out yValue))
                {
                    northing = double.Parse(yValue, CultureInfo.InvariantCulture);
                }

                if (kv.TryGetValue("heading", out var headingValue) && double.TryParse(headingValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedHeading))
                {
                    heading = parsedHeading;
                }

                if (kv.TryGetValue("color", out var colorValue))
                {
                    color = colorValue;
                }

                if (kv.TryGetValue("notes", out var noteValue))
                {
                    notes = noteValue;
                }
            }
            else
            {
                if (parts.Length > 0)
                {
                    int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out id);
                }

                if (parts.Length > 1)
                {
                    label = parts[1];
                }

                if (parts.Length > 2)
                {
                    easting = double.Parse(parts[2], CultureInfo.InvariantCulture);
                }

                if (parts.Length > 3)
                {
                    northing = double.Parse(parts[3], CultureInfo.InvariantCulture);
                }

                if (parts.Length > 4 && double.TryParse(parts[4], NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedHeading))
                {
                    heading = parsedHeading;
                }

                if (parts.Length > 5)
                {
                    color = parts[5];
                }

                if (parts.Length > 6)
                {
                    notes = parts[6];
                }
            }

            if (easting.HasValue && northing.HasValue)
            {
                var point = new PlanarPoint(easting.Value, northing.Value);
                flags.Add(new LegacyFlag(id, label, point, heading, color, notes));
            }
        }

        return flags;
    }

    private static LegacyContourResume LoadContour(string directory)
    {
        var path = Path.Combine(directory, "Contour.txt");
        if (!File.Exists(path))
        {
            return LegacyContourResume.Empty;
        }

        var savedStrips = new List<LegacyContourStrip>();
        List<PlanarPoint>? pendingVertices = null;
        var isRecording = false;

        using var reader = new StreamReader(path);
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            line = line.Trim();
            if (string.IsNullOrEmpty(line) || line.StartsWith("$", StringComparison.Ordinal) || line.StartsWith("#", StringComparison.Ordinal))
            {
                continue;
            }

            if (TryParseKeyValue(line, out var key, out var value))
            {
                if (key.Equals("isrecording", StringComparison.OrdinalIgnoreCase))
                {
                    isRecording = bool.TryParse(value, out var flag) && flag;
                }
                else if (key.Equals("strip", StringComparison.OrdinalIgnoreCase))
                {
                    var vertices = ReadPointBlock(reader, value);
                    if (vertices.Count > 0)
                    {
                        savedStrips.Add(new LegacyContourStrip(vertices));
                    }
                }
                else if (key.Equals("buffer", StringComparison.OrdinalIgnoreCase) || key.Equals("pending", StringComparison.OrdinalIgnoreCase))
                {
                    var vertices = ReadPointBlock(reader, value);
                    if (vertices.Count > 0)
                    {
                        pendingVertices = vertices;
                    }
                }
            }
        }

        return new LegacyContourResume(isRecording, savedStrips, pendingVertices is null ? null : new LegacyContourStrip(pendingVertices));
    }

    private static IReadOnlyList<LegacyRecordedPath> LoadRecordedPaths(string directory)
    {
        var path = Path.Combine(directory, "RecPath.txt");
        if (!File.Exists(path))
        {
            return Array.Empty<LegacyRecordedPath>();
        }

        var paths = new List<LegacyRecordedPath>();
        string currentName = "Recorded Path";
        var samples = new List<LegacyRecordedPathPoint>();

        using var reader = new StreamReader(path);
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            line = line.Trim();
            if (string.IsNullOrEmpty(line) || line.StartsWith("$", StringComparison.Ordinal) || line.StartsWith("#", StringComparison.Ordinal))
            {
                continue;
            }

            if (TryParseKeyValue(line, out var key, out var value) && key.Equals("path", StringComparison.OrdinalIgnoreCase))
            {
                CommitCurrent();
                currentName = string.IsNullOrWhiteSpace(value) ? currentName : value.Trim();
                continue;
            }

            var parts = line.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 4)
            {
                continue;
            }

            var easting = double.Parse(parts[0], CultureInfo.InvariantCulture);
            var northing = double.Parse(parts[1], CultureInfo.InvariantCulture);
            var heading = double.Parse(parts[2], CultureInfo.InvariantCulture);
            var speed = double.Parse(parts[3], CultureInfo.InvariantCulture);
            var autoSteer = parts.Length > 4 && bool.TryParse(parts[4], out var enabled) && enabled;

            samples.Add(new LegacyRecordedPathPoint(new PlanarPoint(easting, northing), heading, speed, autoSteer));
        }

        CommitCurrent();
        return paths;

        void CommitCurrent()
        {
            if (samples.Count == 0)
            {
                return;
            }

            paths.Add(new LegacyRecordedPath(currentName, samples.ToList()));
            samples.Clear();
        }
    }

    private static IReadOnlyList<LegacyTramTemplate> LoadTramTemplates(string directory)
    {
        var path = Path.Combine(directory, "Tram.txt");
        if (!File.Exists(path))
        {
            return Array.Empty<LegacyTramTemplate>();
        }

        var templates = new List<LegacyTramTemplate>();
        string templateName = "Tram Template";
        double spacing = 0;
        var outer = new List<PlanarPoint>();
        var passes = new List<IReadOnlyList<PlanarPoint>>();

        using var reader = new StreamReader(path);
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            line = line.Trim();
            if (string.IsNullOrEmpty(line) || line.StartsWith("$", StringComparison.Ordinal) || line.StartsWith("#", StringComparison.Ordinal))
            {
                continue;
            }

            if (TryParseKeyValue(line, out var key, out var value))
            {
                if (key.Equals("template", StringComparison.OrdinalIgnoreCase) || key.Equals("name", StringComparison.OrdinalIgnoreCase))
                {
                    CommitTemplate();
                    templateName = string.IsNullOrWhiteSpace(value) ? "Tram Template" : value.Trim();
                    continue;
                }

                if (key.Equals("spacing", StringComparison.OrdinalIgnoreCase))
                {
                    spacing = double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedSpacing) ? parsedSpacing : 0;
                    continue;
                }

                if (key.Equals("outer", StringComparison.OrdinalIgnoreCase))
                {
                    outer = ReadPointBlock(reader, value);
                    continue;
                }

                if (key.Equals("pass", StringComparison.OrdinalIgnoreCase))
                {
                    var pass = ReadPointBlock(reader, value);
                    if (pass.Count > 0)
                    {
                        passes.Add(pass);
                    }
                }
            }
        }

        CommitTemplate();
        return templates;

        void CommitTemplate()
        {
            if (outer.Count == 0 && passes.Count == 0)
            {
                outer = new List<PlanarPoint>();
                passes = new List<IReadOnlyList<PlanarPoint>>();
                return;
            }

            templates.Add(new LegacyTramTemplate(templateName, spacing, outer.ToList(), passes.ToList()));
            outer = new List<PlanarPoint>();
            passes = new List<IReadOnlyList<PlanarPoint>>();
        }
    }

    private static LegacyWorkedAreaHistory LoadWorkedArea(string directory)
    {
        var path = Path.Combine(directory, "Sections.txt");
        if (!File.Exists(path))
        {
            return LegacyWorkedAreaHistory.Empty;
        }

        double cellSize = 0;
        string? layerId = null;
        var saved = new List<LegacyWorkedAreaCell>();
        var pending = new List<LegacyWorkedAreaCell>();
        var targetList = saved;

        using var reader = new StreamReader(path);
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            line = line.Trim();
            if (string.IsNullOrEmpty(line) || line.StartsWith("$", StringComparison.Ordinal) || line.StartsWith("#", StringComparison.Ordinal))
            {
                continue;
            }

            if (TryParseKeyValue(line, out var key, out var value))
            {
                if (key.Equals("cellsize", StringComparison.OrdinalIgnoreCase))
                {
                    cellSize = double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) ? parsed : 0;
                    continue;
                }

                if (key.Equals("layer", StringComparison.OrdinalIgnoreCase))
                {
                    layerId = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
                    continue;
                }

                if (key.Equals("pending", StringComparison.OrdinalIgnoreCase))
                {
                    targetList = pending;
                    continue;
                }

                if (key.Equals("saved", StringComparison.OrdinalIgnoreCase))
                {
                    targetList = saved;
                    continue;
                }
            }

            var parts = line.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 3)
            {
                continue;
            }

            var easting = double.Parse(parts[0], CultureInfo.InvariantCulture);
            var northing = double.Parse(parts[1], CultureInfo.InvariantCulture);
            var coverage = double.Parse(parts[2], CultureInfo.InvariantCulture);
            targetList.Add(new LegacyWorkedAreaCell(new PlanarPoint(easting, northing), coverage));
        }

        return new LegacyWorkedAreaHistory(cellSize, saved, pending, layerId);
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

    private static Dictionary<string, string> ReadKeyValueFile(string path)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in File.ReadAllLines(path))
        {
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("$", StringComparison.Ordinal) || trimmed.StartsWith("#", StringComparison.Ordinal))
            {
                continue;
            }

            if (TryParseKeyValue(trimmed, out var key, out var value))
            {
                map[key] = value;
            }
        }

        return map;
    }

    private static bool TryParseKeyValue(string line, out string key, out string value)
    {
        var separatorIndex = line.IndexOf('=');
        if (separatorIndex < 0)
        {
            separatorIndex = line.IndexOf(':');
        }

        if (separatorIndex < 0)
        {
            key = string.Empty;
            value = string.Empty;
            return false;
        }

        key = line[..separatorIndex].Trim();
        value = line[(separatorIndex + 1)..].Trim();
        return key.Length > 0;
    }

    private static string? TryGetFirst(Dictionary<string, string> map, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (map.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }

    private static double ParseDouble(Dictionary<string, string> map, string[] keys, double defaultValue)
    {
        var value = TryGetFirst(map, keys);
        if (value is null)
        {
            return defaultValue;
        }

        return double.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : defaultValue;
    }

    private static double? TryParseNullableDouble(Dictionary<string, string> map, params string[] keys)
    {
        var value = TryGetFirst(map, keys);
        if (value is null)
        {
            return null;
        }

        return double.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
    }

    private static List<PlanarPoint> ReadPointBlock(StreamReader reader, string countValue)
    {
        var count = 0;
        if (!string.IsNullOrWhiteSpace(countValue))
        {
            int.TryParse(countValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out count);
        }

        var points = new List<PlanarPoint>(Math.Max(count, 0));
        for (var i = 0; i < count; i++)
        {
            var line = reader.ReadLine();
            if (line is null)
            {
                break;
            }

            line = line.Trim();
            if (string.IsNullOrEmpty(line))
            {
                i--;
                continue;
            }

            var parts = line.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
            {
                continue;
            }

            var easting = double.Parse(parts[0], CultureInfo.InvariantCulture);
            var northing = double.Parse(parts[1], CultureInfo.InvariantCulture);
            points.Add(new PlanarPoint(easting, northing));
        }

        return points;
    }
}
