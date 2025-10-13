using System;
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

        try
        {
            await foreach (var frame in _source.ReadAllAsync(context.CancellationToken))
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
        catch (OperationCanceledException) when (context.CancellationToken.IsCancellationRequested)
        {
        }
    }
}
