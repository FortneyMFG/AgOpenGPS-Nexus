using System;
using System.Collections.Generic;

namespace Aog.UI.Avalonia.ViewModels;

/// <summary>
/// Represents an individual preset surfaced in the preset switcher panel.
/// </summary>
public sealed class PresetOptionViewModel : ObservableObject
{
    private readonly PresetSwitcherViewModel _owner;
    private readonly DelegateCommand _applyCommand;
    private bool _isActive;

    /// <summary>
    /// Initializes a new instance of the <see cref="PresetOptionViewModel"/> class.
    /// </summary>
    internal PresetOptionViewModel(
        PresetSwitcherViewModel owner,
        string presetId,
        string displayName,
        string description,
        string layoutSummary,
        string capabilitySummary,
        PresetOrchestrationState orchestrationState,
        string statusDisplay,
        string? taskSummary,
        IReadOnlyList<string>? alerts,
        bool isActive)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        PresetId = presetId ?? throw new ArgumentNullException(nameof(presetId));
        DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
        Description = description ?? string.Empty;
        LayoutSummary = layoutSummary ?? string.Empty;
        CapabilitySummary = capabilitySummary ?? string.Empty;
        OrchestrationState = orchestrationState;
        StatusDisplay = statusDisplay ?? string.Empty;
        TaskSummary = taskSummary;
        Alerts = alerts ?? Array.Empty<string>();
        _isActive = isActive;
        _applyCommand = new DelegateCommand(_ => _owner.ApplyPreset(this), _ => IsEnabled);
    }

    /// <summary>Gets the preset identifier.</summary>
    public string PresetId { get; }

    /// <summary>Gets the display name shown in the UI.</summary>
    public string DisplayName { get; }

    /// <summary>Gets the descriptive summary of the preset.</summary>
    public string Description { get; }

    /// <summary>Gets the summary of the linked layout.</summary>
    public string LayoutSummary { get; }

    /// <summary>Gets the capability summary for the preset.</summary>
    public string CapabilitySummary { get; }

    /// <summary>Gets the orchestration state for the preset.</summary>
    public PresetOrchestrationState OrchestrationState { get; }

    /// <summary>Gets the status display string.</summary>
    public string StatusDisplay { get; }

    /// <summary>Gets the orchestration task summary when available.</summary>
    public string? TaskSummary { get; }

    /// <summary>Gets the alerts associated with the preset.</summary>
    public IReadOnlyList<string> Alerts { get; }

    /// <summary>Gets a value indicating whether the preset has alerts.</summary>
    public bool HasAlerts => Alerts.Count > 0;

    /// <summary>Gets a value indicating whether the preset exposes a task summary.</summary>
    public bool HasTaskSummary => !string.IsNullOrWhiteSpace(TaskSummary);

    /// <summary>Gets the command used to apply the preset.</summary>
    public DelegateCommand ApplyCommand => _applyCommand;

    /// <summary>Gets a value indicating whether the preset is currently active.</summary>
    public bool IsActive
    {
        get => _isActive;
        private set
        {
            if (SetProperty(ref _isActive, value))
            {
                OnPropertyChanged(nameof(IsEnabled));
                OnPropertyChanged(nameof(ButtonLabel));
                _applyCommand.RaiseCanExecuteChanged();
            }
        }
    }

    /// <summary>Gets a value indicating whether the preset can be applied.</summary>
    public bool IsEnabled => !IsActive && OrchestrationState != PresetOrchestrationState.Blocked;

    /// <summary>Gets the label displayed on the apply button.</summary>
    public string ButtonLabel => IsActive ? "Active" : "Apply";

    /// <summary>
    /// Sets the active flag without publishing public setters.
    /// </summary>
    internal void SetActive(bool isActive) => IsActive = isActive;
}
