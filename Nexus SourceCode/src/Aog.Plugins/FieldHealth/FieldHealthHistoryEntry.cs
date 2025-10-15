using System;

namespace Aog.Plugins.FieldHealth;

/// <summary>
/// Represents a single historical transition for a field health observation.
/// </summary>
public sealed record FieldHealthHistoryEntry
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FieldHealthHistoryEntry"/> class.
    /// </summary>
    public FieldHealthHistoryEntry(string featureId, FieldHealthObservationStatus status, FieldHealthSeverity severity, DateTimeOffset changedAt)
    {
        if (string.IsNullOrWhiteSpace(featureId))
        {
            throw new ArgumentException("FeatureId must be provided.", nameof(featureId));
        }

        FeatureId = featureId;
        Status = status;
        Severity = severity;
        ChangedAt = changedAt;
    }

    /// <summary>
    /// Identifier for the observation feature associated with this transition.
    /// </summary>
    public string FeatureId { get; init; }

    /// <summary>
    /// Observation status applied at the recorded timestamp.
    /// </summary>
    public FieldHealthObservationStatus Status { get; init; }

    /// <summary>
    /// Severity level applied at the recorded timestamp.
    /// </summary>
    public FieldHealthSeverity Severity { get; init; }

    /// <summary>
    /// Timestamp when the transition occurred.
    /// </summary>
    public DateTimeOffset ChangedAt { get; init; }
}
