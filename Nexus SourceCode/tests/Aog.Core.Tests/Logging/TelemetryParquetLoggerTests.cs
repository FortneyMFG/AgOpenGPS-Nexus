using System;
using System.IO;
using System.Threading.Tasks;
using Aog.Core.Eventing;
using Aog.Core.Logging;
using Aog.Core.V1;
using FluentAssertions;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Parquet;
using Parquet.Data;
using Xunit;
using TelemetryParquetLoggerOptions = Aog.Core.Logging.TelemetryParquetLogger.TelemetryParquetLoggerOptions;

namespace Aog.Core.Tests.Logging;

public class TelemetryParquetLoggerTests
{
    private const string JobId = "job:2024-demo";
    private const string SeasonId = "season:2024";
    private const string SessionId = "session:alpha";

    [Fact]
    public async Task Logger_Writes_All_Topics_To_Parquet()
    {
        var eventBus = new InMemoryEventBus();
        var outputDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var options = new TelemetryParquetLoggerOptions
        {
            OutputDirectory = outputDirectory
        };

        var timestamp = Timestamp.FromDateTime(DateTime.SpecifyKind(new DateTime(2024, 1, 1, 12, 0, 0), DateTimeKind.Utc));

        {
            await using var logger = await TelemetryParquetLogger.CreateAsync(eventBus, options);

            await eventBus.PublishAsync(new Pose
            {
                Header = new Header
                {
                    Sequence = 1,
                    Timestamp = timestamp,
                    Frame = "earth",
                    Source = "sim",
                    JobId = JobId,
                    SeasonId = SeasonId,
                    SessionId = SessionId
                },
                LatitudeDeg = 52.1,
                LongitudeDeg = -1.2,
                AltitudeM = 123.4,
                HeadingRad = 1.5,
                RollRad = -0.01,
                PitchRad = 0.02,
                SpeedMps = 4.5,
                YawRateRadps = 0.1
            });

            await eventBus.PublishAsync(new Imu
            {
                Header = new Header
                {
                    Sequence = 2,
                    Timestamp = timestamp,
                    Frame = "imu",
                    Source = "sim",
                    JobId = JobId,
                    SeasonId = SeasonId,
                    SessionId = SessionId
                },
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

            await eventBus.PublishAsync(new CanFrame
            {
                Header = new Header
                {
                    Sequence = 3,
                    Timestamp = timestamp,
                    Frame = "vehicle",
                    Source = "can",
                    JobId = JobId,
                    SeasonId = SeasonId,
                    SessionId = SessionId
                },
                ArbitrationId = 0x18FF50,
                Payload = ByteString.CopyFrom(new byte[] { 0xAA, 0xBB, 0xCC }),
                IsExtendedId = true,
                IsRemoteRequest = false
            });

            await eventBus.PublishAsync(new SectionMask
            {
                Header = new Header
                {
                    Sequence = 4,
                    Timestamp = timestamp,
                    Frame = "sections",
                    Source = "sim",
                    JobId = JobId,
                    SeasonId = SeasonId,
                    SessionId = SessionId
                },
                SectionCount = 6,
                Mask = 0b001011
            });

            await eventBus.PublishAsync(new PluginTelemetryEvent
            {
                Header = new Header
                {
                    Sequence = 5,
                    Timestamp = timestamp,
                    Source = "plugin-host",
                    JobId = JobId,
                    SeasonId = SeasonId,
                    SessionId = SessionId
                },
                PluginId = "autosteer",
                Topic = "state",
                Payload = new byte[] { 0x01, 0x02, 0x03 }
            });
        }

        AssertPose(options, timestamp);
        AssertImu(options, timestamp);
        AssertCan(options, timestamp);
        AssertIo(options, timestamp);
        AssertPlugin(options, timestamp);
    }

    [Fact]
    public async Task Logger_Populates_Context_From_Options_When_HeaderMissing()
    {
        var eventBus = new InMemoryEventBus();
        var outputDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var options = new TelemetryParquetLoggerOptions
        {
            OutputDirectory = outputDirectory,
            JobId = "job:fallback",
            SeasonId = "season:fallback",
            SessionId = "session:fallback"
        };

        var timestamp = Timestamp.FromDateTime(DateTime.SpecifyKind(new DateTime(2024, 1, 2, 8, 30, 0), DateTimeKind.Utc));

        { // ensure logger disposed before assertions
            await using var logger = await TelemetryParquetLogger.CreateAsync(eventBus, options);

            await eventBus.PublishAsync(new Pose
            {
                Header = new Header
                {
                    Sequence = 1,
                    Timestamp = timestamp,
                    Frame = "earth",
                    Source = "sim"
                },
                LatitudeDeg = 10.0,
                LongitudeDeg = 20.0,
                AltitudeM = 100.0,
                HeadingRad = 0.0,
                RollRad = 0.0,
                PitchRad = 0.0,
                SpeedMps = 0.0,
                YawRateRadps = 0.0
            });
        }

        using var stream = File.OpenRead(Path.Combine(options.OutputDirectory, options.PoseFileName));
        using var reader = ParquetReader.Create(stream);
        using var rowGroup = reader.OpenRowGroupReader(0);

        ReadColumn<string?>(reader.Schema, rowGroup, "job_id")[0].Should().Be("job:fallback");
        ReadColumn<string?>(reader.Schema, rowGroup, "season_id")[0].Should().Be("season:fallback");
        ReadColumn<string?>(reader.Schema, rowGroup, "session_id")[0].Should().Be("session:fallback");
    }

    private static void AssertPose(TelemetryParquetLoggerOptions options, Timestamp expectedTimestamp)
    {
        using var stream = File.OpenRead(Path.Combine(options.OutputDirectory, options.PoseFileName));
        using var reader = ParquetReader.Create(stream);
        reader.RowGroupCount.Should().Be(1);

        using var rowGroup = reader.OpenRowGroupReader(0);
        ReadColumn<ulong>(reader.Schema, rowGroup, "sequence").Should().Equal(1);
        ReadColumn<DateTime?>(reader.Schema, rowGroup, "timestamp_utc")[0]
            .Should().Be(expectedTimestamp.ToDateTime().ToUniversalTime());
        ReadColumn<string?>(reader.Schema, rowGroup, "frame")[0].Should().Be("earth");
        ReadColumn<string?>(reader.Schema, rowGroup, "source")[0].Should().Be("sim");
        ReadColumn<string?>(reader.Schema, rowGroup, "job_id")[0].Should().Be(JobId);
        ReadColumn<string?>(reader.Schema, rowGroup, "season_id")[0].Should().Be(SeasonId);
        ReadColumn<string?>(reader.Schema, rowGroup, "session_id")[0].Should().Be(SessionId);
        ReadColumn<double>(reader.Schema, rowGroup, "latitude_deg")[0].Should().BeApproximately(52.1, 1e-9);
        ReadColumn<double>(reader.Schema, rowGroup, "longitude_deg")[0].Should().BeApproximately(-1.2, 1e-9);
        ReadColumn<double>(reader.Schema, rowGroup, "altitude_m")[0].Should().BeApproximately(123.4, 1e-9);
        ReadColumn<double>(reader.Schema, rowGroup, "heading_rad")[0].Should().BeApproximately(1.5, 1e-9);
        ReadColumn<double>(reader.Schema, rowGroup, "roll_rad")[0].Should().BeApproximately(-0.01, 1e-9);
        ReadColumn<double>(reader.Schema, rowGroup, "pitch_rad")[0].Should().BeApproximately(0.02, 1e-9);
        ReadColumn<double>(reader.Schema, rowGroup, "speed_mps")[0].Should().BeApproximately(4.5, 1e-9);
        ReadColumn<double>(reader.Schema, rowGroup, "yaw_rate_radps")[0].Should().BeApproximately(0.1, 1e-9);
    }

    private static void AssertImu(TelemetryParquetLoggerOptions options, Timestamp expectedTimestamp)
    {
        using var stream = File.OpenRead(Path.Combine(options.OutputDirectory, options.ImuFileName));
        using var reader = ParquetReader.Create(stream);
        reader.RowGroupCount.Should().Be(1);

        using var rowGroup = reader.OpenRowGroupReader(0);
        ReadColumn<ulong>(reader.Schema, rowGroup, "sequence")[0].Should().Be(2);
        ReadColumn<DateTime?>(reader.Schema, rowGroup, "timestamp_utc")[0]
            .Should().Be(expectedTimestamp.ToDateTime().ToUniversalTime());
        ReadColumn<string?>(reader.Schema, rowGroup, "frame")[0].Should().Be("imu");
        ReadColumn<string?>(reader.Schema, rowGroup, "source")[0].Should().Be("sim");
        ReadColumn<string?>(reader.Schema, rowGroup, "job_id")[0].Should().Be(JobId);
        ReadColumn<string?>(reader.Schema, rowGroup, "season_id")[0].Should().Be(SeasonId);
        ReadColumn<string?>(reader.Schema, rowGroup, "session_id")[0].Should().Be(SessionId);
        ReadColumn<double>(reader.Schema, rowGroup, "accel_x_mps2")[0].Should().BeApproximately(0.1, 1e-9);
        ReadColumn<double>(reader.Schema, rowGroup, "accel_y_mps2")[0].Should().BeApproximately(-0.2, 1e-9);
        ReadColumn<double>(reader.Schema, rowGroup, "accel_z_mps2")[0].Should().BeApproximately(9.81, 1e-9);
        ReadColumn<double>(reader.Schema, rowGroup, "gyro_x_radps")[0].Should().BeApproximately(0.01, 1e-9);
        ReadColumn<double>(reader.Schema, rowGroup, "gyro_y_radps")[0].Should().BeApproximately(0.02, 1e-9);
        ReadColumn<double>(reader.Schema, rowGroup, "gyro_z_radps")[0].Should().BeApproximately(0.03, 1e-9);
        ReadColumn<double>(reader.Schema, rowGroup, "mag_x_ut")[0].Should().BeApproximately(10.1, 1e-9);
        ReadColumn<double>(reader.Schema, rowGroup, "mag_y_ut")[0].Should().BeApproximately(11.2, 1e-9);
        ReadColumn<double>(reader.Schema, rowGroup, "mag_z_ut")[0].Should().BeApproximately(9.9, 1e-9);
        ReadColumn<double>(reader.Schema, rowGroup, "temperature_c")[0].Should().BeApproximately(35.5, 1e-9);
    }

    private static void AssertCan(TelemetryParquetLoggerOptions options, Timestamp expectedTimestamp)
    {
        using var stream = File.OpenRead(Path.Combine(options.OutputDirectory, options.CanFileName));
        using var reader = ParquetReader.Create(stream);
        reader.RowGroupCount.Should().Be(1);

        using var rowGroup = reader.OpenRowGroupReader(0);
        ReadColumn<ulong>(reader.Schema, rowGroup, "sequence")[0].Should().Be(3);
        ReadColumn<DateTime?>(reader.Schema, rowGroup, "timestamp_utc")[0]
            .Should().Be(expectedTimestamp.ToDateTime().ToUniversalTime());
        ReadColumn<string?>(reader.Schema, rowGroup, "frame")[0].Should().Be("vehicle");
        ReadColumn<string?>(reader.Schema, rowGroup, "source")[0].Should().Be("can");
        ReadColumn<string?>(reader.Schema, rowGroup, "job_id")[0].Should().Be(JobId);
        ReadColumn<string?>(reader.Schema, rowGroup, "season_id")[0].Should().Be(SeasonId);
        ReadColumn<string?>(reader.Schema, rowGroup, "session_id")[0].Should().Be(SessionId);
        ReadColumn<uint>(reader.Schema, rowGroup, "arbitration_id")[0].Should().Be(0x18FF50);
        var payload = ReadColumn<byte[]?>(reader.Schema, rowGroup, "payload")[0];
        payload.Should().NotBeNull();
        payload!.Should().Equal(0xAA, 0xBB, 0xCC);
        ReadColumn<bool>(reader.Schema, rowGroup, "is_extended_id")[0].Should().BeTrue();
        ReadColumn<bool>(reader.Schema, rowGroup, "is_remote_request")[0].Should().BeFalse();
    }

    private static void AssertIo(TelemetryParquetLoggerOptions options, Timestamp expectedTimestamp)
    {
        using var stream = File.OpenRead(Path.Combine(options.OutputDirectory, options.IoFileName));
        using var reader = ParquetReader.Create(stream);
        reader.RowGroupCount.Should().Be(1);

        using var rowGroup = reader.OpenRowGroupReader(0);
        ReadColumn<ulong>(reader.Schema, rowGroup, "sequence")[0].Should().Be(4);
        ReadColumn<DateTime?>(reader.Schema, rowGroup, "timestamp_utc")[0]
            .Should().Be(expectedTimestamp.ToDateTime().ToUniversalTime());
        ReadColumn<string?>(reader.Schema, rowGroup, "frame")[0].Should().Be("sections");
        ReadColumn<string?>(reader.Schema, rowGroup, "source")[0].Should().Be("sim");
        ReadColumn<string?>(reader.Schema, rowGroup, "job_id")[0].Should().Be(JobId);
        ReadColumn<string?>(reader.Schema, rowGroup, "season_id")[0].Should().Be(SeasonId);
        ReadColumn<string?>(reader.Schema, rowGroup, "session_id")[0].Should().Be(SessionId);
        ReadColumn<uint>(reader.Schema, rowGroup, "section_count")[0].Should().Be(6);
        ReadColumn<uint>(reader.Schema, rowGroup, "mask")[0].Should().Be(0b001011);
    }

    private static void AssertPlugin(TelemetryParquetLoggerOptions options, Timestamp expectedTimestamp)
    {
        using var stream = File.OpenRead(Path.Combine(options.OutputDirectory, options.PluginFileName));
        using var reader = ParquetReader.Create(stream);
        reader.RowGroupCount.Should().Be(1);

        using var rowGroup = reader.OpenRowGroupReader(0);
        ReadColumn<ulong>(reader.Schema, rowGroup, "sequence")[0].Should().Be(5);
        ReadColumn<DateTime?>(reader.Schema, rowGroup, "timestamp_utc")[0]
            .Should().Be(expectedTimestamp.ToDateTime().ToUniversalTime());
        ReadColumn<string?>(reader.Schema, rowGroup, "source")[0].Should().Be("plugin-host");
        ReadColumn<string?>(reader.Schema, rowGroup, "job_id")[0].Should().Be(JobId);
        ReadColumn<string?>(reader.Schema, rowGroup, "season_id")[0].Should().Be(SeasonId);
        ReadColumn<string?>(reader.Schema, rowGroup, "session_id")[0].Should().Be(SessionId);
        ReadColumn<string>(reader.Schema, rowGroup, "plugin_id")[0].Should().Be("autosteer");
        ReadColumn<string>(reader.Schema, rowGroup, "topic")[0].Should().Be("state");
        var payload = ReadColumn<byte[]?>(reader.Schema, rowGroup, "payload")[0];
        payload.Should().NotBeNull();
        payload!.Should().Equal(0x01, 0x02, 0x03);
    }

    private static T[] ReadColumn<T>(Schema schema, ParquetRowGroupReader reader, string columnName)
    {
        var field = (DataField)schema.DataFields.Single(
            f => string.Equals(f.Name, columnName, StringComparison.OrdinalIgnoreCase));
        return (T[])reader.ReadColumn(field).Data;
    }
}
