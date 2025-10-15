using System;

namespace Aog.Plugins.CostProfit;

/// <summary>
/// Represents the mutable state of an inventory lot in the ledger.
/// </summary>
public sealed class InventoryLotSnapshot
{
    internal InventoryLotSnapshot(
        InventoryLotDefinition definition,
        double quantityOnHand,
        double committedQuantity,
        decimal totalCostBasis,
        DateTimeOffset updatedAt,
        string updatedBy)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        QuantityOnHand = quantityOnHand;
        CommittedQuantity = committedQuantity;
        TotalCostBasis = totalCostBasis;
        UpdatedAt = updatedAt;
        UpdatedBy = updatedBy;
    }

    /// <summary>
    /// Gets the immutable lot definition.
    /// </summary>
    public InventoryLotDefinition Definition { get; }

    /// <summary>
    /// Gets the quantity currently on hand.
    /// </summary>
    public double QuantityOnHand { get; internal set; }

    /// <summary>
    /// Gets the quantity reserved for scheduled work.
    /// </summary>
    public double CommittedQuantity { get; internal set; }

    /// <summary>
    /// Gets the total cost basis remaining on the lot.
    /// </summary>
    public decimal TotalCostBasis { get; internal set; }

    /// <summary>
    /// Gets the timestamp of the last ledger update.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; internal set; }

    /// <summary>
    /// Gets the actor that last modified the lot state.
    /// </summary>
    public string UpdatedBy { get; internal set; }

    /// <summary>
    /// Gets the weighted average cost per unit.
    /// </summary>
    public decimal WeightedCostPerUnit => QuantityOnHand > 0
        ? decimal.Round(TotalCostBasis / (decimal)QuantityOnHand, 4, MidpointRounding.AwayFromZero)
        : 0m;
}
