using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aog.Core.Simulation;
using Aog.Core.Simulation.Configuration;

namespace Aog.Core.Tests.Simulation;

internal sealed class SimulationPerformanceHarness
{
    private readonly SimulationGraph _graph;
    private readonly int _outputCount;
    private readonly TimeSpan _step;

    public SimulationPerformanceHarness(SimulationConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var catalog = new SimulationCatalog();
        foreach (var descriptor in configuration.CreateProviderDescriptors())
        {
            catalog.Register(descriptor);
        }

        _graph = catalog.BuildGraph();
        _outputCount = _graph.Providers.Sum(provider => provider.Outputs.Count);
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

        var bus = new InMemorySimBus();
        var accumulator = new HarnessAccumulator();

        foreach (var descriptor in _graph.Providers)
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

            foreach (var descriptor in _graph.Providers)
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

        return new SimulationPerformanceResult(
            iterations,
            _graph.Providers.Count,
            accumulator.TotalMessages,
            _outputCount,
            stopwatch.Elapsed,
            accumulator.Checksum);
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
}

internal sealed record SimulationPerformanceResult(
    int Iterations,
    int ProviderCount,
    int TotalMessages,
    int TotalOutputs,
    TimeSpan Elapsed,
    double Checksum)
{
    public double MessagesPerIteration => (double)TotalMessages / Iterations;
}
