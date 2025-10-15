using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Aog.UI.Avalonia.ViewModels;

/// <summary>
/// Surfaces preset options and orchestration status as described in ADR-032. Implements NX-297 by wiring
/// dependency health, background task progress, and active preset selection.
/// </summary>
public sealed class PresetSwitcherViewModel : ObservableObject
{
    private readonly List<PresetOptionViewModel> _presets;
    private PresetOptionViewModel? _selectedPreset;
    private PresetOptionViewModel? _activePreset;
    private bool _isApplying;
    private string? _statusMessage;

    /// <summary>
    /// Initializes a new instance of the <see cref="PresetSwitcherViewModel"/> class.
    /// </summary>
    /// <param name="presets">Preset options available to the operator.</param>
    public PresetSwitcherViewModel(IEnumerable<PresetOptionViewModel> presets)
    {
        ArgumentNullException.ThrowIfNull(presets);

        _presets = presets.ToList();
        if (_presets.Count == 0)
        {
            throw new ArgumentException("At least one preset must be provided.", nameof(presets));
        }

        _selectedPreset = _presets[0];
        _activePreset = _selectedPreset;
    }

    /// <summary>Gets a curated sample aligned with ADR-032 orchestration flows.</summary>
    public static PresetSwitcherViewModel CreateSample()
    {
        var planterTasks = new[]
        {
            new PresetTaskStatusViewModel("task:verify-capabilities", "Verify capabilities", PresetTaskState.Succeeded, 1.0, "All dependencies satisfied."),
            new PresetTaskStatusViewModel("task:warmup-gnss", "Warm up GNSS corrections", PresetTaskState.Running, 0.65, "Streaming RTCM"),
            new PresetTaskStatusViewModel("task:load-layout", "Load planter dashboard layout", PresetTaskState.Pending),
        };

        var sprayerTasks = new[]
        {
            new PresetTaskStatusViewModel("task:verify-capabilities", "Verify capabilities", PresetTaskState.Succeeded, 1.0, "Telemetry mesh connected."),
            new PresetTaskStatusViewModel("task:prime-system", "Prime spray system", PresetTaskState.Running, 0.35, "Awaiting pressure"),
            new PresetTaskStatusViewModel("task:load-layout", "Load sprayer dashboard layout", PresetTaskState.Pending),
        };

        var harvestTasks = new[]
        {
            new PresetTaskStatusViewModel("task:verify-capabilities", "Verify capabilities", PresetTaskState.Succeeded, 1.0),
            new PresetTaskStatusViewModel("task:load-layout", "Load harvest layout", PresetTaskState.Succeeded, 1.0),
            new PresetTaskStatusViewModel("task:start-logging", "Start yield logging", PresetTaskState.Succeeded, 1.0, "Replay harness active"),
        };

        var presets = new[]
        {
            new PresetOptionViewModel(
                presetId: "preset:planter-16r",
                displayName: "Planter – 16 Row",
                summary: "Loads planter configuration, sections, and dashboard layout.",
                implementDisplayName: "Planter 16R",
                layoutDisplayName: "Planter productivity",
                health: PresetHealthState.Ready,
                requiresJobContext: true,
                blockers: Array.Empty<string>(),
                tasks: planterTasks),
            new PresetOptionViewModel(
                presetId: "preset:sprayer-120ft",
                displayName: "Sprayer – 120 ft",
                summary: "Hydrates boom presets and constraint overrides.",
                implementDisplayName: "Sprayer 120ft",
                layoutDisplayName: "Application overview",
                health: PresetHealthState.Warning,
                requiresJobContext: true,
                blockers: new[] { "Mapping plugin warming up" },
                tasks: sprayerTasks),
            new PresetOptionViewModel(
                presetId: "preset:combine-yield",
                displayName: "Combine – Yield",
                summary: "Sets up yield logging and harvest layout.",
                implementDisplayName: "Combine",
                layoutDisplayName: "Harvest analytics",
                health: PresetHealthState.Ready,
                requiresJobContext: false,
                blockers: Array.Empty<string>(),
                tasks: harvestTasks),
        };

        return new PresetSwitcherViewModel(presets)
        {
            StatusMessage = "Planter – 16 Row active",
        };
    }

    /// <summary>Gets the presets exposed to the operator.</summary>
    public IReadOnlyList<PresetOptionViewModel> Presets => _presets;

    /// <summary>Gets or sets the selected preset in the switcher.</summary>
    public PresetOptionViewModel? SelectedPreset
    {
        get => _selectedPreset;
        set
        {
            if (SetProperty(ref _selectedPreset, value) && value is not null)
            {
                StatusMessage = value == _activePreset
                    ? $"{value.DisplayName} active"
                    : $"Ready to apply {value.DisplayName}";
            }
        }
    }

    /// <summary>Gets the preset currently applied to the runtime.</summary>
    public PresetOptionViewModel? ActivePreset
    {
        get => _activePreset;
        private set => SetProperty(ref _activePreset, value);
    }

    /// <summary>Gets a value indicating whether orchestration is currently applying a preset.</summary>
    public bool IsApplying
    {
        get => _isApplying;
        private set => SetProperty(ref _isApplying, value);
    }

    /// <summary>Gets a status message summarising the most recent orchestration step.</summary>
    public string? StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    /// <summary>Begins applying the specified preset and resets task progress.</summary>
    public void BeginApply(PresetOptionViewModel preset)
    {
        ArgumentNullException.ThrowIfNull(preset);

        IsApplying = true;
        StatusMessage = $"Applying {preset.DisplayName}...";
        foreach (var task in preset.Tasks)
        {
            task.Reset();
        }

        SelectedPreset = preset;
    }

    /// <summary>Updates the state of a preset orchestration task.</summary>
    public void UpdateTaskState(string taskId, PresetTaskState state, double? progress = null, string? detail = null)
    {
        if (SelectedPreset is null)
        {
            return;
        }

        var task = SelectedPreset.Tasks.FirstOrDefault(t => string.Equals(t.TaskId, taskId, StringComparison.Ordinal));
        if (task is null)
        {
            return;
        }

        task.SetState(state);
        if (progress.HasValue)
        {
            task.Progress = progress.Value;
        }

        if (detail is not null)
        {
            task.Detail = detail;
        }
    }

    /// <summary>Completes orchestration for the supplied preset.</summary>
    public void CompleteApply(PresetOptionViewModel preset, bool succeeded, string? message = null)
    {
        ArgumentNullException.ThrowIfNull(preset);

        IsApplying = false;
        if (succeeded)
        {
            ActivePreset = preset;
            StatusMessage = message ?? $"{preset.DisplayName} applied";
            foreach (var task in preset.Tasks.Where(task => task.State is PresetTaskState.Pending or PresetTaskState.Running))
            {
                task.SetState(PresetTaskState.Succeeded);
                task.Progress = 1.0;
            }
        }
        else
        {
            StatusMessage = message ?? $"Failed to apply {preset.DisplayName}";
        }
    }
}

/// <summary>Represents a preset surfaced in the switcher.</summary>
public sealed class PresetOptionViewModel : ObservableObject
{
    private readonly List<PresetTaskStatusViewModel> _tasks;
    private PresetHealthState _health;
    private IReadOnlyList<string> _blockers;

    /// <summary>Initializes a new instance of the <see cref="PresetOptionViewModel"/> class.</summary>
    public PresetOptionViewModel(
        string presetId,
        string displayName,
        string summary,
        string implementDisplayName,
        string layoutDisplayName,
        PresetHealthState health,
        bool requiresJobContext,
        IEnumerable<string>? blockers = null,
        IEnumerable<PresetTaskStatusViewModel>? tasks = null)
    {
        PresetId = presetId ?? throw new ArgumentNullException(nameof(presetId));
        DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
        Summary = summary ?? throw new ArgumentNullException(nameof(summary));
        ImplementDisplayName = implementDisplayName ?? throw new ArgumentNullException(nameof(implementDisplayName));
        LayoutDisplayName = layoutDisplayName ?? throw new ArgumentNullException(nameof(layoutDisplayName));
        RequiresJobContext = requiresJobContext;
        _health = health;
        _blockers = blockers is null
            ? Array.Empty<string>()
            : new ReadOnlyCollection<string>(blockers.ToList());
        _tasks = tasks?.ToList() ?? new List<PresetTaskStatusViewModel>();
    }

    /// <summary>Gets the preset identifier.</summary>
    public string PresetId { get; }

    /// <summary>Gets the display name.</summary>
    public string DisplayName { get; }

    /// <summary>Gets the summary describing the preset.</summary>
    public string Summary { get; }

    /// <summary>Gets the implement friendly name associated with the preset.</summary>
    public string ImplementDisplayName { get; }

    /// <summary>Gets the layout friendly name associated with the preset.</summary>
    public string LayoutDisplayName { get; }

    /// <summary>Gets a value indicating whether the preset requires an active job context.</summary>
    public bool RequiresJobContext { get; }

    /// <summary>Gets or sets the health state of the preset.</summary>
    public PresetHealthState Health
    {
        get => _health;
        set => SetProperty(ref _health, value);
    }

    /// <summary>Gets blockers preventing orchestration. Empty list indicates no blockers.</summary>
    public IReadOnlyList<string> Blockers
    {
        get => _blockers;
        set => SetProperty(ref _blockers, new ReadOnlyCollection<string>(value?.ToList() ?? new List<string>()));
    }

    /// <summary>Gets the orchestration tasks tied to the preset.</summary>
    public IReadOnlyList<PresetTaskStatusViewModel> Tasks => _tasks;

    /// <summary>Resets orchestration tasks to their default state.</summary>
    public void ResetTasks()
    {
        foreach (var task in _tasks)
        {
            task.Reset();
        }
    }
}

/// <summary>Tracks the progress of a preset orchestration task.</summary>
public sealed class PresetTaskStatusViewModel : ObservableObject
{
    private PresetTaskState _state;
    private double _progress;
    private string? _detail;

    /// <summary>Initializes a new instance of the <see cref="PresetTaskStatusViewModel"/> class.</summary>
    public PresetTaskStatusViewModel(
        string taskId,
        string displayName,
        PresetTaskState state = PresetTaskState.Pending,
        double progress = 0d,
        string? detail = null)
    {
        TaskId = taskId ?? throw new ArgumentNullException(nameof(taskId));
        DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
        _state = state;
        _progress = Clamp(progress);
        _detail = detail;
    }

    /// <summary>Gets the task identifier.</summary>
    public string TaskId { get; }

    /// <summary>Gets the task display name.</summary>
    public string DisplayName { get; }

    /// <summary>Gets or sets the orchestration state.</summary>
    public PresetTaskState State
    {
        get => _state;
        private set => SetProperty(ref _state, value);
    }

    /// <summary>Gets or sets the task progress (0–1).</summary>
    public double Progress
    {
        get => _progress;
        set => SetProperty(ref _progress, Clamp(value));
    }

    /// <summary>Gets a human readable detail message for the task.</summary>
    public string? Detail
    {
        get => _detail;
        set => SetProperty(ref _detail, value);
    }

    /// <summary>Resets the task to the pending state.</summary>
    public void Reset()
    {
        SetState(PresetTaskState.Pending);
        Progress = 0d;
        Detail = null;
    }

    /// <summary>Updates the task state.</summary>
    public void SetState(PresetTaskState state)
    {
        State = state;
    }

    private static double Clamp(double value)
        => double.IsNaN(value) || double.IsInfinity(value)
            ? 0d
            : Math.Clamp(value, 0d, 1d);
}

/// <summary>Enumerates orchestration task states.</summary>
public enum PresetTaskState
{
    Pending,
    Running,
    Succeeded,
    Failed,
    Skipped,
}

/// <summary>Enumerates preset health states surfaced to operators.</summary>
public enum PresetHealthState
{
    Ready,
    Warning,
    Blocked,
}
