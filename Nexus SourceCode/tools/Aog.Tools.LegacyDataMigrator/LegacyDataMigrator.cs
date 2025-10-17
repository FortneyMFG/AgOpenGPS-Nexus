using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;
using Parquet;
using Parquet.Data;
using Parquet.Schema;
using ParquetSchema = Parquet.Schema.ParquetSchema;

namespace Aog.Tools.LegacyDataMigrator;

/// <summary>
/// Migrates legacy AgOpenGPS V6 logs and field history files into Nexus-compatible formats.
/// </summary>
public sealed class LegacyDataMigrator
{
    private static readonly JsonSerializerOptions LayerSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
    };

    private static readonly DateTimeOffset FieldHealthBaseTimestamp = new(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset YieldBaseTimestamp = new(2020, 6, 1, 0, 0, 0, TimeSpan.Zero);

    private static readonly IReadOnlyDictionary<string, string> FieldHealthSeverityByColor = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["red"] = "critical",
        ["orange"] = "high",
        ["yellow"] = "moderate",
        ["blue"] = "moderate",
        ["purple"] = "high",
        ["green"] = "low"
    };

    /// <summary>
    /// Migrates telemetry logs and field histories into the output directory.
    /// </summary>
    /// <param name="options">Migration options.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>Summary describing migrated artefacts.</returns>
    public async Task<LegacyMigrationReport> MigrateAsync(
        LegacyMigrationOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();
        cancellationToken.ThrowIfCancellationRequested();

        var report = new LegacyMigrationReport(options.OutputDirectory);
        var logsRoot = options.ResolveLogsDirectory();

        if (Directory.Exists(logsRoot))
        {
            var posePath = Path.Combine(logsRoot, "pose.csv");
            if (File.Exists(posePath))
            {
                report = report with { PoseCount = report.PoseCount + await WritePoseAsync(posePath, options.OutputDirectory) };
            }
            else
            {
                report = AppendSkipped(report, posePath + " (missing)");
            }

            var imuPath = Path.Combine(logsRoot, "imu.csv");
            if (File.Exists(imuPath))
            {
                report = report with { ImuCount = report.ImuCount + await WriteImuAsync(imuPath, options.OutputDirectory) };
            }
            else
            {
                report = AppendSkipped(report, imuPath + " (missing)");
            }

            var canPath = Path.Combine(logsRoot, "can.csv");
            if (File.Exists(canPath))
            {
                report = report with { CanCount = report.CanCount + await WriteCanAsync(canPath, options.OutputDirectory) };
            }
            else
            {
                report = AppendSkipped(report, canPath + " (missing)");
            }

            var sectionsPath = Path.Combine(logsRoot, "sections.csv");
            if (File.Exists(sectionsPath))
            {
                report = report with { SectionCount = report.SectionCount + await WriteSectionsAsync(sectionsPath, options.OutputDirectory) };
            }
            else
            {
                report = AppendSkipped(report, sectionsPath + " (missing)");
            }

            var pluginPath = Path.Combine(logsRoot, "plugin.csv");
            if (File.Exists(pluginPath))
            {
                report = report with { PluginCount = report.PluginCount + await WritePluginAsync(pluginPath, options.OutputDirectory) };
            }
            else
            {
                report = AppendSkipped(report, pluginPath + " (missing)");
            }

            var weatherPath = Path.Combine(logsRoot, "weather.csv");
            if (File.Exists(weatherPath))
            {
                report = report with { WeatherCount = report.WeatherCount + await WriteWeatherAsync(weatherPath, options.OutputDirectory) };
            }
            else
            {
                report = AppendSkipped(report, weatherPath + " (missing)");
            }
        }
        else
        {
            report = AppendSkipped(report, logsRoot + " (missing)");
            var expectedLogs = new[]
            {
                Path.Combine(logsRoot, "pose.csv"),
                Path.Combine(logsRoot, "imu.csv"),
                Path.Combine(logsRoot, "can.csv"),
                Path.Combine(logsRoot, "sections.csv"),
                Path.Combine(logsRoot, "plugin.csv"),
                Path.Combine(logsRoot, "weather.csv"),
            };

            foreach (var missing in expectedLogs)
            {
                report = AppendSkipped(report, missing + " (missing)");
            }
        }

        var fieldHistoryPath = options.ResolveFieldHistoryPath();
        if (File.Exists(fieldHistoryPath))
        {
            var history = ParseFieldHistory(fieldHistoryPath);
            var historyOutputPath = Path.Combine(options.OutputDirectory, "field-history.json");
            Directory.CreateDirectory(options.OutputDirectory);
            var json = JsonSerializer.Serialize(history, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(historyOutputPath, json);

            var fieldCount = history.Fields.Count;
            var entryCount = history.Fields.Sum(f => f.Entries.Count);
            report = report with
            {
                FieldHistoryFields = fieldCount,
                FieldHistoryEntries = entryCount,
            };
        }
        else
        {
            report = AppendSkipped(report, fieldHistoryPath + " (missing)");
        }

        var flagsPath = Path.Combine(options.InputDirectory, options.FlagsFileName);
        if (File.Exists(flagsPath))
        {
            var outputPath = Path.Combine(options.OutputDirectory, "risk.other.layer.json");
            var observationCount = WriteFieldHealthLayer(flagsPath, outputPath);
            report = report with { FieldHealthObservationCount = report.FieldHealthObservationCount + observationCount };
        }
        else
        {
            report = AppendSkipped(report, flagsPath + " (missing)");
        }

        var yieldPath = Path.Combine(logsRoot, options.YieldTelemetryFileName);
        if (File.Exists(yieldPath))
        {
            var outputPath = Path.Combine(options.OutputDirectory, "yield.actual.layer.json");
            var sampleCount = WriteYieldLayer(yieldPath, outputPath);
            report = report with { YieldSampleCount = report.YieldSampleCount + sampleCount };
        }
        else
        {
            report = AppendSkipped(report, yieldPath + " (missing)");
        }

        return report;
    }

    private static LegacyMigrationReport AppendSkipped(LegacyMigrationReport report, string path)
    {
        var entries = report.SkippedFiles.Append(path).ToList();
        return report with { SkippedFiles = entries };
    }

    private static int WriteFieldHealthLayer(string flagsPath, string outputPath)
    {
        var flags = ParseLegacyFlags(flagsPath);
        if (flags.Count == 0)
        {
            return 0;
        }

        var observations = CreateFieldHealthObservations(flags);
        var sortedByAscending = observations.OrderBy(o => o.ObservedAt).ToList();
        var createdAt = sortedByAscending.First().ObservedAt;
        var lastModified = sortedByAscending.Last().ObservedAt;

        var statistics = CreateFieldHealthStatistics(observations);
        var history = CreateFieldHealthHistory(sortedByAscending);
        var metadata = new FieldHealthMetadataDocument(
            SchemaRef: "https://agopengps.org/schemas/FieldHealthRiskLayer.v1.json",
            Notes: null,
            Tags: Array.Empty<string>(),
            Observations: observations,
            Statistics: statistics,
            History: history);

        var provenance = new LayerProvenanceDocument(
            Source: "legacy:flags",
            Transform: "migrate:legacy-flags",
            Hash: ComputeHash(observations.Select(o => o.FeatureId + o.Severity + o.ObservedAt.ToUnixTimeSeconds())),
            CreatedAt: lastModified,
            Actor: "system:legacy-migrator");

        var document = new FieldHealthLayerDocument(
            SchemaVersion: "1.0.0",
            Id: "layer:risk.other.legacy",
            Kind: "risk.other",
            JobId: "job:legacy",
            SessionId: "session:legacy",
            Units: "severity-index",
            CreatedAt: createdAt,
            CreatedBy: "system:legacy-migrator",
            LastModifiedAt: lastModified,
            Provenance: provenance,
            Metadata: metadata);

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        var json = JsonSerializer.Serialize(document, LayerSerializerOptions);
        File.WriteAllText(outputPath, json);
        return observations.Count;
    }

    private static int WriteYieldLayer(string csvPath, string outputPath)
    {
        var samples = ParseYieldSamples(csvPath);
        if (samples.Count == 0)
        {
            return 0;
        }

        var ordered = samples.OrderBy(s => s.Timestamp).ToList();
        var createdAt = ordered.First().Timestamp;
        var lastTimestamp = ordered.Last().Timestamp;
        var yields = ordered.Select(s => s.YieldKgPerHa).ToList();

        var mean = yields.Average();
        var median = ComputeMedian(yields);
        var stdDev = ComputeStandardDeviation(yields, mean);
        var min = yields.Min();
        var max = yields.Max();
        var totalMassKg = ordered.Sum(s => s.YieldKgPerHa * s.AreaHa);

        var averageAreaHa = ordered.Average(s => s.AreaHa > 0 ? s.AreaHa : 0.01);
        var cellSizeMeters = Math.Sqrt(Math.Max(averageAreaHa, 1e-6) * 10_000d);

        var metadata = new YieldLayerMetadataDocument(
            Grid: new YieldGridMetadata(cellSizeMeters, "EPSG:32615"),
            Smoothing: new YieldSmoothingMetadata("none", null, null, null),
            Calibration: new YieldCalibrationMetadata(
                ProfileId: "calibration:legacy-default",
                AppliedAt: createdAt,
                Source: "legacy",
                SensorModel: null,
                Notes: null,
                Factors: null),
            Aggregation: new YieldAggregationMetadata(
                Basis: "area",
                Scopes: new[] { "job", "field" },
                UpdatedAt: lastTimestamp,
                Bins: new YieldAggregationBinsMetadata("quantile", 5, null, null)),
            Statistics: new YieldStatisticsMetadata(
                Count: ordered.Count,
                Mean: mean,
                Median: median,
                StdDev: stdDev,
                Min: min,
                Max: max,
                TotalMassKg: totalMassKg));

        var provenance = new LayerProvenanceDocument(
            Source: "legacy:yield",
            Transform: "migrate:legacy-yield",
            Hash: ComputeHash(ordered.Select(s => s.Timestamp.ToUnixTimeSeconds() + s.YieldKgPerHa.ToString(CultureInfo.InvariantCulture))),
            CreatedAt: lastTimestamp,
            Actor: "system:legacy-migrator");

        var document = new YieldLayerDocument(
            SchemaVersion: "1.0.0",
            Id: "layer:yield.actual.legacy",
            Kind: "yield.actual",
            JobId: "job:legacy",
            SessionId: "session:legacy",
            FieldId: null,
            Units: "kg/ha",
            CreatedAt: createdAt,
            CreatedBy: "system:legacy-migrator",
            LastModifiedAt: lastTimestamp,
            Provenance: provenance,
            Metadata: metadata);

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        var json = JsonSerializer.Serialize(document, LayerSerializerOptions);
        File.WriteAllText(outputPath, json);
        return ordered.Count;
    }

    private static List<LegacyFlagRow> ParseLegacyFlags(string path)
    {
        var flags = new List<LegacyFlagRow>();
        using var reader = new StreamReader(path);
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            line = line.Trim();
            if (string.IsNullOrEmpty(line) || line.StartsWith("$", StringComparison.Ordinal) || line.StartsWith("#", StringComparison.Ordinal))
            {
                continue;
            }

            int id = 0;
            string? label = null;
            double? easting = null;
            double? northing = null;
            double heading = 0;
            string color = "yellow";
            string? notes = null;

            if (line.Contains('=', StringComparison.Ordinal))
            {
                var kvPairs = line.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                    .Select(part => part.Split('=', 2, StringSplitOptions.TrimEntries))
                    .Where(pair => pair.Length == 2)
                    .ToDictionary(pair => pair[0].ToLowerInvariant(), pair => pair[1]);

                if (kvPairs.TryGetValue("id", out var idValue))
                {
                    _ = int.TryParse(idValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out id);
                }

                if (kvPairs.TryGetValue("label", out var labelValue))
                {
                    label = labelValue;
                }

                if (kvPairs.TryGetValue("x", out var xValue) || kvPairs.TryGetValue("easting", out xValue))
                {
                    easting = double.Parse(xValue, CultureInfo.InvariantCulture);
                }

                if (kvPairs.TryGetValue("y", out var yValue) || kvPairs.TryGetValue("northing", out yValue))
                {
                    northing = double.Parse(yValue, CultureInfo.InvariantCulture);
                }

                if (kvPairs.TryGetValue("heading", out var headingValue) && double.TryParse(headingValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedHeading))
                {
                    heading = parsedHeading;
                }

                if (kvPairs.TryGetValue("color", out var colorValue))
                {
                    color = colorValue;
                }

                if (kvPairs.TryGetValue("notes", out var notesValue))
                {
                    notes = notesValue;
                }
            }
            else
            {
                var parts = line.Split(',', StringSplitOptions.TrimEntries);
                if (parts.Length > 0)
                {
                    _ = int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out id);
                }

                if (parts.Length > 1 && !string.IsNullOrEmpty(parts[1]))
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
                flags.Add(new LegacyFlagRow(id, label, easting.Value, northing.Value, heading, color, notes));
            }
        }

        return flags;
    }

    private static List<FieldHealthObservationDocument> CreateFieldHealthObservations(IReadOnlyList<LegacyFlagRow> flags)
    {
        var observations = new List<FieldHealthObservationDocument>(flags.Count);
        for (var i = 0; i < flags.Count; i++)
        {
            var flag = flags[i];
            var observedAt = FieldHealthBaseTimestamp.AddMinutes((i + 1) * 5);
            var featureId = $"feature:legacy-flag-{flag.Id.ToString("D4", CultureInfo.InvariantCulture)}";
            var label = string.IsNullOrWhiteSpace(flag.Label) ? $"Legacy Flag {flag.Id}" : flag.Label.Trim();
            var severity = ResolveSeverity(flag.Color);
            var geometryHash = ComputeShortHash(string.Format(CultureInfo.InvariantCulture, "{0:F3},{1:F3}", flag.EastingMeters, flag.NorthingMeters));

            observations.Add(new FieldHealthObservationDocument(
                FeatureId: featureId,
                ZoneId: null,
                Label: label,
                Severity: severity,
                ObservedAt: observedAt,
                Observer: "system:legacy-migrator",
                Notes: string.IsNullOrWhiteSpace(flag.Notes) ? null : flag.Notes,
                Attachments: Array.Empty<string>(),
                SessionId: "session:legacy",
                AreaHa: null,
                GeometryHash: geometryHash,
                LastUpdatedAt: null,
                Status: "active"));
        }

        return observations
            .OrderByDescending(o => o.ObservedAt)
            .ThenBy(o => o.FeatureId, StringComparer.Ordinal)
            .ToList();
    }

    private static FieldHealthStatisticsDocument CreateFieldHealthStatistics(IReadOnlyList<FieldHealthObservationDocument> observations)
    {
        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["none"] = 0,
            ["low"] = 0,
            ["moderate"] = 0,
            ["high"] = 0,
            ["critical"] = 0
        };

        foreach (var observation in observations)
        {
            if (counts.ContainsKey(observation.Severity))
            {
                counts[observation.Severity]++;
            }
        }

        var lastSurveyedAt = observations.Max(o => o.ObservedAt);
        var severityCounts = new FieldHealthSeverityCountsDocument(
            None: counts["none"],
            Low: counts["low"],
            Moderate: counts["moderate"],
            High: counts["high"],
            Critical: counts["critical"]);

        return new FieldHealthStatisticsDocument(0, severityCounts, lastSurveyedAt);
    }

    private static FieldHealthHistoryDocument CreateFieldHealthHistory(IReadOnlyList<FieldHealthObservationDocument> observationsAscending)
    {
        var entries = observationsAscending
            .Select(o => new FieldHealthHistoryEntryDocument(o.FeatureId, o.Status, o.Severity, o.ObservedAt))
            .ToList();

        var toggles = new FieldHealthHistoryTogglesDocument(true, true, false);
        return new FieldHealthHistoryDocument(entries, toggles);
    }

    private static string ResolveSeverity(string color)
    {
        if (string.IsNullOrWhiteSpace(color))
        {
            return "low";
        }

        if (FieldHealthSeverityByColor.TryGetValue(color, out var mapped))
        {
            return mapped;
        }

        if (int.TryParse(color, NumberStyles.Integer, CultureInfo.InvariantCulture, out var numeric))
        {
            return numeric switch
            {
                0 => "critical",
                1 => "low",
                2 => "moderate",
                _ => "low"
            };
        }

        return "low";
    }

    private static List<LegacyYieldSample> ParseYieldSamples(string path)
    {
        var samples = new List<LegacyYieldSample>();
        using var reader = new StreamReader(path);
        _ = reader.ReadLine(); // header

        int index = 0;
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            line = line.Trim();
            if (string.IsNullOrEmpty(line) || line.StartsWith("#", StringComparison.Ordinal))
            {
                continue;
            }

            var parts = line.Split(',', StringSplitOptions.TrimEntries);
            if (parts.Length < 6)
            {
                continue;
            }

            DateTimeOffset timestamp;
            if (!DateTimeOffset.TryParse(parts[0], CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out timestamp))
            {
                timestamp = YieldBaseTimestamp.AddSeconds(index * 2);
            }

            var easting = double.Parse(parts[1], CultureInfo.InvariantCulture);
            var northing = double.Parse(parts[2], CultureInfo.InvariantCulture);
            var yieldValue = double.Parse(parts[3], CultureInfo.InvariantCulture);
            double? moisture = string.IsNullOrWhiteSpace(parts[4]) ? null : double.Parse(parts[4], CultureInfo.InvariantCulture);
            var area = double.TryParse(parts[5], NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedArea) ? parsedArea : 0.01;

            samples.Add(new LegacyYieldSample(timestamp, easting, northing, yieldValue, moisture, area <= 0 ? 0.01 : area));
            index++;
        }

        return samples;
    }

    private static double ComputeMedian(IReadOnlyList<double> values)
    {
        if (values.Count == 0)
        {
            return 0;
        }

        var ordered = values.OrderBy(v => v).ToList();
        var midpoint = ordered.Count / 2;
        if (ordered.Count % 2 == 0)
        {
            return (ordered[midpoint - 1] + ordered[midpoint]) / 2.0;
        }

        return ordered[midpoint];
    }

    private static double ComputeStandardDeviation(IReadOnlyList<double> values, double mean)
    {
        if (values.Count <= 1)
        {
            return 0;
        }

        var sumSquares = values.Sum(v => Math.Pow(v - mean, 2));
        var variance = sumSquares / values.Count;
        return Math.Sqrt(variance);
    }

    private static string ComputeHash(IEnumerable<string> components)
    {
        var builder = new StringBuilder();
        foreach (var component in components)
        {
            if (builder.Length > 0)
            {
                builder.Append('|');
            }

            builder.Append(component);
        }

        var bytes = Encoding.UTF8.GetBytes(builder.ToString());
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string ComputeShortHash(string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash)[..8].ToLowerInvariant();
    }

    private static async Task<int> WritePoseAsync(string filePath, string outputDirectory)
    {
        var rows = LegacyPoseCsvParser.Parse(filePath);
        if (rows.Count == 0)
        {
            return 0;
        }

        var schema = TelemetrySchemas.Pose.Schema;
        await using var stream = CreateParquetStream(outputDirectory, "pose.parquet");
        using var writer = await ParquetWriter.CreateAsync(schema, stream).ConfigureAwait(false);
        using var rowGroup = writer.CreateRowGroup();

        await rowGroup.WriteColumnAsync(new DataColumn(TelemetrySchemas.Pose.Sequence, rows.Select(r => r.Sequence).ToArray())).ConfigureAwait(false);
        await rowGroup.WriteColumnAsync(new DataColumn(TelemetrySchemas.Pose.Timestamp, rows.Select(r => r.TimestampUtc).ToArray())).ConfigureAwait(false);
        await rowGroup.WriteColumnAsync(new DataColumn(TelemetrySchemas.Pose.Frame, rows.Select(r => r.Frame).ToArray())).ConfigureAwait(false);
        await rowGroup.WriteColumnAsync(new DataColumn(TelemetrySchemas.Pose.Source, rows.Select(r => r.Source).ToArray())).ConfigureAwait(false);
        await rowGroup.WriteColumnAsync(CreateEmptyStringColumn(TelemetrySchemas.Pose.JobId, rows.Count)).ConfigureAwait(false);
        await rowGroup.WriteColumnAsync(CreateEmptyStringColumn(TelemetrySchemas.Pose.SeasonId, rows.Count)).ConfigureAwait(false);
        await rowGroup.WriteColumnAsync(CreateEmptyStringColumn(TelemetrySchemas.Pose.SessionId, rows.Count)).ConfigureAwait(false);
        await rowGroup.WriteColumnAsync(new DataColumn(TelemetrySchemas.Pose.LatitudeDeg, rows.Select(r => r.LatitudeDeg).ToArray())).ConfigureAwait(false);
        await rowGroup.WriteColumnAsync(new DataColumn(TelemetrySchemas.Pose.LongitudeDeg, rows.Select(r => r.LongitudeDeg).ToArray())).ConfigureAwait(false);
        await rowGroup.WriteColumnAsync(new DataColumn(TelemetrySchemas.Pose.AltitudeM, rows.Select(r => r.AltitudeM).ToArray())).ConfigureAwait(false);
        await rowGroup.WriteColumnAsync(new DataColumn(TelemetrySchemas.Pose.HeadingRad, rows.Select(r => r.HeadingRad).ToArray())).ConfigureAwait(false);
        await rowGroup.WriteColumnAsync(new DataColumn(TelemetrySchemas.Pose.RollRad, rows.Select(r => r.RollRad).ToArray())).ConfigureAwait(false);
        await rowGroup.WriteColumnAsync(new DataColumn(TelemetrySchemas.Pose.PitchRad, rows.Select(r => r.PitchRad).ToArray())).ConfigureAwait(false);
        await rowGroup.WriteColumnAsync(new DataColumn(TelemetrySchemas.Pose.SpeedMps, rows.Select(r => r.SpeedMps).ToArray())).ConfigureAwait(false);
        await rowGroup.WriteColumnAsync(new DataColumn(TelemetrySchemas.Pose.YawRateRadps, rows.Select(r => r.YawRateRadps).ToArray())).ConfigureAwait(false);

        return rows.Count;
    }

    private static async Task<int> WriteImuAsync(string filePath, string outputDirectory)
    {
        var rows = LegacyImuCsvParser.Parse(filePath);
        if (rows.Count == 0)
        {
            return 0;
        }

        await using var stream = CreateParquetStream(outputDirectory, "imu.parquet");
        using var writer = await ParquetWriter.CreateAsync(TelemetrySchemas.Imu.Schema, stream).ConfigureAwait(false);
        using var rowGroup = writer.CreateRowGroup();

        await rowGroup.WriteColumnAsync(new DataColumn(TelemetrySchemas.Imu.Sequence, rows.Select(r => r.Sequence).ToArray())).ConfigureAwait(false);
        await rowGroup.WriteColumnAsync(new DataColumn(TelemetrySchemas.Imu.Timestamp, rows.Select(r => r.TimestampUtc).ToArray())).ConfigureAwait(false);
        await rowGroup.WriteColumnAsync(new DataColumn(TelemetrySchemas.Imu.Frame, rows.Select(r => r.Frame).ToArray())).ConfigureAwait(false);
        await rowGroup.WriteColumnAsync(new DataColumn(TelemetrySchemas.Imu.Source, rows.Select(r => r.Source).ToArray())).ConfigureAwait(false);
        await rowGroup.WriteColumnAsync(CreateEmptyStringColumn(TelemetrySchemas.Imu.JobId, rows.Count)).ConfigureAwait(false);
        await rowGroup.WriteColumnAsync(CreateEmptyStringColumn(TelemetrySchemas.Imu.SeasonId, rows.Count)).ConfigureAwait(false);
        await rowGroup.WriteColumnAsync(CreateEmptyStringColumn(TelemetrySchemas.Imu.SessionId, rows.Count)).ConfigureAwait(false);
        await rowGroup.WriteColumnAsync(new DataColumn(TelemetrySchemas.Imu.AccelXMps2, rows.Select(r => r.AccelXMps2).ToArray())).ConfigureAwait(false);
        await rowGroup.WriteColumnAsync(new DataColumn(TelemetrySchemas.Imu.AccelYMps2, rows.Select(r => r.AccelYMps2).ToArray())).ConfigureAwait(false);
        await rowGroup.WriteColumnAsync(new DataColumn(TelemetrySchemas.Imu.AccelZMps2, rows.Select(r => r.AccelZMps2).ToArray())).ConfigureAwait(false);
        await rowGroup.WriteColumnAsync(new DataColumn(TelemetrySchemas.Imu.GyroXRadps, rows.Select(r => r.GyroXRadps).ToArray())).ConfigureAwait(false);
        await rowGroup.WriteColumnAsync(new DataColumn(TelemetrySchemas.Imu.GyroYRadps, rows.Select(r => r.GyroYRadps).ToArray())).ConfigureAwait(false);
        await rowGroup.WriteColumnAsync(new DataColumn(TelemetrySchemas.Imu.GyroZRadps, rows.Select(r => r.GyroZRadps).ToArray())).ConfigureAwait(false);
        await rowGroup.WriteColumnAsync(new DataColumn(TelemetrySchemas.Imu.MagXUt, rows.Select(r => r.MagXUt).ToArray())).ConfigureAwait(false);
        await rowGroup.WriteColumnAsync(new DataColumn(TelemetrySchemas.Imu.MagYUt, rows.Select(r => r.MagYUt).ToArray())).ConfigureAwait(false);
        await rowGroup.WriteColumnAsync(new DataColumn(TelemetrySchemas.Imu.MagZUt, rows.Select(r => r.MagZUt).ToArray())).ConfigureAwait(false);
        await rowGroup.WriteColumnAsync(new DataColumn(TelemetrySchemas.Imu.TemperatureC, rows.Select(r => r.TemperatureC).ToArray())).ConfigureAwait(false);

        return rows.Count;
    }

    private static async Task<int> WriteCanAsync(string filePath, string outputDirectory)
    {
        var rows = LegacyCanCsvParser.Parse(filePath);
        if (rows.Count == 0)
        {
            return 0;
        }

        await using var stream = CreateParquetStream(outputDirectory, "can.parquet");
        using var writer = await ParquetWriter.CreateAsync(TelemetrySchemas.Can.Schema, stream).ConfigureAwait(false);
        using var rowGroup = writer.CreateRowGroup();

        await rowGroup.WriteColumnAsync(new DataColumn(TelemetrySchemas.Can.Sequence, rows.Select(r => r.Sequence).ToArray())).ConfigureAwait(false);
        await rowGroup.WriteColumnAsync(new DataColumn(TelemetrySchemas.Can.Timestamp, rows.Select(r => r.TimestampUtc).ToArray())).ConfigureAwait(false);
        await rowGroup.WriteColumnAsync(new DataColumn(TelemetrySchemas.Can.Frame, rows.Select(r => r.Frame).ToArray())).ConfigureAwait(false);
        await rowGroup.WriteColumnAsync(new DataColumn(TelemetrySchemas.Can.Source, rows.Select(r => r.Source).ToArray())).ConfigureAwait(false);
        await rowGroup.WriteColumnAsync(CreateEmptyStringColumn(TelemetrySchemas.Can.JobId, rows.Count)).ConfigureAwait(false);
        await rowGroup.WriteColumnAsync(CreateEmptyStringColumn(TelemetrySchemas.Can.SeasonId, rows.Count)).ConfigureAwait(false);
        await rowGroup.WriteColumnAsync(CreateEmptyStringColumn(TelemetrySchemas.Can.SessionId, rows.Count)).ConfigureAwait(false);
        await rowGroup.WriteColumnAsync(new DataColumn(TelemetrySchemas.Can.ArbitrationId, rows.Select(r => r.ArbitrationId).ToArray())).ConfigureAwait(false);
        await rowGroup.WriteColumnAsync(new DataColumn(TelemetrySchemas.Can.Payload, rows.Select(r => r.Payload).ToArray())).ConfigureAwait(false);
        await rowGroup.WriteColumnAsync(new DataColumn(TelemetrySchemas.Can.IsExtendedId, rows.Select(r => r.IsExtendedId).ToArray())).ConfigureAwait(false);
        await rowGroup.WriteColumnAsync(new DataColumn(TelemetrySchemas.Can.IsRemoteRequest, rows.Select(r => r.IsRemoteRequest).ToArray())).ConfigureAwait(false);

        return rows.Count;
    }

    private static async Task<int> WriteSectionsAsync(string filePath, string outputDirectory)
    {
        var rows = LegacySectionCsvParser.Parse(filePath);
        if (rows.Count == 0)
        {
            return 0;
        }

        await using var stream = CreateParquetStream(outputDirectory, "io.parquet");
        using var writer = await ParquetWriter.CreateAsync(TelemetrySchemas.Io.Schema, stream).ConfigureAwait(false);
        using var rowGroup = writer.CreateRowGroup();

        await rowGroup.WriteColumnAsync(new DataColumn(TelemetrySchemas.Io.Sequence, rows.Select(r => r.Sequence).ToArray())).ConfigureAwait(false);
        await rowGroup.WriteColumnAsync(new DataColumn(TelemetrySchemas.Io.Timestamp, rows.Select(r => r.TimestampUtc).ToArray())).ConfigureAwait(false);
        await rowGroup.WriteColumnAsync(new DataColumn(TelemetrySchemas.Io.Frame, rows.Select(r => r.Frame).ToArray())).ConfigureAwait(false);
        await rowGroup.WriteColumnAsync(new DataColumn(TelemetrySchemas.Io.Source, rows.Select(r => r.Source).ToArray())).ConfigureAwait(false);
        await rowGroup.WriteColumnAsync(CreateEmptyStringColumn(TelemetrySchemas.Io.JobId, rows.Count)).ConfigureAwait(false);
        await rowGroup.WriteColumnAsync(CreateEmptyStringColumn(TelemetrySchemas.Io.SeasonId, rows.Count)).ConfigureAwait(false);
        await rowGroup.WriteColumnAsync(CreateEmptyStringColumn(TelemetrySchemas.Io.SessionId, rows.Count)).ConfigureAwait(false);
        await rowGroup.WriteColumnAsync(new DataColumn(TelemetrySchemas.Io.SectionCount, rows.Select(r => r.SectionCount).ToArray())).ConfigureAwait(false);
        await rowGroup.WriteColumnAsync(new DataColumn(TelemetrySchemas.Io.Mask, rows.Select(r => r.Mask).ToArray())).ConfigureAwait(false);

        return rows.Count;
    }

    private static async Task<int> WritePluginAsync(string filePath, string outputDirectory)
    {
        var rows = LegacyPluginCsvParser.Parse(filePath);
        if (rows.Count == 0)
        {
            return 0;
        }

        await using var stream = CreateParquetStream(outputDirectory, "plugin.parquet");
        using var writer = await ParquetWriter.CreateAsync(TelemetrySchemas.Plugin.Schema, stream).ConfigureAwait(false);
        using var rowGroup = writer.CreateRowGroup();

        await rowGroup.WriteColumnAsync(new DataColumn(TelemetrySchemas.Plugin.Sequence, rows.Select(r => r.Sequence).ToArray())).ConfigureAwait(false);
        await rowGroup.WriteColumnAsync(new DataColumn(TelemetrySchemas.Plugin.Timestamp, rows.Select(r => r.TimestampUtc).ToArray())).ConfigureAwait(false);
        await rowGroup.WriteColumnAsync(new DataColumn(TelemetrySchemas.Plugin.Source, rows.Select(r => r.Source).ToArray())).ConfigureAwait(false);
        await rowGroup.WriteColumnAsync(CreateEmptyStringColumn(TelemetrySchemas.Plugin.JobId, rows.Count)).ConfigureAwait(false);
        await rowGroup.WriteColumnAsync(CreateEmptyStringColumn(TelemetrySchemas.Plugin.SeasonId, rows.Count)).ConfigureAwait(false);
        await rowGroup.WriteColumnAsync(CreateEmptyStringColumn(TelemetrySchemas.Plugin.SessionId, rows.Count)).ConfigureAwait(false);
        await rowGroup.WriteColumnAsync(new DataColumn(TelemetrySchemas.Plugin.PluginId, rows.Select(r => r.PluginId).ToArray())).ConfigureAwait(false);
        await rowGroup.WriteColumnAsync(new DataColumn(TelemetrySchemas.Plugin.Topic, rows.Select(r => r.Topic).ToArray())).ConfigureAwait(false);
        await rowGroup.WriteColumnAsync(new DataColumn(TelemetrySchemas.Plugin.Payload, rows.Select(r => r.Payload).ToArray())).ConfigureAwait(false);

        return rows.Count;
    }

    private static async Task<int> WriteWeatherAsync(string filePath, string outputDirectory)
    {
        var rows = LegacyWeatherCsvParser.Parse(filePath);
        if (rows.Count == 0)
        {
            return 0;
        }

        await using var stream = CreateParquetStream(outputDirectory, "weather.parquet");
        using var writer = await ParquetWriter.CreateAsync(TelemetrySchemas.Weather.Schema, stream).ConfigureAwait(false);
        using var rowGroup = writer.CreateRowGroup();

        await rowGroup.WriteColumnAsync(new DataColumn(TelemetrySchemas.Weather.Sequence, rows.Select(r => r.Sequence).ToArray())).ConfigureAwait(false);
        await rowGroup.WriteColumnAsync(new DataColumn(TelemetrySchemas.Weather.Timestamp, rows.Select(r => r.TimestampUtc).ToArray())).ConfigureAwait(false);
        await rowGroup.WriteColumnAsync(new DataColumn(TelemetrySchemas.Weather.Source, rows.Select(r => r.Source).ToArray())).ConfigureAwait(false);
        await rowGroup.WriteColumnAsync(new DataColumn(TelemetrySchemas.Weather.TemperatureC, rows.Select(r => r.TemperatureC).ToArray())).ConfigureAwait(false);
        await rowGroup.WriteColumnAsync(new DataColumn(TelemetrySchemas.Weather.HumidityPct, rows.Select(r => r.HumidityPct).ToArray())).ConfigureAwait(false);
        await rowGroup.WriteColumnAsync(new DataColumn(TelemetrySchemas.Weather.WindKph, rows.Select(r => r.WindKph).ToArray())).ConfigureAwait(false);
        await rowGroup.WriteColumnAsync(new DataColumn(TelemetrySchemas.Weather.WindDirectionDeg, rows.Select(r => r.WindDirectionDeg).ToArray())).ConfigureAwait(false);
        await rowGroup.WriteColumnAsync(new DataColumn(TelemetrySchemas.Weather.WindGustKph, rows.Select(r => r.WindGustKph).ToArray())).ConfigureAwait(false);
        await rowGroup.WriteColumnAsync(new DataColumn(TelemetrySchemas.Weather.RainfallMm, rows.Select(r => r.RainfallMm).ToArray())).ConfigureAwait(false);
        await rowGroup.WriteColumnAsync(new DataColumn(TelemetrySchemas.Weather.PressureKpa, rows.Select(r => r.PressureKpa).ToArray())).ConfigureAwait(false);
        await rowGroup.WriteColumnAsync(new DataColumn(TelemetrySchemas.Weather.DewPointC, rows.Select(r => r.DewPointC).ToArray())).ConfigureAwait(false);
        await rowGroup.WriteColumnAsync(new DataColumn(TelemetrySchemas.Weather.WetBulbC, rows.Select(r => r.WetBulbC).ToArray())).ConfigureAwait(false);
        await rowGroup.WriteColumnAsync(new DataColumn(TelemetrySchemas.Weather.DeltaTC, rows.Select(r => r.DeltaTC).ToArray())).ConfigureAwait(false);
        await rowGroup.WriteColumnAsync(new DataColumn(TelemetrySchemas.Weather.EvapotranspirationMm, rows.Select(r => r.EvapotranspirationMm).ToArray())).ConfigureAwait(false);
        await rowGroup.WriteColumnAsync(new DataColumn(TelemetrySchemas.Weather.SolarIrradianceWm2, rows.Select(r => r.SolarIrradianceWm2).ToArray())).ConfigureAwait(false);
        await rowGroup.WriteColumnAsync(new DataColumn(TelemetrySchemas.Weather.UvIndex, rows.Select(r => r.UvIndex).ToArray())).ConfigureAwait(false);
        await rowGroup.WriteColumnAsync(new DataColumn(TelemetrySchemas.Weather.CloudCoverPct, rows.Select(r => r.CloudCoverPct).ToArray())).ConfigureAwait(false);
        await rowGroup.WriteColumnAsync(new DataColumn(TelemetrySchemas.Weather.VisibilityKm, rows.Select(r => r.VisibilityKm).ToArray())).ConfigureAwait(false);
        await rowGroup.WriteColumnAsync(new DataColumn(TelemetrySchemas.Weather.SoilTempC, rows.Select(r => r.SoilTempC).ToArray())).ConfigureAwait(false);
        await rowGroup.WriteColumnAsync(new DataColumn(TelemetrySchemas.Weather.SoilMoisturePct, rows.Select(r => r.SoilMoisturePct).ToArray())).ConfigureAwait(false);
        await rowGroup.WriteColumnAsync(new DataColumn(TelemetrySchemas.Weather.LeafWetnessPct, rows.Select(r => r.LeafWetnessPct).ToArray())).ConfigureAwait(false);

        return rows.Count;
    }

    private static FieldHistoryDocument ParseFieldHistory(string path)
    {
        var lines = File.ReadAllLines(path);
        if (lines.Length == 0)
        {
            return new FieldHistoryDocument(new List<FieldHistoryField>());
        }

        var fields = new Dictionary<string, List<FieldHistoryEntry>>(StringComparer.OrdinalIgnoreCase);
        for (var i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i]))
            {
                continue;
            }

            var parts = lines[i].Split(',', StringSplitOptions.TrimEntries);
            if (parts.Length < 3)
            {
                continue;
            }

            var fieldName = parts[0];
            if (string.IsNullOrWhiteSpace(fieldName))
            {
                fieldName = "Unnamed Field";
            }

            var timestamp = DateTime.Parse(parts[1], CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);
            var area = double.Parse(parts[2], CultureInfo.InvariantCulture);
            var operatorName = parts.Length > 3 && !string.IsNullOrWhiteSpace(parts[3]) ? parts[3] : null;

            if (!fields.TryGetValue(fieldName, out var entries))
            {
                entries = new List<FieldHistoryEntry>();
                fields[fieldName] = entries;
            }

            entries.Add(new FieldHistoryEntry(timestamp, area, operatorName));
        }

        var orderedFields = fields
            .OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .Select(pair => new FieldHistoryField(
                pair.Key,
                pair.Value
                    .OrderBy(entry => entry.TimestampUtc)
                    .ToList()))
            .ToList();

        return new FieldHistoryDocument(orderedFields);
    }

    private static DataColumn CreateEmptyStringColumn(DataField<string?> field, int count)
        => new(field, new string?[count]);

    private static FileStream CreateParquetStream(string outputDirectory, string fileName)
    {
        Directory.CreateDirectory(outputDirectory);
        var path = Path.Combine(outputDirectory, fileName);
        return new FileStream(
            path,
            FileMode.Create,
            FileAccess.Write,
            FileShare.Read,
            bufferSize: 4096,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
    }
}

/// <summary>
/// Options used to configure the migration utility.
/// </summary>
public sealed class LegacyMigrationOptions
{
    /// <summary>
    /// Gets or sets the legacy root directory containing logs and history CSVs.
    /// </summary>
    public string InputDirectory { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the output directory for migrated assets.
    /// </summary>
    public string OutputDirectory { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the logs directory relative to <see cref="InputDirectory"/>.
    /// </summary>
    public string LogsDirectoryName { get; set; } = "logs";

    /// <summary>
    /// Gets or sets the field history CSV file relative to <see cref="InputDirectory"/>.
    /// </summary>
    public string FieldHistoryFileName { get; set; } = "field-history.csv";

    /// <summary>
    /// Gets or sets the relative path to the legacy flags file used for field health remapping.
    /// </summary>
    public string FlagsFileName { get; set; } = "Flags.txt";

    /// <summary>
    /// Gets or sets the legacy yield telemetry CSV located under the logs directory.
    /// </summary>
    public string YieldTelemetryFileName { get; set; } = "yield.csv";

    internal void Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(InputDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(OutputDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(LogsDirectoryName);
        ArgumentException.ThrowIfNullOrWhiteSpace(FieldHistoryFileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(FlagsFileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(YieldTelemetryFileName);

        if (!Directory.Exists(InputDirectory))
        {
            throw new DirectoryNotFoundException($"Legacy directory not found: {InputDirectory}");
        }
    }

    internal string ResolveLogsDirectory()
        => Path.GetFullPath(Path.IsPathRooted(LogsDirectoryName) ? LogsDirectoryName : Path.Combine(InputDirectory, LogsDirectoryName));

    internal string ResolveFieldHistoryPath()
        => Path.GetFullPath(Path.IsPathRooted(FieldHistoryFileName) ? FieldHistoryFileName : Path.Combine(InputDirectory, FieldHistoryFileName));
}

/// <summary>
/// Summary describing the assets generated by the migration.
/// </summary>
public sealed record LegacyMigrationReport(
    string OutputDirectory,
    int PoseCount = 0,
    int ImuCount = 0,
    int CanCount = 0,
    int SectionCount = 0,
    int PluginCount = 0,
    int WeatherCount = 0,
    int FieldHistoryFields = 0,
    int FieldHistoryEntries = 0,
    int FieldHealthObservationCount = 0,
    int YieldSampleCount = 0,
    IReadOnlyList<string> SkippedFiles = null!)
{
    public LegacyMigrationReport(string outputDirectory)
        : this(outputDirectory, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, Array.Empty<string>())
    {
    }
};

internal static class LegacyPoseCsvParser
{
    public static IReadOnlyList<PoseRow> Parse(string path)
    {
        var rows = new List<PoseRow>();
        using var reader = new StreamReader(path);
        reader.ReadLine();
        while (!reader.EndOfStream)
        {
            var line = reader.ReadLine();
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var parts = line.Split(',', StringSplitOptions.TrimEntries);
            if (parts.Length < 12)
            {
                continue;
            }

            rows.Add(new PoseRow(
                ulong.Parse(parts[0], CultureInfo.InvariantCulture),
                ParseTimestamp(parts[1]),
                Normalize(parts[2]),
                Normalize(parts[3]),
                double.Parse(parts[4], CultureInfo.InvariantCulture),
                double.Parse(parts[5], CultureInfo.InvariantCulture),
                double.Parse(parts[6], CultureInfo.InvariantCulture),
                double.Parse(parts[7], CultureInfo.InvariantCulture),
                double.Parse(parts[8], CultureInfo.InvariantCulture),
                double.Parse(parts[9], CultureInfo.InvariantCulture),
                double.Parse(parts[10], CultureInfo.InvariantCulture),
                double.Parse(parts[11], CultureInfo.InvariantCulture)));
        }

        return rows;
    }

    private static DateTime? ParseTimestamp(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var timestamp = DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);
        return DateTime.SpecifyKind(timestamp, DateTimeKind.Utc);
    }

    private static string? Normalize(string value)
        => string.IsNullOrWhiteSpace(value) ? null : value;

    internal sealed record PoseRow(
        ulong Sequence,
        DateTime? TimestampUtc,
        string? Frame,
        string? Source,
        double LatitudeDeg,
        double LongitudeDeg,
        double AltitudeM,
        double HeadingRad,
        double RollRad,
        double PitchRad,
        double SpeedMps,
        double YawRateRadps);
}

internal static class LegacyImuCsvParser
{
    public static IReadOnlyList<ImuRow> Parse(string path)
    {
        var rows = new List<ImuRow>();
        using var reader = new StreamReader(path);
        reader.ReadLine();
        while (!reader.EndOfStream)
        {
            var line = reader.ReadLine();
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var parts = line.Split(',', StringSplitOptions.TrimEntries);
            if (parts.Length < 14)
            {
                continue;
            }

            rows.Add(new ImuRow(
                ulong.Parse(parts[0], CultureInfo.InvariantCulture),
                ParseTimestamp(parts[1]),
                Normalize(parts[2]),
                Normalize(parts[3]),
                double.Parse(parts[4], CultureInfo.InvariantCulture),
                double.Parse(parts[5], CultureInfo.InvariantCulture),
                double.Parse(parts[6], CultureInfo.InvariantCulture),
                double.Parse(parts[7], CultureInfo.InvariantCulture),
                double.Parse(parts[8], CultureInfo.InvariantCulture),
                double.Parse(parts[9], CultureInfo.InvariantCulture),
                double.Parse(parts[10], CultureInfo.InvariantCulture),
                double.Parse(parts[11], CultureInfo.InvariantCulture),
                double.Parse(parts[12], CultureInfo.InvariantCulture),
                double.Parse(parts[13], CultureInfo.InvariantCulture)));
        }

        return rows;
    }

    private static DateTime? ParseTimestamp(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var timestamp = DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);
        return DateTime.SpecifyKind(timestamp, DateTimeKind.Utc);
    }

    private static string? Normalize(string value)
        => string.IsNullOrWhiteSpace(value) ? null : value;

    internal sealed record ImuRow(
        ulong Sequence,
        DateTime? TimestampUtc,
        string? Frame,
        string? Source,
        double AccelXMps2,
        double AccelYMps2,
        double AccelZMps2,
        double GyroXRadps,
        double GyroYRadps,
        double GyroZRadps,
        double MagXUt,
        double MagYUt,
        double MagZUt,
        double TemperatureC);
}

internal static class LegacyCanCsvParser
{
    public static IReadOnlyList<CanRow> Parse(string path)
    {
        var rows = new List<CanRow>();
        using var reader = new StreamReader(path);
        reader.ReadLine();
        while (!reader.EndOfStream)
        {
            var line = reader.ReadLine();
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var parts = line.Split(',', StringSplitOptions.TrimEntries);
            if (parts.Length < 8)
            {
                continue;
            }

            rows.Add(new CanRow(
                ulong.Parse(parts[0], CultureInfo.InvariantCulture),
                ParseTimestamp(parts[1]),
                Normalize(parts[2]),
                Normalize(parts[3]),
                ParseArbitrationId(parts[4]),
                ParsePayload(parts[5]),
                bool.Parse(parts[6]),
                bool.Parse(parts[7])));
        }

        return rows;
    }

    private static DateTime? ParseTimestamp(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var timestamp = DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);
        return DateTime.SpecifyKind(timestamp, DateTimeKind.Utc);
    }

    private static uint ParseArbitrationId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return 0;
        }

        var trimmed = value.Trim();
        if (trimmed.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            return uint.Parse(trimmed.AsSpan(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        }

        if (trimmed.StartsWith("#", StringComparison.Ordinal))
        {
            return uint.Parse(trimmed.AsSpan(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        }

        if (trimmed.All(char.IsLetterOrDigit) && trimmed.Any(c => char.IsLetter(c)))
        {
            return uint.Parse(trimmed, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        }

        return uint.Parse(trimmed, CultureInfo.InvariantCulture);
    }

    private static byte[]? ParsePayload(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Array.Empty<byte>();
        }

        var sanitized = value.Replace(" ", string.Empty, StringComparison.Ordinal);
        if (sanitized.Length % 2 != 0)
        {
            sanitized = "0" + sanitized;
        }

        var buffer = new byte[sanitized.Length / 2];
        for (var i = 0; i < buffer.Length; i++)
        {
            buffer[i] = byte.Parse(sanitized.Substring(i * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        }

        return buffer;
    }

    private static string? Normalize(string value)
        => string.IsNullOrWhiteSpace(value) ? null : value;

    internal sealed record CanRow(
        ulong Sequence,
        DateTime? TimestampUtc,
        string? Frame,
        string? Source,
        uint ArbitrationId,
        byte[]? Payload,
        bool IsExtendedId,
        bool IsRemoteRequest);
}

internal static class LegacySectionCsvParser
{
    public static IReadOnlyList<SectionRow> Parse(string path)
    {
        var rows = new List<SectionRow>();
        using var reader = new StreamReader(path);
        reader.ReadLine();
        while (!reader.EndOfStream)
        {
            var line = reader.ReadLine();
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var parts = line.Split(',', StringSplitOptions.TrimEntries);
            if (parts.Length < 6)
            {
                continue;
            }

            rows.Add(new SectionRow(
                ulong.Parse(parts[0], CultureInfo.InvariantCulture),
                ParseTimestamp(parts[1]),
                Normalize(parts[2]),
                Normalize(parts[3]),
                uint.Parse(parts[4], CultureInfo.InvariantCulture),
                ParseMask(parts[5])));
        }

        return rows;
    }

    private static DateTime? ParseTimestamp(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var timestamp = DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);
        return DateTime.SpecifyKind(timestamp, DateTimeKind.Utc);
    }

    private static uint ParseMask(string value)
    {
        if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            return uint.Parse(value.AsSpan(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        }

        if (value.StartsWith("#", StringComparison.Ordinal))
        {
            return uint.Parse(value.AsSpan(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        }

        if (value.All(char.IsDigit))
        {
            return uint.Parse(value, CultureInfo.InvariantCulture);
        }

        return uint.Parse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
    }

    private static string? Normalize(string value)
        => string.IsNullOrWhiteSpace(value) ? null : value;

    internal sealed record SectionRow(
        ulong Sequence,
        DateTime? TimestampUtc,
        string? Frame,
        string? Source,
        uint SectionCount,
        uint Mask);
}

internal static class LegacyPluginCsvParser
{
    public static IReadOnlyList<PluginRow> Parse(string path)
    {
        var rows = new List<PluginRow>();
        using var reader = new StreamReader(path);
        reader.ReadLine();
        while (!reader.EndOfStream)
        {
            var line = reader.ReadLine();
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var parts = line.Split(',', StringSplitOptions.TrimEntries);
            if (parts.Length < 6)
            {
                continue;
            }

            rows.Add(new PluginRow(
                ulong.Parse(parts[0], CultureInfo.InvariantCulture),
                ParseTimestamp(parts[1]),
                Normalize(parts[2]),
                parts[3],
                parts[4],
                ParsePayload(parts[5])));
        }

        return rows;
    }

    private static DateTime? ParseTimestamp(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var timestamp = DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);
        return DateTime.SpecifyKind(timestamp, DateTimeKind.Utc);
    }

    private static byte[]? ParsePayload(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Array.Empty<byte>();
        }

        return Convert.FromBase64String(value);
    }

    private static string? Normalize(string value)
        => string.IsNullOrWhiteSpace(value) ? null : value;

    internal sealed record PluginRow(
        ulong Sequence,
        DateTime? TimestampUtc,
        string? Source,
        string PluginId,
        string Topic,
        byte[]? Payload);
}

internal static class LegacyWeatherCsvParser
{
    public static IReadOnlyList<WeatherRow> Parse(string path)
    {
        var rows = new List<WeatherRow>();
        using var reader = new StreamReader(path);
        reader.ReadLine();
        while (!reader.EndOfStream)
        {
            var line = reader.ReadLine();
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var parts = line.Split(',', StringSplitOptions.TrimEntries);
            if (parts.Length < 3)
            {
                continue;
            }

            rows.Add(new WeatherRow(
                ulong.Parse(parts[0], CultureInfo.InvariantCulture),
                ParseTimestamp(GetPart(parts, 1)),
                Normalize(GetPart(parts, 2)),
                ParseNullableDouble(GetPart(parts, 3)),
                ParseNullableDouble(GetPart(parts, 4)),
                ParseNullableDouble(GetPart(parts, 5)),
                ParseNullableDouble(GetPart(parts, 6)),
                ParseNullableDouble(GetPart(parts, 7)),
                ParseNullableDouble(GetPart(parts, 8)),
                ParseNullableDouble(GetPart(parts, 9)),
                ParseNullableDouble(GetPart(parts, 10)),
                ParseNullableDouble(GetPart(parts, 11)),
                ParseNullableDouble(GetPart(parts, 12)),
                ParseNullableDouble(GetPart(parts, 13)),
                ParseNullableDouble(GetPart(parts, 14)),
                ParseNullableDouble(GetPart(parts, 15)),
                ParseNullableDouble(GetPart(parts, 16)),
                ParseNullableDouble(GetPart(parts, 17)),
                ParseNullableDouble(GetPart(parts, 18)),
                ParseNullableDouble(GetPart(parts, 19)),
                ParseNullableDouble(GetPart(parts, 20))));
        }

        return rows;
    }

    private static DateTime? ParseTimestamp(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var timestamp = DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);
        return DateTime.SpecifyKind(timestamp, DateTimeKind.Utc);
    }

    private static double? ParseNullableDouble(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return double.Parse(value, CultureInfo.InvariantCulture);
    }

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value;

    private static string? GetPart(string[] parts, int index)
        => index < parts.Length ? parts[index] : null;

    internal sealed record WeatherRow(
        ulong Sequence,
        DateTime? TimestampUtc,
        string? Source,
        double? TemperatureC,
        double? HumidityPct,
        double? WindKph,
        double? WindDirectionDeg,
        double? WindGustKph,
        double? RainfallMm,
        double? PressureKpa,
        double? DewPointC,
        double? WetBulbC,
        double? DeltaTC,
        double? EvapotranspirationMm,
        double? SolarIrradianceWm2,
        double? UvIndex,
        double? CloudCoverPct,
        double? VisibilityKm,
        double? SoilTempC,
        double? SoilMoisturePct,
        double? LeafWetnessPct);
}

internal sealed record FieldHistoryDocument(IReadOnlyList<FieldHistoryField> Fields);

internal sealed record FieldHistoryField(string FieldName, IReadOnlyList<FieldHistoryEntry> Entries);

internal sealed record FieldHistoryEntry(DateTime TimestampUtc, double AreaHectares, string? Operator);

internal sealed record LegacyFlagRow(int Id, string? Label, double EastingMeters, double NorthingMeters, double HeadingDegrees, string Color, string? Notes);

internal sealed record FieldHealthLayerDocument(
    string SchemaVersion,
    string Id,
    string Kind,
    string? JobId,
    string? SessionId,
    string Units,
    DateTimeOffset CreatedAt,
    string CreatedBy,
    DateTimeOffset LastModifiedAt,
    LayerProvenanceDocument Provenance,
    FieldHealthMetadataDocument Metadata);

internal sealed record FieldHealthMetadataDocument(
    string SchemaRef,
    string? Notes,
    IReadOnlyList<string> Tags,
    IReadOnlyList<FieldHealthObservationDocument> Observations,
    FieldHealthStatisticsDocument Statistics,
    FieldHealthHistoryDocument History);

internal sealed record FieldHealthObservationDocument(
    string FeatureId,
    Guid? ZoneId,
    string Label,
    string Severity,
    DateTimeOffset ObservedAt,
    string Observer,
    string? Notes,
    IReadOnlyList<string> Attachments,
    string? SessionId,
    double? AreaHa,
    string GeometryHash,
    DateTimeOffset? LastUpdatedAt,
    string Status);

internal sealed record FieldHealthStatisticsDocument(double TotalAreaHa, FieldHealthSeverityCountsDocument SeverityCounts, DateTimeOffset LastSurveyedAt);

internal sealed record FieldHealthSeverityCountsDocument(int None, int Low, int Moderate, int High, int Critical);

internal sealed record FieldHealthHistoryDocument(IReadOnlyList<FieldHealthHistoryEntryDocument> Entries, FieldHealthHistoryTogglesDocument Toggles);

internal sealed record FieldHealthHistoryEntryDocument(string FeatureId, string Status, string Severity, DateTimeOffset ChangedAt);

internal sealed record FieldHealthHistoryTogglesDocument(bool ShowActive, bool ShowMonitor, bool ShowResolved);

internal sealed record YieldLayerDocument(
    string SchemaVersion,
    string Id,
    string Kind,
    string? JobId,
    string? SessionId,
    string? FieldId,
    string Units,
    DateTimeOffset CreatedAt,
    string CreatedBy,
    DateTimeOffset LastModifiedAt,
    LayerProvenanceDocument Provenance,
    YieldLayerMetadataDocument Metadata);

internal sealed record YieldLayerMetadataDocument(
    YieldGridMetadata Grid,
    YieldSmoothingMetadata Smoothing,
    YieldCalibrationMetadata Calibration,
    YieldAggregationMetadata Aggregation,
    YieldStatisticsMetadata Statistics);

internal sealed record YieldGridMetadata(double CellSizeMeters, string Projection);

internal sealed record YieldSmoothingMetadata(string Method, double? WindowSeconds, double? LagCompensationSeconds, int? Passes);

internal sealed record YieldCalibrationMetadata(string ProfileId, DateTimeOffset? AppliedAt, string? Source, string? SensorModel, string? Notes, IReadOnlyDictionary<string, double>? Factors);

internal sealed record YieldAggregationMetadata(string Basis, IReadOnlyList<string> Scopes, DateTimeOffset UpdatedAt, YieldAggregationBinsMetadata Bins);

internal sealed record YieldAggregationBinsMetadata(string Scheme, int? Count, IReadOnlyList<double>? Breaks, IReadOnlyList<string>? Labels);

internal sealed record YieldStatisticsMetadata(int Count, double Mean, double Median, double StdDev, double Min, double Max, double TotalMassKg);

internal sealed record LegacyYieldSample(DateTimeOffset Timestamp, double EastingMeters, double NorthingMeters, double YieldKgPerHa, double? MoisturePercent, double AreaHa);

internal sealed record LayerProvenanceDocument(string Source, string Transform, string Hash, DateTimeOffset CreatedAt, string Actor);

internal static class TelemetrySchemas
{
    internal static class Pose
    {
        public static readonly DataField<ulong> Sequence = new("sequence");
        public static readonly DateTimeDataField Timestamp = new("timestamp_utc", DateTimeFormat.DateAndTime, true);
        public static readonly DataField<string?> Frame = new("frame");
        public static readonly DataField<string?> Source = new("source");
        public static readonly DataField<string?> JobId = new("job_id");
        public static readonly DataField<string?> SeasonId = new("season_id");
        public static readonly DataField<string?> SessionId = new("session_id");
        public static readonly DataField<double> LatitudeDeg = new("latitude_deg");
        public static readonly DataField<double> LongitudeDeg = new("longitude_deg");
        public static readonly DataField<double> AltitudeM = new("altitude_m");
        public static readonly DataField<double> HeadingRad = new("heading_rad");
        public static readonly DataField<double> RollRad = new("roll_rad");
        public static readonly DataField<double> PitchRad = new("pitch_rad");
        public static readonly DataField<double> SpeedMps = new("speed_mps");
        public static readonly DataField<double> YawRateRadps = new("yaw_rate_radps");
        public static readonly ParquetSchema Schema = new(
            Sequence,
            Timestamp,
            Frame,
            Source,
            JobId,
            SeasonId,
            SessionId,
            LatitudeDeg,
            LongitudeDeg,
            AltitudeM,
            HeadingRad,
            RollRad,
            PitchRad,
            SpeedMps,
            YawRateRadps);
    }

    internal static class Imu
    {
        public static readonly DataField<ulong> Sequence = new("sequence");
        public static readonly DateTimeDataField Timestamp = new("timestamp_utc", DateTimeFormat.DateAndTime, true);
        public static readonly DataField<string?> Frame = new("frame");
        public static readonly DataField<string?> Source = new("source");
        public static readonly DataField<string?> JobId = new("job_id");
        public static readonly DataField<string?> SeasonId = new("season_id");
        public static readonly DataField<string?> SessionId = new("session_id");
        public static readonly DataField<double> AccelXMps2 = new("accel_x_mps2");
        public static readonly DataField<double> AccelYMps2 = new("accel_y_mps2");
        public static readonly DataField<double> AccelZMps2 = new("accel_z_mps2");
        public static readonly DataField<double> GyroXRadps = new("gyro_x_radps");
        public static readonly DataField<double> GyroYRadps = new("gyro_y_radps");
        public static readonly DataField<double> GyroZRadps = new("gyro_z_radps");
        public static readonly DataField<double> MagXUt = new("mag_x_ut");
        public static readonly DataField<double> MagYUt = new("mag_y_ut");
        public static readonly DataField<double> MagZUt = new("mag_z_ut");
        public static readonly DataField<double> TemperatureC = new("temperature_c");
        public static readonly ParquetSchema Schema = new(
            Sequence,
            Timestamp,
            Frame,
            Source,
            JobId,
            SeasonId,
            SessionId,
            AccelXMps2,
            AccelYMps2,
            AccelZMps2,
            GyroXRadps,
            GyroYRadps,
            GyroZRadps,
            MagXUt,
            MagYUt,
            MagZUt,
            TemperatureC);
    }

    internal static class Can
    {
        public static readonly DataField<ulong> Sequence = new("sequence");
        public static readonly DateTimeDataField Timestamp = new("timestamp_utc", DateTimeFormat.DateAndTime, true);
        public static readonly DataField<string?> Frame = new("frame");
        public static readonly DataField<string?> Source = new("source");
        public static readonly DataField<string?> JobId = new("job_id");
        public static readonly DataField<string?> SeasonId = new("season_id");
        public static readonly DataField<string?> SessionId = new("session_id");
        public static readonly DataField<uint> ArbitrationId = new("arbitration_id");
        public static readonly DataField<byte[]?> Payload = new("payload");
        public static readonly DataField<bool> IsExtendedId = new("is_extended_id");
        public static readonly DataField<bool> IsRemoteRequest = new("is_remote_request");
        public static readonly ParquetSchema Schema = new(
            Sequence,
            Timestamp,
            Frame,
            Source,
            JobId,
            SeasonId,
            SessionId,
            ArbitrationId,
            Payload,
            IsExtendedId,
            IsRemoteRequest);
    }

    internal static class Io
    {
        public static readonly DataField<ulong> Sequence = new("sequence");
        public static readonly DateTimeDataField Timestamp = new("timestamp_utc", DateTimeFormat.DateAndTime, true);
        public static readonly DataField<string?> Frame = new("frame");
        public static readonly DataField<string?> Source = new("source");
        public static readonly DataField<string?> JobId = new("job_id");
        public static readonly DataField<string?> SeasonId = new("season_id");
        public static readonly DataField<string?> SessionId = new("session_id");
        public static readonly DataField<uint> SectionCount = new("section_count");
        public static readonly DataField<uint> Mask = new("mask");
        public static readonly ParquetSchema Schema = new(
            Sequence,
            Timestamp,
            Frame,
            Source,
            JobId,
            SeasonId,
            SessionId,
            SectionCount,
            Mask);
    }

    internal static class Plugin
    {
        public static readonly DataField<ulong> Sequence = new("sequence");
        public static readonly DateTimeDataField Timestamp = new("timestamp_utc", DateTimeFormat.DateAndTime, true);
        public static readonly DataField<string?> Source = new("source");
        public static readonly DataField<string?> JobId = new("job_id");
        public static readonly DataField<string?> SeasonId = new("season_id");
        public static readonly DataField<string?> SessionId = new("session_id");
        public static readonly DataField<string> PluginId = new("plugin_id");
        public static readonly DataField<string> Topic = new("topic");
        public static readonly DataField<byte[]?> Payload = new("payload");
        public static readonly ParquetSchema Schema = new(
            Sequence,
            Timestamp,
            Source,
            JobId,
            SeasonId,
            SessionId,
            PluginId,
            Topic,
            Payload);
    }

    internal static class Weather
    {
        public static readonly DataField<ulong> Sequence = new("sequence");
        public static readonly DateTimeDataField Timestamp = new("timestamp_utc", DateTimeFormat.DateAndTime, true);
        public static readonly DataField<string?> Source = new("source");
        public static readonly DataField<double?> TemperatureC = new("temperature_c");
        public static readonly DataField<double?> HumidityPct = new("humidity_pct");
        public static readonly DataField<double?> WindKph = new("wind_kph");
        public static readonly DataField<double?> WindDirectionDeg = new("wind_dir_deg");
        public static readonly DataField<double?> WindGustKph = new("wind_gust_kph");
        public static readonly DataField<double?> RainfallMm = new("rainfall_mm");
        public static readonly DataField<double?> PressureKpa = new("pressure_kpa");
        public static readonly DataField<double?> DewPointC = new("dew_point_c");
        public static readonly DataField<double?> WetBulbC = new("wet_bulb_c");
        public static readonly DataField<double?> DeltaTC = new("delta_t_c");
        public static readonly DataField<double?> EvapotranspirationMm = new("evapotranspiration_mm");
        public static readonly DataField<double?> SolarIrradianceWm2 = new("solar_irradiance_wm2");
        public static readonly DataField<double?> UvIndex = new("uv_index");
        public static readonly DataField<double?> CloudCoverPct = new("cloud_cover_pct");
        public static readonly DataField<double?> VisibilityKm = new("visibility_km");
        public static readonly DataField<double?> SoilTempC = new("soil_temp_c");
        public static readonly DataField<double?> SoilMoisturePct = new("soil_moisture_pct");
        public static readonly DataField<double?> LeafWetnessPct = new("leaf_wetness_pct");
        public static readonly ParquetSchema Schema = new(
            Sequence,
            Timestamp,
            Source,
            TemperatureC,
            HumidityPct,
            WindKph,
            WindDirectionDeg,
            WindGustKph,
            RainfallMm,
            PressureKpa,
            DewPointC,
            WetBulbC,
            DeltaTC,
            EvapotranspirationMm,
            SolarIrradianceWm2,
            UvIndex,
            CloudCoverPct,
            VisibilityKm,
            SoilTempC,
            SoilMoisturePct,
            LeafWetnessPct);
    }
}
