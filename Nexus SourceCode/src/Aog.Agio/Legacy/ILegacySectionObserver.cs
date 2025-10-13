using System.Threading;
using System.Threading.Tasks;
using Aog.Core.V1;

namespace Aog.Agio.Legacy;

/// <summary>
/// Receives section mask updates decoded from legacy PGNs.
/// </summary>
public interface ILegacySectionObserver
{
    /// <summary>
    /// Called when a section mask update is decoded from the UDP transport.
    /// </summary>
    /// <param name="mask">Decoded section mask.</param>
    /// <param name="cancellationToken">Cancellation token controlling the callback.</param>
    ValueTask OnSectionMaskAsync(SectionMask mask, CancellationToken cancellationToken);
}
