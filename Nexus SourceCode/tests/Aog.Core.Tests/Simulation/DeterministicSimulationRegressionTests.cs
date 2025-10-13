using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Aog.Core.Simulation;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Aog.Core.Tests.Simulation;

public sealed class DeterministicSimulationRegressionTests
{
    private readonly ITestOutputHelper _output;

    public DeterministicSimulationRegressionTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task TenSecondRun_MatchesGoldenCsvAsync()
    {
        var generator = new TenSecondDeterministicScenario();
        var csv = await generator.RunAsync();

        var goldenPath = Path.Combine(AppContext.BaseDirectory, "Simulation", "Data", "DeterministicSimRegression.golden.csv");
        File.Exists(goldenPath).Should().BeTrue("the golden dataset must be present for regression tests");

        var golden = await File.ReadAllTextAsync(goldenPath);
        if (!string.Equals(csv, golden, StringComparison.Ordinal))
        {
            _output.WriteLine("Generated CSV:\n{0}", csv);
        }

        csv.Should().Be(golden);
    }

    private sealed class TenSecondDeterministicScenario
    {
        private const double BaseSpeedMetersPerSecond = 1.65;
        private const double BaseYawRateRadiansPerSecond = 0.045;
        private static readonly TimeSpan Step = TimeSpan.FromMilliseconds(100);
        private const int Seed = 2024;
        private const int Steps = 100;

        public async Task<string> RunAsync()
        {
            var rng = new DeterministicSimRng(Seed);
            var clock = new FixedStepSimClock(Step);
            var state = new VehicleState(0d, 0d, Math.PI / 18d, BaseSpeedMetersPerSecond);
            var builder = new StringBuilder();
            builder.AppendLine("tick,elapsed_ms,x_m,y_m,heading_deg,velocity_mps,yaw_rate_dps,ax_mps2");

            AppendRow(builder, clock.Current, state, BaseYawRateRadiansPerSecond, 0d);

            for (var i = 0; i < Steps; i++)
            {
                var (nextState, yawRate, acceleration) = StepForward(state, rng);
                state = nextState;
                var time = await clock.AdvanceAsync();
                AppendRow(builder, time, state, yawRate, acceleration);
            }

            return builder.ToString();
        }

        private static (VehicleState NextState, double YawRate, double Acceleration) StepForward(VehicleState current, ISimRng rng)
        {
            var dt = Step.TotalSeconds;
            var speedJitter = (rng.NextDouble() - 0.5d) * 0.06d; // +/- 0.03 m/s variation
            var yawJitter = (rng.NextDouble() - 0.5d) * 0.02d;   // +/- 0.01 rad/s variation

            var nextSpeed = BaseSpeedMetersPerSecond + speedJitter;
            var yawRate = BaseYawRateRadiansPerSecond + yawJitter;
            var nextHeading = current.Heading + yawRate * dt;
            var distance = nextSpeed * dt;

            var nextX = current.X + distance * Math.Cos(nextHeading);
            var nextY = current.Y + distance * Math.Sin(nextHeading);
            var acceleration = (nextSpeed - current.Speed) / dt;

            var nextState = new VehicleState(nextX, nextY, nextHeading, nextSpeed);
            return (nextState, yawRate, acceleration);
        }

        private static void AppendRow(StringBuilder builder, SimTime time, VehicleState state, double yawRate, double acceleration)
        {
            builder
                .Append(time.Tick)
                .Append(',')
                .Append(time.Elapsed.TotalMilliseconds.ToString("F0", CultureInfo.InvariantCulture))
                .Append(',')
                .Append(state.X.ToString("F6", CultureInfo.InvariantCulture))
                .Append(',')
                .Append(state.Y.ToString("F6", CultureInfo.InvariantCulture))
                .Append(',')
                .Append((state.Heading * 180d / Math.PI).ToString("F4", CultureInfo.InvariantCulture))
                .Append(',')
                .Append(state.Speed.ToString("F4", CultureInfo.InvariantCulture))
                .Append(',')
                .Append((yawRate * 180d / Math.PI).ToString("F4", CultureInfo.InvariantCulture))
                .Append(',')
                .Append(acceleration.ToString("F5", CultureInfo.InvariantCulture))
                .Append('\n');
        }

        private sealed record VehicleState(double X, double Y, double Heading, double Speed);
    }
}
