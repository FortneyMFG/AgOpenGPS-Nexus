using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Aog.Plugins.FieldHealth;

/// <summary>
/// Represents a single field health risk observation captured during scouting.
/// </summary>
public sealed record FieldHealthObservation
{
    private static readonly Regex FeatureIdPattern = new("^feature:[A-Za-z0-9._:-]+$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex ObserverPattern = new("^(user|system|plugin):[A-Za-z0-9._:-]+$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex SessionIdPattern = new("^session:[A-Za-z0-9._:-]+$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex GeometryHashPattern = new("^[A-Fa-f0-9]{8,64}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>
    /// Unique identifier for the editable zone feature backing this observation.
    /// </summary>
    public string FeatureId { get; init; } = string.Empty;

    /// <summary>
    /// Optional Zone registry identifier when persisted separately from the layer document.
    /// </summary>
    public Guid? ZoneId { get; init; }

    public string? Label { get; init; }

    /// <summary>
    /// Severity classification for the risk.
    /// </summary>
    public FieldHealthSeverity Severity { get; init; }

    /// <summary>
    /// Timestamp when the observation was captured (UTC).
    /// </summary>
    public DateTimeOffset ObservedAt { get; init; }

    /// <summary>
    /// Actor responsible for capturing the observation.
    /// </summary>
    public string Observer { get; init; } = string.Empty;

    public string? Notes { get; init; }

    public IReadOnlyList<string> Attachments { get; init; } = Array.Empty<string>();

    public string? SessionId { get; init; }

    /// <summary>
    /// Approximate area of the affected zone in hectares.
    /// </summary>
    public double? AreaHa { get; init; }

    /// <summary>
    /// Deterministic hash referencing the zone geometry payload.
    /// </summary>
    public string? GeometryHash { get; init; }

    /// <summary>
    /// Timestamp when the observation attributes were last updated.
    /// </summary>
    public DateTimeOffset? LastUpdatedAt { get; init; }

    /// <summary>
    /// Lifecycle state for analytics and reporting.
    /// </summary>
    public FieldHealthObservationStatus? Status { get; init; }

    /// <summary>
    /// Validates the observation and throws when any property violates schema expectations.
    /// </summary>
    public void Validate()
    {
        if (!FeatureIdPattern.IsMatch(FeatureId ?? string.Empty))
        {
            throw new ArgumentException("FeatureId must match ^feature:[A-Za-z0-9._:-]+$.", nameof(FeatureId));
        }

        if (!Enum.IsDefined(typeof(FieldHealthSeverity), Severity))
        {
            throw new ArgumentOutOfRangeException(nameof(Severity), Severity, "Severity is not supported.");
        }

        if (ObservedAt == default)
        {
            throw new ArgumentException("ObservedAt must be specified.", nameof(ObservedAt));
        }

        if (!ObserverPattern.IsMatch(Observer ?? string.Empty))
        {
            throw new ArgumentException("Observer must match ^(user|system|plugin):[A-Za-z0-9._:-]+$.", nameof(Observer));
        }

        if (SessionId is not null && !SessionIdPattern.IsMatch(SessionId))
        {
            throw new ArgumentException("SessionId must match ^session:[A-Za-z0-9._:-]+$ when provided.", nameof(SessionId));
        }

        if (GeometryHash is not null && !GeometryHashPattern.IsMatch(GeometryHash))
        {
            throw new ArgumentException("GeometryHash must be a hexadecimal string between 8 and 64 characters.", nameof(GeometryHash));
        }

        if (Attachments is null)
        {
            throw new ArgumentException("Attachments collection cannot be null.", nameof(Attachments));
        }

        if (Attachments.Any(a => string.IsNullOrWhiteSpace(a)))
        {
            throw new ArgumentException("Attachments cannot contain null or whitespace entries.", nameof(Attachments));
        }

        if (AreaHa is not null)
        {
            if (!double.IsFinite(AreaHa.Value) || AreaHa.Value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(AreaHa), AreaHa, "AreaHa must be non-negative and finite.");
            }
        }

        if (LastUpdatedAt is { } updated && updated < ObservedAt)
        {
            throw new ArgumentException("LastUpdatedAt cannot be earlier than ObservedAt.", nameof(LastUpdatedAt));
        }

        if (Status is not null && !Enum.IsDefined(typeof(FieldHealthObservationStatus), Status))
        {
            throw new ArgumentOutOfRangeException(nameof(Status), Status, "Status is not supported.");
        }
    }
}
