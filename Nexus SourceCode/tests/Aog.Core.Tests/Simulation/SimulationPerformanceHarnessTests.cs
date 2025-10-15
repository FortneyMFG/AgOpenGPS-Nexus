using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Aog.Core.Simulation.Configuration;
using Aog.Plugins;
using FluentAssertions;
using Xunit;

namespace Aog.Core.Tests.Simulation;

public sealed class SimulationPerformanceHarnessTests
{
    private static readonly Lazy<(SimulationConfiguration Configuration, SimulationPerformanceHarness Harness)> HarnessState = new(LoadConfiguration);

    public static IEnumerable<object[]> ScenarioIds()
    {
        return HarnessState.Value.Configuration.Scenarios
            .Select(scenario => new object[] { scenario.ScenarioId });
    }

    [Theory]
    [MemberData(nameof(ScenarioIds))]
    public async Task Scenario_CompletesWithinBudgetAsync(string scenarioId)
    {
        var (configuration, harness) = HarnessState.Value;
        var scenario = configuration.Scenarios.Single(s => s.ScenarioId == scenarioId);

        var result = await harness.RunScenarioAsync(scenario, iterations: 250);

        var expectedProviders = new HashSet<string>(StringComparer.Ordinal);
        foreach (var route in configuration.Routes)
        {
            expectedProviders.Add(route.Source);
        }

        foreach (var route in scenario.Routes)
        {
            expectedProviders.Add(route.Source);
        }

        var providerLookup = configuration.Providers.ToDictionary(provider => provider.ProviderId, StringComparer.Ordinal);
        var expectedOutputs = expectedProviders.Sum(providerId => providerLookup.TryGetValue(providerId, out var provider)
            ? provider.Outputs.Count
            : 0);

        result.Iterations.Should().Be(250);
        result.ProviderCount.Should().Be(expectedProviders.Count);
        result.TotalOutputs.Should().BeGreaterThan(0);
        result.TotalOutputs.Should().Be(expectedOutputs);
        result.TotalMessages.Should().Be(result.Iterations * result.TotalOutputs);
        result.Elapsed.Should().BeLessThan(TimeSpan.FromMilliseconds(400));
        result.Checksum.Should().NotBe(0d);
        result.BudgetSnapshot.TotalElapsed.Should().Be(result.Elapsed);
        result.BudgetSnapshot.ExpectedMessageCount.Should().Be(result.TotalMessages);
        result.BudgetSnapshot.RecordedMessageCount.Should().Be(result.TotalMessages);
        result.BudgetSnapshot.MessagesPerSecond.Should().BeGreaterThan(6000);
        result.BudgetSnapshot.Topics.Should().HaveCount(result.TotalOutputs);
        result.BudgetSnapshot.Topics.Select(topic => topic.Topic).Should().OnlyHaveUniqueItems();
        result.BudgetSnapshot.Topics.Should().OnlyContain(topic => topic.PublishCount == result.Iterations);
        result.BudgetSnapshot.Topics.Should().OnlyContain(topic => topic.AverageDuration >= TimeSpan.Zero);
    }

    [Fact]
    public async Task ScenarioMatrix_CoversAllPluginProviders()
    {
        var (configuration, _) = HarnessState.Value;
        var manifestProviders = await LoadManifestProvidersAsync();
        var scenarioSources = configuration.Scenarios
            .SelectMany(s => s.Routes)
            .Select(route => route.Source)
            .ToHashSet(StringComparer.Ordinal);

        scenarioSources.Should().NotBeEmpty();
        foreach (var providerId in manifestProviders)
        {
            scenarioSources.Should().Contain(providerId);
        }
    }

    private static (SimulationConfiguration Configuration, SimulationPerformanceHarness Harness) LoadConfiguration()
    {
        var root = GetRepositoryRoot();
        var matrixPath = Path.Combine(root, "docs", "scenarios", "performance-matrix.json");
        var json = File.ReadAllText(matrixPath);
        var configuration = SimulationConfigurationLoader.Load(json);
        var harness = new SimulationPerformanceHarness(configuration);
        return (configuration, harness);
    }

    private static async Task<HashSet<string>> LoadManifestProvidersAsync()
    {
        var root = GetRepositoryRoot();
        var manifestRoot = Path.Combine(root, "docs", "plugins", "manifests");
        var loader = new PluginManifestLoader();
        var providerIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (var path in Directory.GetFiles(manifestRoot, "*.json", SearchOption.AllDirectories))
        {
            var manifest = await loader.LoadAsync(path);
            foreach (var provider in manifest.SimulationProviders)
            {
                providerIds.Add(provider.ProviderId);
            }
        }

        return providerIds;
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
                ?? throw new InvalidOperationException("Failed to locate repository root for simulation performance tests.");
        }

        throw new InvalidOperationException("Failed to locate repository root for simulation performance tests.");
    }
}
