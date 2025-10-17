using System;
using System.Buffers.Binary;
using Aog.Agio.Legacy;
using Aog.Core.V1;
using Xunit;

namespace Aog.Agio.Tests;

public sealed class LegacySteerCodecTests
{
    [Fact]
    public void EncodeAndDecodeSteerCommand_RoundTrips()
    {
        var codec = new LegacySteerCodec();
        var command = new SteerCmd
        {
            TargetWheelAngleDeg = -7.25,
            Enable = true,
        };
        var sections = new SectionMask
        {
            SectionCount = 12,
            Mask = 0b1010_0101_0011,
        };
        var metadata = new LegacySteerCommandMetadata
        {
            SpeedKph = 14.6,
            GuidanceStatus = 2,
            TramControl = 0x5A,
        };

        var frame = codec.EncodeSteerCommand(command, sections, metadata);

        Assert.True(codec.TryDecodeSteerCommand(frame, out var decodedCommand, out var decodedMetadata, out var decodedSections));
        Assert.Equal(command.TargetWheelAngleDeg, decodedCommand.TargetWheelAngleDeg, 3);
        Assert.True(decodedCommand.Enable);
        Assert.Equal(metadata.GuidanceStatus, decodedMetadata.GuidanceStatus);
        Assert.Equal(metadata.SpeedKph, decodedMetadata.SpeedKph, 6);
        Assert.Equal(metadata.TramControl, decodedMetadata.TramControl);
        Assert.Equal((uint)(sections.Mask & 0xFFF), decodedSections.Mask);
        Assert.Equal(16u, decodedSections.SectionCount); // legacy PGN capacity
    }

    [Fact]
    public void TryDecodeSteerCommand_RemoteOrTramBitsAloneDoNotEngage()
    {
        var codec = new LegacySteerCodec();
        var command = new SteerCmd
        {
            TargetWheelAngleDeg = 5.0,
            Enable = false,
        };
        var metadata = new LegacySteerCommandMetadata
        {
            // remote + tram (or gps) bits set, BUT engaged bit (0x01) is NOT set
            GuidanceStatus = 0b0000_0110,
        };

        var frame = codec.EncodeSteerCommand(command, metadata: metadata);

        Assert.True(codec.TryDecodeSteerCommand(frame, out var decodedCommand, out _, out _));
        Assert.False(decodedCommand.Enable);
    }

    [Fact]
    public void TryDecodeSteerCommand_EngagedBitOverridesMetadata()
    {
        var codec = new LegacySteerCodec();
        var command = new SteerCmd
        {
            TargetWheelAngleDeg = -3.25,
            Enable = true,
        };
        var metadata = new LegacySteerCommandMetadata
        {
            // keep other bits; encoder should set engaged (0x01) because Enable=true
            GuidanceStatus = 0b0000_0110,
        };

        var frame = codec.EncodeSteerCommand(command, metadata: metadata);

        Assert.True(codec.TryDecodeSteerCommand(frame, out var decodedCommand, out _, out _));
        Assert.True(decodedCommand.Enable);
    }

    [Fact]
    public void EncodeSteerCommand_DefaultsStatusFromEnable()
    {
        var codec = new LegacySteerCodec();
        var command = new SteerCmd { TargetWheelAngleDeg = 3.5, Enable = true };

        var frame = codec.EncodeSteerCommand(command);

        Assert.Equal(1, frame[7]);
    }

    [Fact]
    public void EncodeSteerCommand_DisabledCommandClearsEngagedBitAndZeroesAngle()
    {
        var codec = new LegacySteerCodec();
        var command = new SteerCmd { TargetWheelAngleDeg = 12.34, Enable = false };
        var metadata = new LegacySteerCommandMetadata
        {
            GuidanceStatus = 0b0000_0011, // engaged + remote
            SpeedKph = 4.2,
        };

        var frame = codec.EncodeSteerCommand(command, metadata: metadata);

        // Expect engaged bit (0x01) cleared, other bits preserved (0b10)
        Assert.Equal(0b0000_0010, frame[7]);

        // Angle should be zero when disabled
        var steerHundredths = BinaryPrimitives.ReadInt16LittleEndian(frame.AsSpan(8, 2));
        Assert.Equal(0, steerHundredths);
    }

    [Theory]
    [InlineData(90.0, 3276)]
    [InlineData(-90.0, -3276)]
    public void EncodeSteerCommand_ClampsSteerAngleToHardwareRange(double angleDeg, short expectedHundredths)
    {
        var codec = new LegacySteerCodec();
        var command = new SteerCmd { TargetWheelAngleDeg = angleDeg, Enable = true };

        var frame = codec.EncodeSteerCommand(command);
        var steerHundredths = BinaryPrimitives.ReadInt16LittleEndian(frame.AsSpan(8, 2));

        Assert.Equal(expectedHundredths, steerHundredths);
    }

    [Fact]
    public void EncodeSteerCommand_TruncatesMaskAboveSectionCount()
    {
        var codec = new LegacySteerCodec();
        var command = new SteerCmd { Enable = true };
        var sections = new SectionMask
        {
            SectionCount = 12,
            Mask = 0b1111_1010_0000_1111,
        };

        var frame = codec.EncodeSteerCommand(command, sections);

        var encodedMask = BinaryPrimitives.ReadUInt16LittleEndian(frame.AsSpan(11, 2));
        Assert.Equal(0b0000_1010_0000_1111u, encodedMask);
    }

    [Fact]
    public void EncodeSteerCommand_PreservesMetadataStatusBitsWhenEnabled()
    {
        var codec = new LegacySteerCodec();
        var command = new SteerCmd { TargetWheelAngleDeg = -1.5, Enable = true };
        var metadata = new LegacySteerCommandMetadata
        {
            GuidanceStatus = 0b0010_0100,
            SpeedKph = 7.8,
        };

        var frame = codec.EncodeSteerCommand(command, metadata: metadata);

        Assert.Equal(0b0010_0101, frame[7]);
    }

    [Fact]
    public void EncodeSteerCommand_AllowsFullSixteenBitMask()
    {
        var codec = new LegacySteerCodec();
        var sections = new SectionMask
        {
            SectionCount = 16,
            Mask = 0xFFFF,
        };

        var frame = codec.EncodeSteerCommand(new SteerCmd { Enable = true }, sections);

        Assert.Equal(0xFF, frame[11]);
        Assert.Equal(0xFF, frame[12]);
    }

    [Fact]
    public void EncodeSteerCommand_TruncatesMaskAboveSixteenBits()
    {
        var codec = new LegacySteerCodec();
        var sections = new SectionMask
        {
            SectionCount = 16,
            Mask = 0x1FFFF,
        };

        var frame = codec.EncodeSteerCommand(new SteerCmd { Enable = true }, sections);

        Assert.Equal(0xFF, frame[11]);
        Assert.Equal(0xFF, frame[12]);
    }

    [Fact]
    public void EncodeSteerCommand_ThrowsWhenSectionCountExceedsSixteen()
    {
        var codec = new LegacySteerCodec();
        var sections = new SectionMask
        {
            SectionCount = 17,
            Mask = 0x1FFFF,
        };

        Assert.Throws<ArgumentOutOfRangeException>(() => codec.EncodeSteerCommand(new SteerCmd { Enable = true }, sections));
    }

    [Fact]
    public void EncodeAndDecodeSteerState_RoundTrips()
    {
        var codec = new LegacySteerCodec();
        var state = new SteerState
        {
            MeasuredWheelAngleDeg = 1.75,
            AppliedEffort = 0.45,
            Engaged = true,
        };
        var metadata = new LegacySteerStateMetadata
        {
            HeadingDeg = 123.45,
            RollDeg = -2.5,
            IsWorkSwitchOn = true,
            IsSteerSwitchOn = true,
            IsRemoteSwitchOn = false,
        };

        var frame = codec.EncodeSteerState(state, metadata);

        Assert.True(codec.TryDecodeSteerState(frame, out var decodedState, out var decodedMetadata));
        Assert.Equal(state.MeasuredWheelAngleDeg, decodedState.MeasuredWheelAngleDeg, 2);
        Assert.Equal(state.AppliedEffort, decodedState.AppliedEffort, 2);
        Assert.True(decodedState.Engaged);
        Assert.Equal(metadata.HeadingDeg, decodedMetadata.HeadingDeg, 2);
        Assert.Equal(metadata.RollDeg, decodedMetadata.RollDeg, 2);
        Assert.True(decodedMetadata.IsWorkSwitchOn);
        Assert.True(decodedMetadata.IsSteerSwitchOn);
        Assert.False(decodedMetadata.IsRemoteSwitchOn);
        Assert.Equal(frame[12], decodedMetadata.RawPwm);
    }
}
