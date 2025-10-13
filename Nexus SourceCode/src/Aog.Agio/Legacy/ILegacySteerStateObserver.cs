using System.Threading;
using System.Threading.Tasks;
using Aog.Core.V1;

namespace Aog.Agio.Legacy;

/// <summary>
/// Receives typed steering feedback decoded from legacy PGNs.
/// </summary>
public interface ILegacySteerStateObserver
{
    /// <summary>
    /// Called when steering feedback is decoded from the UDP transport.
    /// </summary>
    /// <param name="state">Decoded steering feedback.</param>
    /// <param name="metadata">Legacy metadata accompanying the feedback.</param>
    /// <param name="cancellationToken">Cancellation token controlling the callback.</param>
    ValueTask OnSteerStateAsync(SteerState state, LegacySteerStateMetadata metadata, CancellationToken cancellationToken);
}
