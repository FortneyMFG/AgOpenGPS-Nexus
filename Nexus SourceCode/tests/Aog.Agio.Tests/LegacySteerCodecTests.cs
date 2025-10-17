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
        Assert.Equal(16u, decodedSections.SectionCount);
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
    public void EncodeSteerCommand_DisabledCommandForcesStatusZeroAndAngleZero()
    {
        var codec = new LegacySteerCodec();
        var command = new SteerCmd { TargetWheelAngleDeg = 12.34, Enable = false };
        var metadata = new LegacySteerCommandMetadata
        {
            GuidanceStatus = 3,
            SpeedKph = 4.2,
        };

        var frame = codec.EncodeSteerCommand(command, metadata: metadata);

        Assert.Equal(0, frame[7]);
        var steerHundredths = BinaryPrimitives.ReadInt16LittleEndian(frame.AsSpan(8, 2));
        Assert.Equal(0, steerHundredths);
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
