using System;

namespace Aog.Agio.Linux;

/// <summary>
/// Options that control the SocketCAN capture backend.
/// </summary>
public sealed class SocketCanOptions
{
    /// <summary>
    /// Gets or sets the SocketCAN interface name (for example, <c>can0</c> or <c>vcan0</c>).
    /// </summary>
    public string InterfaceName { get; set; } = "can0";

    /// <summary>
    /// Gets or sets the polling interval used when the CAN socket is idle.
    /// </summary>
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromMilliseconds(5);
}
