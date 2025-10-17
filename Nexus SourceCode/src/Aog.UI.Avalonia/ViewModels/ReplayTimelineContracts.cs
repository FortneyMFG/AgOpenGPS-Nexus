using System;
using System.Collections.Generic;
using System.Linq;

namespace Aog.UI.Avalonia.ViewModels;

/// <summary>
/// Describes a timeline surface exposing replay telemetry samples and bookmark metadata.
/// </summary>
public interface IReplayTimeline
{
    /// <summary>Raised when the timeline publishes new speed and heading samples.</summary>
    event EventHandler<ReplayTimelineSamplesChangedEventArgs>? SamplesChanged;

    /// <summary>Raised when the timeline publishes an updated set of bookmarks.</summary>
    event EventHandler<ReplayTimelineBookmarksChangedEventArgs>? BookmarksChanged;

    /// <summary>Gets the currently buffered speed samples.</summary>
    IReadOnlyList<double> SpeedSamples { get; }

    /// <summary>Gets the currently buffered heading samples.</summary>
    IReadOnlyList<double> HeadingSamples { get; }

    /// <summary>Gets the bookmarks surfaced alongside the replay timeline.</summary>
    IReadOnlyList<ReplayTimelineBookmark> Bookmarks { get; }
}

/// <summary>
/// Describes a component capable of exporting replay timeline artefacts.
/// </summary>
public interface IReplayTimelineExporter
{
    /// <summary>Raised when the exporter surfaces a new status message.</summary>
    event EventHandler<ReplayExportStatusChangedEventArgs>? ExportStatusChanged;
}

/// <summary>
/// Event arguments raised when the timeline publishes new sample sets.
/// </summary>
public sealed class ReplayTimelineSamplesChangedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReplayTimelineSamplesChangedEventArgs"/> class.
    /// </summary>
    /// <param name="speedSamples">Speed samples normalised to render in the chart.</param>
    /// <param name="headingSamples">Heading delta samples used to highlight turns.</param>
    public ReplayTimelineSamplesChangedEventArgs(
        IEnumerable<double> speedSamples,
        IEnumerable<double> headingSamples)
    {
        SpeedSamples = (speedSamples ?? throw new ArgumentNullException(nameof(speedSamples))).ToArray();
        HeadingSamples = (headingSamples ?? throw new ArgumentNullException(nameof(headingSamples))).ToArray();
    }

    /// <summary>Gets the speed samples.</summary>
    public IReadOnlyList<double> SpeedSamples { get; }

    /// <summary>Gets the heading samples.</summary>
    public IReadOnlyList<double> HeadingSamples { get; }
}

/// <summary>
/// Event arguments raised when the timeline publishes new bookmark metadata.
/// </summary>
public sealed class ReplayTimelineBookmarksChangedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReplayTimelineBookmarksChangedEventArgs"/> class.
    /// </summary>
    /// <param name="bookmarks">Bookmarks associated with the timeline.</param>
    public ReplayTimelineBookmarksChangedEventArgs(IEnumerable<ReplayTimelineBookmark> bookmarks)
    {
        Bookmarks = (bookmarks ?? throw new ArgumentNullException(nameof(bookmarks))).ToArray();
    }

    /// <summary>Gets the bookmarks published by the timeline.</summary>
    public IReadOnlyList<ReplayTimelineBookmark> Bookmarks { get; }
}

/// <summary>
/// Event arguments raised when the exporter surfaces a new status message.
/// </summary>
public sealed class ReplayExportStatusChangedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReplayExportStatusChangedEventArgs"/> class.
    /// </summary>
    /// <param name="status">Human readable status text.</param>
    public ReplayExportStatusChangedEventArgs(string status)
    {
        Status = status ?? throw new ArgumentNullException(nameof(status));
    }

    /// <summary>Gets the export status text.</summary>
    public string Status { get; }
}

/// <summary>
/// Represents a bookmark in the replay timeline with timestamped notes.
/// </summary>
/// <param name="Timestamp">Position of the bookmark within the replay.</param>
/// <param name="Label">Short label shown in the UI.</param>
/// <param name="Notes">Optional notes describing the bookmark.</param>
public sealed record ReplayTimelineBookmark(TimeSpan Timestamp, string Label, string Notes);
