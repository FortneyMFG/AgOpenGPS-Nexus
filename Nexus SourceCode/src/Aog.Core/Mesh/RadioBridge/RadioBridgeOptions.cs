using System;

namespace Aog.Core.Mesh.RadioBridge;

/// <summary>
/// Options that govern the behaviour of the radio bridge transport stack.
/// </summary>
public sealed record RadioBridgeOptions
{
    /// <summary>
    /// Gets the unique identifier of the bridge device within the mesh.
    /// </summary>
    public required string DeviceId { get; init; }

    /// <summary>
    /// Gets or sets the maximum number of in-flight frames awaiting acknowledgement.
    /// Defaults to 32 to match the replay window described in ADR-048.
    /// </summary>
    public int ReplayWindow { get; init; } = 32;

    /// <summary>
    /// Gets or sets the base retry interval used for retransmissions when no link
    /// quality signal is available.
    /// </summary>
    public TimeSpan BaseRetryInterval { get; init; } = TimeSpan.FromMilliseconds(250);

    /// <summary>
    /// Gets or sets the maximum back-off interval allowed for retransmissions.
    /// </summary>
    public TimeSpan MaxRetryInterval { get; init; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Gets or sets the maximum number of retransmission attempts before declaring
    /// delivery failure.
    /// </summary>
    public int MaxRetransmissions { get; init; } = 5;
}
