using System;

namespace Aog.Core.Simulation;

/// <summary>
/// Represents the position of the simulation clock as a tick number and elapsed time.
/// </summary>
public readonly record struct SimTime(long Tick, TimeSpan Elapsed)
{
    /// <summary>
    /// Gets the zero time (tick 0, elapsed 00:00:00).
    /// </summary>
    public static SimTime Zero { get; } = new(0, TimeSpan.Zero);

    /// <summary>
    /// Creates a <see cref="SimTime"/> aligned with the supplied fixed step.
    /// </summary>
    /// <param name="tick">The simulation tick number.</param>
    /// <param name="step">The fixed step duration.</param>
    /// <returns>A <see cref="SimTime"/> value representing <paramref name="tick"/> steps.</returns>
    public static SimTime FromTick(long tick, TimeSpan step)
    {
        if (tick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tick));
        }

        if (step <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(step));
        }

        var elapsedTicks = checked(step.Ticks * tick);
        return new SimTime(tick, TimeSpan.FromTicks(elapsedTicks));
    }

    /// <summary>
    /// Indicates whether this value is aligned with the provided fixed step size.
    /// </summary>
    /// <param name="step">The fixed step.</param>
    /// <returns><c>true</c> when <see cref="Elapsed"/> equals <paramref name="step"/> × <see cref="Tick"/>.</returns>
    public bool IsAlignedWith(TimeSpan step)
    {
        if (step <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(step));
        }

        return Elapsed.Ticks == step.Ticks * Tick;
    }

    /// <summary>
    /// Advances the time by the provided step size.
    /// </summary>
    /// <param name="step">The step to apply.</param>
    /// <returns>A new <see cref="SimTime"/> incremented by <paramref name="step"/>.</returns>
    public SimTime Advance(TimeSpan step)
    {
        if (step <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(step));
        }

        var nextTick = checked(Tick + 1);
        return new SimTime(nextTick, Elapsed + step);
    }
}
