using System;
using System.Threading;
using System.Threading.Tasks;
using Aog.Core.Simulation;
using Grpc.Core;

namespace Aog.Agio.Sim;

/// <summary>
/// Helper that forwards messages published on the simulation bus to gRPC server streams.
/// </summary>
internal static class SimBusStreamForwarder
{
    public static Task StreamAsync<TMessage>(
        ISimBus simBus,
        string topic,
        IServerStreamWriter<TMessage> responseStream,
        ServerCallContext context)
    {
        if (simBus is null)
            throw new ArgumentNullException(nameof(simBus));
        if (string.IsNullOrWhiteSpace(topic))
            throw new ArgumentException("Topic name is required.", nameof(topic));
        if (responseStream is null)
            throw new ArgumentNullException(nameof(responseStream));
        if (context is null)
            throw new ArgumentNullException(nameof(context));

        return StreamCoreAsync(simBus, topic, responseStream, context.CancellationToken);
    }

    private static async Task StreamCoreAsync<TMessage>(
        ISimBus simBus,
        string topic,
        IServerStreamWriter<TMessage> responseStream,
        CancellationToken cancellationToken)
    {
        using var subscription = simBus.Subscribe<TMessage>(topic, async (message, _) =>
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            try
            {
                await responseStream.WriteAsync(message.Payload).ConfigureAwait(false);
            }
            catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled)
            {
                // Client cancelled the stream; swallow to allow clean shutdown.
            }
        });

        await WaitForCancellationAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task WaitForCancellationAsync(CancellationToken cancellationToken)
    {
        if (!cancellationToken.CanBeCanceled)
        {
            // Fall back to an indefinite delay that will observe cancellation via exception.
            try
            {
                await Task.Delay(Timeout.Infinite, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Expected when the token is eventually cancelled.
            }

            return;
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        var tcs = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        using (cancellationToken.Register(static state =>
            ((TaskCompletionSource<object?>)state!).TrySetResult(null), tcs))
        {
            await tcs.Task.ConfigureAwait(false);
        }
    }
}
