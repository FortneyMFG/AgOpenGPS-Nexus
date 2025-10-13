using System.Threading;
using System.Threading.Tasks;
using Aog.Protos.Capabilities.V1;

namespace Aog.Core.Host.Capabilities;

/// <summary>
/// gRPC-backed implementation of <see cref="ICapabilitiesHandshakeClient"/>.
/// </summary>
public sealed class GrpcCapabilitiesHandshakeClient : ICapabilitiesHandshakeClient
{
    private readonly CapabilitiesService.CapabilitiesServiceClient _client;

    public GrpcCapabilitiesHandshakeClient(CapabilitiesService.CapabilitiesServiceClient client)
    {
        _client = client;
    }

    public async Task<HandshakeResponse> HandshakeAsync(HandshakeRequest request, CancellationToken cancellationToken)
    {
        var call = _client.HandshakeAsync(request, cancellationToken: cancellationToken);
        return await call.ResponseAsync.ConfigureAwait(false);
    }
}
