using System;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace Aog.Core.Tests.Replay;

public sealed class ControllerReplayFixtureTests
{
    [Fact]
    public async Task CreateAsync_WritesDeterministicScenario()
    {
        using var tempDirectory = ControllerReplayFixture.CreateTemporaryDirectory();

        var scenario = await ControllerReplayFixture.CreateAsync(tempDirectory.Path);

        scenario.ReplayOptions.InputDirectory.Should().Be(tempDirectory.Path);
        scenario.StartTimestamp.Should().Be(new DateTime(2024, 2, 15, 14, 0, 0, DateTimeKind.Utc));
        scenario.Duration.Should().Be(TimeSpan.FromSeconds(5));
        scenario.ControllerCommands.Should().HaveCount(2);
        scenario.ControllerCommands[0].PluginId.Should().Be("controllers.guidance");
        scenario.ControllerCommands[1].PluginId.Should().Be("controllers.sections");

        var parquetFiles = Directory.GetFiles(tempDirectory.Path, "*.parquet", SearchOption.AllDirectories);
        parquetFiles.Should().NotBeEmpty("fixture should emit parquet logs for replay");
    }
}
