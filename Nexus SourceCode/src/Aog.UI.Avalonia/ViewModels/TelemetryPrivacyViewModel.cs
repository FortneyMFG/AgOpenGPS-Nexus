using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Aog.UI.Avalonia.Telemetry;

namespace Aog.UI.Avalonia.ViewModels;

/// <summary>
/// Provides opt-in controls for crash telemetry and summarizes pending reports.
/// </summary>
public sealed class TelemetryPrivacyViewModel : INotifyPropertyChanged
{
    private readonly ICrashTelemetryService _service;
    private readonly ICommand _uploadReportsCommand;
    private readonly ICommand _clearReportsCommand;

    private bool _isTelemetryOptedIn;
    private int _pendingReportCount;
    private CrashReportSummary? _lastReport;
    private string? _statusMessage;
    private bool _hasError;

    /// <summary>
    /// Initializes a new instance of the <see cref="TelemetryPrivacyViewModel"/> class.
    /// </summary>
    /// <param name="service">Crash telemetry service backing the view-model.</param>
    public TelemetryPrivacyViewModel(ICrashTelemetryService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _uploadReportsCommand = new DelegateCommand(UploadPendingReports);
        _clearReportsCommand = new DelegateCommand(ClearPendingReports);
        RefreshState();
    }

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Gets or sets a value indicating whether telemetry uploads are enabled.</summary>
    public bool IsTelemetryOptedIn
    {
        get => _isTelemetryOptedIn;
        set
        {
            if (value == _isTelemetryOptedIn)
            {
                return;
            }

            _isTelemetryOptedIn = value;
            _service.SetTelemetryOptIn(value);
            OnPropertyChanged();
            RefreshState();
        }
    }

    /// <summary>Gets the number of crash reports waiting to be uploaded.</summary>
    public int PendingReportCount
    {
        get => _pendingReportCount;
        private set
        {
            if (value == _pendingReportCount)
            {
                return;
            }

            _pendingReportCount = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasPendingReports));
            OnPropertyChanged(nameof(PendingReportSummary));
        }
    }

    /// <summary>Gets a human readable summary of the pending crash reports.</summary>
    public string PendingReportSummary
    {
        get
        {
            if (PendingReportCount == 0)
            {
                return "No crash reports are waiting to be uploaded.";
            }

            var last = _lastReport;
            if (last is null)
            {
                return $"{PendingReportCount} crash report(s) waiting for upload.";
            }

            return $"{PendingReportCount} crash report(s) waiting. Last: {last.Timestamp:u} — {last.ExceptionType}.";
        }
    }

    /// <summary>Gets a value indicating whether any crash reports are pending.</summary>
    public bool HasPendingReports => PendingReportCount > 0;

    /// <summary>Gets a status message describing the last action result.</summary>
    public string? StatusMessage
    {
        get => _statusMessage;
        private set
        {
            if (value == _statusMessage)
            {
                return;
            }

            _statusMessage = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasStatusMessage));
        }
    }

    /// <summary>Gets a value indicating whether the last status message represents an error.</summary>
    public bool HasError
    {
        get => _hasError;
        private set
        {
            if (value == _hasError)
            {
                return;
            }

            _hasError = value;
            OnPropertyChanged();
        }
    }

    /// <summary>Gets a value indicating whether a status message is available.</summary>
    public bool HasStatusMessage => !string.IsNullOrWhiteSpace(StatusMessage);

    /// <summary>Gets the command that uploads pending crash reports.</summary>
    public ICommand UploadReportsCommand => _uploadReportsCommand;

    /// <summary>Gets the command that deletes pending crash reports without uploading them.</summary>
    public ICommand ClearReportsCommand => _clearReportsCommand;

    private void UploadPendingReports()
    {
        try
        {
            if (!IsTelemetryOptedIn)
            {
                HasError = true;
                StatusMessage = "Enable telemetry to upload crash reports.";
                return;
            }

            var uploaded = _service.UploadPendingReports();
            HasError = false;
            StatusMessage = uploaded.Count == 0
                ? "No crash reports were pending upload."
                : $"Uploaded {uploaded.Count} crash report(s).";
        }
        catch (Exception ex)
        {
            HasError = true;
            StatusMessage = $"Failed to upload crash reports: {ex.Message}";
        }
        finally
        {
            RefreshState();
        }
    }

    private void ClearPendingReports()
    {
        try
        {
            _service.ClearPendingReports();
            HasError = false;
            StatusMessage = "Pending crash reports cleared.";
        }
        catch (Exception ex)
        {
            HasError = true;
            StatusMessage = $"Failed to clear crash reports: {ex.Message}";
        }
        finally
        {
            RefreshState();
        }
    }

    private void RefreshState()
    {
        var state = _service.GetState();
        _isTelemetryOptedIn = state.IsTelemetryOptedIn;
        OnPropertyChanged(nameof(IsTelemetryOptedIn));

        PendingReportCount = state.PendingReports.Count;
        _lastReport = state.PendingReports.Count == 0
            ? null
            : state.PendingReports[^1];
        OnPropertyChanged(nameof(PendingReportSummary));
        OnPropertyChanged(nameof(HasPendingReports));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private sealed class DelegateCommand : ICommand
    {
        private readonly Action _execute;

        public DelegateCommand(Action execute)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        }

        public event EventHandler? CanExecuteChanged
        {
            add { }
            remove { }
        }

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter) => _execute();
    }
}
