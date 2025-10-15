using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Aog.UI.Avalonia.ViewModels;

/// <summary>
/// Coordinates zone gating toggles and manual override policy UX surfaced in the shell.
/// </summary>
public sealed class ZoneConstraintPolicyViewModel : ObservableObject
{
    private readonly ObservableCollection<ZoneConstraintToggleViewModel> _toggles;
    private readonly ObservableCollection<ZoneOverrideEventViewModel> _overrideHistory;
    private readonly Func<DateTimeOffset> _clock;
    private string _statusMessage;
    private string _policySummary;

    /// <summary>
    /// Initializes a new instance of the <see cref="ZoneConstraintPolicyViewModel"/> class.
    /// </summary>
    /// <param name="clock">Optional clock used for deterministic tests.</param>
    public ZoneConstraintPolicyViewModel(Func<DateTimeOffset>? clock = null)
    {
        _clock = clock ?? (() => DateTimeOffset.UtcNow);
        _statusMessage = "Zone gating policies follow ADR-027 defaults. Review overrides before engaging automation.";
        _policySummary = string.Empty;

        _overrideHistory = new ObservableCollection<ZoneOverrideEventViewModel>();
        _overrideHistory.CollectionChanged += (_, __) =>
        {
            OnPropertyChanged(nameof(OverrideHistory));
            OnPropertyChanged(nameof(HasOverrideHistory));
            OnPropertyChanged(nameof(HasNoOverrideHistory));
        };

        _toggles = new ObservableCollection<ZoneConstraintToggleViewModel>();
        SeedToggles();
        UpdatePolicySummary();
    }

    /// <summary>Gets the toggles representing the canonical zone types.</summary>
    public IReadOnlyList<ZoneConstraintToggleViewModel> Toggles => _toggles;

    /// <summary>Gets the recorded manual override history entries.</summary>
    public IReadOnlyList<ZoneOverrideEventViewModel> OverrideHistory => _overrideHistory;

    /// <summary>Gets the status message displayed alongside the toggles.</summary>
    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    /// <summary>Gets the policy summary computed from the current toggle state.</summary>
    public string PolicySummary
    {
        get => _policySummary;
        private set => SetProperty(ref _policySummary, value);
    }

    /// <summary>Gets a value indicating whether any override history entries exist.</summary>
    public bool HasOverrideHistory => _overrideHistory.Count > 0;

    /// <summary>Gets a value indicating whether no override history entries have been recorded.</summary>
    public bool HasNoOverrideHistory => !HasOverrideHistory;

    private void SeedToggles()
    {
        _toggles.Add(new ZoneConstraintToggleViewModel(
            zoneType: "boundary",
            displayName: "Field boundaries",
            description: "Hard stop for guidance and automation outside the legal boundary.",
            bufferSummary: "Buffers: drive 2.0 m, work 0.5 m",
            policySummary: "Boundary gating cannot be disabled while automation is engaged.",
            supportsManualOverride: false,
            isEnabled: true,
            overridePolicyHint: "Boundary enforcement is always on to satisfy compliance requirements.",
            defaultOverrideReason: string.Empty,
            defaultOverrideDuration: null,
            clock: _clock,
            stateChanged: OnToggleStateChanged,
            overrideApplied: OnOverrideApplied,
            overrideCleared: OnOverrideCleared));

        _toggles.Add(new ZoneConstraintToggleViewModel(
            zoneType: "headland",
            displayName: "Headland zones",
            description: "Trim guidance passes and bias turn planners inside buffered headlands.",
            bufferSummary: "Buffers: drive 1.5 m, work 1.0 m",
            policySummary: "Operators may temporarily relax headland trimming after acknowledging the timer.",
            supportsManualOverride: true,
            isEnabled: true,
            overridePolicyHint: "Hold for 3 seconds to allow limited headland override (max 3 minutes).",
            defaultOverrideReason: "Cleanup pass inside headland",
            defaultOverrideDuration: TimeSpan.FromMinutes(3),
            clock: _clock,
            stateChanged: OnToggleStateChanged,
            overrideApplied: OnOverrideApplied,
            overrideCleared: OnOverrideCleared));

        _toggles.Add(new ZoneConstraintToggleViewModel(
            zoneType: "keepout",
            displayName: "Keep-out zones",
            description: "Forces autosteer disengage and sections off in hazardous areas.",
            bufferSummary: "Buffers: drive 3.0 m, work 5.0 m",
            policySummary: "Manual overrides require hold-to-confirm and explicit reason logging.",
            supportsManualOverride: true,
            isEnabled: true,
            overridePolicyHint: "Hold for 5 seconds; override automatically clears after 5 minutes.",
            defaultOverrideReason: "Rock pile bypass for obstacle removal",
            defaultOverrideDuration: TimeSpan.FromMinutes(5),
            clock: _clock,
            stateChanged: OnToggleStateChanged,
            overrideApplied: OnOverrideApplied,
            overrideCleared: OnOverrideCleared));

        _toggles.Add(new ZoneConstraintToggleViewModel(
            zoneType: "workdisabled",
            displayName: "Work-disabled zones",
            description: "Allows driving while forcing product off with audit logging.",
            bufferSummary: "Buffers: drive 1.0 m, work 4.0 m",
            policySummary: "Timer-based overrides capture actor, reason, and expiry for compliance.",
            supportsManualOverride: true,
            isEnabled: true,
            overridePolicyHint: "Timer limit 10 minutes; requires agronomy acknowledgement on resume.",
            defaultOverrideReason: "Spot spraying required for replant",
            defaultOverrideDuration: TimeSpan.FromMinutes(10),
            clock: _clock,
            stateChanged: OnToggleStateChanged,
            overrideApplied: OnOverrideApplied,
            overrideCleared: OnOverrideCleared));
    }

    private void OnToggleStateChanged()
    {
        UpdatePolicySummary();
        StatusMessage = PolicySummary;
    }

    private void OnOverrideApplied(ZoneOverrideEvent zoneEvent)
    {
        _overrideHistory.Insert(0, new ZoneOverrideEventViewModel(
            zoneEvent.ZoneTypeDisplay,
            zoneEvent.ActionDisplay,
            zoneEvent.Actor,
            zoneEvent.Reason,
            zoneEvent.Timestamp,
            zoneEvent.ExpiresAt));

        StatusMessage = $"{zoneEvent.ZoneTypeDisplay} override active. Review audit log before resuming automation.";
    }

    private void OnOverrideCleared(ZoneOverrideEvent zoneEvent)
    {
        _overrideHistory.Insert(0, new ZoneOverrideEventViewModel(
            zoneEvent.ZoneTypeDisplay,
            zoneEvent.ActionDisplay,
            zoneEvent.Actor,
            zoneEvent.Reason,
            zoneEvent.Timestamp,
            zoneEvent.ExpiresAt));

        StatusMessage = $"{zoneEvent.ZoneTypeDisplay} override cleared.";
    }

    private void UpdatePolicySummary()
    {
        var disabled = _toggles.Where(toggle => !toggle.IsEnabled).Select(toggle => toggle.DisplayName).ToArray();
        if (disabled.Length == 0)
        {
            PolicySummary = "All zone constraints are enforcing automation gates. Overrides require operator acknowledgement.";
        }
        else
        {
            PolicySummary = $"Automation gating disabled for {string.Join(", ", disabled)}. Review before engaging controllers.";
        }
    }
}
