using System;
using System.Collections.Generic;
using System.Linq;

namespace Aog.Core.Simulation;

/// <summary>
/// Aggregates publish timings for simulation topics so scenarios can enforce performance budgets.
/// </summary>
public sealed class SimulationPerformanceBudgetRecorder
{
    private readonly object _gate = new();
    private readonly Dictionary<string, TopicStatistics> _topics = new(StringComparer.OrdinalIgnoreCase);
    private long _totalMessages;

    /// <summary>
    /// Records a publish operation for the given topic.
    /// </summary>
    /// <param name="topic">Topic identifier.</param>
    /// <param name="duration">Elapsed time for the publish operation.</param>
    public void RecordPublish(string topic, TimeSpan duration)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);
        if (duration < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(duration), "Duration cannot be negative.");
        }

        lock (_gate)
        {
            if (!_topics.TryGetValue(topic, out var stats))
            {
                stats = new TopicStatistics(topic);
                _topics[topic] = stats;
            }

            stats.Record(duration);
            _totalMessages++;
        }
    }

    /// <summary>
    /// Generates a snapshot of the recorded metrics.
    /// </summary>
    /// <param name="totalElapsed">Total elapsed time for the scenario.</param>
    /// <param name="expectedMessages">Expected message count for the scenario.</param>
    /// <returns>A snapshot of publish metrics.</returns>
    public SimulationPerformanceBudgetSnapshot Snapshot(TimeSpan totalElapsed, int expectedMessages)
    {
        if (totalElapsed < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(totalElapsed), "Elapsed time cannot be negative.");
        }

        lock (_gate)
        {
            var topics = _topics.Values
                .Select(stats => stats.ToTopicPerformance())
                .OrderBy(entry => entry.Topic, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            return new SimulationPerformanceBudgetSnapshot(totalElapsed, expectedMessages, _totalMessages, topics);
        }
    }

    private sealed class TopicStatistics
    {
        private readonly string _topic;
        private long _count;
        private long _totalTicks;
        private long _maxTicks;

        public TopicStatistics(string topic)
        {
            _topic = topic;
        }

        public void Record(TimeSpan duration)
        {
            var ticks = duration.Ticks;
            if (ticks < 0)
            {
                ticks = 0;
            }

            _count++;
            _totalTicks += ticks;
            if (ticks > _maxTicks)
            {
                _maxTicks = ticks;
            }
        }

        public SimulationTopicPerformance ToTopicPerformance()
        {
            var total = TimeSpan.FromTicks(_totalTicks);
            var max = TimeSpan.FromTicks(_maxTicks);
            return new SimulationTopicPerformance(_topic, _count, total, max);
        }
    }
}

/// <summary>
/// Snapshot of simulation publish performance for enforcing budgets.
/// </summary>
public sealed record SimulationPerformanceBudgetSnapshot(
    TimeSpan TotalElapsed,
    int ExpectedMessageCount,
    long RecordedMessageCount,
    IReadOnlyList<SimulationTopicPerformance> Topics)
{
    /// <summary>Gets the average publish rate in messages per second.</summary>
    public double MessagesPerSecond => TotalElapsed <= TimeSpan.Zero
        ? double.PositiveInfinity
        : RecordedMessageCount / TotalElapsed.TotalSeconds;
}

/// <summary>
/// Aggregated publish metrics for a single topic.
/// </summary>
public sealed record SimulationTopicPerformance(
    string Topic,
    long PublishCount,
    TimeSpan TotalDuration,
    TimeSpan MaxDuration)
{
    /// <summary>Gets the average publish duration for the topic.</summary>
    public TimeSpan AverageDuration => PublishCount == 0
        ? TimeSpan.Zero
        : TimeSpan.FromTicks(TotalDuration.Ticks / PublishCount);
}
