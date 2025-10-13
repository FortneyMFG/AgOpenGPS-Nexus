using System;

namespace Aog.Core.Simulation;

/// <summary>
/// Describes a deterministic random number generator bound to the simulation clock.
/// </summary>
public interface ISimRng
{
    /// <summary>
    /// Gets the current seed that produced the sequence.
    /// </summary>
    int Seed { get; }

    /// <summary>
    /// Produces the next non-negative random integer.
    /// </summary>
    int Next();

    /// <summary>
    /// Produces the next non-negative random integer that is less than <paramref name="maxValue"/>.
    /// </summary>
    int Next(int maxValue);

    /// <summary>
    /// Produces a random integer within the specified range.
    /// </summary>
    int Next(int minValue, int maxValue);

    /// <summary>
    /// Produces the next random double in the range [0, 1).
    /// </summary>
    double NextDouble();

    /// <summary>
    /// Fills the provided span with random bytes.
    /// </summary>
    void NextBytes(Span<byte> buffer);

    /// <summary>
    /// Resets the generator to a new deterministic sequence based on <paramref name="seed"/>.
    /// </summary>
    void Reseed(int seed);
}
