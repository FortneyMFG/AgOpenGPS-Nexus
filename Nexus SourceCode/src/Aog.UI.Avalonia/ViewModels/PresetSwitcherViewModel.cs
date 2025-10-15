using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Aog.UI.Avalonia.ViewModels;

/// <summary>
/// Provides presentation data for the preset switcher panel.
/// </summary>
public sealed class PresetSwitcherViewModel : ObservableObject
{
    private readonly List<PresetOptionViewModel> _presets = new();
    private readonly ReadOnlyCollection<PresetOptionViewModel> _readOnlyPresets;
    private string _activePresetDisplay = string.Empty;
    private string _activePresetDescription = string.Empty;
    private string _activePresetLayout = string.Empty;
    private string _activePresetCapabilities = string.Empty;
    private string _orchestrationStatus = string.Empty;
    private string? _activePresetTaskSummary;
    private IReadOnlyList<string> _activePresetAlerts = Array.Empty<string>();
    private bool _isBusy;

    private PresetSwitcherViewModel(string summary)
    {
        Summary = summary ?? throw new ArgumentNullException(nameof(summary));
        _readOnlyPresets = _presets.AsReadOnly();
    }

    /// <summary>Gets the descriptive summary displayed above the presets list.</summary>
    public string Summary { get; }

    /// <summary>Gets the presets surfaced in the UI.</summary>
    public IReadOnlyList<PresetOptionViewModel> Presets => _readOnlyPresets;

    /// <summary>Gets the display name of the active preset.</summary>
    public string ActivePresetDisplay
    {
        get => _activePresetDisplay;
        private set => SetProperty(ref _activePresetDisplay, value);
    }

    /// <summary>Gets the description for the active preset.</summary>
    public string ActivePresetDescription
    {
        get => _activePresetDescription;
        private set => SetProperty(ref _activePresetDescription, value);
    }

    /// <summary>Gets the layout summary for the active preset.</summary>
    public string ActivePresetLayout
    {
        get => _activePresetLayout;
        private set => SetProperty(ref _activePresetLayout, value);
    }

    /// <summary>Gets the capability summary for the active preset.</summary>
    public string ActivePresetCapabilities
    {
        get => _activePresetCapabilities;
        private set => SetProperty(ref _activePresetCapabilities, value);
    }

    /// <summary>Gets the orchestration status for the active preset.</summary>
    public string OrchestrationStatus
    {
        get => _orchestrationStatus;
        private set => SetProperty(ref _orchestrationStatus, value);
    }

    /// <summary>Gets the orchestration task summary for the active preset.</summary>
    public string? ActivePresetTaskSummary
    {
        get => _activePresetTaskSummary;
        private set
        {
            if (SetProperty(ref _activePresetTaskSummary, value))
            {
                OnPropertyChanged(nameof(HasActivePresetTaskSummary));
            }
        }
    }

    /// <summary>Gets a value indicating whether the active preset exposes a task summary.</summary>
    public bool HasActivePresetTaskSummary => !string.IsNullOrWhiteSpace(ActivePresetTaskSummary);

    /// <summary>Gets the alerts associated with the active preset.</summary>
    public IReadOnlyList<string> ActivePresetAlerts
    {
        get => _activePresetAlerts;
        private set
        {
            if (SetProperty(ref _activePresetAlerts, value))
            {
                OnPropertyChanged(nameof(HasActivePresetAlerts));
            }
        }
    }

    /// <summary>Gets a value indicating whether the active preset exposes alerts.</summary>
    public bool HasActivePresetAlerts => ActivePresetAlerts.Count > 0;

    /// <summary>Gets a value indicating whether orchestration is actively running.</summary>
    public bool IsBusy
    {
        get => _isBusy;
        private set => SetProperty(ref _isBusy, value);
    }

    /// <summary>
    /// Creates a sample preset switcher populated with representative presets.
    /// </summary>
    public static PresetSwitcherViewModel CreateSample()
    {
        var switcher = new PresetSwitcherViewModel(
            "Apply equipment presets and monitor orchestration progress surfaced from TaskService.");

        switcher.AddPreset(
            presetId: "preset:planter-16r",
            displayName: "Planter – 16R",
            description: "Links rate, sections, and mapping for the 16 row planter bundle.",
            layoutSummary: "Layout: Planter dashboard v2 (live link)",
            capabilitySummary: "Capabilities: Rate control, Sections, Mapping",
            orchestrationState: PresetOrchestrationState.Ready,
            statusDisplay: "Ready — Last applied at 07:15 UTC",
            taskSummary: "All dependencies healthy and ready for fieldwork.",
            alerts: Array.Empty<string>(),
            isActive: true);

        switcher.AddPreset(
            presetId: "preset:sprayer-120ft",
            displayName: "Sprayer – 120 ft",
            description: "Preps boom sections, weather guardrails, and GNSS warm-up for spraying.",
            layoutSummary: "Layout: Sprayer operations snapshot (local copy)",
            capabilitySummary: "Capabilities: Sections, Weather guardrails",
            orchestrationState: PresetOrchestrationState.Running,
            statusDisplay: "Running — Orchestrating 3 of 5 tasks",
            taskSummary: "Mapping plugin warming caches and validating leases.",
            alerts: new[]
            {
                "Mapping plugin warming caches",
                "Sections plugin verifying capability leases"
            },
            isActive: false);

        switcher.AddPreset(
            presetId: "preset:harvest-combine",
            displayName: "Harvest – Combine",
            description: "Loads yield analytics, telemetry logging, and radio bridge for harvest rigs.",
            layoutSummary: "Layout: Harvest dashboard (live link missing)",
            capabilitySummary: "Capabilities: Yield analytics, Telemetry logging",
            orchestrationState: PresetOrchestrationState.Blocked,
            statusDisplay: "Blocked — Awaiting dependency recovery",
            taskSummary: null,
            alerts: new[]
            {
                "Telemetry logging plugin offline",
                "Radio bridge awaiting provisioning handshake"
            },
            isActive: false);

        switcher.InitializeActivePreset();
        return switcher;
    }

    internal void ApplyPreset(PresetOptionViewModel option)
    {
        if (option is null)
        {
            throw new ArgumentNullException(nameof(option));
        }

        foreach (var preset in _presets)
        {
            preset.SetActive(ReferenceEquals(preset, option));
        }

        ActivePresetDisplay = option.DisplayName;
        ActivePresetDescription = option.Description;
        ActivePresetLayout = option.LayoutSummary;
        ActivePresetCapabilities = option.CapabilitySummary;
        OrchestrationStatus = option.StatusDisplay;
        ActivePresetTaskSummary = option.TaskSummary;
        ActivePresetAlerts = option.Alerts;
        IsBusy = option.OrchestrationState == PresetOrchestrationState.Running;
    }

    private void InitializeActivePreset()
    {
        var initial = _presets.FirstOrDefault(preset => preset.IsActive) ?? _presets.FirstOrDefault();
        if (initial is not null)
        {
            ApplyPreset(initial);
        }
    }

    private void AddPreset(
        string presetId,
        string displayName,
        string description,
        string layoutSummary,
        string capabilitySummary,
        PresetOrchestrationState orchestrationState,
        string statusDisplay,
        string? taskSummary,
        IReadOnlyList<string> alerts,
        bool isActive)
    {
        var preset = new PresetOptionViewModel(
            this,
            presetId,
            displayName,
            description,
            layoutSummary,
            capabilitySummary,
            orchestrationState,
            statusDisplay,
            taskSummary,
            alerts,
            isActive);

        _presets.Add(preset);
    }
}
