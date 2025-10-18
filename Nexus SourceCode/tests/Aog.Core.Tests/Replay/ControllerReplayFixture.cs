using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Aog.Core.Eventing;
using Aog.Core.Logging;
using Aog.Core.Replay;
using Aog.Core.V1;
using Google.Protobuf.WellKnownTypes;

namespace Aog.Core.Tests.Replay;

internal static class ControllerReplayFixture
{
    private const string DirectoryPrefix = "nexus-controller-replay";

    public static async Task<ControllerReplayScenario> CreateAsync(
        string directory,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(directory);
        Directory.CreateDirectory(directory);

        var options = new TelemetryParquetLogger.TelemetryParquetLoggerOptions
        {
            OutputDirectory = directory
        };

        var bus = new InMemoryEventBus();
        await using var logger = await TelemetryParquetLogger.CreateAsync(bus, options, cancellationToken);

        var startTimestamp = DateTime.SpecifyKind(new DateTime(2024, 2, 15, 14, 0, 0), DateTimeKind.Utc);
        ulong sequence = 1;

        var pose = new Pose
        {
            Header = CreateHeader(sequence++, "earth", "sim", startTimestamp),
            LatitudeDeg = 52.215,
            LongitudeDeg = -1.512,
            AltitudeM = 110.2,
            HeadingRad = 1.42,
            RollRad = -0.015,
            PitchRad = 0.027,
            SpeedMps = 5.2,
            YawRateRadps = 0.085
        };
        await bus.PublishAsync(pose).ConfigureAwait(false);

        var imu = new Imu
        {
            Header = CreateHeader(sequence++, "imu", "sim", startTimestamp + TimeSpan.FromSeconds(1)),
            AccelXMps2 = 0.18,
            AccelYMps2 = -0.11,
            AccelZMps2 = 9.79,
            GyroXRadps = 0.012,
            GyroYRadps = 0.018,
            GyroZRadps = 0.031,
            MagXUt = 9.8,
            MagYUt = 11.1,
            MagZUt = 9.6,
            TemperatureC = 34.7
        };
        await bus.PublishAsync(imu).ConfigureAwait(false);

        var canFrame = new CanFrame
        {
            Header = CreateHeader(sequence++, "vehicle", "can", startTimestamp + TimeSpan.FromSeconds(2)),
            ArbitrationId = 0x18FF51,
            Payload = new byte[] { 0x10, 0x20, 0x30, 0x40 },
            IsExtendedId = true,
            IsRemoteRequest = false
        };
        await bus.PublishAsync(canFrame).ConfigureAwait(false);

        var sectionMask = new SectionMask
        {
            Header = CreateHeader(sequence++, "sections", "controller", startTimestamp + TimeSpan.FromSeconds(3)),
            SectionCount = 8,
            Mask = 0b0010_1011
        };
        await bus.PublishAsync(sectionMask).ConfigureAwait(false);

        var guidancePayload = "{\"wheelAngleDeg\":2.5,\"lookAheadSeconds\":0.9,\"source\":\"fixture\"}";
        var guidanceCommand = new PluginTelemetryEvent
        {
            Header = CreateHeader(sequence++, "controller", "sim", startTimestamp + TimeSpan.FromSeconds(4)),
            PluginId = "controllers.guidance",
            Topic = "command",
            Payload = Encoding.UTF8.GetBytes(guidancePayload)
        };
        await bus.PublishAsync(guidanceCommand).ConfigureAwait(false);

        var sectionsPayload = "{\"mask\":43,\"sectionCount\":8,\"operator\":\"Fixture\",\"workState\":\"Active\"}";
        var sectionsCommand = new PluginTelemetryEvent
        {
            Header = CreateHeader(sequence++, "controller", "sim", startTimestamp + TimeSpan.FromSeconds(5)),
            PluginId = "controllers.sections",
            Topic = "state",
            Payload = Encoding.UTF8.GetBytes(sectionsPayload)
        };
        await bus.PublishAsync(sectionsCommand).ConfigureAwait(false);

        var commands = new List<ControllerReplayCommand>
        {
            new(
                guidanceCommand.PluginId,
                guidanceCommand.Topic,
                guidancePayload,
                guidanceCommand.Header?.Sequence ?? 0,
                guidanceCommand.Header?.Timestamp?.ToDateTime() ?? startTimestamp + TimeSpan.FromSeconds(4)),
            new(
                sectionsCommand.PluginId,
                sectionsCommand.Topic,
                sectionsPayload,
                sectionsCommand.Header?.Sequence ?? 0,
                sectionsCommand.Header?.Timestamp?.ToDateTime() ?? startTimestamp + TimeSpan.FromSeconds(5))
        };

        return new ControllerReplayScenario(
            new TelemetryReplayOptions { InputDirectory = directory },
            startTimestamp,
            TimeSpan.FromSeconds(5),
            pose,
            imu,
            canFrame,
            sectionMask,
            commands);
    }

    public static TemporaryDirectory CreateTemporaryDirectory()
    {
        return new TemporaryDirectory(DirectoryPrefix);
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
}

internal sealed record ControllerReplayScenario(
    TelemetryReplayOptions ReplayOptions,
    DateTime StartTimestamp,
    TimeSpan Duration,
    Pose PoseSample,
    Imu ImuSample,
    CanFrame CanFrameSample,
    SectionMask SectionSample,
    IReadOnlyList<ControllerReplayCommand> ControllerCommands);

internal sealed record ControllerReplayCommand(
    string PluginId,
    string Topic,
    string PayloadJson,
    ulong Sequence,
    DateTime TimestampUtc)
{
    public string PayloadDisplay => PayloadJson;
    public string TimestampDisplay => TimestampUtc.ToString("u", System.Globalization.CultureInfo.InvariantCulture);
}
