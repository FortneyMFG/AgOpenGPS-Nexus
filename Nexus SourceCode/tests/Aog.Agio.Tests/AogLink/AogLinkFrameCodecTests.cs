using System;
using Aog.Agio.AogLink;
using Xunit;

namespace Aog.Agio.Tests.AogLink;

public sealed class AogLinkFrameCodecTests
{
    [Fact]
    public void EncodeAndDecode_RoundTripPayload()
    {
        var payload = new byte[] { 1, 2, 3, 4 };
        var header = new AogLinkFrameHeader(1, AogLinkMessageCatalog.ControlClass, 0x0101, 42, 0x10, 0x20, (ushort)payload.Length);
        var frame = new AogLinkFrame(header, payload);

        var encoded = AogLinkFrameCodec.Encode(frame);
        var decoded = AogLinkFrameCodec.Decode(encoded);

        Assert.Equal(header, decoded.Header);
        Assert.Equal(payload, decoded.Payload.ToArray());
    }

    [Fact]
    public void Decode_InvalidLength_Throws()
    {
        var payload = new byte[] { 1, 2 };
        var header = new AogLinkFrameHeader(1, 0, 0, 0, 0, 0, 4);
        var frame = new AogLinkFrame(header, payload);
        var encoded = AogLinkFrameCodec.Encode(frame);
        encoded[^1] = 0; // corrupt length by truncating payload

        Assert.Throws<InvalidOperationException>(() => AogLinkFrameCodec.Decode(encoded));
    }
}
