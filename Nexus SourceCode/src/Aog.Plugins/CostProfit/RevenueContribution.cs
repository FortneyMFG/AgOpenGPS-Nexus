using System;
using System.Collections.Generic;

namespace Aog.Plugins.CostProfit;

/// <summary>
/// Represents a revenue input used when computing profit rollups.
/// </summary>
public sealed class RevenueContribution
{
    private readonly IReadOnlyDictionary<string, string> _attributes;

    /// <summary>
    /// Initializes a new instance of the <see cref="RevenueContribution"/> class.
    /// </summary>
    public RevenueContribution(
        string id,
        CostScope scope,
        decimal amount,
        string currency,
        DateTimeOffset timestamp,
        string source,
        string actor,
        string? layerId = null,
        string? notes = null,
        IReadOnlyDictionary<string, string>? attributes = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Contribution identifier is required.", nameof(id));
        }

        Scope = scope ?? throw new ArgumentNullException(nameof(scope));

        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new ArgumentException("Currency is required.", nameof(currency));
        }

        if (string.IsNullOrWhiteSpace(source))
        {
            throw new ArgumentException("Source is required.", nameof(source));
        }

        if (string.IsNullOrWhiteSpace(actor))
        {
            throw new ArgumentException("Actor is required.", nameof(actor));
        }

        Id = id.Trim();
        Amount = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
        Currency = currency.Trim();
        Timestamp = timestamp;
        Source = source.Trim();
        Actor = actor.Trim();
        LayerId = string.IsNullOrWhiteSpace(layerId) ? null : layerId.Trim();
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        _attributes = attributes is null
            ? new Dictionary<string, string>()
            : new Dictionary<string, string>(attributes, StringComparer.Ordinal);
    }

    /// <summary>
    /// Gets the identifier for the contribution.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the scope that the revenue applies to.
    /// </summary>
    public CostScope Scope { get; }

    /// <summary>
    /// Gets the revenue amount.
    /// </summary>
    public decimal Amount { get; }

    /// <summary>
    /// Gets the currency the amount is denominated in.
    /// </summary>
    public string Currency { get; }

    /// <summary>
    /// Gets the timestamp associated with the contribution.
    /// </summary>
    public DateTimeOffset Timestamp { get; }

    /// <summary>
    /// Gets the source system that produced the revenue data.
    /// </summary>
    public string Source { get; }

    /// <summary>
    /// Gets the actor that ingested or verified the contribution.
    /// </summary>
    public string Actor { get; }

    /// <summary>
    /// Gets an optional profit layer identifier tied to the revenue.
    /// </summary>
    public string? LayerId { get; }

    /// <summary>
    /// Gets optional notes captured with the revenue.
    /// </summary>
    public string? Notes { get; }

    /// <summary>
    /// Gets additional attributes describing the revenue.
    /// </summary>
    public IReadOnlyDictionary<string, string> Attributes => _attributes;
}
