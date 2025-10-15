using System.Collections.Generic;
using Aog.Core.Guidance;
using Aog.Core.V1;
using Aog.Plugins.AutoSteer;

namespace Aog.Plugins.Guidance;

/// <summary>
/// Produces guidance previews by combining the turn planner with the pure pursuit controller.
/// </summary>
public sealed class GuidanceLanePlanner
{
    private readonly GuidanceTurnPlanner _turnPlanner;
    private readonly PurePursuitController _controller;

    public GuidanceLanePlanner(GuidanceTurnPlanner? turnPlanner = null, PurePursuitController? controller = null)
    {
        _turnPlanner = turnPlanner ?? new GuidanceTurnPlanner();
        _controller = controller ?? new PurePursuitController();
    }

    public GuidanceTurnPlanner TurnPlanner => _turnPlanner;

    public PurePursuitController Controller => _controller;

    public (GuidanceTurnPlan Plan, GuidanceLanePreview Preview) BuildTurnPreview(
        VehicleState vehicleState,
        IReadOnlyList<PathPoint> currentPass,
        IReadOnlyList<PathPoint> nextPass,
        ConstraintLookAheadContext? constraintContext = null)
    {
        var plan = _turnPlanner.Plan(currentPass, nextPass);
        var steering = _controller.ComputeSteeringAngle(vehicleState, plan.Path);
        var preview = _controller.LastPreview;

        GuidanceLaneConstraintState? constraintState = null;
        if (constraintContext is not null)
        {
            var context = constraintContext.Value;
            constraintState = new GuidanceLaneConstraintState(
                new PoseZoneMask
                {
                    InsideBoundary = true,
                    InsideHeadland = context.InsideHeadland
                },
                context.HasBlockingConstraint,
                context.InsideHeadland,
                context.DistanceToConstraintMeters);
        }

        var lanePreview = new GuidanceLanePreview(
            preview.CrossTrackError,
            preview.HeadingErrorRadians,
            preview.LookAheadDistanceMeters,
            new GuidanceLanePoint(preview.TargetX, preview.TargetY, preview.TargetHeading, preview.TargetCurvaturePerMeter),
            steering,
            controllerEnabled: true,
            preview.TargetCurvaturePerMeter,
            constraintState);

        return (plan, lanePreview);
    }
}
