using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
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
    private double _exportProgress;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReplayTimelineViewModel"/> class.
    /// </summary>
    public ReplayTimelineViewModel(TimeProvider? timeProvider = null)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
        ExportCsvCommand = new DelegateCommand(parameter => UpdateExportStatus("CSV", parameter));
        ExportGeoJsonCommand = new DelegateCommand(parameter => UpdateExportStatus("GeoJSON", parameter));
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

    /// <summary>Gets the normalized progress of the active export if any.</summary>
    public double ExportProgress => _exportProgress;

    /// <summary>Gets a value indicating whether an export is currently in flight.</summary>
    public bool IsExportInProgress => _exportProgress is > 0d and < 1d;

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

    private void UpdateExportStatus(string format, object? progressArgument)
    {
        if (TryConvertProgress(progressArgument, out var progress))
        {
            _exportProgress = Math.Clamp(progress, 0d, 1d);
        }

        ExportStatus = $"Export queued: {format} snapshot at {_timeProvider.GetLocalNow():HH:mm:ss}";
        OnPropertyChanged(nameof(ExportProgress));
        OnPropertyChanged(nameof(IsExportInProgress));
    }

    private static bool TryConvertProgress(object? progressArgument, out double progress)
    {
        switch (progressArgument)
        {
            case null:
                progress = 0d;
                return false;
            case double direct:
                progress = direct;
                return true;
            case float floatValue:
                progress = floatValue;
                return true;
            case int intValue:
                progress = intValue;
                return true;
            case IConvertible convertible:
                try
                {
                    progress = Convert.ToDouble(convertible, CultureInfo.InvariantCulture);
                    return true;
                }
                catch (FormatException)
                {
                    break;
                }
                catch (InvalidCastException)
                {
                    break;
                }
        }

        progress = 0d;
        return false;
    }
}
