using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Aog.UI.Avalonia.ViewModels;

/// <summary>
/// Coordinates zone import and export workflows surfaced in the shell.
/// </summary>
public sealed class ZoneImportExportPanelViewModel : ObservableObject
{
    private readonly ObservableCollection<ZoneImportWorkflowViewModel> _workflows;
    private readonly ObservableCollection<ZoneTransferEventViewModel> _activityLog;
    private readonly Func<DateTimeOffset> _clock;
    private string _statusMessage;

    /// <summary>
    /// Initializes a new instance of the <see cref="ZoneImportExportPanelViewModel"/> class.
    /// </summary>
    public ZoneImportExportPanelViewModel(Func<DateTimeOffset>? clock = null)
    {
        _clock = clock ?? (() => DateTimeOffset.UtcNow);
        _statusMessage = "Import shapefiles, GeoPackage layers, or export ISOXML bundles to keep the zone registry in sync.";

        _activityLog = new ObservableCollection<ZoneTransferEventViewModel>();
        _activityLog.CollectionChanged += (_, __) =>
        {
            OnPropertyChanged(nameof(ActivityLog));
            OnPropertyChanged(nameof(HasActivity));
            OnPropertyChanged(nameof(HasNoActivity));
        };

        _workflows = new ObservableCollection<ZoneImportWorkflowViewModel>();
        SeedWorkflows();
    }

    /// <summary>Gets the available zone transfer workflows.</summary>
    public IReadOnlyList<ZoneImportWorkflowViewModel> Workflows => _workflows;

    /// <summary>Gets the activity log entries for completed transfers.</summary>
    public IReadOnlyList<ZoneTransferEventViewModel> ActivityLog => _activityLog;

    /// <summary>Gets the banner status message describing the latest activity.</summary>
    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    /// <summary>Gets a value indicating whether activity has been recorded.</summary>
    public bool HasActivity => _activityLog.Count > 0;

    /// <summary>Gets a value indicating whether no activity entries exist.</summary>
    public bool HasNoActivity => !HasActivity;

    private void SeedWorkflows()
    {
        _workflows.Add(new ZoneImportWorkflowViewModel(
            workflowName: "Import Shapefile",
            actionLabel: "Import .shp",
            description: "Normalizes Shapefile polygons, applies CRS policy, and appends zones to the registry.",
            transportSummary: "Input: Boundary.shp + .prj → Output: zone registry",
            policySummary: "Validates geometry buffers and logs CRS reprojection per ADR-022.",
            isExport: false,
            sampleFeatureCount: 12,
            resultSummaryTemplate: "Imported {0} zones at {1} (EPSG:26915).",
            clock: _clock,
            logCallback: LogTransfer,
            statusCallback: UpdateStatus));

        _workflows.Add(new ZoneImportWorkflowViewModel(
            workflowName: "Import GeoPackage",
            actionLabel: "Import .gpkg",
            description: "Ingests GeoPackage layers with buffer metadata and provenance hashes.",
            transportSummary: "Input: zones.gpkg → Output: zone registry",
            policySummary: "Captures per-layer provenance and buffer defaults per ADR-027.",
            isExport: false,
            sampleFeatureCount: 8,
            resultSummaryTemplate: "Imported {0} buffered features at {1} with provenance recorded.",
            clock: _clock,
            logCallback: LogTransfer,
            statusCallback: UpdateStatus));

        _workflows.Add(new ZoneImportWorkflowViewModel(
            workflowName: "Export ISOXML",
            actionLabel: "Export ISOXML",
            description: "Generates TaskData with zone polygons and audit manifest for partner controllers.",
            transportSummary: "Output: TaskData/ZONE/ZoneData.xml",
            policySummary: "Ensures schema hashes and buffers are stamped into the export manifest.",
            isExport: true,
            sampleFeatureCount: 3,
            resultSummaryTemplate: "Exported {0} zones at {1}; bundle hashed for provenance.",
            clock: _clock,
            logCallback: LogTransfer,
            statusCallback: UpdateStatus));
    }

    private void LogTransfer(ZoneTransferEvent transfer)
    {
        _activityLog.Insert(0, new ZoneTransferEventViewModel(
            transfer.WorkflowName,
            transfer.ActionDisplay,
            transfer.Detail,
            transfer.Timestamp));

        UpdateStatus(transfer.Detail);
    }

    private void UpdateStatus(string message)
    {
        StatusMessage = message;
    }
}
