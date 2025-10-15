using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace Aog.Tools.LegacyJobMigrator;

/// <summary>
/// Migrates legacy <c>job.json</c> documents into the ADR-041 session-aware schema.
/// </summary>
public sealed class LegacyJobMigrator
{
    private const string TargetSchemaVersion = "1.1.0";

    /// <summary>
    /// Migrates the provided job document.
    /// </summary>
    /// <param name="options">Migration options.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A report describing the migration outcome.</returns>
    public async Task<LegacyJobMigrationReport> MigrateAsync(
        LegacyJobMigrationOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();
        cancellationToken.ThrowIfCancellationRequested();

        var inputPath = Path.GetFullPath(options.JobFilePath);
        var outputPath = Path.GetFullPath(options.OutputFilePath ?? inputPath);

        await using var stream = File.OpenRead(inputPath);
        var node = await JsonNode.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (node is not JsonObject root)
        {
            throw new InvalidDataException("job.json must contain a JSON object.");
        }

        var existingSessions = root["sessions"] as JsonArray;
        var hasSessions = existingSessions is { Count: > 0 };
        if (hasSessions && !options.Force)
        {
            return new LegacyJobMigrationReport(
                inputPath,
                outputPath,
                updated: false,
                skipped: true,
                sessionId: null,
                seasonAssigned: false);
        }

        var jobState = GetString(root, "state");
        var createdAt = GetTimestamp(root, "createdAt") ?? DateTimeOffset.UtcNow;
        var updatedAt = GetTimestamp(root, "updatedAt") ?? createdAt;
        var startedAt = GetTimestamp(root, "startedAt") ?? createdAt;

        var sessionState = MapSessionState(jobState);
        var sessionId = options.SessionId ?? DetermineSessionId(root);
        var sessionName = !string.IsNullOrWhiteSpace(options.SessionName)
            ? options.SessionName!
            : "Migrated Session";

        var endedAt = DetermineSessionEnd(sessionState, root, updatedAt);

        var session = new JsonObject
        {
            ["id"] = sessionId,
            ["state"] = sessionState,
            ["startedAt"] = FormatTimestamp(startedAt),
            ["lastModifiedAt"] = FormatTimestamp(updatedAt),
        };

        if (!string.IsNullOrWhiteSpace(sessionName))
        {
            session["name"] = sessionName;
        }

        if (endedAt is not null)
        {
            session["endedAt"] = FormatTimestamp(endedAt.Value);
        }

        var operators = options.OperatorIds?.Where(o => !string.IsNullOrWhiteSpace(o)).Select(o => o!).Distinct(StringComparer.OrdinalIgnoreCase).ToList()
            ?? new List<string>();
        if (operators.Count > 0)
        {
            var activeOperators = new JsonArray();
            foreach (var op in operators)
            {
                activeOperators.Add(op);
            }

            session["activeOperators"] = activeOperators;
        }

        var jobStats = GetOrCreateObject(root, "stats");
        var sessionStats = BuildSessionStats(jobStats);
        if (sessionStats.Count > 0)
        {
            session["stats"] = sessionStats;
        }

        var sessions = new JsonArray
        {
            session,
        };

        root["sessions"] = sessions;
        root["schemaVersion"] = TargetSchemaVersion;
        root["startedAt"] = FormatTimestamp(startedAt);

        if (sessionState == "completed" && endedAt is not null)
        {
            root["endedAt"] = FormatTimestamp(endedAt.Value);
        }
        else
        {
            root.Remove("endedAt");
        }

        if (sessionState is "active" or "paused")
        {
            root["activeSessionId"] = sessionId;
        }
        else
        {
            root.Remove("activeSessionId");
        }

        var completedSessions = sessionState == "completed" ? 1 : 0;
        jobStats["completedSessionCount"] = completedSessions;

        var assignedSeason = AssignSeason(root, options.SeasonId);

        var json = root.ToJsonString(new JsonSerializerOptions
        {
            WriteIndented = options.WriteIndented,
        });

        var outputDirectory = Path.GetDirectoryName(outputPath) ?? Environment.CurrentDirectory;
        Directory.CreateDirectory(outputDirectory);

        var tempPath = Path.Combine(outputDirectory, $".{Path.GetFileName(outputPath)}.{Guid.NewGuid():N}.tmp");
        try
        {
            await File.WriteAllTextAsync(tempPath, json, cancellationToken).ConfigureAwait(false);
            File.Move(tempPath, outputPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }

        return new LegacyJobMigrationReport(
            inputPath,
            outputPath,
            updated: true,
            skipped: false,
            sessionId,
            assignedSeason);
    }

    private static JsonObject BuildSessionStats(JsonObject jobStats)
    {
        var stats = new JsonObject();
        CopyStat(jobStats, stats, "totalAreaHa", "areaHa");
        CopyStat(jobStats, stats, "totalDistanceKm", "distanceKm");
        CopyStat(jobStats, stats, "activeDurationSec", "durationSec");
        CopyStat(jobStats, stats, "coveragePct", "coveragePct");
        return stats;
    }

    private static void CopyStat(JsonObject source, JsonObject destination, string sourceName, string destinationName)
    {
        if (source.TryGetPropertyValue(sourceName, out var node) && node is JsonValue value)
        {
            if (TryGetNumber(value, out var number))
            {
                destination[destinationName] = number;
            }
        }
    }

    private static bool TryGetNumber(JsonValue value, out double number)
    {
        if (value.TryGetValue<double>(out var dbl))
        {
            number = dbl;
            return true;
        }

        if (value.TryGetValue<int>(out var intValue))
        {
            number = intValue;
            return true;
        }

        if (value.TryGetValue<long>(out var longValue))
        {
            number = longValue;
            return true;
        }

        if (value.TryGetValue<float>(out var floatValue))
        {
            number = floatValue;
            return true;
        }

        number = 0;
        return false;
    }

    private static bool AssignSeason(JsonObject root, string? seasonId)
    {
        if (string.IsNullOrWhiteSpace(seasonId))
        {
            return false;
        }

        var context = GetOrCreateObject(root, "context");
        var existing = GetString(context, "seasonId");
        if (string.Equals(existing, seasonId, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        context["seasonId"] = seasonId;
        return true;
    }

    private static string DetermineSessionId(JsonObject root)
    {
        var stats = root["stats"] as JsonObject;
        var existingCount = GetInt(stats, "completedSessionCount");
        if (existingCount is int count && count >= 0)
        {
            return $"session:{count + 1}";
        }

        return "session:1";
    }

    private static int? GetInt(JsonObject? obj, string propertyName)
    {
        if (obj is null)
        {
            return null;
        }

        if (obj.TryGetPropertyValue(propertyName, out var node) && node is JsonValue value)
        {
            if (value.TryGetValue<int>(out var intValue))
            {
                return intValue;
            }

            if (value.TryGetValue<long>(out var longValue))
            {
                return (int)longValue;
            }
        }

        return null;
    }

    private static DateTimeOffset? DetermineSessionEnd(string sessionState, JsonObject root, DateTimeOffset fallback)
    {
        if (sessionState == "completed")
        {
            return GetTimestamp(root, "endedAt") ?? fallback;
        }

        return GetTimestamp(root, "endedAt");
    }

    private static string MapSessionState(string? jobState)
        => jobState?.ToLowerInvariant() switch
        {
            "active" => "active",
            "mounted" => "active",
            "paused" => "paused",
            _ => "completed",
        };

    private static JsonObject GetOrCreateObject(JsonObject root, string propertyName)
    {
        if (root.TryGetPropertyValue(propertyName, out var node) && node is JsonObject existing)
        {
            return existing;
        }

        var created = new JsonObject();
        root[propertyName] = created;
        return created;
    }

    private static string? GetString(JsonObject root, string propertyName)
    {
        if (root.TryGetPropertyValue(propertyName, out var node) && node is JsonValue value && value.TryGetValue<string>(out var result))
        {
            return result;
        }

        return null;
    }

    private static DateTimeOffset? GetTimestamp(JsonObject root, string propertyName)
    {
        var text = GetString(root, propertyName);
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        if (DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var parsed))
        {
            return parsed;
        }

        return null;
    }

    private static string FormatTimestamp(DateTimeOffset timestamp)
        => timestamp.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);
}

/// <summary>
/// Options describing the job migration operation.
/// </summary>
public sealed class LegacyJobMigrationOptions
{
    /// <summary>
    /// Gets or sets the path to the legacy <c>job.json</c> file.
    /// </summary>
    public string JobFilePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the destination path for the migrated job file.
    /// Defaults to <see cref="JobFilePath"/>.
    /// </summary>
    public string? OutputFilePath { get; set; } = null;

    /// <summary>
    /// Gets or sets the optional season identifier to assign.
    /// </summary>
    public string? SeasonId { get; set; } = null;

    /// <summary>
    /// Gets or sets an optional session identifier. When not supplied a deterministic
    /// identifier is derived from the legacy stats.
    /// </summary>
    public string? SessionId { get; set; } = null;

    /// <summary>
    /// Gets or sets an optional session name to apply.
    /// </summary>
    public string? SessionName { get; set; } = null;

    /// <summary>
    /// Gets or sets the operator identifiers to seed on the generated session.
    /// </summary>
    public IReadOnlyList<string> OperatorIds { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets a value indicating whether existing sessions should be overwritten.
    /// </summary>
    public bool Force { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether the output JSON should be indented.
    /// </summary>
    public bool WriteIndented { get; set; } = true;

    internal void Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(JobFilePath);
        var fullPath = Path.GetFullPath(JobFilePath);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"job.json not found: {JobFilePath}");
        }

        if (!string.IsNullOrWhiteSpace(OutputFilePath))
        {
            Path.GetFullPath(OutputFilePath);
        }
    }
}

/// <summary>
/// Report describing the outcome of a migration.
/// </summary>
/// <param name="InputPath">Original job file path.</param>
/// <param name="OutputPath">Destination file path.</param>
/// <param name="Updated">Whether the job file was updated.</param>
/// <param name="Skipped">Whether the migration was skipped because sessions already existed.</param>
/// <param name="SessionId">Identifier of the session created during migration.</param>
/// <param name="SeasonAssigned">Whether a season identifier was assigned or updated.</param>
public sealed record LegacyJobMigrationReport(
    string InputPath,
    string OutputPath,
    bool Updated,
    bool Skipped,
    string? SessionId,
    bool SeasonAssigned);
