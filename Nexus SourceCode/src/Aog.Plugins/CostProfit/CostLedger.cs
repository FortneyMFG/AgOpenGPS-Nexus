using System;
using System.Collections.Generic;
using System.Linq;

namespace Aog.Plugins.CostProfit;

/// <summary>
/// In-memory ledger that manages cost records and inventory lot balances.
/// </summary>
public sealed class CostLedger
{
    private readonly Dictionary<string, CostRecord> _records = new(StringComparer.Ordinal);
    private readonly Dictionary<string, InventoryLotSnapshot> _lots = new(StringComparer.Ordinal);
    private readonly object _sync = new();

    /// <summary>
    /// Records a direct cost entry in the ledger.
    /// </summary>
    /// <param name="record">Cost record to store.</param>
    public void RecordCost(CostRecord record)
    {
        if (record is null)
        {
            throw new ArgumentNullException(nameof(record));
        }

        lock (_sync)
        {
            if (_records.ContainsKey(record.Id))
            {
                throw new InvalidOperationException($"Cost record '{record.Id}' has already been recorded.");
            }

            _records[record.Id] = record;
        }
    }

    /// <summary>
    /// Registers a new inventory lot with optional initial quantity and cost basis.
    /// </summary>
    /// <param name="definition">Lot definition.</param>
    /// <param name="initialQuantity">Quantity received when the lot is created.</param>
    /// <param name="initialCost">Cost associated with the initial quantity.</param>
    /// <param name="actor">Actor performing the registration.</param>
    /// <param name="timestamp">Timestamp of the registration.</param>
    public InventoryLotSnapshot RegisterLot(
        InventoryLotDefinition definition,
        double initialQuantity,
        decimal initialCost,
        string actor,
        DateTimeOffset timestamp)
    {
        if (definition is null)
        {
            throw new ArgumentNullException(nameof(definition));
        }

        if (initialQuantity < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(initialQuantity), "Quantity cannot be negative.");
        }

        if (initialCost < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(initialCost), "Cost basis cannot be negative.");
        }

        if (string.IsNullOrWhiteSpace(actor))
        {
            throw new ArgumentException("Actor is required.", nameof(actor));
        }

        var normalizedActor = actor.Trim();
        lock (_sync)
        {
            if (_lots.ContainsKey(definition.LotId))
            {
                throw new InvalidOperationException($"Inventory lot '{definition.LotId}' has already been registered.");
            }

            var snapshot = new InventoryLotSnapshot(
                definition,
                initialQuantity,
                committedQuantity: 0,
                totalCostBasis: decimal.Round(initialCost, 2, MidpointRounding.AwayFromZero),
                updatedAt: timestamp,
                updatedBy: normalizedActor);

            _lots[definition.LotId] = snapshot;
            return Clone(snapshot);
        }
    }

    /// <summary>
    /// Receives inventory into an existing lot and updates the weighted cost basis.
    /// </summary>
    public InventoryLotSnapshot ReceiveInventory(
        string lotId,
        double quantity,
        decimal cost,
        string actor,
        DateTimeOffset timestamp)
    {
        if (string.IsNullOrWhiteSpace(lotId))
        {
            throw new ArgumentException("Lot identifier is required.", nameof(lotId));
        }

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero.");
        }

        if (cost < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(cost), "Cost basis cannot be negative.");
        }

        if (string.IsNullOrWhiteSpace(actor))
        {
            throw new ArgumentException("Actor is required.", nameof(actor));
        }

        var normalizedActor = actor.Trim();
        lock (_sync)
        {
            if (!_lots.TryGetValue(lotId, out var snapshot))
            {
                throw new KeyNotFoundException($"Inventory lot '{lotId}' has not been registered.");
            }

            snapshot.QuantityOnHand += quantity;
            snapshot.TotalCostBasis = decimal.Round(snapshot.TotalCostBasis + cost, 2, MidpointRounding.AwayFromZero);
            snapshot.UpdatedAt = timestamp;
            snapshot.UpdatedBy = normalizedActor;
            return Clone(snapshot);
        }
    }

    /// <summary>
    /// Commits inventory for a planned operation.
    /// </summary>
    public InventoryLotSnapshot CommitInventory(
        string lotId,
        double quantity,
        string actor,
        DateTimeOffset timestamp)
    {
        if (string.IsNullOrWhiteSpace(lotId))
        {
            throw new ArgumentException("Lot identifier is required.", nameof(lotId));
        }

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(actor))
        {
            throw new ArgumentException("Actor is required.", nameof(actor));
        }

        var normalizedActor = actor.Trim();
        lock (_sync)
        {
            if (!_lots.TryGetValue(lotId, out var snapshot))
            {
                throw new KeyNotFoundException($"Inventory lot '{lotId}' has not been registered.");
            }

            if (snapshot.CommittedQuantity + quantity > snapshot.QuantityOnHand)
            {
                throw new InvalidOperationException("Cannot commit more inventory than is on hand.");
            }

            snapshot.CommittedQuantity += quantity;
            snapshot.UpdatedAt = timestamp;
            snapshot.UpdatedBy = normalizedActor;
            return Clone(snapshot);
        }
    }

    /// <summary>
    /// Releases previously committed inventory.
    /// </summary>
    public InventoryLotSnapshot ReleaseInventory(
        string lotId,
        double quantity,
        string actor,
        DateTimeOffset timestamp)
    {
        if (string.IsNullOrWhiteSpace(lotId))
        {
            throw new ArgumentException("Lot identifier is required.", nameof(lotId));
        }

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(actor))
        {
            throw new ArgumentException("Actor is required.", nameof(actor));
        }

        var normalizedActor = actor.Trim();
        lock (_sync)
        {
            if (!_lots.TryGetValue(lotId, out var snapshot))
            {
                throw new KeyNotFoundException($"Inventory lot '{lotId}' has not been registered.");
            }

            if (quantity > snapshot.CommittedQuantity)
            {
                throw new InvalidOperationException("Cannot release more inventory than has been committed.");
            }

            snapshot.CommittedQuantity -= quantity;
            snapshot.UpdatedAt = timestamp;
            snapshot.UpdatedBy = normalizedActor;
            return Clone(snapshot);
        }
    }

    /// <summary>
    /// Consumes inventory from a lot and records the resulting cost entry.
    /// </summary>
    /// <param name="lotId">Inventory lot identifier.</param>
    /// <param name="quantity">Quantity consumed.</param>
    /// <param name="recordId">Identifier applied to the generated cost record.</param>
    /// <param name="scope">Scope that consumed the inventory.</param>
    /// <param name="category">Cost category to record.</param>
    /// <param name="timestamp">Timestamp of the consumption.</param>
    /// <param name="actor">Actor performing the consumption.</param>
    /// <param name="jobLayerId">Optional layer identifier.</param>
    /// <param name="notes">Optional notes.</param>
    public CostRecord ConsumeInventory(
        string lotId,
        double quantity,
        string recordId,
        CostScope scope,
        CostCategory category,
        DateTimeOffset timestamp,
        string actor,
        string? jobLayerId = null,
        string? notes = null)
    {
        if (string.IsNullOrWhiteSpace(lotId))
        {
            throw new ArgumentException("Lot identifier is required.", nameof(lotId));
        }

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(recordId))
        {
            throw new ArgumentException("Record identifier is required.", nameof(recordId));
        }

        if (string.IsNullOrWhiteSpace(actor))
        {
            throw new ArgumentException("Actor is required.", nameof(actor));
        }

        var normalizedActor = actor.Trim();
        lock (_sync)
        {
            if (!_lots.TryGetValue(lotId, out var snapshot))
            {
                throw new KeyNotFoundException($"Inventory lot '{lotId}' has not been registered.");
            }

            if (quantity > snapshot.QuantityOnHand)
            {
                throw new InvalidOperationException("Cannot consume more inventory than is on hand.");
            }

            if (_records.ContainsKey(recordId))
            {
                throw new InvalidOperationException($"Cost record '{recordId}' has already been recorded.");
            }

            var unitCost = snapshot.QuantityOnHand <= 0
                ? 0m
                : snapshot.TotalCostBasis / (decimal)snapshot.QuantityOnHand;

            var amount = decimal.Round(unitCost * (decimal)quantity, 2, MidpointRounding.AwayFromZero);
            if (amount <= 0)
            {
                throw new InvalidOperationException("Calculated cost amount must be greater than zero.");
            }

            snapshot.QuantityOnHand -= quantity;
            snapshot.TotalCostBasis = decimal.Round(snapshot.TotalCostBasis - amount, 2, MidpointRounding.AwayFromZero);
            snapshot.CommittedQuantity = Math.Max(0, snapshot.CommittedQuantity - quantity);
            snapshot.UpdatedAt = timestamp;
            snapshot.UpdatedBy = normalizedActor;

            var record = new CostRecord(
                recordId,
                scope,
                category,
                amount,
                snapshot.Definition.Currency,
                timestamp,
                normalizedActor,
                quantity,
                snapshot.Definition.QuantityUnits,
                jobLayerId,
                snapshot.Definition.LotId,
                source: "plugin:cost-profit/ledger",
                notes: notes);

            _records[record.Id] = record;
            return record;
        }
    }

    /// <summary>
    /// Gets a snapshot of a specific inventory lot.
    /// </summary>
    public InventoryLotSnapshot? GetLot(string lotId)
    {
        if (string.IsNullOrWhiteSpace(lotId))
        {
            throw new ArgumentException("Lot identifier is required.", nameof(lotId));
        }

        lock (_sync)
        {
            return _lots.TryGetValue(lotId, out var snapshot) ? Clone(snapshot) : null;
        }
    }

    /// <summary>
    /// Returns an immutable view of all cost records.
    /// </summary>
    public IReadOnlyCollection<CostRecord> GetRecords()
    {
        lock (_sync)
        {
            return _records.Values.ToArray();
        }
    }

    /// <summary>
    /// Produces an aggregated summary filtered by scope attributes.
    /// </summary>
    public CostLedgerSummary Summarize(CostScopeFilter filter)
    {
        if (filter is null)
        {
            throw new ArgumentNullException(nameof(filter));
        }

        filter.Normalize();
        var totals = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

        lock (_sync)
        {
            foreach (var record in _records.Values)
            {
                if (!record.Scope.Matches(filter))
                {
                    continue;
                }

                if (totals.TryGetValue(record.Currency, out var current))
                {
                    totals[record.Currency] = current + record.Amount;
                }
                else
                {
                    totals[record.Currency] = record.Amount;
                }
            }
        }

        return new CostLedgerSummary(filter, totals);
    }

    private static InventoryLotSnapshot Clone(InventoryLotSnapshot snapshot)
    {
        return new InventoryLotSnapshot(
            snapshot.Definition,
            snapshot.QuantityOnHand,
            snapshot.CommittedQuantity,
            snapshot.TotalCostBasis,
            snapshot.UpdatedAt,
            snapshot.UpdatedBy);
    }
}
