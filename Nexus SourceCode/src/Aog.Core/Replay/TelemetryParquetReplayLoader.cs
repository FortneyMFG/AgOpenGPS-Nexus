using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aog.Core.Eventing;
using Aog.Core.Logging;
using Aog.Core.V1;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Parquet;
using Parquet.Data;
using Parquet.Schema;

namespace Aog.Core.Replay;

internal static class TelemetryParquetReplayLoader
{
    public static Task<List<ReplayFrame>> LoadAsync(
        TelemetryReplayOptions options,
        CancellationToken cancellationToken)
    {
        options.Validate();
        var entries = new List<ReplayEntry>();

        cancellationToken.ThrowIfCancellationRequested();
        entries.AddRange(ReadPose(options.ResolvePath(options.PoseFileName)));

        cancellationToken.ThrowIfCancellationRequested();
        entries.AddRange(ReadImu(options.ResolvePath(options.ImuFileName)));

        cancellationToken.ThrowIfCancellationRequested();
        entries.AddRange(ReadCan(options.ResolvePath(options.CanFileName)));

        cancellationToken.ThrowIfCancellationRequested();
        entries.AddRange(ReadIo(options.ResolvePath(options.IoFileName)));

        cancellationToken.ThrowIfCancellationRequested();
        entries.AddRange(ReadPlugin(options.ResolvePath(options.PluginFileName)));

        var frames = BuildFrames(entries);
        return Task.FromResult(frames);
    }

    private static IEnumerable<ReplayEntry> ReadPose(string path)
    {
        if (!File.Exists(path))
        {
            yield break;
        }

        using var stream = File.OpenRead(path);
        using var reader = ParquetReader.Create(stream);
        var schema = reader.Schema;

        for (var rowGroupIndex = 0; rowGroupIndex < reader.RowGroupCount; rowGroupIndex++)
        {
            using var rowGroup = reader.OpenRowGroupReader(rowGroupIndex);
            var sequences = ReadColumn<ulong>(schema, rowGroup, 0);
            var timestamps = ReadColumn<DateTime?>(schema, rowGroup, 1);
            var frames = ReadColumn<string?>(schema, rowGroup, 2);
            var sources = ReadColumn<string?>(schema, rowGroup, 3);
            var latitude = ReadColumn<double>(schema, rowGroup, 4);
            var longitude = ReadColumn<double>(schema, rowGroup, 5);
            var altitude = ReadColumn<double>(schema, rowGroup, 6);
            var heading = ReadColumn<double>(schema, rowGroup, 7);
            var roll = ReadColumn<double>(schema, rowGroup, 8);
            var pitch = ReadColumn<double>(schema, rowGroup, 9);
            var speed = ReadColumn<double>(schema, rowGroup, 10);
            var yawRate = ReadColumn<double>(schema, rowGroup, 11);

            for (var i = 0; i < sequences.Length; i++)
            {
                var header = CreateHeader(sequences[i], timestamps[i], frames[i], sources[i]);
                var message = new Pose
                {
                    Header = header,
                    LatitudeDeg = latitude[i],
                    LongitudeDeg = longitude[i],
                    AltitudeM = altitude[i],
                    HeadingRad = heading[i],
                    RollRad = roll[i],
                    PitchRad = pitch[i],
                    SpeedMps = speed[i],
                    YawRateRadps = yawRate[i]
                };

                yield return CreateEntry("pose", sequences[i], timestamps[i], message);
            }
        }
    }

    private static IEnumerable<ReplayEntry> ReadImu(string path)
    {
        if (!File.Exists(path))
        {
            yield break;
        }

        using var stream = File.OpenRead(path);
        using var reader = ParquetReader.Create(stream);
        var schema = reader.Schema;

        for (var rowGroupIndex = 0; rowGroupIndex < reader.RowGroupCount; rowGroupIndex++)
        {
            using var rowGroup = reader.OpenRowGroupReader(rowGroupIndex);
            var sequences = ReadColumn<ulong>(schema, rowGroup, 0);
            var timestamps = ReadColumn<DateTime?>(schema, rowGroup, 1);
            var frames = ReadColumn<string?>(schema, rowGroup, 2);
            var sources = ReadColumn<string?>(schema, rowGroup, 3);
            var accelX = ReadColumn<double>(schema, rowGroup, 4);
            var accelY = ReadColumn<double>(schema, rowGroup, 5);
            var accelZ = ReadColumn<double>(schema, rowGroup, 6);
            var gyroX = ReadColumn<double>(schema, rowGroup, 7);
            var gyroY = ReadColumn<double>(schema, rowGroup, 8);
            var gyroZ = ReadColumn<double>(schema, rowGroup, 9);
            var magX = ReadColumn<double>(schema, rowGroup, 10);
            var magY = ReadColumn<double>(schema, rowGroup, 11);
            var magZ = ReadColumn<double>(schema, rowGroup, 12);
            var temperature = ReadColumn<double>(schema, rowGroup, 13);

            for (var i = 0; i < sequences.Length; i++)
            {
                var header = CreateHeader(sequences[i], timestamps[i], frames[i], sources[i]);
                var message = new Imu
                {
                    Header = header,
                    AccelXMps2 = accelX[i],
                    AccelYMps2 = accelY[i],
                    AccelZMps2 = accelZ[i],
                    GyroXRadps = gyroX[i],
                    GyroYRadps = gyroY[i],
                    GyroZRadps = gyroZ[i],
                    MagXUt = magX[i],
                    MagYUt = magY[i],
                    MagZUt = magZ[i],
                    TemperatureC = temperature[i]
                };

                yield return CreateEntry("imu", sequences[i], timestamps[i], message);
            }
        }
    }

    private static IEnumerable<ReplayEntry> ReadCan(string path)
    {
        if (!File.Exists(path))
        {
            yield break;
        }

        using var stream = File.OpenRead(path);
        using var reader = ParquetReader.Create(stream);
        var schema = reader.Schema;

        for (var rowGroupIndex = 0; rowGroupIndex < reader.RowGroupCount; rowGroupIndex++)
        {
            using var rowGroup = reader.OpenRowGroupReader(rowGroupIndex);
            var sequences = ReadColumn<ulong>(schema, rowGroup, 0);
            var timestamps = ReadColumn<DateTime?>(schema, rowGroup, 1);
            var frames = ReadColumn<string?>(schema, rowGroup, 2);
            var sources = ReadColumn<string?>(schema, rowGroup, 3);
            var arbitrationId = ReadColumn<uint>(schema, rowGroup, 4);
            var payloads = ReadColumn<byte[]?>(schema, rowGroup, 5);
            var isExtended = ReadColumn<bool>(schema, rowGroup, 6);
            var isRemote = ReadColumn<bool>(schema, rowGroup, 7);

            for (var i = 0; i < sequences.Length; i++)
            {
                var header = CreateHeader(sequences[i], timestamps[i], frames[i], sources[i]);
                var payload = payloads[i];
                var message = new CanFrame
                {
                    Header = header,
                    ArbitrationId = arbitrationId[i],
                    Payload = payload is null || payload.Length == 0 ? ByteString.Empty : ByteString.CopyFrom(payload),
                    IsExtendedId = isExtended[i],
                    IsRemoteRequest = isRemote[i]
                };

                yield return CreateEntry("can", sequences[i], timestamps[i], message);
            }
        }
    }

    private static IEnumerable<ReplayEntry> ReadIo(string path)
    {
        if (!File.Exists(path))
        {
            yield break;
        }

        using var stream = File.OpenRead(path);
        using var reader = ParquetReader.Create(stream);
        var schema = reader.Schema;

        for (var rowGroupIndex = 0; rowGroupIndex < reader.RowGroupCount; rowGroupIndex++)
        {
            using var rowGroup = reader.OpenRowGroupReader(rowGroupIndex);
            var sequences = ReadColumn<ulong>(schema, rowGroup, 0);
            var timestamps = ReadColumn<DateTime?>(schema, rowGroup, 1);
            var frames = ReadColumn<string?>(schema, rowGroup, 2);
            var sources = ReadColumn<string?>(schema, rowGroup, 3);
            var sectionCount = ReadColumn<uint>(schema, rowGroup, 4);
            var mask = ReadColumn<uint>(schema, rowGroup, 5);

            for (var i = 0; i < sequences.Length; i++)
            {
                var header = CreateHeader(sequences[i], timestamps[i], frames[i], sources[i]);
                var message = new SectionMask
                {
                    Header = header,
                    SectionCount = sectionCount[i],
                    Mask = mask[i]
                };

                yield return CreateEntry("io", sequences[i], timestamps[i], message);
            }
        }
    }

    private static IEnumerable<ReplayEntry> ReadPlugin(string path)
    {
        if (!File.Exists(path))
        {
            yield break;
        }

        using var stream = File.OpenRead(path);
        using var reader = ParquetReader.Create(stream);
        var schema = reader.Schema;

        for (var rowGroupIndex = 0; rowGroupIndex < reader.RowGroupCount; rowGroupIndex++)
        {
            using var rowGroup = reader.OpenRowGroupReader(rowGroupIndex);
            var sequences = ReadColumn<ulong>(schema, rowGroup, 0);
            var timestamps = ReadColumn<DateTime?>(schema, rowGroup, 1);
            var sources = ReadColumn<string?>(schema, rowGroup, 2);
            var pluginId = ReadColumn<string>(schema, rowGroup, 3);
            var topic = ReadColumn<string>(schema, rowGroup, 4);
            var payloads = ReadColumn<byte[]?>(schema, rowGroup, 5);

            for (var i = 0; i < sequences.Length; i++)
            {
                var header = CreateHeader(sequences[i], timestamps[i], frame: null, sources[i]);
                header.Frame = string.Empty;

                var payload = payloads[i];
                var message = new PluginTelemetryEvent
                {
                    Header = header,
                    PluginId = pluginId[i] ?? string.Empty,
                    Topic = topic[i] ?? string.Empty,
                    Payload = payload is null || payload.Length == 0
                        ? ReadOnlyMemory<byte>.Empty
                        : new ReadOnlyMemory<byte>(payload)
                };

                yield return CreateEntry("plugin", sequences[i], timestamps[i], message);
            }
        }
    }

    private static List<ReplayFrame> BuildFrames(List<ReplayEntry> entries)
    {
        if (entries.Count == 0)
        {
            return new List<ReplayFrame>();
        }

        entries.Sort(static (left, right) =>
        {
            var timestampComparison = Nullable.Compare(left.TimestampUtc, right.TimestampUtc);
            if (timestampComparison != 0)
            {
                return timestampComparison;
            }

            var topicComparison = string.Compare(left.Topic, right.Topic, StringComparison.Ordinal);
            if (topicComparison != 0)
            {
                return topicComparison;
            }

            return left.Sequence.CompareTo(right.Sequence);
        });

        var baseline = entries
            .Where(entry => entry.TimestampUtc.HasValue)
            .Select(entry => EnsureUtc(entry.TimestampUtc!.Value))
            .DefaultIfEmpty(DateTime.SpecifyKind(DateTime.UnixEpoch, DateTimeKind.Utc))
            .Min();

        var frames = new List<ReplayFrame>(entries.Count);
        var lastOffset = TimeSpan.Zero;
        foreach (var entry in entries)
        {
            var offset = entry.TimestampUtc.HasValue
                ? EnsureUtc(entry.TimestampUtc.Value) - baseline
                : lastOffset;

            if (offset < TimeSpan.Zero)
            {
                offset = TimeSpan.Zero;
            }

            frames.Add(new ReplayFrame(offset, entry.TimestampUtc, entry.PublishAsync));
            lastOffset = offset;
        }

        return frames;
    }

    private static Header CreateHeader(ulong sequence, DateTime? timestampUtc, string? frame, string? source)
    {
        var header = new Header
        {
            Sequence = sequence,
            Frame = string.IsNullOrWhiteSpace(frame) ? string.Empty : frame,
            Source = string.IsNullOrWhiteSpace(source) ? string.Empty : source
        };

        if (timestampUtc.HasValue)
        {
            var utc = EnsureUtc(timestampUtc.Value);
            header.Timestamp = Timestamp.FromDateTime(utc);
        }

        return header;
    }

    private static ReplayEntry CreateEntry<TMessage>(string topic, ulong sequence, DateTime? timestampUtc, TMessage message)
    {
        return new ReplayEntry(
            topic,
            sequence,
            timestampUtc,
            (bus, token) => bus.PublishAsync(message, token));
    }

    private static T[] ReadColumn<T>(Schema schema, ParquetRowGroupReader reader, int index)
    {
        var field = (DataField)schema.Fields[index];
        return (T[])reader.ReadColumn(field).Data;
    }

    private static DateTime EnsureUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Unspecified => DateTime.SpecifyKind(value, DateTimeKind.Utc),
            _ => value.ToUniversalTime(),
        };
    }

    private sealed record ReplayEntry(
        string Topic,
        ulong Sequence,
        DateTime? TimestampUtc,
        Func<IEventBus, CancellationToken, ValueTask> PublishAsync);
}
