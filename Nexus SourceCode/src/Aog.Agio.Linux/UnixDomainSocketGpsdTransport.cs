using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aog.Agio.Linux;

/// <summary>
/// Connects to gpsd over a Unix domain socket.
/// </summary>
public sealed class UnixDomainSocketGpsdTransport : IGpsdTransport
{
    private readonly GpsdClientOptions _options;
    private readonly ILogger<UnixDomainSocketGpsdTransport> _logger;

    public UnixDomainSocketGpsdTransport(IOptions<GpsdClientOptions> options, ILogger<UnixDomainSocketGpsdTransport> logger)
    {
        _options = (options ?? throw new ArgumentNullException(nameof(options))).Value ?? throw new ArgumentException("Options are required.", nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IGpsdSession> ConnectAsync(CancellationToken cancellationToken)
    {
        var socketPath = _options.SocketPath;
        if (string.IsNullOrWhiteSpace(socketPath))
        {
            throw new InvalidOperationException("gpsd socket path must be configured.");
        }

        if (!File.Exists(socketPath))
        {
            throw new GpsdUnavailableException($"gpsd socket '{socketPath}' does not exist.");
        }

        var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);

        try
        {
            var endpoint = new UnixDomainSocketEndPoint(socketPath);
            await socket.ConnectAsync(endpoint, cancellationToken).ConfigureAwait(false);
        }
        catch (SocketException ex)
        {
            socket.Dispose();
            throw new GpsdUnavailableException($"Failed to connect to gpsd socket '{socketPath}'.", ex);
        }

        var stream = new NetworkStream(socket, ownsSocket: true);
        var reader = new StreamReader(stream, Encoding.ASCII, detectEncodingFromByteOrderMarks: false, bufferSize: 1024, leaveOpen: true);
        var writer = new StreamWriter(stream, Encoding.ASCII, bufferSize: 1024, leaveOpen: true)
        {
            NewLine = "\n",
            AutoFlush = true,
        };

        _logger.LogInformation("Connected to gpsd at {SocketPath}.", socketPath);
        return new GpsdStreamSession(stream, reader, writer, _logger);
    }

    private sealed class GpsdStreamSession : IGpsdSession
    {
        private readonly Stream _stream;
        private readonly StreamReader _reader;
        private readonly StreamWriter _writer;
        private readonly ILogger _logger;

        public GpsdStreamSession(Stream stream, StreamReader reader, StreamWriter writer, ILogger logger)
        {
            _stream = stream;
            _reader = reader;
            _writer = writer;
            _logger = logger;
        }

        public async Task SendAsync(string command, CancellationToken cancellationToken)
        {
            if (command is null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            _logger.LogDebug("Sending gpsd command: {Command}", command);
            await _writer.WriteLineAsync(command).ConfigureAwait(false);
            await _writer.FlushAsync().ConfigureAwait(false);
        }

        public async IAsyncEnumerable<string> ReadLinesAsync([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                string? line;
                try
                {
                    line = await _reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    yield break;
                }

                if (line is null)
                {
                    yield break;
                }

                yield return line;
            }
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                await _writer.FlushAsync().ConfigureAwait(false);
            }
            catch
            {
                // Swallow flush exceptions during dispose.
            }

            _writer.Dispose();
            _reader.Dispose();
            await _stream.DisposeAsync().ConfigureAwait(false);
        }
    }
}
