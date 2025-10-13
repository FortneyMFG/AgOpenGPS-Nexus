using System;
using System.ComponentModel.DataAnnotations;

namespace Aog.Agio;

/// <summary>
/// Configures the heartbeat and failsafe behaviour enforced between AGiO and plugins.
/// </summary>
public sealed class AgioSafetyOptions
{
    /// <summary>
    /// Minimum allowed heartbeat interval in milliseconds.
    /// </summary>
    public const int MinimumHeartbeatMs = 100;

    /// <summary>
    /// Maximum allowed heartbeat interval in milliseconds.
    /// </summary>
    public const int MaximumHeartbeatMs = 60_000;

    /// <summary>
    /// Gets or sets the heartbeat interval required to keep actuators enabled.
    /// </summary>
    [Range(MinimumHeartbeatMs, MaximumHeartbeatMs)]
    public int HeartbeatMs { get; set; } = 250;

    /// <summary>
    /// Gets or sets the configured failsafe policy.
    /// </summary>
    [Required]
    public FailsafeSettings Failsafe { get; set; } = new();

    /// <summary>
    /// Gets the configured heartbeat timeout as a <see cref="TimeSpan"/>.
    /// </summary>
    public TimeSpan HeartbeatTimeout => TimeSpan.FromMilliseconds(HeartbeatMs);

    /// <summary>
    /// Supported failsafe actions.
    /// </summary>
    public enum FailsafeAction
    {
        /// <summary>
        /// Disable the corresponding actuator output when the watchdog expires.
        /// </summary>
        Disable,

        /// <summary>
        /// Hold the last known-good command when the watchdog expires.
        /// </summary>
        Hold,
    }

    /// <summary>
    /// Describes failsafe policies applied to individual actuator classes.
    /// </summary>
    public sealed class FailsafeSettings
    {
        /// <summary>
        /// Gets or sets the steering failsafe policy.
        /// </summary>
        public FailsafeAction Steer { get; set; } = FailsafeAction.Disable;

        /// <summary>
        /// Gets or sets the sections failsafe policy.
        /// </summary>
        public FailsafeAction Sections { get; set; } = FailsafeAction.Disable;
    }
}
