using System;
using System.Collections.Generic;

namespace Aog.Plugins.CostProfit;

/// <summary>
/// Represents a normalized cost entry that the cost/profit plugin ingests.
/// </summary>
public sealed class CostRecord
{
    private readonly IReadOnlyDictionary<string, string> _attributes;

    /// <summary>
    /// Initializes a new instance of the <see cref="CostRecord"/> class.
    /// </summary>
    public CostRecord(
        string id,
        CostScope scope,
        CostCategory category,
        decimal amount,
        string currency,
        DateTimeOffset timestamp,
        string actor,
        double? quantity = null,
        string? quantityUnits = null,
        string? layerId = null,
        string? inventoryLotId = null,
        string? source = null,
        string? externalReference = null,
        string? notes = null,
        IReadOnlyDictionary<string, string>? attributes = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Record identifier is required.", nameof(id));
        }

        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new ArgumentException("Currency is required.", nameof(currency));
        }

        if (string.IsNullOrWhiteSpace(actor))
        {
            throw new ArgumentException("Actor is required.", nameof(actor));
        }

        if (quantity.HasValue && quantity.Value <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero when provided.");
        }

        Id = id.Trim();
        Scope = scope ?? throw new ArgumentNullException(nameof(scope));
        Category = category;
        Amount = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
        Currency = currency.Trim();
        Timestamp = timestamp;
        Actor = actor.Trim();
        Quantity = quantity;
        QuantityUnits = string.IsNullOrWhiteSpace(quantityUnits) ? null : quantityUnits.Trim();
        LayerId = string.IsNullOrWhiteSpace(layerId) ? null : layerId.Trim();
        InventoryLotId = string.IsNullOrWhiteSpace(inventoryLotId) ? null : inventoryLotId.Trim();
        Source = string.IsNullOrWhiteSpace(source) ? "plugin:cost-profit" : source.Trim();
        ExternalReference = string.IsNullOrWhiteSpace(externalReference) ? null : externalReference.Trim();
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        _attributes = attributes is null
            ? new Dictionary<string, string>()
            : new Dictionary<string, string>(attributes, StringComparer.Ordinal);
    }

    /// <summary>
    /// Gets the identifier for the cost record.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the scope that the record applies to.
    /// </summary>
    public CostScope Scope { get; }

    /// <summary>
    /// Gets the cost category.
    /// </summary>
    public CostCategory Category { get; }

    /// <summary>
    /// Gets the recorded cost amount.
    /// </summary>
    public decimal Amount { get; }

    /// <summary>
    /// Gets the ISO 4217 currency code for the cost.
    /// </summary>
    public string Currency { get; }

    /// <summary>
    /// Gets the timestamp when the cost was captured.
    /// </summary>
    public DateTimeOffset Timestamp { get; }

    /// <summary>
    /// Gets the actor or system that captured the record.
    /// </summary>
    public string Actor { get; }

    /// <summary>
    /// Gets the optional quantity associated with the cost.
    /// </summary>
    public double? Quantity { get; }

    /// <summary>
    /// Gets the optional quantity units.
    /// </summary>
    public string? QuantityUnits { get; }

    /// <summary>
    /// Gets an optional layer identifier linked to the cost entry.
    /// </summary>
    public string? LayerId { get; }

    /// <summary>
    /// Gets the optional inventory lot reference.
    /// </summary>
    public string? InventoryLotId { get; }

    /// <summary>
    /// Gets the source that produced the record.
    /// </summary>
    public string Source { get; }

    /// <summary>
    /// Gets an optional external reference identifier.
    /// </summary>
    public string? ExternalReference { get; }

    /// <summary>
    /// Gets optional operator notes.
    /// </summary>
    public string? Notes { get; }

    /// <summary>
    /// Gets custom attributes attached to the record.
    /// </summary>
    public IReadOnlyDictionary<string, string> Attributes => _attributes;

    /// <summary>
    /// Creates a new record with a different amount.
    /// </summary>
    /// <param name="amount">New amount to apply.</param>
    /// <param name="actor">Actor responsible for the adjustment.</param>
    /// <param name="timestamp">Timestamp of the adjustment.</param>
    public CostRecord WithAmount(decimal amount, string actor, DateTimeOffset timestamp)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(actor))
        {
            throw new ArgumentException("Actor must be provided.", nameof(actor));
        }

        return new CostRecord(
            Id,
            Scope,
            Category,
            decimal.Round(amount, 2, MidpointRounding.AwayFromZero),
            Currency,
            timestamp,
            actor,
            Quantity,
            QuantityUnits,
            LayerId,
            InventoryLotId,
            Source,
            ExternalReference,
            Notes,
            _attributes);
    }
}
