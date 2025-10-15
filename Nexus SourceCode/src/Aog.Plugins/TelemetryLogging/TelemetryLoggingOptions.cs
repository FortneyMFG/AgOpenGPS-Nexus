using System;
using System.IO;
using System.Text.Json.Serialization;

namespace Aog.Plugins.TelemetryLogging;

/// <summary>
/// Configuration used by the telemetry logging coordinator.
/// </summary>
public sealed class TelemetryLoggingOptions
{
    private string _rootDirectory = GetDefaultRootDirectory();
    private string _metadataFileName = "session.json";
    private TelemetryLogFileNames _files = new();

    /// <summary>
    /// Gets or sets the directory where session folders will be created.
    /// </summary>
    [JsonPropertyName("rootDirectory")]
    public string RootDirectory
    {
        get => _rootDirectory;
        set => _rootDirectory = string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Root directory must be provided.", nameof(value))
            : value;
    }

    /// <summary>
    /// Gets or sets the metadata file name written inside each session directory.
    /// </summary>
    [JsonPropertyName("metadataFileName")]
    public string MetadataFileName
    {
        get => _metadataFileName;
        set => _metadataFileName = string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Metadata file name must be provided.", nameof(value))
            : value;
    }

    /// <summary>
    /// Gets or sets the file names used when writing telemetry parquet files.
    /// </summary>
    [JsonPropertyName("files")]
    public TelemetryLogFileNames Files
    {
        get => _files;
        set => _files = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// Validates option values and normalises paths.
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(_rootDirectory))
        {
            throw new ArgumentException("Root directory must be provided.", nameof(RootDirectory));
        }

        if (string.IsNullOrWhiteSpace(_metadataFileName))
        {
            throw new ArgumentException("Metadata file name must be provided.", nameof(MetadataFileName));
        }

        _files ??= new TelemetryLogFileNames();
        _files.Validate();
    }

    private static string GetDefaultRootDirectory()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (string.IsNullOrWhiteSpace(home))
        {
            home = AppContext.BaseDirectory;
        }

        return Path.Combine(home, "AgOpenGPS", "telemetry");
    }
}
