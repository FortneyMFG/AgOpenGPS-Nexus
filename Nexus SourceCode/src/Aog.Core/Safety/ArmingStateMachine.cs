using System;

namespace Aog.Core.Safety;

/// <summary>
/// Enforces arming requirements so Core only emits actuator outputs when both armed and profile valid.
/// </summary>
public sealed class ArmingStateMachine
{
    private readonly object _gate = new();
    private ArmingState _state = ArmingState.Disarmed;
    private bool _profileValid;

    /// <summary>
    /// Gets the current arming state.
    /// </summary>
    public ArmingState State
    {
        get
        {
            lock (_gate)
            {
                return _state;
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether the active guidance profile is valid.
    /// </summary>
    public bool HasValidProfile
    {
        get
        {
            lock (_gate)
            {
                return _profileValid;
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether actuator outputs may be emitted.
    /// </summary>
    public bool CanEmitOutputs
    {
        get
        {
            lock (_gate)
            {
                return _state == ArmingState.Armed && _profileValid;
            }
        }
    }

    /// <summary>
    /// Attempts to arm the system. Arming requires a valid guidance profile.
    /// </summary>
    public ArmingTransitionResult Arm()
    {
        lock (_gate)
        {
            var previous = _state;

            if (!_profileValid)
            {
                return ArmingTransitionResult.Blocked(previous, "Guidance profile is not valid.");
            }

            if (previous == ArmingState.Armed)
            {
                return ArmingTransitionResult.Allowed(previous, previous, changed: false);
            }

            _state = ArmingState.Armed;
            return ArmingTransitionResult.Allowed(previous, _state, changed: true);
        }
    }

    /// <summary>
    /// Disarms the system.
    /// </summary>
    public ArmingTransitionResult Disarm()
    {
        lock (_gate)
        {
            var previous = _state;

            if (previous == ArmingState.Disarmed)
            {
                return ArmingTransitionResult.Allowed(previous, previous, changed: false);
            }

            _state = ArmingState.Disarmed;
            return ArmingTransitionResult.Allowed(previous, _state, changed: true);
        }
    }

    /// <summary>
    /// Updates the profile validity flag. Invalidating the profile automatically disarms the system.
    /// </summary>
    public ProfileValidityUpdateResult SetProfileValidity(bool isValid)
    {
        lock (_gate)
        {
            var previousValidity = _profileValid;
            var previousState = _state;

            if (previousValidity == isValid)
            {
                return new ProfileValidityUpdateResult(previousValidity, _profileValid, changed: false, causedDisarm: false, previousState, _state);
            }

            _profileValid = isValid;

            if (!isValid && _state == ArmingState.Armed)
            {
                _state = ArmingState.Disarmed;
                return new ProfileValidityUpdateResult(previousValidity, _profileValid, changed: true, causedDisarm: true, previousState, _state);
            }

            return new ProfileValidityUpdateResult(previousValidity, _profileValid, changed: true, causedDisarm: false, previousState, _state);
        }
    }

    /// <summary>
    /// Determines whether outputs are permitted in the current state.
    /// </summary>
    public OutputGateResult EvaluateOutputs()
    {
        lock (_gate)
        {
            if (_state != ArmingState.Armed)
            {
                return OutputGateResult.Blocked("System is disarmed.");
            }

            if (!_profileValid)
            {
                return OutputGateResult.Blocked("Guidance profile is not valid.");
            }

            return OutputGateResult.Allowed();
        }
    }

    /// <summary>
    /// Throws if outputs are not currently permitted.
    /// </summary>
    public void EnsureCanEmitOutputs()
    {
        var result = EvaluateOutputs();
        if (!result.Allowed)
        {
            throw new InvalidOperationException(result.Reason ?? "Outputs are currently blocked.");
        }
    }
}

/// <summary>
/// Represents the current arming mode for Core output gating.
/// </summary>
public enum ArmingState
{
    Disarmed = 0,
    Armed = 1
}

/// <summary>
/// Outcome of an arming or disarming request.
/// </summary>
public readonly record struct ArmingTransitionResult(
    bool Success,
    bool StateChanged,
    ArmingState PreviousState,
    ArmingState CurrentState,
    string? FailureReason)
{
    public static ArmingTransitionResult Allowed(ArmingState previous, ArmingState current, bool changed)
        => new(true, changed, previous, current, null);

    public static ArmingTransitionResult Blocked(ArmingState previous, string reason)
        => new(false, false, previous, previous, reason ?? throw new ArgumentNullException(nameof(reason)));

    public void EnsureSuccess()
    {
        if (!Success)
        {
            throw new InvalidOperationException(FailureReason ?? "Arming transition failed.");
        }
    }
}

/// <summary>
/// Describes the effect of updating the profile validity flag.
/// </summary>
public readonly record struct ProfileValidityUpdateResult(
    bool PreviousValue,
    bool CurrentValue,
    bool Changed,
    bool CausedDisarm,
    ArmingState PreviousState,
    ArmingState CurrentState)
{
    public bool StateChanged => PreviousState != CurrentState;
}

/// <summary>
/// Represents the result of checking whether outputs are allowed.
/// </summary>
public readonly record struct OutputGateResult(bool Allowed, string? Reason)
{
    public static OutputGateResult Allowed()
        => new(true, null);

    public static OutputGateResult Blocked(string reason)
        => new(false, reason ?? throw new ArgumentNullException(nameof(reason)));
}
