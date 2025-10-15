using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Aog.Link.V1;
using Google.Protobuf;
using Microsoft.Extensions.Logging;

namespace Aog.Bridge.Host.AogLink.Serial;

/// <summary>
/// Implements the RS-485/serial transport for AOG-Link using COBS framing and CRC-16.
/// </summary>
public sealed class SerialAogLinkTransport : Aog.Bridge.Host.AogLink.Transports.IAogLinkTransport
{
    private readonly Stream _stream;
    private readonly bool _leaveOpen;
    private readonly ILogger<SerialAogLinkTransport> _logger;
    private readonly Channel<LinkEnvelope> _inbound;
    private readonly CancellationTokenSource _cts = new();
    private readonly ConcurrentBag<Task> _backgroundTasks = new();

    public SerialAogLinkTransport(Stream stream, ILogger<SerialAogLinkTransport> logger, bool leaveOpen = false)
    {
        _stream = stream ?? throw new ArgumentNullException(nameof(stream));
        if (!stream.CanRead || !stream.CanWrite)
        {
            throw new ArgumentException("Stream must support reading and writing.", nameof(stream));
        }

        _leaveOpen = leaveOpen;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _inbound = Channel.CreateUnbounded<LinkEnvelope>(new UnboundedChannelOptions
        {
            AllowSynchronousContinuations = false,
            SingleReader = false,
            SingleWriter = false,
        });
    }

    public ValueTask StartAsync(CancellationToken cancellationToken)
    {
        _backgroundTasks.Add(Task.Run(() => ReadLoopAsync(_cts.Token), cancellationToken));
        return ValueTask.CompletedTask;
    }

    public async ValueTask StopAsync(CancellationToken cancellationToken)
    {
        _cts.Cancel();

        while (_backgroundTasks.TryTake(out var task))
        {
            try
            {
                await task.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
        }

        if (!_leaveOpen)
        {
            await _stream.DisposeAsync().ConfigureAwait(false);
        }
    }

    public async ValueTask SendAsync(LinkEnvelope envelope, CancellationToken cancellationToken = default)
    {
        if (envelope is null)
            throw new ArgumentNullException(nameof(envelope));

        var payload = envelope.ToByteArray();
        var encoded = AogLinkSerialCodec.Encode(payload);

        await _stream.WriteAsync(encoded, cancellationToken).ConfigureAwait(false);
        await _stream.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    public async IAsyncEnumerable<LinkEnvelope> ReadAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        while (await _inbound.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
        {
            while (_inbound.Reader.TryRead(out var envelope))
            {
                yield return envelope;
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync(CancellationToken.None);
        _cts.Dispose();
        GC.SuppressFinalize(this);
    }

    private async Task ReadLoopAsync(CancellationToken cancellationToken)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(1024);
        try
        {
            var frameBuffer = new List<byte>(256);

            while (!cancellationToken.IsCancellationRequested)
            {
                int read;
                try
                {
                    read = await _stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                if (read == 0)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(10), cancellationToken).ConfigureAwait(false);
                    continue;
                }

                for (var i = 0; i < read; i++)
                {
                    var value = buffer[i];
                    frameBuffer.Add(value);

                    if (value != 0)
                    {
                        continue;
                    }

                    var frame = frameBuffer.ToArray();
                    frameBuffer.Clear();

                    if (!AogLinkSerialCodec.TryDecode(frame, out var payload))
                    {
                        _logger.LogWarning("Discarding invalid serial frame (CRC mismatch).");
                        continue;
                    }

                    try
                    {
                        var envelope = LinkEnvelope.Parser.ParseFrom(payload);
                        await _inbound.Writer.WriteAsync(envelope, cancellationToken).ConfigureAwait(false);
                    }
                    catch (InvalidProtocolBufferException ex)
                    {
                        _logger.LogWarning(ex, "Failed to decode serial frame into LinkEnvelope.");
                    }
                }
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}
