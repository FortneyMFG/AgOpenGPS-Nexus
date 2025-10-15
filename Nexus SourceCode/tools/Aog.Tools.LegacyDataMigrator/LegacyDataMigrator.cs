using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Parquet;
using Parquet.Data;
using Parquet.Schema;

namespace Aog.Tools.LegacyDataMigrator;

/// <summary>
/// Migrates legacy AgOpenGPS V6 logs and field history files into Nexus-compatible formats.
/// </summary>
public sealed class LegacyDataMigrator
{
    /// <summary>
    /// Migrates telemetry logs and field histories into the output directory.
    /// </summary>
    /// <param name="options">Migration options.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>Summary describing migrated artefacts.</returns>
    public Task<LegacyMigrationReport> MigrateAsync(
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
                report = report with { PoseCount = report.PoseCount + WritePose(posePath, options.OutputDirectory) };
            }
            else
            {
                report = AppendSkipped(report, posePath + " (missing)");
            }

            var imuPath = Path.Combine(logsRoot, "imu.csv");
            if (File.Exists(imuPath))
            {
                report = report with { ImuCount = report.ImuCount + WriteImu(imuPath, options.OutputDirectory) };
            }
            else
            {
                report = AppendSkipped(report, imuPath + " (missing)");
            }

            var canPath = Path.Combine(logsRoot, "can.csv");
            if (File.Exists(canPath))
            {
                report = report with { CanCount = report.CanCount + WriteCan(canPath, options.OutputDirectory) };
            }
            else
            {
                report = AppendSkipped(report, canPath + " (missing)");
            }

            var sectionsPath = Path.Combine(logsRoot, "sections.csv");
            if (File.Exists(sectionsPath))
            {
                report = report with { SectionCount = report.SectionCount + WriteSections(sectionsPath, options.OutputDirectory) };
            }
            else
            {
                report = AppendSkipped(report, sectionsPath + " (missing)");
            }

            var pluginPath = Path.Combine(logsRoot, "plugin.csv");
            if (File.Exists(pluginPath))
            {
                report = report with { PluginCount = report.PluginCount + WritePlugin(pluginPath, options.OutputDirectory) };
            }
            else
            {
                report = AppendSkipped(report, pluginPath + " (missing)");
            }
        }
        else
        {
            report = AppendSkipped(report, logsRoot + " (missing)");
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

        return Task.FromResult(report);
    }

    private static LegacyMigrationReport AppendSkipped(LegacyMigrationReport report, string path)
    {
        var entries = report.SkippedFiles.Append(path).ToList();
        return report with { SkippedFiles = entries };
    }

    private static int WritePose(string filePath, string outputDirectory)
    {
        var rows = LegacyPoseCsvParser.Parse(filePath);
        if (rows.Count == 0)
        {
            return 0;
        }

        var schema = TelemetrySchemas.Pose.Schema;
        using var stream = CreateParquetStream(outputDirectory, "pose.parquet");
        using var writer = new ParquetWriter(schema, stream);
        using var rowGroup = writer.CreateRowGroup(rows.Count);

        rowGroup.WriteColumn(new DataColumn(TelemetrySchemas.Pose.Sequence, rows.Select(r => r.Sequence).ToArray()));
        rowGroup.WriteColumn(new DataColumn(TelemetrySchemas.Pose.Timestamp, rows.Select(r => r.TimestampUtc).ToArray()));
        rowGroup.WriteColumn(new DataColumn(TelemetrySchemas.Pose.Frame, rows.Select(r => r.Frame).ToArray()));
        rowGroup.WriteColumn(new DataColumn(TelemetrySchemas.Pose.Source, rows.Select(r => r.Source).ToArray()));
        rowGroup.WriteColumn(CreateEmptyStringColumn(TelemetrySchemas.Pose.JobId, rows.Count));
        rowGroup.WriteColumn(CreateEmptyStringColumn(TelemetrySchemas.Pose.SeasonId, rows.Count));
        rowGroup.WriteColumn(CreateEmptyStringColumn(TelemetrySchemas.Pose.SessionId, rows.Count));
        rowGroup.WriteColumn(new DataColumn(TelemetrySchemas.Pose.LatitudeDeg, rows.Select(r => r.LatitudeDeg).ToArray()));
        rowGroup.WriteColumn(new DataColumn(TelemetrySchemas.Pose.LongitudeDeg, rows.Select(r => r.LongitudeDeg).ToArray()));
        rowGroup.WriteColumn(new DataColumn(TelemetrySchemas.Pose.AltitudeM, rows.Select(r => r.AltitudeM).ToArray()));
        rowGroup.WriteColumn(new DataColumn(TelemetrySchemas.Pose.HeadingRad, rows.Select(r => r.HeadingRad).ToArray()));
        rowGroup.WriteColumn(new DataColumn(TelemetrySchemas.Pose.RollRad, rows.Select(r => r.RollRad).ToArray()));
        rowGroup.WriteColumn(new DataColumn(TelemetrySchemas.Pose.PitchRad, rows.Select(r => r.PitchRad).ToArray()));
        rowGroup.WriteColumn(new DataColumn(TelemetrySchemas.Pose.SpeedMps, rows.Select(r => r.SpeedMps).ToArray()));
        rowGroup.WriteColumn(new DataColumn(TelemetrySchemas.Pose.YawRateRadps, rows.Select(r => r.YawRateRadps).ToArray()));

        return rows.Count;
    }

    private static int WriteImu(string filePath, string outputDirectory)
    {
        var rows = LegacyImuCsvParser.Parse(filePath);
        if (rows.Count == 0)
        {
            return 0;
        }

        using var stream = CreateParquetStream(outputDirectory, "imu.parquet");
        using var writer = new ParquetWriter(TelemetrySchemas.Imu.Schema, stream);
        using var rowGroup = writer.CreateRowGroup(rows.Count);

        rowGroup.WriteColumn(new DataColumn(TelemetrySchemas.Imu.Sequence, rows.Select(r => r.Sequence).ToArray()));
        rowGroup.WriteColumn(new DataColumn(TelemetrySchemas.Imu.Timestamp, rows.Select(r => r.TimestampUtc).ToArray()));
        rowGroup.WriteColumn(new DataColumn(TelemetrySchemas.Imu.Frame, rows.Select(r => r.Frame).ToArray()));
        rowGroup.WriteColumn(new DataColumn(TelemetrySchemas.Imu.Source, rows.Select(r => r.Source).ToArray()));
        rowGroup.WriteColumn(CreateEmptyStringColumn(TelemetrySchemas.Imu.JobId, rows.Count));
        rowGroup.WriteColumn(CreateEmptyStringColumn(TelemetrySchemas.Imu.SeasonId, rows.Count));
        rowGroup.WriteColumn(CreateEmptyStringColumn(TelemetrySchemas.Imu.SessionId, rows.Count));
        rowGroup.WriteColumn(new DataColumn(TelemetrySchemas.Imu.AccelXMps2, rows.Select(r => r.AccelXMps2).ToArray()));
        rowGroup.WriteColumn(new DataColumn(TelemetrySchemas.Imu.AccelYMps2, rows.Select(r => r.AccelYMps2).ToArray()));
        rowGroup.WriteColumn(new DataColumn(TelemetrySchemas.Imu.AccelZMps2, rows.Select(r => r.AccelZMps2).ToArray()));
        rowGroup.WriteColumn(new DataColumn(TelemetrySchemas.Imu.GyroXRadps, rows.Select(r => r.GyroXRadps).ToArray()));
        rowGroup.WriteColumn(new DataColumn(TelemetrySchemas.Imu.GyroYRadps, rows.Select(r => r.GyroYRadps).ToArray()));
        rowGroup.WriteColumn(new DataColumn(TelemetrySchemas.Imu.GyroZRadps, rows.Select(r => r.GyroZRadps).ToArray()));
        rowGroup.WriteColumn(new DataColumn(TelemetrySchemas.Imu.MagXUt, rows.Select(r => r.MagXUt).ToArray()));
        rowGroup.WriteColumn(new DataColumn(TelemetrySchemas.Imu.MagYUt, rows.Select(r => r.MagYUt).ToArray()));
        rowGroup.WriteColumn(new DataColumn(TelemetrySchemas.Imu.MagZUt, rows.Select(r => r.MagZUt).ToArray()));
        rowGroup.WriteColumn(new DataColumn(TelemetrySchemas.Imu.TemperatureC, rows.Select(r => r.TemperatureC).ToArray()));

        return rows.Count;
    }

    private static int WriteCan(string filePath, string outputDirectory)
    {
        var rows = LegacyCanCsvParser.Parse(filePath);
        if (rows.Count == 0)
        {
            return 0;
        }

        using var stream = CreateParquetStream(outputDirectory, "can.parquet");
        using var writer = new ParquetWriter(TelemetrySchemas.Can.Schema, stream);
        using var rowGroup = writer.CreateRowGroup(rows.Count);

        rowGroup.WriteColumn(new DataColumn(TelemetrySchemas.Can.Sequence, rows.Select(r => r.Sequence).ToArray()));
        rowGroup.WriteColumn(new DataColumn(TelemetrySchemas.Can.Timestamp, rows.Select(r => r.TimestampUtc).ToArray()));
        rowGroup.WriteColumn(new DataColumn(TelemetrySchemas.Can.Frame, rows.Select(r => r.Frame).ToArray()));
        rowGroup.WriteColumn(new DataColumn(TelemetrySchemas.Can.Source, rows.Select(r => r.Source).ToArray()));
        rowGroup.WriteColumn(CreateEmptyStringColumn(TelemetrySchemas.Can.JobId, rows.Count));
        rowGroup.WriteColumn(CreateEmptyStringColumn(TelemetrySchemas.Can.SeasonId, rows.Count));
        rowGroup.WriteColumn(CreateEmptyStringColumn(TelemetrySchemas.Can.SessionId, rows.Count));
        rowGroup.WriteColumn(new DataColumn(TelemetrySchemas.Can.ArbitrationId, rows.Select(r => r.ArbitrationId).ToArray()));
        rowGroup.WriteColumn(new DataColumn(TelemetrySchemas.Can.Payload, rows.Select(r => r.Payload).ToArray()));
        rowGroup.WriteColumn(new DataColumn(TelemetrySchemas.Can.IsExtendedId, rows.Select(r => r.IsExtendedId).ToArray()));
        rowGroup.WriteColumn(new DataColumn(TelemetrySchemas.Can.IsRemoteRequest, rows.Select(r => r.IsRemoteRequest).ToArray()));

        return rows.Count;
    }

    private static int WriteSections(string filePath, string outputDirectory)
    {
        var rows = LegacySectionCsvParser.Parse(filePath);
        if (rows.Count == 0)
        {
            return 0;
        }

        using var stream = CreateParquetStream(outputDirectory, "io.parquet");
        using var writer = new ParquetWriter(TelemetrySchemas.Io.Schema, stream);
        using var rowGroup = writer.CreateRowGroup(rows.Count);

        rowGroup.WriteColumn(new DataColumn(TelemetrySchemas.Io.Sequence, rows.Select(r => r.Sequence).ToArray()));
        rowGroup.WriteColumn(new DataColumn(TelemetrySchemas.Io.Timestamp, rows.Select(r => r.TimestampUtc).ToArray()));
        rowGroup.WriteColumn(new DataColumn(TelemetrySchemas.Io.Frame, rows.Select(r => r.Frame).ToArray()));
        rowGroup.WriteColumn(new DataColumn(TelemetrySchemas.Io.Source, rows.Select(r => r.Source).ToArray()));
        rowGroup.WriteColumn(CreateEmptyStringColumn(TelemetrySchemas.Io.JobId, rows.Count));
        rowGroup.WriteColumn(CreateEmptyStringColumn(TelemetrySchemas.Io.SeasonId, rows.Count));
        rowGroup.WriteColumn(CreateEmptyStringColumn(TelemetrySchemas.Io.SessionId, rows.Count));
        rowGroup.WriteColumn(new DataColumn(TelemetrySchemas.Io.SectionCount, rows.Select(r => r.SectionCount).ToArray()));
        rowGroup.WriteColumn(new DataColumn(TelemetrySchemas.Io.Mask, rows.Select(r => r.Mask).ToArray()));

        return rows.Count;
    }

    private static int WritePlugin(string filePath, string outputDirectory)
    {
        var rows = LegacyPluginCsvParser.Parse(filePath);
        if (rows.Count == 0)
        {
            return 0;
        }

        using var stream = CreateParquetStream(outputDirectory, "plugin.parquet");
        using var writer = new ParquetWriter(TelemetrySchemas.Plugin.Schema, stream);
        using var rowGroup = writer.CreateRowGroup(rows.Count);

        rowGroup.WriteColumn(new DataColumn(TelemetrySchemas.Plugin.Sequence, rows.Select(r => r.Sequence).ToArray()));
        rowGroup.WriteColumn(new DataColumn(TelemetrySchemas.Plugin.Timestamp, rows.Select(r => r.TimestampUtc).ToArray()));
        rowGroup.WriteColumn(new DataColumn(TelemetrySchemas.Plugin.Source, rows.Select(r => r.Source).ToArray()));
        rowGroup.WriteColumn(CreateEmptyStringColumn(TelemetrySchemas.Plugin.JobId, rows.Count));
        rowGroup.WriteColumn(CreateEmptyStringColumn(TelemetrySchemas.Plugin.SeasonId, rows.Count));
        rowGroup.WriteColumn(CreateEmptyStringColumn(TelemetrySchemas.Plugin.SessionId, rows.Count));
        rowGroup.WriteColumn(new DataColumn(TelemetrySchemas.Plugin.PluginId, rows.Select(r => r.PluginId).ToArray()));
        rowGroup.WriteColumn(new DataColumn(TelemetrySchemas.Plugin.Topic, rows.Select(r => r.Topic).ToArray()));
        rowGroup.WriteColumn(new DataColumn(TelemetrySchemas.Plugin.Payload, rows.Select(r => r.Payload).ToArray()));

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

    internal void Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(InputDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(OutputDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(LogsDirectoryName);
        ArgumentException.ThrowIfNullOrWhiteSpace(FieldHistoryFileName);

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
    int FieldHistoryFields = 0,
    int FieldHistoryEntries = 0,
    IReadOnlyList<string> SkippedFiles = null!)
{
    public LegacyMigrationReport(string outputDirectory)
        : this(outputDirectory, 0, 0, 0, 0, 0, 0, 0, Array.Empty<string>())
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
                uint.Parse(parts[4], CultureInfo.InvariantCulture),
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

internal sealed record FieldHistoryDocument(IReadOnlyList<FieldHistoryField> Fields);

internal sealed record FieldHistoryField(string FieldName, IReadOnlyList<FieldHistoryEntry> Entries);

internal sealed record FieldHistoryEntry(DateTime TimestampUtc, double AreaHectares, string? Operator);

internal static class TelemetrySchemas
{
    internal static class Pose
    {
        public static readonly DataField<ulong> Sequence = new("sequence");
        public static readonly DateTimeDataField Timestamp = new("timestamp_utc", DateTimeFormat.DateAndTime, hasNulls: true);
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
        public static readonly Schema Schema = new(
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
        public static readonly DateTimeDataField Timestamp = new("timestamp_utc", DateTimeFormat.DateAndTime, hasNulls: true);
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
        public static readonly Schema Schema = new(
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
        public static readonly DateTimeDataField Timestamp = new("timestamp_utc", DateTimeFormat.DateAndTime, hasNulls: true);
        public static readonly DataField<string?> Frame = new("frame");
        public static readonly DataField<string?> Source = new("source");
        public static readonly DataField<string?> JobId = new("job_id");
        public static readonly DataField<string?> SeasonId = new("season_id");
        public static readonly DataField<string?> SessionId = new("session_id");
        public static readonly DataField<uint> ArbitrationId = new("arbitration_id");
        public static readonly DataField<byte[]?> Payload = new("payload");
        public static readonly DataField<bool> IsExtendedId = new("is_extended_id");
        public static readonly DataField<bool> IsRemoteRequest = new("is_remote_request");
        public static readonly Schema Schema = new(
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
        public static readonly DateTimeDataField Timestamp = new("timestamp_utc", DateTimeFormat.DateAndTime, hasNulls: true);
        public static readonly DataField<string?> Frame = new("frame");
        public static readonly DataField<string?> Source = new("source");
        public static readonly DataField<string?> JobId = new("job_id");
        public static readonly DataField<string?> SeasonId = new("season_id");
        public static readonly DataField<string?> SessionId = new("session_id");
        public static readonly DataField<uint> SectionCount = new("section_count");
        public static readonly DataField<uint> Mask = new("mask");
        public static readonly Schema Schema = new(
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
        public static readonly DateTimeDataField Timestamp = new("timestamp_utc", DateTimeFormat.DateAndTime, hasNulls: true);
        public static readonly DataField<string?> Source = new("source");
        public static readonly DataField<string?> JobId = new("job_id");
        public static readonly DataField<string?> SeasonId = new("season_id");
        public static readonly DataField<string?> SessionId = new("session_id");
        public static readonly DataField<string> PluginId = new("plugin_id");
        public static readonly DataField<string> Topic = new("topic");
        public static readonly DataField<byte[]?> Payload = new("payload");
        public static readonly Schema Schema = new(
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
}
