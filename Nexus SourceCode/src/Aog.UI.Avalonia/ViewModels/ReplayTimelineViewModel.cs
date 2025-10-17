using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Aog.UI.Avalonia.ViewModels;

/// <summary>
/// Provides derived analytics for replay sessions including bookmarks and export hooks.
/// </summary>
public sealed class ReplayTimelineViewModel : ObservableObject, IDisposable
{
    private readonly TimeProvider _timeProvider;
    private readonly ObservableCollection<ReplayTimelineBookmarkViewModel> _bookmarks = new();
    private readonly ReadOnlyObservableCollection<ReplayTimelineBookmarkViewModel> _readonlyBookmarks;
    private readonly IReplayTimeline? _timeline;
    private readonly IReplayTimelineExporter? _exporter;
    private readonly EventHandler<ReplayTimelineSamplesChangedEventArgs>? _samplesChangedHandler;
    private readonly EventHandler<ReplayTimelineBookmarksChangedEventArgs>? _bookmarksChangedHandler;
    private readonly EventHandler<ReplayExportStatusChangedEventArgs>? _exportStatusChangedHandler;

    private IReadOnlyList<double> _speedSamples = Array.Empty<double>();
    private IReadOnlyList<double> _headingSamples = Array.Empty<double>();
    private string _exportStatus = "Exports generate CSV/GeoJSON in upcoming milestones.";
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReplayTimelineViewModel"/> class.
    /// </summary>
    public ReplayTimelineViewModel(
        TimeProvider? timeProvider = null,
        IReplayTimeline? timeline = null,
        IReplayTimelineExporter? exporter = null)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
        _timeline = timeline;
        _exporter = exporter;

        ExportCsvCommand = new DelegateCommand(_ => UpdateExportStatus("CSV"));
        ExportGeoJsonCommand = new DelegateCommand(_ => UpdateExportStatus("GeoJSON"));
        _readonlyBookmarks = new ReadOnlyObservableCollection<ReplayTimelineBookmarkViewModel>(_bookmarks);

        if (timeline is not null)
        {
            _samplesChangedHandler = (_, args) => OnSamplesChanged(args);
            _bookmarksChangedHandler = (_, args) => OnBookmarksChanged(args);

            timeline.SamplesChanged += _samplesChangedHandler;
            timeline.BookmarksChanged += _bookmarksChangedHandler;

            UpdateSamples(timeline.SpeedSamples, timeline.HeadingSamples);
            UpdateBookmarks(timeline.Bookmarks);
        }

        if (exporter is not null)
        {
            _exportStatusChangedHandler = (_, args) => OnExportStatusChanged(args);
            exporter.ExportStatusChanged += _exportStatusChangedHandler;
        }
    }

    /// <summary>Gets the normalized vehicle speed samples used to render the timeline chart.</summary>
    public IReadOnlyList<double> SpeedSamples
    {
        get => _speedSamples;
        private set => SetProperty(ref _speedSamples, value);
    }

    /// <summary>Gets the heading change samples used to highlight turns.</summary>
    public IReadOnlyList<double> HeadingSamples
    {
        get => _headingSamples;
        private set => SetProperty(ref _headingSamples, value);
    }

    /// <summary>Gets the bookmarks surfaced alongside the timeline.</summary>
    public ReadOnlyObservableCollection<ReplayTimelineBookmarkViewModel> Bookmarks => _readonlyBookmarks;

    /// <summary>Gets a status message that reflects the latest export command.</summary>
    public string ExportStatus
    {
        get => _exportStatus;
        private set => SetProperty(ref _exportStatus, value);
    }

    /// <summary>Gets the command used to trigger CSV exports.</summary>
    public DelegateCommand ExportCsvCommand { get; }

    /// <summary>Gets the command used to trigger GeoJSON exports.</summary>
    public DelegateCommand ExportGeoJsonCommand { get; }

    /// <summary>Seeds the view-model with sample data.</summary>
    public void ApplySampleData(IEnumerable<double> speeds, IEnumerable<double> headingChanges, IEnumerable<ReplayTimelineBookmarkViewModel> bookmarks)
    {
        ArgumentNullException.ThrowIfNull(speeds);
        ArgumentNullException.ThrowIfNull(headingChanges);
        ArgumentNullException.ThrowIfNull(bookmarks);

        SpeedSamples = speeds.ToArray();
        HeadingSamples = headingChanges.ToArray();

        _bookmarks.Clear();
        foreach (var bookmark in bookmarks)
        {
            _bookmarks.Add(bookmark);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (_timeline is not null)
        {
            if (_samplesChangedHandler is not null)
            {
                _timeline.SamplesChanged -= _samplesChangedHandler;
            }

            if (_bookmarksChangedHandler is not null)
            {
                _timeline.BookmarksChanged -= _bookmarksChangedHandler;
            }
        }

        if (_exporter is not null && _exportStatusChangedHandler is not null)
        {
            _exporter.ExportStatusChanged -= _exportStatusChangedHandler;
        }
    }

    private void UpdateExportStatus(string format)
    {
        if (_disposed)
        {
            return;
        }

        ExportStatus = $"Export queued: {format} snapshot at {_timeProvider.GetLocalNow():HH:mm:ss}";
    }

    private void OnSamplesChanged(ReplayTimelineSamplesChangedEventArgs args)
    {
        if (_disposed)
        {
            return;
        }

        UpdateSamples(args.SpeedSamples, args.HeadingSamples);
    }

    private void OnBookmarksChanged(ReplayTimelineBookmarksChangedEventArgs args)
    {
        if (_disposed)
        {
            return;
        }

        UpdateBookmarks(args.Bookmarks);
    }

    private void OnExportStatusChanged(ReplayExportStatusChangedEventArgs args)
    {
        if (_disposed)
        {
            return;
        }

        ExportStatus = args.Status;
    }

    private void UpdateSamples(IEnumerable<double> speedSamples, IEnumerable<double> headingSamples)
    {
        SpeedSamples = speedSamples?.ToArray() ?? Array.Empty<double>();
        HeadingSamples = headingSamples?.ToArray() ?? Array.Empty<double>();
    }

    private void UpdateBookmarks(IEnumerable<ReplayTimelineBookmark> bookmarks)
    {
        _bookmarks.Clear();

        if (bookmarks is null)
        {
            return;
        }

        foreach (var bookmark in bookmarks)
        {
            _bookmarks.Add(new ReplayTimelineBookmarkViewModel(bookmark.Timestamp, bookmark.Label, bookmark.Notes));
        }
    }
}
