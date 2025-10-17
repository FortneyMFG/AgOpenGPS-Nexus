using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Aog.UI.Avalonia.ViewModels;

/// <summary>
/// Provides derived analytics for replay sessions including bookmarks and export hooks.
/// </summary>
public sealed class ReplayTimelineViewModel : ObservableObject
{
    private readonly TimeProvider _timeProvider;
    private readonly ObservableCollection<ReplayTimelineBookmarkViewModel> _bookmarks = new();
    private readonly ReadOnlyObservableCollection<ReplayTimelineBookmarkViewModel> _readonlyBookmarks;

    private IReadOnlyList<double> _speedSamples = Array.Empty<double>();
    private IReadOnlyList<double> _headingSamples = Array.Empty<double>();
    private string _exportStatus = "Exports generate CSV/GeoJSON in upcoming milestones.";

    /// <summary>
    /// Initializes a new instance of the <see cref="ReplayTimelineViewModel"/> class.
    /// </summary>
    public ReplayTimelineViewModel(TimeProvider? timeProvider = null)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
        ExportCsvCommand = new DelegateCommand(_ => UpdateExportStatus("CSV"));
        ExportGeoJsonCommand = new DelegateCommand(_ => UpdateExportStatus("GeoJSON"));
        _readonlyBookmarks = new ReadOnlyObservableCollection<ReplayTimelineBookmarkViewModel>(_bookmarks);
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

    private void UpdateExportStatus(string format)
    {
        ExportStatus = $"Export queued: {format} snapshot at {_timeProvider.GetLocalNow():HH:mm:ss}";
    }
}
