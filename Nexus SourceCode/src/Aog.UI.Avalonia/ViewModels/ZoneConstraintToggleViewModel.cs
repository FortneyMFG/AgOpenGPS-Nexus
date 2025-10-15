using System;

namespace Aog.UI.Avalonia.ViewModels;

/// <summary>
/// Represents enforcement state and manual override policy for a zone type.
/// </summary>
public sealed class ZoneConstraintToggleViewModel : ObservableObject
{
    private const string DefaultActor = "Operator: Demo";

    private readonly Func<DateTimeOffset> _clock;
    private readonly Action _stateChanged;
    private readonly Action<ZoneOverrideEvent> _overrideApplied;
    private readonly Action<ZoneOverrideEvent> _overrideCleared;
    private readonly string _defaultOverrideReason;
    private readonly TimeSpan? _defaultOverrideDuration;

    private bool _isEnabled;
    private bool _hasActiveOverride;
    private DateTimeOffset? _overrideExpiresAt;
    private string? _overrideReason;

    private readonly DelegateCommand _applyOverrideCommand;
    private readonly DelegateCommand _clearOverrideCommand;

    /// <summary>
    /// Initializes a new instance of the <see cref="ZoneConstraintToggleViewModel"/> class.
    /// </summary>
    public ZoneConstraintToggleViewModel(
        string zoneType,
        string displayName,
        string description,
        string bufferSummary,
        string policySummary,
        bool supportsManualOverride,
        bool isEnabled,
        string overridePolicyHint,
        string defaultOverrideReason,
        TimeSpan? defaultOverrideDuration,
        Func<DateTimeOffset> clock,
        Action stateChanged,
        Action<ZoneOverrideEvent> overrideApplied,
        Action<ZoneOverrideEvent> overrideCleared)
    {
        ZoneType = zoneType ?? throw new ArgumentNullException(nameof(zoneType));
        DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        BufferSummary = bufferSummary ?? throw new ArgumentNullException(nameof(bufferSummary));
        PolicySummary = policySummary ?? throw new ArgumentNullException(nameof(policySummary));
        SupportsManualOverride = supportsManualOverride;
        OverridePolicyHint = overridePolicyHint ?? string.Empty;
        _defaultOverrideReason = defaultOverrideReason ?? string.Empty;
        _defaultOverrideDuration = defaultOverrideDuration;
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _stateChanged = stateChanged ?? throw new ArgumentNullException(nameof(stateChanged));
        _overrideApplied = overrideApplied ?? throw new ArgumentNullException(nameof(overrideApplied));
        _overrideCleared = overrideCleared ?? throw new ArgumentNullException(nameof(overrideCleared));

        _applyOverrideCommand = new DelegateCommand(_ => ApplyOverride(), _ => CanApplyOverride());
        _clearOverrideCommand = new DelegateCommand(_ => ClearOverride(), _ => CanClearOverride());

        _isEnabled = isEnabled;
        _hasActiveOverride = false;
    }

    /// <summary>Gets the zone type identifier.</summary>
    public string ZoneType { get; }

    /// <summary>Gets the operator-facing display name for the zone type.</summary>
    public string DisplayName { get; }

    /// <summary>Gets a description of how the zone impacts automation.</summary>
    public string Description { get; }

    /// <summary>Gets a summary of the configured buffers for this zone.</summary>
    public string BufferSummary { get; }

    /// <summary>Gets a summary of the enforcement policy for this zone type.</summary>
    public string PolicySummary { get; }

    /// <summary>Gets a hint describing how manual overrides behave.</summary>
    public string OverridePolicyHint { get; }

    /// <summary>Gets a value indicating whether an override policy hint should be displayed.</summary>
    public bool HasOverridePolicyHint => !string.IsNullOrWhiteSpace(OverridePolicyHint);

    /// <summary>Gets a value indicating whether manual overrides are supported.</summary>
    public bool SupportsManualOverride { get; }

    /// <summary>Gets or sets a value indicating whether automation gating is enabled.</summary>
    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            if (SetProperty(ref _isEnabled, value))
            {
                OnPropertyChanged(nameof(EnforcementDisplay));
                _stateChanged();
            }
        }
    }

    /// <summary>Gets a human-readable description of the enforcement state.</summary>
    public string EnforcementDisplay => IsEnabled ? "Constraint enforced" : "Constraint disabled";

    /// <summary>Gets a value indicating whether a manual override is currently active.</summary>
    public bool HasActiveOverride
    {
        get => _hasActiveOverride;
        private set
        {
            if (SetProperty(ref _hasActiveOverride, value))
            {
                OnPropertyChanged(nameof(OverrideStatusDisplay));
                OnPropertyChanged(nameof(OverrideExpiryDisplay));
                _applyOverrideCommand.RaiseCanExecuteChanged();
                _clearOverrideCommand.RaiseCanExecuteChanged();
                _stateChanged();
            }
        }
    }

    /// <summary>Gets the descriptive status for any active overrides.</summary>
    public string OverrideStatusDisplay
    {
        get
        {
            if (!HasActiveOverride)
            {
                return "No active overrides.";
            }

            var expiry = OverrideExpiryDisplay;
            return _overrideReason is { Length: > 0 }
                ? $"Override active {expiry}. Reason: {_overrideReason}."
                : $"Override active {expiry}.";
        }
    }

    /// <summary>Gets the expiry display string for the active override.</summary>
    public string OverrideExpiryDisplay
    {
        get
        {
            if (!HasActiveOverride)
            {
                return string.Empty;
            }

            return _overrideExpiresAt is { } expires
                ? $"until {expires.ToLocalTime():HH:mm}"
                : "(no expiry)";
        }
    }

    /// <summary>Gets the command that applies a manual override.</summary>
    public DelegateCommand ApplyOverrideCommand => _applyOverrideCommand;

    /// <summary>Gets the command that clears the active manual override.</summary>
    public DelegateCommand ClearOverrideCommand => _clearOverrideCommand;

    private void ApplyOverride()
    {
        if (!SupportsManualOverride)
        {
            return;
        }

        var timestamp = _clock();
        _overrideReason = string.IsNullOrWhiteSpace(_defaultOverrideReason)
            ? "Operator acknowledged constraint."
            : _defaultOverrideReason;
        _overrideExpiresAt = _defaultOverrideDuration.HasValue
            ? timestamp.Add(_defaultOverrideDuration.Value)
            : null;

        HasActiveOverride = true;

        _overrideApplied(new ZoneOverrideEvent(
            DisplayName,
            "Override applied",
            DefaultActor,
            _overrideReason!,
            timestamp,
            _overrideExpiresAt));
    }

    private void ClearOverride()
    {
        if (!SupportsManualOverride || !HasActiveOverride)
        {
            return;
        }

        var timestamp = _clock();
        var reason = _overrideReason ?? "Operator cleared override.";
        var expiresAt = _overrideExpiresAt;

        HasActiveOverride = false;
        _overrideReason = null;
        _overrideExpiresAt = null;

        _overrideCleared(new ZoneOverrideEvent(
            DisplayName,
            "Override cleared",
            DefaultActor,
            reason,
            timestamp,
            expiresAt));
    }

    private bool CanApplyOverride()
    {
        return SupportsManualOverride && !HasActiveOverride;
    }

    private bool CanClearOverride()
    {
        return SupportsManualOverride && HasActiveOverride;
    }
}
