using System;
using System.Collections.Generic;

namespace Aog.Plugins.CostProfit;

/// <summary>
/// Describes an inventory lot tracked by the material ledger.
/// </summary>
public sealed class InventoryLotDefinition
{
    private readonly IReadOnlyDictionary<string, string> _attributes;

    /// <summary>
    /// Initializes a new instance of the <see cref="InventoryLotDefinition"/> class.
    /// </summary>
    public InventoryLotDefinition(
        string lotId,
        string sku,
        string displayName,
        string supplier,
        string quantityUnits,
        string currency,
        string storageLocation,
        DateTimeOffset receivedAt,
        string createdBy,
        DateTimeOffset? expiration = null,
        IReadOnlyDictionary<string, string>? attributes = null)
    {
        if (string.IsNullOrWhiteSpace(lotId))
        {
            throw new ArgumentException("Lot identifier is required.", nameof(lotId));
        }

        if (string.IsNullOrWhiteSpace(sku))
        {
            throw new ArgumentException("SKU is required.", nameof(sku));
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Display name is required.", nameof(displayName));
        }

        if (string.IsNullOrWhiteSpace(quantityUnits))
        {
            throw new ArgumentException("Quantity units are required.", nameof(quantityUnits));
        }

        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new ArgumentException("Currency is required.", nameof(currency));
        }

        if (string.IsNullOrWhiteSpace(storageLocation))
        {
            throw new ArgumentException("Storage location is required.", nameof(storageLocation));
        }

        if (string.IsNullOrWhiteSpace(createdBy))
        {
            throw new ArgumentException("Actor is required.", nameof(createdBy));
        }

        LotId = lotId.Trim();
        Sku = sku.Trim();
        DisplayName = displayName.Trim();
        Supplier = string.IsNullOrWhiteSpace(supplier) ? "Unknown" : supplier.Trim();
        QuantityUnits = quantityUnits.Trim();
        Currency = currency.Trim();
        StorageLocation = storageLocation.Trim();
        ReceivedAt = receivedAt;
        CreatedBy = createdBy.Trim();
        Expiration = expiration;
        _attributes = attributes is null
            ? new Dictionary<string, string>()
            : new Dictionary<string, string>(attributes, StringComparer.Ordinal);
    }

    /// <summary>
    /// Gets the lot identifier.
    /// </summary>
    public string LotId { get; }

    /// <summary>
    /// Gets the stock keeping unit identifier.
    /// </summary>
    public string Sku { get; }

    /// <summary>
    /// Gets the human friendly display name.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Gets the supplier or manufacturer name.
    /// </summary>
    public string Supplier { get; }

    /// <summary>
    /// Gets the quantity units (e.g., kg, L, bags).
    /// </summary>
    public string QuantityUnits { get; }

    /// <summary>
    /// Gets the currency used for cost basis calculations.
    /// </summary>
    public string Currency { get; }

    /// <summary>
    /// Gets the storage location description.
    /// </summary>
    public string StorageLocation { get; }

    /// <summary>
    /// Gets the timestamp when the lot was received.
    /// </summary>
    public DateTimeOffset ReceivedAt { get; }

    /// <summary>
    /// Gets the actor who registered the lot.
    /// </summary>
    public string CreatedBy { get; }

    /// <summary>
    /// Gets the optional expiration timestamp.
    /// </summary>
    public DateTimeOffset? Expiration { get; }

    /// <summary>
    /// Gets custom attributes describing compliance or other metadata.
    /// </summary>
    public IReadOnlyDictionary<string, string> Attributes => _attributes;
}
