using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

using Aog.Agio.Linux.Gpsd;

namespace Aog.Agio.Linux.Tests;

public class UnixDomainSocketGpsdConnectionFactoryTests
{
    [Fact]
    public async Task ConnectAsync_UsesLatestOptionsValue()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        await using var initialServer = UnixDomainSocketServer.Start();

        var options = new TestOptionsMonitor<GpsdClientOptions>(new GpsdClientOptions
        {
            SocketPath = initialServer.Path,
        });

        var factory = new UnixDomainSocketGpsdConnectionFactory(
            NullLogger<UnixDomainSocketGpsdConnectionFactory>.Instance,
            options);

        var initialStream = await factory.ConnectAsync(cts.Token);
        Assert.NotNull(initialStream);
        await initialServer.WaitForClientAsync(cts.Token);
        initialStream!.Dispose();

        await using var updatedServer = UnixDomainSocketServer.Start();
        options.Update(new GpsdClientOptions
        {
            SocketPath = updatedServer.Path,
        });

        var updatedStream = await factory.ConnectAsync(cts.Token);
        Assert.NotNull(updatedStream);
        await updatedServer.WaitForClientAsync(cts.Token);
        updatedStream!.Dispose();
    }

    private sealed class UnixDomainSocketServer : IAsyncDisposable
    {
        private readonly string _path;
        private readonly Socket _listener;
        private readonly Task<Socket> _acceptTask;
        private Socket? _acceptedClient;

        private UnixDomainSocketServer(string path, Socket listener, Task<Socket> acceptTask)
        {
            _path = path;
            _listener = listener;
            _acceptTask = acceptTask;
        }

        public string Path => _path;

        public static UnixDomainSocketServer Start()
        {
            var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"gpsd-test-{Guid.NewGuid():N}.sock");
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            var listener = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
            listener.Bind(new UnixDomainSocketEndPoint(path));
            listener.Listen(backlog: 1);

            return new UnixDomainSocketServer(path, listener, listener.AcceptAsync().AsTask());
        }

        public async Task WaitForClientAsync(CancellationToken cancellationToken)
        {
            _acceptedClient = await _acceptTask.WaitAsync(cancellationToken).ConfigureAwait(false);
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                _listener.Dispose();
            }
            finally
            {
                if (File.Exists(_path))
                {
                    File.Delete(_path);
                }
            }

            if (_acceptedClient is not null)
            {
                _acceptedClient.Dispose();
                return;
            }

            if (_acceptTask.IsCompletedSuccessfully)
            {
                _acceptTask.Result.Dispose();
                return;
            }

            if (!_acceptTask.IsCompleted)
            {
                try
                {
                    var socket = await _acceptTask.WaitAsync(TimeSpan.Zero).ConfigureAwait(false);
                    socket.Dispose();
                }
                catch
                {
                }
            }
        }
    }
}
