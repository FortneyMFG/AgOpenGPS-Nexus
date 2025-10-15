using System;
using System.Threading;
using System.Threading.Tasks;

namespace Aog.Core.Simulation;

/// <summary>
/// Aggregates the simulation clock, bus, and deterministic RNG into a reusable fabric.
/// </summary>
public sealed class CompositeSimulationFabric
{
    public CompositeSimulationFabric(SimulationCatalog catalog, ISimClock clock, ISimBus bus, ISimRng rng)
    {
        Catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        Clock = clock ?? throw new ArgumentNullException(nameof(clock));
        Bus = bus ?? throw new ArgumentNullException(nameof(bus));
        Rng = rng ?? throw new ArgumentNullException(nameof(rng));
    }

    /// <summary>Gets the simulation provider catalog.</summary>
    public SimulationCatalog Catalog { get; }

    /// <summary>Gets the simulation clock driving providers.</summary>
    public ISimClock Clock { get; }

    /// <summary>Gets the shared simulation bus.</summary>
    public ISimBus Bus { get; }

    /// <summary>Gets the deterministic RNG seeded for the current session.</summary>
    public ISimRng Rng { get; }

    /// <summary>Builds an execution graph for the registered providers.</summary>
    public SimulationGraph BuildGraph() => Catalog.BuildGraph();

    /// <summary>Advances the simulation clock by one fixed step.</summary>
    public ValueTask<SimTime> AdvanceAsync(CancellationToken cancellationToken = default) => Clock.AdvanceAsync(cancellationToken);

    /// <summary>Resets the clock and RNG to the provided seed.</summary>
    public void Reset(int seed, SimTime? start = null)
    {
        if (Rng is DeterministicSimRng deterministic)
        {
            deterministic.Reseed(seed);
        }
        else
        {
            Rng.Reseed(seed);
        }

        Clock.Reset(start);
    }

    /// <summary>
    /// Creates a fabric with the default deterministic components used by Nexus.
    /// </summary>
    public static CompositeSimulationFabric CreateDefault(TimeSpan? step = null, int seed = 2024)
    {
        var catalog = new SimulationCatalog();
        var clock = new FixedStepSimClock(step ?? TimeSpan.FromMilliseconds(10));
        var bus = new InMemorySimBus();
        var rng = new DeterministicSimRng(seed);
        return new CompositeSimulationFabric(catalog, clock, bus, rng);
    }
}
