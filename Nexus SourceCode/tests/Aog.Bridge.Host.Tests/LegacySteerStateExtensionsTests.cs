using Aog.Bridge.Host.AogLink.Legacy;
using Aog.Core.V1;
using Google.Protobuf;
using Xunit;

namespace Aog.Bridge.Host.Tests;

public sealed class LegacySteerStateExtensionsTests
{
    [Fact]
    public void SetLegacyHeadingDegrees_PersistsThroughClone()
    {
        var state = new SteerState();
        state.SetLegacyHeadingDegrees(123.45);

        var clone = state.Clone();

        Assert.True(clone.TryGetLegacyHeadingDegrees(out var heading));
        Assert.Equal(123.45, heading, 3);
    }

    [Fact]
    public void SetLegacyHeadingDegrees_PersistsThroughSerializationRoundtrip()
    {
        var state = new SteerState();
        state.SetLegacyHeadingDegrees(271.828);

        var bytes = state.ToByteArray();
        var roundTrip = SteerState.Parser.ParseFrom(bytes);

        Assert.True(roundTrip.TryGetLegacyHeadingDegrees(out var heading));
        Assert.Equal(271.828, heading, 3);
    }

    [Fact]
    public void ClearLegacyHeadingDegrees_RemovesPersistedValue()
    {
        var state = new SteerState();
        state.SetLegacyHeadingDegrees(45.0);
        state.ClearLegacyHeadingDegrees();

        Assert.False(state.TryGetLegacyHeadingDegrees(out _));

        var bytes = state.ToByteArray();
        var roundTrip = SteerState.Parser.ParseFrom(bytes);
        Assert.False(roundTrip.TryGetLegacyHeadingDegrees(out _));
    }
}
