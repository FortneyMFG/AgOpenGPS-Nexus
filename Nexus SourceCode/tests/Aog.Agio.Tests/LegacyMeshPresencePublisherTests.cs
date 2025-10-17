using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aog.Agio.Legacy;
using Aog.Core.Mesh;
using Aog.Core.V1;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Options;
using Xunit;

namespace Aog.Agio.Tests;

public sealed class LegacyMeshPresencePublisherTests
{
    [Fact]
    public void Constructor_RegistersDeviceWithMesh()
    {
        var meshService = new RecordingMeshService();
        var options = Options.Create(new LegacyMeshOptions
        {
            DeviceId = " device:legacy ",
            Label = " Legacy Gateway ",
            DefaultSeasonId = "season:2025",
            DefaultJobId = "job:alpha",
            ShareSeasonId = "*",
            ShareJobId = "*",
            Capabilities = new List<string> { "legacy.gateway", " legacy.pose " },
        });

        _ = new LegacyMeshPresencePublisher(meshService, options);

        var registration = Assert.Single(meshService.Registrations);
        Assert.Equal("device:legacy", registration.DeviceId);
        Assert.Equal("Legacy Gateway", registration.Label);
        Assert.Single(registration.ShareProfile.Grants);
        var grant = registration.ShareProfile.Grants[0];
        Assert.Equal("*", grant.SeasonId);
        Assert.Equal("*", grant.JobId);
    }

    [Fact]
    public async Task PublishPresenceAsync_ForwardsPoseToMeshService()
    {
        var meshService = new RecordingMeshService();
        var options = Options.Create(new LegacyMeshOptions
        {
            DeviceId = "device:legacy",
            Label = "Legacy Gateway",
            DefaultSeasonId = "season:2025",
            DefaultJobId = "job:alpha",
            DefaultSessionId = "session:local",
            Metadata = new Dictionary<string, string> { ["operator"] = "Ada" },
        });

        var publisher = new LegacyMeshPresencePublisher(meshService, options);
        meshService.Registrations.Clear();

        var pose = new Pose
        {
            Header = new Header
            {
                Timestamp = Timestamp.FromDateTimeOffset(new DateTimeOffset(2025, 3, 19, 12, 0, 0, TimeSpan.Zero)),
                JobId = " job:beta ",
                SeasonId = " season:2024 ",
                SessionId = " session:99 ",
            },
            LatitudeDeg = 51.123,
            LongitudeDeg = -114.456,
            HeadingRad = Math.PI / 2,
            AltitudeM = 1123.4,
            SpeedMps = 4.2,
        };

        var metadata = new LegacyPoseMetadata
        {
            FixQuality = 5,
            SatellitesTracked = 18,
            HdopTimes100 = 95,
            AgeOfCorrectionsTimes100 = 150,
            ImuHeadingHundredths = 1234,
            ImuRollHundredths = -45,
        };

        await publisher.PublishPresenceAsync(pose, metadata, CancellationToken.None);

        var update = Assert.Single(meshService.PresenceUpdates);
        Assert.Equal("device:legacy", update.DeviceId);
        Assert.Equal("season:2024", update.Session.SeasonId);
        Assert.Equal("job:beta", update.Session.JobId);
        Assert.Equal("session:99", update.Session.SessionId);
        Assert.NotNull(update.Pose.HeadingDegrees);
        Assert.Equal(90.0, update.Pose.HeadingDegrees.Value, 5);
        Assert.Equal(4.2, update.Pose.SpeedMetersPerSecond);
        Assert.Equal(1123.4, update.Pose.AltitudeMeters);
        Assert.Equal("Ada", update.Metadata!["operator"]);
        Assert.Equal("5", update.Metadata!["legacy.fixQuality"]);
        Assert.Equal("0.95", update.Metadata!["legacy.hdop"]);
        Assert.Equal("1.5", update.Metadata!["legacy.correctionsAgeSeconds"]);
    }

    [Fact]
    public async Task PublishPresenceAsync_DropsNonFinitePoseValues()
    {
        var meshService = new RecordingMeshService();
        var options = Options.Create(new LegacyMeshOptions
        {
            DeviceId = "device:legacy",
            Label = "Legacy Gateway",
            DefaultSeasonId = "season:2025",
            DefaultJobId = "job:alpha",
        });

        var publisher = new LegacyMeshPresencePublisher(meshService, options);
        meshService.Registrations.Clear();

        var pose = new Pose
        {
            LatitudeDeg = double.NaN,
            LongitudeDeg = double.PositiveInfinity,
            AltitudeM = double.NaN,
            HeadingRad = double.NegativeInfinity,
            SpeedMps = double.NaN,
        };

        await publisher.PublishPresenceAsync(pose, new LegacyPoseMetadata(), CancellationToken.None);

        var update = Assert.Single(meshService.PresenceUpdates);
        Assert.Equal(0.0, update.Pose.Latitude);
        Assert.Equal(0.0, update.Pose.Longitude);
        Assert.Null(update.Pose.AltitudeMeters);
        Assert.Null(update.Pose.HeadingDegrees);
        Assert.Null(update.Pose.SpeedMetersPerSecond);
    }

    [Fact]
    public async Task PublishPresenceAsync_UsesDiscoveryMetadata()
    {
        var meshService = new RecordingMeshService();
        var options = Options.Create(new LegacyMeshOptions
        {
            DeviceId = "device:legacy",
            Label = "Legacy Gateway",
            DefaultSeasonId = "season:2025",
            DefaultJobId = "job:alpha",
        });

        var publisher = new LegacyMeshPresencePublisher(meshService, options);
        var announcement = new LegacyDiscoveryAnnouncement
        {
            VendorId = 0x7C,
            ProductId = 0x01,
            VariantId = 0x02,
            McuId = (byte)LegacyDeviceMcu.Teensy,
            FirmwareMajor = 1,
            FirmwareMinor = 2,
            FirmwarePatch = 3,
            Capabilities = LegacyDeviceCapabilityFlags.OverTheAirUpdates,
            Health = LegacyDeviceHealthFlags.None,
        };

        await publisher.OnDiscoveryAsync(announcement, CancellationToken.None);

        var pose = new Pose { LatitudeDeg = 51.0, LongitudeDeg = -114.0 };
        await publisher.PublishPresenceAsync(pose, new LegacyPoseMetadata(), CancellationToken.None);

        var metadata = Assert.Single(meshService.PresenceUpdates).Metadata!;
        Assert.Equal("1.2.3", metadata["legacy.discovery.firmware"]);
        Assert.Equal(((byte)LegacyDeviceCapabilityFlags.OverTheAirUpdates).ToString(), metadata["legacy.discovery.capabilitiesMask"]);
    }

    private sealed class RecordingMeshService : ILiveTelemetryMeshService
    {
        public List<MeshDeviceRegistration> Registrations { get; } = new();
        public List<MeshPresenceUpdate> PresenceUpdates { get; } = new();

        public ValueTask RegisterOrUpdateDeviceAsync(MeshDeviceRegistration registration, CancellationToken cancellationToken = default)
        {
            Registrations.Add(registration);
            return ValueTask.CompletedTask;
        }

        public ValueTask PublishAsync(MeshPublishRequest request, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;

        public IAsyncEnumerable<MeshPublication> SubscribeAsync(MeshSubscriptionRequest request, CancellationToken cancellationToken = default)
        {
            return AsyncEnumerable.Empty<MeshPublication>();
        }

        public ValueTask UpdatePresenceAsync(MeshPresenceUpdate update, CancellationToken cancellationToken = default)
        {
            PresenceUpdates.Add(update);
            return ValueTask.CompletedTask;
        }

        public IReadOnlyList<MeshPresenceSnapshot> ListPresence(string? seasonId = null, string? jobId = null) => Array.Empty<MeshPresenceSnapshot>();
    }
}
