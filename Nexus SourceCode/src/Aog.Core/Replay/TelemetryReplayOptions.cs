using System;
using System.IO;

namespace Aog.Core.Replay;

/// <summary>
/// Options used to configure telemetry replay sessions.
/// </summary>
public sealed class TelemetryReplayOptions
{
    /// <summary>
    /// Gets or sets the directory containing the recorded telemetry parquet files.
    /// </summary>
    public required string InputDirectory { get; init; }

    /// <summary>
    /// Gets or sets the file name for pose telemetry.
    /// </summary>
    public string PoseFileName { get; init; } = "pose.parquet";

    /// <summary>
    /// Gets or sets the file name for IMU telemetry.
    /// </summary>
    public string ImuFileName { get; init; } = "imu.parquet";

    /// <summary>
    /// Gets or sets the file name for CAN telemetry.
    /// </summary>
    public string CanFileName { get; init; } = "can.parquet";

    /// <summary>
    /// Gets or sets the file name for section IO telemetry.
    /// </summary>
    public string IoFileName { get; init; } = "io.parquet";

    /// <summary>
    /// Gets or sets the file name for plugin telemetry.
    /// </summary>
    public string PluginFileName { get; init; } = "plugin.parquet";

    /// <summary>
    /// Resolves an absolute path for the specified telemetry file.
    /// </summary>
    /// <param name="fileName">The file name to resolve.</param>
    /// <returns>An absolute path rooted in <see cref="InputDirectory"/>.</returns>
    public string ResolvePath(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("A non-empty file name must be provided.", nameof(fileName));
        }

        return Path.Combine(InputDirectory ?? string.Empty, fileName);
    }

    /// <summary>
    /// Validates that the options are ready for use.
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(InputDirectory))
        {
            throw new InvalidOperationException("Telemetry replay requires a directory containing parquet files.");
        }
    }
}
