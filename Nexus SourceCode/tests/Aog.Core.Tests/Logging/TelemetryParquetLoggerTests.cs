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
                    Source = "sim"
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
                    Source = "sim"
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
                    Source = "can"
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
                    Source = "sim"
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
                    Source = "plugin-host"
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

    private static void AssertPose(TelemetryParquetLoggerOptions options, Timestamp expectedTimestamp)
    {
        using var stream = File.OpenRead(Path.Combine(options.OutputDirectory, options.PoseFileName));
        using var reader = ParquetReader.Create(stream);
        reader.RowGroupCount.Should().Be(1);

        using var rowGroup = reader.OpenRowGroupReader(0);
        ReadColumn<ulong>(reader.Schema, rowGroup, 0).Should().Equal(1);
        ReadColumn<DateTime?>(reader.Schema, rowGroup, 1)[0].Should().Be(expectedTimestamp.ToDateTime().ToUniversalTime());
        ReadColumn<string?>(reader.Schema, rowGroup, 2)[0].Should().Be("earth");
        ReadColumn<string?>(reader.Schema, rowGroup, 3)[0].Should().Be("sim");
        ReadColumn<double>(reader.Schema, rowGroup, 4)[0].Should().BeApproximately(52.1, 1e-9);
        ReadColumn<double>(reader.Schema, rowGroup, 5)[0].Should().BeApproximately(-1.2, 1e-9);
        ReadColumn<double>(reader.Schema, rowGroup, 6)[0].Should().BeApproximately(123.4, 1e-9);
        ReadColumn<double>(reader.Schema, rowGroup, 7)[0].Should().BeApproximately(1.5, 1e-9);
        ReadColumn<double>(reader.Schema, rowGroup, 8)[0].Should().BeApproximately(-0.01, 1e-9);
        ReadColumn<double>(reader.Schema, rowGroup, 9)[0].Should().BeApproximately(0.02, 1e-9);
        ReadColumn<double>(reader.Schema, rowGroup, 10)[0].Should().BeApproximately(4.5, 1e-9);
        ReadColumn<double>(reader.Schema, rowGroup, 11)[0].Should().BeApproximately(0.1, 1e-9);
    }

    private static void AssertImu(TelemetryParquetLoggerOptions options, Timestamp expectedTimestamp)
    {
        using var stream = File.OpenRead(Path.Combine(options.OutputDirectory, options.ImuFileName));
        using var reader = ParquetReader.Create(stream);
        reader.RowGroupCount.Should().Be(1);

        using var rowGroup = reader.OpenRowGroupReader(0);
        ReadColumn<ulong>(reader.Schema, rowGroup, 0)[0].Should().Be(2);
        ReadColumn<DateTime?>(reader.Schema, rowGroup, 1)[0].Should().Be(expectedTimestamp.ToDateTime().ToUniversalTime());
        ReadColumn<string?>(reader.Schema, rowGroup, 2)[0].Should().Be("imu");
        ReadColumn<string?>(reader.Schema, rowGroup, 3)[0].Should().Be("sim");
        ReadColumn<double>(reader.Schema, rowGroup, 4)[0].Should().BeApproximately(0.1, 1e-9);
        ReadColumn<double>(reader.Schema, rowGroup, 5)[0].Should().BeApproximately(-0.2, 1e-9);
        ReadColumn<double>(reader.Schema, rowGroup, 6)[0].Should().BeApproximately(9.81, 1e-9);
        ReadColumn<double>(reader.Schema, rowGroup, 7)[0].Should().BeApproximately(0.01, 1e-9);
        ReadColumn<double>(reader.Schema, rowGroup, 8)[0].Should().BeApproximately(0.02, 1e-9);
        ReadColumn<double>(reader.Schema, rowGroup, 9)[0].Should().BeApproximately(0.03, 1e-9);
        ReadColumn<double>(reader.Schema, rowGroup, 10)[0].Should().BeApproximately(10.1, 1e-9);
        ReadColumn<double>(reader.Schema, rowGroup, 11)[0].Should().BeApproximately(11.2, 1e-9);
        ReadColumn<double>(reader.Schema, rowGroup, 12)[0].Should().BeApproximately(9.9, 1e-9);
        ReadColumn<double>(reader.Schema, rowGroup, 13)[0].Should().BeApproximately(35.5, 1e-9);
    }

    private static void AssertCan(TelemetryParquetLoggerOptions options, Timestamp expectedTimestamp)
    {
        using var stream = File.OpenRead(Path.Combine(options.OutputDirectory, options.CanFileName));
        using var reader = ParquetReader.Create(stream);
        reader.RowGroupCount.Should().Be(1);

        using var rowGroup = reader.OpenRowGroupReader(0);
        ReadColumn<ulong>(reader.Schema, rowGroup, 0)[0].Should().Be(3);
        ReadColumn<DateTime?>(reader.Schema, rowGroup, 1)[0].Should().Be(expectedTimestamp.ToDateTime().ToUniversalTime());
        ReadColumn<string?>(reader.Schema, rowGroup, 2)[0].Should().Be("vehicle");
        ReadColumn<string?>(reader.Schema, rowGroup, 3)[0].Should().Be("can");
        ReadColumn<uint>(reader.Schema, rowGroup, 4)[0].Should().Be(0x18FF50);
        var payload = ReadColumn<byte[]?>(reader.Schema, rowGroup, 5)[0];
        payload.Should().NotBeNull();
        payload!.Should().Equal(0xAA, 0xBB, 0xCC);
        ReadColumn<bool>(reader.Schema, rowGroup, 6)[0].Should().BeTrue();
        ReadColumn<bool>(reader.Schema, rowGroup, 7)[0].Should().BeFalse();
    }

    private static void AssertIo(TelemetryParquetLoggerOptions options, Timestamp expectedTimestamp)
    {
        using var stream = File.OpenRead(Path.Combine(options.OutputDirectory, options.IoFileName));
        using var reader = ParquetReader.Create(stream);
        reader.RowGroupCount.Should().Be(1);

        using var rowGroup = reader.OpenRowGroupReader(0);
        ReadColumn<ulong>(reader.Schema, rowGroup, 0)[0].Should().Be(4);
        ReadColumn<DateTime?>(reader.Schema, rowGroup, 1)[0].Should().Be(expectedTimestamp.ToDateTime().ToUniversalTime());
        ReadColumn<string?>(reader.Schema, rowGroup, 2)[0].Should().Be("sections");
        ReadColumn<string?>(reader.Schema, rowGroup, 3)[0].Should().Be("sim");
        ReadColumn<uint>(reader.Schema, rowGroup, 4)[0].Should().Be(6);
        ReadColumn<uint>(reader.Schema, rowGroup, 5)[0].Should().Be(0b001011);
    }

    private static void AssertPlugin(TelemetryParquetLoggerOptions options, Timestamp expectedTimestamp)
    {
        using var stream = File.OpenRead(Path.Combine(options.OutputDirectory, options.PluginFileName));
        using var reader = ParquetReader.Create(stream);
        reader.RowGroupCount.Should().Be(1);

        using var rowGroup = reader.OpenRowGroupReader(0);
        ReadColumn<ulong>(reader.Schema, rowGroup, 0)[0].Should().Be(5);
        ReadColumn<DateTime?>(reader.Schema, rowGroup, 1)[0].Should().Be(expectedTimestamp.ToDateTime().ToUniversalTime());
        ReadColumn<string?>(reader.Schema, rowGroup, 2)[0].Should().Be("plugin-host");
        ReadColumn<string>(reader.Schema, rowGroup, 3)[0].Should().Be("autosteer");
        ReadColumn<string>(reader.Schema, rowGroup, 4)[0].Should().Be("state");
        var payload = ReadColumn<byte[]?>(reader.Schema, rowGroup, 5)[0];
        payload.Should().NotBeNull();
        payload!.Should().Equal(0x01, 0x02, 0x03);
    }

    private static T[] ReadColumn<T>(Schema schema, ParquetRowGroupReader reader, int index)
    {
        var field = (DataField)schema.Fields[index];
        return (T[])reader.ReadColumn(field).Data;
    }
}
