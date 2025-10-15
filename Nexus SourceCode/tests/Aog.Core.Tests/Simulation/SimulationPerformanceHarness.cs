using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aog.Core.Simulation;
using Aog.Core.Simulation.Configuration;

namespace Aog.Core.Tests.Simulation;

internal sealed class SimulationPerformanceHarness
{
    private readonly IReadOnlyList<SimulationRouteConfiguration> _baselineRoutes;
    private readonly SimulationGraph _graph;
    private readonly TimeSpan _step;

    public SimulationPerformanceHarness(SimulationConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        _baselineRoutes = configuration.Routes ?? Array.Empty<SimulationRouteConfiguration>();
        var catalog = new SimulationCatalog();
        foreach (var descriptor in configuration.CreateProviderDescriptors())
        {
            catalog.Register(descriptor);
        }

        _graph = catalog.BuildGraph();
        _step = TimeSpan.FromMilliseconds(50);
    }

    public async Task<SimulationPerformanceResult> RunScenarioAsync(
        SimulationScenarioConfiguration scenario,
        int iterations,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        if (iterations <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(iterations));
        }

        var recorder = new SimulationPerformanceBudgetRecorder();
        var bus = new InstrumentedSimBus(new InMemorySimBus(), recorder);
        var accumulator = new HarnessAccumulator();
        var providers = ResolveScenarioProviders(scenario);
        var totalOutputs = providers.Sum(provider => provider.Outputs.Count);
        if (totalOutputs == 0)
        {
            throw new InvalidOperationException($"Scenario '{scenario.ScenarioId}' resolved no outputs to publish.");
        }

        foreach (var descriptor in providers)
        {
            foreach (var topic in descriptor.Outputs)
            {
                bus.Subscribe<SimPayload>(topic, (message, token) =>
                {
                    accumulator.Record(message.Payload);
                    return ValueTask.CompletedTask;
                });
            }
        }

        var seed = scenario.Options?.Seed ?? 2024;
        var rng = new DeterministicSimRng(seed);
        var time = SimTime.Zero;
        var stopwatch = Stopwatch.StartNew();

        for (var tick = 0; tick < iterations; tick++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            foreach (var descriptor in providers)
            {
                var jitter = rng.NextDouble();
                var payload = new SimPayload(descriptor.ProviderId, tick, jitter);
                foreach (var topic in descriptor.Outputs)
                {
                    await bus.PublishAsync(topic, time, payload, cancellationToken).ConfigureAwait(false);
                }
            }

            time = time.Advance(_step);
        }

        stopwatch.Stop();

        var expectedMessages = iterations * totalOutputs;
        var budgetSnapshot = recorder.Snapshot(stopwatch.Elapsed, expectedMessages);

        return new SimulationPerformanceResult(
            iterations,
            providers.Count,
            accumulator.TotalMessages,
            totalOutputs,
            stopwatch.Elapsed,
            accumulator.Checksum,
            budgetSnapshot);
    }

    private sealed record struct SimPayload(string ProviderId, int Tick, double Value);

    private sealed class HarnessAccumulator
    {
        private double _checksum;

        public int TotalMessages { get; private set; }

        public void Record(SimPayload payload)
        {
            TotalMessages++;
            _checksum += Math.Sin(payload.Value * (payload.Tick + 1));
        }

        public double Checksum => _checksum;
    }

    private IReadOnlyList<SimulationProviderDescriptor> ResolveScenarioProviders(SimulationScenarioConfiguration scenario)
    {
        var routeSources = new HashSet<string>(StringComparer.Ordinal);
        foreach (var route in _baselineRoutes)
        {
            if (!string.IsNullOrWhiteSpace(route?.Source))
            {
                routeSources.Add(route.Source);
            }
        }

        if (scenario.Routes is { Count: > 0 })
        {
            foreach (var route in scenario.Routes)
            {
                if (!string.IsNullOrWhiteSpace(route?.Source))
                {
                    routeSources.Add(route.Source);
                }
            }
        }

        var providers = new List<SimulationProviderDescriptor>();
        foreach (var descriptor in _graph.Providers)
        {
            if (routeSources.Contains(descriptor.ProviderId))
            {
                providers.Add(descriptor);
            }
        }

        return providers;
    }
}

internal sealed record SimulationPerformanceResult(
    int Iterations,
    int ProviderCount,
    int TotalMessages,
    int TotalOutputs,
    TimeSpan Elapsed,
    double Checksum,
    SimulationPerformanceBudgetSnapshot BudgetSnapshot)
{
    public double MessagesPerIteration => (double)TotalMessages / Iterations;
}
