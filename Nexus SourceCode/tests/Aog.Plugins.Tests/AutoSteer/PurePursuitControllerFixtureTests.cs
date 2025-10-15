using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Aog.Plugins.AutoSteer;
using FluentAssertions;
using Xunit;

namespace Aog.Plugins.Tests.AutoSteer;

public sealed class PurePursuitControllerFixtureTests
{
    [Fact]
    public void BaselineFixtures_MatchExpectedSteering()
    {
        var fixtures = LoadFixtures();
        fixtures.Should().NotBeEmpty();

        var path = BuildStraightPath(60, 1.0);

        foreach (var fixture in fixtures)
        {
            var controller = new PurePursuitController(fixture.LookAhead)
            {
                SteeringAngleLimitRadians = Math.PI / 2
            };

            var state = new VehicleState(
                fixture.State.X,
                fixture.State.Y,
                DegreesToRadians(fixture.State.HeadingDeg),
                speedMetersPerSecond: 5.0,
                wheelbaseMeters: fixture.Wheelbase);

            var steering = controller.ComputeSteeringAngle(state, path);
            steering.Should().BeApproximately(fixture.ExpectedSteeringRadians, 1e-9);

            var preview = controller.LastPreview;
            preview.TargetCurvaturePerMeter.Should().BeApproximately(fixture.ExpectedCurvature, 1e-9);
            preview.HeadingErrorRadians.Should().BeApproximately(fixture.ExpectedHeadingError, 1e-9);
            preview.LookAheadDistanceMeters.Should().BeApproximately(fixture.LookAhead, 1e-9);
        }
    }

    private static IReadOnlyList<PathPoint> BuildStraightPath(double lengthMeters, double spacingMeters)
    {
        var points = new List<PathPoint>();
        for (double x = 0; x <= lengthMeters; x += spacingMeters)
        {
            points.Add(new PathPoint(x, 0));
        }

        if (points.Count == 0 || Math.Abs(points[^1].X - lengthMeters) > 1e-6)
        {
            points.Add(new PathPoint(lengthMeters, 0));
        }

        return points;
    }

    private static IReadOnlyList<PurePursuitFixture> LoadFixtures()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Baselines", "PurePursuitFixtures.json");
        var json = File.ReadAllText(path);
        var fixtures = JsonSerializer.Deserialize<List<PurePursuitFixture>>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return fixtures ?? Array.Empty<PurePursuitFixture>();
    }

    private static double DegreesToRadians(double degrees) => Math.PI / 180d * degrees;

    private sealed record PurePursuitFixture(
        FixtureState State,
        double LookAhead,
        double Wheelbase,
        double ExpectedSteeringRadians,
        double ExpectedCurvature,
        double ExpectedHeadingError);

    private sealed record FixtureState(double X, double Y, double HeadingDeg);
}
