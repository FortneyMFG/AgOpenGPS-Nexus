using System;
using System.Collections.Generic;
using System.Linq;

namespace Aog.UI.Avalonia.ViewModels;

/// <summary>
/// Describes a RadioBridge endpoint that is undergoing provisioning.
/// </summary>
public sealed class RadioProvisioningDeviceViewModel
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RadioProvisioningDeviceViewModel"/> class.
    /// </summary>
    public RadioProvisioningDeviceViewModel(
        string deviceId,
        string displayName,
        string transportDisplay,
        string signalDisplay,
        string topicGrantSummary,
        IReadOnlyList<RadioProvisioningStepViewModel> steps,
        string? note = null)
    {
        DeviceId = deviceId ?? throw new ArgumentNullException(nameof(deviceId));
        DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
        TransportDisplay = transportDisplay ?? throw new ArgumentNullException(nameof(transportDisplay));
        SignalDisplay = signalDisplay ?? throw new ArgumentNullException(nameof(signalDisplay));
        TopicGrantSummary = topicGrantSummary ?? throw new ArgumentNullException(nameof(topicGrantSummary));
        Steps = steps ?? throw new ArgumentNullException(nameof(steps));
        Note = note;
    }

    /// <summary>Gets the device identifier.</summary>
    public string DeviceId { get; }

    /// <summary>Gets the operator facing label.</summary>
    public string DisplayName { get; }

    /// <summary>Gets a display string describing the transport and firmware.</summary>
    public string TransportDisplay { get; }

    /// <summary>Gets the current radio health summary.</summary>
    public string SignalDisplay { get; }

    /// <summary>Gets a summary of mesh topics granted to the device.</summary>
    public string TopicGrantSummary { get; }

    /// <summary>Gets the provisioning workflow steps.</summary>
    public IReadOnlyList<RadioProvisioningStepViewModel> Steps { get; }

    /// <summary>Gets an optional operator note or instruction.</summary>
    public string? Note { get; }

    /// <summary>Gets a value indicating whether an operator note is present.</summary>
    public bool HasNote => !string.IsNullOrWhiteSpace(Note);

    /// <summary>Gets the aggregate status display derived from the steps.</summary>
    public string StatusDisplay
    {
        get
        {
            if (Steps.Any(step => step.IsError))
            {
                return "Needs attention";
            }

            if (Steps.All(step => step.IsCompleted))
            {
                return "Provisioned";
            }

            if (Steps.Any(step => step.IsInProgress))
            {
                return "Provisioning";
            }

            return "Pending";
        }
    }

    /// <summary>Gets a value indicating whether the device currently requires attention.</summary>
    public bool RequiresAttention => Steps.Any(step => step.IsError);
}
