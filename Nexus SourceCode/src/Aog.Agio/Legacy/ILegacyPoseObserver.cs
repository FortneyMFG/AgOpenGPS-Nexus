using System.Threading;
using System.Threading.Tasks;
using Aog.Core.V1;

namespace Aog.Agio.Legacy;

/// <summary>
/// Receives typed pose updates decoded from legacy PGNs.
/// </summary>
public interface ILegacyPoseObserver
{
    /// <summary>
    /// Called when a pose is decoded from the UDP transport.
    /// </summary>
    /// <param name="pose">Decoded pose message.</param>
    /// <param name="metadata">Legacy metadata attached to the pose.</param>
    /// <param name="cancellationToken">Cancellation token controlling the callback.</param>
    ValueTask OnPoseAsync(Pose pose, LegacyPoseMetadata metadata, CancellationToken cancellationToken);
}
