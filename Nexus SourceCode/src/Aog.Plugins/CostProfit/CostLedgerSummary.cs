using System;
using System.Collections.Generic;

namespace Aog.Plugins.CostProfit;

/// <summary>
/// Represents aggregated ledger totals for a scope filter.
/// </summary>
public sealed class CostLedgerSummary
{
    private readonly IReadOnlyDictionary<string, decimal> _totalsByCurrency;

    internal CostLedgerSummary(CostScopeFilter filter, Dictionary<string, decimal> totalsByCurrency)
    {
        Filter = filter ?? throw new ArgumentNullException(nameof(filter));
        _totalsByCurrency = new Dictionary<string, decimal>(totalsByCurrency, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the filter that produced the summary.
    /// </summary>
    public CostScopeFilter Filter { get; }

    /// <summary>
    /// Gets a map of currency code to aggregated amount.
    /// </summary>
    public IReadOnlyDictionary<string, decimal> TotalsByCurrency => _totalsByCurrency;
}
