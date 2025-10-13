using System.Threading;
using System.Threading.Tasks;
using Aog.Protos.Capabilities.V1;

namespace Aog.Core.Host.Capabilities;

/// <summary>
/// Abstraction over the RPC client used to perform the capabilities handshake.
/// </summary>
public interface ICapabilitiesHandshakeClient
{
    /// <summary>
    /// Executes the capabilities handshake against AGiO.
    /// </summary>
    /// <param name="request">The handshake request to send.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    Task<HandshakeResponse> HandshakeAsync(HandshakeRequest request, CancellationToken cancellationToken);
}
