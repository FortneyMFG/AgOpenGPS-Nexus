using System.Threading;
using System.Threading.Tasks;
using Aog.Core.V1;

namespace Aog.Agio.Legacy;

/// <summary>
/// Publishes legacy pose telemetry into the live telemetry mesh.
/// </summary>
public interface ILegacyMeshPresencePublisher
{
    /// <summary>
    /// Publishes a decoded pose as a mesh presence heartbeat.
    /// </summary>
    /// <param name="pose">Decoded pose message.</param>
    /// <param name="metadata">Legacy metadata accompanying the pose.</param>
    /// <param name="cancellationToken">Cancellation token controlling the operation.</param>
    ValueTask PublishPresenceAsync(Pose pose, LegacyPoseMetadata metadata, CancellationToken cancellationToken);

    /// <summary>
    /// Notifies the publisher of a discovery announcement so mesh metadata can be enriched.
    /// </summary>
    /// <param name="announcement">Discovery payload broadcast by the legacy device.</param>
    /// <param name="cancellationToken">Cancellation token controlling the operation.</param>
    ValueTask OnDiscoveryAsync(LegacyDiscoveryAnnouncement announcement, CancellationToken cancellationToken);
}
