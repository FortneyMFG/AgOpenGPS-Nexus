using System;
using System.Collections.Generic;
using System.Linq;

namespace Aog.UI.Avalonia.ViewModels;

/// <summary>
/// Represents a mesh device and its share/subscribe grants in the UI.
/// </summary>
public sealed class MeshDeviceAccessViewModel
{
    public MeshDeviceAccessViewModel(
        string deviceId,
        string displayName,
        string presenceDisplay,
        string lastSeenDisplay,
        string capabilitySummary,
        IReadOnlyList<MeshAccessGrantViewModel> shareGrants,
        IReadOnlyList<MeshAccessGrantViewModel> subscribeGrants)
    {
        DeviceId = deviceId ?? throw new ArgumentNullException(nameof(deviceId));
        DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
        PresenceDisplay = presenceDisplay ?? throw new ArgumentNullException(nameof(presenceDisplay));
        LastSeenDisplay = lastSeenDisplay ?? throw new ArgumentNullException(nameof(lastSeenDisplay));
        CapabilitySummary = capabilitySummary ?? throw new ArgumentNullException(nameof(capabilitySummary));
        ShareGrants = shareGrants ?? throw new ArgumentNullException(nameof(shareGrants));
        SubscribeGrants = subscribeGrants ?? throw new ArgumentNullException(nameof(subscribeGrants));
    }

    /// <summary>Gets the device identifier.</summary>
    public string DeviceId { get; }

    /// <summary>Gets the human-readable display name for the device.</summary>
    public string DisplayName { get; }

    /// <summary>Gets a formatted presence summary.</summary>
    public string PresenceDisplay { get; }

    /// <summary>Gets a formatted "last seen" display string.</summary>
    public string LastSeenDisplay { get; }

    /// <summary>Gets the capability summary for the device.</summary>
    public string CapabilitySummary { get; }

    /// <summary>Gets the share grants associated with the device.</summary>
    public IReadOnlyList<MeshAccessGrantViewModel> ShareGrants { get; }

    /// <summary>Gets the subscribe grants associated with the device.</summary>
    public IReadOnlyList<MeshAccessGrantViewModel> SubscribeGrants { get; }

    /// <summary>Gets a value indicating whether any share grants exist.</summary>
    public bool HasShareGrants => ShareGrants.Count > 0;

    /// <summary>Gets a value indicating whether any subscribe grants exist.</summary>
    public bool HasSubscribeGrants => SubscribeGrants.Count > 0;

    /// <summary>Gets a single string summarising layers shared by this device.</summary>
    public string ShareLayerSummary => FormatLayerSummary(ShareGrants);

    /// <summary>Gets a single string summarising layers subscribed by this device.</summary>
    public string SubscribeLayerSummary => FormatLayerSummary(SubscribeGrants);

    private static string FormatLayerSummary(IEnumerable<MeshAccessGrantViewModel> grants)
    {
        var layers = grants
            .Select(grant => grant.LayerDisplay)
            .Where(layer => !string.IsNullOrWhiteSpace(layer))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return layers.Length == 0 ? "—" : string.Join(", ", layers);
    }
}
