using System.IO;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Aog.Agio.Linux.Gpsd;

/// <summary>
/// Minimal gpsd client that issues a WATCH command and streams TPV reports.
/// </summary>
public sealed class GpsdClient
{
    private const string WatchCommand = "?WATCH={\"enable\":true,\"json\":true}";

    private static readonly Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    private readonly IGpsdConnectionFactory _connectionFactory;
    private readonly ILogger<GpsdClient> _logger;

    public GpsdClient(IGpsdConnectionFactory connectionFactory, ILogger<GpsdClient> logger)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Subscribes to gpsd TPV reports until cancellation or the stream ends.
    /// </summary>
    /// <exception cref="GpsdSocketUnavailableException">Thrown when gpsd is not reachable.</exception>
    public async IAsyncEnumerable<GpsdTpvReport> WatchAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        Stream? connection;
        try
        {
            connection = await _connectionFactory.ConnectAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (GpsdUnavailableException)
        {
            throw;
        }
        catch (SocketException ex) when (ex.SocketErrorCode == SocketError.AccessDenied)
        {
            throw new GpsdUnavailableException(
                $"Failed to connect to gpsd. SocketError: {ex.SocketErrorCode}.",
                ex);
        }
        catch (SocketException ex)
        {
            throw new GpsdSocketUnavailableException("Failed to connect to gpsd.", ex);
        }
        if (connection is null)
        {
            throw new GpsdSocketUnavailableException("gpsd socket is unavailable.");
        }

        await using var stream = connection;

        using var reader = new StreamReader(stream, Utf8NoBom, detectEncodingFromByteOrderMarks: false, bufferSize: 1024, leaveOpen: true);
        using var writer = new StreamWriter(stream, Utf8NoBom, bufferSize: 1024, leaveOpen: true)
        {
            NewLine = "\n",
            AutoFlush = true,
        };

        try
        {
            await writer.WriteLineAsync(WatchCommand, cancellationToken).ConfigureAwait(false);
            await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is IOException or ObjectDisposedException)
        {
            throw new GpsdSocketUnavailableException("Failed to send WATCH command to gpsd.", ex);
        }

        while (!cancellationToken.IsCancellationRequested)
        {
            string? line;
            try
            {
                line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex) when (ex is IOException or ObjectDisposedException)
            {
                _logger.LogDebug(ex, "gpsd stream read failed.");
                yield break;
            }

            if (line is null)
            {
                yield break;
            }

            if (!GpsdTpvReport.TryParse(line, out var report) || report is null)
            {
                continue;
            }

            yield return report;
        }
    }
}
