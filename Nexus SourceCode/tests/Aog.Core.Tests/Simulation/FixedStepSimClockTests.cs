using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aog.Core.Simulation;
using FluentAssertions;
using Xunit;

namespace Aog.Core.Tests.Simulation;

public static class FixedStepSimClockTests
{
    [Fact]
    public static async Task AdvanceAsync_ProducesDeterministicSequence()
    {
        var step = TimeSpan.FromMilliseconds(20);
        var first = new FixedStepSimClock(step);
        var second = new FixedStepSimClock(step);

        var firstSequence = await CollectAsync(first, 10);
        var secondSequence = await CollectAsync(second, 10);

        firstSequence.Should().Equal(secondSequence);
        firstSequence.Select(t => t.Elapsed).Should().BeInAscendingOrder();
        firstSequence.Last().Elapsed.Should().Be(step * 10);
    }

    [Fact]
    public static void Reset_ToAlignedValue_Succeeds()
    {
        var step = TimeSpan.FromSeconds(1);
        var clock = new FixedStepSimClock(step);

        var aligned = SimTime.FromTick(5, step);
        clock.Reset(aligned);

        clock.Current.Should().Be(aligned);
    }

    [Fact]
    public static void Constructor_WithNonPositiveStep_Throws()
    {
        Action act = () => new FixedStepSimClock(TimeSpan.Zero);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public static void Reset_WithUnalignedTime_Throws()
    {
        var step = TimeSpan.FromMilliseconds(5);
        var clock = new FixedStepSimClock(step);
        var invalid = new SimTime(3, TimeSpan.FromMilliseconds(12));

        Action act = () => clock.Reset(invalid);
        act.Should().Throw<ArgumentException>();
    }

    private static async Task<IReadOnlyList<SimTime>> CollectAsync(ISimClock clock, int steps)
    {
        var result = new List<SimTime>();
        for (var i = 0; i < steps; i++)
        {
            var time = await clock.AdvanceAsync();
            result.Add(time);
        }

        return result;
    }
}
