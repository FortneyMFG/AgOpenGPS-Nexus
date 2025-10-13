using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Aog.Core.V1;
using Aog.Protos.Agio.V1;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

namespace Aog.Agio.Linux;

/// <summary>
/// gRPC service that streams SocketCAN frames to subscribers and accepts published frames from the listener.
/// </summary>
public sealed class SocketCanBusService : CanBusService.CanBusServiceBase
{
    private readonly object _sync = new();
    private readonly List<Channel<CanFrame>> _channels = new();

    public void Publish(CanFrame frame)
    {
        if (frame is null)
        {
            throw new ArgumentNullException(nameof(frame));
        }

        Channel<CanFrame>[] snapshot;
        lock (_sync)
        {
            snapshot = _channels.ToArray();
        }

        var isFirst = true;
        foreach (var channel in snapshot)
        {
            if (!channel.Writer.TryWrite(isFirst ? frame : frame.Clone()))
            {
                RemoveChannel(channel);
            }
            isFirst = false;
        }
    }

    public override async Task SubscribeFrames(Empty request, IServerStreamWriter<CanFrame> responseStream, ServerCallContext context)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));
        if (responseStream is null)
            throw new ArgumentNullException(nameof(responseStream));
        if (context is null)
            throw new ArgumentNullException(nameof(context));

        var channel = Channel.CreateUnbounded<CanFrame>(new UnboundedChannelOptions
        {
            AllowSynchronousContinuations = false,
            SingleReader = true,
            SingleWriter = false,
        });

        lock (_sync)
        {
            _channels.Add(channel);
        }

        try
        {
            await foreach (var frame in channel.Reader.ReadAllAsync(context.CancellationToken).ConfigureAwait(false))
            {
                await responseStream.WriteAsync(frame).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (context.CancellationToken.IsCancellationRequested)
        {
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled)
        {
        }
        finally
        {
            RemoveChannel(channel);
        }
    }

    private void RemoveChannel(Channel<CanFrame> channel)
    {
        lock (_sync)
        {
            _channels.Remove(channel);
        }

        channel.Writer.TryComplete();
    }
}
