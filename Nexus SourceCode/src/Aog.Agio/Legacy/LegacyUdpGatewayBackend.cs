using System;
using System.Threading;
using System.Threading.Tasks;
using Aog.Core.Mesh;
using Aog.Core.V1;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Aog.Agio.Legacy;

/// <summary>
/// Registers services that make up the legacy UDP gateway.
/// </summary>
public sealed class LegacyUdpGatewayBackend : IAgioBackend
{
    /// <inheritdoc />
    public string Name => "Legacy UDP Gateway";

    /// <inheritdoc />
    public void ConfigureServices(IServiceCollection services)
    {
        if (services is null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        services.AddSingleton<LegacyPoseCodec>();
        services.AddSingleton<LegacyDiscoveryCodec>();
        services.AddSingleton<LegacySteerCodec>();
        services.AddSingleton<ILiveTelemetryMeshService, LiveTelemetryMeshService>();
        services.AddSingleton<ILegacyUdpTransport, NullLegacyUdpTransport>();
        services.AddSingleton<ILegacyPoseObserver, NullLegacyPoseObserver>();
        services.AddSingleton<ILegacyDiscoveryObserver, NullLegacyDiscoveryObserver>();
        services.AddSingleton<ILegacySteerCommandObserver, NullLegacySteerCommandObserver>();
        services.AddSingleton<ILegacySteerStateObserver, NullLegacySteerStateObserver>();
        services.AddSingleton<ILegacySectionObserver, NullLegacySectionObserver>();
        services.AddSingleton<LegacyMeshPresencePublisher>();
        services.AddSingleton<ILegacyMeshPresencePublisher>(provider => provider.GetRequiredService<LegacyMeshPresencePublisher>());
        services.AddSingleton<IHostedService>(provider => provider.GetRequiredService<LegacyMeshPresencePublisher>());
        services.AddSingleton<LegacyUdpGateway>();
    }

    private sealed class NullLegacyUdpTransport : ILegacyUdpTransport
    {
        public ValueTask SendAsync(ReadOnlyMemory<byte> datagram, CancellationToken cancellationToken) => ValueTask.CompletedTask;
    }

    private sealed class NullLegacyPoseObserver : ILegacyPoseObserver
    {
        public ValueTask OnPoseAsync(Pose pose, LegacyPoseMetadata metadata, CancellationToken cancellationToken) => ValueTask.CompletedTask;
    }

    private sealed class NullLegacyDiscoveryObserver : ILegacyDiscoveryObserver
    {
        public ValueTask OnDiscoveryAsync(LegacyDiscoveryAnnouncement announcement, CancellationToken cancellationToken) => ValueTask.CompletedTask;
    }

    private sealed class NullLegacySteerCommandObserver : ILegacySteerCommandObserver
    {
        public ValueTask OnSteerCommandAsync(SteerCmd command, LegacySteerCommandMetadata metadata, CancellationToken cancellationToken) => ValueTask.CompletedTask;
    }

    private sealed class NullLegacySteerStateObserver : ILegacySteerStateObserver
    {
        public ValueTask OnSteerStateAsync(SteerState state, LegacySteerStateMetadata metadata, CancellationToken cancellationToken) => ValueTask.CompletedTask;
    }

    private sealed class NullLegacySectionObserver : ILegacySectionObserver
    {
        public ValueTask OnSectionMaskAsync(SectionMask mask, CancellationToken cancellationToken) => ValueTask.CompletedTask;
    }
}
