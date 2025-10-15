using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Aog.Core.V1;
using Aog.Guidance.V1;
using Google.Protobuf;

namespace Aog.Core.Guidance;

/// <summary>
/// Canonical lane templates used by the guidance planner.
/// </summary>
public enum GuidanceLaneTemplate
{
    Straight,
    Curve,
    Adaptive
}

/// <summary>
/// Static metadata describing a published guidance lane.
/// </summary>
public sealed record GuidanceLaneMetadata
{
    public GuidanceLaneMetadata(
        string laneId,
        GuidanceLaneTemplate template,
        string label,
        string? fieldId = null,
        string? jobId = null,
        string? sessionId = null)
    {
        if (string.IsNullOrWhiteSpace(laneId))
        {
            throw new ArgumentException("Lane identifier must be provided.", nameof(laneId));
        }

        LaneId = laneId;
        Template = template;
        Label = label ?? string.Empty;
        FieldId = string.IsNullOrWhiteSpace(fieldId) ? null : fieldId;
        JobId = string.IsNullOrWhiteSpace(jobId) ? null : jobId;
        SessionId = string.IsNullOrWhiteSpace(sessionId) ? null : sessionId;
    }

    public string LaneId { get; }

    public GuidanceLaneTemplate Template { get; }

    public string Label { get; }

    public string? FieldId { get; }

    public string? JobId { get; }

    public string? SessionId { get; }
}

/// <summary>
/// Represents a sampled point along a lane pass.
/// </summary>
public sealed record GuidanceLanePoint(double EastingMeters, double NorthingMeters, double HeadingRadians, double? CurvaturePerMeter = null);

/// <summary>
/// Captures the constraint state active for the current preview.
/// </summary>
public sealed record GuidanceLaneConstraintState
{
    public GuidanceLaneConstraintState(
        PoseZoneMask? zoneMask,
        bool hasBlockingConstraint,
        bool insideHeadland,
        double? distanceToConstraintMeters)
    {
        ZoneMask = zoneMask?.Clone();
        HasBlockingConstraint = hasBlockingConstraint;
        InsideHeadland = insideHeadland;
        DistanceToConstraintMeters = distanceToConstraintMeters;
    }

    public PoseZoneMask? ZoneMask { get; }

    public bool HasBlockingConstraint { get; }

    public bool InsideHeadland { get; }

    public double? DistanceToConstraintMeters { get; }
}

/// <summary>
/// Planner preview context describing the active vehicle state.
/// </summary>
public sealed record GuidanceLanePreview
{
    public GuidanceLanePreview(
        double crossTrackErrorMeters,
        double headingErrorRadians,
        double lookAheadDistanceMeters,
        GuidanceLanePoint targetPoint,
        double controllerOutput,
        bool controllerEnabled,
        double? targetCurvaturePerMeter = null,
        GuidanceLaneConstraintState? constraint = null)
    {
        if (double.IsNaN(crossTrackErrorMeters))
        {
            throw new ArgumentException("Cross-track error must be a valid number.", nameof(crossTrackErrorMeters));
        }

        if (lookAheadDistanceMeters < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(lookAheadDistanceMeters), lookAheadDistanceMeters, "Look-ahead distance must be non-negative.");
        }

        TargetPoint = targetPoint ?? throw new ArgumentNullException(nameof(targetPoint));
        CrossTrackErrorMeters = crossTrackErrorMeters;
        HeadingErrorRadians = headingErrorRadians;
        LookAheadDistanceMeters = lookAheadDistanceMeters;
        ControllerOutput = controllerOutput;
        ControllerEnabled = controllerEnabled;
        TargetCurvaturePerMeter = targetCurvaturePerMeter;
        Constraint = constraint;
    }

    public double CrossTrackErrorMeters { get; }

    public double HeadingErrorRadians { get; }

    public double LookAheadDistanceMeters { get; }

    public GuidanceLanePoint TargetPoint { get; }

    public double ControllerOutput { get; }

    public bool ControllerEnabled { get; }

    public double? TargetCurvaturePerMeter { get; }

    public GuidanceLaneConstraintState? Constraint { get; }
}

/// <summary>
/// Geometry for a single pass belonging to a guidance lane.
/// </summary>
public sealed record GuidanceLanePass
{
    public GuidanceLanePass(int index, IReadOnlyList<GuidanceLanePoint> points, double headingRadians, double signedDistanceMeters)
    {
        if (points is null)
        {
            throw new ArgumentNullException(nameof(points));
        }

        if (points.Count == 0)
        {
            throw new ArgumentException("Lane passes require at least one sampled point.", nameof(points));
        }

        Index = index;
        Points = new ReadOnlyCollection<GuidanceLanePoint>(points.ToArray());
        HeadingRadians = headingRadians;
        SignedDistanceMeters = signedDistanceMeters;
    }

    public int Index { get; }

    public IReadOnlyList<GuidanceLanePoint> Points { get; }

    public double HeadingRadians { get; }

    public double SignedDistanceMeters { get; }
}

/// <summary>
/// Complete guidance lane definition and preview.
/// </summary>
public sealed record GuidanceLane
{
    public GuidanceLane(
        GuidanceLaneMetadata metadata,
        double laneSpacingMeters,
        double implementWidthMeters,
        double overlapMeters,
        double nudgeMeters,
        double extensionLengthMeters,
        double baseHeadingRadians,
        IReadOnlyList<GuidanceLanePass> passes,
        GuidanceLanePreview? preview = null)
    {
        Metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));

        if (passes is null)
        {
            throw new ArgumentNullException(nameof(passes));
        }

        var passArray = passes.ToArray();
        if (passArray.Length == 0)
        {
            throw new ArgumentException("Guidance lanes must contain at least one pass.", nameof(passes));
        }

        LaneSpacingMeters = laneSpacingMeters;
        ImplementWidthMeters = implementWidthMeters;
        OverlapMeters = overlapMeters;
        NudgeMeters = nudgeMeters;
        ExtensionLengthMeters = extensionLengthMeters;
        BaseHeadingRadians = baseHeadingRadians;
        Passes = new ReadOnlyCollection<GuidanceLanePass>(passArray);
        Preview = preview;
    }

    public GuidanceLaneMetadata Metadata { get; }

    public double LaneSpacingMeters { get; }

    public double ImplementWidthMeters { get; }

    public double OverlapMeters { get; }

    public double NudgeMeters { get; }

    public double ExtensionLengthMeters { get; }

    public double BaseHeadingRadians { get; }

    public IReadOnlyList<GuidanceLanePass> Passes { get; }

    public GuidanceLanePreview? Preview { get; }
}

/// <summary>
/// Wrapper carrying the protobuf header alongside a lane payload.
/// </summary>
public sealed record GuidanceLanePublish
{
    public GuidanceLanePublish(Header header, GuidanceLane lane)
    {
        Header = header?.Clone() ?? throw new ArgumentNullException(nameof(header));
        Lane = lane ?? throw new ArgumentNullException(nameof(lane));
    }

    public Header Header { get; }

    public GuidanceLane Lane { get; }
}

/// <summary>
/// Bundled lane publish frame mirroring <see cref="GuidanceLaneFrame"/>.
/// </summary>
public sealed record GuidanceLaneFramePublish
{
    public GuidanceLaneFramePublish(Header header, IReadOnlyList<GuidanceLanePublish> lanes)
    {
        Header = header?.Clone() ?? throw new ArgumentNullException(nameof(header));

        if (lanes is null)
        {
            throw new ArgumentNullException(nameof(lanes));
        }

        Lanes = new ReadOnlyCollection<GuidanceLanePublish>(lanes.Select(l => l ?? throw new ArgumentException("Lane entries cannot be null.", nameof(lanes))).ToArray());
    }

    public Header Header { get; }

    public IReadOnlyList<GuidanceLanePublish> Lanes { get; }
}

/// <summary>
/// Conversion helpers between domain models and generated protobuf messages.
/// </summary>
public static class GuidanceLaneContractsExtensions
{
    public static GuidanceLanePublish ToModel(this Aog.Guidance.V1.GuidanceLane message)
    {
        if (message is null)
        {
            throw new ArgumentNullException(nameof(message));
        }

        if (message.Header is null)
        {
            throw new InvalidOperationException("Guidance lane message must include a header.");
        }

        if (message.Metadata is null)
        {
            throw new InvalidOperationException("Guidance lane message missing metadata.");
        }

        var metadata = message.Metadata.ToModel();
        var passes = message.Passes.Select(pass => pass.ToModel()).ToArray();
        var preview = message.Preview?.ToModel();

        var lane = new GuidanceLane(
            metadata,
            message.LaneSpacingM,
            message.ImplementWidthM,
            message.OverlapM,
            message.NudgeM,
            message.ExtensionLengthM,
            message.BaseHeadingRad,
            passes,
            preview);

        return new GuidanceLanePublish(message.Header.Clone(), lane);
    }

    public static Aog.Guidance.V1.GuidanceLane ToProto(this GuidanceLanePublish publish)
    {
        if (publish is null)
        {
            throw new ArgumentNullException(nameof(publish));
        }

        var message = new Aog.Guidance.V1.GuidanceLane
        {
            Header = publish.Header.Clone(),
            Metadata = publish.Lane.Metadata.ToProto(),
            LaneSpacingM = publish.Lane.LaneSpacingMeters,
            ImplementWidthM = publish.Lane.ImplementWidthMeters,
            OverlapM = publish.Lane.OverlapMeters,
            NudgeM = publish.Lane.NudgeMeters,
            ExtensionLengthM = publish.Lane.ExtensionLengthMeters,
            BaseHeadingRad = publish.Lane.BaseHeadingRadians
        };

        message.Passes.AddRange(publish.Lane.Passes.Select(pass => pass.ToProto()));

        if (publish.Lane.Preview is { } preview)
        {
            message.Preview = preview.ToProto();
        }

        return message;
    }

    public static GuidanceLaneFramePublish ToModel(this GuidanceLaneFrame frame)
    {
        if (frame is null)
        {
            throw new ArgumentNullException(nameof(frame));
        }

        if (frame.Header is null)
        {
            throw new InvalidOperationException("Guidance lane frame must include a header.");
        }

        var lanes = frame.Lanes.Select(lane => lane.ToModel()).ToArray();
        return new GuidanceLaneFramePublish(frame.Header.Clone(), lanes);
    }

    public static GuidanceLaneFrame ToProto(this GuidanceLaneFramePublish frame)
    {
        if (frame is null)
        {
            throw new ArgumentNullException(nameof(frame));
        }

        var message = new GuidanceLaneFrame
        {
            Header = frame.Header.Clone()
        };

        message.Lanes.AddRange(frame.Lanes.Select(lane => lane.ToProto()));
        return message;
    }

    private static GuidanceLaneMetadata ToModel(this LaneMetadata metadata)
    {
        var template = metadata.Template switch
        {
            LaneTemplate.LaneTemplateStraight => GuidanceLaneTemplate.Straight,
            LaneTemplate.LaneTemplateCurve => GuidanceLaneTemplate.Curve,
            LaneTemplate.LaneTemplateAdaptive => GuidanceLaneTemplate.Adaptive,
            _ => throw new InvalidOperationException($"Unsupported lane template '{metadata.Template}'.")
        };

        return new GuidanceLaneMetadata(metadata.LaneId, template, metadata.Label, metadata.FieldId, metadata.JobId, metadata.SessionId);
    }

    private static LaneMetadata ToProto(this GuidanceLaneMetadata metadata)
    {
        var proto = new LaneMetadata
        {
            LaneId = metadata.LaneId,
            Template = metadata.Template switch
            {
                GuidanceLaneTemplate.Straight => LaneTemplate.LaneTemplateStraight,
                GuidanceLaneTemplate.Curve => LaneTemplate.LaneTemplateCurve,
                GuidanceLaneTemplate.Adaptive => LaneTemplate.LaneTemplateAdaptive,
                _ => throw new InvalidOperationException($"Unsupported lane template '{metadata.Template}'.")
            },
            Label = metadata.Label
        };

        if (metadata.FieldId is { Length: > 0 })
        {
            proto.FieldId = metadata.FieldId;
        }

        if (metadata.JobId is { Length: > 0 })
        {
            proto.JobId = metadata.JobId;
        }

        if (metadata.SessionId is { Length: > 0 })
        {
            proto.SessionId = metadata.SessionId;
        }

        return proto;
    }

    private static GuidanceLanePass ToModel(this LanePass pass)
    {
        var points = pass.Points.Select(point => point.ToModel()).ToArray();
        return new GuidanceLanePass(pass.Index, points, pass.HeadingRad, pass.SignedDistanceM);
    }

    private static LanePass ToProto(this GuidanceLanePass pass)
    {
        var proto = new LanePass
        {
            Index = pass.Index,
            HeadingRad = pass.HeadingRadians,
            SignedDistanceM = pass.SignedDistanceMeters
        };

        proto.Points.AddRange(pass.Points.Select(point => point.ToProto()));
        return proto;
    }

    private static GuidanceLanePoint ToModel(this LanePoint point)
    {
        double? curvature = point.HasCurvaturePerMeter ? point.CurvaturePerMeter : null;
        return new GuidanceLanePoint(point.EastingM, point.NorthingM, point.HeadingRad, curvature);
    }

    private static LanePoint ToProto(this GuidanceLanePoint point)
    {
        var proto = new LanePoint
        {
            EastingM = point.EastingMeters,
            NorthingM = point.NorthingMeters,
            HeadingRad = point.HeadingRadians
        };

        if (point.CurvaturePerMeter.HasValue)
        {
            proto.CurvaturePerMeter = point.CurvaturePerMeter.Value;
        }

        return proto;
    }

    private static GuidanceLanePreview ToModel(this LanePreview preview)
    {
        if (preview.Target is null)
        {
            throw new InvalidOperationException("Lane preview missing target information.");
        }

        if (preview.Target.Point is null)
        {
            throw new InvalidOperationException("Lane preview target missing point geometry.");
        }

        var targetPoint = preview.Target.Point.ToModel();
        var targetCurvature = preview.Target.HasCurvaturePerMeter ? preview.Target.CurvaturePerMeter : targetPoint.CurvaturePerMeter;
        var constraint = preview.Constraint?.ToModel();

        return new GuidanceLanePreview(
            preview.CrossTrackErrorM,
            preview.HeadingErrorRad,
            preview.LookaheadDistanceM,
            targetPoint,
            preview.ControllerOutput,
            preview.ControllerEnabled,
            targetCurvature,
            constraint);
    }

    private static LanePreview ToProto(this GuidanceLanePreview preview)
    {
        var proto = new LanePreview
        {
            CrossTrackErrorM = preview.CrossTrackErrorMeters,
            HeadingErrorRad = preview.HeadingErrorRadians,
            LookaheadDistanceM = preview.LookAheadDistanceMeters,
            ControllerOutput = preview.ControllerOutput,
            ControllerEnabled = preview.ControllerEnabled,
            Target = new PreviewTarget
            {
                Point = preview.TargetPoint.ToProto()
            }
        };

        if (preview.TargetCurvaturePerMeter.HasValue)
        {
            proto.Target.CurvaturePerMeter = preview.TargetCurvaturePerMeter.Value;
        }

        if (preview.Constraint is { } constraint)
        {
            proto.Constraint = constraint.ToProto();
        }

        return proto;
    }

    private static GuidanceLaneConstraintState? ToModel(this LaneConstraintState? state)
    {
        if (state is null)
        {
            return null;
        }

        var hasZoneMask = state.ZoneMask is not null;
        var hasDistance = state.HasDistanceToConstraintM;
        if (!hasZoneMask && !state.HasBlockingConstraint && !state.InsideHeadland && !hasDistance)
        {
            return null;
        }

        var zoneMask = state.ZoneMask?.Clone();
        var distance = hasDistance ? state.DistanceToConstraintM : (double?)null;
        return new GuidanceLaneConstraintState(zoneMask, state.HasBlockingConstraint, state.InsideHeadland, distance);
    }

    private static LaneConstraintState ToProto(this GuidanceLaneConstraintState state)
    {
        var proto = new LaneConstraintState
        {
            HasBlockingConstraint = state.HasBlockingConstraint,
            InsideHeadland = state.InsideHeadland
        };

        if (state.ZoneMask is { } mask)
        {
            proto.ZoneMask = mask.Clone();
        }

        if (state.DistanceToConstraintMeters.HasValue)
        {
            proto.DistanceToConstraintM = state.DistanceToConstraintMeters.Value;
        }

        return proto;
    }
}
