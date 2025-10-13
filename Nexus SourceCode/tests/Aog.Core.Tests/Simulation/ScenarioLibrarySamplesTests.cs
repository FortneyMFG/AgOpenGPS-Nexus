using System;
using System.IO;
using System.Linq;
using Aog.Core.Simulation.Configuration;
using FluentAssertions;
using Xunit;

namespace Aog.Core.Tests.Simulation;

public sealed class ScenarioLibrarySamplesTests
{
    [Fact]
    public void LibrarySamples_ParseSuccessfully()
    {
        var root = GetRepositoryRoot();
        var libraryPath = Path.Combine(root, "docs", "scenarios", "library.json");
        File.Exists(libraryPath).Should().BeTrue("the scenario library document should exist for documentation consumers");

        var json = File.ReadAllText(libraryPath);
        var configuration = SimulationConfigurationLoader.Load(json);

        configuration.Scenarios.Should().HaveCount(3);
        configuration.Scenarios.Select(s => s.ScenarioId)
            .Should().Contain(new[] { "baseline-guidance", "headland-training", "replay-overlay" });
        configuration.Scenarios.Should().OnlyContain(s => s.Routes.Count > 0);
        configuration.Scenarios.Should().OnlyContain(s => s.Options is not null);
    }

    private static string GetRepositoryRoot()
    {
        var path = AppContext.BaseDirectory;
        for (var i = 0; i < 10; i++)
        {
            if (File.Exists(Path.Combine(path, "tasks.md")))
            {
                return path;
            }

            path = Path.GetDirectoryName(path)
                ?? throw new InvalidOperationException("Failed to locate repository root for scenario tests.");
        }

        throw new InvalidOperationException("Failed to locate repository root for scenario tests.");
    }
}
