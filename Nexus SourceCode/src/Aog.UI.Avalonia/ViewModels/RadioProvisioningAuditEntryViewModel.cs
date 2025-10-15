using System;

namespace Aog.UI.Avalonia.ViewModels;

/// <summary>
/// Represents the severity of a provisioning audit entry.
/// </summary>
public enum RadioProvisioningAuditSeverity
{
    /// <summary>Informational entry with no required action.</summary>
    Info,

    /// <summary>Warning that highlights an operator follow-up.</summary>
    Warning,
}

/// <summary>
/// Represents a journal entry produced while provisioning radio bridges.
/// </summary>
public sealed class RadioProvisioningAuditEntryViewModel
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RadioProvisioningAuditEntryViewModel"/> class.
    /// </summary>
    public RadioProvisioningAuditEntryViewModel(
        string summary,
        string detail,
        RadioProvisioningAuditSeverity severity,
        DateTimeOffset timestamp)
    {
        Summary = summary ?? throw new ArgumentNullException(nameof(summary));
        Detail = detail ?? throw new ArgumentNullException(nameof(detail));
        Severity = severity;
        Timestamp = timestamp;
    }

    /// <summary>Gets the short summary for the entry.</summary>
    public string Summary { get; }

    /// <summary>Gets additional detail for the entry.</summary>
    public string Detail { get; }

    /// <summary>Gets the severity classification.</summary>
    public RadioProvisioningAuditSeverity Severity { get; }

    /// <summary>Gets the timestamp for the entry.</summary>
    public DateTimeOffset Timestamp { get; }

    /// <summary>Gets the severity display string.</summary>
    public string SeverityDisplay => Severity switch
    {
        RadioProvisioningAuditSeverity.Info => "Info",
        RadioProvisioningAuditSeverity.Warning => "Warning",
        _ => Severity.ToString(),
    };

    /// <summary>Gets a formatted timestamp display.</summary>
    public string TimestampDisplay => Timestamp.ToLocalTime().ToString("HH:mm:ss");

    /// <summary>Gets a value indicating whether the entry represents a warning.</summary>
    public bool IsWarning => Severity == RadioProvisioningAuditSeverity.Warning;
}
