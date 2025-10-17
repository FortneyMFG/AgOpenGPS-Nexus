using System;
using System.Linq;
using Aog.Core.Guidance;
using Aog.Core.V1;
using FluentAssertions;
using Google.Protobuf.WellKnownTypes;
using Xunit;

namespace Aog.Core.Tests.Guidance;

public sealed class GuidanceLaneContractTests
{
    [Fact]
    public void GuidanceLane_RoundTripsThroughProto()
    {
        var metadata = new GuidanceLaneMetadata(
            laneId: "lane-001",
            template: GuidanceLaneTemplate.Straight,
            label: "North-South Reference",
            fieldId: "field-17",
            jobId: "job-2024",
            sessionId: "session-5");

        var passPoints = new[]
        {
            new GuidanceLanePoint(0, 0, 0, 0),
            new GuidanceLanePoint(10, 0, 0, 0),
            new GuidanceLanePoint(20, 0, 0, 0)
        };

        var pass = new GuidanceLanePass(index: 0, passPoints, headingRadians: 0, signedDistanceMeters: 0);

        var constraintState = new GuidanceLaneConstraintState(
            zoneMask: new PoseZoneMask { InsideBoundary = true, InsideHeadland = true },
            hasBlockingConstraint: false,
            insideHeadland: true,
            distanceToConstraintMeters: 8.5);

        var preview = new GuidanceLanePreview(
            crossTrackErrorMeters: 0.12,
            headingErrorRadians: -0.05,
            lookAheadDistanceMeters: 6.4,
            targetPoint: new GuidanceLanePoint(22, 0, 0.02, 0.0015),
            controllerOutput: 0.18,
            controllerEnabled: true,
            targetCurvaturePerMeter: 0.0015,
            constraint: constraintState);

        var lane = new GuidanceLane(
            metadata,
            laneSpacingMeters: 3.0,
            implementWidthMeters: 4.5,
            overlapMeters: 0.2,
            nudgeMeters: 0.1,
            extensionLengthMeters: 200,
            baseHeadingRadians: 0,
            passes: new[] { pass },
            preview: preview);

        var header = new Header
        {
            Sequence = 42,
            Timestamp = Timestamp.FromDateTime(DateTime.SpecifyKind(new DateTime(2024, 1, 1, 8, 30, 0), DateTimeKind.Utc)),
            Frame = "field",
            Source = "planner"
        };

        var publish = new GuidanceLanePublish(header, lane);

        var proto = publish.ToProto();
        proto.Header.Sequence.Should().Be(42);
        proto.Metadata.LaneId.Should().Be("lane-001");
        proto.Metadata.Template.Should().Be(Aog.Guidance.V1.LaneTemplate.Straight);
        proto.LaneSpacingM.Should().BeApproximately(3.0, 1e-9);
        proto.Passes.Should().HaveCount(1);
        proto.Passes[0].Points.Select(p => p.EastingM).Should().ContainInOrder(0, 10, 20);
        proto.Preview.Should().NotBeNull();
        proto.Preview.Target.Point.EastingM.Should().BeApproximately(22, 1e-9);
        proto.Preview.Constraint.ZoneMask.InsideHeadland.Should().BeTrue();
        proto.Preview.Constraint.DistanceToConstraintM.Should().BeApproximately(8.5, 1e-9);

        var roundTrip = proto.ToModel();
        roundTrip.Header.Sequence.Should().Be(publish.Header.Sequence);
        roundTrip.Header.Source.Should().Be(publish.Header.Source);
        roundTrip.Lane.Metadata.LaneId.Should().Be(metadata.LaneId);
        roundTrip.Lane.Metadata.Template.Should().Be(GuidanceLaneTemplate.Straight);
        roundTrip.Lane.Passes.Should().HaveCount(1);
        roundTrip.Lane.Passes[0].Points.Select(p => p.EastingMeters).Should().ContainInOrder(0, 10, 20);
        roundTrip.Lane.Preview.Should().NotBeNull();
        roundTrip.Lane.Preview!.Constraint!.InsideHeadland.Should().BeTrue();
        roundTrip.Lane.Preview.Constraint.DistanceToConstraintMeters.Should().BeApproximately(8.5, 1e-9);
        roundTrip.Lane.Preview.TargetPoint.EastingMeters.Should().BeApproximately(22, 1e-9);
        roundTrip.Lane.Preview.TargetCurvaturePerMeter.Should().BeApproximately(0.0015, 1e-12);

        var frameHeader = new Header
        {
            Sequence = 100,
            Timestamp = Timestamp.FromDateTime(DateTime.SpecifyKind(new DateTime(2024, 1, 1, 8, 31, 0), DateTimeKind.Utc)),
            Frame = "field",
            Source = "planner"
        };

        var frame = new GuidanceLaneFramePublish(frameHeader, new[] { publish });
        var frameProto = frame.ToProto();
        frameProto.Header.Sequence.Should().Be(100);
        frameProto.Lanes.Should().HaveCount(1);

        var frameRoundTrip = frameProto.ToModel();
        frameRoundTrip.Header.Sequence.Should().Be(100);
        frameRoundTrip.Lanes.Should().HaveCount(1);
        frameRoundTrip.Lanes[0].Lane.Metadata.LaneId.Should().Be("lane-001");
    }
}
