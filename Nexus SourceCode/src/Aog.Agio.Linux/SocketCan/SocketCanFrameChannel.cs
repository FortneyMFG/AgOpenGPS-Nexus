using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Aog.Core.V1;

namespace Aog.Agio.Linux.SocketCan;

/// <summary>
/// Publishes SocketCAN frames to subscribers via an asynchronous channel.
/// </summary>
public sealed class SocketCanFrameChannel : ISocketCanFramePublisher, ISocketCanFrameSource
{
    private readonly ConcurrentDictionary<Guid, Channel<CanFrame>> _subscribers = new();

    public SocketCanFrameChannel()
    {
    }

    /// <inheritdoc />
    public async ValueTask PublishAsync(CanFrame frame, CancellationToken cancellationToken)
    {
        if (frame is null)
        {
            throw new ArgumentNullException(nameof(frame));
        }

        foreach (var (subscriptionId, channel) in _subscribers)
        {
            if (channel.Writer.TryWrite(frame))
            {
                continue;
            }

            try
            {
                await channel.Writer.WriteAsync(frame, cancellationToken).ConfigureAwait(false);
            }
            catch (ChannelClosedException)
            {
                _subscribers.TryRemove(subscriptionId, out _);
            }
        }
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<CanFrame> ReadAllAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var subscriptionId = Guid.NewGuid();
        var channel = Channel.CreateUnbounded<CanFrame>(new UnboundedChannelOptions
        {
            AllowSynchronousContinuations = false,
            SingleReader = true,
            SingleWriter = false,
        });

        if (!_subscribers.TryAdd(subscriptionId, channel))
        {
            channel.Writer.TryComplete();
            throw new InvalidOperationException("Failed to register SocketCAN subscriber channel.");
        }

        try
        {
            await foreach (var frame in channel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
            {
                yield return frame;
            }
        }
        finally
        {
            _subscribers.TryRemove(subscriptionId, out _);
            channel.Writer.TryComplete();
        }
    }
}

/// <summary>
/// Publishes translated CAN frames to downstream services.
/// </summary>
public interface ISocketCanFramePublisher
{
    /// <summary>
    /// Publishes a CAN frame to all subscribers.
    /// </summary>
    ValueTask PublishAsync(CanFrame frame, CancellationToken cancellationToken);
}

/// <summary>
/// Exposes an asynchronous stream of translated CAN frames.
/// </summary>
public interface ISocketCanFrameSource
{
    /// <summary>
    /// Reads all frames published to the channel until the supplied token is cancelled.
    /// </summary>
    IAsyncEnumerable<CanFrame> ReadAllAsync(CancellationToken cancellationToken);
}
