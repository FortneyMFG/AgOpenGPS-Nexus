using System;
using System.Text.Json.Serialization;

namespace Aog.Plugins.TelemetryLogging;

/// <summary>
/// Represents the relative file names used to persist telemetry topics for a session.
/// </summary>
public sealed class TelemetryLogFileNames
{
    /// <summary>
    /// Gets or sets the file name used for pose telemetry.
    /// </summary>
    [JsonPropertyName("pose")]
    public string Pose { get; set; } = "pose.parquet";

    /// <summary>
    /// Gets or sets the file name used for IMU telemetry.
    /// </summary>
    [JsonPropertyName("imu")]
    public string Imu { get; set; } = "imu.parquet";

    /// <summary>
    /// Gets or sets the file name used for CAN telemetry.
    /// </summary>
    [JsonPropertyName("can")]
    public string Can { get; set; } = "can.parquet";

    /// <summary>
    /// Gets or sets the file name used for IO telemetry (section masks, etc.).
    /// </summary>
    [JsonPropertyName("io")]
    public string Io { get; set; } = "io.parquet";

    /// <summary>
    /// Gets or sets the file name used for plugin-published telemetry payloads.
    /// </summary>
    [JsonPropertyName("plugin")]
    public string Plugin { get; set; } = "plugin.parquet";

    /// <summary>
    /// Creates a deep copy of this instance.
    /// </summary>
    public TelemetryLogFileNames Clone() => new()
    {
        Pose = Pose,
        Imu = Imu,
        Can = Can,
        Io = Io,
        Plugin = Plugin
    };

    /// <summary>
    /// Validates that all file names are populated.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when a file name is blank.</exception>
    public void Validate()
    {
        ValidateFileName(Pose, nameof(Pose));
        ValidateFileName(Imu, nameof(Imu));
        ValidateFileName(Can, nameof(Can));
        ValidateFileName(Io, nameof(Io));
        ValidateFileName(Plugin, nameof(Plugin));
    }

    private static void ValidateFileName(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{parameterName} must be provided.", parameterName);
        }
    }
}
