using System;
using System.Collections.Generic;

namespace Aog.UI.Avalonia.ViewModels;

/// <summary>
/// Aggregates the state required to surface radio provisioning workflows in the UI shell.
/// </summary>
public sealed class RadioProvisioningPanelViewModel
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RadioProvisioningPanelViewModel"/> class.
    /// </summary>
    public RadioProvisioningPanelViewModel(
        string summary,
        string nextActionDisplay,
        IReadOnlyList<RadioProvisioningDeviceViewModel> devices,
        IReadOnlyList<RadioProvisioningProfileViewModel> profiles,
        IReadOnlyList<RadioProvisioningAuditEntryViewModel> auditTrail)
    {
        Summary = summary ?? throw new ArgumentNullException(nameof(summary));
        NextActionDisplay = nextActionDisplay ?? throw new ArgumentNullException(nameof(nextActionDisplay));
        Devices = devices ?? throw new ArgumentNullException(nameof(devices));
        Profiles = profiles ?? throw new ArgumentNullException(nameof(profiles));
        AuditTrail = auditTrail ?? throw new ArgumentNullException(nameof(auditTrail));
    }

    /// <summary>Gets the headline summary for the panel.</summary>
    public string Summary { get; }

    /// <summary>Gets the next recommended operator action.</summary>
    public string NextActionDisplay { get; }

    /// <summary>Gets the devices being provisioned.</summary>
    public IReadOnlyList<RadioProvisioningDeviceViewModel> Devices { get; }

    /// <summary>Gets the previously generated provisioning profiles.</summary>
    public IReadOnlyList<RadioProvisioningProfileViewModel> Profiles { get; }

    /// <summary>Gets the provisioning audit trail entries.</summary>
    public IReadOnlyList<RadioProvisioningAuditEntryViewModel> AuditTrail { get; }

    /// <summary>Gets a value indicating whether any profiles have been generated.</summary>
    public bool HasProfiles => Profiles.Count > 0;

    /// <summary>Gets a value indicating whether any audit entries are available.</summary>
    public bool HasAuditEntries => AuditTrail.Count > 0;

    /// <summary>Gets a value indicating whether any devices are pending provisioning.</summary>
    public bool HasDevices => Devices.Count > 0;

    /// <summary>Creates a sample view-model with representative provisioning state.</summary>
    public static RadioProvisioningPanelViewModel CreateSample()
    {
        var now = DateTimeOffset.UtcNow;

        var devices = new List<RadioProvisioningDeviceViewModel>
        {
            new(
                "bridge.elrs.alpha",
                "Combine Alpha Bridge",
                "ELRS 2.4 GHz • Firmware 1.4.2",
                "RSSI −62 dBm • Latency 12 ms",
                "Season 2025 • Harvest AM — presence, coverage, yield.delta",
                new List<RadioProvisioningStepViewModel>
                {
                    new("Transport handshake", "ELRS handshake accepted by mesh coordinator.", RadioProvisioningStepStatus.Completed, now.AddMinutes(-12)),
                    new("Topic registry sync", "Synced 12 topics from manifest v3 (hash 8B2A).", RadioProvisioningStepStatus.Completed, now.AddMinutes(-11)),
                    new("Key exchange", "AES-CCM key fingerprint 9A1C acknowledged.", RadioProvisioningStepStatus.Completed, now.AddMinutes(-9)),
                    new("Reliability profile", "Selective repeat window 32 • FEC auto", RadioProvisioningStepStatus.Completed, now.AddMinutes(-9)),
                },
                "Ready to activate on combine.alpha"),
            new(
                "bridge.lora.bravo",
                "Cart Bravo Bridge",
                "LoRa 915 MHz • Firmware 0.9.8",
                "RSSI −78 dBm • Latency 48 ms",
                "Season 2025 • Transfer Cart — presence, trail",
                new List<RadioProvisioningStepViewModel>
                {
                    new("Transport handshake", "LoRa handshake completed; awaiting telemetry keepalive.", RadioProvisioningStepStatus.Completed, now.AddMinutes(-7)),
                    new("Topic registry sync", "Diff applied: 6 topics staged (manifest v3).", RadioProvisioningStepStatus.InProgress, now.AddMinutes(-2)),
                    new("Key exchange", "Awaiting operator approval for shared key export.", RadioProvisioningStepStatus.Pending, null),
                    new("Reliability profile", "Planned: window 24 • adaptive resend", RadioProvisioningStepStatus.Pending, null),
                },
                "Confirm key export once topic sync completes."),
            new(
                "bridge.elrs.delta",
                "Scout Tablet Delta",
                "ELRS 915 MHz • Firmware 1.3.0",
                "RSSI −84 dBm • Latency 85 ms",
                "All seasons — presence only",
                new List<RadioProvisioningStepViewModel>
                {
                    new("Transport handshake", "Handshake rejected: authentication failure.", RadioProvisioningStepStatus.Error, now.AddMinutes(-3)),
                    new("Topic registry sync", "Blocked pending authentication.", RadioProvisioningStepStatus.Pending, null),
                    new("Key exchange", "Pre-shared key mismatch detected.", RadioProvisioningStepStatus.Error, now.AddMinutes(-3)),
                    new("Reliability profile", "Awaiting handshake recovery.", RadioProvisioningStepStatus.Pending, null),
                },
                "Regenerate profile and confirm operator PIN before reattempting."),
        };

        var profiles = new List<RadioProvisioningProfileViewModel>
        {
            new(
                "combine.alpha",
                "ELRS 2.4 GHz",
                "Season 2025 Harvest • presence, coverage, yield.delta",
                "Key fingerprint 9A1C-7FEE",
                "Selective repeat window 32 • FEC adaptive",
                now.AddHours(-2)),
            new(
                "cart.bravo",
                "LoRa 915 MHz",
                "Season 2025 Cart • presence, trail",
                "Key fingerprint 4B12-3C90",
                "Window 24 • resend interval 180 ms",
                now.AddHours(-6)),
        };

        var auditTrail = new List<RadioProvisioningAuditEntryViewModel>
        {
            new(
                "combine.alpha provisioned",
                "AES-CCM key distributed and acked by mesh controller.",
                RadioProvisioningAuditSeverity.Info,
                now.AddMinutes(-8)),
            new(
                "scout.delta quarantined",
                "Authentication failure exceeded retries; bridge placed in quarantine mode.",
                RadioProvisioningAuditSeverity.Warning,
                now.AddMinutes(-3)),
            new(
                "manifest registry updated",
                "Topic registry v3 published with ELRS/LoRa hash alignment.",
                RadioProvisioningAuditSeverity.Info,
                now.AddMinutes(-1)),
        };

        var summary = "3 radio bridges in provisioning — 1 ready, 1 progressing, 1 needs attention.";
        var nextAction = "Next action: review scout.delta authentication failure before reissuing keys.";

        return new RadioProvisioningPanelViewModel(summary, nextAction, devices, profiles, auditTrail);
    }
}
