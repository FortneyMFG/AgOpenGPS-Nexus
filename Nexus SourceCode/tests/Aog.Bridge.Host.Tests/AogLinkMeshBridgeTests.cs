using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aog.Bridge.Host.AogLink;
using Aog.Core.Mesh;
using Aog.Core.V1;
using Aog.Link.V1;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aog.Bridge.Host.Tests;

public sealed class AogLinkMeshBridgeTests
{
    [Fact]
    public async Task HandleAsync_DiscoveryAnnounceRegistersMeshDevice()
    {
        var mesh = new FakeMeshService();
        var bridge = new AogLinkMeshBridge(mesh, NullLogger<AogLinkMeshBridge>.Instance);

        var envelope = new LinkEnvelope
        {
            Header = new FrameHeader
            {
                Source = 42,
                MessageClass = LinkClass.System,
                MessageType = MessageType.LinkMessageTypeDiscoveryAnnounce,
            },
            DiscoveryAnnounce = new DiscoveryAnnounce
            {
                Identity = new NodeIdentity
                {
                    NodeId = 42,
                    HardwareModel = "AGIO-MCU",
                    FirmwareVersion = "1.2.3",
                    Role = NodeRole.NodeRoleController,
                    Priority = NodePriority.Default,
                },
            },
        };

        envelope.DiscoveryAnnounce.CapabilityIds.AddRange(new[] { "presence", " coverage " });

        await bridge.HandleAsync(envelope, envelope.DiscoveryAnnounce, AogLinkMessageKind.DiscoveryAnnounce, CancellationToken.None);

        var registration = Assert.Single(mesh.Registrations);
        Assert.Equal("aog-link:42", registration.DeviceId);
        Assert.Equal("AGIO-MCU FW 1.2.3", registration.Label);
        Assert.Equal(new[] { "coverage", "presence" }, registration.Capabilities?.ToArray());

        var grant = Assert.Single(registration.ShareProfile!.Grants);
        Assert.Equal("*", grant.SeasonId);
        Assert.Equal("*", grant.JobId);
        Assert.Equal(MeshDataTier.Presence, grant.Tiers);
    }

    [Fact]
    public async Task HandleAsync_PosePublishesPresenceUpdate()
    {
        var mesh = new FakeMeshService();
        var bridge = new AogLinkMeshBridge(mesh, NullLogger<AogLinkMeshBridge>.Instance);

        var pose = new Pose
        {
            Header = new Header
            {
                SeasonId = " season:2025 ",
                JobId = " job:alpha ",
                SessionId = "session:1",
                Source = "mcu",
                Frame = "vehicle",
                Timestamp = Timestamp.FromDateTimeOffset(new DateTimeOffset(2025, 3, 19, 12, 0, 0, TimeSpan.Zero)),
            },
            LatitudeDeg = 45.123,
            LongitudeDeg = -96.321,
            AltitudeM = 310.5,
            HeadingRad = Math.PI / 2,
            SpeedMps = 5.2,
        };

        var envelope = new LinkEnvelope
        {
            Header = new FrameHeader
            {
                Source = 7,
                MessageClass = LinkClass.Telemetry,
                MessageType = MessageType.LinkMessageTypeTelemetryPose,
            },
            Pose = pose,
        };

        await bridge.HandleAsync(envelope, pose, AogLinkMessageKind.Pose, CancellationToken.None);

        var registration = Assert.Single(mesh.Registrations);
        Assert.Equal("aog-link:7", registration.DeviceId);
        Assert.Equal("AOG-Link Node 7", registration.Label);

        var update = Assert.Single(mesh.PresenceUpdates);
        Assert.Equal("aog-link:7", update.DeviceId);
        Assert.Equal("season:2025", update.Session.SeasonId);
        Assert.Equal("job:alpha", update.Session.JobId);
        Assert.Equal("session:1", update.Session.SessionId);
        Assert.Equal(45.123, update.Pose.Latitude, 6);
        Assert.Equal(-96.321, update.Pose.Longitude, 6);
        Assert.Equal(310.5, update.Pose.AltitudeMeters!.Value, 6);
        Assert.Equal(90.0, update.Pose.HeadingDegrees!.Value, 6);
        Assert.Equal(5.2, update.Pose.SpeedMetersPerSecond!.Value, 6);
        Assert.Equal(new DateTimeOffset(2025, 3, 19, 12, 0, 0, TimeSpan.Zero), update.Timestamp);

        Assert.Equal("mcu", update.Metadata["telemetrySource"]);
        Assert.Equal("vehicle", update.Metadata["frame"]);
        Assert.Equal("0x0007", update.Metadata["linkSource"]);
        Assert.Equal("inferred", update.Metadata["registration"]);
    }

    private sealed class FakeMeshService : ILiveTelemetryMeshService
    {
        public List<MeshDeviceRegistration> Registrations { get; } = new();
        public List<MeshPresenceUpdate> PresenceUpdates { get; } = new();
        public List<MeshPublishRequest> PublishRequests { get; } = new();

        public ValueTask RegisterOrUpdateDeviceAsync(MeshDeviceRegistration registration, CancellationToken cancellationToken = default)
        {
            Registrations.Add(registration);
            return ValueTask.CompletedTask;
        }

        public ValueTask PublishAsync(MeshPublishRequest request, CancellationToken cancellationToken = default)
        {
            PublishRequests.Add(request);
            return ValueTask.CompletedTask;
        }

        public IAsyncEnumerable<MeshPublication> SubscribeAsync(MeshSubscriptionRequest request, CancellationToken cancellationToken = default)
        {
            return AsyncEnumerable.Empty<MeshPublication>();
        }

        public ValueTask UpdatePresenceAsync(MeshPresenceUpdate update, CancellationToken cancellationToken = default)
        {
            PresenceUpdates.Add(update);
            return ValueTask.CompletedTask;
        }

        public IReadOnlyList<MeshPresenceSnapshot> ListPresence(string? seasonId = null, string? jobId = null)
        {
            return Array.Empty<MeshPresenceSnapshot>();
        }
    }
}
