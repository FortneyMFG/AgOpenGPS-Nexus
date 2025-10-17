using Aog.Agio.AogLink;
using Aog.Protos.Capabilities.V1;
using FluentAssertions;
using Xunit;

namespace Aog.Agio.Tests.AogLink;

public sealed class AogLinkBridgeTests
{
    [Fact]
    public void HandshakeRoundTrip_PreservesPayload()
    {
        var bridge = new AogLinkBridge();
        var request = new HandshakeRequest
        {
            NodeId = "core-node",
            SessionId = "session-123",
            Role = CapabilityRole.Core,
        };
        request.Capabilities.Add(new CapabilityDescriptor { Name = "guidance.control", Version = "1.0.0" });

        var frame = bridge.CreateHandshakeFrame(request, sequence: 7, source: 0x20, destination: 0x10);
        var decoded = bridge.ParseHandshakeRequest(frame);

        decoded.Should().BeEquivalentTo(request);
    }

    [Fact]
    public void ParseHandshakeResponse_ValidatesMessageType()
    {
        var bridge = new AogLinkBridge();
        var response = new HandshakeResponse
        {
            NodeId = "agio",
            SessionId = "session",
            Role = CapabilityRole.Agio,
        };
        var frame = bridge.CreateHandshakeResponseFrame(response, sequence: 8, source: 0x10, destination: 0x20);

        var decoded = bridge.ParseHandshakeResponse(frame);
        decoded.Should().BeEquivalentTo(response);
    }
}
