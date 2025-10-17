using System.Text.Json;
using Aog.Tools.LegacyDataMigrator;
using FluentAssertions;
using Parquet;
using Parquet.Data;
using Parquet.Schema;
using Xunit;

namespace Aog.Tools.LegacyDataMigrator.Tests;

public sealed class LegacyDataMigratorTests
{
    private static readonly string SampleLegacyPath = Path.Combine("Data", "LegacyField");

    [Fact]
    public async Task MigrateAsync_WritesTelemetryAndHistory()
    {
        var legacyPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", SampleLegacyPath));
        using var temp = new TempDirectory();

        var migrator = new LegacyDataMigrator();
        var report = await migrator.MigrateAsync(new LegacyMigrationOptions
        {
            InputDirectory = legacyPath,
            OutputDirectory = temp.Path,
        }).ConfigureAwait(false);

        report.PoseCount.Should().Be(2);
        report.ImuCount.Should().Be(1);
        report.CanCount.Should().Be(1);
        report.SectionCount.Should().Be(1);
        report.PluginCount.Should().Be(1);
        report.WeatherCount.Should().Be(2);
        report.FieldHistoryFields.Should().Be(2);
        report.FieldHistoryEntries.Should().Be(3);
        report.FieldHealthObservationCount.Should().Be(3);
        report.YieldSampleCount.Should().Be(3);
        report.SkippedFiles.Should().BeEmpty();

        var posePath = Path.Combine(temp.Path, "pose.parquet");
        File.Exists(posePath).Should().BeTrue();
        using (var reader = OpenReader(posePath))
        {
            reader.RowGroupCount.Should().Be(1);
            var schema = reader.Schema;
            using var rowGroup = reader.OpenRowGroupReader(0);
            var latitudeField = schema.DataFields.Single(f => f.Name == "latitude_deg");
            var speedField = schema.DataFields.Single(f => f.Name == "speed_mps");
            var latitudes = ReadColumn(rowGroup, latitudeField).Cast<double>().ToArray();
            var speeds = ReadColumn(rowGroup, speedField).Cast<double>().ToArray();

            latitudes.Should().ContainInOrder(45.123, 45.124);
            speeds.Should().Contain(new[] { 5.5, 5.6 });
        }

        var canPath = Path.Combine(temp.Path, "can.parquet");
        using (var reader = OpenReader(canPath))
        {
            reader.RowGroupCount.Should().Be(1);
            var schema = reader.Schema;
            using var rowGroup = reader.OpenRowGroupReader(0);
            var payloadField = schema.DataFields.Single(f => f.Name == "payload");
            var payloads = ReadColumn(rowGroup, payloadField).Cast<byte[]?>().ToArray();
            payloads.Should().HaveCount(1);
            payloads[0].Should().BeEquivalentTo(new byte[] { 0x0A, 0xFF });
        }

        var weatherPath = Path.Combine(temp.Path, "weather.parquet");
        File.Exists(weatherPath).Should().BeTrue();
        using (var reader = OpenReader(weatherPath))
        {
            reader.RowGroupCount.Should().Be(1);
            var schema = reader.Schema;
            using var rowGroup = reader.OpenRowGroupReader(0);
            var temperatureField = schema.DataFields.Single(f => f.Name == "temperature_c");
            var rainfallField = schema.DataFields.Single(f => f.Name == "rainfall_mm");
            var temperatures = ReadColumn(rowGroup, temperatureField).Cast<double?>().ToArray();
            var rainfall = ReadColumn(rowGroup, rainfallField).Cast<double?>().ToArray();

            temperatures.Should().Equal(new double?[] { 12.5, 13.1 });
            rainfall.Should().Equal(new double?[] { 0.3, 0.8 });
        }

        var historyPath = Path.Combine(temp.Path, "field-history.json");
        File.Exists(historyPath).Should().BeTrue();
        using var document = JsonDocument.Parse(File.ReadAllText(historyPath));
        var fields = document.RootElement.GetProperty("Fields").EnumerateArray().ToList();
        fields.Should().HaveCount(2);

        var north = fields.Single(f => f.GetProperty("FieldName").GetString() == "North 40");
        var northEntries = north.GetProperty("Entries").EnumerateArray().ToList();
        northEntries.Should().HaveCount(2);
        northEntries[0].GetProperty("AreaHectares").GetDouble().Should().Be(15.2);
        northEntries[1].GetProperty("Operator").GetString().Should().Be("Bob");

        var riskPath = Path.Combine(temp.Path, "risk.other.layer.json");
        File.Exists(riskPath).Should().BeTrue();
        using (var riskDoc = JsonDocument.Parse(File.ReadAllText(riskPath)))
        {
            var root = riskDoc.RootElement;
            root.GetProperty("kind").GetString().Should().Be("risk.other");
            var metadata = root.GetProperty("metadata");
            var observations = metadata.GetProperty("observations").EnumerateArray().ToList();
            observations.Should().HaveCount(3);
            observations.Select(o => o.GetProperty("severity").GetString())
                .Should().BeEquivalentTo(new[] { "critical", "moderate", "high" });

            var severityCounts = metadata.GetProperty("statistics").GetProperty("severityCounts");
            severityCounts.GetProperty("critical").GetInt32().Should().Be(1);
            severityCounts.GetProperty("high").GetInt32().Should().Be(1);
            severityCounts.GetProperty("moderate").GetInt32().Should().Be(1);
        }

        var yieldPath = Path.Combine(temp.Path, "yield.actual.layer.json");
        File.Exists(yieldPath).Should().BeTrue();
        using (var yieldDoc = JsonDocument.Parse(File.ReadAllText(yieldPath)))
        {
            var root = yieldDoc.RootElement;
            root.GetProperty("kind").GetString().Should().Be("yield.actual");
            root.GetProperty("units").GetString().Should().Be("kg/ha");

            var metadata = root.GetProperty("metadata");
            metadata.GetProperty("statistics").GetProperty("count").GetInt32().Should().Be(3);
            metadata.GetProperty("statistics").GetProperty("mean").GetDouble()
                .Should().BeApproximately(8733.3333, 1e-3);
            metadata.GetProperty("statistics").GetProperty("totalMassKg").GetDouble()
                .Should().BeApproximately(262, 1e-6);
            metadata.GetProperty("grid").GetProperty("cellSizeMeters").GetDouble()
                .Should().BeApproximately(10, 1e-6);
            metadata.GetProperty("aggregation").GetProperty("bins").GetProperty("count").GetInt32().Should().Be(5);
        }
    }

    [Fact]
    public async Task MigrateAsync_ReportsMissingFiles()
    {
        using var tempInput = new TempDirectory();
        using var tempOutput = new TempDirectory();

        var migrator = new LegacyDataMigrator();
        var report = await migrator.MigrateAsync(new LegacyMigrationOptions
        {
            InputDirectory = tempInput.Path,
            OutputDirectory = tempOutput.Path,
        }).ConfigureAwait(false);

        report.PoseCount.Should().Be(0);
        report.ImuCount.Should().Be(0);
        report.CanCount.Should().Be(0);
        report.SectionCount.Should().Be(0);
        report.PluginCount.Should().Be(0);
        report.WeatherCount.Should().Be(0);
        report.FieldHistoryEntries.Should().Be(0);
        report.SkippedFiles.Should().Contain(file => file.Contains("pose.csv", StringComparison.OrdinalIgnoreCase));
        report.SkippedFiles.Should().Contain(file => file.Contains("weather.csv", StringComparison.OrdinalIgnoreCase));
        report.SkippedFiles.Should().Contain(file => file.Contains("field-history.csv", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ProgramMain_ReturnsZeroOnSuccess()
    {
        var legacyPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", SampleLegacyPath));
        using var tempOutput = new TempDirectory();

        var exitCode = await Program.Main(new[] { "migrate", "--input", legacyPath, "--output", tempOutput.Path });
        exitCode.Should().Be(0);

        Directory.EnumerateFiles(tempOutput.Path).Should().Contain(file => file.EndsWith("pose.parquet", StringComparison.OrdinalIgnoreCase));
    }

    private static ParquetReader OpenReader(string path)
    {
        return ParquetReader
            .CreateAsync(path, parquetOptions: null, cancellationToken: CancellationToken.None)
            .ConfigureAwait(false)
            .GetAwaiter()
            .GetResult();
    }

    private static Array ReadColumn(ParquetRowGroupReader rowGroup, DataField field)
    {
        return rowGroup
            .ReadColumnAsync(field, CancellationToken.None)
            .ConfigureAwait(false)
            .GetAwaiter()
            .GetResult()
            .Data;
    }

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "legacy-migration-test-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(Path))
                {
                    Directory.Delete(Path, recursive: true);
                }
            }
            catch
            {
            }
        }
    }
}
