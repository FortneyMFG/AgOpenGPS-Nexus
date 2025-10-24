using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using Aog.Core.Eventing;
using Aog.Core.Logging;
using Aog.Core.Replay;
using Aog.Core.V1;
using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Aog.Core.Tests.Replay;

public sealed class TelemetryReplayControllerTests
{
    [Fact]
    public async Task PlayAsync_ReplaysTelemetryAndUpdatesState()
    {
        using var tempDirectory = ControllerReplayFixture.CreateTemporaryDirectory();
        var scenario = await ControllerReplayFixture.CreateAsync(tempDirectory.DirectoryPath);

        var playbackBus = new InMemoryEventBus();
        var poses = new List<Pose>();
        var imus = new List<Imu>();
        var cans = new List<CanFrame>();
        var sections = new List<SectionMask>();
        var plugins = new List<PluginTelemetryEvent>();

        playbackBus.Subscribe<Pose>((message, _) =>
        {
            poses.Add(message);
            return ValueTask.CompletedTask;
        });

        playbackBus.Subscribe<Imu>((message, _) =>
        {
            imus.Add(message);
            return ValueTask.CompletedTask;
        });

        playbackBus.Subscribe<CanFrame>((message, _) =>
        {
            cans.Add(message);
            return ValueTask.CompletedTask;
        });

        playbackBus.Subscribe<SectionMask>((message, _) =>
        {
            sections.Add(message);
            return ValueTask.CompletedTask;
        });

        playbackBus.Subscribe<PluginTelemetryEvent>((message, _) =>
        {
            plugins.Add(message);
            return ValueTask.CompletedTask;
        });

        var timeProvider = new FakeTimeProvider();
        timeProvider.SetUtcNow(new DateTimeOffset(scenario.StartTimestamp, TimeSpan.Zero));

        await using var controller = await TelemetryReplayController.CreateAsync(
            playbackBus,
            scenario.ReplayOptions,
            timeProvider);

        controller.State.Duration.Should().Be(scenario.Duration);
        controller.State.Position.Should().Be(TimeSpan.Zero);

        await controller.PlayAsync();

        await AdvanceUntilAsync(timeProvider, () => poses.Count == 1, TimeSpan.FromMilliseconds(500));
        await AdvanceUntilAsync(timeProvider, () => imus.Count == 1, TimeSpan.FromMilliseconds(250));

        await controller.SetPlaybackRateAsync(2.0);

        await AdvanceUntilAsync(timeProvider, () => cans.Count == 1, TimeSpan.FromMilliseconds(250));
        await AdvanceUntilAsync(timeProvider, () => sections.Count == 1, TimeSpan.FromMilliseconds(250));
        await AdvanceUntilAsync(
            timeProvider,
            () => plugins.Count == scenario.ControllerCommands.Count,
            TimeSpan.FromMilliseconds(250));

        controller.State.IsPlaying.Should().BeFalse();
        controller.State.Position.Should().Be(controller.State.Duration);

        poses.Should().ContainSingle();
        imus.Should().ContainSingle();
        cans.Should().ContainSingle();
        sections.Should().ContainSingle();
        plugins.Should().HaveCount(scenario.ControllerCommands.Count);

        var pose = poses[0];
        pose.Header.Sequence.Should().Be(scenario.PoseSample.Header.Sequence);
        pose.Header.Frame.Should().Be(scenario.PoseSample.Header.Frame);
        pose.Header.Source.Should().Be(scenario.PoseSample.Header.Source);
        pose.Header.Timestamp.Should().NotBeNull();
        pose.LatitudeDeg.Should().BeApproximately(scenario.PoseSample.LatitudeDeg, 1e-9);
        pose.LongitudeDeg.Should().BeApproximately(scenario.PoseSample.LongitudeDeg, 1e-9);
        pose.AltitudeM.Should().BeApproximately(scenario.PoseSample.AltitudeM, 1e-9);

        var imu = imus[0];
        imu.Header.Sequence.Should().Be(scenario.ImuSample.Header.Sequence);
        imu.AccelXMps2.Should().BeApproximately(scenario.ImuSample.AccelXMps2, 1e-9);
        imu.GyroZRadps.Should().BeApproximately(scenario.ImuSample.GyroZRadps, 1e-9);

        var can = cans[0];
        can.Header.Sequence.Should().Be(scenario.CanFrameSample.Header.Sequence);
        can.ArbitrationId.Should().Be(scenario.CanFrameSample.ArbitrationId);
        can.Payload.ToArray().Should().Equal(scenario.CanFrameSample.Payload.ToArray());

        var section = sections[0];
        section.Header.Sequence.Should().Be(scenario.SectionSample.Header.Sequence);
        section.Mask.Should().Be(scenario.SectionSample.Mask);

        foreach (var command in scenario.ControllerCommands)
        {
            var plugin = plugins.Single(p => string.Equals(p.PluginId, command.PluginId, StringComparison.Ordinal));
            plugin.Header?.Sequence.Should().Be(command.Sequence);
            plugin.Topic.Should().Be(command.Topic);
            Encoding.UTF8.GetString(plugin.Payload.Span).Should().Be(command.PayloadJson);
        }
    }

    [Fact]
    public async Task SeekAsync_JumpsToRequestedPosition()
    {
        using var tempDirectory = ControllerReplayFixture.CreateTemporaryDirectory();
        var scenario = await ControllerReplayFixture.CreateAsync(tempDirectory.DirectoryPath);

        var playbackBus = new InMemoryEventBus();
        var poses = new List<Pose>();
        var sections = new List<SectionMask>();
        var plugins = new List<PluginTelemetryEvent>();

        playbackBus.Subscribe<Pose>((message, _) =>
        {
            poses.Add(message);
            return ValueTask.CompletedTask;
        });

        playbackBus.Subscribe<SectionMask>((message, _) =>
        {
            sections.Add(message);
            return ValueTask.CompletedTask;
        });

        playbackBus.Subscribe<PluginTelemetryEvent>((message, _) =>
        {
            plugins.Add(message);
            return ValueTask.CompletedTask;
        });

        var timeProvider = new FakeTimeProvider();
        timeProvider.SetUtcNow(new DateTimeOffset(scenario.StartTimestamp, TimeSpan.Zero));

        await using var controller = await TelemetryReplayController.CreateAsync(
            playbackBus,
            scenario.ReplayOptions,
            timeProvider);

        await controller.PlayAsync();
        await AdvanceUntilAsync(timeProvider, () => poses.Count == 1, TimeSpan.FromMilliseconds(500));

        await controller.SeekAsync(TimeSpan.FromSeconds(4));
        controller.State.Position.Should().Be(TimeSpan.FromSeconds(4));
        controller.State.IsPlaying.Should().BeTrue();

        await AdvanceUntilAsync(timeProvider, () => sections.Count == 1, TimeSpan.FromMilliseconds(250));
        await AdvanceUntilAsync(
            timeProvider,
            () => plugins.Count == scenario.ControllerCommands.Count,
            TimeSpan.FromMilliseconds(250));

        poses.Should().ContainSingle();
        sections.Should().ContainSingle();
        plugins.Should().HaveCount(scenario.ControllerCommands.Count);
    }

    private static async Task AdvanceUntilAsync(FakeTimeProvider provider, Func<bool> predicate, TimeSpan step, int maxSteps = 40)
    {
        for (var i = 0; i < maxSteps && !predicate(); i++)
        {
            provider.Advance(step);
            await Task.Yield();
        }

        predicate().Should().BeTrue();
    }
}
