using System;

namespace Aog.UI.Avalonia.ViewModels;

/// <summary>
/// Represents an entry in the manual zone override history per ADR-027.
/// </summary>
public sealed class ZoneOverrideEventViewModel
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ZoneOverrideEventViewModel"/> class.
    /// </summary>
    /// <param name="zoneTypeDisplay">Display name for the affected zone type.</param>
    /// <param name="actionDisplay">Display text describing the action that occurred.</param>
    /// <param name="actor">Actor responsible for the override.</param>
    /// <param name="reason">Reason provided for the override.</param>
    /// <param name="timestamp">Timestamp when the event occurred.</param>
    /// <param name="expiresAt">Optional expiry timestamp.</param>
    public ZoneOverrideEventViewModel(
        string zoneTypeDisplay,
        string actionDisplay,
        string actor,
        string reason,
        DateTimeOffset timestamp,
        DateTimeOffset? expiresAt)
    {
        ZoneTypeDisplay = zoneTypeDisplay ?? throw new ArgumentNullException(nameof(zoneTypeDisplay));
        ActionDisplay = actionDisplay ?? throw new ArgumentNullException(nameof(actionDisplay));
        Actor = actor ?? throw new ArgumentNullException(nameof(actor));
        Reason = reason ?? string.Empty;
        Timestamp = timestamp;
        ExpiresAt = expiresAt;
    }

    /// <summary>Gets the display name for the affected zone type.</summary>
    public string ZoneTypeDisplay { get; }

    /// <summary>Gets the description of the action that occurred.</summary>
    public string ActionDisplay { get; }

    /// <summary>Gets the actor who performed the override.</summary>
    public string Actor { get; }

    /// <summary>Gets the reason attached to the override.</summary>
    public string Reason { get; }

    /// <summary>Gets the timestamp for the event.</summary>
    public DateTimeOffset Timestamp { get; }

    /// <summary>Gets the optional expiry timestamp for the override.</summary>
    public DateTimeOffset? ExpiresAt { get; }

    /// <summary>Gets a short timestamp string for display.</summary>
    public string TimestampDisplay => Timestamp.ToLocalTime().ToString("HH:mm:ss");

    /// <summary>Gets an optional expiry timestamp string for display.</summary>
    public string ExpiryDisplay => ExpiresAt?.ToLocalTime().ToString("HH:mm") ?? "—";

    /// <summary>Gets the combined actor/reason display text.</summary>
    public string DetailDisplay => string.IsNullOrWhiteSpace(Reason) ? Actor : $"{Actor} — {Reason}";
}

/// <summary>
/// Structured override event emitted by zone toggle view-models when overrides change state.
/// </summary>
/// <param name="ZoneTypeDisplay">Display name for the affected zone type.</param>
/// <param name="ActionDisplay">Display text describing the change.</param>
/// <param name="Actor">Actor responsible for the change.</param>
/// <param name="Reason">Reason provided for the override change.</param>
/// <param name="Timestamp">Timestamp when the change occurred.</param>
/// <param name="ExpiresAt">Optional expiry timestamp.</param>
public readonly record struct ZoneOverrideEvent(
    string ZoneTypeDisplay,
    string ActionDisplay,
    string Actor,
    string Reason,
    DateTimeOffset Timestamp,
    DateTimeOffset? ExpiresAt);
