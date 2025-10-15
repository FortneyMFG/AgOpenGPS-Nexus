using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Aog.Agio.Ntrip;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aog.Agio.Tests.Ntrip;

public sealed class NtripClientTests
{
    [Fact]
    public async Task RunAsync_StreamsCorrectionsToSink()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;

        var options = new NtripClientOptions
        {
            Host = "127.0.0.1",
            Port = port,
            MountPoint = "MOUNT",
            ReceiveBufferSize = 1024,
            ReconnectBackoff = TimeSpan.FromMilliseconds(10)
        };

        var sink = new RecordingSink();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var runCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token);

        var client = new NtripClient(options, new[] { sink }, NullLogger<NtripClient>.Instance, TimeProvider.System);

        var serverTask = RunNtripServerAsync(listener, "MOUNT", payload: Encoding.ASCII.GetBytes("RTCM"), runCts.Token);
        var runTask = client.RunAsync(runCts.Token);

        var received = await sink.WaitForPayloadAsync(cts.Token).ConfigureAwait(false);
        Assert.Equal("RTCM", Encoding.ASCII.GetString(received));

        var request = await serverTask.ConfigureAwait(false);
        Assert.Contains("GET /MOUNT HTTP/1.1", request, StringComparison.Ordinal);
        Assert.Contains("User-Agent: AgOpenGPS-Nexus/1.0", request, StringComparison.Ordinal);

        runCts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await runTask.ConfigureAwait(false)).ConfigureAwait(false);
    }

    [Fact]
    public async Task RunAsync_IncludesAuthorizationHeaderWhenCredentialsProvided()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;

        var options = new NtripClientOptions
        {
            Host = "127.0.0.1",
            Port = port,
            MountPoint = "secure",
            Username = "demo",
            Password = "password"
        };

        var sink = new RecordingSink();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var runCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token);

        var client = new NtripClient(options, new[] { sink }, NullLogger<NtripClient>.Instance, TimeProvider.System);

        var serverTask = RunNtripServerAsync(listener, "secure", payload: Encoding.ASCII.GetBytes("data"), runCts.Token);
        var runTask = client.RunAsync(runCts.Token);

        await sink.WaitForPayloadAsync(cts.Token).ConfigureAwait(false);

        var request = await serverTask.ConfigureAwait(false);
        Assert.Contains("Authorization: Basic", request, StringComparison.Ordinal);
        Assert.Contains("ZGVtbzpwYXNzd29yZA==", request, StringComparison.Ordinal); // demo:password

        runCts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await runTask.ConfigureAwait(false)).ConfigureAwait(false);
    }

    private static async Task<string> RunNtripServerAsync(TcpListener listener, string expectedMountPoint, byte[] payload, CancellationToken cancellationToken)
    {
        await using var client = await listener.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false);
        await using var network = client.GetStream();

        var request = await ReadRequestAsync(network, cancellationToken).ConfigureAwait(false);
        Assert.Contains($"GET /{expectedMountPoint} HTTP/1.1", request, StringComparison.Ordinal);

        var response = "ICY 200 OK\r\nServer: test\r\n\r\n";
        var responseBytes = Encoding.ASCII.GetBytes(response);
        await network.WriteAsync(responseBytes, 0, responseBytes.Length, cancellationToken).ConfigureAwait(false);
        await network.WriteAsync(payload, 0, payload.Length, cancellationToken).ConfigureAwait(false);
        await network.FlushAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            await Task.Delay(Timeout.Infinite, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Expected when the test cancels the token.
        }

        return request;
    }

    private static async Task<string> ReadRequestAsync(NetworkStream stream, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(stream, Encoding.ASCII, detectEncodingFromByteOrderMarks: false, bufferSize: 1024, leaveOpen: true);
        var builder = new StringBuilder();
        while (true)
        {
            var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            if (line is null)
            {
                throw new InvalidOperationException("Client disconnected before completing request.");
            }

            builder.Append(line).Append("\r\n");
            if (line.Length == 0)
            {
                break;
            }
        }

        return builder.ToString();
    }

    private sealed class RecordingSink : INtripCorrectionSink
    {
        private readonly TaskCompletionSource<byte[]> _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public ValueTask PublishAsync(ReadOnlyMemory<byte> payload, CancellationToken cancellationToken)
        {
            if (!_tcs.Task.IsCompleted)
            {
                _tcs.TrySetResult(payload.ToArray());
            }

            return ValueTask.CompletedTask;
        }

        public Task<byte[]> WaitForPayloadAsync(CancellationToken cancellationToken) => _tcs.Task.WaitAsync(cancellationToken);
    }
}
