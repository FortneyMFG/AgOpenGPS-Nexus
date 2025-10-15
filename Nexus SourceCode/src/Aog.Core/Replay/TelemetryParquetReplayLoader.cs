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
            var sequences = ReadColumn<ulong>(schema, rowGroup, "sequence");
            var timestamps = ReadColumn<DateTime?>(schema, rowGroup, "timestamp_utc");
            var frames = ReadColumn<string?>(schema, rowGroup, "frame");
            var sources = ReadColumn<string?>(schema, rowGroup, "source");
            var jobIds = ReadOptionalStringColumn(schema, rowGroup, "job_id", sequences.Length);
            var seasonIds = ReadOptionalStringColumn(schema, rowGroup, "season_id", sequences.Length);
            var sessionIds = ReadOptionalStringColumn(schema, rowGroup, "session_id", sequences.Length);
            var latitude = ReadColumn<double>(schema, rowGroup, "latitude_deg");
            var longitude = ReadColumn<double>(schema, rowGroup, "longitude_deg");
            var altitude = ReadColumn<double>(schema, rowGroup, "altitude_m");
            var heading = ReadColumn<double>(schema, rowGroup, "heading_rad");
            var roll = ReadColumn<double>(schema, rowGroup, "roll_rad");
            var pitch = ReadColumn<double>(schema, rowGroup, "pitch_rad");
            var speed = ReadColumn<double>(schema, rowGroup, "speed_mps");
            var yawRate = ReadColumn<double>(schema, rowGroup, "yaw_rate_radps");

            for (var i = 0; i < sequences.Length; i++)
            {
                var header = CreateHeader(sequences[i], timestamps[i], frames[i], sources[i], jobIds[i], seasonIds[i], sessionIds[i]);
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
            var sequences = ReadColumn<ulong>(schema, rowGroup, "sequence");
            var timestamps = ReadColumn<DateTime?>(schema, rowGroup, "timestamp_utc");
            var frames = ReadColumn<string?>(schema, rowGroup, "frame");
            var sources = ReadColumn<string?>(schema, rowGroup, "source");
            var jobIds = ReadOptionalStringColumn(schema, rowGroup, "job_id", sequences.Length);
            var seasonIds = ReadOptionalStringColumn(schema, rowGroup, "season_id", sequences.Length);
            var sessionIds = ReadOptionalStringColumn(schema, rowGroup, "session_id", sequences.Length);
            var accelX = ReadColumn<double>(schema, rowGroup, "accel_x_mps2");
            var accelY = ReadColumn<double>(schema, rowGroup, "accel_y_mps2");
            var accelZ = ReadColumn<double>(schema, rowGroup, "accel_z_mps2");
            var gyroX = ReadColumn<double>(schema, rowGroup, "gyro_x_radps");
            var gyroY = ReadColumn<double>(schema, rowGroup, "gyro_y_radps");
            var gyroZ = ReadColumn<double>(schema, rowGroup, "gyro_z_radps");
            var magX = ReadColumn<double>(schema, rowGroup, "mag_x_ut");
            var magY = ReadColumn<double>(schema, rowGroup, "mag_y_ut");
            var magZ = ReadColumn<double>(schema, rowGroup, "mag_z_ut");
            var temperature = ReadColumn<double>(schema, rowGroup, "temperature_c");

            for (var i = 0; i < sequences.Length; i++)
            {
                var header = CreateHeader(sequences[i], timestamps[i], frames[i], sources[i], jobIds[i], seasonIds[i], sessionIds[i]);
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
            var sequences = ReadColumn<ulong>(schema, rowGroup, "sequence");
            var timestamps = ReadColumn<DateTime?>(schema, rowGroup, "timestamp_utc");
            var frames = ReadColumn<string?>(schema, rowGroup, "frame");
            var sources = ReadColumn<string?>(schema, rowGroup, "source");
            var jobIds = ReadOptionalStringColumn(schema, rowGroup, "job_id", sequences.Length);
            var seasonIds = ReadOptionalStringColumn(schema, rowGroup, "season_id", sequences.Length);
            var sessionIds = ReadOptionalStringColumn(schema, rowGroup, "session_id", sequences.Length);
            var arbitrationId = ReadColumn<uint>(schema, rowGroup, "arbitration_id");
            var payloads = ReadColumn<byte[]?>(schema, rowGroup, "payload");
            var isExtended = ReadColumn<bool>(schema, rowGroup, "is_extended_id");
            var isRemote = ReadColumn<bool>(schema, rowGroup, "is_remote_request");

            for (var i = 0; i < sequences.Length; i++)
            {
                var header = CreateHeader(sequences[i], timestamps[i], frames[i], sources[i], jobIds[i], seasonIds[i], sessionIds[i]);
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
            var sequences = ReadColumn<ulong>(schema, rowGroup, "sequence");
            var timestamps = ReadColumn<DateTime?>(schema, rowGroup, "timestamp_utc");
            var frames = ReadColumn<string?>(schema, rowGroup, "frame");
            var sources = ReadColumn<string?>(schema, rowGroup, "source");
            var jobIds = ReadOptionalStringColumn(schema, rowGroup, "job_id", sequences.Length);
            var seasonIds = ReadOptionalStringColumn(schema, rowGroup, "season_id", sequences.Length);
            var sessionIds = ReadOptionalStringColumn(schema, rowGroup, "session_id", sequences.Length);
            var sectionCount = ReadColumn<uint>(schema, rowGroup, "section_count");
            var mask = ReadColumn<uint>(schema, rowGroup, "mask");

            for (var i = 0; i < sequences.Length; i++)
            {
                var header = CreateHeader(sequences[i], timestamps[i], frames[i], sources[i], jobIds[i], seasonIds[i], sessionIds[i]);
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
            var sequences = ReadColumn<ulong>(schema, rowGroup, "sequence");
            var timestamps = ReadColumn<DateTime?>(schema, rowGroup, "timestamp_utc");
            var sources = ReadColumn<string?>(schema, rowGroup, "source");
            var jobIds = ReadOptionalStringColumn(schema, rowGroup, "job_id", sequences.Length);
            var seasonIds = ReadOptionalStringColumn(schema, rowGroup, "season_id", sequences.Length);
            var sessionIds = ReadOptionalStringColumn(schema, rowGroup, "session_id", sequences.Length);
            var pluginId = ReadColumn<string>(schema, rowGroup, "plugin_id");
            var topic = ReadColumn<string>(schema, rowGroup, "topic");
            var payloads = ReadColumn<byte[]?>(schema, rowGroup, "payload");

            for (var i = 0; i < sequences.Length; i++)
            {
                var header = CreateHeader(sequences[i], timestamps[i], frame: null, sources[i], jobIds[i], seasonIds[i], sessionIds[i]);

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

    private static Header CreateHeader(
        ulong sequence,
        DateTime? timestampUtc,
        string? frame,
        string? source,
        string? jobId,
        string? seasonId,
        string? sessionId)
    {
        var header = new Header
        {
            Sequence = sequence,
            Frame = string.IsNullOrWhiteSpace(frame) ? string.Empty : frame,
            Source = string.IsNullOrWhiteSpace(source) ? string.Empty : source,
            JobId = string.IsNullOrWhiteSpace(jobId) ? string.Empty : jobId,
            SeasonId = string.IsNullOrWhiteSpace(seasonId) ? string.Empty : seasonId,
            SessionId = string.IsNullOrWhiteSpace(sessionId) ? string.Empty : sessionId
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

    private static T[] ReadColumn<T>(Schema schema, ParquetRowGroupReader reader, string columnName)
    {
        var field = GetRequiredDataField(schema, columnName);
        return (T[])reader.ReadColumn(field).Data;
    }

    private static string?[] ReadOptionalStringColumn(
        Schema schema,
        ParquetRowGroupReader reader,
        string columnName,
        int rowCount)
    {
        if (!TryGetDataField(schema, columnName, out var field))
        {
            return new string?[rowCount];
        }

        return (string?[])reader.ReadColumn(field).Data;
    }

    private static DataField GetRequiredDataField(Schema schema, string columnName)
    {
        if (TryGetDataField(schema, columnName, out var field) && field is not null)
        {
            return field;
        }

        throw new InvalidOperationException($"Telemetry parquet file is missing required column '{columnName}'.");
    }

    private static bool TryGetDataField(Schema schema, string columnName, out DataField? field)
    {
        foreach (var candidate in schema.Fields)
        {
            if (candidate is DataField dataField &&
                string.Equals(dataField.Name, columnName, StringComparison.OrdinalIgnoreCase))
            {
                field = dataField;
                return true;
            }
        }

        field = null;
        return false;
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
