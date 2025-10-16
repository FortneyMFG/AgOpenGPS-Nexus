using System;
using System.Net;
using System.Net.Sockets;
using FluentAssertions;
using Nexus.Cli.Host.Core.Endpoints;
using Nexus.Cli.Host.Core.Status;
using Xunit;

namespace Nexus.Cli.Host.Tests;

public class CoreStatusProbeTests
{
    [Fact]
    public async Task CheckAsync_ReturnsHealthy_ForReachableTcpEndpoint()
    {
        using var listener = new TcpListener(IPAddress.Loopback, port: 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        var endpoint = new CoreEndpoint(CoreTransportKind.Tcp, $"https://127.0.0.1:{port}", "test");
        var probe = new CoreStatusProbe(TimeProvider.System);

        var acceptTask = listener.AcceptTcpClientAsync();
        var result = await probe.CheckAsync(endpoint, CancellationToken.None);

        result.State.Should().Be(CoreStatusState.Healthy);
        result.Latency.Should().NotBeNull();

        using var client = await acceptTask.WaitAsync(TimeSpan.FromSeconds(2));
        client.Should().NotBeNull();
    }

    [Fact]
    public async Task CheckAsync_ReturnsUnavailable_ForUnreachableTcpEndpoint()
    {
        var endpoint = new CoreEndpoint(CoreTransportKind.Tcp, "https://127.0.0.1:6553", "test");
        var probe = new CoreStatusProbe(TimeProvider.System);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(750));
        var result = await probe.CheckAsync(endpoint, cts.Token);

        result.State.Should().Be(CoreStatusState.Unavailable);
        result.Message.Should().NotBeNull();
    }
}
