using System;
using System.Threading.Tasks;
using Aog.Core.Simulation;
using FluentAssertions;
using Xunit;

namespace Aog.Core.Tests.Simulation;

public sealed class CompositeSimulationFabricTests
{
    [Fact]
    public async Task AdvanceAsync_UsesUnderlyingClock()
    {
        var fabric = CompositeSimulationFabric.CreateDefault(step: TimeSpan.FromMilliseconds(5));

        var first = await fabric.AdvanceAsync();
        var second = await fabric.AdvanceAsync();

        first.Elapsed.Should().Be(TimeSpan.FromMilliseconds(5));
        second.Elapsed.Should().Be(TimeSpan.FromMilliseconds(10));
    }

    [Fact]
    public void Reset_ReinitialisesRngAndClock()
    {
        var fabric = CompositeSimulationFabric.CreateDefault();

        fabric.Reset(seed: 100, start: SimTime.Zero);

        fabric.Clock.Current.Should().Be(SimTime.Zero);
        fabric.Rng.Seed.Should().Be(100);
        fabric.Rng.Next().Should().Be(new DeterministicSimRng(100).Next());
    }
}
