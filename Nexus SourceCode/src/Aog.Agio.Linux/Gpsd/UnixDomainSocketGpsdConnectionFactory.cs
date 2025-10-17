using System.IO;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aog.Agio.Linux.Gpsd;

/// <summary>
/// Connects to gpsd via its unix domain socket.
/// </summary>
public sealed class UnixDomainSocketGpsdConnectionFactory : IGpsdConnectionFactory
{
    private readonly ILogger<UnixDomainSocketGpsdConnectionFactory> _logger;
    private readonly IOptionsMonitor<GpsdClientOptions> _options;

    public UnixDomainSocketGpsdConnectionFactory(
        ILogger<UnixDomainSocketGpsdConnectionFactory> logger,
        IOptionsMonitor<GpsdClientOptions> options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _ = options.CurrentValue ?? throw new ArgumentException("Options are required.", nameof(options));
    }

    /// <inheritdoc />
    public async Task<Stream?> ConnectAsync(CancellationToken cancellationToken)
    {
        var socketPath = _options.CurrentValue?.SocketPath;
        if (string.IsNullOrEmpty(socketPath))
        {
            _logger.LogDebug("gpsd socket path not configured; skipping connection attempt.");
            return null;
        }

        var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        try
        {
            await socket.ConnectAsync(new UnixDomainSocketEndPoint(socketPath), cancellationToken).ConfigureAwait(false);
            return new NetworkStream(socket, ownsSocket: true);
        }
        catch (SocketException ex) when (ex.SocketErrorCode == SocketError.AccessDenied)
        {
            socket.Dispose();
            throw new GpsdUnavailableException(
                $"Failed to connect to gpsd socket '{socketPath}'. SocketError: {ex.SocketErrorCode}.",
                ex);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to connect to gpsd socket {SocketPath}.", socketPath);
            socket.Dispose();
            return null;
        }
    }
}
