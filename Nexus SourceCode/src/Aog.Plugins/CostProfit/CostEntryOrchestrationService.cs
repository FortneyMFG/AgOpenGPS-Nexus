using System;
using System.Collections.Generic;
using System.Globalization;

namespace Aog.Plugins.CostProfit;

/// <summary>
/// Coordinates cost capture flows by normalizing <see cref="CostEntryDraft"/> values
/// and persisting them into the <see cref="CostLedger"/> with idempotency
/// guarantees.
/// </summary>
public sealed class CostEntryOrchestrationService
{
    private readonly CostLedger _ledger;
    private readonly TimeProvider _timeProvider;
    private readonly Dictionary<string, CostRecord> _recordsById;
    private readonly Dictionary<string, string> _externalReferenceIndex;
    private readonly object _sync = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="CostEntryOrchestrationService"/>
    /// class.
    /// </summary>
    /// <param name="ledger">Ledger used to persist cost entries.</param>
    /// <param name="timeProvider">Optional time provider for deterministic testing.</param>
    public CostEntryOrchestrationService(CostLedger ledger, TimeProvider? timeProvider = null)
    {
        _ledger = ledger ?? throw new ArgumentNullException(nameof(ledger));
        _timeProvider = timeProvider ?? TimeProvider.System;
        _recordsById = new Dictionary<string, CostRecord>(StringComparer.Ordinal);
        _externalReferenceIndex = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var record in _ledger.GetRecords())
        {
            _recordsById[record.Id] = record;
            if (!string.IsNullOrWhiteSpace(record.ExternalReference))
            {
                _externalReferenceIndex[record.ExternalReference] = record.Id;
            }
        }
    }

    /// <summary>
    /// Captures a single cost entry draft, performing duplicate detection based on
    /// record identifiers and external references.
    /// </summary>
    /// <param name="draft">Draft to capture.</param>
    /// <returns>The persisted <see cref="CostRecord"/>.</returns>
    public CostRecord CaptureManualEntry(CostEntryDraft draft)
    {
        if (draft is null)
        {
            throw new ArgumentNullException(nameof(draft));
        }

        lock (_sync)
        {
            if (draft.ExternalReference is string externalReference &&
                _externalReferenceIndex.TryGetValue(externalReference, out var existingId) &&
                _recordsById.TryGetValue(existingId, out var existingRecord))
            {
                return existingRecord;
            }

            var recordId = ResolveRecordIdentifier(draft.RecordId);
            if (_recordsById.ContainsKey(recordId))
            {
                throw new InvalidOperationException(string.Create(
                    CultureInfo.InvariantCulture,
                    "Cost record '{0}' has already been captured.",
                    recordId));
            }

            var timestamp = draft.Timestamp ?? _timeProvider.GetUtcNow();
            var source = string.IsNullOrWhiteSpace(draft.Source)
                ? "plugin:cost-profit/manual"
                : draft.Source!;

            var record = new CostRecord(
                recordId,
                draft.Scope,
                draft.Category,
                draft.Amount,
                draft.Currency,
                timestamp,
                draft.Actor,
                draft.Quantity,
                draft.QuantityUnits,
                draft.LayerId,
                draft.InventoryLotId,
                source,
                draft.ExternalReference,
                draft.Notes,
                draft.Attributes);

            _ledger.RecordCost(record);
            _recordsById[record.Id] = record;
            if (draft.ExternalReference is string external)
            {
                _externalReferenceIndex[external] = record.Id;
            }

            return record;
        }
    }

    /// <summary>
    /// Imports a batch of drafts, returning the unique set of captured records in
    /// submission order.
    /// </summary>
    /// <param name="drafts">Drafts to import.</param>
    public IReadOnlyList<CostRecord> ImportBatch(IEnumerable<CostEntryDraft> drafts)
    {
        if (drafts is null)
        {
            throw new ArgumentNullException(nameof(drafts));
        }

        var captured = new List<CostRecord>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var draft in drafts)
        {
            if (draft is null)
            {
                throw new ArgumentException("Draft collections cannot contain null entries.", nameof(drafts));
            }

            var record = CaptureManualEntry(draft);
            if (seen.Add(record.Id))
            {
                captured.Add(record);
            }
        }

        return captured;
    }

    private string ResolveRecordIdentifier(string? requestedId)
    {
        if (!string.IsNullOrWhiteSpace(requestedId))
        {
            return requestedId.Trim();
        }

        string identifier;
        do
        {
            identifier = $"cost:{Guid.NewGuid():N}";
        }
        while (_recordsById.ContainsKey(identifier));

        return identifier;
    }
}
