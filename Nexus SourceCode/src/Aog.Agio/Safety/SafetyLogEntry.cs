using System;
using System.Collections.Generic;

namespace Aog.Agio.Safety;

/// <summary>
/// Represents a structured event recorded in the safety log.
/// </summary>
public sealed record SafetyLogEntry
{
    /// <summary>
    /// Gets the timestamp (UTC) when the event occurred.
    /// </summary>
    public DateTimeOffset TimestampUtc { get; init; }

    /// <summary>
    /// Gets the safety event identifier.
    /// </summary>
    public string EventType { get; init; } = string.Empty;

    /// <summary>
    /// Gets additional structured metadata associated with the event.
    /// </summary>
    public IReadOnlyDictionary<string, object?> Data { get; init; } = new Dictionary<string, object?>();

    public static SafetyLogEntry HeartbeatReceived(DateTimeOffset timestampUtc, TimeSpan timeout, bool resumed)
    {
        return new SafetyLogEntry
        {
            TimestampUtc = timestampUtc,
            EventType = SafetyLogEvents.HeartbeatReceived,
            Data = new Dictionary<string, object?>
            {
                ["timeoutMs"] = (int)Math.Round(timeout.TotalMilliseconds),
                ["resumed"] = resumed,
            },
        };
    }

    public static SafetyLogEntry HeartbeatCleared(DateTimeOffset timestampUtc)
    {
        return new SafetyLogEntry
        {
            TimestampUtc = timestampUtc,
            EventType = SafetyLogEvents.HeartbeatCleared,
        };
    }

    public static SafetyLogEntry HeartbeatExpired(DateTimeOffset timestampUtc, TimeSpan timeout, string reason)
    {
        return new SafetyLogEntry
        {
            TimestampUtc = timestampUtc,
            EventType = SafetyLogEvents.HeartbeatExpired,
            Data = new Dictionary<string, object?>
            {
                ["timeoutMs"] = (int)Math.Round(timeout.TotalMilliseconds),
                ["reason"] = reason,
            },
        };
    }

    public static SafetyLogEntry FailsafeApplied(DateTimeOffset timestampUtc, string actuator, string action)
    {
        return new SafetyLogEntry
        {
            TimestampUtc = timestampUtc,
            EventType = SafetyLogEvents.FailsafeApplied,
            Data = new Dictionary<string, object?>
            {
                ["actuator"] = actuator,
                ["action"] = action,
            },
        };
    }
}
