using System.Threading.Channels;
using Aog.Core.V1;

namespace Aog.Agio.Linux.SocketCan;

/// <summary>
/// Publishes SocketCAN frames to subscribers via an asynchronous channel.
/// </summary>
public sealed class SocketCanFrameChannel : ISocketCanFramePublisher, ISocketCanFrameSource
{
    private readonly Channel<CanFrame> _channel;

    public SocketCanFrameChannel()
    {
        _channel = Channel.CreateUnbounded<CanFrame>(new UnboundedChannelOptions
        {
            AllowSynchronousContinuations = false,
            SingleReader = false,
            SingleWriter = true,
        });
    }

    /// <inheritdoc />
    public ValueTask PublishAsync(CanFrame frame, CancellationToken cancellationToken)
    {
        if (frame is null)
        {
            throw new ArgumentNullException(nameof(frame));
        }

        if (_channel.Writer.TryWrite(frame))
        {
            return ValueTask.CompletedTask;
        }

        return _channel.Writer.WriteAsync(frame, cancellationToken);
    }

    /// <inheritdoc />
    public IAsyncEnumerable<CanFrame> ReadAllAsync(CancellationToken cancellationToken)
    {
        return _channel.Reader.ReadAllAsync(cancellationToken);
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
