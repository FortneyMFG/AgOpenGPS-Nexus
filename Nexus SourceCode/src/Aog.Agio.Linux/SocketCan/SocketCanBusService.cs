using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Aog.Core.V1;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace Aog.Agio.Linux.SocketCan;

/// <summary>
/// gRPC service that streams frames received via SocketCAN to subscribers.
/// </summary>
public sealed class SocketCanBusService : CanBusService.CanBusServiceBase
{
    private const int SubscriberQueueCapacity = 32;
    private static readonly TimeSpan SlowSubscriberTimeout = TimeSpan.FromSeconds(1);

    private readonly ISocketCanFrameSource _source;
    private readonly ILogger<SocketCanBusService> _logger;

    public SocketCanBusService(ISocketCanFrameSource source, ILogger<SocketCanBusService> logger)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public override async Task SubscribeFrames(Empty request, IServerStreamWriter<CanFrame> responseStream, ServerCallContext context)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (responseStream is null)
        {
            throw new ArgumentNullException(nameof(responseStream));
        }

        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        var cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(context.CancellationToken);

        try
        {
            var queue = Channel.CreateBounded<CanFrame>(new BoundedChannelOptions(SubscriberQueueCapacity)
            {
                AllowSynchronousContinuations = false,
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = true,
            });

            var pumpTask = PumpFramesAsync(queue, cancellationSource, context);
            var writeTask = WriteFramesAsync(queue, responseStream, cancellationSource);

            try
            {
                await Task.WhenAll(pumpTask, writeTask).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationSource.IsCancellationRequested)
            {
            }
            finally
            {
                queue.Writer.TryComplete();
            }
        }
        finally
        {
            cancellationSource.Dispose();
        }
    }

    private async Task PumpFramesAsync(Channel<CanFrame> queue, CancellationTokenSource cancellationSource, ServerCallContext context)
    {
        try
        {
            await foreach (var frame in _source.ReadAllAsync(cancellationSource.Token).ConfigureAwait(false))
            {
                if (queue.Writer.TryWrite(frame))
                {
                    continue;
                }

                var waitTask = queue.Writer.WaitToWriteAsync(cancellationSource.Token).AsTask();
                var completed = await Task.WhenAny(waitTask, Task.Delay(SlowSubscriberTimeout)).ConfigureAwait(false);

                if (completed != waitTask)
                {
                    LogSlowSubscriberEvicted(context, frame, "Writer wait timed out");
                    cancellationSource.Cancel();
                    break;
                }

                bool canWrite;
                try
                {
                    canWrite = await waitTask.ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cancellationSource.IsCancellationRequested)
                {
                    break;
                }

                if (!canWrite)
                {
                    LogSlowSubscriberEvicted(context, frame, "Writer channel completed while waiting to write");
                    cancellationSource.Cancel();
                    break;
                }

                if (!queue.Writer.TryWrite(frame))
                {
                    LogSlowSubscriberEvicted(context, frame, "Writer remained saturated after wait");
                    cancellationSource.Cancel();
                    break;
                }
            }
        }
        catch (OperationCanceledException) when (cancellationSource.IsCancellationRequested)
        {
        }
        finally
        {
            queue.Writer.TryComplete();
        }
    }

    private void LogSlowSubscriberEvicted(ServerCallContext context, CanFrame frame, string reason)
    {
        _logger.LogWarning(
            "SocketCAN subscriber {Peer} evicted due to slow consumption ({Reason}). Last attempted frame arbitration ID {ArbitrationId}.",
            context.Peer,
            reason,
            frame.ArbitrationId);
    }

    private static async Task WriteFramesAsync(
        Channel<CanFrame> queue,
        IServerStreamWriter<CanFrame> responseStream,
        CancellationTokenSource cancellationSource)
    {
        try
        {
            await foreach (var frame in queue.Reader.ReadAllAsync(cancellationSource.Token).ConfigureAwait(false))
            {
                try
                {
                    await responseStream.WriteAsync(frame).ConfigureAwait(false);
                }
                catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled)
                {
                    break;
                }
            }
        }
        catch (OperationCanceledException) when (cancellationSource.IsCancellationRequested)
        {
        }
        finally
        {
            cancellationSource.Cancel();
        }
    }
}
