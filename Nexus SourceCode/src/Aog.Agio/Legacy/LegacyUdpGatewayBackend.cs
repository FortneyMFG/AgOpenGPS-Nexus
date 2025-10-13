using System;
using System.Threading;
using System.Threading.Tasks;
using Aog.Core.V1;
using Microsoft.Extensions.DependencyInjection;

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
        services.AddSingleton<ILegacyUdpTransport, NullLegacyUdpTransport>();
        services.AddSingleton<ILegacyPoseObserver, NullLegacyPoseObserver>();
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
}
