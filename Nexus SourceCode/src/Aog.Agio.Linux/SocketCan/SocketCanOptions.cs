using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;

namespace Aog.Agio.Linux.SocketCan;

/// <summary>
/// Configuration options for the SocketCAN backend.
/// </summary>
public sealed class SocketCanOptions
{
    /// <summary>
    /// Default time to wait before re-attempting a SocketCAN connection after a fault.
    /// </summary>
    public static readonly TimeSpan DefaultReconnectDelay = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Default receive timeout applied to the underlying CAN socket.
    /// </summary>
    public static readonly TimeSpan DefaultReceiveTimeout = TimeSpan.FromMilliseconds(200);

    private string? _interfaceName = "can0";
    private string _sourcePrefix = "linux/socketcan";
    private TimeSpan _reconnectDelay = DefaultReconnectDelay;
    private TimeSpan _receiveTimeout = DefaultReceiveTimeout;

    /// <summary>
    /// Gets or sets the SocketCAN interface name to bind (for example, <c>can0</c> or <c>vcan0</c>).
    /// </summary>
    public string? InterfaceName
    {
        get => _interfaceName;
        set
        {
            if (value is null || value.Length == 0)
            {
                _interfaceName = null;
                return;
            }

            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ValidationException(
                    "Interface name cannot contain only whitespace. Assign null or empty to disable SocketCAN.");
            }

            _interfaceName = value;
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether virtual interfaces (for example vcan) should be considered when resolving
    /// the interface name.
    /// </summary>
    public bool IncludeVirtualInterfaces { get; set; } = true;

    /// <summary>
    /// Gets or sets the time to wait before retrying after a failure to connect or read from the CAN interface.
    /// </summary>
    public TimeSpan ReconnectDelay
    {
        get => _reconnectDelay;
        set
        {
            if (value < TimeSpan.Zero && value != Timeout.InfiniteTimeSpan)
            {
                throw new ValidationException("Reconnect delay cannot be negative.");
            }

            _reconnectDelay = value;
        }
    }

    /// <summary>
    /// Gets or sets the SocketCAN receive timeout. Values less than or equal to zero map to an infinite timeout.
    /// </summary>
    public TimeSpan ReceiveTimeout
    {
        get => _receiveTimeout;
        set
        {
            if (value < TimeSpan.Zero && value != Timeout.InfiniteTimeSpan)
            {
                throw new ValidationException("Receive timeout cannot be negative.");
            }

            _receiveTimeout = value;
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether frames transmitted locally should be echoed back to the reader.
    /// </summary>
    public bool ReceiveOwnMessages { get; set; } = true;

    /// <summary>
    /// Gets or sets the prefix applied to telemetry sources emitted by the backend.
    /// </summary>
    [Required]
    public string SourcePrefix
    {
        get => _sourcePrefix;
        set => _sourcePrefix = string.IsNullOrWhiteSpace(value)
            ? throw new ValidationException("Source prefix is required.")
            : value;
    }

    /// <summary>
    /// Computes the SocketCAN receive timeout value in milliseconds suitable for <see cref="SocketCANSharp.Network.RawCanSocket"/>.
    /// </summary>
    public int GetReceiveTimeoutMilliseconds()
    {
        if (_receiveTimeout == Timeout.InfiniteTimeSpan || _receiveTimeout <= TimeSpan.Zero)
        {
            return 0;
        }

        var milliseconds = (int)Math.Ceiling(_receiveTimeout.TotalMilliseconds);
        return Math.Clamp(milliseconds, 1, int.MaxValue);
    }
}
