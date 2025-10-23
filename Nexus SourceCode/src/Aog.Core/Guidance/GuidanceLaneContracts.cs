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
    /// <summary>Lane geometry comprised of straight, parallel passes.</summary>
    Straight,
    /// <summary>Lane geometry derived from curved passes.</summary>
    Curve,
    /// <summary>Lane geometry that adapts dynamically to the field environment.</summary>
    Adaptive
}

/// <summary>
/// Static metadata describing a published guidance lane.
/// </summary>
public sealed record GuidanceLaneMetadata
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GuidanceLaneMetadata"/> record.
    /// </summary>
    /// <param name="laneId">Unique identifier of the lane.</param>
    /// <param name="template">Template describing the lane geometry.</param>
    /// <param name="label">Human-readable lane label.</param>
    /// <param name="fieldId">Optional field identifier associated with the lane.</param>
    /// <param name="jobId">Optional job identifier associated with the lane.</param>
    /// <param name="sessionId">Optional session identifier associated with the lane.</param>
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

    /// <summary>Gets the unique identifier of the lane.</summary>
    public string LaneId { get; }

    /// <summary>Gets the template describing the lane geometry.</summary>
    public GuidanceLaneTemplate Template { get; }

    /// <summary>Gets the human-readable lane label.</summary>
    public string Label { get; }

    /// <summary>Gets the optional field identifier associated with the lane.</summary>
    public string? FieldId { get; }

    /// <summary>Gets the optional job identifier associated with the lane.</summary>
    public string? JobId { get; }

    /// <summary>Gets the optional session identifier associated with the lane.</summary>
    public string? SessionId { get; }
}

/// <summary>
/// Represents a sampled point along a lane pass.
/// </summary>
/// <param name="EastingMeters">Point easting in metres.</param>
/// <param name="NorthingMeters">Point northing in metres.</param>
/// <param name="HeadingRadians">Vehicle heading at the sample in radians.</param>
/// <param name="CurvaturePerMeter">Optional curvature sample at the point.</param>
public sealed record GuidanceLanePoint(double EastingMeters, double NorthingMeters, double HeadingRadians, double? CurvaturePerMeter = null);

/// <summary>
/// Captures the constraint state active for the current preview.
/// </summary>
public sealed record GuidanceLaneConstraintState
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GuidanceLaneConstraintState"/> record.
    /// </summary>
    /// <param name="zoneMask">Active zone mask affecting the lane.</param>
    /// <param name="hasBlockingConstraint">True when a blocking constraint is present.</param>
    /// <param name="insideHeadland">True when the vehicle is inside the configured headland.</param>
    /// <param name="distanceToConstraintMeters">Distance to the blocking constraint in metres, when known.</param>
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

    /// <summary>Gets the active zone mask affecting the lane.</summary>
    public PoseZoneMask? ZoneMask { get; }

    /// <summary>Gets a value indicating whether a blocking constraint is present.</summary>
    public bool HasBlockingConstraint { get; }

    /// <summary>Gets a value indicating whether the vehicle is inside the headland.</summary>
    public bool InsideHeadland { get; }

    /// <summary>Gets the distance to the blocking constraint in metres, when known.</summary>
    public double? DistanceToConstraintMeters { get; }
}

/// <summary>
/// Planner preview context describing the active vehicle state.
/// </summary>
public sealed record GuidanceLanePreview
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GuidanceLanePreview"/> record.
    /// </summary>
    /// <param name="crossTrackErrorMeters">Cross-track error in metres.</param>
    /// <param name="headingErrorRadians">Heading error in radians.</param>
    /// <param name="lookAheadDistanceMeters">Look-ahead distance in metres.</param>
    /// <param name="targetPoint">Target point that the planner is tracking.</param>
    /// <param name="controllerOutput">Controller output applied to the vehicle.</param>
    /// <param name="controllerEnabled">Value indicating whether the controller is enabled.</param>
    /// <param name="targetCurvaturePerMeter">Optional target curvature at the look-ahead point.</param>
    /// <param name="constraint">Optional constraint state affecting the preview.</param>
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

    /// <summary>Gets the cross-track error in metres.</summary>
    public double CrossTrackErrorMeters { get; }

    /// <summary>Gets the heading error in radians.</summary>
    public double HeadingErrorRadians { get; }

    /// <summary>Gets the look-ahead distance in metres.</summary>
    public double LookAheadDistanceMeters { get; }

    /// <summary>Gets the target point that the planner is tracking.</summary>
    public GuidanceLanePoint TargetPoint { get; }

    /// <summary>Gets the controller output applied to the vehicle.</summary>
    public double ControllerOutput { get; }

    /// <summary>Gets a value indicating whether the controller is enabled.</summary>
    public bool ControllerEnabled { get; }

    /// <summary>Gets the optional target curvature at the look-ahead point.</summary>
    public double? TargetCurvaturePerMeter { get; }

    /// <summary>Gets the optional constraint state affecting the preview.</summary>
    public GuidanceLaneConstraintState? Constraint { get; }
}

/// <summary>
/// Geometry for a single pass belonging to a guidance lane.
/// </summary>
public sealed record GuidanceLanePass
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GuidanceLanePass"/> record.
    /// </summary>
    /// <param name="index">Zero-based pass index.</param>
    /// <param name="points">Sampled points belonging to the pass.</param>
    /// <param name="headingRadians">Average pass heading in radians.</param>
    /// <param name="signedDistanceMeters">Signed offset from the reference pass in metres.</param>
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

    /// <summary>Gets the zero-based pass index.</summary>
    public int Index { get; }

    /// <summary>Gets the sampled points belonging to the pass.</summary>
    public IReadOnlyList<GuidanceLanePoint> Points { get; }

    /// <summary>Gets the average pass heading in radians.</summary>
    public double HeadingRadians { get; }

    /// <summary>Gets the signed offset from the reference pass in metres.</summary>
    public double SignedDistanceMeters { get; }
}

/// <summary>
/// Complete guidance lane definition and preview.
/// </summary>
public sealed record GuidanceLane
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GuidanceLane"/> record.
    /// </summary>
    /// <param name="metadata">Metadata describing the lane.</param>
    /// <param name="laneSpacingMeters">Lane spacing in metres.</param>
    /// <param name="implementWidthMeters">Implement width in metres.</param>
    /// <param name="overlapMeters">Configured overlap in metres.</param>
    /// <param name="nudgeMeters">Nudge offset applied in metres.</param>
    /// <param name="extensionLengthMeters">Extension length in metres.</param>
    /// <param name="baseHeadingRadians">Base heading in radians.</param>
    /// <param name="passes">Pass geometry belonging to the lane.</param>
    /// <param name="preview">Optional preview describing the current planner state.</param>
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

    /// <summary>Gets the metadata describing the lane.</summary>
    public GuidanceLaneMetadata Metadata { get; }

    /// <summary>Gets the lane spacing in metres.</summary>
    public double LaneSpacingMeters { get; }

    /// <summary>Gets the implement width in metres.</summary>
    public double ImplementWidthMeters { get; }

    /// <summary>Gets the configured overlap in metres.</summary>
    public double OverlapMeters { get; }

    /// <summary>Gets the nudge offset applied in metres.</summary>
    public double NudgeMeters { get; }

    /// <summary>Gets the extension length in metres.</summary>
    public double ExtensionLengthMeters { get; }

    /// <summary>Gets the base heading in radians.</summary>
    public double BaseHeadingRadians { get; }

    /// <summary>Gets the pass geometry belonging to the lane.</summary>
    public IReadOnlyList<GuidanceLanePass> Passes { get; }

    /// <summary>Gets the optional preview describing the current planner state.</summary>
    public GuidanceLanePreview? Preview { get; }
}

/// <summary>
/// Wrapper carrying the protobuf header alongside a lane payload.
/// </summary>
public sealed record GuidanceLanePublish
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GuidanceLanePublish"/> record.
    /// </summary>
    /// <param name="header">Protobuf header accompanying the lane.</param>
    /// <param name="lane">Lane payload to publish.</param>
    public GuidanceLanePublish(Header header, GuidanceLane lane)
    {
        Header = header?.Clone() ?? throw new ArgumentNullException(nameof(header));
        Lane = lane ?? throw new ArgumentNullException(nameof(lane));
    }

    /// <summary>Gets the protobuf header accompanying the lane.</summary>
    public Header Header { get; }

    /// <summary>Gets the lane payload to publish.</summary>
    public GuidanceLane Lane { get; }
}

/// <summary>
/// Bundled lane publish frame mirroring <see cref="GuidanceLaneFrame"/>.
/// </summary>
public sealed record GuidanceLaneFramePublish
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GuidanceLaneFramePublish"/> record.
    /// </summary>
    /// <param name="header">Protobuf header accompanying the frame.</param>
    /// <param name="lanes">Lane publishes included in the frame.</param>
    public GuidanceLaneFramePublish(Header header, IReadOnlyList<GuidanceLanePublish> lanes)
    {
        Header = header?.Clone() ?? throw new ArgumentNullException(nameof(header));

        if (lanes is null)
        {
            throw new ArgumentNullException(nameof(lanes));
        }

        Lanes = new ReadOnlyCollection<GuidanceLanePublish>(lanes.Select(l => l ?? throw new ArgumentException("Lane entries cannot be null.", nameof(lanes))).ToArray());
    }

    /// <summary>Gets the protobuf header accompanying the frame.</summary>
    public Header Header { get; }

    /// <summary>Gets the lane publishes included in the frame.</summary>
    public IReadOnlyList<GuidanceLanePublish> Lanes { get; }
}

/// <summary>
/// Conversion helpers between domain models and generated protobuf messages.
/// </summary>
public static class GuidanceLaneContractsExtensions
{
    /// <summary>
    /// Converts a protobuf guidance lane into the domain model representation.
    /// </summary>
    /// <param name="message">Protobuf message received from the planner.</param>
    /// <returns>Domain model publish payload containing the lane.</returns>
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

    /// <summary>
    /// Converts a domain model lane publish into its protobuf representation.
    /// </summary>
    /// <param name="publish">Lane publish to convert.</param>
    /// <returns>Protobuf message suitable for transport.</returns>
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

    /// <summary>
    /// Converts a protobuf lane frame into the domain model representation.
    /// </summary>
    /// <param name="frame">Protobuf frame message.</param>
    /// <returns>Domain model frame publish containing individual lanes.</returns>
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

    /// <summary>
    /// Converts a domain model lane frame into its protobuf representation.
    /// </summary>
    /// <param name="frame">Domain model frame to convert.</param>
    /// <returns>Protobuf frame message.</returns>
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
            LaneTemplate.Straight => GuidanceLaneTemplate.Straight,
            LaneTemplate.Curve => GuidanceLaneTemplate.Curve,
            LaneTemplate.Adaptive => GuidanceLaneTemplate.Adaptive,
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
                GuidanceLaneTemplate.Straight => LaneTemplate.Straight,
                GuidanceLaneTemplate.Curve => LaneTemplate.Curve,
                GuidanceLaneTemplate.Adaptive => LaneTemplate.Adaptive,
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
