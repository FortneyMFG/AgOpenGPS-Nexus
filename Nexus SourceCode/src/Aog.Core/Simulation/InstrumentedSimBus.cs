using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Aog.Core.Simulation;

/// <summary>
/// Wraps another <see cref="ISimBus"/> instance and records publish timings for performance budgets.
/// </summary>
public sealed class InstrumentedSimBus : ISimBus
{
    private readonly ISimBus _inner;
    private readonly SimulationPerformanceBudgetRecorder _recorder;

    /// <summary>
    /// Initializes a new instance of the <see cref="InstrumentedSimBus"/> class.
    /// </summary>
    /// <param name="inner">Underlying bus that delivers messages.</param>
    /// <param name="recorder">Recorder that aggregates publish metrics.</param>
    public InstrumentedSimBus(ISimBus inner, SimulationPerformanceBudgetRecorder recorder)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _recorder = recorder ?? throw new ArgumentNullException(nameof(recorder));
    }

    /// <inheritdoc />
    public IDisposable Subscribe<TMessage>(string topic, Func<SimMessage<TMessage>, CancellationToken, ValueTask> handler)
        => _inner.Subscribe(topic, handler);

    /// <inheritdoc />
    public ValueTask PublishAsync<TMessage>(string topic, SimTime time, TMessage payload, CancellationToken cancellationToken = default)
    {
        var startTimestamp = Stopwatch.GetTimestamp();
        var publishTask = _inner.PublishAsync(topic, time, payload, cancellationToken);

        if (publishTask.IsCompletedSuccessfully)
        {
            _recorder.RecordPublish(topic, CalculateElapsed(startTimestamp));
            return publishTask;
        }

        return AwaitedAsync(publishTask, topic, startTimestamp);
    }

    private async ValueTask AwaitedAsync(ValueTask publishTask, string topic, long startTimestamp)
    {
        try
        {
            await publishTask.ConfigureAwait(false);
        }
        finally
        {
            _recorder.RecordPublish(topic, CalculateElapsed(startTimestamp));
        }
    }

    private static TimeSpan CalculateElapsed(long startTimestamp)
    {
        var elapsedTicks = Stopwatch.GetTimestamp() - startTimestamp;
        var elapsedSeconds = (double)elapsedTicks / Stopwatch.Frequency;
        return TimeSpan.FromSeconds(elapsedSeconds);
    }
}
