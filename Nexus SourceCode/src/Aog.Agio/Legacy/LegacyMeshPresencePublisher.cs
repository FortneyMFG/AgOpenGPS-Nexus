using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Aog.Core.Mesh;
using Aog.Core.V1;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aog.Agio.Legacy;

/// <summary>
/// Bridges decoded legacy telemetry into the live telemetry mesh presence stream.
/// </summary>
public sealed class LegacyMeshPresencePublisher : ILegacyMeshPresencePublisher, IHostedService
{
    private readonly ILiveTelemetryMeshService _meshService;
    private readonly MeshSessionDescriptor _defaultSession;
    private readonly string _shareSeasonId;
    private readonly string _shareJobId;
    private readonly string _deviceId;
    private readonly string _deviceLabel;
    private readonly IReadOnlyList<string> _capabilities;
    private readonly Dictionary<string, string> _staticMetadata;
    private readonly ILogger<LegacyMeshPresencePublisher> _logger;
    private readonly MeshDeviceRegistration _registration;
    private LegacyDiscoveryAnnouncement? _lastDiscovery;

    /// <summary>
    /// Initializes a new instance of the <see cref="LegacyMeshPresencePublisher"/> class.
    /// </summary>
    public LegacyMeshPresencePublisher(
        ILiveTelemetryMeshService meshService,
        IOptions<LegacyMeshOptions> optionsAccessor,
        ILogger<LegacyMeshPresencePublisher> logger)
    {
        _meshService = meshService ?? throw new ArgumentNullException(nameof(meshService));
        if (optionsAccessor is null)
        {
            throw new ArgumentNullException(nameof(optionsAccessor));
        }

        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var options = optionsAccessor.Value ?? throw new ArgumentException("Mesh options must be configured.", nameof(optionsAccessor));

        _deviceId = NormalizeRequired(options.DeviceId, nameof(options.DeviceId));
        _deviceLabel = NormalizeRequired(options.Label, nameof(options.Label));
        _shareSeasonId = NormalizeRequired(options.ShareSeasonId, nameof(options.ShareSeasonId));
        _shareJobId = NormalizeRequired(options.ShareJobId, nameof(options.ShareJobId));

        var defaultSeason = NormalizeRequired(options.DefaultSeasonId, nameof(options.DefaultSeasonId));
        var defaultJob = NormalizeRequired(options.DefaultJobId, nameof(options.DefaultJobId));
        var defaultSession = NormalizeOptional(options.DefaultSessionId);
        var layerNamespace = NormalizeLayerNamespace(options.LayerNamespace);

        _defaultSession = new MeshSessionDescriptor(defaultSeason, defaultJob, defaultSession, layerNamespace);
        _capabilities = NormalizeCapabilities(options.Capabilities);
        _staticMetadata = NormalizeMetadata(options.Metadata);

        _registration = new MeshDeviceRegistration(
            _deviceId,
            _deviceLabel,
            _capabilities,
            new MeshShareProfile(new[]
            {
                new MeshShareGrant(_shareSeasonId, _shareJobId, MeshDataTier.Presence)
            }),
            MeshSubscribeProfile.Empty);
    }

    /// <inheritdoc />
    public ValueTask OnDiscoveryAsync(LegacyDiscoveryAnnouncement announcement, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(announcement);
        cancellationToken.ThrowIfCancellationRequested();

        Volatile.Write(ref _lastDiscovery, announcement);
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public async ValueTask PublishPresenceAsync(Pose pose, LegacyPoseMetadata metadata, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(pose);
        ArgumentNullException.ThrowIfNull(metadata);
        cancellationToken.ThrowIfCancellationRequested();

        var header = pose.Header;
        var session = BuildSessionDescriptor(header);
        var meshPose = BuildMeshPose(pose);
        var timestamp = ExtractTimestamp(header);
        var metadataPayload = BuildMetadata(metadata);

        var update = new MeshPresenceUpdate(
            _deviceId,
            session,
            meshPose,
            MeshPresenceState.Online,
            metadataPayload,
            timestamp);

        await _meshService.UpdatePresenceAsync(update, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Performs asynchronous registration of the legacy mesh device with the mesh service.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token controlling the operation.</param>
    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            await _meshService.RegisterOrUpdateDeviceAsync(_registration, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register legacy mesh presence device {DeviceId}.", _deviceId);
            throw;
        }
    }

    Task IHostedService.StartAsync(CancellationToken cancellationToken) => InitializeAsync(cancellationToken);

    Task IHostedService.StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private MeshSessionDescriptor BuildSessionDescriptor(Header? header)
    {
        var season = NormalizeIdentifierWithFallback(header?.SeasonId, _defaultSession.SeasonId);
        var job = NormalizeIdentifierWithFallback(header?.JobId, _defaultSession.JobId);
        var session = NormalizeOptional(header?.SessionId) ?? _defaultSession.SessionId;
        return new MeshSessionDescriptor(season, job, session, _defaultSession.LayerNamespace);
    }

    private static MeshPose BuildMeshPose(Pose pose)
    {
        var altitude = double.IsFinite(pose.AltitudeM) ? pose.AltitudeM : (double?)null;
        var headingDegrees = double.IsFinite(pose.HeadingRad) ? pose.HeadingRad * 180.0 / Math.PI : (double?)null;
        var speed = double.IsFinite(pose.SpeedMps) ? pose.SpeedMps : (double?)null;

        return new MeshPose(
            pose.LatitudeDeg,
            pose.LongitudeDeg,
            altitude,
            headingDegrees,
            speed);
    }

    private IReadOnlyDictionary<string, string> BuildMetadata(LegacyPoseMetadata metadata)
    {
        var snapshot = new Dictionary<string, string>(_staticMetadata, StringComparer.OrdinalIgnoreCase)
        {
            ["legacy.sourceAddress"] = metadata.SourceAddress.ToString(CultureInfo.InvariantCulture),
            ["legacy.fixQuality"] = metadata.FixQuality.ToString(CultureInfo.InvariantCulture),
            ["legacy.satellitesTracked"] = metadata.SatellitesTracked.ToString(CultureInfo.InvariantCulture),
        };

        if (metadata.HdopTimes100 > 0)
        {
            snapshot["legacy.hdop"] = (metadata.HdopTimes100 / 100.0).ToString("G", CultureInfo.InvariantCulture);
        }

        if (metadata.AgeOfCorrectionsTimes100 > 0)
        {
            snapshot["legacy.correctionsAgeSeconds"] = (metadata.AgeOfCorrectionsTimes100 / 100.0).ToString("G", CultureInfo.InvariantCulture);
        }

        if (metadata.ImuHeadingHundredths != 0)
        {
            snapshot["legacy.imuHeadingDeg"] = (metadata.ImuHeadingHundredths / 100.0).ToString("G", CultureInfo.InvariantCulture);
        }

        if (metadata.ImuRollHundredths != 0)
        {
            snapshot["legacy.imuRollDeg"] = (metadata.ImuRollHundredths / 100.0).ToString("G", CultureInfo.InvariantCulture);
        }

        if (metadata.ImuPitchHundredths != 0)
        {
            snapshot["legacy.imuPitchDeg"] = (metadata.ImuPitchHundredths / 100.0).ToString("G", CultureInfo.InvariantCulture);
        }

        if (metadata.ImuYawRateHundredths != 0)
        {
            snapshot["legacy.imuYawRateDegPerSec"] = (metadata.ImuYawRateHundredths / 100.0).ToString("G", CultureInfo.InvariantCulture);
        }

        var discovery = Volatile.Read(ref _lastDiscovery);
        if (discovery is not null)
        {
            snapshot["legacy.discovery.vendorId"] = discovery.VendorId.ToString(CultureInfo.InvariantCulture);
            snapshot["legacy.discovery.productId"] = discovery.ProductId.ToString(CultureInfo.InvariantCulture);
            snapshot["legacy.discovery.variantId"] = discovery.VariantId.ToString(CultureInfo.InvariantCulture);
            snapshot["legacy.discovery.mcu"] = discovery.KnownMcu.ToString();
            snapshot["legacy.discovery.firmware"] = discovery.FirmwareVersion;
            snapshot["legacy.discovery.capabilitiesMask"] = ((byte)discovery.Capabilities).ToString(CultureInfo.InvariantCulture);
            snapshot["legacy.discovery.healthMask"] = ((byte)discovery.Health).ToString(CultureInfo.InvariantCulture);
        }

        return snapshot;
    }

    private static DateTimeOffset? ExtractTimestamp(Header? header)
    {
        if (header?.Timestamp is Timestamp timestamp)
        {
            return timestamp.ToDateTimeOffset();
        }

        return null;
    }

    private static string NormalizeLayerNamespace(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "presence";
        }

        return value.Trim();
    }

    private static string NormalizeRequired(string value, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Legacy mesh option '{propertyName}' must be provided.");
        }

        return value.Trim();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string NormalizeIdentifierWithFallback(string? candidate, string fallback)
    {
        return string.IsNullOrWhiteSpace(candidate) ? fallback : candidate.Trim();
    }

    private static IReadOnlyList<string> NormalizeCapabilities(IReadOnlyCollection<string>? capabilities)
    {
        if (capabilities is null || capabilities.Count == 0)
        {
            return Array.Empty<string>();
        }

        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var capability in capabilities)
        {
            if (string.IsNullOrWhiteSpace(capability))
            {
                continue;
            }

            var normalized = capability.Trim();
            set.Add(normalized);
        }

        if (set.Count == 0)
        {
            return Array.Empty<string>();
        }

        var list = new List<string>(set);
        list.Sort(StringComparer.OrdinalIgnoreCase);
        return list;
    }

    private static Dictionary<string, string> NormalizeMetadata(IDictionary<string, string>? metadata)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (metadata is null)
        {
            return result;
        }

        foreach (var pair in metadata)
        {
            if (string.IsNullOrWhiteSpace(pair.Key) || pair.Value is null)
            {
                continue;
            }

            result[pair.Key.Trim()] = pair.Value.Trim();
        }

        return result;
    }
}
