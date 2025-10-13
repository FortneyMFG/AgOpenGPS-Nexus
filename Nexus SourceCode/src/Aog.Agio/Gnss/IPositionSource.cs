using System;
using System.Threading;
using System.Threading.Tasks;
using Aog.Core.V1;

namespace Aog.Agio.Gnss;

/// <summary>
/// Represents a concrete source of GNSS pose updates.
/// </summary>
public interface IPositionSource : IAsyncDisposable
{
    /// <summary>
    /// Gets the diagnostic name for the source instance.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Starts the source and streams poses until completion or cancellation.
    /// </summary>
    /// <param name="publish">Delegate invoked when a pose should be emitted.</param>
    /// <param name="cancellationToken">Token used to stop the source.</param>
    /// <returns>The terminal result of the source run.</returns>
    Task<PositionSourceResult> RunAsync(Func<Pose, CancellationToken, ValueTask> publish, CancellationToken cancellationToken);
}
