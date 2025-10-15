using System;
using Aog.Bridge.Host.AogLink.Serial;

namespace Aog.Bridge.Host.Tests;

public sealed class SerialCodecTests
{
    [Fact]
    public void EncodeDecode_RoundTripsPayload()
    {
        var payload = new byte[32];
        new Random(42).NextBytes(payload);

        var encoded = AogLinkSerialCodec.Encode(payload);
        Assert.True(AogLinkSerialCodec.TryDecode(encoded, out var decoded));
        Assert.Equal(payload, decoded);
    }
}
