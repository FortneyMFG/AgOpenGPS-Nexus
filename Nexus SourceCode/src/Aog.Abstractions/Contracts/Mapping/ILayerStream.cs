using System.Collections.Generic;
using System.Threading;

namespace Aog.Abstractions.Mapping;

/// <summary>
/// Represents a typed stream of layer changes or journal events.
/// </summary>
/// <typeparam name="T">Payload type emitted by the stream.</typeparam>
public interface ILayerStream<out T>
{
    /// <summary>
    /// Reads the stream until cancellation.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    IAsyncEnumerable<T> ReadAsync(CancellationToken cancellationToken = default);
}
