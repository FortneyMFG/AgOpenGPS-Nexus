using System;

namespace Aog.Core.Simulation;

/// <summary>
/// A deterministic random number generator backed by <see cref="Random"/>.
/// </summary>
public sealed class DeterministicSimRng : ISimRng
{
    private readonly object _gate = new();
    private Random _random;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeterministicSimRng"/> class.
    /// </summary>
    /// <param name="seed">The seed used to initialize the generator.</param>
    public DeterministicSimRng(int seed)
    {
        Seed = seed;
        _random = new Random(seed);
    }

    /// <inheritdoc />
    public int Seed { get; private set; }

    /// <inheritdoc />
    public int Next()
    {
        lock (_gate)
        {
            return _random.Next();
        }
    }

    /// <inheritdoc />
    public int Next(int maxValue)
    {
        lock (_gate)
        {
            return _random.Next(maxValue);
        }
    }

    /// <inheritdoc />
    public int Next(int minValue, int maxValue)
    {
        lock (_gate)
        {
            return _random.Next(minValue, maxValue);
        }
    }

    /// <inheritdoc />
    public double NextDouble()
    {
        lock (_gate)
        {
            return _random.NextDouble();
        }
    }

    /// <inheritdoc />
    public void NextBytes(Span<byte> buffer)
    {
        lock (_gate)
        {
            _random.NextBytes(buffer);
        }
    }

    /// <inheritdoc />
    public void Reseed(int seed)
    {
        lock (_gate)
        {
            Seed = seed;
            _random = new Random(seed);
        }
    }
}
