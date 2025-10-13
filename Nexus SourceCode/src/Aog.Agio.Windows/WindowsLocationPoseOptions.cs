using System;

namespace Aog.Agio.Windows;

/// <summary>
/// Configures how the Windows Location fallback provider publishes pose updates.
/// </summary>
public sealed class WindowsLocationPoseOptions
{
    private string _frame = "earth";
    private string _source = "windows.location";

    /// <summary>
    /// Gets or sets the coordinate frame identifier stamped onto emitted poses.
    /// </summary>
    public string Frame
    {
        get => _frame;
        init => _frame = string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Frame identifier cannot be null or whitespace.", nameof(value))
            : value;
    }

    /// <summary>
    /// Gets or sets the source tag stamped onto emitted poses.
    /// </summary>
    public string Source
    {
        get => _source;
        init => _source = string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Source identifier cannot be null or whitespace.", nameof(value))
            : value;
    }

    /// <summary>
    /// Gets or sets the fallback altitude (meters) used when the Windows API omits altitude.
    /// </summary>
    public double DefaultAltitudeM { get; init; }

    /// <summary>
    /// Gets or sets the sequence number assigned to the first published pose.
    /// </summary>
    /// <remarks>
    /// The publisher increments the value for each subsequent message.
    /// </remarks>
    public ulong StartingSequence { get; init; }
}
