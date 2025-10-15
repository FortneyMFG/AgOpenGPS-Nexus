using System;
using System.Collections.Generic;

namespace Aog.Plugins.CostProfit;

/// <summary>
/// Represents aggregated profit metrics for a particular scope filter.
/// </summary>
public sealed class ProfitRollup
{
    private readonly IReadOnlyDictionary<string, decimal> _revenueByCurrency;
    private readonly IReadOnlyDictionary<string, decimal> _costByCurrency;
    private readonly IReadOnlyDictionary<string, decimal> _profitByCurrency;
    private readonly IReadOnlyDictionary<string, decimal> _marginByCurrency;

    internal ProfitRollup(
        CostScopeFilter filter,
        IDictionary<string, decimal> revenueByCurrency,
        IDictionary<string, decimal> costByCurrency,
        IDictionary<string, decimal> profitByCurrency,
        IDictionary<string, decimal> marginByCurrency)
    {
        Filter = filter ?? throw new ArgumentNullException(nameof(filter));
        _revenueByCurrency = new Dictionary<string, decimal>(revenueByCurrency, StringComparer.OrdinalIgnoreCase);
        _costByCurrency = new Dictionary<string, decimal>(costByCurrency, StringComparer.OrdinalIgnoreCase);
        _profitByCurrency = new Dictionary<string, decimal>(profitByCurrency, StringComparer.OrdinalIgnoreCase);
        _marginByCurrency = new Dictionary<string, decimal>(marginByCurrency, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the filter that produced the rollup.
    /// </summary>
    public CostScopeFilter Filter { get; }

    /// <summary>
    /// Gets revenue totals grouped by currency.
    /// </summary>
    public IReadOnlyDictionary<string, decimal> RevenueByCurrency => _revenueByCurrency;

    /// <summary>
    /// Gets cost totals grouped by currency.
    /// </summary>
    public IReadOnlyDictionary<string, decimal> CostByCurrency => _costByCurrency;

    /// <summary>
    /// Gets profit totals grouped by currency.
    /// </summary>
    public IReadOnlyDictionary<string, decimal> ProfitByCurrency => _profitByCurrency;

    /// <summary>
    /// Gets gross margin ratios grouped by currency.
    /// </summary>
    public IReadOnlyDictionary<string, decimal> MarginByCurrency => _marginByCurrency;

    /// <summary>
    /// Gets the revenue total for the requested currency or zero when unavailable.
    /// </summary>
    public decimal GetRevenue(string currency) => TryGet(_revenueByCurrency, currency);

    /// <summary>
    /// Gets the cost total for the requested currency or zero when unavailable.
    /// </summary>
    public decimal GetCost(string currency) => TryGet(_costByCurrency, currency);

    /// <summary>
    /// Gets the profit total for the requested currency or zero when unavailable.
    /// </summary>
    public decimal GetProfit(string currency) => TryGet(_profitByCurrency, currency);

    /// <summary>
    /// Gets the gross margin ratio for the requested currency or zero when unavailable.
    /// </summary>
    public decimal GetMargin(string currency) => TryGet(_marginByCurrency, currency);

    private static decimal TryGet(IReadOnlyDictionary<string, decimal> map, string currency)
    {
        if (string.IsNullOrWhiteSpace(currency))
        {
            return 0m;
        }

        return map.TryGetValue(currency, out var value) ? value : 0m;
    }
}
