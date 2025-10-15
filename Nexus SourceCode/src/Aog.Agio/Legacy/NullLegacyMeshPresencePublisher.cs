using System.Threading;
using System.Threading.Tasks;
using Aog.Core.V1;

namespace Aog.Agio.Legacy;

/// <summary>
/// No-op implementation used when mesh presence publishing is disabled.
/// </summary>
public sealed class NullLegacyMeshPresencePublisher : ILegacyMeshPresencePublisher
{
    /// <summary>
    /// Gets the singleton instance of the no-op publisher.
    /// </summary>
    public static NullLegacyMeshPresencePublisher Instance { get; } = new();

    private NullLegacyMeshPresencePublisher()
    {
    }

    /// <inheritdoc />
    public ValueTask PublishPresenceAsync(Pose pose, LegacyPoseMetadata metadata, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask OnDiscoveryAsync(LegacyDiscoveryAnnouncement announcement, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }
}
