using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using SocketCANSharp;
using SocketCANSharp.Network;

namespace Aog.Agio.Linux.SocketCan;

/// <summary>
/// Factory responsible for creating SocketCAN clients bound to a specific interface.
/// </summary>
public interface ISocketCanClientFactory
{
    /// <summary>
    /// Creates a client bound to the configured SocketCAN interface.
    /// </summary>
    ValueTask<ISocketCanClient> CreateAsync(SocketCanOptions options, CancellationToken cancellationToken);
}

/// <summary>
/// Exposes SocketCAN network interfaces to allow dependency injection in tests.
/// </summary>
public interface ICanNetworkInterfaceProvider
{
    /// <summary>
    /// Returns all interfaces visible to the host.
    /// </summary>
    IReadOnlyList<CanNetworkInterface> GetAll(bool includeVirtualInterfaces);
}

/// <summary>
/// Default implementation that discovers interfaces via <see cref="CanNetworkInterface.GetAllInterfaces(bool)"/>.
/// </summary>
public sealed class SocketCanNetworkInterfaceProvider : ICanNetworkInterfaceProvider
{
    /// <inheritdoc />
    public IReadOnlyList<CanNetworkInterface> GetAll(bool includeVirtualInterfaces)
    {
        return CanNetworkInterface
            .GetAllInterfaces(includeVirtualInterfaces)
            .ToList();
    }
}

/// <summary>
/// Creates SocketCAN clients using <see cref="SocketCANSharp"/> primitives.
/// </summary>
public sealed class SocketCanClientFactory : ISocketCanClientFactory
{
    private readonly ICanNetworkInterfaceProvider _interfaceProvider;

    public SocketCanClientFactory(ICanNetworkInterfaceProvider interfaceProvider)
    {
        _interfaceProvider = interfaceProvider ?? throw new ArgumentNullException(nameof(interfaceProvider));
    }

    /// <inheritdoc />
    public ValueTask<ISocketCanClient> CreateAsync(SocketCanOptions options, CancellationToken cancellationToken)
    {
        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        cancellationToken.ThrowIfCancellationRequested();

        var interfaceName = options.InterfaceName;
        if (string.IsNullOrWhiteSpace(interfaceName))
        {
            throw new InvalidOperationException("SocketCAN interface is not configured.");
        }

        var interfaces = _interfaceProvider.GetAll(options.IncludeVirtualInterfaces);
        var selected = interfaces.FirstOrDefault(iface => string.Equals(
            iface?.Name,
            interfaceName,
            StringComparison.OrdinalIgnoreCase));

        if (selected is null)
        {
            throw new SocketCanInterfaceNotFoundException(interfaceName);
        }

        var socket = new RawCanSocket();
        try
        {
            socket.ReceiveOwnMessages = options.ReceiveOwnMessages;
            socket.ReceiveTimeout = options.GetReceiveTimeoutMilliseconds();
            socket.Bind(selected);
            return ValueTask.FromResult<ISocketCanClient>(new SocketCanClient(socket, selected.Name));
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    }
}

/// <summary>
/// Represents a connected SocketCAN client capable of reading frames from the network.
/// </summary>
public interface ISocketCanClient : IDisposable
{
    /// <summary>
    /// Gets the interface name for logging and telemetry.
    /// </summary>
    string InterfaceName { get; }

    /// <summary>
    /// Reads a frame from the CAN socket.
    /// </summary>
    SocketCanFrameReadResult ReadFrame(CancellationToken cancellationToken);
}

/// <summary>
/// Encapsulates the outcome of a socket read operation.
/// </summary>
public readonly struct SocketCanFrameReadResult
{
    private SocketCanFrameReadResult(SocketCanFrameReadStatus status, CanFrame frame)
    {
        Status = status;
        Frame = frame;
    }

    /// <summary>
    /// Gets the read status.
    /// </summary>
    public SocketCanFrameReadStatus Status { get; }

    /// <summary>
    /// Gets the frame that was read when <see cref="Status"/> equals <see cref="SocketCanFrameReadStatus.Frame"/>.
    /// </summary>
    public CanFrame Frame { get; }

    /// <summary>
    /// Creates a result representing a successfully read CAN frame.
    /// </summary>
    public static SocketCanFrameReadResult FromFrame(CanFrame frame) => new(SocketCanFrameReadStatus.Frame, frame);

    /// <summary>
    /// Creates a result representing a timeout.
    /// </summary>
    public static SocketCanFrameReadResult Timeout() => new(SocketCanFrameReadStatus.Timeout, default);
}

/// <summary>
/// Enumerates possible read outcomes for the SocketCAN client.
/// </summary>
public enum SocketCanFrameReadStatus
{
    /// <summary>
    /// A frame was successfully read from the socket.
    /// </summary>
    Frame,

    /// <summary>
    /// The read attempt timed out with no frame available.
    /// </summary>
    Timeout,
}

internal sealed class SocketCanClient : ISocketCanClient
{
    private readonly RawCanSocket _socket;

    public SocketCanClient(RawCanSocket socket, string interfaceName)
    {
        _socket = socket ?? throw new ArgumentNullException(nameof(socket));
        InterfaceName = interfaceName ?? throw new ArgumentNullException(nameof(interfaceName));
    }

    /// <inheritdoc />
    public string InterfaceName { get; }

    /// <inheritdoc />
    public SocketCanFrameReadResult ReadFrame(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var frame = default(CanFrame);
            var bytesRead = _socket.Read(ref frame);
            if (bytesRead <= 0)
            {
                return SocketCanFrameReadResult.Timeout();
            }

            return SocketCanFrameReadResult.FromFrame(frame);
        }
        catch (SocketCanException ex) when (IsTransient(ex))
        {
            return SocketCanFrameReadResult.Timeout();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _socket.Dispose();
    }

    private static bool IsTransient(SocketCanException exception)
    {
        return exception.SocketErrorCode is SocketError.TimedOut
            or SocketError.WouldBlock
            or SocketError.Interrupted;
    }
}

/// <summary>
/// Exception thrown when the requested SocketCAN interface cannot be resolved.
/// </summary>
public sealed class SocketCanInterfaceNotFoundException : Exception
{
    public SocketCanInterfaceNotFoundException(string interfaceName)
        : base($"SocketCAN interface '{interfaceName}' was not found.")
    {
        InterfaceName = interfaceName;
    }

    /// <summary>
    /// Gets the unresolved interface name.
    /// </summary>
    public string InterfaceName { get; }
}
