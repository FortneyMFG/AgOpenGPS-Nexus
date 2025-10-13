using System;

namespace Aog.UI.Avalonia.ViewModels;

/// <summary>
/// Represents a single tuning event recorded by the AutoSteer dashboard.
/// </summary>
public sealed class SteerTuningEventViewModel
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SteerTuningEventViewModel"/> class.
    /// </summary>
    /// <param name="timestamp">When the event occurred.</param>
    /// <param name="description">Human-readable description of the change.</param>
    public SteerTuningEventViewModel(DateTimeOffset timestamp, string description)
    {
        Timestamp = timestamp;
        Description = description ?? throw new ArgumentNullException(nameof(description));
    }

    /// <summary>Gets the event timestamp.</summary>
    public DateTimeOffset Timestamp { get; }

    /// <summary>Gets the event description.</summary>
    public string Description { get; }

    /// <summary>Gets a formatted timestamp suitable for UI display.</summary>
    public string DisplayTimestamp => Timestamp.ToLocalTime().ToString("HH:mm:ss");
}
