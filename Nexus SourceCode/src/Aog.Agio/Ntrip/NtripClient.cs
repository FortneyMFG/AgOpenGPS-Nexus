using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Aog.Agio.Ntrip;

/// <summary>
/// Streams RTCM corrections from an NTRIP caster and forwards them to registered sinks.
/// </summary>
public sealed class NtripClient
{
    private static readonly ReadOnlyMemory<byte> HeaderTerminator = new byte[] { (byte)'\r', (byte)'\n', (byte)'\r', (byte)'\n' };

    private readonly NtripClientOptions _options;
    private readonly IReadOnlyList<INtripCorrectionSink> _sinks;
    private readonly ILogger<NtripClient> _logger;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="NtripClient"/> class.
    /// </summary>
    public NtripClient(
        NtripClientOptions options,
        IEnumerable<INtripCorrectionSink> sinks,
        ILogger<NtripClient> logger,
        TimeProvider? timeProvider = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _options.Validate();

        _sinks = sinks is null ? throw new ArgumentNullException(nameof(sinks)) : new List<INtripCorrectionSink>(sinks);
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>
    /// Runs the NTRIP session until cancelled.
    /// </summary>
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting NTRIP client for mountpoint {MountPoint} on {Host}:{Port}.", _options.MountPoint, _options.Host, _options.Port);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var keepAlive = await RunSessionAsync(cancellationToken).ConfigureAwait(false);
                if (!keepAlive)
                {
                    await DelayAsync(_options.ReconnectBackoff, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "NTRIP session terminated unexpectedly. Reconnecting in {Delay}.", _options.ReconnectBackoff);
                await DelayAsync(_options.ReconnectBackoff, cancellationToken).ConfigureAwait(false);
            }
        }

        _logger.LogInformation("NTRIP client for mountpoint {MountPoint} shutting down.", _options.MountPoint);
    }

    private async Task<bool> RunSessionAsync(CancellationToken cancellationToken)
    {
        await using var connection = await ConnectAsync(cancellationToken).ConfigureAwait(false);

        if (!connection.InitialPayload.IsEmpty)
        {
            await PublishAsync(connection.InitialPayload, cancellationToken).ConfigureAwait(false);
        }

        var buffer = ArrayPool<byte>.Shared.Rent(_options.ReceiveBufferSize);
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var read = await connection.Stream.ReadAsync(buffer.AsMemory(0, _options.ReceiveBufferSize), cancellationToken).ConfigureAwait(false);
                if (read == 0)
                {
                    _logger.LogWarning("NTRIP connection closed by caster. Will attempt to reconnect after {Delay}.", _options.ReconnectBackoff);
                    return false;
                }

                await PublishAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }

        return true;
    }

    private async Task PublishAsync(ReadOnlyMemory<byte> payload, CancellationToken cancellationToken)
    {
        if (payload.IsEmpty)
        {
            return;
        }

        if (_sinks.Count == 0)
        {
            _logger.LogDebug("Discarding {Length} bytes of RTCM corrections because no sinks are registered.", payload.Length);
            return;
        }

        foreach (var sink in _sinks)
        {
            await sink.PublishAsync(payload, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<NtripConnection> ConnectAsync(CancellationToken cancellationToken)
    {
        var tcpClient = new TcpClient();
        using var connectCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        connectCts.CancelAfter(_options.ConnectionTimeout);

        try
        {
            _logger.LogDebug("Connecting to NTRIP caster {Host}:{Port}...", _options.Host, _options.Port);

            await tcpClient.ConnectAsync(_options.Host, _options.Port, connectCts.Token).ConfigureAwait(false);
            connectCts.Token.ThrowIfCancellationRequested();

            Stream stream = tcpClient.GetStream();
            if (_options.UseTls)
            {
                var sslStream = new SslStream(stream, leaveInnerStreamOpen: false, ValidateServerCertificate);
                var options = new SslClientAuthenticationOptions
                {
                    EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
                    TargetHost = _options.Host
                };

                await sslStream.AuthenticateAsClientAsync(options, cancellationToken).ConfigureAwait(false);
                stream = sslStream;
            }

            await SendRequestAsync(stream, cancellationToken).ConfigureAwait(false);
            var response = await ReadResponseAsync(stream, cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Established NTRIP session with {Host}:{Port} ({StatusLine}).", _options.Host, _options.Port, response.StatusLine);

            return new NtripConnection(tcpClient, stream, response.InitialPayload);
        }
        catch
        {
            tcpClient.Dispose();
            throw;
        }
    }

    private bool ValidateServerCertificate(object sender, System.Security.Cryptography.X509Certificates.X509Certificate? certificate, System.Security.Cryptography.X509Certificates.X509Chain? chain, SslPolicyErrors sslPolicyErrors)
    {
        if (sslPolicyErrors == SslPolicyErrors.None)
        {
            return true;
        }

        if (_options.AllowInvalidCertificates)
        {
            _logger.LogWarning("Ignoring TLS certificate validation errors: {Errors}.", sslPolicyErrors);
            return true;
        }

        _logger.LogError("TLS certificate validation failed: {Errors}.", sslPolicyErrors);
        return false;
    }

    private async Task SendRequestAsync(Stream stream, CancellationToken cancellationToken)
    {
        var builder = new StringBuilder();
        builder.Append("GET ").Append(_options.GetRequestPath()).Append(" HTTP/1.1\r\n");
        builder.Append("Host: ").Append(_options.Host).Append('\r').Append('\n');
        builder.Append("User-Agent: ").Append(_options.UserAgent).Append('\r').Append('\n');
        builder.Append("Ntrip-Version: Ntrip/2.0\r\n");
        builder.Append("Connection: keep-alive\r\n");
        builder.Append("Accept: */*\r\n");

        if (!string.IsNullOrEmpty(_options.NmeaGgaSentence))
        {
            builder.Append("Ntrip-GGA: ").Append(_options.NmeaGgaSentence).Append('\r').Append('\n');
        }

        if (!string.IsNullOrEmpty(_options.Username))
        {
            var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_options.Username}:{_options.Password}"));
            builder.Append("Authorization: Basic ").Append(credentials).Append('\r').Append('\n');
        }

        builder.Append('\r').Append('\n');
        var requestBytes = Encoding.ASCII.GetBytes(builder.ToString());
        await stream.WriteAsync(requestBytes.AsMemory(0, requestBytes.Length), cancellationToken).ConfigureAwait(false);
        await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<NtripResponse> ReadResponseAsync(Stream stream, CancellationToken cancellationToken)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(4096);
        var headerBuffer = new ArrayBufferWriter<byte>();
        try
        {
            while (true)
            {
                var read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken).ConfigureAwait(false);
                if (read == 0)
                {
                    throw new IOException("NTRIP caster closed the connection before sending a response.");
                }

                headerBuffer.Write(buffer.AsSpan(0, read));
                var span = headerBuffer.WrittenSpan;
                var index = span.IndexOf(HeaderTerminator.Span);
                if (index < 0)
                {
                    if (headerBuffer.WrittenCount > 16384)
                    {
                        throw new InvalidOperationException("NTRIP response headers exceeded allowed size.");
                    }

                    continue;
                }

                var headerSpan = span.Slice(0, index);
                var statusLineEnd = headerSpan.IndexOf("\r\n"u8);
                if (statusLineEnd < 0)
                {
                    throw new InvalidOperationException("Malformed NTRIP response: missing status line terminator.");
                }

                var statusLine = Encoding.ASCII.GetString(headerSpan.Slice(0, statusLineEnd));
                if (!statusLine.Contains(" 200", StringComparison.OrdinalIgnoreCase) && !statusLine.StartsWith("ICY 200", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException($"NTRIP caster rejected request: {statusLine}.");
                }

                var payloadStart = index + HeaderTerminator.Length;
                var remainderLength = headerBuffer.WrittenCount - payloadStart;
                if (remainderLength <= 0)
                {
                    return new NtripResponse(statusLine, ReadOnlyMemory<byte>.Empty);
                }

                var array = headerBuffer.WrittenSpan.Slice(payloadStart, remainderLength).ToArray();
                return new NtripResponse(statusLine, array);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken)
    {
        if (delay <= TimeSpan.Zero)
        {
            return Task.CompletedTask;
        }

        return Task.Delay(delay, _timeProvider, cancellationToken);
    }

    private sealed class NtripConnection : IAsyncDisposable
    {
        private readonly TcpClient _client;
        private readonly Stream _stream;

        public NtripConnection(TcpClient client, Stream stream, ReadOnlyMemory<byte> initialPayload)
        {
            _client = client;
            _stream = stream;
            InitialPayload = initialPayload;
        }

        public Stream Stream => _stream;

        public ReadOnlyMemory<byte> InitialPayload { get; }

        public ValueTask DisposeAsync()
        {
            try
            {
                _stream.Dispose();
            }
            finally
            {
                _client.Dispose();
            }

            return ValueTask.CompletedTask;
        }
    }

    private sealed record NtripResponse(string StatusLine, ReadOnlyMemory<byte> InitialPayload);
}
