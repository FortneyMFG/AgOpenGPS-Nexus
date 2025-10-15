using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Aog.Core.Jobs;
using Aog.Plugins.JobTasks;
using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Aog.Plugins.Tests.JobTasks;

public sealed class JobTasksPersistenceTests
{
    [Fact]
    public async Task SaveAsync_WritesManifestAndResumeFile()
    {
        using var temp = new TemporaryDirectory();
        var jobRoot = Path.Combine(temp.DirectoryPath, "Jobs", "North40");
        var layout = new JobStoreLayout(
            jobRoot,
            Path.Combine(jobRoot, "data"),
            Path.Combine(jobRoot, "Resume.txt"),
            Path.Combine(jobRoot, "attachments"));

        var createdAt = new DateTimeOffset(2025, 5, 5, 7, 15, 0, TimeSpan.Zero);
        var updatedAt = createdAt.AddHours(2);

        var metadata = new JobMetadata(
            "job:20250505T071500000",
            "north-40-planting",
            "North 40 Planting",
            JobLifecycleState.Active,
            createdAt,
            updatedAt,
            "session:1",
            new JobContext(
                "farm:alpha",
                new[] { "field:north-40" },
                "season:2025",
                "work:order-1",
                "Morning pass"),
            new[] { "corn", "planter" });

        var session = new JobSessionSnapshot(
            "session:1",
            JobSessionState.Active,
            createdAt.AddMinutes(5),
            updatedAt,
            "Morning session",
            null,
            new[] { "user:operator.maya" },
            new JobSessionStatisticsSnapshot(12.4, 8.1, 5400, 58.2),
            null);

        var equipment = new JobEquipmentSnapshot(
            VehicleId: " equipment:tractor.alpha ",
            ImplementId: "equipment:Planter-16R",
            PresetId: " preset:planter-16r ",
            LayoutId: " layout:planter-dashboard ");

        var snapshot = new JobSnapshot(metadata, layout, new[] { session }, Equipment: equipment);
        var timeProvider = new FakeTimeProvider(createdAt.AddHours(3));
        var persistence = new JobTasksPersistence(timeProvider);

        await persistence.SaveAsync(snapshot);

        Directory.Exists(layout.JobRoot).Should().BeTrue();
        Directory.Exists(layout.DataDirectory).Should().BeTrue();
        Directory.Exists(layout.AttachmentsDirectory!).Should().BeTrue();

        var manifestPath = Path.Combine(jobRoot, "job.json");
        File.Exists(manifestPath).Should().BeTrue();

        var manifestJson = await File.ReadAllTextAsync(manifestPath);
        using (var document = JsonDocument.Parse(manifestJson))
        {
            var root = document.RootElement;
            root.GetProperty("jobId").GetString().Should().Be(metadata.JobId);
            root.GetProperty("slug").GetString().Should().Be(metadata.Slug);
            root.GetProperty("state").GetString().Should().Be("active");
            var paths = root.GetProperty("paths");
            paths.GetProperty("jobRoot").GetString().Should().Be(layout.JobRoot);
            paths.GetProperty("resumeFile").GetString().Should().Be(layout.ResumeFile);
            var sessionsElement = root.GetProperty("sessions");
            sessionsElement.GetArrayLength().Should().Be(1);
            sessionsElement[0].GetProperty("id").GetString().Should().Be(session.SessionId);

            var equipmentElement = root.GetProperty("equipment");
            equipmentElement.GetProperty("vehicleId").GetString().Should().Be("equipment:tractor.alpha");
            equipmentElement.GetProperty("implementId").GetString().Should().Be("equipment:Planter-16R");
            equipmentElement.GetProperty("presetId").GetString().Should().Be("preset:planter-16r");
            equipmentElement.GetProperty("layoutId").GetString().Should().Be("layout:planter-dashboard");
        }

        var resumeContent = await File.ReadAllTextAsync(layout.ResumeFile);
        resumeContent.Should().Contain($"JobId={metadata.JobId}");
        resumeContent.Should().Contain($"SavedAt={timeProvider.GetUtcNow():O}");
        resumeContent.Should().Contain("[Session session:1]");
        resumeContent.Should().Contain("VehicleId=equipment:tractor.alpha");
        resumeContent.Should().Contain("ImplementId=equipment:Planter-16R");
        resumeContent.Should().Contain("PresetId=preset:planter-16r");
        resumeContent.Should().Contain("LayoutId=layout:planter-dashboard");

        var loaded = await persistence.LoadAsync(jobRoot);
        loaded.Metadata.Should().Be(metadata);
        loaded.Layout.JobRoot.Should().Be(layout.JobRoot);
        loaded.Layout.ResumeFile.Should().Be(layout.ResumeFile);
        loaded.Sessions.Should().ContainSingle().Which.Should().Be(session);
        loaded.Equipment.Should().Be(new JobEquipmentSnapshot(
            VehicleId: "equipment:tractor.alpha",
            ImplementId: "equipment:Planter-16R",
            PresetId: "preset:planter-16r",
            LayoutId: "layout:planter-dashboard"));
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public string DirectoryPath { get; }

        public TemporaryDirectory()
        {
            DirectoryPath = Path.Combine(Path.GetTempPath(), "aog-jobtasks-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(DirectoryPath);
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(DirectoryPath))
                {
                    Directory.Delete(DirectoryPath, recursive: true);
                }
            }
            catch
            {
                // Ignore cleanup failures in test runs.
            }
        }
    }
}
