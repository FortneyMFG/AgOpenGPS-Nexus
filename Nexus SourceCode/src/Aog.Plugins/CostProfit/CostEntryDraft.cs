using System;
using System.Collections.Generic;

namespace Aog.Plugins.CostProfit;

/// <summary>
/// Represents a cost entry draft captured by UI or import workflows before it is
/// committed to the <see cref="CostLedger"/>.
/// </summary>
public sealed class CostEntryDraft
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CostEntryDraft"/> class.
    /// </summary>
    public CostEntryDraft(
        CostScope scope,
        CostCategory category,
        decimal amount,
        string currency,
        string actor,
        DateTimeOffset? timestamp = null,
        string? recordId = null,
        double? quantity = null,
        string? quantityUnits = null,
        string? layerId = null,
        string? inventoryLotId = null,
        string? source = null,
        string? externalReference = null,
        string? notes = null,
        IReadOnlyDictionary<string, string>? attributes = null)
    {
        Scope = scope ?? throw new ArgumentNullException(nameof(scope));
        Category = category;

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

        Amount = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
        Currency = currency.Trim();
        Actor = actor.Trim();
        Timestamp = timestamp;
        RecordId = string.IsNullOrWhiteSpace(recordId) ? null : recordId.Trim();
        Quantity = quantity;
        QuantityUnits = string.IsNullOrWhiteSpace(quantityUnits) ? null : quantityUnits.Trim();
        LayerId = string.IsNullOrWhiteSpace(layerId) ? null : layerId.Trim();
        InventoryLotId = string.IsNullOrWhiteSpace(inventoryLotId) ? null : inventoryLotId.Trim();
        Source = string.IsNullOrWhiteSpace(source) ? null : source.Trim();
        ExternalReference = string.IsNullOrWhiteSpace(externalReference) ? null : externalReference.Trim();
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        Attributes = attributes is null
            ? new Dictionary<string, string>()
            : new Dictionary<string, string>(attributes, StringComparer.Ordinal);
    }

    /// <summary>
    /// Gets the optional record identifier to use when persisting the entry.
    /// </summary>
    public string? RecordId { get; }

    /// <summary>
    /// Gets the scope that the entry applies to.
    /// </summary>
    public CostScope Scope { get; }

    /// <summary>
    /// Gets the cost category associated with the entry.
    /// </summary>
    public CostCategory Category { get; }

    /// <summary>
    /// Gets the monetary amount to record.
    /// </summary>
    public decimal Amount { get; }

    /// <summary>
    /// Gets the ISO 4217 currency code for the entry.
    /// </summary>
    public string Currency { get; }

    /// <summary>
    /// Gets the operator or system that captured the entry.
    /// </summary>
    public string Actor { get; }

    /// <summary>
    /// Gets the optional timestamp supplied by the caller.
    /// </summary>
    public DateTimeOffset? Timestamp { get; }

    /// <summary>
    /// Gets the optional quantity associated with the entry.
    /// </summary>
    public double? Quantity { get; }

    /// <summary>
    /// Gets the optional quantity units.
    /// </summary>
    public string? QuantityUnits { get; }

    /// <summary>
    /// Gets an optional layer identifier related to the entry.
    /// </summary>
    public string? LayerId { get; }

    /// <summary>
    /// Gets an optional inventory lot reference.
    /// </summary>
    public string? InventoryLotId { get; }

    /// <summary>
    /// Gets an optional source label used when persisting the entry.
    /// </summary>
    public string? Source { get; }

    /// <summary>
    /// Gets an optional external reference used for idempotency.
    /// </summary>
    public string? ExternalReference { get; }

    /// <summary>
    /// Gets optional operator notes.
    /// </summary>
    public string? Notes { get; }

    /// <summary>
    /// Gets custom attributes to persist on the resulting cost record.
    /// </summary>
    public IReadOnlyDictionary<string, string> Attributes { get; }
}
