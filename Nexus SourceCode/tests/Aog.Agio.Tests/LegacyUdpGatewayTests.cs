using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aog.Agio;
using Aog.Agio.Legacy;
using Aog.Core.V1;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Aog.Agio.Tests;

public sealed class LegacyUdpGatewayTests
{
    [Fact]
    public async Task PublishPoseAsync_SendsEncodedFrame()
    {
        var poseCodec = new LegacyPoseCodec();
        var discoveryCodec = new LegacyDiscoveryCodec();
        var steerCodec = new LegacySteerCodec();
        var transport = new RecordingTransport();
        var observer = new RecordingObserver();
        var discoveryObserver = new RecordingDiscoveryObserver();
        var steerCommandObserver = new RecordingSteerCommandObserver();
        var steerStateObserver = new RecordingSteerStateObserver();
        var sectionObserver = new RecordingSectionObserver();
        var gateway = new LegacyUdpGateway(
            poseCodec,
            discoveryCodec,
            steerCodec,
            transport,
            observer,
            discoveryObserver,
            steerCommandObserver,
            steerStateObserver,
            sectionObserver,
            NullLegacyMeshPresencePublisher.Instance,
            new FixedTimeProvider(DateTimeOffset.UtcNow));

        var pose = new Pose { LatitudeDeg = 51.2, LongitudeDeg = -114.1 };
        var metadata = new LegacyPoseMetadata { FixQuality = 4, SatellitesTracked = 17 };

        await gateway.PublishPoseAsync(pose, metadata);

        Assert.Single(transport.Frames);
        Assert.True(poseCodec.TryDecodePose(transport.Frames[0].Span, out var decodedPose, out var decodedMetadata));
        Assert.Equal(pose.LatitudeDeg, decodedPose.LatitudeDeg, 6);
        Assert.Equal(metadata.FixQuality, decodedMetadata.FixQuality);
        Assert.Equal(metadata.SatellitesTracked, decodedMetadata.SatellitesTracked);
    }

    [Fact]
    public async Task PublishSteerCommandAsync_UsesActuatorFailsafe()
    {
        var poseCodec = new LegacyPoseCodec();
        var discoveryCodec = new LegacyDiscoveryCodec();
        var steerCodec = new LegacySteerCodec();
        var transport = new RecordingTransport();
        var observer = new RecordingObserver();
        var discoveryObserver = new RecordingDiscoveryObserver();
        var steerCommandObserver = new RecordingSteerCommandObserver();
        var steerStateObserver = new RecordingSteerStateObserver();
        var sectionObserver = new RecordingSectionObserver();
        var failsafe = new RecordingFailsafeService
        {
            SteerResult = new SteerCmd
            {
                Enable = false,
                TargetWheelAngleDeg = -12.5,
                FeedForward = 0.25,
                ControllerOutput = -0.5,
            },
            SectionResult = new SectionMask
            {
                SectionCount = 16,
                Mask = 0x00AA,
            },
        };

        var gateway = new LegacyUdpGateway(
            poseCodec,
            discoveryCodec,
            steerCodec,
            transport,
            observer,
            discoveryObserver,
            steerCommandObserver,
            steerStateObserver,
            sectionObserver,
            NullLegacyMeshPresencePublisher.Instance,
            new FixedTimeProvider(DateTimeOffset.UtcNow),
            failsafe);

        var command = new SteerCmd
        {
            Enable = true,
            TargetWheelAngleDeg = 3.4,
            FeedForward = 0.1,
            ControllerOutput = 0.2,
        };

        var sections = new SectionMask
        {
            SectionCount = 16,
            Mask = 0x00FF,
        };

        await gateway.PublishSteerCommandAsync(command, sections).ConfigureAwait(false);

        Assert.True(failsafe.ReportHeartbeatCalled);
        Assert.Same(command, failsafe.LastSteerCommand);
        Assert.Same(sections, failsafe.LastSectionMask);

        var frame = Assert.Single(transport.Frames);
        Assert.True(steerCodec.TryDecodeSteerCommand(frame.Span, out var decoded, out _, out var decodedSections));
        Assert.False(decoded.Enable);
        Assert.Equal(failsafe.SteerResult.TargetWheelAngleDeg, decoded.TargetWheelAngleDeg, 3);
        Assert.Equal(failsafe.SectionResult.Mask, decodedSections.Mask);
    }

    [Fact]
    public async Task PublishSectionMaskAsync_UsesLastFilteredCommandSnapshot()
    {
        var poseCodec = new LegacyPoseCodec();
        var discoveryCodec = new LegacyDiscoveryCodec();
        var steerCodec = new LegacySteerCodec();
        var transport = new RecordingTransport();
        var poseObserver = new RecordingObserver();
        var discoveryObserver = new RecordingDiscoveryObserver();
        var steerCommandObserver = new RecordingSteerCommandObserver();
        var steerStateObserver = new RecordingSteerStateObserver();
        var sectionObserver = new RecordingSectionObserver();
        var sanitizedCommand = new SteerCmd
        {
            Enable = true,
            TargetWheelAngleDeg = 6.5,
            FeedForward = 0.15,
            ControllerOutput = -0.35,
        };
        var expectedSnapshot = sanitizedCommand.Clone();
        var sanitizedSections = new SectionMask
        {
            SectionCount = 8,
            Mask = 0x0003,
        };
        var failsafe = new RecordingFailsafeService
        {
            SectionResult = sanitizedSections,
        };
        var steerCallCount = 0;
        failsafe.FilterSteerCommandCallback = command =>
        {
            steerCallCount++;
            return steerCallCount == 1 ? sanitizedCommand : command;
        };

        var gateway = new LegacyUdpGateway(
            poseCodec,
            discoveryCodec,
            steerCodec,
            transport,
            poseObserver,
            discoveryObserver,
            steerCommandObserver,
            steerStateObserver,
            sectionObserver,
            NullLegacyMeshPresencePublisher.Instance,
            new FixedTimeProvider(DateTimeOffset.UtcNow),
            failsafe);

        var initialCommand = new SteerCmd
        {
            Enable = false,
            TargetWheelAngleDeg = -1.5,
            FeedForward = 0.05,
            ControllerOutput = 0.25,
        };

        var initialSections = new SectionMask
        {
            SectionCount = 8,
            Mask = 0x000F,
        };

        await gateway.PublishSteerCommandAsync(initialCommand, initialSections).ConfigureAwait(false);

        transport.Frames.Clear();

        failsafe.SectionResult = new SectionMask
        {
            SectionCount = 8,
            Mask = 0x00F0,
        };

        sanitizedCommand.TargetWheelAngleDeg = 42.0;
        sanitizedCommand.ControllerOutput = 0.9;

        var updatedSections = new SectionMask
        {
            SectionCount = 8,
            Mask = 0x00F0,
        };

        await gateway.PublishSectionMaskAsync(updatedSections).ConfigureAwait(false);

        var frame = Assert.Single(transport.Frames);
        Assert.True(steerCodec.TryDecodeSteerCommand(frame.Span, out var decodedCommand, out _, out var decodedSections));
        Assert.Equal(expectedSnapshot.Enable, decodedCommand.Enable);
        Assert.Equal(expectedSnapshot.TargetWheelAngleDeg, decodedCommand.TargetWheelAngleDeg, 3);
        Assert.Equal(expectedSnapshot.FeedForward, decodedCommand.FeedForward, 3);
        Assert.Equal(expectedSnapshot.ControllerOutput, decodedCommand.ControllerOutput, 3);
        Assert.Equal(failsafe.SectionResult!.Mask, decodedSections.Mask);
        Assert.Equal(failsafe.SectionResult.SectionCount, decodedSections.SectionCount);
    }

    [Fact]
    public async Task PublishSectionMaskAsync_ReevaluatesFailsafeOnSend()
    {
        var poseCodec = new LegacyPoseCodec();
        var discoveryCodec = new LegacyDiscoveryCodec();
        var steerCodec = new LegacySteerCodec();
        var transport = new RecordingTransport();
        var poseObserver = new RecordingObserver();
        var discoveryObserver = new RecordingDiscoveryObserver();
        var steerCommandObserver = new RecordingSteerCommandObserver();
        var steerStateObserver = new RecordingSteerStateObserver();
        var sectionObserver = new RecordingSectionObserver();
        var safeCommand = new SteerCmd
        {
            Enable = false,
            TargetWheelAngleDeg = 0,
            FeedForward = 0,
            ControllerOutput = 0,
        };
        var safeSections = new SectionMask
        {
            SectionCount = 8,
            Mask = 0x0000,
        };
        var failsafe = new RecordingFailsafeService();
        var steerCall = 0;
        failsafe.FilterSteerCommandCallback = command =>
        {
            steerCall++;
            return steerCall == 1 ? command.Clone() : safeCommand;
        };
        var sectionCall = 0;
        failsafe.FilterSectionMaskCallback = mask =>
        {
            sectionCall++;
            return sectionCall == 1 ? mask.Clone() : safeSections;
        };

        var gateway = new LegacyUdpGateway(
            poseCodec,
            discoveryCodec,
            steerCodec,
            transport,
            poseObserver,
            discoveryObserver,
            steerCommandObserver,
            steerStateObserver,
            sectionObserver,
            NullLegacyMeshPresencePublisher.Instance,
            new FixedTimeProvider(DateTimeOffset.UtcNow),
            failsafe);

        var initialCommand = new SteerCmd
        {
            Enable = true,
            TargetWheelAngleDeg = 12.5,
            FeedForward = 0.2,
            ControllerOutput = 0.4,
        };

        var initialSections = new SectionMask
        {
            SectionCount = 8,
            Mask = 0x00FF,
        };

        await gateway.PublishSteerCommandAsync(initialCommand, initialSections).ConfigureAwait(false);

        transport.Frames.Clear();

        var updatedSections = new SectionMask
        {
            SectionCount = 8,
            Mask = 0x00F0,
        };

        await gateway.PublishSectionMaskAsync(updatedSections).ConfigureAwait(false);

        var frame = Assert.Single(transport.Frames);
        Assert.True(steerCodec.TryDecodeSteerCommand(frame.Span, out var decodedCommand, out _, out var decodedSections));
        Assert.Equal(safeCommand.Enable, decodedCommand.Enable);
        Assert.Equal(safeCommand.TargetWheelAngleDeg, decodedCommand.TargetWheelAngleDeg, 3);
        Assert.Equal(safeCommand.FeedForward, decodedCommand.FeedForward, 3);
        Assert.Equal(safeCommand.ControllerOutput, decodedCommand.ControllerOutput, 3);
        Assert.Equal(safeSections.Mask, decodedSections.Mask);
        Assert.Equal(safeSections.SectionCount, decodedSections.SectionCount);
    }

    [Fact]
    public async Task PublishSectionMaskAsync_WithoutSteerSnapshotLogsWarning()
    {
        var poseCodec = new LegacyPoseCodec();
        var discoveryCodec = new LegacyDiscoveryCodec();
        var steerCodec = new LegacySteerCodec();
        var transport = new RecordingTransport();
        var poseObserver = new RecordingObserver();
        var discoveryObserver = new RecordingDiscoveryObserver();
        var steerCommandObserver = new RecordingSteerCommandObserver();
        var steerStateObserver = new RecordingSteerStateObserver();
        var sectionObserver = new RecordingSectionObserver();
        var logger = new RecordingLogger<LegacyUdpGateway>();

        var gateway = new LegacyUdpGateway(
            poseCodec,
            discoveryCodec,
            steerCodec,
            transport,
            poseObserver,
            discoveryObserver,
            steerCommandObserver,
            steerStateObserver,
            sectionObserver,
            NullLegacyMeshPresencePublisher.Instance,
            new FixedTimeProvider(DateTimeOffset.UtcNow),
            logger: logger);

        var sections = new SectionMask
        {
            SectionCount = 8,
            Mask = 0x000F,
        };

        await gateway.PublishSectionMaskAsync(sections).ConfigureAwait(false);

        Assert.Empty(transport.Frames);

        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Information, entry.Level);
        Assert.Contains(
            "no steer command snapshot has been published",
            entry.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task HandleDatagramAsync_ForwardsPoseToObserver()
    {
        var poseCodec = new LegacyPoseCodec();
        var discoveryCodec = new LegacyDiscoveryCodec();
        var steerCodec = new LegacySteerCodec();
        var transport = new RecordingTransport();
        var observer = new RecordingObserver();
        var discoveryObserver = new RecordingDiscoveryObserver();
        var timestamp = new DateTimeOffset(2024, 05, 01, 12, 30, 00, TimeSpan.Zero);
        var steerCommandObserver = new RecordingSteerCommandObserver();
        var steerStateObserver = new RecordingSteerStateObserver();
        var sectionObserver = new RecordingSectionObserver();
        var meshPublisher = new RecordingMeshPresencePublisher();
        var gateway = new LegacyUdpGateway(
            poseCodec,
            discoveryCodec,
            steerCodec,
            transport,
            observer,
            discoveryObserver,
            steerCommandObserver,
            steerStateObserver,
            sectionObserver,
            meshPublisher,
            new FixedTimeProvider(timestamp));

        var pose = new Pose { LatitudeDeg = 51.123, LongitudeDeg = -114.456, HeadingRad = 1.5 };
        var metadata = new LegacyPoseMetadata { FixQuality = 5, SatellitesTracked = 12 };
        var frame = poseCodec.EncodePose(pose, metadata);

        await gateway.HandleDatagramAsync(frame);

        Assert.Single(observer.Poses);
        var observed = observer.Poses[0];
        Assert.NotNull(observed.Header);
        Assert.Equal("legacy/udp/main_gps", observed.Header.Source);
        Assert.Equal("earth", observed.Header.Frame);
        Assert.Equal(1UL, observed.Header.Sequence);
        Assert.Equal(Timestamp.FromDateTimeOffset(timestamp), observed.Header.Timestamp);
        Assert.Equal(pose.LatitudeDeg, observed.LatitudeDeg, 6);

        Assert.Single(observer.Metadata);
        var publishedPose = Assert.Single(meshPublisher.Poses);
        Assert.Equal(pose.LatitudeDeg, publishedPose.LatitudeDeg, 6);
        var publishedMetadata = Assert.Single(meshPublisher.Metadata);
        Assert.Equal(metadata.SatellitesTracked, publishedMetadata.SatellitesTracked);
    }

    [Fact]
    public async Task HandleDatagramAsync_ForwardsDiscoveryToMeshPublisher()
    {
        var poseCodec = new LegacyPoseCodec();
        var discoveryCodec = new LegacyDiscoveryCodec();
        var steerCodec = new LegacySteerCodec();
        var transport = new RecordingTransport();
        var poseObserver = new RecordingObserver();
        var discoveryObserver = new RecordingDiscoveryObserver();
        var steerCommandObserver = new RecordingSteerCommandObserver();
        var steerStateObserver = new RecordingSteerStateObserver();
        var sectionObserver = new RecordingSectionObserver();
        var meshPublisher = new RecordingMeshPresencePublisher();
        var gateway = new LegacyUdpGateway(
            poseCodec,
            discoveryCodec,
            steerCodec,
            transport,
            poseObserver,
            discoveryObserver,
            steerCommandObserver,
            steerStateObserver,
            sectionObserver,
            meshPublisher,
            new FixedTimeProvider(DateTimeOffset.UtcNow));

        var announcement = new LegacyDiscoveryAnnouncement
        {
            VendorId = 0x7C,
            ProductId = 0x01,
            VariantId = 0x02,
            McuId = (byte)LegacyDeviceMcu.Stm32,
            FirmwareMajor = 2,
            FirmwareMinor = 5,
            FirmwarePatch = 9,
            Capabilities = LegacyDeviceCapabilityFlags.CanBootloader,
            Health = LegacyDeviceHealthFlags.VoltageLow,
        };

        var frame = discoveryCodec.Encode(announcement);

        await gateway.HandleDatagramAsync(frame);

        Assert.Single(meshPublisher.Discoveries);
        Assert.Equal(announcement.FirmwareMajor, meshPublisher.Discoveries[0].FirmwareMajor);
    }

    [Fact]
    public async Task HandleDatagramAsync_IgnoresInvalidFrame()
    {
        var poseCodec = new LegacyPoseCodec();
        var discoveryCodec = new LegacyDiscoveryCodec();
        var gateway = new LegacyUdpGateway(
            poseCodec,
            discoveryCodec,
            new LegacySteerCodec(),
            new RecordingTransport(),
            new RecordingObserver(),
            new RecordingDiscoveryObserver(),
            new RecordingSteerCommandObserver(),
            new RecordingSteerStateObserver(),
            new RecordingSectionObserver(),
            NullLegacyMeshPresencePublisher.Instance,
            new FixedTimeProvider(DateTimeOffset.UtcNow));
        var invalid = new byte[LegacyPoseCodec.MainAntennaFrameLength];

        await gateway.HandleDatagramAsync(invalid);

        // No exception and no observer calls expected.
    }

    [Fact]
    public async Task PublishDiscoveryAsync_SendsAnnouncement()
    {
        var poseCodec = new LegacyPoseCodec();
        var discoveryCodec = new LegacyDiscoveryCodec();
        var steerCodec = new LegacySteerCodec();
        var transport = new RecordingTransport();
        var steerCommandObserver = new RecordingSteerCommandObserver();
        var steerStateObserver = new RecordingSteerStateObserver();
        var sectionObserver = new RecordingSectionObserver();
        var gateway = new LegacyUdpGateway(
            poseCodec,
            discoveryCodec,
            steerCodec,
            transport,
            new RecordingObserver(),
            new RecordingDiscoveryObserver(),
            steerCommandObserver,
            steerStateObserver,
            sectionObserver,
            NullLegacyMeshPresencePublisher.Instance,
            new FixedTimeProvider(DateTimeOffset.UtcNow));

        var announcement = new LegacyDiscoveryAnnouncement
        {
            VendorId = 0x7C,
            ProductId = 0x01,
            VariantId = 0x02,
            McuId = (byte)LegacyDeviceMcu.Teensy,
            FirmwareMajor = 1,
            FirmwareMinor = 2,
            FirmwarePatch = 3,
            Capabilities = LegacyDeviceCapabilityFlags.OverTheAirUpdates | LegacyDeviceCapabilityFlags.DualBankFirmware,
            Health = LegacyDeviceHealthFlags.None,
        };

        await gateway.PublishDiscoveryAsync(announcement);

        var frame = Assert.Single(transport.Frames);
        Assert.True(discoveryCodec.TryDecode(frame.Span, out var decoded));
        Assert.Equal(announcement.VendorId, decoded.VendorId);
        Assert.Equal(announcement.ProductId, decoded.ProductId);
        Assert.Equal(announcement.VariantId, decoded.VariantId);
        Assert.Equal(announcement.McuId, decoded.McuId);
        Assert.Equal(announcement.FirmwareMajor, decoded.FirmwareMajor);
        Assert.Equal(announcement.FirmwareMinor, decoded.FirmwareMinor);
        Assert.Equal(announcement.FirmwarePatch, decoded.FirmwarePatch);
        Assert.Equal(announcement.Capabilities, decoded.Capabilities);
    }

    [Fact]
    public async Task HandleDatagramAsync_ForwardsDiscovery()
    {
        var poseCodec = new LegacyPoseCodec();
        var discoveryCodec = new LegacyDiscoveryCodec();
        var steerCodec = new LegacySteerCodec();
        var transport = new RecordingTransport();
        var poseObserver = new RecordingObserver();
        var discoveryObserver = new RecordingDiscoveryObserver();
        var steerCommandObserver = new RecordingSteerCommandObserver();
        var steerStateObserver = new RecordingSteerStateObserver();
        var sectionObserver = new RecordingSectionObserver();
        var gateway = new LegacyUdpGateway(
            poseCodec,
            discoveryCodec,
            steerCodec,
            transport,
            poseObserver,
            discoveryObserver,
            steerCommandObserver,
            steerStateObserver,
            sectionObserver,
            NullLegacyMeshPresencePublisher.Instance,
            new FixedTimeProvider(DateTimeOffset.UtcNow));

        var announcement = new LegacyDiscoveryAnnouncement
        {
            VendorId = 0x7C,
            ProductId = 0x01,
            VariantId = 0x02,
            McuId = (byte)LegacyDeviceMcu.Stm32,
            FirmwareMajor = 2,
            FirmwareMinor = 5,
            FirmwarePatch = 9,
            Capabilities = LegacyDeviceCapabilityFlags.CanBootloader | LegacyDeviceCapabilityFlags.UsbDfu,
            Health = LegacyDeviceHealthFlags.VoltageLow,
        };

        var frame = discoveryCodec.Encode(announcement);

        await gateway.HandleDatagramAsync(frame);

        Assert.Empty(poseObserver.Poses);
        var observed = Assert.Single(discoveryObserver.Announcements);
        Assert.Equal(announcement.VendorId, observed.VendorId);
        Assert.Equal(announcement.FirmwareVersion, observed.FirmwareVersion);
        Assert.Equal(announcement.Capabilities, observed.Capabilities);
        Assert.Equal(announcement.Health, observed.Health);
    }

    [Fact]
    public async Task PublishSteerCommandAsync_SendsEncodedFrame()
    {
        var poseCodec = new LegacyPoseCodec();
        var discoveryCodec = new LegacyDiscoveryCodec();
        var steerCodec = new LegacySteerCodec();
        var transport = new RecordingTransport();
        var poseObserver = new RecordingObserver();
        var discoveryObserver = new RecordingDiscoveryObserver();
        var steerCommandObserver = new RecordingSteerCommandObserver();
        var steerStateObserver = new RecordingSteerStateObserver();
        var sectionObserver = new RecordingSectionObserver();
        var gateway = new LegacyUdpGateway(
            poseCodec,
            discoveryCodec,
            steerCodec,
            transport,
            poseObserver,
            discoveryObserver,
            steerCommandObserver,
            steerStateObserver,
            sectionObserver,
            NullLegacyMeshPresencePublisher.Instance,
            new FixedTimeProvider(DateTimeOffset.UtcNow));

        var command = new SteerCmd { TargetWheelAngleDeg = 2.5, Enable = true };
        var sections = new SectionMask { SectionCount = 8, Mask = 0b1010_0101 };
        var metadata = new LegacySteerCommandMetadata { SpeedKph = 12.3, GuidanceStatus = 0, TramControl = 0x33 };

        await gateway.PublishSteerCommandAsync(command, sections, metadata);

        var frame = Assert.Single(transport.Frames);
        Assert.True(steerCodec.TryDecodeSteerCommand(frame.Span, out var decodedCommand, out var decodedMetadata, out var decodedSections));
        Assert.Equal(command.TargetWheelAngleDeg, decodedCommand.TargetWheelAngleDeg, 2);
        Assert.True(decodedCommand.Enable);
        Assert.Equal(1, decodedMetadata.GuidanceStatus);
        Assert.Equal(metadata.TramControl, decodedMetadata.TramControl);
        Assert.Equal((uint)(sections.Mask & 0xFFFF), decodedSections.Mask);
    }

    [Fact]
    public async Task HandleDatagramAsync_ForwardsSteerCommandAndSections()
    {
        var poseCodec = new LegacyPoseCodec();
        var discoveryCodec = new LegacyDiscoveryCodec();
        var steerCodec = new LegacySteerCodec();
        var transport = new RecordingTransport();
        var poseObserver = new RecordingObserver();
        var discoveryObserver = new RecordingDiscoveryObserver();
        var steerCommandObserver = new RecordingSteerCommandObserver();
        var steerStateObserver = new RecordingSteerStateObserver();
        var sectionObserver = new RecordingSectionObserver();
        var timestamp = new DateTimeOffset(2024, 05, 02, 08, 45, 00, TimeSpan.Zero);
        var gateway = new LegacyUdpGateway(
            poseCodec,
            discoveryCodec,
            steerCodec,
            transport,
            poseObserver,
            discoveryObserver,
            steerCommandObserver,
            steerStateObserver,
            sectionObserver,
            NullLegacyMeshPresencePublisher.Instance,
            new FixedTimeProvider(timestamp));

        var command = new SteerCmd { TargetWheelAngleDeg = -4.5, Enable = true };
        var sections = new SectionMask { SectionCount = 12, Mask = 0b1010_0001_0101 };
        var metadata = new LegacySteerCommandMetadata { SpeedKph = 15.2, GuidanceStatus = 3, TramControl = 7 };
        var frame = steerCodec.EncodeSteerCommand(command, sections, metadata);

        await gateway.HandleDatagramAsync(frame);

        var recordedCommand = Assert.Single(steerCommandObserver.Commands);
        Assert.NotNull(recordedCommand.Header);
        Assert.Equal("legacy/udp/steer_cmd", recordedCommand.Header.Source);
        Assert.Equal(1UL, recordedCommand.Header.Sequence);
        Assert.Equal(Timestamp.FromDateTimeOffset(timestamp), recordedCommand.Header.Timestamp);
        Assert.Equal(command.TargetWheelAngleDeg, recordedCommand.TargetWheelAngleDeg, 3);

        var recordedMetadata = Assert.Single(steerCommandObserver.Metadata);
        Assert.Equal(metadata.GuidanceStatus, recordedMetadata.GuidanceStatus);
        Assert.Equal(metadata.SpeedKph, recordedMetadata.SpeedKph, 6);
        Assert.Equal(metadata.TramControl, recordedMetadata.TramControl);

        var recordedSection = Assert.Single(sectionObserver.Masks);
        Assert.NotNull(recordedSection.Header);
        Assert.Equal("legacy/udp/sections", recordedSection.Header.Source);
        Assert.Equal(1UL, recordedSection.Header.Sequence);
        Assert.Equal(Timestamp.FromDateTimeOffset(timestamp), recordedSection.Header.Timestamp);
        Assert.Equal((uint)(sections.Mask & 0xFFFF), recordedSection.Mask);
        Assert.Equal(16u, recordedSection.SectionCount);

        Assert.Empty(poseObserver.Poses);
        Assert.Empty(discoveryObserver.Announcements);
    }

    [Fact]
    public async Task HandleDatagramAsync_ForwardsSteerState()
    {
        var poseCodec = new LegacyPoseCodec();
        var discoveryCodec = new LegacyDiscoveryCodec();
        var steerCodec = new LegacySteerCodec();
        var transport = new RecordingTransport();
        var poseObserver = new RecordingObserver();
        var discoveryObserver = new RecordingDiscoveryObserver();
        var steerCommandObserver = new RecordingSteerCommandObserver();
        var steerStateObserver = new RecordingSteerStateObserver();
        var sectionObserver = new RecordingSectionObserver();
        var timestamp = new DateTimeOffset(2024, 05, 03, 09, 15, 00, TimeSpan.Zero);
        var gateway = new LegacyUdpGateway(
            poseCodec,
            discoveryCodec,
            steerCodec,
            transport,
            poseObserver,
            discoveryObserver,
            steerCommandObserver,
            steerStateObserver,
            sectionObserver,
            NullLegacyMeshPresencePublisher.Instance,
            new FixedTimeProvider(timestamp));

        var state = new SteerState { MeasuredWheelAngleDeg = 1.25, AppliedEffort = 0.5, Engaged = true };
        var metadata = new LegacySteerStateMetadata { HeadingDeg = 87.5, RollDeg = -1.2, IsSteerSwitchOn = true, IsWorkSwitchOn = false, IsRemoteSwitchOn = true };
        var frame = steerCodec.EncodeSteerState(state, metadata);

        await gateway.HandleDatagramAsync(frame);

        var recordedState = Assert.Single(steerStateObserver.States);
        Assert.NotNull(recordedState.Header);
        Assert.Equal("legacy/udp/steer_state", recordedState.Header.Source);
        Assert.Equal(1UL, recordedState.Header.Sequence);
        Assert.Equal(Timestamp.FromDateTimeOffset(timestamp), recordedState.Header.Timestamp);
        Assert.Equal(state.MeasuredWheelAngleDeg, recordedState.MeasuredWheelAngleDeg, 2);
        Assert.Equal(state.AppliedEffort, recordedState.AppliedEffort, 2);
        Assert.True(recordedState.Engaged);

        var recordedMetadata = Assert.Single(steerStateObserver.Metadata);
        Assert.Equal(metadata.HeadingDeg, recordedMetadata.HeadingDeg, 2);
        Assert.Equal(metadata.RollDeg, recordedMetadata.RollDeg, 2);
        Assert.True(recordedMetadata.IsSteerSwitchOn);
        Assert.Equal(metadata.IsRemoteSwitchOn, recordedMetadata.IsRemoteSwitchOn);
        Assert.Equal(metadata.IsWorkSwitchOn, recordedMetadata.IsWorkSwitchOn);
        Assert.Equal(frame.Span[12], recordedMetadata.RawPwm);

        Assert.Empty(poseObserver.Poses);
        Assert.Empty(discoveryObserver.Announcements);
        Assert.Empty(sectionObserver.Masks);
        Assert.Empty(steerCommandObserver.Commands);
    }

    private sealed class RecordingTransport : ILegacyUdpTransport
    {
        public List<ReadOnlyMemory<byte>> Frames { get; } = new();

        public ValueTask SendAsync(ReadOnlyMemory<byte> datagram, CancellationToken cancellationToken)
        {
            Frames.Add(datagram.ToArray());
            return ValueTask.CompletedTask;
        }
    }

    private sealed class RecordingObserver : ILegacyPoseObserver
    {
        public List<Pose> Poses { get; } = new();
        public List<LegacyPoseMetadata> Metadata { get; } = new();

        public ValueTask OnPoseAsync(Pose pose, LegacyPoseMetadata metadata, CancellationToken cancellationToken)
        {
            Poses.Add(pose);
            Metadata.Add(metadata);
            return ValueTask.CompletedTask;
        }
    }

    private sealed class RecordingDiscoveryObserver : ILegacyDiscoveryObserver
    {
        public List<LegacyDiscoveryAnnouncement> Announcements { get; } = new();

        public ValueTask OnDiscoveryAsync(LegacyDiscoveryAnnouncement announcement, CancellationToken cancellationToken)
        {
            Announcements.Add(announcement);
            return ValueTask.CompletedTask;
        }
    }

    private sealed class RecordingSteerCommandObserver : ILegacySteerCommandObserver
    {
        public List<SteerCmd> Commands { get; } = new();
        public List<LegacySteerCommandMetadata> Metadata { get; } = new();

        public ValueTask OnSteerCommandAsync(SteerCmd command, LegacySteerCommandMetadata metadata, CancellationToken cancellationToken)
        {
            Commands.Add(command);
            Metadata.Add(metadata);
            return ValueTask.CompletedTask;
        }
    }

    private sealed class RecordingSteerStateObserver : ILegacySteerStateObserver
    {
        public List<SteerState> States { get; } = new();
        public List<LegacySteerStateMetadata> Metadata { get; } = new();

        public ValueTask OnSteerStateAsync(SteerState state, LegacySteerStateMetadata metadata, CancellationToken cancellationToken)
        {
            States.Add(state);
            Metadata.Add(metadata);
            return ValueTask.CompletedTask;
        }
    }

    private sealed class RecordingSectionObserver : ILegacySectionObserver
    {
        public List<SectionMask> Masks { get; } = new();

        public ValueTask OnSectionMaskAsync(SectionMask mask, CancellationToken cancellationToken)
        {
            Masks.Add(mask);
            return ValueTask.CompletedTask;
        }
    }

    private sealed class RecordingFailsafeService : IActuatorFailsafeService
    {
        public SteerCmd? SteerResult { get; set; }

        public SectionMask? SectionResult { get; set; }

        public Func<SteerCmd, SteerCmd>? FilterSteerCommandCallback { get; set; }

        public Func<SectionMask, SectionMask>? FilterSectionMaskCallback { get; set; }

        public bool ReportHeartbeatCalled { get; private set; }

        public SteerCmd? LastSteerCommand { get; private set; }

        public SectionMask? LastSectionMask { get; private set; }

        public bool HasActiveHeartbeat { get; set; } = true;

        public DateTimeOffset? LastHeartbeatUtc { get; set; }

        public TimeSpan HeartbeatTimeout { get; set; } = TimeSpan.Zero;

        public void ReportHeartbeat()
        {
            ReportHeartbeatCalled = true;
        }

        public void ClearHeartbeat()
        {
        }

        public SteerCmd FilterSteerCommand(SteerCmd command)
        {
            ArgumentNullException.ThrowIfNull(command);
            LastSteerCommand = command;

            if (FilterSteerCommandCallback is not null)
            {
                return FilterSteerCommandCallback(command);
            }

            return SteerResult ?? command;
        }

        public SectionMask FilterSectionMask(SectionMask mask)
        {
            ArgumentNullException.ThrowIfNull(mask);
            LastSectionMask = mask;

            if (FilterSectionMaskCallback is not null)
            {
                return FilterSectionMaskCallback(mask);
            }

            return SectionResult ?? mask;
        }
    }

    private sealed class RecordingMeshPresencePublisher : ILegacyMeshPresencePublisher
    {
        public List<Pose> Poses { get; } = new();
        public List<LegacyPoseMetadata> Metadata { get; } = new();
        public List<LegacyDiscoveryAnnouncement> Discoveries { get; } = new();

        public ValueTask PublishPresenceAsync(Pose pose, LegacyPoseMetadata metadata, CancellationToken cancellationToken)
        {
            Poses.Add(pose);
            Metadata.Add(metadata);
            return ValueTask.CompletedTask;
        }

        public ValueTask OnDiscoveryAsync(LegacyDiscoveryAnnouncement announcement, CancellationToken cancellationToken)
        {
            Discoveries.Add(announcement);
            return ValueTask.CompletedTask;
        }
    }

    private sealed class RecordingLogger<T> : ILogger<T>
    {
        public List<LogEntry> Entries { get; } = new();

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Entries.Add(new LogEntry(logLevel, formatter(state, exception), exception));
        }

        public readonly record struct LogEntry(LogLevel Level, string Message, Exception? Exception);

        private sealed class NullScope : IDisposable
        {
            public static NullScope Instance { get; } = new();

            public void Dispose()
            {
            }
        }
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _value;

        public FixedTimeProvider(DateTimeOffset value)
        {
            _value = value;
        }

        public override DateTimeOffset GetUtcNow() => _value;
    }
}
