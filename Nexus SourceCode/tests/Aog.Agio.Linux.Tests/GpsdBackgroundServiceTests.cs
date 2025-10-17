using System.Reflection;
using Aog.Agio.Linux.Gpsd;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Aog.Agio.Linux.Tests;

public sealed class GpsdBackgroundServiceTests
{
    [Fact]
    public async Task ExecuteAsync_ExitsImmediately_WhenSocketDisabled()
    {
        var options = Options.Create(new GpsdClientOptions
        {
            SocketPath = string.Empty,
        });
        var client = new GpsdClient(new ThrowingConnectionFactory(), NullLogger<GpsdClient>.Instance);
        var service = new GpsdBackgroundService(client, NullLogger<GpsdBackgroundService>.Instance, options);

        var executeAsync = typeof(GpsdBackgroundService).GetMethod(
            "ExecuteAsync",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(executeAsync);

        var task = (Task)executeAsync!.Invoke(service, new object[] { CancellationToken.None })!;
        await task;
    }

    private sealed class ThrowingConnectionFactory : IGpsdConnectionFactory
    {
        public Task<Stream?> ConnectAsync(CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("GpsdBackgroundService should not attempt to connect when disabled.");
        }
    }
}
