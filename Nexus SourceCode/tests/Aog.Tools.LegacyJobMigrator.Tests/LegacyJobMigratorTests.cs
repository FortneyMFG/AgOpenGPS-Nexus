using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Aog.Tools.LegacyJobMigrator;
using FluentAssertions;
using Xunit;

namespace Aog.Tools.LegacyJobMigrator.Tests;

public sealed class LegacyJobMigratorTests
{
    private static readonly string SampleJobPath = Path.Combine("Data", "LegacyJob", "job.json");

    [Fact]
    public async Task MigrateAsync_AddsSessionAndSeason()
    {
        var sourcePath = Path.Combine(AppContext.BaseDirectory, SampleJobPath);
        using var tempFile = TempFile.CopyFrom(sourcePath);

        var migrator = new LegacyJobMigrator();
        var report = await migrator.MigrateAsync(new LegacyJobMigrationOptions
        {
            JobFilePath = tempFile.Path,
            SeasonId = "season:2025",
            SessionName = "Legacy Session",
            OperatorIds = new[] { "user:operator.maya", "user:operator.maya" },
        }).ConfigureAwait(false);

        report.Updated.Should().BeTrue();
        report.Skipped.Should().BeFalse();
        report.SessionId.Should().Be("session:1");
        report.SeasonAssigned.Should().BeTrue();

        using var document = JsonDocument.Parse(File.ReadAllText(tempFile.Path));
        var root = document.RootElement;

        root.GetProperty("schemaVersion").GetString().Should().Be("1.1.0");
        root.GetProperty("context").GetProperty("seasonId").GetString().Should().Be("season:2025");
        root.GetProperty("stats").GetProperty("completedSessionCount").GetInt32().Should().Be(1);

        var sessions = root.GetProperty("sessions").EnumerateArray().ToList();
        sessions.Should().HaveCount(1);
        var session = sessions[0];
        session.GetProperty("id").GetString().Should().Be("session:1");
        session.GetProperty("name").GetString().Should().Be("Legacy Session");
        session.GetProperty("state").GetString().Should().Be("completed");
        session.GetProperty("activeOperators").EnumerateArray().Select(o => o.GetString()).Should().ContainSingle("user:operator.maya");

        var stats = session.GetProperty("stats");
        stats.GetProperty("areaHa").GetDouble().Should().Be(12.5);
        stats.GetProperty("distanceKm").GetDouble().Should().Be(18.2);
        stats.GetProperty("durationSec").GetDouble().Should().Be(7200);
        stats.GetProperty("coveragePct").GetDouble().Should().BeApproximately(95.4, 0.0001);
    }

    [Fact]
    public async Task MigrateAsync_SkipsWhenSessionsExistUnlessForced()
    {
        var sourcePath = Path.Combine(AppContext.BaseDirectory, SampleJobPath);
        using var tempFile = TempFile.CopyFrom(sourcePath);

        // Seed a session entry to simulate a previously migrated job.
        var json = File.ReadAllText(tempFile.Path);
        var legacyWithSession = json.Replace("\n}", ",\n  \"sessions\": [{\"id\": \"session:legacy\", \"state\": \"completed\", \"startedAt\": \"2024-04-10T05:00:00Z\", \"lastModifiedAt\": \"2024-04-11T02:15:00Z\"}]\n}");
        File.WriteAllText(tempFile.Path, legacyWithSession);

        var migrator = new LegacyJobMigrator();
        var skipped = await migrator.MigrateAsync(new LegacyJobMigrationOptions
        {
            JobFilePath = tempFile.Path,
        }).ConfigureAwait(false);

        skipped.Updated.Should().BeFalse();
        skipped.Skipped.Should().BeTrue();

        var forced = await migrator.MigrateAsync(new LegacyJobMigrationOptions
        {
            JobFilePath = tempFile.Path,
            Force = true,
            SessionId = "session:override",
        }).ConfigureAwait(false);

        forced.Updated.Should().BeTrue();
        forced.SessionId.Should().Be("session:override");

        using var document = JsonDocument.Parse(File.ReadAllText(tempFile.Path));
        var sessions = document.RootElement.GetProperty("sessions").EnumerateArray().ToList();
        sessions.Should().HaveCount(1);
        sessions[0].GetProperty("id").GetString().Should().Be("session:override");
    }

    [Fact]
    public async Task ProgramMain_MigratesSingleJob()
    {
        var sourcePath = Path.Combine(AppContext.BaseDirectory, SampleJobPath);
        using var tempFile = TempFile.CopyFrom(sourcePath);

        var exitCode = await Program.Main(new[]
        {
            "migrate",
            "--input", tempFile.Path,
            "--season", "season:2026",
            "--operator", "user:operator.liam",
        });

        exitCode.Should().Be(0);

        using var document = JsonDocument.Parse(File.ReadAllText(tempFile.Path));
        var root = document.RootElement;
        root.GetProperty("context").GetProperty("seasonId").GetString().Should().Be("season:2026");
        root.GetProperty("sessions").EnumerateArray().Should().HaveCount(1);
    }

    private sealed class TempFile : IDisposable
    {
        private TempFile(string path)
        {
            Path = path;
        }

        public string Path { get; }

        public static TempFile CopyFrom(string source)
        {
            var destination = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "legacy-job-migrator-test-" + Guid.NewGuid().ToString("N") + ".json");
            File.Copy(source, destination, overwrite: true);
            return new TempFile(destination);
        }

        public void Dispose()
        {
            try
            {
                if (File.Exists(Path))
                {
                    File.Delete(Path);
                }
            }
            catch
            {
            }
        }
    }
}
