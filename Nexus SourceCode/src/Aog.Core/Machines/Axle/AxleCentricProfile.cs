using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Aog.Core.Machines.Axle;

/// <summary>
/// Represents the axle-centric configuration that drives the runtime kinematics pipeline.
/// </summary>
public sealed class AxleCentricProfile
{
    private readonly ReadOnlyDictionary<string, AxleModeProfile> _modes;

    /// <summary>
    /// Initializes a new instance of the <see cref="AxleCentricProfile"/> class.
    /// </summary>
    public AxleCentricProfile(
        string profileId,
        string schemaVersion,
        IReadOnlyList<AxleNode> axles,
        IReadOnlyList<AxleJoint> joints,
        IReadOnlyDictionary<string, AxleModeProfile> modes,
        LateMeasurementPolicy lateMeasurementPolicy,
        CompatibilityGuard compatibility,
        int deterministicSeed,
        string contentHash)
    {
        if (string.IsNullOrWhiteSpace(profileId))
        {
            throw new ArgumentException("Profile identifier is required.", nameof(profileId));
        }

        if (string.IsNullOrWhiteSpace(schemaVersion))
        {
            throw new ArgumentException("Schema version is required.", nameof(schemaVersion));
        }

        ProfileId = profileId;
        SchemaVersion = schemaVersion;
        Axles = axles ?? throw new ArgumentNullException(nameof(axles));
        Joints = joints ?? throw new ArgumentNullException(nameof(joints));
        _modes = new ReadOnlyDictionary<string, AxleModeProfile>(
            modes?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value, StringComparer.OrdinalIgnoreCase)
            ?? throw new ArgumentNullException(nameof(modes)));
        LateMeasurementPolicy = lateMeasurementPolicy ?? throw new ArgumentNullException(nameof(lateMeasurementPolicy));
        Compatibility = compatibility ?? throw new ArgumentNullException(nameof(compatibility));
        DeterministicSeed = deterministicSeed;
        ContentHash = contentHash ?? throw new ArgumentNullException(nameof(contentHash));
    }

    /// <summary>
    /// Gets the user facing profile identifier.
    /// </summary>
    public string ProfileId { get; }

    /// <summary>
    /// Gets the schema version that produced this profile.
    /// </summary>
    public string SchemaVersion { get; }

    /// <summary>
    /// Gets the deterministic seed advertised by the profile.
    /// </summary>
    public int DeterministicSeed { get; }

    /// <summary>
    /// Gets the SHA-256 hash of the canonical JSON payload.
    /// </summary>
    public string ContentHash { get; }

    /// <summary>
    /// Gets the compatibility guard block.
    /// </summary>
    public CompatibilityGuard Compatibility { get; }

    /// <summary>
    /// Gets the policy for late-arriving measurements.
    /// </summary>
    public LateMeasurementPolicy LateMeasurementPolicy { get; }

    /// <summary>
    /// Gets the axle nodes that compose the machine graph.
    /// </summary>
    public IReadOnlyList<AxleNode> Axles { get; }

    /// <summary>
    /// Gets the joints that connect the axle graph.
    /// </summary>
    public IReadOnlyList<AxleJoint> Joints { get; }

    /// <summary>
    /// Gets the mode catalog keyed by mode identifier.
    /// </summary>
    public IReadOnlyDictionary<string, AxleModeProfile> Modes => _modes;

    /// <summary>
    /// Looks up the requested mode or throws an informative exception if it is missing.
    /// </summary>
    public AxleModeProfile GetMode(string mode)
    {
        if (mode is null)
        {
            throw new ArgumentNullException(nameof(mode));
        }

        if (_modes.TryGetValue(mode, out var profile))
        {
            return profile;
        }

        throw new KeyNotFoundException($"Mode '{mode}' is not present in the axle-centric profile '{ProfileId}'.");
    }

    /// <summary>
    /// Calculates the maximum slip limit across all modes.
    /// </summary>
    public double GetMaximumSlipLimit()
    {
        return _modes.Count == 0 ? 0 : _modes.Values.Max(m => m.SlipLimit);
    }
}

/// <summary>
/// Describes a single axle node that participates in the runtime kinematic graph.
/// </summary>
public sealed class AxleNode
{
    public AxleNode(string id, AxleNodeRole role, double? curvatureLimit, double? slipLimit)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Axle identifier is required.", nameof(id));
        }

        Id = id;
        Role = role;
        CurvatureLimit = curvatureLimit;
        SlipLimit = slipLimit;
    }

    /// <summary>
    /// Gets the unique axle identifier.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the role that the axle plays in the machine.
    /// </summary>
    public AxleNodeRole Role { get; }

    /// <summary>
    /// Gets the optional per-axle curvature limit in inverse metres.
    /// </summary>
    public double? CurvatureLimit { get; }

    /// <summary>
    /// Gets the optional slip limit for the axle expressed as a unitless ratio.
    /// </summary>
    public double? SlipLimit { get; }
}

/// <summary>
/// Enumeration of the supported axle roles.
/// </summary>
public enum AxleNodeRole
{
    Unknown = 0,
    Drive,
    Steer,
    Articulation,
    Implement,
    Caster
}

/// <summary>
/// Describes the joint between two axle nodes.
/// </summary>
public sealed class AxleJoint
{
    public AxleJoint(string parent, string child, AxleJointType type)
    {
        if (string.IsNullOrWhiteSpace(parent))
        {
            throw new ArgumentException("Parent axle is required.", nameof(parent));
        }

        if (string.IsNullOrWhiteSpace(child))
        {
            throw new ArgumentException("Child axle is required.", nameof(child));
        }

        Parent = parent;
        Child = child;
        Type = type;
    }

    /// <summary>
    /// Gets the parent axle identifier.
    /// </summary>
    public string Parent { get; }

    /// <summary>
    /// Gets the child axle identifier.
    /// </summary>
    public string Child { get; }

    /// <summary>
    /// Gets the joint type.
    /// </summary>
    public AxleJointType Type { get; }
}

/// <summary>
/// Enumeration of supported joint types.
/// </summary>
public enum AxleJointType
{
    Rigid = 0,
    Articulated,
    Hitch,
    Drawbar
}

/// <summary>
/// Mode specific curvature and slip limits advertised by the profile.
/// </summary>
public sealed class AxleModeProfile
{
    public AxleModeProfile(string id, double curvatureLimit, double slipLimit, DriveDirectionPolicy driveDirectionPolicy)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Mode identifier is required.", nameof(id));
        }

        if (curvatureLimit <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(curvatureLimit), "Curvature limit must be positive.");
        }

        if (slipLimit < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(slipLimit), "Slip limit cannot be negative.");
        }

        Id = id;
        CurvatureLimit = curvatureLimit;
        SlipLimit = slipLimit;
        DriveDirectionPolicy = driveDirectionPolicy;
    }

    /// <summary>
    /// Gets the mode identifier.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the maximum curvature in inverse metres.
    /// </summary>
    public double CurvatureLimit { get; }

    /// <summary>
    /// Gets the maximum permitted slip.
    /// </summary>
    public double SlipLimit { get; }

    /// <summary>
    /// Gets the policy describing allowed drive directions.
    /// </summary>
    public DriveDirectionPolicy DriveDirectionPolicy { get; }

    /// <summary>
    /// Computes the minimum feasible turn radius for this mode.
    /// </summary>
    public double GetMinimumTurnRadiusMeters()
    {
        return CurvatureLimit <= 0 ? double.PositiveInfinity : 1.0 / CurvatureLimit;
    }
}

/// <summary>
/// Policies describing how automation is allowed to command drive direction.
/// </summary>
public enum DriveDirectionPolicy
{
    ForwardOnly = 0,
    ReverseOnly,
    Bidirectional
}

/// <summary>
/// Encapsulates late measurement handling policies.
/// </summary>
public sealed class LateMeasurementPolicy
{
    public LateMeasurementPolicy(bool allowLateMeasurements, double maximumLatencyMilliseconds)
    {
        if (maximumLatencyMilliseconds < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumLatencyMilliseconds));
        }

        AllowLateMeasurements = allowLateMeasurements;
        MaximumLatencyMilliseconds = maximumLatencyMilliseconds;
    }

    public bool AllowLateMeasurements { get; }

    public double MaximumLatencyMilliseconds { get; }
}

/// <summary>
/// Advertises compatibility information.
/// </summary>
public sealed class CompatibilityGuard
{
    public CompatibilityGuard(Version minimumCoreVersion, string schemaVersion)
    {
        MinimumCoreVersion = minimumCoreVersion ?? throw new ArgumentNullException(nameof(minimumCoreVersion));
        SchemaVersion = schemaVersion ?? throw new ArgumentNullException(nameof(schemaVersion));
    }

    public Version MinimumCoreVersion { get; }

    public string SchemaVersion { get; }
}

/// <summary>
/// Result container for the ingestion pipeline.
/// </summary>
public sealed class AxleIngestionResult
{
    public AxleIngestionResult(AxleCentricProfile? profile, IReadOnlyList<AxleIngestionMessage> messages, AxleCentricTelemetry telemetry)
    {
        Profile = profile;
        Messages = messages ?? throw new ArgumentNullException(nameof(messages));
        Telemetry = telemetry ?? throw new ArgumentNullException(nameof(telemetry));
    }

    public AxleCentricProfile? Profile { get; }

    public IReadOnlyList<AxleIngestionMessage> Messages { get; }

    public AxleCentricTelemetry Telemetry { get; }

    public bool IsSuccess => Profile is not null && !Messages.Any(m => m.Severity == AxleIngestionSeverity.Error);
}

/// <summary>
/// Telemetry captured when loading a profile.
/// </summary>
public sealed class AxleCentricTelemetry
{
    public AxleCentricTelemetry(int axleCount, IReadOnlyDictionary<string, double> modeCurvatureLimits, double maximumSlipLimit, string profileHash)
    {
        AxleCount = axleCount;
        var curvatureLimits = modeCurvatureLimits is null
            ? new Dictionary<string, double>()
            : new Dictionary<string, double>(modeCurvatureLimits);
        ModeCurvatureLimits = new ReadOnlyDictionary<string, double>(curvatureLimits);
        MaximumSlipLimit = maximumSlipLimit;
        ProfileHash = profileHash ?? string.Empty;
    }

    public int AxleCount { get; }

    public IReadOnlyDictionary<string, double> ModeCurvatureLimits { get; }

    public double MaximumSlipLimit { get; }

    public string ProfileHash { get; }
}

/// <summary>
/// Message emitted by the ingestion pipeline.
/// </summary>
public sealed class AxleIngestionMessage
{
    public AxleIngestionMessage(string code, AxleIngestionSeverity severity, string message)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Message code is required.", nameof(code));
        }

        Code = code;
        Severity = severity;
        Message = message ?? string.Empty;
    }

    public string Code { get; }

    public AxleIngestionSeverity Severity { get; }

    public string Message { get; }
}

/// <summary>
/// Severity levels emitted by the ingestion pipeline.
/// </summary>
public enum AxleIngestionSeverity
{
    Info = 0,
    Warning,
    Error
}

/// <summary>
/// Configures behaviour of the axle-centric ingestion pipeline.
/// </summary>
public sealed class AxleIngestionOptions
{
    public static readonly AxleIngestionOptions Default = new(new Version(1, 2), null);

    public AxleIngestionOptions(Version? coreVersion, int? deterministicSeedOverride)
    {
        CoreVersion = coreVersion ?? new Version(1, 0);
        DeterministicSeedOverride = deterministicSeedOverride;
    }

    /// <summary>
    /// Gets the version of Core requesting ingestion.
    /// </summary>
    public Version CoreVersion { get; }

    /// <summary>
    /// Gets the optional deterministic seed override.
    /// </summary>
    public int? DeterministicSeedOverride { get; }
}
