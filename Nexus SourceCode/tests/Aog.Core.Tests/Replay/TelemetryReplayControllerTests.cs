using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Aog.Core.Eventing;
using Aog.Core.Logging;
using Aog.Core.Replay;
using Aog.Core.V1;
using FluentAssertions;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Aog.Core.Tests.Replay;

public sealed class TelemetryReplayControllerTests
{
    [Fact]
    public async Task PlayAsync_ReplaysTelemetryAndUpdatesState()
    {
        var tempDirectory = CreateTempDirectory();
        try
        {
            await WriteSampleTelemetryAsync(tempDirectory);

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

            var timeProvider = new ManualTimeProvider();
            timeProvider.SetUtcNow(DateTime.SpecifyKind(new DateTime(2024, 1, 1), DateTimeKind.Utc));

            var replayOptions = new TelemetryReplayOptions
            {
                InputDirectory = tempDirectory
            };

            await using var controller = await TelemetryReplayController.CreateAsync(
                playbackBus,
                replayOptions,
                timeProvider);

            controller.State.Duration.Should().Be(TimeSpan.FromSeconds(4));
            controller.State.Position.Should().Be(TimeSpan.Zero);

            await controller.PlayAsync();

            await AdvanceUntilAsync(timeProvider, () => poses.Count == 1, TimeSpan.FromMilliseconds(500));
            await AdvanceUntilAsync(timeProvider, () => imus.Count == 1, TimeSpan.FromMilliseconds(250));

            await controller.SetPlaybackRateAsync(2.0);

            await AdvanceUntilAsync(timeProvider, () => cans.Count == 1, TimeSpan.FromMilliseconds(250));
            await AdvanceUntilAsync(timeProvider, () => sections.Count == 1, TimeSpan.FromMilliseconds(250));
            await AdvanceUntilAsync(timeProvider, () => plugins.Count == 1, TimeSpan.FromMilliseconds(250));

            controller.State.IsPlaying.Should().BeFalse();
            controller.State.Position.Should().Be(controller.State.Duration);

            poses.Should().ContainSingle();
            imus.Should().ContainSingle();
            cans.Should().ContainSingle();
            sections.Should().ContainSingle();
            plugins.Should().ContainSingle();

            var pose = poses[0];
            pose.Header.Sequence.Should().Be(1);
            pose.Header.Frame.Should().Be("earth");
            pose.Header.Source.Should().Be("sim");
            pose.Header.Timestamp.Should().NotBeNull();
            pose.LatitudeDeg.Should().BeApproximately(52.1, 1e-9);
            pose.LongitudeDeg.Should().BeApproximately(-1.2, 1e-9);
            pose.AltitudeM.Should().BeApproximately(123.4, 1e-9);

            var imu = imus[0];
            imu.Header.Sequence.Should().Be(2);
            imu.AccelXMps2.Should().BeApproximately(0.1, 1e-9);
            imu.GyroZRadps.Should().BeApproximately(0.03, 1e-9);

            var can = cans[0];
            can.Header.Sequence.Should().Be(3);
            can.ArbitrationId.Should().Be(0x18FF50);
            can.Payload.Should().Be(ByteString.CopyFrom(new byte[] { 0xAA, 0xBB, 0xCC }));

            var section = sections[0];
            section.Header.Sequence.Should().Be(4);
            section.Mask.Should().Be(0b001011);

            var plugin = plugins[0];
            plugin.Header?.Sequence.Should().Be(5);
            plugin.PluginId.Should().Be("autosteer");
            plugin.Topic.Should().Be("state");
            plugin.Payload.ToArray().Should().Equal(0x01, 0x02, 0x03);
        }
        finally
        {
            TryDeleteDirectory(tempDirectory);
        }
    }

    [Fact]
    public async Task SeekAsync_JumpsToRequestedPosition()
    {
        var tempDirectory = CreateTempDirectory();
        try
        {
            await WriteSampleTelemetryAsync(tempDirectory);

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

            var timeProvider = new ManualTimeProvider();
            timeProvider.SetUtcNow(DateTime.SpecifyKind(new DateTime(2024, 1, 1), DateTimeKind.Utc));

            var replayOptions = new TelemetryReplayOptions
            {
                InputDirectory = tempDirectory
            };

            await using var controller = await TelemetryReplayController.CreateAsync(
                playbackBus,
                replayOptions,
                timeProvider);

            await controller.PlayAsync();
            await AdvanceUntilAsync(timeProvider, () => poses.Count == 1, TimeSpan.FromMilliseconds(500));

            await controller.SeekAsync(TimeSpan.FromSeconds(3));
            controller.State.Position.Should().Be(TimeSpan.FromSeconds(3));
            controller.State.IsPlaying.Should().BeTrue();

            await AdvanceUntilAsync(timeProvider, () => sections.Count == 1, TimeSpan.FromMilliseconds(250));
            await AdvanceUntilAsync(timeProvider, () => plugins.Count == 1, TimeSpan.FromMilliseconds(250));

            poses.Should().ContainSingle();
            sections.Should().ContainSingle();
            plugins.Should().ContainSingle();
        }
        finally
        {
            TryDeleteDirectory(tempDirectory);
        }
    }

    private static async Task WriteSampleTelemetryAsync(string directory)
    {
        var options = new TelemetryParquetLogger.TelemetryParquetLoggerOptions
        {
            OutputDirectory = directory
        };

        var bus = new InMemoryEventBus();
        await using var logger = await TelemetryParquetLogger.CreateAsync(bus, options);

        var baseTime = DateTime.SpecifyKind(new DateTime(2024, 1, 1, 12, 0, 0), DateTimeKind.Utc);

        await bus.PublishAsync(new Pose
        {
            Header = CreateHeader(sequence: 1, frame: "earth", source: "sim", baseTime),
            LatitudeDeg = 52.1,
            LongitudeDeg = -1.2,
            AltitudeM = 123.4,
            HeadingRad = 1.5,
            RollRad = -0.01,
            PitchRad = 0.02,
            SpeedMps = 4.5,
            YawRateRadps = 0.1
        });

        await bus.PublishAsync(new Imu
        {
            Header = CreateHeader(sequence: 2, frame: "imu", source: "sim", baseTime + TimeSpan.FromSeconds(1)),
            AccelXMps2 = 0.1,
            AccelYMps2 = -0.2,
            AccelZMps2 = 9.81,
            GyroXRadps = 0.01,
            GyroYRadps = 0.02,
            GyroZRadps = 0.03,
            MagXUt = 10.1,
            MagYUt = 11.2,
            MagZUt = 9.9,
            TemperatureC = 35.5
        });

        await bus.PublishAsync(new CanFrame
        {
            Header = CreateHeader(sequence: 3, frame: "vehicle", source: "can", baseTime + TimeSpan.FromSeconds(2)),
            ArbitrationId = 0x18FF50,
            Payload = ByteString.CopyFrom(new byte[] { 0xAA, 0xBB, 0xCC }),
            IsExtendedId = true,
            IsRemoteRequest = false
        });

        await bus.PublishAsync(new SectionMask
        {
            Header = CreateHeader(sequence: 4, frame: "sections", source: "sim", baseTime + TimeSpan.FromSeconds(3)),
            SectionCount = 6,
            Mask = 0b001011
        });

        await bus.PublishAsync(new PluginTelemetryEvent
        {
            Header = CreateHeader(sequence: 5, frame: "plugin-host", source: "plugin", baseTime + TimeSpan.FromSeconds(4)),
            PluginId = "autosteer",
            Topic = "state",
            Payload = new byte[] { 0x01, 0x02, 0x03 }
        });
    }

    private static Header CreateHeader(ulong sequence, string frame, string source, DateTime timestamp)
    {
        return new Header
        {
            Sequence = sequence,
            Frame = frame,
            Source = source,
            Timestamp = Timestamp.FromDateTime(timestamp)
        };
    }

    private static async Task AdvanceUntilAsync(ManualTimeProvider provider, Func<bool> predicate, TimeSpan step, int maxSteps = 40)
    {
        for (var i = 0; i < maxSteps && !predicate(); i++)
        {
            provider.Advance(step);
            await Task.Yield();
        }

        predicate().Should().BeTrue();
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "nexus-replay-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch
        {
            // Ignore cleanup failures so tests do not flake on locked files.
        }
    }
}
