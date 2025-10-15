using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;

namespace Aog.UI.Avalonia.ViewModels;

/// <summary>
/// Presents severity analytics for field health risk layers.
/// </summary>
public sealed class FieldHealthPanelViewModel : ObservableObject
{
    private readonly ObservableCollection<FieldHealthSeverityBucketViewModel> _severityBuckets = new();
    private readonly ObservableCollection<FieldHealthHistoryEntryViewModel> _allHistoryEntries = new();
    private readonly ObservableCollection<FieldHealthHistoryEntryViewModel> _filteredHistoryEntries = new();
    private readonly ReadOnlyObservableCollection<FieldHealthSeverityBucketViewModel> _severityView;
    private readonly ReadOnlyObservableCollection<FieldHealthHistoryEntryViewModel> _historyView;
    private string _layerDisplayName = "Field health";
    private string _scopeDisplay = "Select a field to review health observations.";
    private string _totalAreaDisplay = "—";
    private string _lastSurveyedDisplay = "—";
    private string _observationsSummary = "No observations reported.";
    private string? _notes;
    private string? _alert;
    private bool _showActive = true;
    private bool _showMonitor = true;
    private bool _showResolved;

    /// <summary>
    /// Initializes a new instance of the <see cref="FieldHealthPanelViewModel"/> class.
    /// </summary>
    public FieldHealthPanelViewModel()
    {
        _severityView = new ReadOnlyObservableCollection<FieldHealthSeverityBucketViewModel>(_severityBuckets);
        _historyView = new ReadOnlyObservableCollection<FieldHealthHistoryEntryViewModel>(_filteredHistoryEntries);
    }

    /// <summary>Gets the layer display name surfaced in the panel header.</summary>
    public string LayerDisplayName
    {
        get => _layerDisplayName;
        private set => SetProperty(ref _layerDisplayName, value);
    }

    /// <summary>Gets the scope summary describing the monitored field/season.</summary>
    public string ScopeDisplay
    {
        get => _scopeDisplay;
        private set => SetProperty(ref _scopeDisplay, value);
    }

    /// <summary>Gets the total area covered by field health observations.</summary>
    public string TotalAreaDisplay
    {
        get => _totalAreaDisplay;
        private set => SetProperty(ref _totalAreaDisplay, value);
    }

    /// <summary>Gets the timestamp of the latest survey.</summary>
    public string LastSurveyedDisplay
    {
        get => _lastSurveyedDisplay;
        private set => SetProperty(ref _lastSurveyedDisplay, value);
    }

    /// <summary>Gets a summary of observation counts by status.</summary>
    public string ObservationsSummary
    {
        get => _observationsSummary;
        private set => SetProperty(ref _observationsSummary, value);
    }

    /// <summary>Gets optional notes describing health context.</summary>
    public string? Notes
    {
        get => _notes;
        private set
        {
            if (SetProperty(ref _notes, value))
            {
                OnPropertyChanged(nameof(HasNotes));
            }
        }
    }

    /// <summary>Gets a value indicating whether contextual notes are available.</summary>
    public bool HasNotes => !string.IsNullOrWhiteSpace(Notes);

    /// <summary>Gets optional alert text shown when critical severity exists.</summary>
    public string? Alert
    {
        get => _alert;
        private set
        {
            if (SetProperty(ref _alert, value))
            {
                OnPropertyChanged(nameof(HasAlert));
            }
        }
    }

    /// <summary>Gets a value indicating whether the panel should show an alert.</summary>
    public bool HasAlert => !string.IsNullOrWhiteSpace(Alert);

    /// <summary>Gets the severity buckets rendered in the UI.</summary>
    public ReadOnlyObservableCollection<FieldHealthSeverityBucketViewModel> SeverityBuckets => _severityView;

    /// <summary>Gets the historical observation trail.</summary>
    public ReadOnlyObservableCollection<FieldHealthHistoryEntryViewModel> HistoryEntries => _historyView;

    /// <summary>Gets or sets a value indicating whether active observations are shown.</summary>
    public bool ShowActive
    {
        get => _showActive;
        set
        {
            if (SetProperty(ref _showActive, value))
            {
                RefreshHistoryEntries();
                OnPropertyChanged(nameof(FilterSummary));
            }
        }
    }

    /// <summary>Gets or sets a value indicating whether monitor observations are shown.</summary>
    public bool ShowMonitor
    {
        get => _showMonitor;
        set
        {
            if (SetProperty(ref _showMonitor, value))
            {
                RefreshHistoryEntries();
                OnPropertyChanged(nameof(FilterSummary));
            }
        }
    }

    /// <summary>Gets or sets a value indicating whether resolved observations are shown.</summary>
    public bool ShowResolved
    {
        get => _showResolved;
        set
        {
            if (SetProperty(ref _showResolved, value))
            {
                RefreshHistoryEntries();
                OnPropertyChanged(nameof(FilterSummary));
            }
        }
    }

    /// <summary>Gets a description of the active history filters.</summary>
    public string FilterSummary
    {
        get
        {
            var states = new List<string>();
            if (ShowActive)
            {
                states.Add("Active");
            }

            if (ShowMonitor)
            {
                states.Add("Monitor");
            }

            if (ShowResolved)
            {
                states.Add("Resolved");
            }

            return states.Count == 0
                ? "No statuses selected; history hidden."
                : "Showing " + string.Join(", ", states) + " observations.";
        }
    }

    /// <summary>
    /// Applies metadata sourced from the field health ingest pipeline.
    /// </summary>
    /// <param name="snapshot">Snapshot containing severity counts and history.</param>
    public void ApplySnapshot(FieldHealthPanelSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        LayerDisplayName = string.IsNullOrWhiteSpace(snapshot.LayerDisplayName)
            ? "Field health"
            : snapshot.LayerDisplayName.Trim();
        ScopeDisplay = string.IsNullOrWhiteSpace(snapshot.ScopeDisplay)
            ? "Select a field to review health observations."
            : snapshot.ScopeDisplay.Trim();
        TotalAreaDisplay = snapshot.TotalAreaHectares > 0
            ? string.Format(CultureInfo.InvariantCulture, "Total monitored area {0:0.0} ha", snapshot.TotalAreaHectares)
            : "Total monitored area —";
        LastSurveyedDisplay = snapshot.LastSurveyedAt.HasValue
            ? snapshot.LastSurveyedAt.Value.ToString("yyyy-MM-dd HH:mm 'UTC'", CultureInfo.InvariantCulture)
            : "Last surveyed —";
        ObservationsSummary = string.Format(
            CultureInfo.InvariantCulture,
            "Active {0} • Monitor {1} • Resolved {2}",
            snapshot.ActiveCount,
            snapshot.MonitorCount,
            snapshot.ResolvedCount);
        Notes = string.IsNullOrWhiteSpace(snapshot.Notes) ? null : snapshot.Notes.Trim();
        Alert = string.IsNullOrWhiteSpace(snapshot.Alert) ? null : snapshot.Alert.Trim();

        ReplaceItems(_severityBuckets, snapshot.SeverityBuckets, bucket => new FieldHealthSeverityBucketViewModel(bucket.Severity, bucket.Count, bucket.AreaHectares, bucket.Description));
        ReplaceItems(_allHistoryEntries, snapshot.HistoryEntries, entry => new FieldHealthHistoryEntryViewModel(entry.Timestamp, entry.Severity, entry.Status, entry.Notes));

        ShowActive = snapshot.ShowActive;
        ShowMonitor = snapshot.ShowMonitor;
        ShowResolved = snapshot.ShowResolved;

        RefreshHistoryEntries();
        OnPropertyChanged(nameof(FilterSummary));
    }

    private void RefreshHistoryEntries()
    {
        _filteredHistoryEntries.Clear();
        foreach (var entry in _allHistoryEntries)
        {
            if (ShouldInclude(entry))
            {
                _filteredHistoryEntries.Add(entry);
            }
        }
    }

    private bool ShouldInclude(FieldHealthHistoryEntryViewModel entry)
    {
        return entry.Status switch
        {
            var status when string.Equals(status, "Active", StringComparison.OrdinalIgnoreCase) => ShowActive,
            var status when string.Equals(status, "Monitor", StringComparison.OrdinalIgnoreCase) => ShowMonitor,
            var status when string.Equals(status, "Resolved", StringComparison.OrdinalIgnoreCase) => ShowResolved,
            _ => true,
        };
    }

    private static void ReplaceItems<TModel, TSnapshot>(ObservableCollection<TModel> collection, IEnumerable<TSnapshot> snapshots, Func<TSnapshot, TModel> factory)
    {
        ArgumentNullException.ThrowIfNull(collection);
        ArgumentNullException.ThrowIfNull(snapshots);
        ArgumentNullException.ThrowIfNull(factory);

        collection.Clear();
        foreach (var snapshot in snapshots)
        {
            collection.Add(factory(snapshot));
        }
    }
}

/// <summary>Represents a snapshot of field health panel metadata.</summary>
/// <param name="LayerDisplayName">Layer label displayed in the UI.</param>
/// <param name="ScopeDisplay">Scope label describing the monitored field/season.</param>
/// <param name="TotalAreaHectares">Total monitored area in hectares.</param>
/// <param name="LastSurveyedAt">Timestamp when the field was last surveyed.</param>
/// <param name="Notes">Optional notes describing risk context.</param>
/// <param name="Alert">Optional alert text for critical severities.</param>
/// <param name="ActiveCount">Number of active observations.</param>
/// <param name="MonitorCount">Number of monitor observations.</param>
/// <param name="ResolvedCount">Number of resolved observations.</param>
/// <param name="ShowActive">Whether active history should be shown.</param>
/// <param name="ShowMonitor">Whether monitor history should be shown.</param>
/// <param name="ShowResolved">Whether resolved history should be shown.</param>
/// <param name="SeverityBuckets">Severity counts grouped by severity.</param>
/// <param name="HistoryEntries">Historical observation timeline.</param>
public sealed record FieldHealthPanelSnapshot(
    string LayerDisplayName,
    string ScopeDisplay,
    double TotalAreaHectares,
    DateTimeOffset? LastSurveyedAt,
    string? Notes,
    string? Alert,
    int ActiveCount,
    int MonitorCount,
    int ResolvedCount,
    bool ShowActive,
    bool ShowMonitor,
    bool ShowResolved,
    IReadOnlyList<FieldHealthSeverityBucketSnapshot> SeverityBuckets,
    IReadOnlyList<FieldHealthHistoryEntrySnapshot> HistoryEntries);

/// <summary>Represents a severity bucket snapshot.</summary>
/// <param name="Severity">Severity label (e.g., High, Moderate).</param>
/// <param name="Count">Number of observations in the bucket.</param>
/// <param name="AreaHectares">Area covered by observations in the bucket.</param>
/// <param name="Description">Optional description.</param>
public sealed record FieldHealthSeverityBucketSnapshot(string Severity, int Count, double AreaHectares, string Description);

/// <summary>Represents a history entry snapshot.</summary>
/// <param name="Timestamp">Timestamp when the observation was updated.</param>
/// <param name="Severity">Severity at the time of the entry.</param>
/// <param name="Status">Observation status (Active, Monitor, Resolved).</param>
/// <param name="Notes">Optional notes recorded with the entry.</param>
public sealed record FieldHealthHistoryEntrySnapshot(DateTimeOffset Timestamp, string Severity, string Status, string Notes);
