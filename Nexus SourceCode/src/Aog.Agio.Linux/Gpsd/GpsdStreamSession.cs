using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Aog.Agio.Linux.Gpsd;

/// <summary>
/// Wraps a gpsd network stream with reader/writer conveniences.
/// </summary>
internal sealed class GpsdStreamSession : IAsyncDisposable
{
    private static readonly Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    private readonly Stream _stream;
    private readonly StreamReader _reader;
    private readonly StreamWriter _writer;
    private readonly ILogger _logger;

    public GpsdStreamSession(Stream stream, ILogger logger)
    {
        _stream = stream ?? throw new ArgumentNullException(nameof(stream));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _reader = new StreamReader(stream, Utf8NoBom, detectEncodingFromByteOrderMarks: false, bufferSize: 1024, leaveOpen: true);
        _writer = new StreamWriter(stream, Utf8NoBom, bufferSize: 1024, leaveOpen: true)
        {
            NewLine = "\n",
            AutoFlush = true,
        };
    }

    public async Task SendAsync(string command, CancellationToken cancellationToken)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        _logger.LogDebug("Sending gpsd command: {Command}", command);
        await _writer.WriteLineAsync(command, cancellationToken).ConfigureAwait(false);
        await _writer.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    public ValueTask<string?> ReadLineAsync(CancellationToken cancellationToken)
    {
        return _reader.ReadLineAsync(cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await _writer.FlushAsync().ConfigureAwait(false);
        }
        catch
        {
            // ignore flush errors during dispose
        }

        _writer.Dispose();
        _reader.Dispose();
        await _stream.DisposeAsync().ConfigureAwait(false);
    }
}
