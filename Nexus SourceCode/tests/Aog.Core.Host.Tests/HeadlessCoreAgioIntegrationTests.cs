using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Aog.Protos.Capabilities.V1;
using Grpc.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Aog.Core.Host.Tests;

public sealed class HeadlessCoreAgioIntegrationTests
{
    [Fact]
    public async Task CoreHost_Completes_Handshake_With_Agio_Service()
    {
        var supportedCapabilities = new[]
        {
            new CapabilityDescriptor { Name = "nav.pose" },
            new CapabilityDescriptor { Name = "nav.imu" }
        };

        var service = new RecordingCapabilitiesService(supportedCapabilities);
        var port = GetFreeTcpPort();

        var server = new Server
        {
            Services = { CapabilitiesService.BindService(service) },
            Ports = { new ServerPort("127.0.0.1", port, ServerCredentials.Insecure) }
        };

        server.Start();

        var configuration = new Dictionary<string, string?>
        {
            ["CoreHost:Agio:Endpoint"] = $"http://127.0.0.1:{port}",
            ["CoreHost:Agio:AllowUnencryptedHttp2"] = "true",
            ["CoreHost:Capabilities:NodeId"] = "core-headless",
            ["CoreHost:Capabilities:SessionPrefix"] = "core-",
            ["CoreHost:Capabilities:DefaultCapabilityVersion"] = "1.0.1",
            ["CoreHost:Capabilities:DefaultCapabilitySummary"] = "integration-test",
            ["CoreHost:Capabilities:AdvertisedCapabilities:0"] = "nav.pose",
            ["CoreHost:Capabilities:AdvertisedCapabilities:1"] = "nav.imu"
        };

        var builder = Program.CreateHostBuilder(Array.Empty<string>())
            .ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(configuration));

        using var host = builder.Build();

        var started = false;
        try
        {
            using var startCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            await host.StartAsync(startCts.Token);
            started = true;

            var request = await service.HandshakeTask.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.Equal("core-headless", request.NodeId);
            Assert.StartsWith("core-", request.SessionId, StringComparison.Ordinal);

            var requestedNames = request.Capabilities
                .Where(c => !string.IsNullOrWhiteSpace(c?.Name))
                .Select(c => c!.Name)
                .ToArray();

            Assert.Contains("nav.pose", requestedNames);
            Assert.Contains("nav.imu", requestedNames);

            var response = service.LastResponse;
            Assert.NotNull(response);
            Assert.Equal("agio-test", response!.NodeId);
            Assert.Equal(CapabilityRole.Agio, response.Role);
            Assert.Empty(response.Rejections);
            Assert.Collection(
                response.AcceptedCapabilities,
                descriptor => Assert.Equal("nav.pose", descriptor.Name),
                descriptor => Assert.Equal("nav.imu", descriptor.Name));
        }
        finally
        {
            if (started)
            {
                using var stopCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                await host.StopAsync(stopCts.Token);
            }

            await server.ShutdownAsync();
        }
    }

    private static int GetFreeTcpPort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var assigned = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return assigned;
    }

    private sealed class RecordingCapabilitiesService : CapabilitiesService.CapabilitiesServiceBase
    {
        private readonly Dictionary<string, CapabilityDescriptor> _supported;
        private readonly TaskCompletionSource<HandshakeRequest> _handshakeSource =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public RecordingCapabilitiesService(IEnumerable<CapabilityDescriptor> capabilities)
        {
            _supported = capabilities
                .Where(descriptor => descriptor is not null && !string.IsNullOrWhiteSpace(descriptor.Name))
                .GroupBy(descriptor => descriptor.Name!, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First().Clone(), StringComparer.OrdinalIgnoreCase);
        }

        public Task<HandshakeRequest> HandshakeTask => _handshakeSource.Task;

        public HandshakeResponse? LastResponse { get; private set; }

        public override Task<HandshakeResponse> Handshake(HandshakeRequest request, ServerCallContext context)
        {
            var response = new HandshakeResponse
            {
                SessionId = request.SessionId,
                NodeId = "agio-test",
                Role = CapabilityRole.Agio
            };

            foreach (var capability in request.Capabilities)
            {
                if (capability is null || string.IsNullOrWhiteSpace(capability.Name))
                {
                    continue;
                }

                if (_supported.TryGetValue(capability.Name, out var descriptor))
                {
                    response.AcceptedCapabilities.Add(descriptor.Clone());
                }
                else
                {
                    response.Rejections.Add(new CapabilityRejection
                    {
                        Capability = capability.Clone(),
                        Reason = "unsupported"
                    });
                }
            }

            LastResponse = response;
            _handshakeSource.TrySetResult(request.Clone());
            return Task.FromResult(response);
        }
    }
}
