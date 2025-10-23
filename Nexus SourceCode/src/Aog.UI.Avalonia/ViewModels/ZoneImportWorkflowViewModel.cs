using System;

namespace Aog.UI.Avalonia.ViewModels;

/// <summary>
/// Represents a zone import or export workflow surfaced in the UI shell.
/// </summary>
public sealed class ZoneImportWorkflowViewModel : ObservableObject
{
    private readonly Func<DateTimeOffset> _clock;
    private readonly Action<ZoneTransferEvent> _logCallback;
    private readonly Action<string> _statusCallback;
    private readonly int _sampleFeatureCount;
    private readonly string _resultSummaryTemplate;

    private bool _isBusy;
    private double _exportProgress;
    private string _statusDisplay;
    private DateTimeOffset? _lastRun;

    /// <summary>
    /// Initializes a new instance of the <see cref="ZoneImportWorkflowViewModel"/> class.
    /// </summary>
    public ZoneImportWorkflowViewModel(
        string workflowName,
        string actionLabel,
        string description,
        string transportSummary,
        string policySummary,
        bool isExport,
        int sampleFeatureCount,
        string resultSummaryTemplate,
        Func<DateTimeOffset> clock,
        Action<ZoneTransferEvent> logCallback,
        Action<string> statusCallback)
    {
        WorkflowName = workflowName ?? throw new ArgumentNullException(nameof(workflowName));
        ActionLabel = actionLabel ?? throw new ArgumentNullException(nameof(actionLabel));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        TransportSummary = transportSummary ?? throw new ArgumentNullException(nameof(transportSummary));
        PolicySummary = policySummary ?? throw new ArgumentNullException(nameof(policySummary));
        IsExport = isExport;
        _sampleFeatureCount = sampleFeatureCount;
        _resultSummaryTemplate = resultSummaryTemplate ?? "{0}";
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _logCallback = logCallback ?? throw new ArgumentNullException(nameof(logCallback));
        _statusCallback = statusCallback ?? throw new ArgumentNullException(nameof(statusCallback));

        _statusDisplay = isExport
            ? "Ready to generate export package."
            : "Ready to stage import into the zone registry.";

        ExecuteCommand = new DelegateCommand(_ => RunWorkflow(), _ => !IsBusy);
    }

    /// <summary>Gets the workflow name.</summary>
    public string WorkflowName { get; }

    /// <summary>Gets the button label used to launch the workflow.</summary>
    public string ActionLabel { get; }

    /// <summary>Gets the description of the workflow.</summary>
    public string Description { get; }

    /// <summary>Gets the transport summary describing input/output locations.</summary>
    public string TransportSummary { get; }

    /// <summary>Gets the policy summary describing validation or audit requirements.</summary>
    public string PolicySummary { get; }

    /// <summary>Gets a value indicating whether the workflow is an export.</summary>
    public bool IsExport { get; }

    /// <summary>Gets a display string describing whether the workflow is an import or export.</summary>
    public string WorkflowTypeDisplay => IsExport ? "Export" : "Import";

    /// <summary>Gets a textual status summary for the workflow.</summary>
    public string StatusDisplay
    {
        get => _statusDisplay;
        private set => SetProperty(ref _statusDisplay, value);
    }

    /// <summary>Gets the most recent completion timestamp display.</summary>
    public string LastRunDisplay => _lastRun?.ToLocalTime().ToString("HH:mm:ss") ?? "—";

    /// <summary>Gets or sets the progress value between 0 and 1.</summary>
    public double ExportProgress
    {
        get => _exportProgress;
        private set
        {
            if (SetProperty(ref _exportProgress, value))
            {
                OnPropertyChanged(nameof(ExportProgressPercent));
            }
        }
    }

    /// <summary>Gets the export progress as a percentage.</summary>
    public double ExportProgressPercent => ExportProgress * 100d;

    /// <summary>Gets a value indicating whether the workflow is running.</summary>
    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                ExecuteCommand.RaiseCanExecuteChanged();
            }
        }
    }

    /// <summary>Gets the command that executes the workflow.</summary>
    public DelegateCommand ExecuteCommand { get; }

    private void RunWorkflow()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        ExportProgress = 0.25;
        StatusDisplay = "Validating CRS and schema...";

        var now = _clock();
        var action = IsExport ? "Export completed" : "Import completed";
        var detail = string.Format(_resultSummaryTemplate, _sampleFeatureCount, now.ToLocalTime().ToString("HH:mm"));

        ExportProgress = 1.0;
        _lastRun = now;
        StatusDisplay = detail;
        OnPropertyChanged(nameof(LastRunDisplay));

        _logCallback(new ZoneTransferEvent(WorkflowName, action, detail, now));
        _statusCallback($"{WorkflowName}: {detail}");

        IsBusy = false;
    }
}
