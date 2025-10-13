using System;
using System.Threading;
using System.Threading.Tasks;

namespace Aog.Agio.Legacy;

/// <summary>
/// Abstraction over the UDP transport used by the legacy gateway.
/// </summary>
public interface ILegacyUdpTransport
{
    /// <summary>
    /// Sends a raw legacy datagram over the transport.
    /// </summary>
    /// <param name="datagram">Bytes to transmit.</param>
    /// <param name="cancellationToken">Cancellation token for the send operation.</param>
    ValueTask SendAsync(ReadOnlyMemory<byte> datagram, CancellationToken cancellationToken);
}
