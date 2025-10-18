using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aog.Core.Mesh;
using Aog.Core.V1;
using Aog.Link.V1;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;

namespace Aog.Bridge.Host.AogLink;

/// <summary>
/// Bridges AOG-Link gateway traffic with the live telemetry mesh service.
/// </summary>
public sealed class AogLinkMeshBridge
{
    private const string DefaultSeasonId = "season:unassigned";
    private const string DefaultJobId = "job:unassigned";

    private static readonly MeshShareProfile PresenceShareProfile = new(new[]
    {
        new MeshShareGrant("*", "*", MeshDataTier.Presence, new[] { "presence" })
    });

    private readonly ILiveTelemetryMeshService _meshService;
    private readonly ILogger<AogLinkMeshBridge> _logger;
    private readonly ConcurrentDictionary<uint, MeshDeviceCacheEntry> _devices = new();

    public AogLinkMeshBridge(ILiveTelemetryMeshService meshService, ILogger<AogLinkMeshBridge> logger)
    {
        _meshService = meshService ?? throw new ArgumentNullException(nameof(meshService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Handles translated AOG-Link messages and mirrors them into the mesh service when applicable.
    /// </summary>
    public async Task HandleAsync(LinkEnvelope envelope, object? message, AogLinkMessageKind kind, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        switch (kind)
        {
            case AogLinkMessageKind.DiscoveryAnnounce when message is DiscoveryAnnounce announce:
                await RegisterDeviceAsync(envelope, announce, cancellationToken).ConfigureAwait(false);
                break;
            case AogLinkMessageKind.Pose when message is Pose pose:
                await UpdatePresenceAsync(envelope, pose, cancellationToken).ConfigureAwait(false);
                break;
        }
    }

    private async Task RegisterDeviceAsync(LinkEnvelope envelope, DiscoveryAnnounce announce, CancellationToken cancellationToken)
    {
        var source = envelope.Header?.Source ?? announce.Identity?.NodeId ?? 0;
        var identity = announce.Identity ?? new NodeIdentity();
        var deviceId = BuildDeviceId(source == 0 ? identity.NodeId : source);
        var label = BuildDeviceLabel(identity, source);
        var capabilities = NormalizeCapabilities(announce.CapabilityIds);

        var registration = new MeshDeviceRegistration(
            deviceId,
            label,
            shareProfile: PresenceShareProfile,
            subscribeProfile: MeshSubscribeProfile.Empty)
        {
            Capabilities = capabilities.Length == 0 ? null : capabilities
        };

        await _meshService.RegisterOrUpdateDeviceAsync(registration, cancellationToken).ConfigureAwait(false);

        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(identity.HardwareModel))
        {
            metadata["hardwareModel"] = identity.HardwareModel.Trim();
        }

        if (!string.IsNullOrWhiteSpace(identity.FirmwareVersion))
        {
            metadata["firmwareVersion"] = identity.FirmwareVersion.Trim();
        }

        metadata["role"] = identity.Role.ToString();
        metadata["priority"] = identity.Priority.ToString();

        if (capabilities.Length > 0)
        {
            metadata["capabilities"] = string.Join(',', capabilities);
        }

        var record = new MeshDeviceCacheEntry(deviceId, metadata);
        _devices[source] = record;

        _logger.LogDebug("Registered mesh device {DeviceId} for AOG-Link node {Source}.", deviceId, source);
    }

    private async Task UpdatePresenceAsync(LinkEnvelope envelope, Pose pose, CancellationToken cancellationToken)
    {
        var source = envelope.Header?.Source ?? 0;
        var device = await EnsureDeviceRegisteredAsync(source, cancellationToken).ConfigureAwait(false);

        var header = pose.Header;
        var seasonId = NormalizeOrDefault(header?.SeasonId, DefaultSeasonId);
        var jobId = NormalizeOrDefault(header?.JobId, DefaultJobId);
        var sessionId = string.IsNullOrWhiteSpace(header?.SessionId) ? null : header!.SessionId.Trim();
        var timestamp = header?.Timestamp?.ToDateTimeOffset();

        var meshPose = new MeshPose(
            pose.LatitudeDeg,
            pose.LongitudeDeg,
            pose.AltitudeM,
            ConvertHeadingToDegrees(pose.HeadingRad),
            pose.SpeedMps);

        var metadata = BuildPresenceMetadata(device.Metadata, header, source);

        var update = new MeshPresenceUpdate(
            device.DeviceId,
            new MeshSessionDescriptor(seasonId, jobId, sessionId),
            meshPose,
            MeshPresenceState.Online,
            metadata,
            timestamp);

        await _meshService.UpdatePresenceAsync(update, cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask<MeshDeviceCacheEntry> EnsureDeviceRegisteredAsync(uint source, CancellationToken cancellationToken)
    {
        if (_devices.TryGetValue(source, out var record))
        {
            return record;
        }

        var deviceId = BuildDeviceId(source);
        var label = BuildFallbackLabel(source);

        var registration = new MeshDeviceRegistration(
            deviceId,
            label,
            shareProfile: PresenceShareProfile,
            subscribeProfile: MeshSubscribeProfile.Empty);

        await _meshService.RegisterOrUpdateDeviceAsync(registration, cancellationToken).ConfigureAwait(false);

        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["registration"] = "inferred"
        };

        var cacheEntry = new MeshDeviceCacheEntry(deviceId, metadata);
        _devices[source] = cacheEntry;

        _logger.LogDebug("Registered inferred mesh device {DeviceId} for AOG-Link node {Source}.", deviceId, source);
        return cacheEntry;
    }

    private static string BuildDeviceId(uint source) => $"aog-link:{source}";

    private static string BuildDeviceLabel(NodeIdentity identity, uint source)
    {
        if (!string.IsNullOrWhiteSpace(identity.HardwareModel) && !string.IsNullOrWhiteSpace(identity.FirmwareVersion))
        {
            return $"{identity.HardwareModel.Trim()} FW {identity.FirmwareVersion.Trim()}";
        }

        if (!string.IsNullOrWhiteSpace(identity.HardwareModel))
        {
            return identity.HardwareModel.Trim();
        }

        if (!string.IsNullOrWhiteSpace(identity.FirmwareVersion))
        {
            return $"AOG-Link Node {source} FW {identity.FirmwareVersion.Trim()}";
        }

        return BuildFallbackLabel(source);
    }

    private static string BuildFallbackLabel(uint source) => $"AOG-Link Node {source}";

    private static string[] NormalizeCapabilities(IEnumerable<string> capabilityIds)
    {
        return capabilityIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string NormalizeOrDefault(string? value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value!.Trim();
    }

    private static double? ConvertHeadingToDegrees(double headingRadians)
    {
        return double.IsNaN(headingRadians) ? null : headingRadians * 180.0 / Math.PI;
    }

    private static IReadOnlyDictionary<string, string> BuildPresenceMetadata(
        IReadOnlyDictionary<string, string> baseMetadata,
        Header? header,
        uint source)
    {
        var metadata = new Dictionary<string, string>(baseMetadata, StringComparer.OrdinalIgnoreCase)
        {
            ["linkSource"] = $"0x{source:X4}"
        };

        if (!string.IsNullOrWhiteSpace(header?.Source))
        {
            metadata["telemetrySource"] = header!.Source.Trim();
        }

        if (!string.IsNullOrWhiteSpace(header?.Frame))
        {
            metadata["frame"] = header!.Frame.Trim();
        }

        return metadata;
    }

    private sealed record MeshDeviceCacheEntry(string DeviceId, IReadOnlyDictionary<string, string> Metadata);
}
