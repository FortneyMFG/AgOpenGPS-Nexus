using System;
using System.Threading;
using System.Threading.Tasks;

namespace Aog.Core.Simulation;

/// <summary>
/// Defines the behaviour of a simulation clock that advances in discrete steps.
/// </summary>
public interface ISimClock
{
    /// <summary>
    /// Gets the fixed step duration used when advancing the clock.
    /// </summary>
    TimeSpan Step { get; }

    /// <summary>
    /// Gets the current simulation time.
    /// </summary>
    SimTime Current { get; }

    /// <summary>
    /// Advances the clock by a single fixed step.
    /// </summary>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>The new simulation time after advancing.</returns>
    ValueTask<SimTime> AdvanceAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets the clock to the provided simulation time.
    /// </summary>
    /// <param name="time">The simulation time to use. When omitted, the clock resets to <see cref="SimTime.Zero"/>.</param>
    void Reset(SimTime? time = null);
}
