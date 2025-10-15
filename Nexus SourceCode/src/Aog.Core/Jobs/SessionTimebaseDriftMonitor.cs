using System;
using System.Collections.Generic;

namespace Aog.Core.Jobs;

/// <summary>
/// Tracks timebase drift for active job sessions by comparing source timestamps against the
/// canonical Core clock. Measurements are accumulated in a sliding window so the monitor can
/// surface when the observed drift rate exceeds the tolerance defined by ADR-021 (≥ 2 ms/min).
/// </summary>
public sealed class SessionTimebaseDriftMonitor
{
    private readonly TimeSpan _observationWindow;
    private readonly TimeSpan _minimumObservation;
    private readonly double _thresholdMillisecondsPerMinute;
    private readonly Dictionary<string, SessionWindow> _sessions = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _mutex = new();

    /// <summary>
    /// Initialises a new instance of the <see cref="SessionTimebaseDriftMonitor"/> class.
    /// </summary>
    /// <param name="observationWindow">Rolling window of canonical time used to evaluate drift.</param>
    /// <param name="thresholdMillisecondsPerMinute">Absolute drift rate that triggers detection.</param>
    /// <param name="minimumObservation">Minimum span of observations required before reporting a drift rate.</param>
    public SessionTimebaseDriftMonitor(
        TimeSpan? observationWindow = null,
        double thresholdMillisecondsPerMinute = 2.0,
        TimeSpan? minimumObservation = null)
    {
        if (thresholdMillisecondsPerMinute <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(thresholdMillisecondsPerMinute));
        }

        _observationWindow = observationWindow ?? TimeSpan.FromMinutes(3);
        if (_observationWindow <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(observationWindow));
        }

        _minimumObservation = minimumObservation ?? TimeSpan.FromSeconds(30);
        if (_minimumObservation < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(minimumObservation));
        }

        _thresholdMillisecondsPerMinute = thresholdMillisecondsPerMinute;
    }

    /// <summary>
    /// Records a new timebase comparison for the supplied session.
    /// </summary>
    /// <param name="sessionId">Stable identifier of the active session.</param>
    /// <param name="canonicalTimestamp">Timestamp derived from the canonical Core clock.</param>
    /// <param name="sourceTimestamp">Timestamp observed from the external time source.</param>
    public void RecordSample(string sessionId, DateTimeOffset canonicalTimestamp, DateTimeOffset sourceTimestamp)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);

        lock (_mutex)
        {
            if (!_sessions.TryGetValue(sessionId, out var window))
            {
                window = new SessionWindow(sessionId);
                _sessions.Add(sessionId, window);
            }

            window.AddSample(canonicalTimestamp, sourceTimestamp, _observationWindow, _minimumObservation, _thresholdMillisecondsPerMinute);
        }
    }

    /// <summary>
    /// Attempts to retrieve the latest drift report for the supplied session.
    /// </summary>
    /// <param name="sessionId">Stable identifier of the session.</param>
    /// <returns>The most recent drift report, or <c>null</c> when no samples have been recorded.</returns>
    public SessionTimebaseDriftReport? TryGetReport(string sessionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);

        lock (_mutex)
        {
            return _sessions.TryGetValue(sessionId, out var window)
                ? window.CreateSnapshot()
                : null;
        }
    }

    /// <summary>
    /// Captures drift reports for all tracked sessions.
    /// </summary>
    /// <returns>A snapshot of drift reports.</returns>
    public IReadOnlyList<SessionTimebaseDriftReport> SnapshotReports()
    {
        lock (_mutex)
        {
            var result = new List<SessionTimebaseDriftReport>(_sessions.Count);
            foreach (var window in _sessions.Values)
            {
                var snapshot = window.CreateSnapshot();
                if (snapshot is not null)
                {
                    result.Add(snapshot);
                }
            }

            return result;
        }
    }

    /// <summary>
    /// Clears any recorded samples for the supplied session.
    /// </summary>
    /// <param name="sessionId">Stable identifier of the session.</param>
    /// <returns><c>true</c> when samples were cleared; otherwise <c>false</c>.</returns>
    public bool ResetSession(string sessionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);

        lock (_mutex)
        {
            return _sessions.Remove(sessionId);
        }
    }

    private sealed class SessionWindow
    {
        private readonly string _sessionId;
        private readonly LinkedList<Sample> _samples = new();
        private SessionTimebaseDriftReport? _snapshot;

        public SessionWindow(string sessionId)
        {
            _sessionId = sessionId;
        }

        public void AddSample(
            DateTimeOffset canonicalTimestamp,
            DateTimeOffset sourceTimestamp,
            TimeSpan observationWindow,
            TimeSpan minimumObservation,
            double thresholdMillisecondsPerMinute)
        {
            if (_samples.Count > 0)
            {
                var last = _samples.Last!.Value;
                if (canonicalTimestamp < last.CanonicalTimestamp)
                {
                    // Ignore out-of-order samples to keep drift calculations monotonic.
                    return;
                }

                if (canonicalTimestamp == last.CanonicalTimestamp)
                {
                    _samples.RemoveLast();
                }
            }

            var offsetMilliseconds = (sourceTimestamp - canonicalTimestamp).TotalMilliseconds;
            _samples.AddLast(new Sample(canonicalTimestamp, sourceTimestamp, offsetMilliseconds));

            var cutoff = canonicalTimestamp - observationWindow;
            while (_samples.Count > 0 && _samples.First!.Value.CanonicalTimestamp < cutoff)
            {
                _samples.RemoveFirst();
            }

            Recalculate(minimumObservation, thresholdMillisecondsPerMinute);
        }

        public SessionTimebaseDriftReport? CreateSnapshot()
        {
            return _snapshot is null ? null : _snapshot with { };
        }

        private void Recalculate(TimeSpan minimumObservation, double thresholdMillisecondsPerMinute)
        {
            if (_samples.Count == 0)
            {
                _snapshot = null;
                return;
            }

            var first = _samples.First!.Value;
            var last = _samples.Last!.Value;
            var observationSpan = last.CanonicalTimestamp - first.CanonicalTimestamp;

            var hasSufficientData = observationSpan >= minimumObservation && observationSpan.TotalMinutes > 0;
            var driftRate = hasSufficientData
                ? (last.OffsetMilliseconds - first.OffsetMilliseconds) / observationSpan.TotalMinutes
                : 0d;

            var offset = TimeSpan.FromMilliseconds(last.OffsetMilliseconds);
            var isDriftDetected = hasSufficientData && Math.Abs(driftRate) >= thresholdMillisecondsPerMinute;

            _snapshot = new SessionTimebaseDriftReport(
                _sessionId,
                last.CanonicalTimestamp,
                last.SourceTimestamp,
                offset,
                driftRate,
                isDriftDetected,
                _samples.Count,
                observationSpan,
                hasSufficientData);
        }
    }

    private readonly record struct Sample(
        DateTimeOffset CanonicalTimestamp,
        DateTimeOffset SourceTimestamp,
        double OffsetMilliseconds);
}

/// <summary>
/// Represents the aggregated drift metrics for a single session.
/// </summary>
/// <param name="SessionId">Identifier of the monitored session.</param>
/// <param name="CanonicalTimestamp">Canonical timestamp of the most recent sample.</param>
/// <param name="SourceTimestamp">Source timestamp of the most recent sample.</param>
/// <param name="Offset">Offset between the source time and canonical time for the latest sample.</param>
/// <param name="DriftRateMillisecondsPerMinute">Estimated drift rate expressed in milliseconds per minute.</param>
/// <param name="IsDriftDetected">Indicates whether the drift rate exceeds the configured threshold.</param>
/// <param name="SampleCount">Number of samples retained in the current observation window.</param>
/// <param name="ObservationWindow">Span between the oldest and newest sample used for the estimate.</param>
/// <param name="HasSufficientData">Indicates whether enough data was collected to estimate a drift rate.</param>
public sealed record SessionTimebaseDriftReport(
    string SessionId,
    DateTimeOffset CanonicalTimestamp,
    DateTimeOffset SourceTimestamp,
    TimeSpan Offset,
    double DriftRateMillisecondsPerMinute,
    bool IsDriftDetected,
    int SampleCount,
    TimeSpan ObservationWindow,
    bool HasSufficientData);
