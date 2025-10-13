using System;
using System.Linq;
using Aog.Core.Simulation;
using FluentAssertions;
using Xunit;

namespace Aog.Core.Tests.Simulation;

public static class DeterministicSimRngTests
{
    [Fact]
    public static void SameSeed_ProducesIdenticalSequence()
    {
        var first = new DeterministicSimRng(12345);
        var second = new DeterministicSimRng(12345);

        var firstSeries = Enumerable.Range(0, 100).Select(_ => first.Next()).ToArray();
        var secondSeries = Enumerable.Range(0, 100).Select(_ => second.Next()).ToArray();

        secondSeries.Should().Equal(firstSeries);
    }

    [Fact]
    public static void Reseed_StartsNewDeterministicSequence()
    {
        var rng = new DeterministicSimRng(123);
        _ = rng.Next();
        _ = rng.Next();

        rng.Reseed(123);

        var sequence = Enumerable.Range(0, 5).Select(_ => rng.Next()).ToArray();
        var fresh = new DeterministicSimRng(123);
        var expected = Enumerable.Range(0, 5).Select(_ => fresh.Next()).ToArray();

        sequence.Should().Equal(expected);
    }

    [Fact]
    public static void NextBytes_FillsBufferDeterministically()
    {
        var rng = new DeterministicSimRng(99);
        Span<byte> buffer = stackalloc byte[8];
        rng.NextBytes(buffer);

        var expected = new byte[8];
        var fresh = new DeterministicSimRng(99);
        fresh.NextBytes(expected);

        buffer.ToArray().Should().Equal(expected);
        buffer.ToArray().Should().Contain(b => b != 0);
    }
}
