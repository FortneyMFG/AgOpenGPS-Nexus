using System;
using System.Threading;
using System.Threading.Tasks;

namespace Aog.Agio.Ntrip;

/// <summary>
/// Represents a target that consumes RTCM correction data streamed from an NTRIP caster.
/// </summary>
public interface INtripCorrectionSink
{
    /// <summary>
    /// Publishes a chunk of RTCM correction data.
    /// </summary>
    /// <param name="payload">The correction payload.</param>
    /// <param name="cancellationToken">Cancellation token used to abort the publish operation.</param>
    ValueTask PublishAsync(ReadOnlyMemory<byte> payload, CancellationToken cancellationToken);
}
