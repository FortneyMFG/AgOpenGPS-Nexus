using System;
using System.Collections.Generic;
using System.Linq;

namespace Aog.Core.Sections;

/// <summary>
/// Enumerates the commanded state for a section output within the control graph.
/// </summary>
public enum SectionCommandState
{
    /// <summary>No command has been observed.</summary>
    Unspecified = 0,

    /// <summary>Section is explicitly commanded off.</summary>
    Off,

    /// <summary>Section is armed/auto but not actively applying product.</summary>
    Armed,

    /// <summary>Section is commanded on and expected to apply product.</summary>
    On
}

/// <summary>
/// Describes the origin of the most recent section state transition.
/// </summary>
public enum SectionCommandSource
{
    /// <summary>Origin could not be determined.</summary>
    Unspecified = 0,

    /// <summary>Operator initiated the transition.</summary>
    Operator,

    /// <summary>Automation/rate controller initiated the transition.</summary>
    Automation,

    /// <summary>Safety or constraint gating forced the transition.</summary>
    Safety,

    /// <summary>Replay or simulation initiated the transition.</summary>
    Replay
}

/// <summary>
/// Represents a single sample on the SectionState timeline.
/// </summary>
/// <param name="Timestamp">Timestamp for the sample.</param>
/// <param name="CommandedState">Latest commanded state.</param>
/// <param name="ActualState">Measured state reported by hardware.</param>
/// <param name="Source">Origin of the command.</param>
/// <param name="ManualOverride">Whether an operator override is active.</param>
/// <param name="WorkOpportunity">Whether conditions indicate product should be applied.</param>
/// <param name="WorkEvent">Whether a product application event occurred.</param>
/// <param name="OpportunityAreaSqMeters">Optional opportunity area represented by this sample.</param>
/// <param name="AppliedAreaSqMeters">Optional applied area represented by this sample.</param>
public sealed record SectionStateSample(
    DateTimeOffset Timestamp,
    SectionCommandState CommandedState,
    SectionCommandState ActualState,
    SectionCommandSource Source,
    bool ManualOverride,
    bool WorkOpportunity,
    bool WorkEvent,
    double? OpportunityAreaSqMeters = null,
    double? AppliedAreaSqMeters = null);

/// <summary>
/// Aggregated state for a section after processing one or more samples.
/// </summary>
/// <param name="SectionId">Identifier for the section.</param>
/// <param name="Timestamp">Timestamp of the latest applied sample.</param>
/// <param name="CommandedState">Latest commanded state.</param>
/// <param name="ActualState">Latest measured state.</param>
/// <param name="Source">Origin of the latest command.</param>
/// <param name="ManualOverride">Whether an operator override is active.</param>
/// <param name="WorkOpportunity">Whether the section currently has a work opportunity.</param>
/// <param name="OpportunityDuration">Cumulative opportunity duration.</param>
/// <param name="ActiveDuration">Cumulative active duration.</param>
/// <param name="CommandCount">Number of commanded state transitions.</param>
/// <param name="EventCount">Number of work events observed.</param>
/// <param name="AppliedAreaSqMeters">Aggregated applied area.</param>
/// <param name="MissedAreaSqMeters">Aggregated missed opportunity area.</param>
public sealed record SectionStateSnapshot(
    string SectionId,
    DateTimeOffset Timestamp,
    SectionCommandState CommandedState,
    SectionCommandState ActualState,
    SectionCommandSource Source,
    bool ManualOverride,
    bool WorkOpportunity,
    TimeSpan OpportunityDuration,
    TimeSpan ActiveDuration,
    int CommandCount,
    int EventCount,
    double AppliedAreaSqMeters,
    double MissedAreaSqMeters);

/// <summary>
/// Calculates opportunity/event tallies for SectionState samples in chronological order.
/// </summary>
public sealed class SectionStateTallyCalculator
{
    private readonly Dictionary<string, SectionAccumulator> _sections = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Applies a SectionState sample for the given section identifier.
    /// </summary>
    /// <param name="sectionId">Stable identifier of the section.</param>
    /// <param name="sample">Sample to apply.</param>
    /// <returns>Snapshot representing the latest state for the section.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="sectionId"/> is empty.</exception>
    public SectionStateSnapshot Apply(string sectionId, SectionStateSample sample)
    {
        if (string.IsNullOrWhiteSpace(sectionId))
        {
            throw new ArgumentException("Section identifier is required.", nameof(sectionId));
        }

        if (!_sections.TryGetValue(sectionId, out var accumulator))
        {
            accumulator = new SectionAccumulator(sectionId);
            _sections.Add(sectionId, accumulator);
        }

        return accumulator.Apply(sample);
    }

    /// <summary>
    /// Produces snapshots for all tracked sections.
    /// </summary>
    public IReadOnlyList<SectionStateSnapshot> GetSnapshots()
    {
        return _sections.Values.Select(static accumulator => accumulator.ToSnapshot()).ToArray();
    }

    private sealed class SectionAccumulator
    {
        private readonly string _sectionId;
        private DateTimeOffset? _lastTimestamp;
        private SectionCommandState _commandedState = SectionCommandState.Unspecified;
        private SectionCommandState _actualState = SectionCommandState.Unspecified;
        private SectionCommandSource _source = SectionCommandSource.Unspecified;
        private bool _manualOverride;
        private bool _workOpportunity;
        private TimeSpan _opportunityDuration;
        private TimeSpan _activeDuration;
        private int _commandCount;
        private int _eventCount;
        private double _appliedArea;
        private double _missedArea;

        internal SectionAccumulator(string sectionId)
        {
            _sectionId = sectionId;
        }

        internal SectionStateSnapshot Apply(SectionStateSample sample)
        {
            if (_lastTimestamp.HasValue && sample.Timestamp < _lastTimestamp.Value)
            {
                throw new ArgumentOutOfRangeException(nameof(sample), "SectionState samples must be processed in chronological order.");
            }

            if (_lastTimestamp.HasValue)
            {
                var delta = sample.Timestamp - _lastTimestamp.Value;
                if (delta < TimeSpan.Zero)
                {
                    throw new ArgumentOutOfRangeException(nameof(sample), "SectionState samples must be processed in chronological order.");
                }

                if (_workOpportunity)
                {
                    _opportunityDuration += delta;
                }

                if (_actualState == SectionCommandState.On)
                {
                    _activeDuration += delta;
                }
            }

            if (_lastTimestamp.HasValue && sample.CommandedState != _commandedState)
            {
                _commandCount++;
            }

            if (sample.WorkEvent)
            {
                _eventCount++;
            }

            if (sample.AppliedAreaSqMeters is > 0)
            {
                _appliedArea += sample.AppliedAreaSqMeters.Value;
            }

            if (sample.OpportunityAreaSqMeters is { } opportunityArea)
            {
                var sanitizedOpportunity = Math.Max(0d, opportunityArea);
                var appliedArea = Math.Max(0d, sample.AppliedAreaSqMeters ?? 0d);

                if (appliedArea < sanitizedOpportunity)
                {
                    _missedArea += sanitizedOpportunity - appliedArea;
                }
            }

            _lastTimestamp = sample.Timestamp;
            _commandedState = sample.CommandedState;
            _actualState = sample.ActualState;
            _source = sample.Source;
            _manualOverride = sample.ManualOverride;
            _workOpportunity = sample.WorkOpportunity;

            return new SectionStateSnapshot(
                _sectionId,
                sample.Timestamp,
                _commandedState,
                _actualState,
                _source,
                _manualOverride,
                _workOpportunity,
                _opportunityDuration,
                _activeDuration,
                _commandCount,
                _eventCount,
                _appliedArea,
                _missedArea);
        }

        internal SectionStateSnapshot ToSnapshot()
        {
            return new SectionStateSnapshot(
                _sectionId,
                _lastTimestamp ?? DateTimeOffset.MinValue,
                _commandedState,
                _actualState,
                _source,
                _manualOverride,
                _workOpportunity,
                _opportunityDuration,
                _activeDuration,
                _commandCount,
                _eventCount,
                _appliedArea,
                _missedArea);
        }
    }
}

