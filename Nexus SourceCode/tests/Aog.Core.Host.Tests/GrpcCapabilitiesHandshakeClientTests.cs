using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aog.Abstractions.Contracts;
using Aog.Core.Host.Capabilities;
using Aog.Protos.Capabilities.V1;
using Grpc.Core;
using Xunit;

namespace Aog.Core.Host.Tests;

public sealed class GrpcCapabilitiesHandshakeClientTests
{
    [Fact]
    public async Task HandshakeAsync_AttachesContractFingerprintHeader()
    {
        var client = new RecordingCapabilitiesClient();
        var handshakeClient = new GrpcCapabilitiesHandshakeClient(client);
        var request = new HandshakeRequest { NodeId = "core", SessionId = "core-123" };

        await handshakeClient.HandshakeAsync(request, CancellationToken.None);

        Assert.NotNull(client.LastHeaders);
        var header = Assert.Single(client.LastHeaders!, h => h.Key == GrpcContractRegistry.FingerprintHeaderName);
        Assert.Equal(GrpcContractRegistry.DescriptorFingerprint, header.Value);
    }

    private sealed class RecordingCapabilitiesClient : CapabilitiesService.CapabilitiesServiceClient
    {
        public Metadata? LastHeaders { get; private set; }

        public override AsyncUnaryCall<HandshakeResponse> HandshakeAsync(
            HandshakeRequest request,
            Metadata? headers = null,
            DateTime? deadline = null,
            CancellationToken cancellationToken = default)
        {
            LastHeaders = headers ?? new Metadata();
            var response = new HandshakeResponse();
            return new AsyncUnaryCall<HandshakeResponse>(
                Task.FromResult(response),
                Task.FromResult(new Metadata()),
                () => Status.DefaultSuccess,
                () => headers ?? new Metadata(),
                () => { });
        }
    }
}
