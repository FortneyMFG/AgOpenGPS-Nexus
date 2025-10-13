namespace Aog.Agio.Legacy;

/// <summary>
/// Captures additional telemetry preserved from the legacy steering feedback PGN.
/// </summary>
public sealed class LegacySteerStateMetadata
{
    /// <summary>
    /// Gets or sets the absolute heading in degrees reported by the legacy module.
    /// </summary>
    public double HeadingDeg { get; init; }

    /// <summary>
    /// Gets or sets the vehicle roll in degrees reported by the legacy module.
    /// </summary>
    public double RollDeg { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether the work switch input is active.
    /// </summary>
    public bool IsWorkSwitchOn { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether the steer switch input is active.
    /// </summary>
    public bool IsSteerSwitchOn { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether the remote switch input is active.
    /// </summary>
    public bool IsRemoteSwitchOn { get; init; }

    /// <summary>
    /// Gets or sets the raw switch bitmap byte from the PGN payload.
    /// </summary>
    public byte SwitchByte { get; init; }

    /// <summary>
    /// Gets or sets the raw PWM byte reported by the legacy module.
    /// </summary>
    public byte RawPwm { get; init; }
}
