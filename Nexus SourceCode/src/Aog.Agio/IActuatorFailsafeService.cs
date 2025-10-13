using System;
using Aog.Core.V1;

namespace Aog.Agio;

/// <summary>
/// Coordinates heartbeat tracking between plugins and AGiO and enforces failsafe behaviour.
/// </summary>
public interface IActuatorFailsafeService
{
    /// <summary>
    /// Gets a value indicating whether a valid heartbeat has been observed within the timeout window.
    /// </summary>
    bool HasActiveHeartbeat { get; }

    /// <summary>
    /// Gets the last heartbeat timestamp observed by the watchdog.
    /// </summary>
    DateTimeOffset? LastHeartbeatUtc { get; }

    /// <summary>
    /// Gets the current heartbeat timeout.
    /// </summary>
    TimeSpan HeartbeatTimeout { get; }

    /// <summary>
    /// Records an inbound heartbeat from plugins or higher level controllers.
    /// </summary>
    void ReportHeartbeat();

    /// <summary>
    /// Clears any remembered heartbeat state, immediately forcing failsafe behaviour.
    /// </summary>
    void ClearHeartbeat();

    /// <summary>
    /// Applies failsafe rules to a steering command.
    /// </summary>
    /// <param name="command">The command to evaluate.</param>
    /// <returns>The command that should be forwarded to hardware.</returns>
    SteerCmd FilterSteerCommand(SteerCmd command);

    /// <summary>
    /// Applies failsafe rules to a section mask command.
    /// </summary>
    /// <param name="mask">The mask to evaluate.</param>
    /// <returns>The mask that should be forwarded to hardware.</returns>
    SectionMask FilterSectionMask(SectionMask mask);
}
