using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Aog.Core.Eventing;
using Aog.Core.Mesh;
using Aog.Core.V1;
using Google.Protobuf.WellKnownTypes;
using Parquet;
using Parquet.Data;
using Parquet.Schema;
using ParquetSchema = Parquet.Schema.ParquetSchema;

namespace Aog.Core.Logging;

/// <summary>
/// Subscribes to the in-process event bus and persists telemetry topics to columnar Parquet files.
/// </summary>
public sealed class TelemetryParquetLogger : IAsyncDisposable
{
    private readonly ParquetTopicWriter<Pose> _poseWriter;
    private readonly ParquetTopicWriter<Imu> _imuWriter;
    private readonly ParquetTopicWriter<CanFrame> _canWriter;
    private readonly ParquetTopicWriter<SectionMask> _ioWriter;
    private readonly ParquetTopicWriter<PluginTelemetryEvent> _pluginWriter;
    private readonly ParquetTopicWriter<MeshTelemetryEvent> _meshWriter;
    private readonly IReadOnlyList<IDisposable> _subscriptions;

    private TelemetryParquetLogger(
        ParquetTopicWriter<Pose> poseWriter,
        ParquetTopicWriter<Imu> imuWriter,
        ParquetTopicWriter<CanFrame> canWriter,
        ParquetTopicWriter<SectionMask> ioWriter,
        ParquetTopicWriter<PluginTelemetryEvent> pluginWriter,
        ParquetTopicWriter<MeshTelemetryEvent> meshWriter,
        IReadOnlyList<IDisposable> subscriptions)
    {
        _poseWriter = poseWriter;
        _imuWriter = imuWriter;
        _canWriter = canWriter;
        _ioWriter = ioWriter;
        _pluginWriter = pluginWriter;
        _meshWriter = meshWriter;
        _subscriptions = subscriptions;
    }

    /// <summary>
    /// Creates and starts a Parquet logger bound to the supplied <see cref="IEventBus"/>.
    /// </summary>
    /// <param name="eventBus">Event bus used to receive telemetry topics.</param>
    /// <param name="options">Configuration describing where log files should be written.</param>
    /// <param name="cancellationToken">Token used to cancel initialisation.</param>
    /// <returns>An active telemetry logger.</returns>
    public static async Task<TelemetryParquetLogger> CreateAsync(
        IEventBus eventBus,
        TelemetryParquetLoggerOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(eventBus);
        ArgumentNullException.ThrowIfNull(options);

        options.Validate();

        var poseWriter = await ParquetTopicWriter<Pose>.CreateAsync(
            options.ResolvePath(options.PoseFileName),
            TelemetryParquetSchemas.Pose.Schema,
            TelemetryParquetRowBuilder.CreatePoseColumns(options),
            cancellationToken).ConfigureAwait(false);

        var imuWriter = await ParquetTopicWriter<Imu>.CreateAsync(
            options.ResolvePath(options.ImuFileName),
            TelemetryParquetSchemas.Imu.Schema,
            TelemetryParquetRowBuilder.CreateImuColumns(options),
            cancellationToken).ConfigureAwait(false);

        var canWriter = await ParquetTopicWriter<CanFrame>.CreateAsync(
            options.ResolvePath(options.CanFileName),
            TelemetryParquetSchemas.Can.Schema,
            TelemetryParquetRowBuilder.CreateCanColumns(options),
            cancellationToken).ConfigureAwait(false);

        var ioWriter = await ParquetTopicWriter<SectionMask>.CreateAsync(
            options.ResolvePath(options.IoFileName),
            TelemetryParquetSchemas.Io.Schema,
            TelemetryParquetRowBuilder.CreateIoColumns(options),
            cancellationToken).ConfigureAwait(false);

        var pluginWriter = await ParquetTopicWriter<PluginTelemetryEvent>.CreateAsync(
            options.ResolvePath(options.PluginFileName),
            TelemetryParquetSchemas.Plugin.Schema,
            TelemetryParquetRowBuilder.CreatePluginColumns(options),
            cancellationToken).ConfigureAwait(false);

        var meshWriter = await ParquetTopicWriter<MeshTelemetryEvent>.CreateAsync(
            options.ResolvePath(options.MeshFileName),
            TelemetryParquetSchemas.Mesh.Schema,
            TelemetryParquetRowBuilder.CreateMeshColumns(options),
            cancellationToken).ConfigureAwait(false);

        var subscriptions = new List<IDisposable>
        {
            eventBus.Subscribe<Pose>((message, token) => poseWriter.WriteAsync(message, token)),
            eventBus.Subscribe<Imu>((message, token) => imuWriter.WriteAsync(message, token)),
            eventBus.Subscribe<CanFrame>((message, token) => canWriter.WriteAsync(message, token)),
            eventBus.Subscribe<SectionMask>((message, token) => ioWriter.WriteAsync(message, token)),
            eventBus.Subscribe<PluginTelemetryEvent>((message, token) => pluginWriter.WriteAsync(message, token)),
            eventBus.Subscribe<MeshTelemetryEvent>((message, token) => meshWriter.WriteAsync(message, token))
        };

        return new TelemetryParquetLogger(poseWriter, imuWriter, canWriter, ioWriter, pluginWriter, meshWriter, subscriptions);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        foreach (var subscription in _subscriptions)
        {
            subscription.Dispose();
        }

        await _poseWriter.DisposeAsync().ConfigureAwait(false);
        await _imuWriter.DisposeAsync().ConfigureAwait(false);
        await _canWriter.DisposeAsync().ConfigureAwait(false);
        await _ioWriter.DisposeAsync().ConfigureAwait(false);
        await _pluginWriter.DisposeAsync().ConfigureAwait(false);
        await _meshWriter.DisposeAsync().ConfigureAwait(false);

        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Options used to configure the Parquet telemetry logger.
    /// </summary>
    public sealed class TelemetryParquetLoggerOptions
    {
        /// <summary>
        /// Gets or sets the directory where Parquet files will be created.
        /// </summary>
        public string OutputDirectory { get; init; } = ".";

        /// <summary>
        /// File name for pose measurements.
        /// </summary>
        public string PoseFileName { get; init; } = "pose.parquet";

        /// <summary>
        /// File name for IMU measurements.
        /// </summary>
        public string ImuFileName { get; init; } = "imu.parquet";

        /// <summary>
        /// File name for CAN frames.
        /// </summary>
        public string CanFileName { get; init; } = "can.parquet";

        /// <summary>
        /// File name for I/O events such as section masks.
        /// </summary>
        public string IoFileName { get; init; } = "io.parquet";

        /// <summary>
        /// File name for plugin-published payloads.
        /// </summary>
        public string PluginFileName { get; init; } = "plugin.parquet";

        /// <summary>
        /// File name for mesh publications captured by the retention worker.
        /// </summary>
        public string MeshFileName { get; init; } = "mesh.parquet";

        /// <summary>
        /// Optional job identifier used to populate telemetry provenance when
        /// message headers do not specify one. Aligns with ADR-041.
        /// </summary>
        public string? JobId { get; init; }

        /// <summary>
        /// Optional session identifier stamped onto each telemetry row when
        /// available.
        /// </summary>
        public string? SessionId { get; init; }

        /// <summary>
        /// Optional season identifier associated with the active job context.
        /// </summary>
        public string? SeasonId { get; init; }

        internal void Validate()
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(OutputDirectory);
            ArgumentException.ThrowIfNullOrWhiteSpace(PoseFileName);
            ArgumentException.ThrowIfNullOrWhiteSpace(ImuFileName);
            ArgumentException.ThrowIfNullOrWhiteSpace(CanFileName);
            ArgumentException.ThrowIfNullOrWhiteSpace(IoFileName);
            ArgumentException.ThrowIfNullOrWhiteSpace(PluginFileName);
            ArgumentException.ThrowIfNullOrWhiteSpace(MeshFileName);
        }

        internal string ResolvePath(string fileName)
        {
            Directory.CreateDirectory(OutputDirectory);
            return Path.Combine(OutputDirectory, fileName);
        }
    }

    private sealed class ParquetTopicWriter<T> : IAsyncDisposable
    {
        private readonly Func<T, DataColumn[]> _columnFactory;
        private readonly Stream _stream;
        private readonly ParquetWriter _writer;
        private readonly SemaphoreSlim _mutex = new(1, 1);

        private ParquetTopicWriter(Func<T, DataColumn[]> columnFactory, Stream stream, ParquetWriter writer)
        {
            _columnFactory = columnFactory;
            _stream = stream;
            _writer = writer;
        }

        public static async Task<ParquetTopicWriter<T>> CreateAsync(
            string path,
            ParquetSchema schema,
            Func<T, DataColumn[]> columnFactory,
            CancellationToken cancellationToken)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var stream = new FileStream(
                path,
                FileMode.Create,
                FileAccess.Write,
                FileShare.Read,
                bufferSize: 4096,
                FileOptions.Asynchronous | FileOptions.SequentialScan);

            var writer = await ParquetWriter.CreateAsync(schema, stream).ConfigureAwait(false);
            return new ParquetTopicWriter<T>(columnFactory, stream, writer);
        }

        public async ValueTask WriteAsync(T message, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await _mutex.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                using var rowGroup = _writer.CreateRowGroup();
                foreach (var column in _columnFactory(message))
                {
                    rowGroup.WriteColumn(column);
                }
            }
            finally
            {
                _mutex.Release();
            }
        }

        public async ValueTask DisposeAsync()
        {
            await _mutex.WaitAsync().ConfigureAwait(false);
            try
            {
                _writer.Dispose();
                if (_stream is FileStream fileStream)
                {
                    await fileStream.FlushAsync().ConfigureAwait(false);
                    fileStream.Dispose();
                }
                else
                {
                    await _stream.FlushAsync().ConfigureAwait(false);
                    await _stream.DisposeAsync().ConfigureAwait(false);
                }
            }
            finally
            {
                _mutex.Release();
                _mutex.Dispose();
            }
        }
    }

    private static class TelemetryParquetSchemas
    {
        internal static class Pose
        {
            public static readonly DataField<ulong> Sequence = new("sequence");
            public static readonly DateTimeDataField Timestamp = new("timestamp_utc", DateTimeFormat.DateAndTime, hasNulls: true);
            public static readonly DataField<string?> Frame = new("frame");
            public static readonly DataField<string?> Source = new("source");
            public static readonly DataField<string?> JobId = new("job_id");
            public static readonly DataField<string?> SeasonId = new("season_id");
            public static readonly DataField<string?> SessionId = new("session_id");
            public static readonly DataField<double> LatitudeDeg = new("latitude_deg");
            public static readonly DataField<double> LongitudeDeg = new("longitude_deg");
            public static readonly DataField<double> AltitudeM = new("altitude_m");
            public static readonly DataField<double> HeadingRad = new("heading_rad");
            public static readonly DataField<double> RollRad = new("roll_rad");
            public static readonly DataField<double> PitchRad = new("pitch_rad");
            public static readonly DataField<double> SpeedMps = new("speed_mps");
            public static readonly DataField<double> YawRateRadps = new("yaw_rate_radps");
            public static readonly ParquetSchema Schema = new(
                Sequence,
                Timestamp,
                Frame,
                Source,
                JobId,
                SeasonId,
                SessionId,
                LatitudeDeg,
                LongitudeDeg,
                AltitudeM,
                HeadingRad,
                RollRad,
                PitchRad,
                SpeedMps,
                YawRateRadps);
        }

        internal static class Imu
        {
            public static readonly DataField<ulong> Sequence = new("sequence");
            public static readonly DateTimeDataField Timestamp = new("timestamp_utc", DateTimeFormat.DateAndTime, hasNulls: true);
            public static readonly DataField<string?> Frame = new("frame");
            public static readonly DataField<string?> Source = new("source");
            public static readonly DataField<string?> JobId = new("job_id");
            public static readonly DataField<string?> SeasonId = new("season_id");
            public static readonly DataField<string?> SessionId = new("session_id");
            public static readonly DataField<double> AccelXMps2 = new("accel_x_mps2");
            public static readonly DataField<double> AccelYMps2 = new("accel_y_mps2");
            public static readonly DataField<double> AccelZMps2 = new("accel_z_mps2");
            public static readonly DataField<double> GyroXRadps = new("gyro_x_radps");
            public static readonly DataField<double> GyroYRadps = new("gyro_y_radps");
            public static readonly DataField<double> GyroZRadps = new("gyro_z_radps");
            public static readonly DataField<double> MagXUt = new("mag_x_ut");
            public static readonly DataField<double> MagYUt = new("mag_y_ut");
            public static readonly DataField<double> MagZUt = new("mag_z_ut");
            public static readonly DataField<double> TemperatureC = new("temperature_c");
            public static readonly ParquetSchema Schema = new(
                Sequence,
                Timestamp,
                Frame,
                Source,
                JobId,
                SeasonId,
                SessionId,
                AccelXMps2,
                AccelYMps2,
                AccelZMps2,
                GyroXRadps,
                GyroYRadps,
                GyroZRadps,
                MagXUt,
                MagYUt,
                MagZUt,
                TemperatureC);
        }

        internal static class Can
        {
            public static readonly DataField<ulong> Sequence = new("sequence");
            public static readonly DateTimeDataField Timestamp = new("timestamp_utc", DateTimeFormat.DateAndTime, hasNulls: true);
            public static readonly DataField<string?> Frame = new("frame");
            public static readonly DataField<string?> Source = new("source");
            public static readonly DataField<string?> JobId = new("job_id");
            public static readonly DataField<string?> SeasonId = new("season_id");
            public static readonly DataField<string?> SessionId = new("session_id");
            public static readonly DataField<uint> ArbitrationId = new("arbitration_id");
            public static readonly DataField<byte[]?> Payload = new("payload");
            public static readonly DataField<bool> IsExtendedId = new("is_extended_id");
            public static readonly DataField<bool> IsRemoteRequest = new("is_remote_request");
            public static readonly ParquetSchema Schema = new(
                Sequence,
                Timestamp,
                Frame,
                Source,
                JobId,
                SeasonId,
                SessionId,
                ArbitrationId,
                Payload,
                IsExtendedId,
                IsRemoteRequest);
        }

        internal static class Io
        {
            public static readonly DataField<ulong> Sequence = new("sequence");
            public static readonly DateTimeDataField Timestamp = new("timestamp_utc", DateTimeFormat.DateAndTime, hasNulls: true);
            public static readonly DataField<string?> Frame = new("frame");
            public static readonly DataField<string?> Source = new("source");
            public static readonly DataField<string?> JobId = new("job_id");
            public static readonly DataField<string?> SeasonId = new("season_id");
            public static readonly DataField<string?> SessionId = new("session_id");
            public static readonly DataField<uint> SectionCount = new("section_count");
            public static readonly DataField<uint> Mask = new("mask");
            public static readonly ParquetSchema Schema = new(
                Sequence,
                Timestamp,
                Frame,
                Source,
                JobId,
                SeasonId,
                SessionId,
                SectionCount,
                Mask);
        }

        internal static class Plugin
        {
            public static readonly DataField<ulong> Sequence = new("sequence");
            public static readonly DateTimeDataField Timestamp = new("timestamp_utc", DateTimeFormat.DateAndTime, hasNulls: true);
            public static readonly DataField<string?> Source = new("source");
            public static readonly DataField<string?> JobId = new("job_id");
            public static readonly DataField<string?> SeasonId = new("season_id");
            public static readonly DataField<string?> SessionId = new("session_id");
            public static readonly DataField<string> PluginId = new("plugin_id");
            public static readonly DataField<string> Topic = new("topic");
            public static readonly DataField<byte[]?> Payload = new("payload");
            public static readonly ParquetSchema Schema = new(
                Sequence,
                Timestamp,
                Source,
                JobId,
                SeasonId,
                SessionId,
                PluginId,
                Topic,
                Payload);
        }

        internal static class Mesh
        {
            public static readonly DataField<long> Sequence = new("sequence");
            public static readonly DataField<string> PublisherDeviceId = new("publisher_device_id");
            public static readonly DataField<string> Topic = new("topic");
            public static readonly DataField<string> SeasonId = new("season_id");
            public static readonly DataField<string> JobId = new("job_id");
            public static readonly DataField<string> LayerNamespace = new("layer_namespace");
            public static readonly DataField<string> Tier = new("tier");
            public static readonly DateTimeDataField PublishedAt = new("published_at_utc", DateTimeFormat.DateAndTime);
            public static readonly DataField<byte[]?> Payload = new("payload");
            public static readonly DataField<string?> MetadataJson = new("metadata_json");
            public static readonly DataField<string?> PresenceJson = new("presence_json");
            public static readonly ParquetSchema Schema = new(
                Sequence,
                PublisherDeviceId,
                Topic,
                SeasonId,
                JobId,
                LayerNamespace,
                Tier,
                PublishedAt,
                Payload,
                MetadataJson,
                PresenceJson);
        }
    }

    private static class TelemetryParquetRowBuilder
    {
        public static Func<Pose, DataColumn[]> CreatePoseColumns(TelemetryParquetLoggerOptions options)
            => message =>
            {
                var header = message.Header;
                return new[]
                {
                    HeaderColumns.Sequence(TelemetryParquetSchemas.Pose.Sequence, header),
                    HeaderColumns.Timestamp(TelemetryParquetSchemas.Pose.Timestamp, header),
                    HeaderColumns.Frame(TelemetryParquetSchemas.Pose.Frame, header),
                    HeaderColumns.Source(TelemetryParquetSchemas.Pose.Source, header),
                    HeaderColumns.JobId(TelemetryParquetSchemas.Pose.JobId, header, options),
                    HeaderColumns.SeasonId(TelemetryParquetSchemas.Pose.SeasonId, header, options),
                    HeaderColumns.SessionId(TelemetryParquetSchemas.Pose.SessionId, header, options),
                    new DataColumn(TelemetryParquetSchemas.Pose.LatitudeDeg, new[] { message.LatitudeDeg }),
                    new DataColumn(TelemetryParquetSchemas.Pose.LongitudeDeg, new[] { message.LongitudeDeg }),
                    new DataColumn(TelemetryParquetSchemas.Pose.AltitudeM, new[] { message.AltitudeM }),
                    new DataColumn(TelemetryParquetSchemas.Pose.HeadingRad, new[] { message.HeadingRad }),
                    new DataColumn(TelemetryParquetSchemas.Pose.RollRad, new[] { message.RollRad }),
                    new DataColumn(TelemetryParquetSchemas.Pose.PitchRad, new[] { message.PitchRad }),
                    new DataColumn(TelemetryParquetSchemas.Pose.SpeedMps, new[] { message.SpeedMps }),
                    new DataColumn(TelemetryParquetSchemas.Pose.YawRateRadps, new[] { message.YawRateRadps })
                };
            };

        public static Func<Imu, DataColumn[]> CreateImuColumns(TelemetryParquetLoggerOptions options)
            => message =>
            {
                var header = message.Header;
                return new[]
                {
                    HeaderColumns.Sequence(TelemetryParquetSchemas.Imu.Sequence, header),
                    HeaderColumns.Timestamp(TelemetryParquetSchemas.Imu.Timestamp, header),
                    HeaderColumns.Frame(TelemetryParquetSchemas.Imu.Frame, header),
                    HeaderColumns.Source(TelemetryParquetSchemas.Imu.Source, header),
                    HeaderColumns.JobId(TelemetryParquetSchemas.Imu.JobId, header, options),
                    HeaderColumns.SeasonId(TelemetryParquetSchemas.Imu.SeasonId, header, options),
                    HeaderColumns.SessionId(TelemetryParquetSchemas.Imu.SessionId, header, options),
                    new DataColumn(TelemetryParquetSchemas.Imu.AccelXMps2, new[] { message.AccelXMps2 }),
                    new DataColumn(TelemetryParquetSchemas.Imu.AccelYMps2, new[] { message.AccelYMps2 }),
                    new DataColumn(TelemetryParquetSchemas.Imu.AccelZMps2, new[] { message.AccelZMps2 }),
                    new DataColumn(TelemetryParquetSchemas.Imu.GyroXRadps, new[] { message.GyroXRadps }),
                    new DataColumn(TelemetryParquetSchemas.Imu.GyroYRadps, new[] { message.GyroYRadps }),
                    new DataColumn(TelemetryParquetSchemas.Imu.GyroZRadps, new[] { message.GyroZRadps }),
                    new DataColumn(TelemetryParquetSchemas.Imu.MagXUt, new[] { message.MagXUt }),
                    new DataColumn(TelemetryParquetSchemas.Imu.MagYUt, new[] { message.MagYUt }),
                    new DataColumn(TelemetryParquetSchemas.Imu.MagZUt, new[] { message.MagZUt }),
                    new DataColumn(TelemetryParquetSchemas.Imu.TemperatureC, new[] { message.TemperatureC })
                };
            };

        public static Func<CanFrame, DataColumn[]> CreateCanColumns(TelemetryParquetLoggerOptions options)
            => message =>
            {
                var header = message.Header;
                return new[]
                {
                    HeaderColumns.Sequence(TelemetryParquetSchemas.Can.Sequence, header),
                    HeaderColumns.Timestamp(TelemetryParquetSchemas.Can.Timestamp, header),
                    HeaderColumns.Frame(TelemetryParquetSchemas.Can.Frame, header),
                    HeaderColumns.Source(TelemetryParquetSchemas.Can.Source, header),
                    HeaderColumns.JobId(TelemetryParquetSchemas.Can.JobId, header, options),
                    HeaderColumns.SeasonId(TelemetryParquetSchemas.Can.SeasonId, header, options),
                    HeaderColumns.SessionId(TelemetryParquetSchemas.Can.SessionId, header, options),
                    new DataColumn(TelemetryParquetSchemas.Can.ArbitrationId, new[] { message.ArbitrationId }),
                    new DataColumn(TelemetryParquetSchemas.Can.Payload, new byte[]?[] { message.Payload.Length == 0 ? Array.Empty<byte>() : message.Payload.ToByteArray() }),
                    new DataColumn(TelemetryParquetSchemas.Can.IsExtendedId, new[] { message.IsExtendedId }),
                    new DataColumn(TelemetryParquetSchemas.Can.IsRemoteRequest, new[] { message.IsRemoteRequest })
                };
            };

        public static Func<SectionMask, DataColumn[]> CreateIoColumns(TelemetryParquetLoggerOptions options)
            => message =>
            {
                var header = message.Header;
                return new[]
                {
                    HeaderColumns.Sequence(TelemetryParquetSchemas.Io.Sequence, header),
                    HeaderColumns.Timestamp(TelemetryParquetSchemas.Io.Timestamp, header),
                    HeaderColumns.Frame(TelemetryParquetSchemas.Io.Frame, header),
                    HeaderColumns.Source(TelemetryParquetSchemas.Io.Source, header),
                    HeaderColumns.JobId(TelemetryParquetSchemas.Io.JobId, header, options),
                    HeaderColumns.SeasonId(TelemetryParquetSchemas.Io.SeasonId, header, options),
                    HeaderColumns.SessionId(TelemetryParquetSchemas.Io.SessionId, header, options),
                    new DataColumn(TelemetryParquetSchemas.Io.SectionCount, new[] { message.SectionCount }),
                    new DataColumn(TelemetryParquetSchemas.Io.Mask, new[] { message.Mask })
                };
            };

        public static Func<PluginTelemetryEvent, DataColumn[]> CreatePluginColumns(TelemetryParquetLoggerOptions options)
            => message =>
            {
                var header = message.Header;
                return new[]
                {
                    HeaderColumns.Sequence(TelemetryParquetSchemas.Plugin.Sequence, header),
                    HeaderColumns.Timestamp(TelemetryParquetSchemas.Plugin.Timestamp, header),
                    HeaderColumns.Source(TelemetryParquetSchemas.Plugin.Source, header),
                    HeaderColumns.JobId(TelemetryParquetSchemas.Plugin.JobId, header, options),
                    HeaderColumns.SeasonId(TelemetryParquetSchemas.Plugin.SeasonId, header, options),
                    HeaderColumns.SessionId(TelemetryParquetSchemas.Plugin.SessionId, header, options),
                    new DataColumn(TelemetryParquetSchemas.Plugin.PluginId, new[] { HeaderColumns.NormalizeString(message.PluginId) ?? string.Empty }),
                    new DataColumn(TelemetryParquetSchemas.Plugin.Topic, new[] { HeaderColumns.NormalizeString(message.Topic) ?? string.Empty }),
                    new DataColumn(TelemetryParquetSchemas.Plugin.Payload, new byte[]?[] { message.Payload.Length == 0 ? Array.Empty<byte>() : message.Payload.ToArray() })
                };
            };

        public static Func<MeshTelemetryEvent, DataColumn[]> CreateMeshColumns(TelemetryParquetLoggerOptions options)
            => message => new[]
            {
                new DataColumn(TelemetryParquetSchemas.Mesh.Sequence, new[] { message.Sequence }),
                new DataColumn(TelemetryParquetSchemas.Mesh.PublisherDeviceId, new[] { message.PublisherDeviceId }),
                new DataColumn(TelemetryParquetSchemas.Mesh.Topic, new[] { message.Topic }),
                new DataColumn(TelemetryParquetSchemas.Mesh.SeasonId, new[] { message.SeasonId }),
                new DataColumn(TelemetryParquetSchemas.Mesh.JobId, new[] { message.JobId }),
                new DataColumn(TelemetryParquetSchemas.Mesh.LayerNamespace, new[] { message.LayerNamespace }),
                new DataColumn(TelemetryParquetSchemas.Mesh.Tier, new[] { message.Tier.ToString() }),
                new DataColumn(TelemetryParquetSchemas.Mesh.PublishedAt, new DateTime[] { message.PublishedAt.UtcDateTime }),
                new DataColumn(TelemetryParquetSchemas.Mesh.Payload, new byte[]?[] { message.Payload.Length == 0 ? Array.Empty<byte>() : message.Payload }),
                new DataColumn(TelemetryParquetSchemas.Mesh.MetadataJson, new[] { message.MetadataJson }),
                new DataColumn(TelemetryParquetSchemas.Mesh.PresenceJson, new[] { message.PresenceJson })
            };

        private static class HeaderColumns
        {
            public static DataColumn Sequence(DataField<ulong> field, Header? header)
                => new(field, new[] { header?.Sequence ?? 0UL });

            public static DataColumn Timestamp(DateTimeDataField field, Header? header)
                => new(field, new DateTime?[] { NormalizeTimestamp(header?.Timestamp) });

            public static DataColumn Frame(DataField<string?> field, Header? header)
                => new(field, new[] { NormalizeString(header?.Frame) });

            public static DataColumn Source(DataField<string?> field, Header? header)
                => new(field, new[] { NormalizeString(header?.Source) });

            public static DataColumn JobId(DataField<string?> field, Header? header, TelemetryParquetLoggerOptions options)
                => new(field, new[] { ResolveContext(header?.JobId, options.JobId) });

            public static DataColumn SeasonId(DataField<string?> field, Header? header, TelemetryParquetLoggerOptions options)
                => new(field, new[] { ResolveContext(header?.SeasonId, options.SeasonId) });

            public static DataColumn SessionId(DataField<string?> field, Header? header, TelemetryParquetLoggerOptions options)
                => new(field, new[] { ResolveContext(header?.SessionId, options.SessionId) });

            private static DateTime? NormalizeTimestamp(Timestamp? timestamp)
            {
                if (timestamp is null)
                {
                    return null;
                }

                var dateTime = timestamp.ToDateTime();
                return dateTime.Kind == DateTimeKind.Unspecified
                    ? DateTime.SpecifyKind(dateTime, DateTimeKind.Utc)
                    : dateTime.ToUniversalTime();
            }

            public static string? NormalizeString(string? value)
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    return null;
                }

                return value;
            }

            private static string? ResolveContext(string? headerValue, string? fallback)
            {
                var normalized = NormalizeString(headerValue);
                return normalized ?? NormalizeString(fallback);
            }
        }
    }
}
