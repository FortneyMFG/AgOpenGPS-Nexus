using System;
using Aog.Plugins.Sections;
using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Aog.Plugins.Tests.Sections;

public sealed class VariableRateTransportGuardTests
{
    [Fact]
    public void Arm_ShouldValidateInputs()
    {
        var guard = new VariableRateTransportGuard();
        Action actLayer = () => guard.Arm(string.Empty, "hash");
        Action actHash = () => guard.Arm("layer", " ");

        actLayer.Should().Throw<ArgumentException>();
        actHash.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void EnsureHeartbeatFresh_ShouldThrowWhenExpired()
    {
        var clock = new FakeTimeProvider();
        var guard = new VariableRateTransportGuard(timeProvider: clock);
        guard.Arm("layer-1", "hash-1");

        clock.Advance(TimeSpan.FromMilliseconds(400));
        Action act = () => guard.EnsureHeartbeatFresh(clock.GetUtcNow());
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void RecordHeartbeat_ShouldValidateHandshake()
    {
        var clock = new FakeTimeProvider();
        var guard = new VariableRateTransportGuard(timeProvider: clock);
        guard.Arm("layer-1", "hash-1");

        Action mismatchLayer = () => guard.RecordHeartbeat("layer-2", "hash-1");
        mismatchLayer.Should().Throw<InvalidOperationException>()
            .WithMessage("*mismatch*");

        Action mismatchHash = () => guard.RecordHeartbeat("layer-1", "hash-2");
        mismatchHash.Should().Throw<InvalidOperationException>()
            .WithMessage("*hash*");

        guard.RecordHeartbeat("layer-1", "hash-1", clock.GetUtcNow());
        guard.EnsureHeartbeatFresh(clock.GetUtcNow() + TimeSpan.FromMilliseconds(100));
    }
}
