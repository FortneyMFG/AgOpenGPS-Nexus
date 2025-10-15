using System;

namespace Aog.UI.Avalonia.ViewModels;

/// <summary>
/// Represents a completed import or export workflow entry for ADR-027 zone interop.
/// </summary>
public sealed class ZoneTransferEventViewModel
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ZoneTransferEventViewModel"/> class.
    /// </summary>
    public ZoneTransferEventViewModel(string workflowName, string actionDisplay, string detail, DateTimeOffset timestamp)
    {
        WorkflowName = workflowName ?? throw new ArgumentNullException(nameof(workflowName));
        ActionDisplay = actionDisplay ?? throw new ArgumentNullException(nameof(actionDisplay));
        Detail = detail ?? string.Empty;
        Timestamp = timestamp;
    }

    /// <summary>Gets the workflow name associated with the transfer.</summary>
    public string WorkflowName { get; }

    /// <summary>Gets the display text summarizing the action.</summary>
    public string ActionDisplay { get; }

    /// <summary>Gets additional detail for the transfer.</summary>
    public string Detail { get; }

    /// <summary>Gets the timestamp when the transfer completed.</summary>
    public DateTimeOffset Timestamp { get; }

    /// <summary>Gets the formatted timestamp for UI surfaces.</summary>
    public string TimestampDisplay => Timestamp.ToLocalTime().ToString("HH:mm:ss");
}

/// <summary>
/// Structured transfer event emitted by zone import/export workflows.
/// </summary>
/// <param name="WorkflowName">Display name of the workflow.</param>
/// <param name="ActionDisplay">Display text describing what occurred.</param>
/// <param name="Detail">Detailed summary of the transfer.</param>
/// <param name="Timestamp">Completion timestamp.</param>
public readonly record struct ZoneTransferEvent(
    string WorkflowName,
    string ActionDisplay,
    string Detail,
    DateTimeOffset Timestamp);
