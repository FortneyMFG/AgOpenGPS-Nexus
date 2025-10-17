using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Aog.Core.V1;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aog.Agio.Linux.SocketCan;

/// <summary>
/// Publishes SocketCAN frames to subscribers via an asynchronous channel.
/// </summary>
public sealed class SocketCanFrameChannel : ISocketCanFramePublisher, ISocketCanFrameSource
{
    private readonly ConcurrentDictionary<Guid, Subscriber> _subscribers = new();
    private readonly int _subscriberCapacity;
    private readonly long _maxSubscriberBackpressureTicks;
    private readonly ILogger<SocketCanFrameChannel> _logger;

    public SocketCanFrameChannel(
        int subscriberCapacity = 64,
        TimeSpan? maxSubscriberBackpressure = null,
        ILogger<SocketCanFrameChannel>? logger = null)
    {
        if (subscriberCapacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(subscriberCapacity), subscriberCapacity, "Channel capacity must be positive.");
        }

        var backpressure = maxSubscriberBackpressure ?? TimeSpan.FromSeconds(1);
        if (backpressure <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(maxSubscriberBackpressure), backpressure, "Backpressure tolerance must be positive.");
        }

        _subscriberCapacity = subscriberCapacity;
        _maxSubscriberBackpressureTicks = ToStopwatchTicks(backpressure);
        _logger = logger ?? NullLogger<SocketCanFrameChannel>.Instance;
    }

    /// <inheritdoc />
    public ValueTask PublishAsync(CanFrame frame, CancellationToken cancellationToken)
    {
        if (frame is null)
        {
            throw new ArgumentNullException(nameof(frame));
        }

        cancellationToken.ThrowIfCancellationRequested();

        var nowTicks = Stopwatch.GetTimestamp();

        foreach (var (subscriptionId, subscriber) in _subscribers)
        {
            var writer = subscriber.Channel.Writer;
            if (writer.TryWrite(frame))
            {
                subscriber.ClearBackpressure();
                continue;
            }

            if (!subscriber.ShouldEvict(nowTicks, _maxSubscriberBackpressureTicks, out var backpressureDurationTicks))
            {
                continue;
            }

            if (_subscribers.TryRemove(subscriptionId, out var removed))
            {
                removed.Channel.Writer.TryComplete(new OperationCanceledException("SocketCAN subscriber removed due to sustained backpressure."));
                _logger.LogWarning(
                    "SocketCAN subscriber {SubscriptionId} evicted after {BackpressureDuration} of backpressure while publishing CAN frame {ArbitrationId}.",
                    subscriptionId,
                    ToTimeSpan(backpressureDurationTicks),
                    frame.ArbitrationId);
            }
        }

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<CanFrame> ReadAllAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var subscriptionId = Guid.NewGuid();
        var channel = Channel.CreateBounded<CanFrame>(new BoundedChannelOptions(_subscriberCapacity)
        {
            AllowSynchronousContinuations = false,
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false,
        });

        var subscriber = new Subscriber(channel);

        if (!_subscribers.TryAdd(subscriptionId, subscriber))
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

    private static long ToStopwatchTicks(TimeSpan duration)
    {
        var ticks = (long)Math.Round(duration.TotalSeconds * Stopwatch.Frequency, MidpointRounding.AwayFromZero);
        return Math.Max(1, ticks);
    }

    private static TimeSpan ToTimeSpan(long stopwatchTicks)
    {
        var seconds = (double)stopwatchTicks / Stopwatch.Frequency;
        return TimeSpan.FromSeconds(seconds);
    }

    private sealed class Subscriber
    {
        private long _firstBackpressureTicks;

        public Subscriber(Channel<CanFrame> channel)
        {
            Channel = channel;
        }

        public Channel<CanFrame> Channel { get; }

        public void ClearBackpressure() => Interlocked.Exchange(ref _firstBackpressureTicks, 0);

        public bool ShouldEvict(long nowTicks, long maxBackpressureTicks, out long backpressureDuration)
        {
            var first = Interlocked.CompareExchange(ref _firstBackpressureTicks, nowTicks, 0);
            if (first == 0)
            {
                first = nowTicks;
            }

            backpressureDuration = nowTicks - first;
            return backpressureDuration >= maxBackpressureTicks;
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
