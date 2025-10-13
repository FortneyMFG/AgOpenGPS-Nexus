using System.Threading;
using System.Threading.Tasks;
using Aog.Core.V1;

namespace Aog.Agio.Legacy;

/// <summary>
/// Receives typed steering commands decoded from legacy PGNs.
/// </summary>
public interface ILegacySteerCommandObserver
{
    /// <summary>
    /// Called when a steering command is decoded from the UDP transport.
    /// </summary>
    /// <param name="command">Decoded steering command.</param>
    /// <param name="metadata">Legacy metadata accompanying the command.</param>
    /// <param name="cancellationToken">Cancellation token controlling the callback.</param>
    ValueTask OnSteerCommandAsync(SteerCmd command, LegacySteerCommandMetadata metadata, CancellationToken cancellationToken);
}
