using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Aog.Agio.Linux.Gpsd;

/// <summary>
/// Minimal gpsd client that issues a WATCH command and streams TPV reports.
/// </summary>
public sealed class GpsdClient
{
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
        await using var stream = await _connectionFactory.ConnectAsync(cancellationToken).ConfigureAwait(false);
        if (stream is null)
        {
            throw new GpsdSocketUnavailableException("gpsd socket is unavailable.");
        }

        using var reader = new StreamReader(stream, Utf8NoBom, detectEncodingFromByteOrderMarks: false, bufferSize: 1024, leaveOpen: true);
        using var writer = new StreamWriter(stream, Utf8NoBom, bufferSize: 1024, leaveOpen: true)
        {
            NewLine = "\n",
            AutoFlush = true,
        };

        try
        {
            await writer.WriteLineAsync("?WATCH={\"enable\":true,\"json\":true}", cancellationToken).ConfigureAwait(false);
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
