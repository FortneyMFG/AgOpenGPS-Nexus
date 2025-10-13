using System;
using System.Collections.Generic;
using Aog.Plugins.AutoSteer;
using FluentAssertions;
using Xunit;

namespace Aog.Plugins.Tests;

public sealed class AutoSteerLiteControllerTests
{
    private static List<PathPoint> CreateStraightPath(double lengthMetres = 120, double spacingMetres = 2)
    {
        var points = new List<PathPoint>();
        for (double x = 0; x <= lengthMetres; x += spacingMetres)
        {
            points.Add(new PathPoint(x, 0));
        }

        return points;
    }

    [Theory]
    [InlineData(AutoSteerMode.PurePursuit)]
    [InlineData(AutoSteerMode.Stanley)]
    public void StraightLineSimulation_TracksAbLine(AutoSteerMode mode)
    {
        var controller = new AutoSteerLiteController(new AutoSteerLiteSettings
        {
            LookAheadDistance = 6,
            StanleyGain = 3,
            StanleySoftening = 1.2,
            SteeringAngleLimitRadians = Math.PI / 180 * 35
        })
        {
            Mode = mode
        };

        var path = CreateStraightPath();
        var state = new VehicleState(x: 0, y: 2, headingRadians: AutoSteerMath.NormalizeAngle(5 * Math.PI / 180), speedMetersPerSecond: 5, wheelbaseMeters: 3);
        const double dt = 0.1;

        for (var i = 0; i < 300; i++)
        {
            var steering = controller.ComputeSteeringAngle(state, path);
            steering.Should().BeLessOrEqualTo(controller.Settings.SteeringAngleLimitRadians + 1e-6);
            steering.Should().BeGreaterOrEqualTo(-controller.Settings.SteeringAngleLimitRadians - 1e-6);
            state = state.Advance(steering, dt);
        }

        Math.Abs(state.Y).Should().BeLessThan(0.1);
        AutoSteerMath.NormalizeAngle(state.HeadingRadians).Should().BeApproximately(0, 1e-2);
    }

    [Fact]
    public void SwitchingModes_AffectsCommand()
    {
        var controller = new AutoSteerLiteController();
        var path = CreateStraightPath(20, 1);
        var state = new VehicleState(5, -1.2, headingRadians: 0.1, speedMetersPerSecond: 4, wheelbaseMeters: 2.5);

        var pureCommand = controller.ComputeSteeringAngle(state, path);
        controller.Mode = AutoSteerMode.Stanley;
        var stanleyCommand = controller.ComputeSteeringAngle(state, path);

        stanleyCommand.Should().NotBeApproximately(pureCommand, 1e-6);
    }

    [Fact]
    public void ComputeSteeringAngle_InvalidPath_Throws()
    {
        var controller = new AutoSteerLiteController();
        var path = new List<PathPoint> { new PathPoint(0, 0) };
        var state = new VehicleState(0, 0, 0, 3, 2);

        var act = () => controller.ComputeSteeringAngle(state, path);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void VehicleState_Advance_RespectsWheelbase()
    {
        var state = new VehicleState(0, 0, 0, speedMetersPerSecond: 5, wheelbaseMeters: 3);
        var next = state.Advance(steeringAngleRadians: Math.PI / 12, timeStepSeconds: 0.5);

        next.X.Should().BeGreaterThan(0);
        next.Y.Should().BeGreaterThan(0);
        next.HeadingRadians.Should().NotBe(0);
    }
}
