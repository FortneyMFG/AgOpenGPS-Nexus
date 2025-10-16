using System.Collections.Generic;
using Aog.Core.Guidance.Axle;
using Aog.Core.Machines.Axle;
using FluentAssertions;
using Xunit;

namespace Aog.Core.Tests.Guidance.Axle;

public sealed class AxleAutomationIntegratorTests
{
    [Fact]
    public void ApplyMode_ForwardsLimitsToSink()
    {
        var profile = new AxleCentricProfile(
            "rig-articulated-tractor",
            "1.0",
            new List<AxleNode> { new("front", AxleNodeRole.Steer, 0.35, 0.1) },
            new List<AxleJoint>(),
            new Dictionary<string, AxleModeProfile>
            {
                ["field"] = new("field", 0.32, 0.12, DriveDirectionPolicy.Bidirectional)
            },
            new LateMeasurementPolicy(true, 50),
            new CompatibilityGuard(new System.Version(1, 0), "1.0"),
            deterministicSeed: 42,
            contentHash: "abcd");

        var sink = new RecordingSink();
        var integrator = new AxleAutomationIntegrator(sink);

        integrator.ApplyMode(profile, "field");

        sink.LastSnapshot.Should().NotBeNull();
        sink.LastSnapshot!.ModeId.Should().Be("field");
        sink.LastSnapshot!.CurvatureLimit.Should().Be(0.32);
        sink.LastSnapshot!.MinimumTurnRadiusMeters.Should().BeApproximately(1 / 0.32, 1e-6);
        sink.LastSnapshot!.SlipLimit.Should().Be(0.12);
        sink.LastSnapshot!.DriveDirectionPolicy.Should().Be(DriveDirectionPolicy.Bidirectional);
        sink.LastSnapshot!.DeterministicSeed.Should().Be(42);
        sink.LastSnapshot!.ProfileHash.Should().Be("abcd");
    }

    private sealed class RecordingSink : IAutomationModeSink
    {
        public AutomationModeSnapshot? LastSnapshot { get; private set; }

        public void PublishModeLimits(AutomationModeSnapshot snapshot)
        {
            LastSnapshot = snapshot;
        }
    }
}
