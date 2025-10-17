using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aog.Agio.Capabilities;
using Aog.Core.Capabilities;
using Aog.Protos.Capabilities.V1;
using Grpc.Core;
using Grpc.Core.Testing;
using Xunit;

namespace Aog.Capabilities.Tests;

public sealed class CapabilitiesHandshakeTests
{
    [Fact]
    public async Task AgioServiceEchoesSupportedCapabilitiesAndFlagsUnknownOnes()
    {
        var factory = new CapabilityDescriptorFactory(defaultVersion: "1.2.3");
        var coreClient = new CoreCapabilitiesClient(factory);
        var request = coreClient.BuildHandshake(
            nodeId: "core-host",
            capabilityNames: new[] { "nav.pose", "nav.imu", "nav.pose" },
            sessionId: "session-123");

        var agioCapabilities = new[]
        {
            new CapabilityDescriptor { Name = "nav.pose", Version = "1.2.3" },
        };

        var service = new AgioCapabilitiesService("agio-host", agioCapabilities);
        var response = await service.Handshake(request, CreateContext());

        Assert.Equal("session-123", response.SessionId);
        Assert.Equal("agio-host", response.NodeId);
        Assert.Equal(CapabilityRole.Agio, response.Role);

        Assert.Collection(response.AcceptedCapabilities,
            descriptor => Assert.Equal("nav.pose", descriptor.Name));

        var rejection = Assert.Single(response.Rejections);
        Assert.Equal("nav.imu", rejection.Capability.Name);
        Assert.Equal("Capability not supported by AGiO host.", rejection.Reason);
    }

    [Fact]
    public async Task AgioServiceUsesHostDescriptorsWhenMetadataDiffers()
    {
        var request = new HandshakeRequest
        {
            SessionId = "session-456",
            NodeId = "core-host",
            Role = CapabilityRole.Core,
        };

        request.Capabilities.Add(new CapabilityDescriptor
        {
            Name = "nav.pose",
            Version = "0.9.0",
            Summary = "Legacy pose stream",
        });

        var agioCapabilities = new[]
        {
            new CapabilityDescriptor
            {
                Name = "nav.pose",
                Version = "1.2.3",
                Summary = "Latest pose stream",
            },
        };

        var service = new AgioCapabilitiesService("agio-host", agioCapabilities);
        var response = await service.Handshake(request, CreateContext());

        var accepted = Assert.Single(response.AcceptedCapabilities);
        Assert.Equal("nav.pose", accepted.Name);
        Assert.Equal("1.2.3", accepted.Version);
        Assert.Equal("Latest pose stream", accepted.Summary);
        Assert.Empty(response.Rejections);
    }

    [Fact]
    public void CapabilityDescriptorFactorySkipsEmptyNames()
    {
        var factory = new CapabilityDescriptorFactory(defaultVersion: "1.0.0", defaultSummary: "Telemetry");

        var descriptors = factory.Create(new[] { "  ", "telemetry.pose", "telemetry.pose", null! });

        var descriptor = Assert.Single(descriptors);
        Assert.Equal("telemetry.pose", descriptor.Name);
        Assert.Equal("1.0.0", descriptor.Version);
        Assert.Equal("Telemetry", descriptor.Summary);
    }

    [Fact]
    public void CapabilityDescriptorFactoryUsesRegistryMetadata()
    {
        var factory = new CapabilityDescriptorFactory();

        var descriptor = Assert.Single(factory.Create(new[] { "mapping:raster" }));

        Assert.Equal("mapping:raster", descriptor.Name);
        Assert.Equal("1.0.0", descriptor.Version);
        Assert.Equal("Publishes raster coverage tiles, rate surfaces, and diagnostics.", descriptor.Summary);
        Assert.Equal("raster", descriptor.Attributes["surface"]);
        Assert.Equal("mapping", descriptor.Attributes["bundle"]);
    }

    private static ServerCallContext CreateContext()
    {
        return TestServerCallContext.Create(
            method: "capabilities.v1.CapabilitiesService/Handshake",
            host: null,
            deadline: DateTime.UtcNow.AddMinutes(1),
            requestHeaders: new Metadata(),
            cancellationToken: CancellationToken.None,
            peer: "ipv4:127.0.0.1",
            authContext: null,
            contextPropagationToken: null,
            responseTrailers: null,
            writeHeadersFunc: _ => Task.CompletedTask);
    }
}
