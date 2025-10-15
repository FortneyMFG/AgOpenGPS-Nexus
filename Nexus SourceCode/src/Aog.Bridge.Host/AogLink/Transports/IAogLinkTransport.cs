using System.Collections.Generic;
using System.Threading;
using Aog.Link.V1;

namespace Aog.Bridge.Host.AogLink.Transports;

/// <summary>
/// Represents a transport capable of sending and receiving AOG-Link envelopes.
/// </summary>
public interface IAogLinkTransport : IAsyncDisposable
{
    /// <summary>
    /// Starts the transport.
    /// </summary>
    ValueTask StartAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Stops the transport and releases resources.
    /// </summary>
    ValueTask StopAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Sends an envelope over the transport.
    /// </summary>
    ValueTask SendAsync(LinkEnvelope envelope, CancellationToken cancellationToken = default);

    /// <summary>
    /// Enumerates incoming envelopes from the transport.
    /// </summary>
    IAsyncEnumerable<LinkEnvelope> ReadAsync(CancellationToken cancellationToken);
}
