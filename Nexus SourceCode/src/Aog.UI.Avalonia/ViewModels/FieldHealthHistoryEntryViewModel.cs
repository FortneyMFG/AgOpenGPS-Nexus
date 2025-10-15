using System;
using System.Globalization;

namespace Aog.UI.Avalonia.ViewModels;

/// <summary>
/// Represents a single entry within the field health history timeline.
/// </summary>
public sealed class FieldHealthHistoryEntryViewModel
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FieldHealthHistoryEntryViewModel"/> class.
    /// </summary>
    /// <param name="timestamp">Timestamp when the observation changed.</param>
    /// <param name="severity">Severity recorded with the entry.</param>
    /// <param name="status">Observation status.</param>
    /// <param name="notes">Optional notes captured alongside the entry.</param>
    public FieldHealthHistoryEntryViewModel(DateTimeOffset timestamp, string severity, string status, string notes)
    {
        Timestamp = timestamp;
        Severity = string.IsNullOrWhiteSpace(severity) ? "Unknown" : severity.Trim();
        Status = string.IsNullOrWhiteSpace(status) ? "Unknown" : status.Trim();
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
    }

    /// <summary>Gets the timestamp when the entry was recorded.</summary>
    public DateTimeOffset Timestamp { get; }

    /// <summary>Gets the severity associated with the entry.</summary>
    public string Severity { get; }

    /// <summary>Gets the observation status.</summary>
    public string Status { get; }

    /// <summary>Gets optional notes captured with the entry.</summary>
    public string? Notes { get; }

    /// <summary>Gets the formatted timestamp string.</summary>
    public string TimestampDisplay => Timestamp.ToString("yyyy-MM-dd HH:mm 'UTC'", CultureInfo.InvariantCulture);

    /// <summary>Gets a value indicating whether notes are present.</summary>
    public bool HasNotes => !string.IsNullOrWhiteSpace(Notes);
}
