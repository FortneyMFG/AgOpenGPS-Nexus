using System;
using System.Collections.Generic;
using System.Linq;
using Aog.Core.V1;

namespace Aog.Core.Safety;

/// <summary>
/// Centralises control gating decisions so automation honours spatial constraints
/// published via <see cref="PoseZoneMask"/> samples.
/// </summary>
public sealed class ControlArbiter
{
    private readonly object _gate = new();
    private readonly TimeProvider _timeProvider;
    private ConstraintGateSnapshot _snapshot;

    /// <summary>
    /// Initializes a new instance of the <see cref="ControlArbiter"/> class.
    /// </summary>
    /// <param name="timeProvider">Optional time provider for deterministic testing.</param>
    public ControlArbiter(TimeProvider? timeProvider = null)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
        _snapshot = ConstraintGateSnapshot.CreateClear(_timeProvider.GetUtcNow());
    }

    /// <summary>
    /// Gets the most recent constraint gate snapshot.
    /// </summary>
    public ConstraintGateSnapshot Snapshot
    {
        get
        {
            lock (_gate)
            {
                return _snapshot;
            }
        }
    }

    /// <summary>
    /// Updates the constraint gate using the supplied zone mask and returns the transition details.
    /// </summary>
    /// <param name="zoneMask">Latest pose zone mask (may be <c>null</c> when unavailable).</param>
    /// <param name="timestampUtc">Optional timestamp applied to the snapshot (defaults to <see cref="TimeProvider.GetUtcNow"/>).</param>
    public ConstraintGateTransition UpdateConstraintState(PoseZoneMask? zoneMask, DateTimeOffset? timestampUtc = null)
    {
        var timestamp = timestampUtc ?? _timeProvider.GetUtcNow();
        var nextSnapshot = ConstraintGateSnapshot.FromZoneMask(zoneMask, timestamp);

        lock (_gate)
        {
            var previous = _snapshot;
            var changed = !previous.EquivalentTo(nextSnapshot);

            _snapshot = nextSnapshot;

            var engaged = nextSnapshot.HasActiveConstraint && !previous.HasActiveConstraint;
            var released = !nextSnapshot.HasActiveConstraint && previous.HasActiveConstraint;
            var shouldLog = ShouldLogTransition(previous, nextSnapshot, changed, engaged);
            var loggedReason = shouldLog ? nextSnapshot.ActiveReason : null;

            return new ConstraintGateTransition(changed, engaged, released, shouldLog, loggedReason, previous, nextSnapshot);
        }
    }

    private static bool ShouldLogTransition(ConstraintGateSnapshot previous, ConstraintGateSnapshot current, bool changed, bool engaged)
    {
        if (!current.HasActiveConstraint)
        {
            return false;
        }

        if (!previous.HasActiveConstraint)
        {
            return true;
        }

        if (!changed)
        {
            return engaged;
        }

        if (previous.ActiveReason != current.ActiveReason)
        {
            return true;
        }

        if (!previous.ActiveZoneIds.SequenceEqual(current.ActiveZoneIds, StringComparer.Ordinal))
        {
            return true;
        }

        return false;
    }
}

/// <summary>
/// Represents the active constraint gate state derived from the latest pose zone mask.
/// </summary>
public sealed class ConstraintGateSnapshot
{
    private static readonly string[] EmptyZones = Array.Empty<string>();

    private ConstraintGateSnapshot(
        DateTimeOffset timestampUtc,
        bool autosteerAllowed,
        bool sectionsAllowed,
        ConstraintGateReason? activeReason,
        bool insideBoundary,
        bool insideHeadland,
        string zoneRegistryHash,
        string[] activeZoneIds)
    {
        TimestampUtc = timestampUtc;
        AutosteerAllowed = autosteerAllowed;
        SectionsAllowed = sectionsAllowed;
        ActiveReason = activeReason;
        InsideBoundary = insideBoundary;
        InsideHeadland = insideHeadland;
        ZoneRegistryHash = zoneRegistryHash ?? string.Empty;
        ActiveZoneIds = activeZoneIds;
    }

    /// <summary>
    /// Gets the timestamp associated with the snapshot.
    /// </summary>
    public DateTimeOffset TimestampUtc { get; }

    /// <summary>
    /// Gets a value indicating whether autosteer outputs are currently permitted.
    /// </summary>
    public bool AutosteerAllowed { get; }

    /// <summary>
    /// Gets a value indicating whether section outputs are currently permitted.
    /// </summary>
    public bool SectionsAllowed { get; }

    /// <summary>
    /// Gets the active gate reason, or <c>null</c> when no constraint is blocking outputs.
    /// </summary>
    public ConstraintGateReason? ActiveReason { get; }

    /// <summary>
    /// Gets a value indicating whether the implement footprint is inside the field boundary.
    /// </summary>
    public bool InsideBoundary { get; }

    /// <summary>
    /// Gets a value indicating whether the implement footprint is inside a headland buffer.
    /// </summary>
    public bool InsideHeadland { get; }

    /// <summary>
    /// Gets the zone registry hash that produced the snapshot.
    /// </summary>
    public string ZoneRegistryHash { get; }

    /// <summary>
    /// Gets the ordered zone identifiers intersecting the footprint.
    /// </summary>
    public IReadOnlyList<string> ActiveZoneIds { get; }

    /// <summary>
    /// Gets a value indicating whether a blocking constraint is active.
    /// </summary>
    public bool HasActiveConstraint => ActiveReason is not null;

    /// <summary>
    /// Creates a snapshot representing an unconstrained state.
    /// </summary>
    public static ConstraintGateSnapshot CreateClear(DateTimeOffset timestampUtc)
        => new(timestampUtc, autosteerAllowed: true, sectionsAllowed: true, null, false, false, string.Empty, EmptyZones);

    /// <summary>
    /// Builds a snapshot from the supplied zone mask.
    /// </summary>
    public static ConstraintGateSnapshot FromZoneMask(PoseZoneMask? mask, DateTimeOffset timestampUtc)
    {
        var insideKeepOut = mask?.InsideKeepOut == true;
        var insideWorkDisabled = mask?.InsideWorkDisabled == true;

        var reason = insideKeepOut
            ? ConstraintGateReason.KeepOut
            : insideWorkDisabled
                ? ConstraintGateReason.WorkDisabled
                : null;

        var autosteerAllowed = reason is not ConstraintGateReason.KeepOut;
        var sectionsAllowed = reason is null;

        var activeZones = mask?.ActiveZoneIds.Count > 0
            ? mask.ActiveZoneIds.ToArray()
            : EmptyZones;

        return new ConstraintGateSnapshot(
            timestampUtc,
            autosteerAllowed,
            sectionsAllowed,
            reason,
            mask?.InsideBoundary == true,
            mask?.InsideHeadland == true,
            mask?.ZoneRegistryHash ?? string.Empty,
            activeZones);
    }

    /// <summary>
    /// Determines whether the supplied snapshot represents the same gating state.
    /// </summary>
    public bool EquivalentTo(ConstraintGateSnapshot other)
    {
        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (other is null)
        {
            return false;
        }

        if (AutosteerAllowed != other.AutosteerAllowed || SectionsAllowed != other.SectionsAllowed)
        {
            return false;
        }

        if (ActiveReason != other.ActiveReason || InsideBoundary != other.InsideBoundary || InsideHeadland != other.InsideHeadland)
        {
            return false;
        }

        if (!string.Equals(ZoneRegistryHash, other.ZoneRegistryHash, StringComparison.Ordinal))
        {
            return false;
        }

        if (!ActiveZoneIds.SequenceEqual(other.ActiveZoneIds, StringComparer.Ordinal))
        {
            return false;
        }

        return true;
    }
}

/// <summary>
/// Describes how the control arbiter state changed after evaluating a zone mask.
/// </summary>
/// <param name="StateChanged">True when any gating property changed.</param>
/// <param name="GateEngaged">True when a blocking constraint became active.</param>
/// <param name="GateReleased">True when all blocking constraints cleared.</param>
/// <param name="ShouldLogEvent">True when the transition should be logged for audit purposes.</param>
/// <param name="LoggedReason">Constraint reason to log when <paramref name="ShouldLogEvent"/> is true.</param>
/// <param name="Previous">Snapshot before the update.</param>
/// <param name="Current">Snapshot after the update.</param>
public readonly record struct ConstraintGateTransition(
    bool StateChanged,
    bool GateEngaged,
    bool GateReleased,
    bool ShouldLogEvent,
    ConstraintGateReason? LoggedReason,
    ConstraintGateSnapshot Previous,
    ConstraintGateSnapshot Current);

/// <summary>
/// Known reasons that may trigger the control constraint gate.
/// </summary>
public enum ConstraintGateReason
{
    /// <summary>
    /// The implement footprint intersected a keep-out zone.
    /// </summary>
    KeepOut = 0,

    /// <summary>
    /// The implement footprint intersected a work-disabled zone.
    /// </summary>
    WorkDisabled = 1,
}
