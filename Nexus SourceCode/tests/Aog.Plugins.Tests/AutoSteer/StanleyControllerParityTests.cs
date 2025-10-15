using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Aog.Plugins.AutoSteer;
using FluentAssertions;
using Xunit;

namespace Aog.Plugins.Tests.AutoSteer;

public sealed class StanleyControllerParityTests
{
    [Fact]
    public void StanleyController_MatchesBaselineSamples()
    {
        var baselinePath = ResolveBaselinePath();
        var json = File.ReadAllText(baselinePath);
        var baseline = JsonSerializer.Deserialize<StanleyParityBaseline>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        baseline.Should().NotBeNull("baseline payload must deserialize");
        baseline!.Path.Should().NotBeEmpty();
        baseline.Samples.Should().NotBeEmpty();

        var path = new List<PathPoint>(baseline.Path.Select(p => new PathPoint(p.X, p.Y)));

        var controller = new AutoSteerLiteController(new AutoSteerLiteSettings
        {
            EnableDynamicLookAhead = false,
            LookAheadDistance = 6.0,
            StanleyGain = 2.2,
            StanleySoftening = 1.0
        })
        {
            Mode = AutoSteerMode.Stanley
        };

        foreach (var sample in baseline.Samples)
        {
            controller.Reset();
            var state = new VehicleState(
                sample.State.X,
                sample.State.Y,
                sample.State.HeadingRadians,
                sample.State.SpeedMetersPerSecond,
                sample.State.WheelbaseMeters);

            var command = controller.ComputeSteeringAngle(state, path);
            command.Should().BeApproximately(sample.ExpectedSteeringRadians, 1e-9, $"sample at x={sample.State.X} y={sample.State.Y}");
        }
    }

    private static string ResolveBaselinePath()
    {
        var path = AppContext.BaseDirectory;
        for (var i = 0; i < 5; i++)
        {
            path = Directory.GetParent(path)?.FullName
                   ?? throw new InvalidOperationException("Unable to locate repository root.");
        }

        return Path.Combine(
            path,
            "tests",
            "Aog.Plugins.Tests",
            "Baselines",
            "StanleyParityBaseline.json");
    }

    private sealed record StanleyParityBaseline(StanleyPathPoint[] Path, StanleySample[] Samples);

    private sealed record StanleyPathPoint(double X, double Y);

    private sealed record StanleySample(StanleyVehicleState State, double ExpectedSteeringRadians);

    private sealed record StanleyVehicleState(
        double X,
        double Y,
        double HeadingRadians,
        double SpeedMetersPerSecond,
        double WheelbaseMeters);
}
