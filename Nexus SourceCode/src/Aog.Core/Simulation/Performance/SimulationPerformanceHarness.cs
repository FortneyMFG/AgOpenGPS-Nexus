using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aog.Core.Simulation.Configuration;

namespace Aog.Core.Simulation.Performance;

/// <summary>
/// Executes lightweight synthetic runs across the simulation provider graph to capture
/// performance characteristics for budget enforcement. The harness fans messages across
/// every declared output so downstream instrumentation can compare the observed timings
/// against the reference hardware budgets defined in ADR-026.
/// </summary>
public sealed class SimulationPerformanceHarness
{
    /// <summary>
    /// Default simulation step applied between iterations when no explicit value is supplied.
    /// </summary>
    public static readonly TimeSpan DefaultStep = TimeSpan.FromMilliseconds(50);

    private readonly SimulationGraph _graph;
    private readonly int _outputCount;
    private readonly TimeSpan _step;

    /// <summary>
    /// Initializes a new instance of the <see cref="SimulationPerformanceHarness"/> class by
    /// building a provider graph from the supplied configuration.
    /// </summary>
    /// <param name="configuration">Simulation configuration describing providers and routes.</param>
    /// <param name="step">Optional fixed simulation step. Defaults to <see cref="DefaultStep"/>.</param>
    public SimulationPerformanceHarness(SimulationConfiguration configuration, TimeSpan? step = null)
        : this(CreateGraph(configuration), step ?? DefaultStep)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SimulationPerformanceHarness"/> class from an
    /// existing execution graph.
    /// </summary>
    /// <param name="graph">Ordered simulation provider graph.</param>
    /// <param name="step">Optional fixed simulation step. Defaults to <see cref="DefaultStep"/>.</param>
    public SimulationPerformanceHarness(SimulationGraph graph, TimeSpan? step = null)
        : this(graph ?? throw new ArgumentNullException(nameof(graph)), step ?? DefaultStep)
    {
    }

    private SimulationPerformanceHarness(SimulationGraph graph, TimeSpan step)
    {
        if (step <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(step));
        }

        _graph = graph;
        _outputCount = _graph.Providers.Sum(provider => provider.Outputs.Count);
        _step = step;
    }

    /// <summary>
    /// Runs a deterministic synthetic scenario to capture throughput and latency metrics.
    /// </summary>
    /// <param name="scenario">Scenario configuration used to seed the run.</param>
    /// <param name="iterations">Number of ticks to execute.</param>
    /// <param name="cancellationToken">Token that cancels the run.</param>
    /// <returns>Measured performance sample.</returns>
    public async Task<SimulationPerformanceSample> RunScenarioAsync(
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

        return new SimulationPerformanceSample(
            scenario.ScenarioId,
            iterations,
            _graph.Providers.Count,
            accumulator.TotalMessages,
            _outputCount,
            stopwatch.Elapsed,
            accumulator.Checksum);
    }

    private static SimulationGraph CreateGraph(SimulationConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var catalog = new SimulationCatalog();
        foreach (var descriptor in configuration.CreateProviderDescriptors())
        {
            catalog.Register(descriptor);
        }

        return catalog.BuildGraph();
    }

    private sealed record struct SimPayload(string ProviderId, int Tick, double Value);

    private sealed class HarnessAccumulator
    {
        private double _checksum;

        public int TotalMessages { get; private set; }

        public double Checksum => _checksum;

        public void Record(SimPayload payload)
        {
            TotalMessages++;
            _checksum += Math.Sin(payload.Value * (payload.Tick + 1));
        }
    }
}
