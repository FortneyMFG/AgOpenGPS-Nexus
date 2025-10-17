using System.IO;
using System.Text.Json.Serialization;
using Aog.Core.Replay;

namespace Aog.Plugins.TelemetryLogging;

/// <summary>
/// Describes a captured telemetry session and the files required to replay it.
/// </summary>
public sealed record TelemetryLogManifest
{
    /// <summary>
    /// Gets the current schema version written by the telemetry logging plugin.
    /// </summary>
    public const string CurrentSchemaVersion = "1.0.0";

    /// <summary>
    /// Gets or sets the schema version used to serialize this manifest.
    /// </summary>
    [JsonPropertyName("schemaVersion")]
    public string SchemaVersion { get; set; } = CurrentSchemaVersion;

    /// <summary>
    /// Gets or sets the identifier of the recorded session.
    /// </summary>
    [JsonPropertyName("sessionId")]
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the identifier of the job that produced the session.
    /// </summary>
    [JsonPropertyName("jobId")]
    public string JobId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the slug used when creating the session directory.
    /// </summary>
    [JsonPropertyName("jobSlug")]
    public string JobSlug { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the human friendly job display name.
    /// </summary>
    [JsonPropertyName("jobDisplayName")]
    public string JobDisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the farm identifier associated with the job.
    /// </summary>
    [JsonPropertyName("farmId")]
    public string? FarmId { get; set; }

    /// <summary>
    /// Gets the collection of field identifiers associated with the job.
    /// </summary>
    [JsonPropertyName("fieldIds")]
    public List<string> FieldIds { get; init; } = new();

    /// <summary>
    /// Gets or sets the optional season identifier associated with the job.
    /// </summary>
    [JsonPropertyName("seasonId")]
    public string? SeasonId { get; set; }

    /// <summary>
    /// Gets or sets the optional work order identifier associated with the job.
    /// </summary>
    [JsonPropertyName("workOrderId")]
    public string? WorkOrderId { get; set; }

    /// <summary>
    /// Gets the tags applied to the job when the session was recorded.
    /// </summary>
    [JsonPropertyName("jobTags")]
    public List<string> JobTags { get; init; } = new();

    /// <summary>
    /// Gets or sets the timestamp when logging began (UTC).
    /// </summary>
    [JsonPropertyName("startedAt")]
    public DateTimeOffset StartedAt { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when logging stopped (UTC).
    /// </summary>
    [JsonPropertyName("endedAt")]
    public DateTimeOffset? EndedAt { get; set; }

    /// <summary>
    /// Gets or sets the optional reason supplied when the session closed.
    /// </summary>
    [JsonPropertyName("closeReason")]
    public string? CloseReason { get; set; }

    /// <summary>
    /// Gets or sets the relative directory (rooted at the logging root) containing the session files.
    /// </summary>
    [JsonPropertyName("directory")]
    public string Directory { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the file names associated with the session.
    /// </summary>
    [JsonPropertyName("files")]
    public TelemetryLogFileNames Files { get; set; } = new();

    /// <summary>
    /// Resolves an absolute path for the session directory using the supplied root.
    /// </summary>
    /// <param name="rootDirectory">Root directory configured for telemetry logging.</param>
    public string ResolveSessionDirectory(string rootDirectory)
    {
        if (string.IsNullOrWhiteSpace(rootDirectory))
        {
            throw new ArgumentException("Root directory must be provided.", nameof(rootDirectory));
        }

        return Path.GetFullPath(Path.Combine(rootDirectory, Directory ?? string.Empty));
    }

    /// <summary>
    /// Creates <see cref="TelemetryReplayOptions"/> that reference the recorded files.
    /// </summary>
    /// <param name="rootDirectory">Root directory configured for telemetry logging.</param>
    public TelemetryReplayOptions CreateReplayOptions(string rootDirectory)
    {
        var inputDirectory = ResolveSessionDirectory(rootDirectory);
        return new TelemetryReplayOptions
        {
            InputDirectory = inputDirectory,
            PoseFileName = Files.Pose,
            ImuFileName = Files.Imu,
            CanFileName = Files.Can,
            IoFileName = Files.Io,
            PluginFileName = Files.Plugin
        };
    }

    /// <summary>
    /// Normalises metadata after deserialization, ensuring optional collections are populated.
    /// </summary>
    public TelemetryLogManifest Normalise()
    {
        var normalizedFieldIds = FieldIds?.Where(id => !string.IsNullOrWhiteSpace(id)).Select(id => id.Trim()).ToList()
            ?? new List<string>();
        var normalizedJobTags = JobTags?.Where(tag => !string.IsNullOrWhiteSpace(tag)).Select(tag => tag.Trim()).ToList()
            ?? new List<string>();
        var normalizedFiles = Files ?? new TelemetryLogFileNames();
        
        return this with
        {
            FieldIds = normalizedFieldIds,
            JobTags = normalizedJobTags,
            Files = normalizedFiles
        };
    }
}
