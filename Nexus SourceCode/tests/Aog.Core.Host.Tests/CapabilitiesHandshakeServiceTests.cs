using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aog.Core.Capabilities;
using Aog.Core.Host.Capabilities;
using Aog.Protos.Capabilities.V1;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aog.Core.Host.Tests;

public sealed class CapabilitiesHandshakeServiceTests
{
    [Fact]
    public async Task PerformsHandshakeAndLogsResults()
    {
        var coreOptions = new CoreCapabilitiesOptions
        {
            NodeId = "core-01",
            SessionPrefix = "core-",
            DefaultCapabilityVersion = "2.0.0",
            DefaultCapabilitySummary = "telemetry"
        };

        coreOptions.AdvertisedCapabilities.Add(" nav.pose ");
        coreOptions.AdvertisedCapabilities.Add("nav.pose");
        coreOptions.AdvertisedCapabilities.Add("nav.imu");
        coreOptions.DefaultCapabilityAttributes["quality"] = "beta";

        var agioOptions = new AgioConnectionOptions
        {
            Endpoint = "https://localhost:5500"
        };

        var logger = new ListLogger<CapabilitiesHandshakeService>();
        var factory = new CapabilityDescriptorFactory(
            coreOptions.DefaultCapabilityVersion,
            coreOptions.DefaultCapabilitySummary,
            coreOptions.DefaultCapabilityAttributes);
        var requestClient = new CoreCapabilitiesClient(factory);

        var response = new HandshakeResponse
        {
            SessionId = "session-response",
            NodeId = "agio-1",
            Role = CapabilityRole.CapabilityRoleAgio,
        };

        response.AcceptedCapabilities.Add(new CapabilityDescriptor { Name = "nav.pose" });
        response.Rejections.Add(new CapabilityRejection
        {
            Capability = new CapabilityDescriptor { Name = "nav.imu" },
            Reason = "unsupported"
        });

        var handshakeClient = new StubHandshakeClient((request, _) =>
        {
            return Task.FromResult(response);
        });

        var service = new CapabilitiesHandshakeService(
            logger,
            Options.Create(coreOptions),
            Options.Create(agioOptions),
            requestClient,
            handshakeClient);

        await service.StartAsync(CancellationToken.None);
        await service.StopAsync(CancellationToken.None);

        var sentRequest = handshakeClient.Request;
        Assert.NotNull(sentRequest);
        Assert.StartsWith("core-", sentRequest!.SessionId, StringComparison.Ordinal);
        Assert.Equal("core-01", sentRequest.NodeId);

        Assert.Collection(
            sentRequest.Capabilities,
            descriptor => Assert.Equal("nav.pose", descriptor.Name),
            descriptor => Assert.Equal("nav.imu", descriptor.Name));

        Assert.Contains(logger.Entries, entry =>
            entry.Level == LogLevel.Information && entry.Message.Contains("AGiO accepted 1 capabilities"));

        Assert.Contains(logger.Entries, entry =>
            entry.Level == LogLevel.Warning && entry.Message.Contains("nav.imu"));
    }

    [Fact]
    public async Task LogsErrorsWhenHandshakeFails()
    {
        var coreOptions = new CoreCapabilitiesOptions
        {
            NodeId = "core-02",
            SessionPrefix = "core-"
        };

        var agioOptions = new AgioConnectionOptions
        {
            Endpoint = "https://localhost:5501"
        };

        var logger = new ListLogger<CapabilitiesHandshakeService>();
        var requestClient = new CoreCapabilitiesClient(new CapabilityDescriptorFactory());

        var handshakeClient = new StubHandshakeClient((_, _) =>
        {
            throw new InvalidOperationException("boom");
        });

        var service = new CapabilitiesHandshakeService(
            logger,
            Options.Create(coreOptions),
            Options.Create(agioOptions),
            requestClient,
            handshakeClient);

        await service.StartAsync(CancellationToken.None);
        await service.StopAsync(CancellationToken.None);

        Assert.Contains(logger.Entries, entry => entry.Level == LogLevel.Error && entry.Message.Contains("Capabilities handshake failed"));
    }

    private sealed class StubHandshakeClient : ICapabilitiesHandshakeClient
    {
        private readonly Func<HandshakeRequest, CancellationToken, Task<HandshakeResponse>> _handler;

        public StubHandshakeClient(Func<HandshakeRequest, CancellationToken, Task<HandshakeResponse>> handler)
        {
            _handler = handler;
        }

        public HandshakeRequest? Request { get; private set; }

        public async Task<HandshakeResponse> HandshakeAsync(HandshakeRequest request, CancellationToken cancellationToken)
        {
            Request = request;
            return await _handler(request, cancellationToken).ConfigureAwait(false);
        }
    }

    private sealed class ListLogger<T> : ILogger<T>
    {
        private sealed class Scope : IDisposable
        {
            public static Scope Instance { get; } = new();
            public void Dispose()
            {
            }
        }

        public List<(LogLevel Level, string Message)> Entries { get; } = new();

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => Scope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Entries.Add((logLevel, formatter(state, exception)));
        }
    }
}
