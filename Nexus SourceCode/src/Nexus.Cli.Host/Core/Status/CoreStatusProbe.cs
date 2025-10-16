using System.IO.Pipes;
using System.Net.Sockets;
using Nexus.Cli.Host.Core.Endpoints;

namespace Nexus.Cli.Host.Core.Status;

public sealed class CoreStatusProbe : ICoreStatusProbe
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromMilliseconds(500);
    private readonly TimeProvider _timeProvider;

    public CoreStatusProbe(TimeProvider? timeProvider = null)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public Task<CoreStatusResult> CheckAsync(CoreEndpoint endpoint, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(endpoint);

        return endpoint.Transport switch
        {
            CoreTransportKind.NamedPipe => CheckNamedPipeAsync(endpoint, cancellationToken),
            CoreTransportKind.UnixDomainSocket => CheckUnixDomainSocketAsync(endpoint, cancellationToken),
            CoreTransportKind.Tcp => CheckTcpAsync(endpoint, cancellationToken),
            _ => Task.FromResult(CreateResult(endpoint, CoreStatusState.Unknown, null, "Unsupported transport.")),
        };
    }

    private async Task<CoreStatusResult> CheckNamedPipeAsync(CoreEndpoint endpoint, CancellationToken cancellationToken)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(DefaultTimeout);
        var start = _timeProvider.GetTimestamp();

        try
        {
            using var stream = new NamedPipeClientStream(".", endpoint.Address, PipeDirection.InOut, PipeOptions.Asynchronous);
            await stream.ConnectAsync(cts.Token).ConfigureAwait(false);
            stream.ReadTimeout = (int)DefaultTimeout.TotalMilliseconds;
            stream.WriteTimeout = (int)DefaultTimeout.TotalMilliseconds;
            return CreateResult(endpoint, CoreStatusState.Healthy, _timeProvider.GetElapsedTime(start), "Connected to named pipe.");
        }
        catch (OperationCanceledException)
        {
            return CreateResult(endpoint, CoreStatusState.Unavailable, _timeProvider.GetElapsedTime(start), "Timed out connecting to named pipe.");
        }
        catch (Exception ex)
        {
            return CreateResult(endpoint, CoreStatusState.Unavailable, _timeProvider.GetElapsedTime(start), ex.Message);
        }
    }

    private async Task<CoreStatusResult> CheckUnixDomainSocketAsync(CoreEndpoint endpoint, CancellationToken cancellationToken)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(DefaultTimeout);
        var start = _timeProvider.GetTimestamp();

        try
        {
            using var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
            var endPoint = new UnixDomainSocketEndPoint(endpoint.Address);
            await socket.ConnectAsync(endPoint, cts.Token).ConfigureAwait(false);
            return CreateResult(endpoint, CoreStatusState.Healthy, _timeProvider.GetElapsedTime(start), "Connected to unix domain socket.");
        }
        catch (OperationCanceledException)
        {
            return CreateResult(endpoint, CoreStatusState.Unavailable, _timeProvider.GetElapsedTime(start), "Timed out connecting to unix domain socket.");
        }
        catch (SocketException ex)
        {
            return CreateResult(endpoint, CoreStatusState.Unavailable, _timeProvider.GetElapsedTime(start), ex.Message);
        }
        catch (Exception ex)
        {
            return CreateResult(endpoint, CoreStatusState.Unavailable, _timeProvider.GetElapsedTime(start), ex.Message);
        }
    }

    private async Task<CoreStatusResult> CheckTcpAsync(CoreEndpoint endpoint, CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(endpoint.Address, UriKind.Absolute, out var uri))
        {
            return CreateResult(endpoint, CoreStatusState.Unavailable, null, "Invalid TCP endpoint URI.");
        }

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(DefaultTimeout);
        var start = _timeProvider.GetTimestamp();

        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(uri.Host, uri.Port, cts.Token).ConfigureAwait(false);
            return CreateResult(endpoint, CoreStatusState.Healthy, _timeProvider.GetElapsedTime(start), "Connected to TCP endpoint.");
        }
        catch (OperationCanceledException)
        {
            return CreateResult(endpoint, CoreStatusState.Unavailable, _timeProvider.GetElapsedTime(start), "Timed out connecting to TCP endpoint.");
        }
        catch (SocketException ex)
        {
            return CreateResult(endpoint, CoreStatusState.Unavailable, _timeProvider.GetElapsedTime(start), ex.Message);
        }
        catch (Exception ex)
        {
            return CreateResult(endpoint, CoreStatusState.Unavailable, _timeProvider.GetElapsedTime(start), ex.Message);
        }
    }

    private CoreStatusResult CreateResult(CoreEndpoint endpoint, CoreStatusState state, TimeSpan? latency, string? message)
    {
        var observedAt = _timeProvider.GetLocalNow();
        return new CoreStatusResult(endpoint, state, latency, message, observedAt);
    }
}
