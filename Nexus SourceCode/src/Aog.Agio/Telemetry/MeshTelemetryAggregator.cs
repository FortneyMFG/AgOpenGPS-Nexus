using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aog.Core.Mesh;
using Aog.Core.V1;
using Aog.Agio.Legacy;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aog.Agio.Telemetry;

/// <summary>
/// Bridges AGiO telemetry feeds into the live mesh presence and trail topics described by ADR-047.
/// </summary>
public sealed class MeshTelemetryAggregator : ILegacyPoseObserver
{
    private static readonly string[] SharedLayers = { "presence", "trail" };
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly ILiveTelemetryMeshService _meshService;
    private readonly MeshTelemetryAggregatorOptions _options;
    private readonly ILogger<MeshTelemetryAggregator> _logger;
    private readonly TimeProvider _timeProvider;
    private readonly string[] _capabilities;
    private readonly HashSet<ContextKey> _contexts = new(ContextKeyComparer.Instance);
    private readonly List<TrailPoint> _trail = new();
    private readonly object _gate = new();
    private DateTimeOffset _lastTrailPublish = DateTimeOffset.MinValue;
    private bool _deviceRegistered;

    /// <summary>
    /// Initializes a new instance of the <see cref="MeshTelemetryAggregator"/> class.
    /// </summary>
    public MeshTelemetryAggregator(
        ILiveTelemetryMeshService meshService,
        IOptions<MeshTelemetryAggregatorOptions> options,
        ILogger<MeshTelemetryAggregator> logger,
        TimeProvider? timeProvider = null)
    {
        _meshService = meshService ?? throw new ArgumentNullException(nameof(meshService));
        _options = (options ?? throw new ArgumentNullException(nameof(options))).Value
            ?? throw new ArgumentException("Options are required.", nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _timeProvider = timeProvider ?? TimeProvider.System;

        _capabilities = _options.Capabilities
            .Where(capability => !string.IsNullOrWhiteSpace(capability))
            .Select(capability => capability.Trim())
            .ToArray();
    }

    /// <inheritdoc />
    public async ValueTask OnPoseAsync(Pose pose, LegacyPoseMetadata metadata, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(pose);
        metadata ??= new LegacyPoseMetadata();

        if (!TryNormalizePoseContext(pose, out var context))
        {
            _logger.LogDebug("Skipping mesh publish because pose is missing season/job context.");
            return;
        }

        await EnsureDeviceRegisteredAsync(context.SeasonId, context.JobId, cancellationToken).ConfigureAwait(false);

        var meshPose = CreateMeshPose(pose);
        if (meshPose is null)
        {
            _logger.LogDebug("Skipping mesh publish because pose contains invalid coordinates.");
            return;
        }

        var timestamp = ResolveTimestamp(pose.Header?.Timestamp);
        var metadataMap = BuildMetadata(pose, metadata);

        var session = new MeshSessionDescriptor(context.SeasonId, context.JobId, context.SessionId);
        var update = new MeshPresenceUpdate(
            _options.DeviceId,
            session,
            meshPose,
            MeshPresenceState.Online,
            metadataMap,
            timestamp);

        await _meshService.UpdatePresenceAsync(update, cancellationToken).ConfigureAwait(false);

        var snapshot = TryCreateTrailSnapshot(context.SeasonId, context.JobId, context.SessionId, timestamp, meshPose);
        if (snapshot is null)
        {
            return;
        }

        var payload = JsonSerializer.SerializeToUtf8Bytes(snapshot, SerializerOptions);
        var trailMetadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["pointCount"] = snapshot.Points.Count.ToString(CultureInfo.InvariantCulture),
        };

        if (!string.IsNullOrEmpty(context.SessionId))
        {
            trailMetadata["sessionId"] = context.SessionId;
        }

        var publishRequest = new MeshPublishRequest(
            _options.DeviceId,
            $"aog/live/{context.SeasonId}/{context.JobId}/trail",
            MeshDataTier.Trails,
            payload,
            timestamp,
            trailMetadata);

        await _meshService.PublishAsync(publishRequest, cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask EnsureDeviceRegisteredAsync(string seasonId, string jobId, CancellationToken cancellationToken)
    {
        MeshShareProfile? profile = null;
        var shouldUpdate = false;

        lock (_gate)
        {
            if (_contexts.Add(new ContextKey(seasonId, jobId)) || !_deviceRegistered)
            {
                var grants = _contexts
                    .OrderBy(context => context.SeasonId, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(context => context.JobId, StringComparer.OrdinalIgnoreCase)
                    .Select(context => new MeshShareGrant(context.SeasonId, context.JobId, MeshDataTier.Presence | MeshDataTier.Trails, SharedLayers))
                    .ToArray();

                profile = new MeshShareProfile(grants);
                shouldUpdate = true;
                _deviceRegistered = true;
            }
        }

        if (!shouldUpdate)
        {
            return;
        }

        var registration = new MeshDeviceRegistration(
            _options.DeviceId,
            _options.DeviceLabel,
            shareProfile: profile,
            subscribeProfile: MeshSubscribeProfile.Empty)
        {
            Capabilities = _capabilities.Length == 0 ? null : _capabilities
        };

        await _meshService.RegisterOrUpdateDeviceAsync(registration, cancellationToken).ConfigureAwait(false);
    }

    private MeshTrailSnapshot? TryCreateTrailSnapshot(string seasonId, string jobId, string? sessionId, DateTimeOffset timestamp, MeshPose pose)
    {
        List<MeshTrailPoint>? points = null;
        var shouldPublish = false;

        lock (_gate)
        {
            _trail.Add(new TrailPoint(pose.Latitude, pose.Longitude, pose.AltitudeMeters, pose.HeadingDegrees, pose.SpeedMetersPerSecond, timestamp));

            if (_trail.Count > _options.TrailCapacity)
            {
                var excess = _trail.Count - _options.TrailCapacity;
                _trail.RemoveRange(0, excess);
            }

            if (_options.TrailPublishInterval <= TimeSpan.Zero || timestamp - _lastTrailPublish >= _options.TrailPublishInterval)
            {
                _lastTrailPublish = timestamp;
                shouldPublish = true;
                points = _trail
                    .Select(point => new MeshTrailPoint(point.Latitude, point.Longitude, point.AltitudeMeters, point.HeadingDegrees, point.SpeedMetersPerSecond, point.Timestamp))
                    .ToList();
            }
        }

        if (!shouldPublish || points is null)
        {
            return null;
        }

        return new MeshTrailSnapshot(
            _options.DeviceId,
            seasonId,
            jobId,
            sessionId,
            timestamp,
            points);
    }

    private static bool TryNormalizePoseContext(Pose pose, out PoseContext context)
    {
        context = default;
        var header = pose.Header;
        if (header is null)
        {
            return false;
        }

        var seasonId = NormalizeIdentifier(header.SeasonId);
        var jobId = NormalizeIdentifier(header.JobId);
        if (string.IsNullOrEmpty(seasonId) || string.IsNullOrEmpty(jobId))
        {
            return false;
        }

        context = new PoseContext(seasonId, jobId, NormalizeOptionalIdentifier(header.SessionId));
        return true;
    }

    private static MeshPose? CreateMeshPose(Pose pose)
    {
        if (!double.IsFinite(pose.LatitudeDeg) || !double.IsFinite(pose.LongitudeDeg))
        {
            return null;
        }

        double? altitude = double.IsFinite(pose.AltitudeM) ? pose.AltitudeM : null;
        double? heading = double.IsFinite(pose.HeadingRad) ? pose.HeadingRad * 180.0 / Math.PI : null;
        double? speed = double.IsFinite(pose.SpeedMps) ? pose.SpeedMps : null;

        return new MeshPose(pose.LatitudeDeg, pose.LongitudeDeg, altitude, heading, speed);
    }

    private DateTimeOffset ResolveTimestamp(Timestamp? timestamp)
    {
        if (timestamp is null || (timestamp.Seconds == 0 && timestamp.Nanos == 0))
        {
            return _timeProvider.GetUtcNow();
        }

        try
        {
            var dto = timestamp.ToDateTimeOffset();
            return dto == default ? _timeProvider.GetUtcNow() : dto;
        }
        catch (ArgumentOutOfRangeException)
        {
            return _timeProvider.GetUtcNow();
        }
    }

    private Dictionary<string, string> BuildMetadata(Pose pose, LegacyPoseMetadata metadata)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var header = pose.Header;

        if (!string.IsNullOrWhiteSpace(header?.Source))
        {
            map["source"] = header.Source.Trim();
        }

        if (!string.IsNullOrWhiteSpace(header?.Frame))
        {
            map["frame"] = header.Frame.Trim();
        }

        if (header?.Sequence is ulong sequence and > 0)
        {
            map["sequence"] = sequence.ToString(CultureInfo.InvariantCulture);
        }

        map["legacySource"] = metadata.SourceAddress.ToString(CultureInfo.InvariantCulture);
        map["fixQuality"] = metadata.FixQuality.ToString(CultureInfo.InvariantCulture);
        map["satellites"] = metadata.SatellitesTracked.ToString(CultureInfo.InvariantCulture);
        map["hdop"] = (metadata.HdopTimes100 / 100.0).ToString("F2", CultureInfo.InvariantCulture);
        map["correctionAgeSeconds"] = (metadata.AgeOfCorrectionsTimes100 / 100.0).ToString("F2", CultureInfo.InvariantCulture);
        map["imuHeadingDeg"] = (metadata.ImuHeadingHundredths / 100.0).ToString("F2", CultureInfo.InvariantCulture);
        map["imuRollDeg"] = (metadata.ImuRollHundredths / 100.0).ToString("F2", CultureInfo.InvariantCulture);
        map["imuPitchDeg"] = (metadata.ImuPitchHundredths / 100.0).ToString("F2", CultureInfo.InvariantCulture);
        map["imuYawRateDegPerSec"] = (metadata.ImuYawRateHundredths / 100.0).ToString("F2", CultureInfo.InvariantCulture);

        if (_capabilities.Length > 0)
        {
            map["capabilities"] = string.Join(',', _capabilities);
        }

        return map;
    }

    private static string NormalizeIdentifier(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    private static string? NormalizeOptionalIdentifier(string? value)
    {
        var normalized = NormalizeIdentifier(value);
        return string.IsNullOrEmpty(normalized) ? null : normalized;
    }

    private readonly record struct PoseContext(string SeasonId, string JobId, string? SessionId);

    private readonly record struct ContextKey(string SeasonId, string JobId);

    private sealed class ContextKeyComparer : IEqualityComparer<ContextKey>
    {
        public static ContextKeyComparer Instance { get; } = new();

        public bool Equals(ContextKey x, ContextKey y)
        {
            return string.Equals(x.SeasonId, y.SeasonId, StringComparison.OrdinalIgnoreCase)
                && string.Equals(x.JobId, y.JobId, StringComparison.OrdinalIgnoreCase);
        }

        public int GetHashCode(ContextKey obj)
        {
            return HashCode.Combine(
                obj.SeasonId?.ToUpperInvariant(),
                obj.JobId?.ToUpperInvariant());
        }
    }

    private readonly record struct TrailPoint(
        double Latitude,
        double Longitude,
        double? AltitudeMeters,
        double? HeadingDegrees,
        double? SpeedMetersPerSecond,
        DateTimeOffset Timestamp);

    /// <summary>
    /// Snapshot of the trail polyline emitted to mesh subscribers.
    /// </summary>
    public sealed record MeshTrailSnapshot(
        string DeviceId,
        string SeasonId,
        string JobId,
        string? SessionId,
        DateTimeOffset GeneratedAt,
        IReadOnlyList<MeshTrailPoint> Points);

    /// <summary>
    /// Represents a single trail point appended to the mesh feed.
    /// </summary>
    public sealed record MeshTrailPoint(
        double Latitude,
        double Longitude,
        double? AltitudeMeters,
        double? HeadingDegrees,
        double? SpeedMetersPerSecond,
        DateTimeOffset Timestamp);
}
