using System;
using System.Threading;
using System.Threading.Tasks;

namespace Aog.Agio.Corrections;

/// <summary>
/// Represents a concrete source of GNSS correction data.
/// </summary>
public interface ICorrectionSource : IAsyncDisposable
{
    /// <summary>
    /// Gets the diagnostic name for the correction source instance.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Starts the source and streams correction fragments until completion or cancellation.
    /// </summary>
    /// <param name="publish">Delegate invoked when a correction fragment should be emitted.</param>
    /// <param name="cancellationToken">Token used to stop the source.</param>
    /// <returns>The terminal result of the source run.</returns>
    Task<CorrectionSourceResult> RunAsync(
        Func<ReadOnlyMemory<byte>, CancellationToken, ValueTask> publish,
        CancellationToken cancellationToken);
}
