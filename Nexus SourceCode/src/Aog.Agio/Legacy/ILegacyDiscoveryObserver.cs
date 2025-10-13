using System.Threading;
using System.Threading.Tasks;

namespace Aog.Agio.Legacy;

/// <summary>
/// Receives notifications whenever a legacy discovery frame is decoded.
/// </summary>
public interface ILegacyDiscoveryObserver
{
    /// <summary>
    /// Called when a discovery announcement is decoded from the UDP transport.
    /// </summary>
    /// <param name="announcement">Announcement describing the module.</param>
    /// <param name="cancellationToken">Cancellation token for the observer operation.</param>
    ValueTask OnDiscoveryAsync(LegacyDiscoveryAnnouncement announcement, CancellationToken cancellationToken);
}
