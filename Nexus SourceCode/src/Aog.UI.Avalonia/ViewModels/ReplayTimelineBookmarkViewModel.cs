using System;

namespace Aog.UI.Avalonia.ViewModels;

/// <summary>
/// Represents a replay bookmark displayed alongside the analysis timeline.
/// </summary>
public sealed class ReplayTimelineBookmarkViewModel
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReplayTimelineBookmarkViewModel"/> class.
    /// </summary>
    /// <param name="timestamp">Time within the replay.</param>
    /// <param name="label">Short display label.</param>
    /// <param name="notes">Optional extended description.</param>
    public ReplayTimelineBookmarkViewModel(TimeSpan timestamp, string label, string notes)
    {
        Label = label ?? throw new ArgumentNullException(nameof(label));
        Notes = notes ?? throw new ArgumentNullException(nameof(notes));
        Timestamp = timestamp;
    }

    /// <summary>Gets the bookmark timestamp.</summary>
    public TimeSpan Timestamp { get; }

    /// <summary>Gets the short label.</summary>
    public string Label { get; }

    /// <summary>Gets the longer description for the bookmark.</summary>
    public string Notes { get; }

    /// <summary>Gets a friendly timestamp for display.</summary>
    public string TimestampDisplay => Timestamp.ToString("mm\:ss");
}
