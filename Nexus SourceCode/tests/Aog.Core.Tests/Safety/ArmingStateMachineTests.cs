using System;
using Aog.Core.Safety;
using FluentAssertions;
using Xunit;

namespace Aog.Core.Tests.Safety;

public sealed class ArmingStateMachineTests
{
    [Fact]
    public void Defaults_ToDisarmedAndProfileInvalid()
    {
        var machine = new ArmingStateMachine();

        machine.State.Should().Be(ArmingState.Disarmed);
        machine.HasValidProfile.Should().BeFalse();
        machine.CanEmitOutputs.Should().BeFalse();

        var gate = machine.EvaluateOutputs();
        gate.Allowed.Should().BeFalse();
        gate.Reason.Should().Contain("disarmed");
    }

    [Fact]
    public void Arm_FailsWhenProfileInvalid()
    {
        var machine = new ArmingStateMachine();

        var result = machine.Arm();

        result.Success.Should().BeFalse();
        result.StateChanged.Should().BeFalse();
        result.FailureReason.Should().Contain("profile");
        machine.State.Should().Be(ArmingState.Disarmed);
    }

    [Fact]
    public void Arm_SucceedsWhenProfileValid()
    {
        var machine = new ArmingStateMachine();
        var update = machine.SetProfileValidity(true);

        update.Changed.Should().BeTrue();
        update.CausedDisarm.Should().BeFalse();

        var result = machine.Arm();

        result.Success.Should().BeTrue();
        result.StateChanged.Should().BeTrue();
        machine.State.Should().Be(ArmingState.Armed);
        machine.CanEmitOutputs.Should().BeTrue();
    }

    [Fact]
    public void Arm_IsIdempotentWhenAlreadyArmed()
    {
        var machine = new ArmingStateMachine();
        machine.SetProfileValidity(true);
        machine.Arm().EnsureSuccess();

        var repeat = machine.Arm();

        repeat.Success.Should().BeTrue();
        repeat.StateChanged.Should().BeFalse();
        machine.State.Should().Be(ArmingState.Armed);
    }

    [Fact]
    public void EnsureCanEmitOutputs_ThrowsWhenDisarmed()
    {
        var machine = new ArmingStateMachine();

        Action act = machine.EnsureCanEmitOutputs;
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*disarmed*");
    }

    [Fact]
    public void InvalidatingProfile_DisarmsAndBlocksOutputs()
    {
        var machine = new ArmingStateMachine();
        machine.SetProfileValidity(true);
        machine.Arm().EnsureSuccess();

        var update = machine.SetProfileValidity(false);

        update.Changed.Should().BeTrue();
        update.CausedDisarm.Should().BeTrue();
        update.StateChanged.Should().BeTrue();
        machine.State.Should().Be(ArmingState.Disarmed);
        machine.CanEmitOutputs.Should().BeFalse();

        var gate = machine.EvaluateOutputs();
        gate.Allowed.Should().BeFalse();
        gate.Reason.Should().Contain("profile");
    }

    [Fact]
    public void Disarm_ReturnsNoChangeWhenAlreadyDisarmed()
    {
        var machine = new ArmingStateMachine();

        var transition = machine.Disarm();

        transition.Success.Should().BeTrue();
        transition.StateChanged.Should().BeFalse();
        transition.PreviousState.Should().Be(ArmingState.Disarmed);
        transition.CurrentState.Should().Be(ArmingState.Disarmed);
    }
}
