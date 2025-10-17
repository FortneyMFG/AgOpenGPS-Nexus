using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Aog.Core.V1;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

namespace Aog.Agio.Linux.SocketCan;

/// <summary>
/// gRPC service that streams frames received via SocketCAN to subscribers.
/// </summary>
public sealed class SocketCanBusService : CanBusService.CanBusServiceBase
{
    private const int SubscriberQueueCapacity = 32;
    private static readonly TimeSpan SlowSubscriberTimeout = TimeSpan.FromSeconds(1);

    private readonly ISocketCanFrameSource _source;

    public SocketCanBusService(ISocketCanFrameSource source)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
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

        using var cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(context.CancellationToken);
        var queue = Channel.CreateBounded<CanFrame>(new BoundedChannelOptions(SubscriberQueueCapacity)
        {
            AllowSynchronousContinuations = false,
            FullMode = BoundedChannelFullMode.DropWrite,
            SingleReader = true,
            SingleWriter = true,
        });

        var pumpTask = PumpFramesAsync(queue, cancellationSource);
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

    private async Task PumpFramesAsync(Channel<CanFrame> queue, CancellationTokenSource cancellationSource)
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
                    break;
                }

                if (!queue.Writer.TryWrite(frame))
                {
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
