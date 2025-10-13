using System;
using System.Threading;
using System.Threading.Tasks;

namespace Aog.Core.Simulation;

/// <summary>
/// A deterministic simulation clock that advances in fixed time steps.
/// </summary>
public sealed class FixedStepSimClock : ISimClock
{
    private readonly object _gate = new();
    private SimTime _current;

    /// <summary>
    /// Initializes a new instance of the <see cref="FixedStepSimClock"/> class.
    /// </summary>
    /// <param name="step">The fixed step applied each time the clock advances.</param>
    /// <param name="start">An optional starting time. Defaults to <see cref="SimTime.Zero"/>.</param>
    public FixedStepSimClock(TimeSpan step, SimTime? start = null)
    {
        if (step <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(step));
        }

        Step = step;
        _current = ValidateStart(start ?? SimTime.Zero);
    }

    /// <inheritdoc />
    public TimeSpan Step { get; }

    /// <inheritdoc />
    public SimTime Current
    {
        get
        {
            lock (_gate)
            {
                return _current;
            }
        }
    }

    /// <inheritdoc />
    public ValueTask<SimTime> AdvanceAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            _current = _current.Advance(Step);
            return ValueTask.FromResult(_current);
        }
    }

    /// <inheritdoc />
    public void Reset(SimTime? time = null)
    {
        var value = ValidateStart(time ?? SimTime.Zero);

        lock (_gate)
        {
            _current = value;
        }
    }

    private SimTime ValidateStart(SimTime value)
    {
        if (value.Tick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value));
        }

        if (value.Elapsed < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(value));
        }

        if (!value.IsAlignedWith(Step))
        {
            throw new ArgumentException("The provided start time is not aligned with the fixed step.", nameof(value));
        }

        return value;
    }
}
