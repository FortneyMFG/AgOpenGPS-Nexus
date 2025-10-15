using System;

namespace Aog.UI.Avalonia.ViewModels;

/// <summary>
/// Status of a provisioning step surfaced in the radio provisioning panel.
/// </summary>
public enum RadioProvisioningStepStatus
{
    /// <summary>Step has not yet started.</summary>
    Pending,

    /// <summary>Step is currently executing.</summary>
    InProgress,

    /// <summary>Step completed successfully.</summary>
    Completed,

    /// <summary>Step failed and requires operator attention.</summary>
    Error,
}

/// <summary>
/// Represents an individual workflow step executed while provisioning a radio bridge.
/// </summary>
public sealed class RadioProvisioningStepViewModel
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RadioProvisioningStepViewModel"/> class.
    /// </summary>
    public RadioProvisioningStepViewModel(
        string title,
        string detail,
        RadioProvisioningStepStatus status,
        DateTimeOffset? updatedAt)
    {
        Title = title ?? throw new ArgumentNullException(nameof(title));
        Detail = detail ?? throw new ArgumentNullException(nameof(detail));
        Status = status;
        UpdatedAt = updatedAt;
    }

    /// <summary>Gets the short display title for the step.</summary>
    public string Title { get; }

    /// <summary>Gets the detailed description of the step.</summary>
    public string Detail { get; }

    /// <summary>Gets the provisioning status.</summary>
    public RadioProvisioningStepStatus Status { get; }

    /// <summary>Gets the last time the step status changed.</summary>
    public DateTimeOffset? UpdatedAt { get; }

    /// <summary>Gets a formatted display string for the status.</summary>
    public string StatusDisplay => Status switch
    {
        RadioProvisioningStepStatus.Completed => "Completed",
        RadioProvisioningStepStatus.InProgress => "In progress",
        RadioProvisioningStepStatus.Pending => "Pending",
        RadioProvisioningStepStatus.Error => "Failed",
        _ => Status.ToString(),
    };

    /// <summary>Gets a formatted display string for the timestamp.</summary>
    public string TimestampDisplay => UpdatedAt?.ToLocalTime().ToString("HH:mm:ss") ?? "—";

    /// <summary>Gets a value indicating whether the step is currently running.</summary>
    public bool IsInProgress => Status == RadioProvisioningStepStatus.InProgress;

    /// <summary>Gets a value indicating whether the step completed successfully.</summary>
    public bool IsCompleted => Status == RadioProvisioningStepStatus.Completed;

    /// <summary>Gets a value indicating whether the step has not yet started.</summary>
    public bool IsPending => Status == RadioProvisioningStepStatus.Pending;

    /// <summary>Gets a value indicating whether the step failed.</summary>
    public bool IsError => Status == RadioProvisioningStepStatus.Error;
}
